using Microsoft.EntityFrameworkCore;
using TummlyBackend.Billing.PlanEntitlements;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Locations;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public sealed class LocationsLifecycleWriteService
        : ILocationsLifecycleWriteService
    {
        private readonly ApplicationDbContext _context;
        private readonly IQrCodeProvisioningService _qrCodeProvisioning;
        private readonly IComplimentaryStarterShopOrderService _complimentaryStarterShopOrders;
        private readonly IPrintReadyQrMaterialsWork _printReadyQrMaterialsWork;
        private readonly IRestaurantAccountTypePromotion _accountTypePromotion;
        private readonly IPricebookCatalog _pricebook;
        private readonly IActiveQrCapacity _activeQrCapacity;
        private readonly IPremisesReviewService _premises;

        public LocationsLifecycleWriteService(
            ApplicationDbContext context,
            IQrCodeProvisioningService qrCodeProvisioning,
            IComplimentaryStarterShopOrderService complimentaryStarterShopOrders,
            IPrintReadyQrMaterialsWork printReadyQrMaterialsWork,
            IRestaurantAccountTypePromotion accountTypePromotion,
            IPricebookCatalog pricebook,
            IActiveQrCapacity activeQrCapacity,
            IPremisesReviewService? premises = null
        )
        {
            _premises = premises ?? new PremisesReviewService(context);
            _context = context;
            _qrCodeProvisioning = qrCodeProvisioning;
            _complimentaryStarterShopOrders = complimentaryStarterShopOrders;
            _printReadyQrMaterialsWork = printReadyQrMaterialsWork;
            _accountTypePromotion = accountTypePromotion;
            _pricebook = pricebook;
            _activeQrCapacity = activeQrCapacity;
        }

        public async Task<LocationLifecycleWriteResult> ActivateDraftAsync(
            int restaurantId,
            int locationId,
            int actorUserId
        )
        {
            var location = await LoadOwnedAsync(restaurantId, locationId);
            if (location == null)
            {
                return new LocationLifecycleWriteResult.NotFound();
            }

            if (location.LifecycleStatus != LocationLifecycleStatus.Draft)
            {
                return new LocationLifecycleWriteResult.InvalidRequest(
                    "Only draft locations can be activated."
                );
            }

            var missing = ValidateActivateFields(location);
            if (missing != null)
            {
                return missing;
            }

            // Duplicate-premises review: wait for an Admin to clear it.
            if (PremisesReviewGate.Blocks(location))
            {
                return new LocationLifecycleWriteResult.Conflict(
                    PremisesReviewGate.BlockedMessage
                );
            }

            var capDeny = await PendingPlanLimits.DenyActivationAsync(
                _context,
                _pricebook,
                restaurantId
            );
            if (capDeny != null)
            {
                return new LocationLifecycleWriteResult.Conflict(capDeny);
            }

            var from = ToWire(location.LifecycleStatus);
            location.LifecycleStatus = LocationLifecycleStatus.Active;
            await EmitLifecycleAsync(
                restaurantId,
                locationId,
                actorUserId,
                from,
                ToWire(LocationLifecycleStatus.Active),
                $"Activated location “{location.LocationName}”."
            );
            await _context.SaveChangesAsync();

            var accountType =
                await _accountTypePromotion.EnsureRoutingAccountTypeAsync(
                    restaurantId
                );

            // Mint any Capture placements added to the starter kit since Draft
            // insert (missing types only). The mint counts the Workspace pool,
            // so it runs under the billing-row lock with the save.
            await using (
                var mintTransaction =
                    await QrCapacityTransaction.BeginIfNeededAsync(
                        _context,
                        CancellationToken.None
                    )
            )
            {
                await _activeQrCapacity.LockBillingAccountAsync(
                    restaurantId,
                    CancellationToken.None
                );
                await _qrCodeProvisioning.MintDefaultQrCodesAsync(location);
                await _context.SaveChangesAsync();
                if (mintTransaction != null)
                {
                    await mintTransaction.CommitAsync();
                }
            }

            var actorDisplayName = await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == actorUserId)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync();

            var complimentary =
                await _complimentaryStarterShopOrders.EnsureForLocationAsync(
                    restaurantId,
                    locationId,
                    actorUserId,
                    actorDisplayName ?? string.Empty
                );

            if (complimentary.ShopOrderId != Guid.Empty)
            {
                await _printReadyQrMaterialsWork.RequestShopOrderEnsureAsync(
                    complimentary.ShopOrderId
                );
            }

            // Starter print-ready ensure also re-mints missing Capture types.
            await _printReadyQrMaterialsWork.RequestEnsureAsync(locationId);

            return new LocationLifecycleWriteResult.Ok(accountType);
        }

        public async Task<LocationLifecycleWriteResult> DeleteDraftAsync(
            int restaurantId,
            int locationId,
            int actorUserId
        )
        {
            var location = await LoadOwnedAsync(restaurantId, locationId);
            if (location == null)
            {
                return new LocationLifecycleWriteResult.NotFound();
            }

            if (location.LifecycleStatus != LocationLifecycleStatus.Draft)
            {
                return new LocationLifecycleWriteResult.InvalidRequest(
                    "Only draft locations can be deleted."
                );
            }

            if (await HasHistoryAsync(locationId))
            {
                return new LocationLifecycleWriteResult.Conflict(
                    "This draft cannot be deleted because it has guest or history records."
                );
            }

            var name = location.LocationName;
            var linkedActivities = await _context.LocationActivities
                .Where(row => row.LocationId == locationId)
                .ToListAsync();
            foreach (var row in linkedActivities)
            {
                row.LocationId = null;
            }

            _context.RestaurantLocations.Remove(location);
            await EmitLifecycleAsync(
                restaurantId,
                locationId: null,
                actorUserId,
                ToWire(LocationLifecycleStatus.Draft),
                "deleted",
                $"Deleted draft location “{name}”."
            );
            await _context.SaveChangesAsync();
            return new LocationLifecycleWriteResult.Ok(
                await CurrentAccountTypeAsync(restaurantId)
            );
        }

        public async Task<LocationLifecycleWriteResult> EditDetailsAsync(
            int restaurantId,
            int locationId,
            int actorUserId,
            AddOwnedLocationRequest request
        )
        {
            var invalid = ValidateEditFields(request);
            if (invalid != null)
            {
                return invalid;
            }

            var location = await LoadOwnedAsync(restaurantId, locationId);
            if (location == null)
            {
                return new LocationLifecycleWriteResult.NotFound();
            }

            if (location.LifecycleStatus == LocationLifecycleStatus.Archived)
            {
                return new LocationLifecycleWriteResult.Conflict(
                    "Archived locations cannot be edited."
                );
            }

            var previousKey = location.PremisesKey
                ?? PremisesKey.ForStorage(
                    PremisesKey.Build(location.Address, location.City, location.Postcode)
                );
            var previousFsa = location.FsaEstablishmentId;

            location.LocationName = request.LocationName.Trim();
            location.Address = request.Address.Trim();
            location.City = request.City.Trim();
            location.Postcode = UkPostcode.FormatForDisplay(request.Postcode!);
            location.LocationPhone = PhoneNumberHelper.NormalizeOptional(
                request.LocationPhone
            );
            location.LocalContact = string.IsNullOrWhiteSpace(request.LocalContact)
                ? null
                : request.LocalContact.Trim();

            // Null means "not sent": keep the stored id. An empty string clears it.
            var fsa = request.FsaEstablishmentId == null
                ? previousFsa
                : PremisesReviewService.NormalizeFsa(request.FsaEstablishmentId);
            var flaggedCheck = await RecheckPremisesAsync(
                location,
                previousKey,
                previousFsa,
                fsa
            );

            await EmitEditedAsync(
                restaurantId,
                locationId,
                actorUserId,
                location.LocationName
            );
            await _context.SaveChangesAsync();
            if (flaggedCheck != null)
            {
                _premises.RecordFlag(location, flaggedCheck);
                await _context.SaveChangesAsync();
            }
            return new LocationLifecycleWriteResult.Ok(
                await CurrentAccountTypeAsync(restaurantId)
            );
        }

        /// <summary>
        /// Keeps the persisted premises key in step with an edit. A Draft that
        /// moved to other premises is checked again: an earlier clearance no
        /// longer applies, and a new match sends it to review and pauses its
        /// QR codes so it holds no capacity. A Location already in review stays
        /// in review; only an Admin resolves it. Returns the check when it
        /// flagged, so the caller can write the audit row after the save.
        /// </summary>
        private async Task<PremisesCheckResult?> RecheckPremisesAsync(
            RestaurantLocation location,
            string? previousKey,
            string? previousFsa,
            string? fsa
        )
        {
            var check = await _premises.CheckAsync(
                new PremisesCheckInput(
                    location.RestaurantId,
                    location.Address,
                    location.City,
                    location.Postcode,
                    fsa,
                    ExcludeLocationId: location.Id
                )
            );
            var changed = check.PremisesKey != previousKey || fsa != previousFsa;
            location.PremisesKey = check.PremisesKey;
            location.FsaEstablishmentId = fsa;

            if (
                !changed
                || location.LifecycleStatus != LocationLifecycleStatus.Draft
                || PremisesReviewGate.Blocks(location)
            )
            {
                return null;
            }

            if (location.PremisesReviewStatus == PremisesReviewStatus.Cleared)
            {
                location.PremisesReviewStatus = PremisesReviewStatus.None;
                location.PremisesReviewedAtUtc = null;
                location.PremisesReviewedByAdminUserId = null;
                location.PremisesReviewReason = null;
            }

            if (!_premises.Apply(location, check))
            {
                return null;
            }

            var activeCodes = await _context.QrCodes
                .Where(qr =>
                    qr.RestaurantLocationId == location.Id
                    && qr.Status == QrCodeStatus.Active
                )
                .ToListAsync();
            foreach (var qr in activeCodes)
            {
                qr.Status = QrCodeStatus.Paused;
            }

            return check;
        }

        private async Task<string> CurrentAccountTypeAsync(int restaurantId)
        {
            var accountType = await _context.Restaurants
                .AsNoTracking()
                .Where(row => row.Id == restaurantId)
                .Select(row => row.AccountType)
                .FirstOrDefaultAsync();
            return string.IsNullOrWhiteSpace(accountType)
                ? "Single"
                : accountType.Trim();
        }

        private async Task<RestaurantLocation?> LoadOwnedAsync(
            int restaurantId,
            int locationId
        )
        {
            return await _context.RestaurantLocations
                .FirstOrDefaultAsync(row =>
                    row.Id == locationId && row.RestaurantId == restaurantId
                );
        }

        private static LocationLifecycleWriteResult.InvalidRequest? ValidateActivateFields(
            RestaurantLocation location
        )
        {
            if (string.IsNullOrWhiteSpace(location.LocationName))
            {
                return new LocationLifecycleWriteResult.InvalidRequest(
                    "Location name is required to activate."
                );
            }

            if (string.IsNullOrWhiteSpace(location.Address))
            {
                return new LocationLifecycleWriteResult.InvalidRequest(
                    "Address is required to activate."
                );
            }

            if (string.IsNullOrWhiteSpace(location.City))
            {
                return new LocationLifecycleWriteResult.InvalidRequest(
                    "City is required to activate."
                );
            }

            if (string.IsNullOrWhiteSpace(location.Postcode))
            {
                return new LocationLifecycleWriteResult.InvalidRequest(
                    "Postcode is required to activate."
                );
            }

            return null;
        }

        private static LocationLifecycleWriteResult.InvalidRequest? ValidateEditFields(
            AddOwnedLocationRequest request
        )
        {
            if (string.IsNullOrWhiteSpace(request.LocationName))
            {
                return new LocationLifecycleWriteResult.InvalidRequest(
                    "Location name is required."
                );
            }

            if (string.IsNullOrWhiteSpace(request.Address))
            {
                return new LocationLifecycleWriteResult.InvalidRequest(
                    "Address is required."
                );
            }

            if (string.IsNullOrWhiteSpace(request.City))
            {
                return new LocationLifecycleWriteResult.InvalidRequest(
                    "City is required."
                );
            }

            if (string.IsNullOrWhiteSpace(request.Postcode))
            {
                return new LocationLifecycleWriteResult.InvalidRequest(
                    "Postcode is required."
                );
            }

            if (
                PremisesReviewService.NormalizeFsa(request.FsaEstablishmentId)
                    is { Length: > PremisesReviewService.FsaMaxLength }
            )
            {
                return new LocationLifecycleWriteResult.InvalidRequest(
                    "FSA establishment id is too long."
                );
            }

            return null;
        }

        private async Task<bool> HasHistoryAsync(int locationId)
        {
            if (
                await _context.LocationGuests.AnyAsync(row =>
                    row.RestaurantLocationId == locationId
                )
            )
            {
                return true;
            }

            if (
                await _context.Feedbacks.AnyAsync(row =>
                    row.RestaurantLocationId == locationId
                )
            )
            {
                return true;
            }

            if (
                await _context.Campaigns.AnyAsync(row =>
                    row.RestaurantLocationId == locationId
                )
            )
            {
                return true;
            }

            // CatalogOffer → location is Restrict; refuse instead of FK 500.
            if (
                await _context.CatalogOffers.AnyAsync(row =>
                    row.RestaurantLocationId == locationId
                )
            )
            {
                return true;
            }

            return false;
        }

        private static string ToWire(LocationLifecycleStatus status) =>
            status switch
            {
                LocationLifecycleStatus.Draft => "draft",
                LocationLifecycleStatus.Active => "active",
                LocationLifecycleStatus.Paused => "paused",
                LocationLifecycleStatus.Archived => "archived",
                _ => status.ToString().ToLowerInvariant(),
            };

        private async Task EmitLifecycleAsync(
            int restaurantId,
            int? locationId,
            int actorUserId,
            string? from,
            string to,
            string description
        )
        {
            var actorDisplayName = await ActorDisplayNameAsync(actorUserId);
            _context.LocationActivities.Add(
                new LocationActivity
                {
                    RestaurantId = restaurantId,
                    LocationId = locationId,
                    ActorUserId = actorUserId,
                    ActorDisplayName = actorDisplayName,
                    Kind = LocationActivityKinds.LifecycleChanged,
                    Description = description,
                    FromValue = from,
                    ToValue = to,
                    OccurredAt = DateTime.UtcNow,
                }
            );
        }

        private async Task EmitEditedAsync(
            int restaurantId,
            int locationId,
            int actorUserId,
            string locationName
        )
        {
            var actorDisplayName = await ActorDisplayNameAsync(actorUserId);
            _context.LocationActivities.Add(
                new LocationActivity
                {
                    RestaurantId = restaurantId,
                    LocationId = locationId,
                    ActorUserId = actorUserId,
                    ActorDisplayName = actorDisplayName,
                    Kind = LocationActivityKinds.LocationEdited,
                    Description = $"Edited details for “{locationName}”.",
                    OccurredAt = DateTime.UtcNow,
                }
            );
        }

        private async Task<string?> ActorDisplayNameAsync(int actorUserId)
        {
            var name = await _context.Users
                .AsNoTracking()
                .Where(row => row.Id == actorUserId)
                .Select(row => row.FullName)
                .FirstOrDefaultAsync();
            return string.IsNullOrWhiteSpace(name) ? null : name.Trim();
        }
    }
}
