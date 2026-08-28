using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.BillingCredits;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public class BillingCreditsService : IBillingCreditsService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPricebookCatalog _pricebookCatalog;
        private readonly ICreditBalanceSnapshot _creditBalance;

        public BillingCreditsService(
            ApplicationDbContext context,
            IPricebookCatalog pricebookCatalog,
            ICreditBalanceSnapshot creditBalance
        )
        {
            _context = context;
            _pricebookCatalog = pricebookCatalog;
            _creditBalance = creditBalance;
        }

        public async Task<BillingCreditsPageDto?> GetPageAsync(
            int userId,
            int restaurantId,
            bool actorCanManage
        )
        {
            var restaurant = await _context.Restaurants
                .AsNoTracking()
                .FirstOrDefaultAsync(row => row.Id == restaurantId);

            if (restaurant == null)
            {
                return null;
            }

            var actorMembership = await _context.RestaurantMemberships
                .AsNoTracking()
                .FirstOrDefaultAsync(row =>
                    row.UserId == userId
                    && row.RestaurantId == restaurantId
                    && row.Status == MembershipStatus.Active
                );
            var actorPermissionRole =
                actorMembership?.PermissionRole ?? PermissionRoles.Owner;

            var owner = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(row => row.Id == restaurant.OwnerUserId);

            var activeLocations = await _context.RestaurantLocations
                .AsNoTracking()
                .CountAsync(row => row.RestaurantId == restaurantId);

            var billingAccount = await LoadRequiredBillingAccountAsync(restaurantId);
            var creditSnapshot = await _creditBalance.GetAccountAsync(restaurantId);
            var isPilot = IsPilotBillingAccount(billingAccount);
            var contractedBook = _pricebookCatalog.GetRequired(
                billingAccount.ContractedPricebookId
            );
            var planKey = SubscriptionPlanKey(billingAccount.SubscriptionPlan);
            if (!contractedBook.Plans.TryGetValue(planKey, out var contractedPlan))
            {
                throw new InvalidOperationException(
                    $"Contracted plan '{billingAccount.SubscriptionPlan}' is missing from pricebook '{billingAccount.ContractedPricebookId}'."
                );
            }

            var eligibleMembers = await LoadEligibleMembersAsync(restaurantId);
            var pilotEndsAt = owner?.ActivationExpiresAt;
            var renewalDateLabel = isPilot && pilotEndsAt != null
                ? $"Pilot ends {FormatUkDate(pilotEndsAt.Value)}"
                : isPilot
                    ? null
                    : "Renews 15 September 2026";
            var scheduledChangeLine = (string?)null;
            var sms5000Available =
                billingAccount.AllowSms5000TopUp
                || string.Equals(
                    billingAccount.SubscriptionPlan,
                    BillingSubscriptionPlans.Group,
                    StringComparison.Ordinal
                );
            var accessLevel = actorCanManage ? "manage" : "view";

            return new BillingCreditsPageDto
            {
                AccessLevel = accessLevel,
                ActorPermissionRole = actorPermissionRole,
                ActorCanManage = actorCanManage,
                ActorCanPersistBillingContacts =
                    actorCanManage
                    && actorPermissionRole == PermissionRoles.Owner,
                WriteCapabilities = ResolveWriteCapabilities(
                    accessLevel,
                    actorPermissionRole
                ),
                PlanSubscription = new PlanSubscriptionSnapshotDto
                {
                    SubscriptionPlan = billingAccount.SubscriptionPlan,
                    BillingStatus = billingAccount.BillingStatus,
                    RenewalDateLabel = renewalDateLabel,
                    EmailCreditsRemaining = ChannelRemaining(creditSnapshot, "email"),
                    SmsCreditsRemaining = ChannelRemaining(creditSnapshot, "sms"),
                    AiCreditsRemaining = ChannelRemaining(creditSnapshot, "ai"),
                    BillingCycle = billingAccount.BillingCycle,
                    PlanPriceNet = _pricebookCatalog.FormatPlanPriceNet(
                        contractedPlan,
                        billingAccount.BillingCycle
                    ),
                    IncludedLocations = contractedPlan.IncludedLocations,
                    ActiveLocations = activeLocations,
                    IncludedEmailCreditsLabel =
                        _pricebookCatalog.FormatIncludedCreditsLabel(
                            contractedPlan,
                            "email"
                        ),
                    IncludedSmsCreditsLabel =
                        _pricebookCatalog.FormatIncludedCreditsLabel(
                            contractedPlan,
                            "sms"
                        ),
                    IncludedAiCreditsLabel =
                        _pricebookCatalog.FormatIncludedCreditsLabel(
                            contractedPlan,
                            "ai"
                        ),
                    StarterKitState = billingAccount.StarterKitState,
                    PricebookId = billingAccount.ContractedPricebookId,
                    ScheduledChangeLine = scheduledChangeLine,
                    IsPilot = isPilot,
                    AllowSms5000TopUp = billingAccount.AllowSms5000TopUp,
                },
                PaymentMethod = isPilot ? null : BuildStubPaymentMethod(),
                Invoices = isPilot ? [] : BuildStubInvoices(),
                BillingContacts = MapBillingContacts(
                    restaurant,
                    billingAccount,
                    eligibleMembers
                ),
                CurrentCatalog = _pricebookCatalog.BuildCurrentCatalog(
                    sms5000Available
                ),
            };
        }

        public async Task<(byte[] Content, string FileName)?> GetInvoicePdfAsync(
            int restaurantId,
            string invoiceNo
        )
        {
            var restaurant = await _context.Restaurants
                .AsNoTracking()
                .FirstOrDefaultAsync(row => row.Id == restaurantId);

            if (restaurant == null)
            {
                return null;
            }

            var owner = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(row => row.Id == restaurant.OwnerUserId);

            if (
                await BillingPlanSnapshotHelper.IsPilotRestaurantAsync(
                    _context,
                    restaurantId,
                    owner
                )
            )
            {
                return null;
            }

            if (!IsKnownInvoiceNo(invoiceNo))
            {
                return null;
            }

            return (BuildStubPdfBytes(invoiceNo), $"{invoiceNo}.pdf");
        }

        public Task<PaymentMethodUpdateSessionDto?> CreatePaymentMethodUpdateSessionAsync(
            int restaurantId
        )
        {
            return Task.FromResult<PaymentMethodUpdateSessionDto?>(
                new PaymentMethodUpdateSessionDto
                {
                    RedirectUrl =
                        "https://sandbox-merchant.revolut.com/hpp/update-payment-method",
                }
            );
        }

        private static bool IsPilotBillingAccount(BillingAccount billingAccount)
        {
            return string.Equals(
                billingAccount.SubscriptionPlan,
                BillingSubscriptionPlans.Pilot,
                StringComparison.Ordinal
            );
        }

        private static PaymentMethodSnapshotDto BuildStubPaymentMethod()
        {
            return new PaymentMethodSnapshotDto
            {
                Kind = "card",
                Brand = "Visa",
                Last4 = "4242",
                ExpiryLabel = "08/28",
            };
        }

        private static List<InvoiceRowDto> BuildStubInvoices()
        {
            return
            [
                new InvoiceRowDto
                {
                    InvoiceNo = "TM-2026-000001",
                    InvoiceDateLabel = "12 Jul 2026",
                    Description = "Growth plan",
                    AmountLabel = "£118.80",
                    Status = "Paid",
                    ShowActions = true,
                },
                new InvoiceRowDto
                {
                    InvoiceNo = "TCN-2026-000001",
                    InvoiceDateLabel = "18 Jul 2026",
                    Description = "Credit note for TM-2026-000001",
                    AmountLabel = "−£11.88",
                    Status = "Issued",
                    ShowActions = true,
                },
                new InvoiceRowDto
                {
                    InvoiceNo = "TM-2026-000002",
                    InvoiceDateLabel = "1 Aug 2026",
                    Description = "Growth plan",
                    AmountLabel = "£118.80",
                    Status = "Void",
                    ShowActions = false,
                },
            ];
        }

        private static bool IsKnownInvoiceNo(string invoiceNo)
        {
            return invoiceNo is "TM-2026-000001" or "TCN-2026-000001";
        }

        private static byte[] BuildStubPdfBytes(string invoiceNo)
        {
            var text = $"%PDF-1.4 stub invoice {invoiceNo}\n%%EOF\n";
            return System.Text.Encoding.UTF8.GetBytes(text);
        }

        private static string FormatUkDate(DateTime value)
        {
            return value.ToString(
                "d MMMM yyyy",
                System.Globalization.CultureInfo.GetCultureInfo("en-GB")
            );
        }

        public async Task<CreditsUsageSnapshotDto?> GetUsageAsync(int restaurantId)
        {
            var restaurant = await _context.Restaurants
                .AsNoTracking()
                .FirstOrDefaultAsync(row => row.Id == restaurantId);

            if (restaurant == null)
            {
                return null;
            }

            var snapshot = await _creditBalance.GetAccountAsync(restaurantId);
            if (snapshot == null)
            {
                return null;
            }

            return new CreditsUsageSnapshotDto
            {
                PeriodLabel = snapshot.PeriodLabel,
                StarterKitState = snapshot.StarterKitState,
                IsPilot = snapshot.IsPilot,
                Channels = snapshot.Channels
                    .Select(channel => new CreditChannelUsageDto
                    {
                        Channel = channel.Channel,
                        CombinedRemaining = channel.Remaining,
                        Held = channel.Held,
                        UsedThisCycle = channel.UsedThisCycle,
                        IncludedThisPeriod = channel.IncludedThisPeriod,
                        PurchasedRemaining = channel.PurchasedRemaining,
                        PurchasedExpiryLabel = channel.EarliestPurchasedExpiryUtc == null
                            ? null
                            : FormatUkDate(channel.EarliestPurchasedExpiryUtc.Value),
                        UsedShare = channel.UsedShare,
                    })
                    .ToList(),
            };
        }

        private static int ChannelRemaining(
            CreditBalanceAccountSnapshot? snapshot,
            string channel
        )
        {
            if (snapshot == null)
            {
                return 0;
            }

            return snapshot.Channels
                .FirstOrDefault(row => row.Channel == channel)
                ?.Remaining ?? 0;
        }

        public async Task<PlanChangeResultDto?> SubmitPlanChangeAsync(
            int userId,
            int restaurantId,
            PlanChangeRequestDto request
        )
        {
            var restaurant = await _context.Restaurants
                .AsNoTracking()
                .FirstOrDefaultAsync(row => row.Id == restaurantId);

            if (restaurant == null)
            {
                return null;
            }

            var actorMembership = await _context.RestaurantMemberships
                .AsNoTracking()
                .FirstOrDefaultAsync(row =>
                    row.UserId == userId
                    && row.RestaurantId == restaurantId
                    && row.Status == MembershipStatus.Active
                );
            var actorRole =
                actorMembership?.PermissionRole ?? PermissionRoles.Owner;

            if (actorRole != PermissionRoles.Owner)
            {
                throw new InvalidOperationException("forbidden");
            }

            var owner = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(row => row.Id == restaurant.OwnerUserId);

            var pilotEndsAt = owner?.ActivationExpiresAt;
            var renewalDateLabel = pilotEndsAt == null
                ? "your renewal date"
                : FormatUkDate(pilotEndsAt.Value);

            var targetPlan = request.TargetPlan.Trim();
            var targetCadence = request.TargetCadence.Trim().ToLowerInvariant();

            if (string.Equals(targetPlan, "Pilot", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("invalid-target");
            }

            var lifecycle = BillingPlanSnapshotHelper.ResolveLifecycle(
                restaurant.Name,
                owner?.ActivationExpiresAt
            );
            var simulatePaidGrowth =
                string.Equals(
                    restaurant.Name,
                    "Billing Venue Schedule Test",
                    StringComparison.Ordinal
                );
            var currentPlan = simulatePaidGrowth
                ? "Growth"
                : lifecycle.IsPilot
                    ? "Pilot"
                    : lifecycle.SubscriptionPlan;
            var isPilot = simulatePaidGrowth ? false : lifecycle.IsPilot;
            string? liveCadence = simulatePaidGrowth || !isPilot ? "monthly" : null;

            if (
                BillingPlanSnapshotHelper.IsAccountLocked(lifecycle.BillingStatus)
                && !isPilot
            )
            {
                throw new InvalidOperationException(
                    BillingPlanSnapshotHelper.LockDenyCode(lifecycle.BillingStatus)
                        ?? "forbidden"
                );
            }

            var payNow = ResolvePlanChangeRequiresPay(
                currentPlan,
                targetPlan,
                isPilot,
                liveCadence,
                targetCadence
            );

            if (payNow)
            {
                return new PlanChangeResultDto
                {
                    Outcome = "pay",
                    RedirectUrl =
                        $"https://checkout.revolut.com/pay/example/{restaurantId}/{targetPlan.ToLowerInvariant()}",
                };
            }

            var cadenceLabel =
                targetCadence == "annual" ? "Annual" : "Monthly";
            var scheduledLine =
                string.Equals(currentPlan, targetPlan, StringComparison.OrdinalIgnoreCase)
                    ? $"Changes to {cadenceLabel} on {renewalDateLabel}"
                    : $"Changes to {targetPlan} on {renewalDateLabel}";

            return new PlanChangeResultDto
            {
                Outcome = "scheduled",
                ScheduledChangeLine = scheduledLine,
            };
        }

        public static bool ResolvePlanChangeRequiresPay(
            string currentPlan,
            string targetPlan,
            bool isPilot,
            string? liveCadence,
            string targetCadence
        )
        {
            if (isPilot)
            {
                return true;
            }

            if (
                string.Equals(currentPlan, targetPlan, StringComparison.OrdinalIgnoreCase)
                && liveCadence != null
                && !string.Equals(liveCadence, targetCadence, StringComparison.OrdinalIgnoreCase)
            )
            {
                return false;
            }

            if (
                liveCadence != null
                && !string.Equals(liveCadence, targetCadence, StringComparison.OrdinalIgnoreCase)
            )
            {
                return false;
            }

            return PlanRank(targetPlan) > PlanRank(currentPlan);
        }

        private static int PlanRank(string plan)
        {
            return plan switch
            {
                "Starter" => 1,
                "Growth" => 2,
                "Group" => 3,
                _ => 0,
            };
        }

        public const int GroupIncludedLocations = 5;

        public const int GroupLocationCap = 30;

        public static BillingWriteCapabilitiesDto ResolveWriteCapabilities(
            string accessLevel,
            string actorPermissionRole
        )
        {
            var canManage = accessLevel == "manage";
            var isOwner = actorPermissionRole == PermissionRoles.Owner;
            var isBillingAdmin =
                actorPermissionRole == PermissionRoles.BillingAdmin;
            var isAdmin = actorPermissionRole == PermissionRoles.Admin;
            var buyOrPay = canManage && (isOwner || isBillingAdmin || isAdmin);

            return new BillingWriteCapabilitiesDto
            {
                ChangePlan = canManage && isOwner,
                BuyTopup = buyOrPay,
                UpdatePaymentMethod = buyOrPay,
                UpdateBillingContacts = canManage && isOwner,
                CancelPlan = canManage && isOwner,
                ChangeExtraLocation = canManage && isOwner,
                ClearScheduledChange = canManage && isOwner,
            };
        }

        public static bool CanRemoveExtraGroupLocation(
            int includedLocations,
            int activeLocations
        )
        {
            if (includedLocations <= GroupIncludedLocations)
            {
                return false;
            }

            return includedLocations - 1 >= activeLocations;
        }

        public async Task<ExtraLocationResultDto?> AddExtraGroupLocationAsync(
            int userId,
            int restaurantId
        )
        {
            var context = await RequireOwnerGroupPlanContextAsync(userId, restaurantId);
            if (context == null)
            {
                return null;
            }

            if (context.IncludedLocations >= GroupLocationCap)
            {
                throw new InvalidOperationException("location-cap-reached");
            }

            return new ExtraLocationResultDto
            {
                Outcome = "pay",
                RedirectUrl =
                    $"https://checkout.revolut.com/pay/example/{restaurantId}/extra-location",
            };
        }

        public async Task<ExtraLocationResultDto?> RemoveExtraGroupLocationAsync(
            int userId,
            int restaurantId
        )
        {
            var context = await RequireOwnerGroupPlanContextAsync(userId, restaurantId);
            if (context == null)
            {
                return null;
            }

            if (
                !CanRemoveExtraGroupLocation(
                    context.IncludedLocations,
                    context.ActiveLocations
                )
            )
            {
                throw new InvalidOperationException("remove-below-floor");
            }

            return new ExtraLocationResultDto
            {
                Outcome = "scheduled",
                ScheduledChangeLine =
                    $"Removes 1 Additional Group Location on {context.RenewalDateLabel}",
            };
        }

        public async Task<CancelPlanResultDto?> CancelPlanAsync(
            int userId,
            int restaurantId
        )
        {
            var restaurant = await _context.Restaurants
                .AsNoTracking()
                .FirstOrDefaultAsync(row => row.Id == restaurantId);

            if (restaurant == null)
            {
                return null;
            }

            var actorMembership = await _context.RestaurantMemberships
                .AsNoTracking()
                .FirstOrDefaultAsync(row =>
                    row.UserId == userId
                    && row.RestaurantId == restaurantId
                    && row.Status == MembershipStatus.Active
                );
            var actorRole =
                actorMembership?.PermissionRole ?? PermissionRoles.Owner;

            if (actorRole != PermissionRoles.Owner)
            {
                throw new InvalidOperationException("forbidden");
            }

            var owner = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(row => row.Id == restaurant.OwnerUserId);

            var isPilot = await BillingPlanSnapshotHelper.IsPilotRestaurantAsync(
                _context,
                restaurantId,
                owner
            );
            if (isPilot)
            {
                throw new InvalidOperationException("pilot-cancel-not-allowed");
            }

            var lifecycle = BillingPlanSnapshotHelper.ResolveLifecycle(
                restaurant.Name,
                owner?.ActivationExpiresAt
            );
            if (BillingPlanSnapshotHelper.IsAccountLocked(lifecycle.BillingStatus))
            {
                throw new InvalidOperationException(
                    BillingPlanSnapshotHelper.LockDenyCode(lifecycle.BillingStatus)
                        ?? "forbidden"
                );
            }

            var planContext = await ResolveLivePlanContextAsync(restaurantId);

            var renewalDate = planContext.RenewalDateLabel.Replace("Renews ", "");

            return new CancelPlanResultDto
            {
                ScheduledChangeLine = $"Cancels on {renewalDate}",
            };
        }

        private async Task<(
            string SubscriptionPlan,
            int IncludedLocations,
            string RenewalDateLabel
        )> ResolveLivePlanContextAsync(int restaurantId)
        {
            var billingAccount = await LoadRequiredBillingAccountAsync(restaurantId);
            var book = _pricebookCatalog.GetRequired(
                billingAccount.ContractedPricebookId
            );
            var planKey = SubscriptionPlanKey(billingAccount.SubscriptionPlan);
            if (!book.Plans.TryGetValue(planKey, out var plan))
            {
                throw new InvalidOperationException(
                    $"Plan '{billingAccount.SubscriptionPlan}' is missing from pricebook."
                );
            }

            var includedLocations = plan.IncludedLocations;
            if (
                string.Equals(
                    billingAccount.SubscriptionPlan,
                    BillingSubscriptionPlans.Group,
                    StringComparison.Ordinal
                )
            )
            {
                // Stub until ticket 24 stamps paid extra Location count on the row.
                var restaurantName = await _context.Restaurants
                    .AsNoTracking()
                    .Where(row => row.Id == restaurantId)
                    .Select(row => row.Name)
                    .FirstAsync();
                if (
                    restaurantName.Contains(
                        "Group Extra",
                        StringComparison.Ordinal
                    )
                )
                {
                    includedLocations += 2;
                }
            }

            return (
                billingAccount.SubscriptionPlan,
                includedLocations,
                "Renews 15 September 2026"
            );
        }

        private async Task<int> CountActiveLocationsAsync(int restaurantId)
        {
            return await _context.RestaurantLocations
                .AsNoTracking()
                .CountAsync(row => row.RestaurantId == restaurantId);
        }

        private sealed record OwnerGroupPlanContext(
            int IncludedLocations,
            int ActiveLocations,
            string RenewalDateLabel
        );

        private async Task<OwnerGroupPlanContext?> RequireOwnerGroupPlanContextAsync(
            int userId,
            int restaurantId
        )
        {
            var restaurant = await _context.Restaurants
                .AsNoTracking()
                .FirstOrDefaultAsync(row => row.Id == restaurantId);

            if (restaurant == null)
            {
                return null;
            }

            var actorMembership = await _context.RestaurantMemberships
                .AsNoTracking()
                .FirstOrDefaultAsync(row =>
                    row.UserId == userId
                    && row.RestaurantId == restaurantId
                    && row.Status == MembershipStatus.Active
                );
            var actorRole =
                actorMembership?.PermissionRole ?? PermissionRoles.Owner;

            if (actorRole != PermissionRoles.Owner)
            {
                throw new InvalidOperationException("forbidden");
            }

            var owner = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(row => row.Id == restaurant.OwnerUserId);

            var isPilot = await BillingPlanSnapshotHelper.IsPilotRestaurantAsync(
                _context,
                restaurantId,
                owner
            );
            if (isPilot)
            {
                throw new InvalidOperationException("not-group-plan");
            }

            var lifecycle = BillingPlanSnapshotHelper.ResolveLifecycle(
                restaurant.Name,
                owner?.ActivationExpiresAt
            );
            if (BillingPlanSnapshotHelper.IsAccountLocked(lifecycle.BillingStatus))
            {
                throw new InvalidOperationException(
                    BillingPlanSnapshotHelper.LockDenyCode(lifecycle.BillingStatus)
                        ?? "forbidden"
                );
            }

            var activeLocations = await CountActiveLocationsAsync(restaurantId);
            var planContext = await ResolveLivePlanContextAsync(restaurantId);

            if (
                !string.Equals(
                    planContext.SubscriptionPlan,
                    BillingSubscriptionPlans.Group,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                throw new InvalidOperationException("not-group-plan");
            }

            return new OwnerGroupPlanContext(
                planContext.IncludedLocations,
                activeLocations,
                planContext.RenewalDateLabel.Replace("Renews ", "")
            );
        }

        public async Task<(
            UpdateBillingContactsResponseDto? Response,
            string? Error,
            int StatusCode
        )> UpdateBillingContactsAsync(
            int actorUserId,
            int restaurantId,
            UpdateBillingContactsRequest request
        )
        {
            var actorMembership = await _context.RestaurantMemberships
                .AsNoTracking()
                .FirstOrDefaultAsync(row =>
                    row.UserId == actorUserId
                    && row.RestaurantId == restaurantId
                    && row.Status == MembershipStatus.Active
                );
            var actorPermissionRole =
                actorMembership?.PermissionRole ?? PermissionRoles.Owner;
            if (actorPermissionRole != PermissionRoles.Owner)
            {
                return (
                    null,
                    "Only the account owner may update billing contacts.",
                    StatusCodes.Status403Forbidden
                );
            }

            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(row => row.Id == restaurantId);

            if (restaurant == null)
            {
                return (null, "Restaurant not found.", StatusCodes.Status404NotFound);
            }

            if (request.BillingContactUserId <= 0)
            {
                return (
                    null,
                    "Billing contact is required.",
                    StatusCodes.Status400BadRequest
                );
            }

            var eligibleIds = await GetEligibleMemberIdsAsync(restaurantId);
            if (!eligibleIds.Contains(request.BillingContactUserId))
            {
                return (
                    null,
                    "Billing contact must be an eligible team member.",
                    StatusCodes.Status400BadRequest
                );
            }

            var billingEmail = NormalizeBillingEmail(request.BillingEmail);
            if (billingEmail != null && !IsValidEmail(billingEmail))
            {
                return (
                    null,
                    "Billing email is not valid.",
                    StatusCodes.Status400BadRequest
                );
            }

            restaurant.BillingContactUserId = request.BillingContactUserId;

            var billingAccount = await _context.BillingAccounts
                .FirstOrDefaultAsync(row => row.RestaurantId == restaurantId);
            if (billingAccount == null)
            {
                return (
                    null,
                    "Billing Account is missing for this restaurant.",
                    StatusCodes.Status500InternalServerError
                );
            }

            billingAccount.BillingEmail = billingEmail;
            billingAccount.LowCreditAlertOwner = request.LowCreditAlerts.Owner;
            billingAccount.LowCreditAlertAdmin = request.LowCreditAlerts.Admin;
            billingAccount.LowCreditAlertBillingContact =
                request.LowCreditAlerts.BillingContact;
            billingAccount.PaymentFailureAlertOwner =
                request.PaymentFailureAlerts.Owner;
            billingAccount.PaymentFailureAlertBillingContact =
                request.PaymentFailureAlerts.BillingContact;

            await _context.SaveChangesAsync();

            var eligibleMembers = await LoadEligibleMembersAsync(restaurantId);
            return (
                new UpdateBillingContactsResponseDto
                {
                    BillingContacts = MapBillingContacts(
                        restaurant,
                        billingAccount,
                        eligibleMembers
                    ),
                },
                null,
                StatusCodes.Status200OK
            );
        }

        private async Task<BillingAccount> LoadRequiredBillingAccountAsync(
            int restaurantId
        )
        {
            var existing = await _context.BillingAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(row => row.RestaurantId == restaurantId);

            if (existing != null)
            {
                return existing;
            }

            throw new InvalidOperationException(
                $"Billing Account is missing for restaurant {restaurantId}."
            );
        }

        public static BillingAccount CreateDefaultBillingAccount(
            int restaurantId,
            string contractedPricebookId
        )
        {
            return new BillingAccount
            {
                RestaurantId = restaurantId,
                RevolutCustomerId = null,
                SubscriptionPlan = BillingSubscriptionPlans.Pilot,
                BillingCycle = null,
                BillingStatus = BillingStatuses.Pilot,
                ContractedPricebookId = contractedPricebookId,
                LowCreditAlertOwner = true,
                LowCreditAlertAdmin = false,
                LowCreditAlertBillingContact = true,
                PaymentFailureAlertOwner = true,
                PaymentFailureAlertBillingContact = true,
                StarterKitState = StarterKitStates.Unused,
            };
        }

        public static void ApplyPaidPlanStub(
            BillingAccount billingAccount,
            string restaurantName
        )
        {
            billingAccount.SubscriptionPlan = ResolveSubscriptionPlan(
                restaurantName,
                isPilot: false
            );
            billingAccount.BillingStatus = BillingStatuses.Active;
            billingAccount.BillingCycle = BillingCycles.Monthly;
        }

        private static string SubscriptionPlanKey(string subscriptionPlan)
        {
            return subscriptionPlan.Trim().ToLowerInvariant() switch
            {
                "pilot" => "pilot",
                "starter" => "starter",
                "growth" => "growth",
                "group" => "group",
                _ => subscriptionPlan.Trim().ToLowerInvariant(),
            };
        }

        private async Task<List<BillingContactPickerItemDto>> LoadEligibleMembersAsync(
            int restaurantId
        )
        {
            var memberIds = await GetEligibleMemberIdsAsync(restaurantId);
            var members = await _context.Users
                .AsNoTracking()
                .Where(user => memberIds.Contains(user.Id))
                .OrderBy(user => user.FullName)
                .ToListAsync();

            if (members.Count == 0)
            {
                var ownerId = await _context.Restaurants
                    .AsNoTracking()
                    .Where(row => row.Id == restaurantId)
                    .Select(row => row.OwnerUserId)
                    .FirstAsync();
                var owner = await _context.Users
                    .AsNoTracking()
                    .FirstAsync(user => user.Id == ownerId);
                members = [owner];
            }

            return members.Select(MapPickerItem).ToList();
        }

        private async Task<HashSet<int>> GetEligibleMemberIdsAsync(int restaurantId)
        {
            var ids = await _context.RestaurantMemberships
                .AsNoTracking()
                .Where(m =>
                    m.RestaurantId == restaurantId
                    && m.Status == MembershipStatus.Active
                )
                .Select(m => m.UserId)
                .ToListAsync();

            if (ids.Count == 0)
            {
                var ownerId = await _context.Restaurants
                    .AsNoTracking()
                    .Where(r => r.Id == restaurantId)
                    .Select(r => r.OwnerUserId)
                    .FirstAsync();
                ids.Add(ownerId);
            }

            return ids.ToHashSet();
        }

        private static BillingContactsSnapshotDto MapBillingContacts(
            Restaurant restaurant,
            BillingAccount billingAccount,
            IReadOnlyList<BillingContactPickerItemDto> eligibleMembers
        )
        {
            return new BillingContactsSnapshotDto
            {
                BillingContactUserId = restaurant.BillingContactUserId,
                BillingEmail = billingAccount.BillingEmail,
                EligibleMembers = eligibleMembers.ToList(),
                LowCreditAlerts = new BillingAlertRoleFlagsDto
                {
                    Owner = billingAccount.LowCreditAlertOwner,
                    Admin = billingAccount.LowCreditAlertAdmin,
                    BillingContact = billingAccount.LowCreditAlertBillingContact,
                },
                PaymentFailureAlerts = new BillingPaymentFailureAlertFlagsDto
                {
                    Owner = billingAccount.PaymentFailureAlertOwner,
                    BillingContact = billingAccount.PaymentFailureAlertBillingContact,
                },
            };
        }

        private static BillingContactPickerItemDto MapPickerItem(User user)
        {
            return new BillingContactPickerItemDto
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email,
            };
        }

        private static string? NormalizeBillingEmail(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value.Trim();
        }

        private static bool IsValidEmail(string value)
        {
            try
            {
                var address = new System.Net.Mail.MailAddress(value);
                return string.Equals(
                    address.Address,
                    value,
                    StringComparison.OrdinalIgnoreCase
                );
            }
            catch
            {
                return false;
            }
        }

        public async Task<(CreditTopUpConfirmDto? Response, int StatusCode, string? ErrorMessage)>
            ConfirmCreditTopUpAsync(
                int userId,
                int restaurantId,
                bool actorCanManage,
                CreditTopUpRequestDto request
            )
        {
            var denial = await ValidateCreditTopUpRequestAsync(
                userId,
                restaurantId,
                actorCanManage,
                request
            );
            if (denial != null)
            {
                return denial.Value;
            }

            var context = await LoadCreditTopUpContextAsync(restaurantId);
            if (context == null)
            {
                return (null, StatusCodes.Status404NotFound, "Restaurant not found.");
            }

            var pack = CreditTopUpPricebook.FindPack(request.Channel, request.Quantity)!;
            return (BuildCreditTopUpConfirm(pack), StatusCodes.Status200OK, null);
        }

        public async Task<(CreditTopUpPayDto? Response, int StatusCode, string? ErrorMessage)>
            PayCreditTopUpAsync(
                int userId,
                int restaurantId,
                bool actorCanManage,
                CreditTopUpRequestDto request
            )
        {
            var denial = await ValidateCreditTopUpRequestAsync(
                userId,
                restaurantId,
                actorCanManage,
                request
            );
            if (denial != null)
            {
                return (
                    null,
                    denial.Value.StatusCode,
                    denial.Value.ErrorMessage
                );
            }

            var context = await LoadCreditTopUpContextAsync(restaurantId);
            if (context == null)
            {
                return (null, StatusCodes.Status404NotFound, "Restaurant not found.");
            }

            var pack = CreditTopUpPricebook.FindPack(request.Channel, request.Quantity)!;
            return (
                new CreditTopUpPayDto
                {
                    RedirectUrl =
                        $"https://checkout.revolut.com/pay/top-up/{restaurantId}/{pack.Channel}/{pack.Quantity}",
                },
                StatusCodes.Status200OK,
                null
            );
        }

        private async Task<(
            CreditTopUpConfirmDto? Response,
            int StatusCode,
            string? ErrorMessage
        )?> ValidateCreditTopUpRequestAsync(
            int userId,
            int restaurantId,
            bool actorCanManage,
            CreditTopUpRequestDto request
        )
        {
            if (!actorCanManage)
            {
                return (null, StatusCodes.Status403Forbidden, null);
            }

            var actorMembership = await _context.RestaurantMemberships
                .AsNoTracking()
                .FirstOrDefaultAsync(row =>
                    row.UserId == userId
                    && row.RestaurantId == restaurantId
                    && row.Status == MembershipStatus.Active
                );
            var actorRole =
                actorMembership?.PermissionRole ?? PermissionRoles.Owner;

            if (!CanBuyCreditTopUp(actorRole))
            {
                return (null, StatusCodes.Status403Forbidden, null);
            }

            var context = await LoadCreditTopUpContextAsync(restaurantId);
            if (context == null)
            {
                return (null, StatusCodes.Status404NotFound, "Restaurant not found.");
            }

            if (context.IsPilot)
            {
                return (
                    null,
                    StatusCodes.Status403Forbidden,
                    "Credit top-ups are unavailable during Pilot."
                );
            }

            if (BillingPlanSnapshotHelper.IsAccountLocked(context.BillingStatus))
            {
                return (
                    null,
                    StatusCodes.Status403Forbidden,
                    BillingPlanSnapshotHelper.LockDenyCode(context.BillingStatus)
                );
            }

            var pack = CreditTopUpPricebook.FindPack(request.Channel, request.Quantity);
            if (pack == null)
            {
                return (
                    null,
                    StatusCodes.Status400BadRequest,
                    "Unknown credit top-up pack."
                );
            }

            if (
                !CreditTopUpPricebook.IsPackVisible(
                    pack,
                    context.SubscriptionPlan,
                    context.AllowSms5000TopUp
                )
            )
            {
                return (
                    null,
                    StatusCodes.Status403Forbidden,
                    "This credit top-up pack is not available on your plan."
                );
            }

            return null;
        }

        private static bool CanBuyCreditTopUp(string permissionRole)
        {
            return permissionRole is PermissionRoles.Owner
                or PermissionRoles.BillingAdmin
                or PermissionRoles.Admin;
        }

        private sealed record CreditTopUpContext(
            bool IsPilot,
            string SubscriptionPlan,
            string BillingStatus,
            bool AllowSms5000TopUp
        );

        private async Task<CreditTopUpContext?> LoadCreditTopUpContextAsync(int restaurantId)
        {
            var restaurant = await _context.Restaurants
                .AsNoTracking()
                .FirstOrDefaultAsync(row => row.Id == restaurantId);

            if (restaurant == null)
            {
                return null;
            }

            var owner = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(row => row.Id == restaurant.OwnerUserId);

            var lifecycle = BillingPlanSnapshotHelper.ResolveLifecycle(
                restaurant.Name,
                owner?.ActivationExpiresAt
            );
            var billingAccount = await LoadRequiredBillingAccountAsync(restaurantId);

            return new CreditTopUpContext(
                lifecycle.IsPilot,
                lifecycle.IsPilot
                    ? "Pilot"
                    : ResolveSubscriptionPlan(restaurant.Name, lifecycle.IsPilot),
                lifecycle.BillingStatus,
                billingAccount.AllowSms5000TopUp
            );
        }

        private static string ResolveSubscriptionPlan(string restaurantName, bool isPilot)
        {
            if (isPilot)
            {
                return "Pilot";
            }

            if (restaurantName.Contains(" Group", StringComparison.Ordinal))
            {
                return "Group";
            }

            if (restaurantName.Contains(" Starter", StringComparison.Ordinal))
            {
                return "Starter";
            }

            return "Growth";
        }

        private static CreditTopUpConfirmDto BuildCreditTopUpConfirm(CreditTopUpPack pack)
        {
            var gross = CreditTopUpPricebook.GrossPounds(pack.NetPounds);
            var vat = gross - pack.NetPounds;

            return new CreditTopUpConfirmDto
            {
                Channel = pack.Channel,
                Quantity = pack.Quantity,
                ChannelLabel = CreditTopUpChannelLabel(pack.Channel),
                NetLabel = CreditTopUpPricebook.FormatPounds(pack.NetPounds),
                GrossLabel = CreditTopUpPricebook.FormatPounds(gross),
                VatLabel = CreditTopUpPricebook.FormatPounds(vat),
            };
        }

        private static string CreditTopUpChannelLabel(string channel)
        {
            return channel switch
            {
                "sms" => "SMS credits",
                "email" => "Email credits",
                "ai" => "AI credits",
                _ => channel,
            };
        }

        public async Task<BillingActivityListDto?> GetActivityAsync(
            int restaurantId,
            int page,
            int pageSize
        )
        {
            var restaurant = await _context.Restaurants
                .AsNoTracking()
                .FirstOrDefaultAsync(row => row.Id == restaurantId);

            if (restaurant == null)
            {
                return null;
            }

            var owner = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(row => row.Id == restaurant.OwnerUserId);

            if (
                await BillingPlanSnapshotHelper.IsPilotRestaurantAsync(
                    _context,
                    restaurantId,
                    owner
                )
            )
            {
                return new BillingActivityListDto
                {
                    Items = [],
                    TotalCount = 0,
                    Page = Math.Max(1, page),
                    PageSize = Math.Max(1, pageSize),
                };
            }

            var allRows = BuildStubBillingActivityRows();
            var safePage = Math.Max(1, page);
            var safePageSize = Math.Max(1, pageSize);
            var skip = (safePage - 1) * safePageSize;

            return new BillingActivityListDto
            {
                Items = allRows.Skip(skip).Take(safePageSize).ToList(),
                TotalCount = allRows.Count,
                Page = safePage,
                PageSize = safePageSize,
            };
        }

        private static List<BillingActivityRowDto> BuildStubBillingActivityRows()
        {
            var now = DateTime.UtcNow;
            return
            [
                new BillingActivityRowDto
                {
                    Id = 1,
                    Kind = BillingActivityKinds.CreditConsumed,
                    OccurredAt = now.AddHours(-2),
                    Channel = "sms",
                    Qty = 212,
                    CampaignName = "Quiet Tuesday Boost",
                    ConsumeSource = "campaign",
                },
                new BillingActivityRowDto
                {
                    Id = 2,
                    Kind = BillingActivityKinds.TopupPurchased,
                    OccurredAt = new DateTime(2026, 7, 18, 9, 15, 0, DateTimeKind.Utc),
                    ActorDisplayName = "James Cole",
                    Channel = "sms",
                    Qty = 1000,
                },
                new BillingActivityRowDto
                {
                    Id = 3,
                    Kind = BillingActivityKinds.InvoicePaid,
                    OccurredAt = new DateTime(2026, 7, 12, 8, 0, 0, DateTimeKind.Utc),
                    InvoiceNo = "TM-2026-000001",
                },
                new BillingActivityRowDto
                {
                    Id = 4,
                    Kind = BillingActivityKinds.SubscriptionRenewed,
                    OccurredAt = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
                    Plan = "Growth",
                },
                new BillingActivityRowDto
                {
                    Id = 5,
                    Kind = BillingActivityKinds.PaymentMethodUpdated,
                    OccurredAt = new DateTime(2026, 6, 20, 14, 30, 0, DateTimeKind.Utc),
                    ActorDisplayName = "James Cole",
                },
                new BillingActivityRowDto
                {
                    Id = 6,
                    Kind = BillingActivityKinds.CreditConsumed,
                    OccurredAt = new DateTime(2026, 6, 15, 11, 0, 0, DateTimeKind.Utc),
                    Channel = "email",
                    Qty = 450,
                    CampaignName = "Summer Launch",
                    ConsumeSource = "campaign",
                },
                new BillingActivityRowDto
                {
                    Id = 7,
                    Kind = BillingActivityKinds.TopupRefunded,
                    OccurredAt = new DateTime(2026, 6, 10, 16, 45, 0, DateTimeKind.Utc),
                    Channel = "sms",
                    Qty = 50,
                },
                new BillingActivityRowDto
                {
                    Id = 8,
                    Kind = BillingActivityKinds.CreditNoteIssued,
                    OccurredAt = new DateTime(2026, 6, 5, 10, 0, 0, DateTimeKind.Utc),
                    CreditNoteNo = "TCN-2026-000001",
                },
                new BillingActivityRowDto
                {
                    Id = 9,
                    Kind = BillingActivityKinds.ManualCreditAdjusted,
                    OccurredAt = new DateTime(2026, 5, 28, 9, 0, 0, DateTimeKind.Utc),
                    Channel = "email",
                    Qty = 100,
                    ManualAdjustDirection = "add",
                },
                new BillingActivityRowDto
                {
                    Id = 10,
                    Kind = BillingActivityKinds.SubscriptionChangeScheduled,
                    OccurredAt = new DateTime(2026, 5, 20, 13, 15, 0, DateTimeKind.Utc),
                    ActorDisplayName = "James Cole",
                    Plan = "Starter",
                    Cadence = "Monthly",
                    ScheduledDateLabel = "15 September 2026",
                },
                new BillingActivityRowDto
                {
                    Id = 11,
                    Kind = BillingActivityKinds.CreditExpired,
                    OccurredAt = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
                    Channel = "sms",
                    Qty = 12,
                },
                new BillingActivityRowDto
                {
                    Id = 12,
                    Kind = BillingActivityKinds.CreditConsumed,
                    OccurredAt = new DateTime(2026, 4, 22, 18, 20, 0, DateTimeKind.Utc),
                    Channel = "sms",
                    Qty = 1,
                    ConsumeSource = "feedback_recovery",
                },
                new BillingActivityRowDto
                {
                    Id = 13,
                    Kind = BillingActivityKinds.SubscriptionCreated,
                    OccurredAt = new DateTime(2026, 4, 1, 8, 0, 0, DateTimeKind.Utc),
                    ActorDisplayName = "James Cole",
                    Plan = "Growth",
                    Cadence = "Monthly",
                },
                new BillingActivityRowDto
                {
                    Id = 14,
                    Kind = BillingActivityKinds.AdditionalLocationAdded,
                    OccurredAt = new DateTime(2026, 3, 15, 12, 0, 0, DateTimeKind.Utc),
                    ActorDisplayName = "James Cole",
                    LocationName = "Soho",
                },
                new BillingActivityRowDto
                {
                    Id = 15,
                    Kind = BillingActivityKinds.SubscriptionUpgraded,
                    OccurredAt = new DateTime(2026, 3, 1, 9, 30, 0, DateTimeKind.Utc),
                    ActorDisplayName = "James Cole",
                    Plan = "Growth",
                },
                new BillingActivityRowDto
                {
                    Id = 16,
                    Kind = BillingActivityKinds.SubscriptionCancelled,
                    OccurredAt = new DateTime(2026, 2, 10, 10, 0, 0, DateTimeKind.Utc),
                    ActorDisplayName = "James Cole",
                    ScheduledDateLabel = "15 March 2026",
                },
                new BillingActivityRowDto
                {
                    Id = 17,
                    Kind = BillingActivityKinds.AdditionalLocationRemoveScheduled,
                    OccurredAt = new DateTime(2026, 2, 1, 11, 0, 0, DateTimeKind.Utc),
                    ActorDisplayName = "James Cole",
                    LocationName = "Camden",
                    ScheduledDateLabel = "1 April 2026",
                },
                new BillingActivityRowDto
                {
                    Id = 18,
                    Kind = BillingActivityKinds.SoftLockEntered,
                    OccurredAt = new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc),
                },
                new BillingActivityRowDto
                {
                    Id = 19,
                    Kind = BillingActivityKinds.DormantEntered,
                    OccurredAt = new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc),
                },
                new BillingActivityRowDto
                {
                    Id = 20,
                    Kind = BillingActivityKinds.ManualCreditAdjusted,
                    OccurredAt = new DateTime(2025, 12, 15, 9, 0, 0, DateTimeKind.Utc),
                    Channel = "ai",
                    Qty = 5,
                    ManualAdjustDirection = "remove",
                },
                new BillingActivityRowDto
                {
                    Id = 21,
                    Kind = BillingActivityKinds.CreditConsumed,
                    OccurredAt = new DateTime(2025, 12, 1, 15, 0, 0, DateTimeKind.Utc),
                    Channel = "email",
                    Qty = 1,
                    CampaignName = "Winter Promo",
                    ConsumeSource = "campaign",
                },
                new BillingActivityRowDto
                {
                    Id = 22,
                    Kind = BillingActivityKinds.InvoicePaid,
                    OccurredAt = new DateTime(2025, 11, 12, 8, 0, 0, DateTimeKind.Utc),
                    InvoiceNo = "TM-2025-000012",
                },
                new BillingActivityRowDto
                {
                    Id = 23,
                    Kind = BillingActivityKinds.TopupPurchased,
                    OccurredAt = new DateTime(2025, 10, 5, 14, 0, 0, DateTimeKind.Utc),
                    ActorDisplayName = "James Cole",
                    Channel = "ai",
                    Qty = 20,
                },
                new BillingActivityRowDto
                {
                    Id = 24,
                    Kind = BillingActivityKinds.SubscriptionChangeScheduled,
                    OccurredAt = new DateTime(2025, 9, 1, 10, 0, 0, DateTimeKind.Utc),
                    ActorDisplayName = "James Cole",
                    Cadence = "Annual",
                    ScheduledDateLabel = "1 October 2025",
                },
                new BillingActivityRowDto
                {
                    Id = 25,
                    Kind = BillingActivityKinds.CreditExpired,
                    OccurredAt = new DateTime(2025, 8, 1, 0, 0, 0, DateTimeKind.Utc),
                    Channel = "email",
                    Qty = 200,
                },
            ];
        }

    }
}
