using Microsoft.EntityFrameworkCore;
using TummlyBackend.Billing.CustomPlan;
using TummlyBackend.Billing.Pricebook;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Admin;
using TummlyBackend.DTOs.BillingCredits;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Read-only Customers directory and account profile for internal Admin staff.
    /// Capacity, credit and audit figures come from the same services the operator
    /// Billing page and the Admin audit log use. Never returns guest data.
    /// </summary>
    public sealed class AdminAccountDirectoryService : IAdminAccountDirectoryService
    {
        public const int DefaultTake = 50;
        public const int MaxTake = 100;

        private const int ProfileAuditTake = 20;
        private const int ProfilePaymentTake = 20;
        private const int ProfileInvoiceTake = 50;

        private readonly ApplicationDbContext _context;
        private readonly IPlanEntitlementsSnapshot _entitlements;
        private readonly IActiveQrCapacity _activeQrCapacity;
        private readonly ICreditBalanceSnapshot _creditBalance;
        private readonly IAdminAuditService _audit;
        private readonly IPricebookCatalog _pricebook;

        public AdminAccountDirectoryService(
            ApplicationDbContext context,
            IPlanEntitlementsSnapshot entitlements,
            IActiveQrCapacity activeQrCapacity,
            ICreditBalanceSnapshot creditBalance,
            IAdminAuditService audit,
            IPricebookCatalog pricebook
        )
        {
            _context = context;
            _entitlements = entitlements;
            _activeQrCapacity = activeQrCapacity;
            _creditBalance = creditBalance;
            _audit = audit;
            _pricebook = pricebook;
        }

        public async Task<AdminAccountListResultDto> ListAsync(
            AdminAccountListQuery query,
            CancellationToken cancellationToken = default
        )
        {
            if (
                query.CreatedFrom is DateTime from
                && query.CreatedBefore is DateTime before
                && from >= before
            )
            {
                throw new ArgumentException(
                    "createdFrom must be earlier than createdTo."
                );
            }

            var take = Math.Clamp(query.Take, 1, MaxTake);
            var skip = Math.Max(0, query.Skip);

            var rows = _context.Restaurants
                .AsNoTracking()
                .Where(row => row.BillingAccount != null);

            var term = query.Search?.Trim();
            if (!string.IsNullOrEmpty(term))
            {
                // A non-numeric term never equals a Workspace ID.
                var workspaceId = int.TryParse(term, out var parsed) ? parsed : -1;
                rows = rows.Where(row =>
                    row.Id == workspaceId
                    || row.Name.Contains(term)
                    || (
                        row.BusinessDetails != null
                        && (
                            (row.BusinessDetails.TradingName ?? string.Empty).Contains(term)
                            || (row.BusinessDetails.LegalBusinessName ?? string.Empty).Contains(term)
                        )
                    )
                );
            }

            var plan = query.SubscriptionPlan?.Trim();
            if (!string.IsNullOrEmpty(plan))
            {
                rows = rows.Where(row => row.BillingAccount!.SubscriptionPlan == plan);
            }

            var status = query.BillingStatus?.Trim();
            if (!string.IsNullOrEmpty(status))
            {
                rows = rows.Where(row => row.BillingAccount!.BillingStatus == status);
            }

            if (query.CreatedFrom is DateTime createdFrom)
            {
                rows = rows.Where(row => row.CreatedAt >= createdFrom);
            }

            if (query.CreatedBefore is DateTime createdBefore)
            {
                rows = rows.Where(row => row.CreatedAt < createdBefore);
            }

            var totalCount = await rows.CountAsync(cancellationToken);

            var items = await rows
                .OrderByDescending(row => row.CreatedAt)
                .ThenByDescending(row => row.Id)
                .Skip(skip)
                .Take(take)
                .Select(row => new AdminAccountListRowDto
                {
                    RestaurantId = row.Id,
                    DisplayName = row.Name,
                    SubscriptionPlan = row.BillingAccount!.SubscriptionPlan,
                    BillingStatus = row.BillingAccount!.BillingStatus,
                    CreatedAtUtc = row.CreatedAt,
                    PeriodEndUtc = row.BillingAccount!.SubscriptionPlan == BillingSubscriptionPlans.Pilot
                        ? row.BillingAccount!.PilotPeriodEnd
                        : row.BillingAccount!.RenewalDateUtc,
                    ActiveLocationCount = row.Locations.Count(
                        location => location.LifecycleStatus == LocationLifecycleStatus.Active
                    ),
                    OwnerDisplayName = row.OwnerUser.FullName,
                })
                .ToListAsync(cancellationToken);

            return new AdminAccountListResultDto
            {
                Items = items,
                TotalCount = totalCount,
                Skip = skip,
                Take = take,
            };
        }

        public async Task<AdminAccountProfileDto?> GetProfileAsync(
            int restaurantId,
            CancellationToken cancellationToken = default
        )
        {
            var restaurant = await _context.Restaurants
                .AsNoTracking()
                .Include(row => row.OwnerUser)
                .Include(row => row.BusinessDetails)
                .Include(row => row.BillingAccount)
                .FirstOrDefaultAsync(row => row.Id == restaurantId, cancellationToken);

            var billing = restaurant?.BillingAccount;
            if (restaurant == null || billing == null)
            {
                return null;
            }

            var now = DateTime.UtcNow;
            var locations = await _context.RestaurantLocations
                .AsNoTracking()
                .Where(row => row.RestaurantId == restaurantId)
                .OrderBy(row => row.Id)
                .ToListAsync(cancellationToken);
            var managers = LocationAssignedManager.MapByLocationId(
                await _context.RestaurantMemberships
                    .AsNoTracking()
                    .Include(row => row.User)
                    .Where(row =>
                        row.RestaurantId == restaurantId
                        && row.Status == MembershipStatus.Active
                        && row.PermissionRole == PermissionRoles.LocationManager
                    )
                    .ToListAsync(cancellationToken)
            );

            var planSnapshot = await _entitlements.GetAccountAsync(restaurantId, cancellationToken);
            var qrAllowance = await _activeQrCapacity.GetAllowanceAsync(billing, cancellationToken);
            var qrUsed = await _activeQrCapacity.CountUsedForRestaurantAsync(
                restaurantId,
                cancellationToken
            );

            var pendingInvites = await _context.TeamInvitations
                .AsNoTracking()
                .Where(row => row.RestaurantId == restaurantId && row.ExpiresAt > now)
                .OrderByDescending(row => row.SentAt)
                .Select(row => new AdminAccountPendingInviteDto
                {
                    MaskedEmail = row.Email,
                    SentAtUtc = row.SentAt,
                    ExpiresAtUtc = row.ExpiresAt,
                })
                .ToListAsync(cancellationToken);
            foreach (var invite in pendingInvites)
            {
                invite.MaskedEmail = GuestContactMask.MaskEmail(invite.MaskedEmail) ?? string.Empty;
            }

            var creditSnapshot = await _creditBalance.GetAccountAsync(restaurantId, cancellationToken);
            var currentPeriodStart = await _context.CreditLedgerEntries
                .AsNoTracking()
                .Where(row =>
                    row.RestaurantId == restaurantId
                    && row.EntryType == CreditLedgerEntryTypes.IncludedAllocation
                    && row.PeriodStartUtc != null
                )
                .Select(row => row.PeriodStartUtc)
                .MaxAsync(cancellationToken);

            var payments = await _context.RevolutOrderIntents
                .AsNoTracking()
                .Where(row => row.RestaurantId == restaurantId)
                .OrderByDescending(row => row.CreatedAtUtc)
                .Take(ProfilePaymentTake)
                .Select(row => new AdminAccountPaymentDto
                {
                    Id = row.Id,
                    Kind = row.Purpose,
                    AmountPence = row.GrossAmountMinor,
                    Currency = TummlyVatInvoice.CurrencyGbp,
                    IsOpen = row.IsOpen,
                    CreatedAtUtc = row.CreatedAtUtc,
                    ProviderReference = row.OrderId,
                })
                .ToListAsync(cancellationToken);
            foreach (var payment in payments)
            {
                payment.ProviderReference = RedactReference(payment.ProviderReference);
            }

            var invoices = await _context.TummlyVatInvoices
                .AsNoTracking()
                .Where(row => row.RestaurantId == restaurantId)
                .OrderByDescending(row => row.InvoiceDateUtc)
                .ThenByDescending(row => row.DocumentNumber)
                .Take(ProfileInvoiceTake)
                .Select(row => new AdminAccountInvoiceDto
                {
                    DocumentNumber = row.DocumentNumber,
                    Type = row.DocumentPrefix == TummlyVatInvoice.PrefixTcn
                        ? "Credit note"
                        : "Invoice",
                    InvoiceDateUtc = row.InvoiceDateUtc,
                    GrossPence = row.GrossPence,
                    Currency = row.Currency,
                })
                .ToListAsync(cancellationToken);

            var audit = await _audit.ListAsync(
                new AdminAuditListQuery(RestaurantId: restaurantId, Take: ProfileAuditTake),
                cancellationToken
            );

            var isPilot = billing.SubscriptionPlan == BillingSubscriptionPlans.Pilot;
            var locationRows = locations
                .Select(row => new AdminAccountLocationRowDto
                {
                    LocationId = row.Id,
                    Name = row.LocationName,
                    LifecycleStatus = row.LifecycleStatus.ToString(),
                    CreatedAtUtc = row.CreatedAt,
                    AssignedManagerCount = managers.ContainsKey(row.Id) ? 1 : 0,
                })
                .ToList();

            return new AdminAccountProfileDto
            {
                Identity = new AdminAccountIdentityDto
                {
                    RestaurantId = restaurant.Id,
                    DisplayName = restaurant.Name,
                    LegalName = restaurant.BusinessDetails?.LegalBusinessName,
                    OwnerName = restaurant.OwnerUser?.FullName,
                    OwnerEmail = restaurant.OwnerUser?.Email,
                    CreatedAtUtc = restaurant.CreatedAt,
                    BillingAccountId = billing.RestaurantId,
                },
                Lifecycle = new AdminAccountLifecycleDto
                {
                    SubscriptionPlan = billing.SubscriptionPlan,
                    BillingCycle = billing.BillingCycle,
                    BillingStatus = billing.BillingStatus,
                    PilotStartedAtUtc = billing.PilotStartedAtUtc,
                    PilotPeriodEndUtc = billing.PilotPeriodEnd,
                    CurrentPeriodStartUtc = currentPeriodStart,
                    CurrentPeriodEndUtc = billing.RenewalDateUtc,
                    ScheduledChange = new AdminAccountScheduledChangeDto
                    {
                        HasScheduledChange = billing.HasScheduledChange,
                        TargetSubscriptionPlan = billing.ScheduledTargetSubscriptionPlan,
                        TargetBillingCycle = billing.ScheduledTargetBillingCycle,
                        TargetExtraLocationCount = billing.ScheduledTargetExtraLocationCount,
                        CancelScheduled = billing.ScheduledCancelPlan,
                        EffectiveAtUtc = billing.HasScheduledChange || billing.ScheduledCancelPlan
                            ? billing.RenewalDateUtc
                            : null,
                        CancelAtUtc = billing.ScheduledCancelPlan ? billing.RenewalDateUtc : null,
                    },
                    SoftLockEnteredAtUtc = billing.SoftLockEnteredAt,
                    DormantEnteredAtUtc = billing.DormantEnteredAt,
                },
                Locations = new AdminAccountLocationsDto
                {
                    Counts = new AdminAccountLocationCountsDto
                    {
                        Included = ResolveIncludedLocations(billing),
                        PaidExtra = billing.PaidExtraLocationCount,
                        Active = locations.Count(row => row.LifecycleStatus == LocationLifecycleStatus.Active),
                        Total = locations.Count,
                    },
                    Items = locationRows,
                },
                Capacity = new AdminAccountCapacityDto
                {
                    Seats = ToLimit(planSnapshot.TeamMembers),
                    QrPlacements = qrAllowance == null
                        ? new PlanEntitlementLimitDto { Available = false }
                        : ToLimit(new PlanEntitlementLimitDto
                        {
                            Cap = qrAllowance.WorkspacePool,
                            Current = qrUsed,
                        }),
                    Offers = ToLimit(planSnapshot.ActiveOffers),
                    PendingInvites = pendingInvites,
                },
                Credits = creditSnapshot == null
                    ? []
                    : CreditChannels.All
                        .Select(channel => ToCreditChannel(creditSnapshot, channel))
                        .ToList(),
                Payments = payments,
                Invoices = invoices,
                RecentAudit = audit.Items,
            };
        }

        /// <summary>
        /// Included Locations of the contracted plan. Custom plans read their
        /// configured quantity; an unresolvable plan reports 0.
        /// </summary>
        private int ResolveIncludedLocations(BillingAccount billing)
        {
            if (
                string.Equals(
                    billing.SubscriptionPlan,
                    BillingSubscriptionPlans.Custom,
                    StringComparison.Ordinal
                )
            )
            {
                return CustomPlanConfig.TryParse(billing.CustomPlanConfigJson)?.Quantities.Locations ?? 0;
            }

            try
            {
                var book = _pricebook.GetRequired(billing.ContractedPricebookId);
                return book.Plans.TryGetValue(
                    billing.SubscriptionPlan.Trim().ToLowerInvariant(),
                    out var plan
                )
                    ? plan.IncludedLocations
                    : 0;
            }
            catch (InvalidOperationException)
            {
                return 0;
            }
        }

        private static AdminAccountCreditChannelDto ToCreditChannel(
            CreditBalanceAccountSnapshot snapshot,
            string channel
        )
        {
            var row = snapshot.Channels.FirstOrDefault(item => item.Channel == channel);
            return new AdminAccountCreditChannelDto
            {
                Channel = channel,
                Available = row?.Remaining ?? 0,
                Reserved = row?.Held ?? 0,
                IncludedThisPeriod = row?.IncludedThisPeriod ?? 0,
                PrepaidRemaining = row?.PurchasedRemaining ?? 0,
                NearestPrepaidExpiryUtc = row?.EarliestPurchasedExpiryUtc,
            };
        }

        /// <summary>Mirrors the Plan entitlements rule: a negative cap is unavailable.</summary>
        private static PlanEntitlementLimitDto ToLimit(PlanEntitlementLimitDto limit)
        {
            if (!limit.Available || limit.Cap < 0)
            {
                return new PlanEntitlementLimitDto { Available = false };
            }

            return new PlanEntitlementLimitDto
            {
                Cap = limit.Cap,
                Current = limit.Current,
                AtCap = limit.Current >= limit.Cap,
                Available = true,
            };
        }

        /// <summary>Provider references show only their last six characters.</summary>
        private static string? RedactReference(string? reference)
        {
            if (string.IsNullOrEmpty(reference))
            {
                return null;
            }

            return reference.Length <= 6
                ? "••••"
                : $"••••{reference[^6..]}";
        }
    }
}
