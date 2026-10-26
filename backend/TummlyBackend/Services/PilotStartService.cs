using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public class PilotStartService : IPilotStartService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICreditLedger _creditLedger;
        private readonly IComplimentaryStarterShopOrderService _complimentaryStarterShopOrders;
        private readonly IPrintReadyQrMaterialsWork _printReadyQrMaterialsWork;
        private readonly ICaptureQrLifecycleService _captureQrLifecycle;
        private readonly IEmailService _emailService;
        private readonly ILogger<PilotStartService> _logger;

        public PilotStartService(
            ApplicationDbContext context,
            ICreditLedger creditLedger,
            IComplimentaryStarterShopOrderService complimentaryStarterShopOrders,
            IPrintReadyQrMaterialsWork printReadyQrMaterialsWork,
            ICaptureQrLifecycleService captureQrLifecycle,
            IEmailService emailService,
            ILogger<PilotStartService> logger
        )
        {
            _logger = logger;
            _context = context;
            _creditLedger = creditLedger;
            _complimentaryStarterShopOrders = complimentaryStarterShopOrders;
            _printReadyQrMaterialsWork = printReadyQrMaterialsWork;
            _captureQrLifecycle = captureQrLifecycle;
            _emailService = emailService;
        }

        public async Task<PilotStartResult> StartPilotAsync(
            int restaurantId,
            CancellationToken cancellationToken = default
        )
        {
            var ownTransaction =
                _context.Database.CurrentTransaction == null
                    ? await _context.Database.BeginTransactionAsync(
                        cancellationToken
                    )
                    : null;

            await using (ownTransaction)
            {
                var result = await StartLockedAsync(restaurantId, cancellationToken);

                if (ownTransaction != null)
                {
                    await ownTransaction.CommitAsync(cancellationToken);
                }

                return result;
            }
        }

        public async Task NotifyStartedAsync(
            int restaurantId,
            PilotStartResult result,
            CancellationToken cancellationToken = default
        )
        {
            if (result.RefusedCode != null)
            {
                return;
            }

            // The account already exists. A lookup or shop-order failure must not
            // fail provisioning. Log the exception type only.
            // Idempotent: returns existing shop order ids on a repeat call, so
            // a retry after a lost notify still queues the materials.
            string? ownerEmail = null;
            string? ownerFullName = null;
            string? restaurantName = null;
            try
            {
                var owner = await _context.Restaurants
                    .AsNoTracking()
                    .Where(row => row.Id == restaurantId)
                    .Select(row => new
                    {
                        row.Name,
                        row.OwnerUserId,
                        OwnerFullName = row.OwnerUser.FullName,
                        OwnerEmail = row.OwnerUser.Email,
                    })
                    .FirstOrDefaultAsync(cancellationToken);
                if (owner == null)
                {
                    return;
                }

                ownerEmail = owner.OwnerEmail;
                ownerFullName = owner.OwnerFullName;
                restaurantName = owner.Name;

                var shopOrderIds =
                    await _complimentaryStarterShopOrders.EnsureAllActiveLocationsAsync(
                        restaurantId,
                        owner.OwnerUserId,
                        owner.OwnerFullName ?? string.Empty,
                        cancellationToken
                    );
                foreach (var shopOrderId in shopOrderIds)
                {
                    await _printReadyQrMaterialsWork.RequestShopOrderEnsureAsync(
                        shopOrderId,
                        cancellationToken
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    "Pilot start shop-order queue failed for restaurant {RestaurantId}: {ExceptionType}",
                    restaurantId,
                    ex.GetType().Name
                );
            }

            if (
                !result.Started
                || result.EndsAtUtc == null
                || ownerEmail == null
                || restaurantName == null
            )
            {
                return;
            }

            await TrySendPilotStartedEmailAsync(
                ownerEmail,
                ownerFullName,
                restaurantName,
                result.EndsAtUtc.Value
            );
        }

        private async Task<PilotStartResult> StartLockedAsync(
            int restaurantId,
            CancellationToken cancellationToken
        )
        {
            var account = await LoadBillingAccountForUpdateAsync(
                restaurantId,
                cancellationToken
            );
            if (account == null)
            {
                throw new InvalidOperationException("billing_account_missing");
            }

            var nowUtc = DateTime.UtcNow;

            if (account.PilotStartedAtUtc != null)
            {
                var alreadyPilot = string.Equals(
                    account.SubscriptionPlan,
                    BillingSubscriptionPlans.Pilot,
                    StringComparison.Ordinal
                );
                return new PilotStartResult(
                    Started: false,
                    RefusedCode: alreadyPilot ? null : "pilot_already_used",
                    StartedAtUtc: account.PilotStartedAtUtc,
                    EndsAtUtc: account.PilotPeriodEnd
                );
            }

            var isFree = string.Equals(
                account.SubscriptionPlan,
                BillingSubscriptionPlans.Free,
                StringComparison.Ordinal
            );
            var isPilot = string.Equals(
                account.SubscriptionPlan,
                BillingSubscriptionPlans.Pilot,
                StringComparison.Ordinal
            );

            // Paid plans (Starter, Growth, Group, Custom) never start a Pilot.
            if (!isFree && !isPilot)
            {
                return new PilotStartResult(
                    Started: false,
                    RefusedCode: "pilot_already_used",
                    StartedAtUtc: null,
                    EndsAtUtc: null
                );
            }

            // Legacy Pilot with no start stamp: record the start, keep the end.
            // No clock sync, no mint, no email.
            if (isPilot)
            {
                account.PilotStartedAtUtc =
                    account.PilotPeriodEnd?.AddDays(
                        -ActivationCodeHelper.ActivationPeriodDays
                    ) ?? nowUtc;
                await _context.SaveChangesAsync(cancellationToken);
                return new PilotStartResult(
                    Started: false,
                    RefusedCode: null,
                    StartedAtUtc: account.PilotStartedAtUtc,
                    EndsAtUtc: account.PilotPeriodEnd
                );
            }

            // Legacy Free account that already received a Pilot mint.
            var hasPriorPilotMint = await _context.CreditLedgerEntries.AnyAsync(
                row =>
                    row.RestaurantId == restaurantId
                    && row.EntryType == CreditLedgerEntryTypes.PilotAllocation,
                cancellationToken
            );
            if (hasPriorPilotMint)
            {
                return new PilotStartResult(
                    Started: false,
                    RefusedCode: "pilot_already_used",
                    StartedAtUtc: null,
                    EndsAtUtc: null
                );
            }

            BillingCreditsService.ApplyPilotSignupBilling(account, nowUtc);
            account.PilotStartedAtUtc = nowUtc;
            var endsAtUtc = account.PilotPeriodEnd!.Value;

            await PilotClockSync.SetForRestaurantAsync(
                _context,
                restaurantId,
                endsAtUtc,
                cancellationToken
            );
            await _context.SaveChangesAsync(cancellationToken);

            var mint = await _creditLedger.MintPilotAtActivationAsync(
                restaurantId,
                cancellationToken
            );
            // The ledger stages its rows on the context. Save them here.
            await _context.SaveChangesAsync(cancellationToken);
            if (!mint.Succeeded && mint.Code != "pilot_already_minted")
            {
                throw new InvalidOperationException(
                    mint.Code ?? "pilot_mint_failed"
                );
            }

            await _captureQrLifecycle.ActivatePlanPausedCodesAsync(
                restaurantId,
                cancellationToken
            );

            return new PilotStartResult(
                Started: true,
                RefusedCode: null,
                StartedAtUtc: nowUtc,
                EndsAtUtc: endsAtUtc
            );
        }

        private async Task<BillingAccount?> LoadBillingAccountForUpdateAsync(
            int restaurantId,
            CancellationToken cancellationToken
        )
        {
            if (_context.Database.IsSqlServer())
            {
                return await _context.BillingAccounts
                    .FromSql(
                        $"""
                        SELECT * FROM BillingAccounts WITH (UPDLOCK, ROWLOCK)
                        WHERE RestaurantId = {restaurantId}
                        """
                    )
                    .FirstOrDefaultAsync(cancellationToken);
            }

            return await _context.BillingAccounts.FirstOrDefaultAsync(
                row => row.RestaurantId == restaurantId,
                cancellationToken
            );
        }

        private async Task TrySendPilotStartedEmailAsync(
            string? ownerEmail,
            string? ownerFullName,
            string restaurantName,
            DateTime endsAtUtc
        )
        {
            if (string.IsNullOrWhiteSpace(ownerEmail))
            {
                return;
            }

            try
            {
                await _emailService.SendPilotStartedEmailAsync(
                    ownerEmail,
                    SignInMetadataResolver.ExtractFirstName(ownerFullName ?? string.Empty),
                    restaurantName,
                    LondonDateFormat.DMmmYyyy(endsAtUtc)
                );
            }
            catch
            {
                // Pilot start must not fail when outbound email fails.
            }
        }
    }
}
