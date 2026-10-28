using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TummlyBackend.Billing.Pricebook;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Capture;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// QR lifecycle module — Create digital guest link, description patch,
    /// Pause/Resume/Rotate/Archive/Restore, and Pause/Activate location
    /// capture (ADR-0025).
    /// </summary>
    public class CaptureQrLifecycleService : ICaptureQrLifecycleService
    {
        private readonly ApplicationDbContext _context;
        private readonly ISmartGuestLinkService _smartGuestLink;
        private readonly IPrintReadyQrMaterialsService _printReadyQrMaterials;
        private readonly IActiveQrCapacity _activeQrCapacity;

        public CaptureQrLifecycleService(
            ApplicationDbContext context,
            ISmartGuestLinkService smartGuestLink,
            IPricebookCatalog pricebookCatalog,
            IPrintReadyQrMaterialsService printReadyQrMaterials,
            IActiveQrCapacity? activeQrCapacity = null
        )
        {
            _context = context;
            _smartGuestLink = smartGuestLink;
            _printReadyQrMaterials = printReadyQrMaterials;
            // DI supplies the shared instance; the fallback keeps the four-argument construction working.
            _activeQrCapacity =
                activeQrCapacity ?? new ActiveQrCapacity(context, pricebookCatalog);
        }

        public async Task<QrLifecycleResult> CreateDigitalGuestLinkAsync(
            CreateDigitalGuestLinkCommand command
        )
        {
            if (string.IsNullOrWhiteSpace(command.LinkName))
            {
                return QrLifecycleResult.Validation(
                    "Link name is required.",
                    field: "linkName"
                );
            }

            var linkName = DigitalGuestLinkNaming.FormatLinkName(
                command.LinkName
            );
            if (linkName.Length > DigitalGuestLinkNaming.LinkNameMaxLength)
            {
                return QrLifecycleResult.Validation(
                    $"Link name must be at most {DigitalGuestLinkNaming.LinkNameMaxLength} characters.",
                    field: "linkName"
                );
            }

            var internalDescription =
                DigitalGuestLinkNaming.FormatInternalDescription(
                    command.InternalDescription
                );
            if (
                internalDescription != null
                && internalDescription.Length
                    > DigitalGuestLinkNaming.InternalDescriptionMaxLength
            )
            {
                return QrLifecycleResult.Validation(
                    $"Internal description must be at most {DigitalGuestLinkNaming.InternalDescriptionMaxLength} characters.",
                    field: "internalDescription"
                );
            }

            if (
                string.IsNullOrWhiteSpace(command.Channel)
                || !Enum.TryParse<DigitalGuestLinkChannel>(
                    command.Channel.Trim(),
                    ignoreCase: false,
                    out var channel
                )
            )
            {
                return QrLifecycleResult.Validation(
                    "Channel is required.",
                    field: "channel"
                );
            }

            var requestedStatus = QrCodeStatus.Active;
            if (!string.IsNullOrWhiteSpace(command.Status))
            {
                if (
                    !Enum.TryParse<QrCodeStatus>(
                        command.Status.Trim(),
                        ignoreCase: false,
                        out requestedStatus
                    )
                    || (requestedStatus != QrCodeStatus.Active
                        && requestedStatus != QrCodeStatus.Paused)
                )
                {
                    return QrLifecycleResult.Validation(
                        "Status must be Active or Paused.",
                        field: "status"
                    );
                }
            }

            var location = await _context.RestaurantLocations
                .AsNoTracking()
                .FirstAsync(l => l.Id == command.LocationId);

            var billingDeny = await OperatorBillingLockGate.EvaluatePaidWriteDenyAsync(
                _context,
                location.RestaurantId
            );
            if (billingDeny != null)
            {
                return QrLifecycleResult.OperatorBillingLocked(billingDeny);
            }

            var status =
                location.CaptureLocationStatus == CaptureLocationStatus.Paused
                    ? QrCodeStatus.Paused
                    : requestedStatus;

            // An Active code is live: same plan gate as Resume and Activate.
            if (status == QrCodeStatus.Active)
            {
                var planDeny = await DenyIfFreePlanAsync(location.RestaurantId);
                if (planDeny != null)
                {
                    return planDeny;
                }
            }

            var normalizedLinkName =
                DigitalGuestLinkNaming.NormalizeLinkName(linkName);

            var nameTaken = await _context.QrCodes.AnyAsync(q =>
                q.RestaurantLocationId == command.LocationId
                && q.QrType == QrType.DigitalGuestLink
                && (q.Status == QrCodeStatus.Active
                    || q.Status == QrCodeStatus.Paused)
                && q.NormalizedLinkName == normalizedLinkName
            );

            if (nameTaken)
            {
                return DigitalLinkNameConflict();
            }

            IDbContextTransaction? transaction = null;
            if (status == QrCodeStatus.Active)
            {
                transaction = await BeginEntitlementTransactionAsync();
            }

            await using (transaction)
            {
                if (status == QrCodeStatus.Active)
                {
                    var denied = await DenyIfActiveQrWouldExceedAsync(
                        command.LocationId,
                        increment: 1
                    );
                    if (denied != null)
                    {
                        return denied;
                    }
                }

                var actorDisplayName = await ResolveActorDisplayNameAsync(
                    command.UserId
                );

                var token = await _smartGuestLink.GenerateTokenAsync();
                var qrCode = new QrCode
                {
                    RestaurantLocationId = command.LocationId,
                    QrType = QrType.DigitalGuestLink,
                    Token = token,
                    Status = status,
                    LinkName = linkName,
                    NormalizedLinkName = normalizedLinkName,
                    Channel = channel,
                    InternalDescription = internalDescription,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByUserId = command.UserId,
                    CreatedByDisplayName = actorDisplayName,
                };

                _context.QrCodes.Add(qrCode);

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    return DigitalLinkNameConflict();
                }

                if (transaction != null)
                {
                    await transaction.CommitAsync();
                }

                return QrLifecycleResult.Ok(new
                {
                    success = true,
                    qrCodeId = qrCode.Id,
                    qrType = qrCode.QrType.ToString(),
                    status = qrCode.Status.ToString(),
                    linkName = qrCode.LinkName,
                    channel = qrCode.Channel?.ToString(),
                    internalDescription = qrCode.InternalDescription,
                    createdAt = qrCode.CreatedAt,
                    createdByDisplayName = qrCode.CreatedByDisplayName,
                    updatedAt = qrCode.UpdatedAt,
                    updatedByDisplayName = qrCode.UpdatedByDisplayName,
                    qrLinkUrl = _smartGuestLink.BuildGuestUrl(qrCode.Token),
                });
            }
        }

        public async Task<QrLifecycleResult> UpdateInternalDescriptionAsync(
            UpdateInternalDescriptionCommand command
        )
        {
            var qrCode = await FindQrCodeAsync(
                command.QrCodeId,
                command.LocationId
            );

            if (qrCode == null)
            {
                return QrLifecycleResult.NotFound();
            }

            if (
                qrCode.Status != QrCodeStatus.Active
                && qrCode.Status != QrCodeStatus.Paused
            )
            {
                return QrLifecycleResult.InvalidTransition(
                    "Only Active or Paused QR codes can update their description."
                );
            }

            var internalDescription =
                DigitalGuestLinkNaming.FormatInternalDescription(
                    command.InternalDescription
                );
            if (
                internalDescription != null
                && internalDescription.Length
                    > DigitalGuestLinkNaming.InternalDescriptionMaxLength
            )
            {
                return QrLifecycleResult.Validation(
                    $"Internal description must be at most {DigitalGuestLinkNaming.InternalDescriptionMaxLength} characters.",
                    field: "internalDescription"
                );
            }

            var actorDisplayName = await ResolveActorDisplayNameAsync(
                command.UserId
            );

            qrCode.InternalDescription = internalDescription;
            qrCode.UpdatedAt = DateTime.UtcNow;
            qrCode.UpdatedByUserId = command.UserId;
            qrCode.UpdatedByDisplayName = actorDisplayName;

            await _context.SaveChangesAsync();

            return QrLifecycleResult.Ok(new
            {
                success = true,
                qrCodeId = qrCode.Id,
                internalDescription = qrCode.InternalDescription,
                updatedAt = qrCode.UpdatedAt,
                updatedByDisplayName = qrCode.UpdatedByDisplayName,
            });
        }

        public Task<QrLifecycleResult> PauseAsync(
            QrCodeLifecycleCommand command
        ) =>
            UpdateQrCodeStatusAsync(
                command,
                expectedStatus: QrCodeStatus.Active,
                nextStatus: QrCodeStatus.Paused,
                invalidTransitionMessage: "Only Active QR codes can be paused."
            );

        public Task<QrLifecycleResult> ResumeAsync(
            QrCodeLifecycleCommand command
        ) =>
            UpdateQrCodeStatusAsync(
                command,
                expectedStatus: QrCodeStatus.Paused,
                nextStatus: QrCodeStatus.Active,
                invalidTransitionMessage: "Only Paused QR codes can be resumed."
            );

        public async Task<QrLifecycleResult> RotateAsync(
            QrCodeLifecycleCommand command
        )
        {
            var qrCode = await FindQrCodeAsync(
                command.QrCodeId,
                command.LocationId
            );

            if (qrCode == null)
            {
                return QrLifecycleResult.NotFound();
            }

            var rotateLocation = await _context.RestaurantLocations
                .AsNoTracking()
                .FirstAsync(l => l.Id == command.LocationId);
            var rotateDeny = await OperatorBillingLockGate.EvaluatePaidWriteDenyAsync(
                _context,
                rotateLocation.RestaurantId
            );
            if (rotateDeny != null)
            {
                return QrLifecycleResult.OperatorBillingLocked(rotateDeny);
            }

            if (qrCode.QrType == QrType.DigitalGuestLink)
            {
                return QrLifecycleResult.InvalidTransition(
                    "Digital guest links cannot be rotated."
                );
            }

            if (
                qrCode.Status != QrCodeStatus.Active
                && qrCode.Status != QrCodeStatus.Paused
            )
            {
                return QrLifecycleResult.InvalidTransition(
                    "Only Active or Paused QR codes can be rotated."
                );
            }

            qrCode.Token = await _smartGuestLink.GenerateTokenAsync();
            var invalidated =
                await _printReadyQrMaterials.TryInvalidateAfterQrRotationAsync(
                    command.LocationId,
                    qrCode.QrType
                );
            if (!invalidated)
            {
                await _context.SaveChangesAsync();
            }

            return QrLifecycleResult.Ok(new
            {
                success = true,
                qrCodeId = qrCode.Id,
                status = qrCode.Status.ToString(),
                qrLinkUrl = _smartGuestLink.BuildGuestUrl(qrCode.Token),
            });
        }

        public async Task<QrLifecycleResult> ArchiveAsync(
            QrCodeLifecycleCommand command
        )
        {
            var qrCode = await FindQrCodeAsync(
                command.QrCodeId,
                command.LocationId
            );

            if (qrCode == null)
            {
                return QrLifecycleResult.NotFound();
            }

            if (
                qrCode.Status != QrCodeStatus.Active
                && qrCode.Status != QrCodeStatus.Paused
            )
            {
                return QrLifecycleResult.InvalidTransition(
                    "Only Active or Paused QR codes can be archived."
                );
            }

            var actorDisplayName = await ResolveActorDisplayNameAsync(
                command.UserId
            );

            qrCode.Status = QrCodeStatus.Archived;
            qrCode.ArchivedAt = DateTime.UtcNow;
            qrCode.ArchivedByUserId = command.UserId;
            qrCode.ArchivedByDisplayName = actorDisplayName;

            var location = await _context.RestaurantLocations
                .FirstAsync(l => l.Id == command.LocationId);
            location.CaptureLocationPauseRestoreQrCodeIdsJson =
                CaptureLocationPauseRestore.Remove(
                    location.CaptureLocationPauseRestoreQrCodeIdsJson,
                    command.QrCodeId
                );

            await _context.SaveChangesAsync();

            return QrLifecycleResult.Ok(new
            {
                success = true,
                qrCodeId = qrCode.Id,
                status = qrCode.Status.ToString(),
                archivedAt = qrCode.ArchivedAt,
                archivedByDisplayName = qrCode.ArchivedByDisplayName,
            });
        }

        public async Task<QrLifecycleResult> RestoreAsync(
            QrCodeLifecycleCommand command
        )
        {
            var qrCode = await FindQrCodeAsync(
                command.QrCodeId,
                command.LocationId
            );

            if (qrCode == null)
            {
                return QrLifecycleResult.NotFound();
            }

            if (qrCode.Status != QrCodeStatus.Archived)
            {
                return QrLifecycleResult.InvalidTransition(
                    "Only Archived QR codes can be restored."
                );
            }

            var restoreDeny = await DenyIfPaidWriteLockedAsync(command.LocationId);
            if (restoreDeny != null)
            {
                return restoreDeny;
            }

            if (qrCode.QrType == QrType.DigitalGuestLink)
            {
                if (qrCode.NormalizedLinkName != null)
                {
                    var nameTaken = await _context.QrCodes.AnyAsync(q =>
                        q.RestaurantLocationId == command.LocationId
                        && q.QrType == QrType.DigitalGuestLink
                        && (q.Status == QrCodeStatus.Active
                            || q.Status == QrCodeStatus.Paused)
                        && q.NormalizedLinkName == qrCode.NormalizedLinkName
                    );

                    if (nameTaken)
                    {
                        return QrLifecycleResult.Conflict(
                            "A digital guest link with this name already exists at this location.",
                            reason: "link_name_occupied"
                        );
                    }
                }
            }
            else
            {
                var slotTaken = await _context.QrCodes.AnyAsync(q =>
                    q.RestaurantLocationId == command.LocationId
                    && q.QrType == qrCode.QrType
                    && (q.Status == QrCodeStatus.Active
                        || q.Status == QrCodeStatus.Paused)
                );

                if (slotTaken)
                {
                    return QrLifecycleResult.Conflict(
                        "A QR code of this type already exists at this location.",
                        reason: "type_slot_occupied"
                    );
                }
            }

            qrCode.Status = QrCodeStatus.Paused;
            qrCode.ArchivedAt = null;
            qrCode.ArchivedByUserId = null;
            qrCode.ArchivedByDisplayName = null;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return QrLifecycleResult.Conflict(
                    qrCode.QrType == QrType.DigitalGuestLink
                        ? "A digital guest link with this name already exists at this location."
                        : "A QR code of this type already exists at this location.",
                    reason: qrCode.QrType == QrType.DigitalGuestLink
                        ? "link_name_occupied"
                        : "type_slot_occupied"
                );
            }

            return QrLifecycleResult.Ok(new
            {
                success = true,
                qrCodeId = qrCode.Id,
                status = qrCode.Status.ToString(),
                qrLinkUrl = _smartGuestLink.BuildGuestUrl(qrCode.Token),
            });
        }

        public async Task<QrLifecycleResult> PauseLocationCaptureAsync(
            LocationCaptureLifecycleCommand command
        )
        {
            var location = await _context.RestaurantLocations
                .FirstAsync(l => l.Id == command.LocationId);

            if (location.CaptureLocationStatus == CaptureLocationStatus.Paused)
            {
                return QrLifecycleResult.InvalidTransition(
                    "Location capture is already paused."
                );
            }

            var activeCodes = await _context.QrCodes
                .Where(q =>
                    q.RestaurantLocationId == command.LocationId
                    && q.Status == QrCodeStatus.Active
                )
                .ToListAsync();

            var restoreIds = activeCodes.Select(q => q.Id).ToList();
            foreach (var qr in activeCodes)
            {
                qr.Status = QrCodeStatus.Paused;
            }

            location.CaptureLocationStatus = CaptureLocationStatus.Paused;
            location.CaptureLocationPauseRestoreQrCodeIdsJson =
                CaptureLocationPauseRestore.Serialize(restoreIds);

            await _context.SaveChangesAsync();

            return QrLifecycleResult.Ok(new
            {
                success = true,
                locationId = location.Id,
                status = location.CaptureLocationStatus.ToString(),
                pausedCount = restoreIds.Count,
                pauseRestoreQrCodeCount = restoreIds.Count,
            });
        }

        public async Task<QrLifecycleResult> ActivateLocationCaptureAsync(
            LocationCaptureLifecycleCommand command
        )
        {
            var location = await _context.RestaurantLocations
                .FirstAsync(l => l.Id == command.LocationId);

            var planDeny = await DenyIfFreePlanAsync(location.RestaurantId);
            if (planDeny != null)
            {
                return planDeny;
            }

            if (location.CaptureLocationStatus != CaptureLocationStatus.Paused)
            {
                return QrLifecycleResult.InvalidTransition(
                    "Location capture is not paused."
                );
            }

            var activateDeny = await DenyIfPaidWriteLockedAsync(command.LocationId);
            if (activateDeny != null)
            {
                return activateDeny;
            }

            var restoreIds = CaptureLocationPauseRestore.Parse(
                location.CaptureLocationPauseRestoreQrCodeIdsJson
            );

            var codesToActivate = await _context.QrCodes
                .Where(q =>
                    q.RestaurantLocationId == command.LocationId
                    && restoreIds.Contains(q.Id)
                    && q.Status == QrCodeStatus.Paused
                )
                .ToListAsync();

            await using var transaction =
                await BeginEntitlementTransactionAsync();
            var denied = await DenyIfActiveQrWouldExceedAsync(
                command.LocationId,
                increment: codesToActivate.Count
            );
            if (denied != null)
            {
                return denied;
            }

            foreach (var qr in codesToActivate)
            {
                qr.Status = QrCodeStatus.Active;
            }

            location.CaptureLocationStatus = CaptureLocationStatus.Active;
            location.CaptureLocationPauseRestoreQrCodeIdsJson = null;

            await _context.SaveChangesAsync();
            if (transaction != null)
            {
                await transaction.CommitAsync();
            }

            return QrLifecycleResult.Ok(new
            {
                success = true,
                locationId = location.Id,
                status = location.CaptureLocationStatus.ToString(),
                activatedCount = codesToActivate.Count,
                pauseRestoreQrCodeCount = 0,
            });
        }

        private async Task<QrLifecycleResult> UpdateQrCodeStatusAsync(
            QrCodeLifecycleCommand command,
            QrCodeStatus expectedStatus,
            QrCodeStatus nextStatus,
            string invalidTransitionMessage
        )
        {
            var qrCode = await FindQrCodeAsync(
                command.QrCodeId,
                command.LocationId
            );

            if (qrCode == null)
            {
                return QrLifecycleResult.NotFound();
            }

            var location = await _context.RestaurantLocations
                .AsNoTracking()
                .FirstAsync(l => l.Id == command.LocationId);

            if (location.CaptureLocationStatus == CaptureLocationStatus.Paused)
            {
                return QrLifecycleResult.LocationLocked();
            }

            if (qrCode.Status != expectedStatus)
            {
                return QrLifecycleResult.InvalidTransition(
                    invalidTransitionMessage
                );
            }

            if (nextStatus == QrCodeStatus.Active)
            {
                var planDeny = await DenyIfFreePlanAsync(location.RestaurantId);
                if (planDeny != null)
                {
                    return planDeny;
                }
            }

            IDbContextTransaction? transaction = null;
            if (nextStatus == QrCodeStatus.Active)
            {
                transaction = await BeginEntitlementTransactionAsync();
            }

            await using (transaction)
            {
                if (nextStatus == QrCodeStatus.Active)
                {
                    var denied = await DenyIfActiveQrWouldExceedAsync(
                        command.LocationId,
                        increment: 1,
                        activatingType: qrCode.QrType
                    );
                    if (denied != null)
                    {
                        return denied;
                    }
                }

                qrCode.Status = nextStatus;
                await _context.SaveChangesAsync();
                if (transaction != null)
                {
                    await transaction.CommitAsync();
                }

                return QrLifecycleResult.Ok(new
                {
                    success = true,
                    qrCodeId = qrCode.Id,
                    status = qrCode.Status.ToString(),
                });
            }
        }

        private async Task<QrLifecycleResult?> DenyIfFreePlanAsync(int restaurantId)
        {
            var account = await _context.BillingAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(row => row.RestaurantId == restaurantId);
            if (account == null)
            {
                return null;
            }

            var deny = OperatorPlanGate.EvaluatePlanDeny(account);
            return deny == null ? null : QrLifecycleResult.OperatorBillingLocked(deny);
        }

        private async Task<QrLifecycleResult?> DenyIfPaidWriteLockedAsync(
            int locationId
        )
        {
            var restaurantId = await _context.RestaurantLocations
                .AsNoTracking()
                .Where(row => row.Id == locationId)
                .Select(row => row.RestaurantId)
                .FirstOrDefaultAsync();
            var deny = await OperatorBillingLockGate.EvaluatePaidWriteDenyAsync(
                _context,
                restaurantId
            );
            return deny == null ? null : QrLifecycleResult.OperatorBillingLocked(deny);
        }

        public async Task<int> ActivatePlanPausedCodesAsync(
            int restaurantId,
            CancellationToken cancellationToken = default
        )
        {
            await using var transaction = await BeginEntitlementTransactionAsync();

            var billingAccount = await _activeQrCapacity.LockBillingAccountAsync(
                restaurantId,
                cancellationToken
            );
            // The pool is Workspace-wide (Commercial V4), so no Location is passed.
            var allowance = await _activeQrCapacity.GetAllowanceAsync(
                billingAccount,
                cancellationToken
            );
            if (allowance == null)
            {
                return 0;
            }

            var workspacePool = allowance.WorkspacePool;

            var locations = await _context.RestaurantLocations
                .Where(row =>
                    row.RestaurantId == restaurantId
                    && row.CaptureLocationStatus == CaptureLocationStatus.Active
                    && row.PremisesReviewStatus != PremisesReviewStatus.PendingReview
                    && row.PremisesReviewStatus != PremisesReviewStatus.Rejected
                )
                .ToListAsync(cancellationToken);
            if (locations.Count == 0)
            {
                return 0;
            }

            var locationIds = locations.Select(row => row.Id).ToList();
            var pausedCodes = await _context.QrCodes
                .Where(qr =>
                    locationIds.Contains(qr.RestaurantLocationId)
                    && qr.Status == QrCodeStatus.Paused
                )
                .OrderBy(qr => qr.CreatedAt)
                .ThenBy(qr => qr.Id)
                .ToListAsync(cancellationToken);

            // Count the Workspace once under the lock, then add each activation's
            // cost. Held reservations at any Location are already in this count.
            var used = await _activeQrCapacity.CountUsedForRestaurantAsync(
                restaurantId,
                cancellationToken
            );
            var activated = 0;
            foreach (var qr in pausedCodes)
            {
                var location = locations.First(row =>
                    row.Id == qr.RestaurantLocationId
                );
                var operatorPaused = CaptureLocationPauseRestore
                    .Parse(location.CaptureLocationPauseRestoreQrCodeIdsJson)
                    .Contains(qr.Id);
                if (operatorPaused)
                {
                    continue;
                }

                // Headroom is Workspace-wide, so held reservations at any Location count.
                var cost = await _activeQrCapacity.ActivationCostAsync(
                    qr.RestaurantLocationId,
                    qr.QrType,
                    cancellationToken
                );
                if (used + cost > workspacePool)
                {
                    continue;
                }

                qr.Status = QrCodeStatus.Active;
                used += cost;
                activated++;
                // Save each activation so the next cost check sees it.
                await _context.SaveChangesAsync(cancellationToken);
            }

            if (transaction != null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return activated;
        }

        private Task<QrCode?> FindQrCodeAsync(int qrCodeId, int locationId) =>
            _context.QrCodes.FirstOrDefaultAsync(q =>
                q.Id == qrCodeId && q.RestaurantLocationId == locationId
            );

        private async Task<IDbContextTransaction?> BeginEntitlementTransactionAsync()
        {
            // Join an ambient transaction (Settings Pause/Resume wraps Capture).
            if (_context.Database.CurrentTransaction != null)
            {
                return null;
            }

            if (!_context.Database.IsSqlServer())
            {
                return null;
            }

            return await _context.Database.BeginTransactionAsync();
        }

        private async Task<QrLifecycleResult?> DenyIfActiveQrWouldExceedAsync(
            int locationId,
            int increment,
            QrType? activatingType = null
        )
        {
            if (increment <= 0)
            {
                return null;
            }

            var restaurantId = await _context.RestaurantLocations
                .Where(location => location.Id == locationId)
                .Select(location => location.RestaurantId)
                .FirstAsync();

            if (await PremisesReviewGate.LocationIsBlockedAsync(_context, locationId))
            {
                return QrLifecycleResult.Conflict(PremisesReviewGate.BlockedMessage);
            }

            var decision = await _activeQrCapacity.EvaluateAsync(
                restaurantId,
                locationId,
                increment,
                activatingType,
                CancellationToken.None
            );
            if (!decision.EntitlementsAvailable)
            {
                return QrLifecycleResult.Conflict(
                    "Plan entitlements are unavailable."
                );
            }

            if (decision.Fits)
            {
                return null;
            }

            return QrLifecycleResult.ActiveQrCapReached(
                decision.WorkspacePool,
                decision.Used
            );
        }

        private async Task<string?> ResolveActorDisplayNameAsync(int userId)
        {
            var actor = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            return actor?.FullName;
        }

        private static QrLifecycleResult DigitalLinkNameConflict() =>
            QrLifecycleResult.Conflict(
                "A digital guest link with this name already exists at this location.",
                field: "linkName"
            );
    }
}
