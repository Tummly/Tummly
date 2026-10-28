using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Billing.PlanEntitlements;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Locations;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public sealed class OwnedLocationInsertService : IOwnedLocationInsertService
    {
        private static readonly ConcurrentDictionary<int, SemaphoreSlim> AccountLocks
            = new();

        private readonly ApplicationDbContext _context;
        private readonly IPricebookCatalog _pricebookCatalog;
        private readonly IQrCodeProvisioningService _qrCodeProvisioning;
        private readonly IPrintReadyQrMaterialsWork _printReadyQrMaterialsWork;
        private readonly IPremisesReviewService _premises;

        public OwnedLocationInsertService(
            ApplicationDbContext context,
            IPricebookCatalog pricebookCatalog,
            IQrCodeProvisioningService qrCodeProvisioning,
            IPrintReadyQrMaterialsWork printReadyQrMaterialsWork,
            IPremisesReviewService? premises = null
        )
        {
            _context = context;
            _pricebookCatalog = pricebookCatalog;
            _qrCodeProvisioning = qrCodeProvisioning;
            _printReadyQrMaterialsWork = printReadyQrMaterialsWork;
            _premises = premises ?? new PremisesReviewService(context);
        }

        public const int ImportMaxRows = 100;

        /// <summary>
        /// Import row context for the premises check. The maps hold each
        /// premises key and FSA id already created earlier in this file, with
        /// that row's index. Those earlier rows are the caller's own data.
        /// </summary>
        private sealed record ImportRowPremises(
            string? Key,
            string? FsaEstablishmentId,
            IReadOnlyDictionary<string, int> EarlierRowsByKey,
            IReadOnlyDictionary<string, int> EarlierRowsByFsa
        );

        public async Task<AddOwnedLocationResult> AddAsync(
            int restaurantId,
            int actorUserId,
            AddOwnedLocationRequest request
        )
        {
            return await AddCoreAsync(restaurantId, actorUserId, request, importRow: null);
        }

        private async Task<AddOwnedLocationResult> AddCoreAsync(
            int restaurantId,
            int actorUserId,
            AddOwnedLocationRequest request,
            ImportRowPremises? importRow
        )
        {
            var invalid = ValidateDraftFields(request);
            if (invalid != null)
            {
                return invalid;
            }

            if (!_context.Database.IsSqlServer())
            {
                var gate = AccountLocks.GetOrAdd(
                    restaurantId,
                    _ => new SemaphoreSlim(1, 1)
                );
                await gate.WaitAsync();
                try
                {
                    return await AddLockedAsync(
                        restaurantId,
                        actorUserId,
                        request,
                        importRow
                    );
                }
                finally
                {
                    gate.Release();
                }
            }

            return await AddLockedAsync(
                restaurantId,
                actorUserId,
                request,
                importRow
            );
        }

        public async Task<ImportOwnedLocationsResult> ImportAsync(
            int restaurantId,
            int actorUserId,
            ImportOwnedLocationsRequest request
        )
        {
            var rows = request.Rows ?? [];
            if (rows.Count == 0)
            {
                return new ImportOwnedLocationsResult.InvalidRequest(
                    "Add at least one location row."
                );
            }

            if (rows.Count > ImportMaxRows)
            {
                return new ImportOwnedLocationsResult.InvalidRequest(
                    $"Import up to {ImportMaxRows} locations at a time."
                );
            }

            var created = new List<ImportCreatedRow>();
            var errors = new List<ImportErrorRow>();
            ImportErrorRow? remainingRowTemplate = null;
            var earlierRowsByKey = new Dictionary<string, int>(StringComparer.Ordinal);
            var earlierRowsByFsa = new Dictionary<string, int>(StringComparer.Ordinal);

            for (var index = 0; index < rows.Count; index++)
            {
                if (remainingRowTemplate != null)
                {
                    errors.Add(
                        remainingRowTemplate with { RowIndex = index }
                    );
                    continue;
                }

                var premisesKey = PremisesKey.Build(
                    rows[index].Address,
                    rows[index].City,
                    rows[index].Postcode
                );
                var fsaId = PremisesReviewService.NormalizeFsa(
                    rows[index].FsaEstablishmentId
                );
                var result = await AddCoreAsync(
                    restaurantId,
                    actorUserId,
                    rows[index],
                    new ImportRowPremises(
                        premisesKey,
                        fsaId,
                        earlierRowsByKey,
                        earlierRowsByFsa
                    )
                );

                switch (result)
                {
                    case AddOwnedLocationResult.Created ok:
                        created.Add(
                            new ImportCreatedRow(index, ok.LocationId, ok.PremisesWarning)
                        );
                        if (premisesKey != null)
                        {
                            earlierRowsByKey.TryAdd(premisesKey, index);
                        }

                        if (fsaId != null)
                        {
                            earlierRowsByFsa.TryAdd(fsaId, index);
                        }
                        break;
                    case AddOwnedLocationResult.InvalidRequest invalid:
                        errors.Add(
                            new ImportErrorRow(index, invalid.Message)
                        );
                        break;
                    case AddOwnedLocationResult.CapReached reached:
                        remainingRowTemplate = new ImportErrorRow(
                            index,
                            reached.Message
                                ?? $"Location cap reached ({reached.Current} of {reached.Cap}).",
                            LocationCap.CapReachedCode,
                            reached.Cap,
                            reached.Current
                        );
                        errors.Add(remainingRowTemplate);
                        break;
                    case AddOwnedLocationResult.FailClosed:
                        if (created.Count == 0)
                        {
                            return new ImportOwnedLocationsResult.FailClosed();
                        }

                        remainingRowTemplate = new ImportErrorRow(
                            index,
                            "Could not create location."
                        );
                        errors.Add(remainingRowTemplate);
                        break;
                    default:
                        errors.Add(
                            new ImportErrorRow(
                                index,
                                "Could not create location."
                            )
                        );
                        break;
                }
            }

            return new ImportOwnedLocationsResult.Completed(created, errors);
        }

        private async Task<AddOwnedLocationResult> AddLockedAsync(
            int restaurantId,
            int actorUserId,
            AddOwnedLocationRequest request,
            ImportRowPremises? importRow
        )
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            await LockBillingAccountRowAsync(restaurantId);

            var billingAccount = await _context.BillingAccounts
                .FirstOrDefaultAsync(row => row.RestaurantId == restaurantId);

            if (billingAccount == null)
            {
                await transaction.RollbackAsync();
                return new AddOwnedLocationResult.FailClosed();
            }

            if (
                !PendingPlanLimits.TryResolveLiveLocationCap(
                    _pricebookCatalog,
                    billingAccount,
                    out var entitled
                )
            )
            {
                await transaction.RollbackAsync();
                return new AddOwnedLocationResult.FailClosed();
            }

            var scheduledCap = PendingPlanLimits.ScheduledLocationCap(
                _pricebookCatalog,
                billingAccount
            );
            var scheduledLimits = scheduledCap != null && scheduledCap < entitled;
            if (scheduledLimits)
            {
                entitled = scheduledCap!.Value;
            }

            var ownedCount = await PendingPlanLimits.CountInsertLocationsAsync(
                _context,
                restaurantId
            );

            if (ownedCount >= entitled)
            {
                await transaction.RollbackAsync();
                return new AddOwnedLocationResult.CapReached(
                    entitled,
                    ownedCount,
                    scheduledLimits
                        ? PendingPlanLimits.LocationCapMessage(billingAccount, entitled)
                        : null
                );
            }

            var actorDisplayName = await _context.Users
                .AsNoTracking()
                .Where(row => row.Id == actorUserId)
                .Select(row => row.FullName)
                .FirstOrDefaultAsync();

            // Import and single add run the same server-owned check. A match
            // never refuses the row: it is saved as Draft and waits for an
            // Admin review. The caller only sees the generic warning.
            var check = await _premises.CheckAsync(
                new PremisesCheckInput(
                    restaurantId,
                    request.Address,
                    request.City,
                    request.Postcode,
                    request.FsaEstablishmentId
                )
            );
            var earlierRowIndex = FindEarlierImportRow(importRow);
            var needsReview = check.Flagged || earlierRowIndex != null;

            var location = new RestaurantLocation
            {
                RestaurantId = restaurantId,
                LocationName = request.LocationName.Trim(),
                Address = request.Address.Trim(),
                City = request.City.Trim(),
                Postcode = UkPostcode.FormatForDisplay(request.Postcode!),
                LocationPhone = PhoneNumberHelper.NormalizeOptional(
                    request.LocationPhone
                ),
                LocalContact = string.IsNullOrWhiteSpace(request.LocalContact)
                    ? null
                    : request.LocalContact.Trim(),
                LifecycleStatus = LocationLifecycleStatus.Draft,
                CreatedAt = DateTime.UtcNow,
            };
            _premises.Apply(location, check, alsoFlagged: earlierRowIndex != null);

            _context.RestaurantLocations.Add(location);
            // A Location under review mints no QR codes, so it holds no capacity.
            await _qrCodeProvisioning.MintDefaultQrCodesAsync(location);
            await _context.SaveChangesAsync();
            if (needsReview)
            {
                _premises.RecordFlag(location, check, earlierRowIndex);
            }

            _context.LocationActivities.Add(
                new LocationActivity
                {
                    RestaurantId = restaurantId,
                    LocationId = location.Id,
                    ActorUserId = actorUserId,
                    ActorDisplayName = string.IsNullOrWhiteSpace(actorDisplayName)
                        ? null
                        : actorDisplayName.Trim(),
                    Kind = LocationActivityKinds.LocationCreated,
                    Description = $"Created draft location “{location.LocationName}”.",
                    ToValue = "draft",
                    OccurredAt = DateTime.UtcNow,
                }
            );
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            if (!needsReview)
            {
                await _printReadyQrMaterialsWork.RequestEnsureAsync(location.Id);
            }

            return new AddOwnedLocationResult.Created(
                location.Id,
                needsReview
                    ? PossibleDuplicatePremises.Warning(earlierRowIndex)
                    : null
            );
        }

        /// <summary>
        /// Index of an earlier row of the same import file with the same
        /// premises key or FSA id, if any.
        /// </summary>
        private static int? FindEarlierImportRow(ImportRowPremises? importRow)
        {
            if (importRow == null)
            {
                return null;
            }

            if (
                importRow.Key != null
                && importRow.EarlierRowsByKey.TryGetValue(importRow.Key, out var byKey)
            )
            {
                return byKey;
            }

            if (
                importRow.FsaEstablishmentId != null
                && importRow.EarlierRowsByFsa.TryGetValue(
                    importRow.FsaEstablishmentId,
                    out var byFsa
                )
            )
            {
                return byFsa;
            }

            return null;
        }

        private static AddOwnedLocationResult.InvalidRequest? ValidateDraftFields(
            AddOwnedLocationRequest request
        )
        {
            if (string.IsNullOrWhiteSpace(request.LocationName))
            {
                return new AddOwnedLocationResult.InvalidRequest(
                    "Location name is required."
                );
            }

            if (string.IsNullOrWhiteSpace(request.Address))
            {
                return new AddOwnedLocationResult.InvalidRequest(
                    "Address is required."
                );
            }

            if (string.IsNullOrWhiteSpace(request.City))
            {
                return new AddOwnedLocationResult.InvalidRequest(
                    "City is required."
                );
            }

            if (string.IsNullOrWhiteSpace(request.Postcode))
            {
                return new AddOwnedLocationResult.InvalidRequest(
                    "Postcode is required."
                );
            }

            if (
                PremisesReviewService.NormalizeFsa(request.FsaEstablishmentId)
                    is { Length: > PremisesReviewService.FsaMaxLength }
            )
            {
                return new AddOwnedLocationResult.InvalidRequest(
                    "FSA establishment id is too long."
                );
            }

            return null;
        }

        private async Task LockBillingAccountRowAsync(int restaurantId)
        {
            if (!_context.Database.IsSqlServer())
            {
                return;
            }

            await _context.Database.ExecuteSqlRawAsync(
                """
                SELECT RestaurantId
                FROM BillingAccounts WITH (UPDLOCK, ROWLOCK)
                WHERE RestaurantId = {0}
                """,
                restaurantId
            );
        }
    }
}
