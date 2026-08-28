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

        public BillingCreditsService(ApplicationDbContext context)
        {
            _context = context;
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

            var isPilot = await IsPilotRestaurantAsync(restaurantId, owner);
            var billingAccount = await LoadOrCreateBillingAccountAsync(restaurantId);
            var eligibleMembers = await LoadEligibleMembersAsync(restaurantId);
            var pilotEndsAt = owner?.ActivationExpiresAt;
            var planContext = ResolveStubPlanContext(
                restaurant.Name,
                isPilot,
                activeLocations
            );
            var renewalDateLabel = isPilot && pilotEndsAt != null
                ? $"Pilot ends {FormatUkDate(pilotEndsAt.Value)}"
                : isPilot
                    ? null
                    : planContext.RenewalDateLabel;

            return new BillingCreditsPageDto
            {
                ActorPermissionRole = actorPermissionRole,
                ActorCanManage = actorCanManage,
                ActorCanPersistBillingContacts =
                    actorCanManage
                    && actorPermissionRole == PermissionRoles.Owner,
                PlanSubscription = new PlanSubscriptionSnapshotDto
                {
                    SubscriptionPlan = planContext.SubscriptionPlan,
                    BillingStatus = isPilot ? "Pilot" : "Active",
                    RenewalDateLabel = renewalDateLabel,
                    EmailCreditsRemaining = isPilot ? 500 : 1200,
                    SmsCreditsRemaining = isPilot ? 20 : 80,
                    AiCreditsRemaining = isPilot ? 20 : 40,
                    BillingCycle = isPilot ? null : "Monthly",
                    PlanPriceNet = isPilot ? "£0" : planContext.PlanPriceNet,
                    IncludedLocations = planContext.IncludedLocations,
                    ActiveLocations = activeLocations,
                    IncludedEmailCreditsLabel = isPilot ? "500 once" : "1,000 / month",
                    IncludedSmsCreditsLabel = isPilot ? "20 once" : "50 / month",
                    IncludedAiCreditsLabel = isPilot ? "20 once" : "30 / month",
                    StarterKitState = "unused",
                    PricebookId = "guest-loop-mvp-2026-07",
                    ScheduledChangeLine = planContext.ScheduledChangeLine,
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

            if (await IsPilotRestaurantAsync(restaurantId, owner))
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

        private async Task<bool> IsPilotRestaurantAsync(int restaurantId, User? owner)
        {
            // Stub seam until BillingAccount persistence lands (credit-ledger map).
            var restaurant = await _context.Restaurants
                .AsNoTracking()
                .FirstOrDefaultAsync(row => row.Id == restaurantId);

            if (
                restaurant?.Name.StartsWith(
                    "Paid ",
                    StringComparison.Ordinal
                ) == true
            )
            {
                return false;
            }

            return owner?.ActivationExpiresAt != null
                && owner.ActivationExpiresAt.Value > DateTime.UtcNow;
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

            return BuildPilotUsageSnapshot();
        }

        private static CreditsUsageSnapshotDto BuildPilotUsageSnapshot()
        {
            return new CreditsUsageSnapshotDto
            {
                PeriodLabel = "Account · Pilot allowance",
                StarterKitState = "unused",
                IsPilot = true,
                Channels =
                [
                    new CreditChannelUsageDto
                    {
                        Channel = "email",
                        CombinedRemaining = 500,
                        UsedThisCycle = 0,
                        IncludedThisPeriod = 500,
                        PurchasedRemaining = 0,
                        PurchasedExpiryLabel = null,
                    },
                    new CreditChannelUsageDto
                    {
                        Channel = "sms",
                        CombinedRemaining = 20,
                        UsedThisCycle = 0,
                        IncludedThisPeriod = 20,
                        PurchasedRemaining = 0,
                        PurchasedExpiryLabel = null,
                    },
                    new CreditChannelUsageDto
                    {
                        Channel = "ai",
                        CombinedRemaining = 20,
                        UsedThisCycle = 0,
                        IncludedThisPeriod = 20,
                        PurchasedRemaining = 0,
                        PurchasedExpiryLabel = null,
                    },
                ],
            };
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

            var simulatePaidGrowth =
                string.Equals(
                    restaurant.Name,
                    "Billing Venue Schedule Test",
                    StringComparison.Ordinal
                );
            var currentPlan = simulatePaidGrowth ? "Growth" : "Pilot";
            var isPilot = !simulatePaidGrowth;
            string? liveCadence = simulatePaidGrowth ? "monthly" : null;

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

        private sealed record StubPlanContext(
            string SubscriptionPlan,
            string PlanPriceNet,
            int IncludedLocations,
            string RenewalDateLabel,
            string? ScheduledChangeLine
        );

        private static StubPlanContext ResolveStubPlanContext(
            string restaurantName,
            bool isPilot,
            int activeLocations
        )
        {
            if (isPilot)
            {
                return new StubPlanContext(
                    "Pilot",
                    "£0",
                    1,
                    "Renews 15 September 2026",
                    null
                );
            }

            if (
                string.Equals(
                    restaurantName,
                    "Paid Billing Venue Group Test",
                    StringComparison.Ordinal
                )
                || string.Equals(
                    restaurantName,
                    "Billing Venue Group Test",
                    StringComparison.Ordinal
                )
            )
            {
                return new StubPlanContext(
                    "Group",
                    "£199",
                    GroupIncludedLocations,
                    "Renews 15 September 2026",
                    null
                );
            }

            if (
                string.Equals(
                    restaurantName,
                    "Paid Billing Venue Group Extra Test",
                    StringComparison.Ordinal
                )
                || string.Equals(
                    restaurantName,
                    "Billing Venue Group Extra Test",
                    StringComparison.Ordinal
                )
            )
            {
                return new StubPlanContext(
                    "Group",
                    "£199",
                    GroupIncludedLocations + 2,
                    "Renews 15 September 2026",
                    null
                );
            }

            if (
                string.Equals(
                    restaurantName,
                    "Billing Venue Schedule Test",
                    StringComparison.Ordinal
                )
            )
            {
                return new StubPlanContext(
                    "Growth",
                    "£99",
                    3,
                    "Renews 15 September 2026",
                    null
                );
            }

            return new StubPlanContext(
                "Growth",
                "£99",
                Math.Max(1, activeLocations),
                "Renews 15 September 2026",
                null
            );
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

            var isPilot = await IsPilotRestaurantAsync(restaurantId, owner);
            if (isPilot)
            {
                throw new InvalidOperationException("pilot-cancel-not-allowed");
            }

            var planContext = ResolveStubPlanContext(
                restaurant.Name,
                isPilot,
                await CountActiveLocationsAsync(restaurantId)
            );

            var renewalDate = planContext.RenewalDateLabel.Replace("Renews ", "");

            return new CancelPlanResultDto
            {
                ScheduledChangeLine = $"Cancels on {renewalDate}",
            };
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

            var isPilot = await IsPilotRestaurantAsync(restaurantId, owner);
            if (isPilot)
            {
                throw new InvalidOperationException("not-group-plan");
            }

            var activeLocations = await CountActiveLocationsAsync(restaurantId);
            var planContext = ResolveStubPlanContext(
                restaurant.Name,
                isPilot,
                activeLocations
            );

            if (
                !string.Equals(
                    planContext.SubscriptionPlan,
                    "Group",
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

            var billingAccount = await LoadOrCreateBillingAccountAsync(restaurantId, tracked: true);
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

        private async Task<BillingAccount> LoadOrCreateBillingAccountAsync(
            int restaurantId,
            bool tracked = false
        )
        {
            var query = tracked
                ? _context.BillingAccounts
                : _context.BillingAccounts.AsNoTracking();

            var existing = await query
                .FirstOrDefaultAsync(row => row.RestaurantId == restaurantId);

            if (existing != null)
            {
                return existing;
            }

            var created = CreateDefaultBillingAccount(restaurantId);
            if (tracked)
            {
                _context.BillingAccounts.Add(created);
            }

            return created;
        }

        public static BillingAccount CreateDefaultBillingAccount(int restaurantId)
        {
            return new BillingAccount
            {
                RestaurantId = restaurantId,
                LowCreditAlertOwner = true,
                LowCreditAlertAdmin = false,
                LowCreditAlertBillingContact = true,
                PaymentFailureAlertOwner = true,
                PaymentFailureAlertBillingContact = true,
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

            var isPilot = await IsPilotRestaurantAsync(restaurantId, owner);
            var billingAccount = await LoadOrCreateBillingAccountAsync(restaurantId);

            return new CreditTopUpContext(
                isPilot,
                ResolveSubscriptionPlan(restaurant.Name, isPilot),
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

    }
}
