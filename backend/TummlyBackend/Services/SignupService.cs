using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Billing.CustomPlan;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Admin;
using TummlyBackend.DTOs.Signup;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public class SignupService : ISignupService
    {
        public const string ChoosePlanMessage = "Choose a plan to continue.";

        private static readonly TimeSpan SessionLoginWindow = TimeSpan.FromHours(24);

        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IProvisioningService _provisioningService;
        private readonly ICustomPlanQuoteService _customPlanQuote;
        private readonly IJwtService _jwt;

        public SignupService(
            ApplicationDbContext context,
            IEmailService emailService,
            IProvisioningService provisioningService,
            ICustomPlanQuoteService customPlanQuote,
            IJwtService jwt
        )
        {
            _context = context;
            _emailService = emailService;
            _provisioningService = provisioningService;
            _customPlanQuote = customPlanQuote;
            _jwt = jwt;
        }

        public async Task<SignupSessionResponse> StartAsync(StartSignupDto dto)
        {
            var email = dto.Email.Trim().ToLower();

            if (!dto.TermsAccepted)
            {
                throw new Exception("Terms must be accepted.");
            }

            var existingUser = await _context.Users.FirstOrDefaultAsync(x =>
                x.Email == email
            );

            if (existingUser != null)
            {
                throw new Exception("Email already in use.");
            }

            var pending = await _context.PendingSignups.FirstOrDefaultAsync(x =>
                x.Email == email
            );

            var isExistingEmailPending =
                pending != null
                && pending.Status == PendingSignupStatuses.EmailPending;

            if (pending != null)
            {
                if (pending.Status == PendingSignupStatuses.Complete)
                {
                    throw new Exception("Email already in use.");
                }

                // Mid-flow: resume existing session (no duplicate row, no OTP).
                if (
                    pending.Status
                        is PendingSignupStatuses.Verified
                            or PendingSignupStatuses.OnboardingComplete
                            or PendingSignupStatuses.AwaitingPayment
                            or PendingSignupStatuses.Provisioning
                )
                {
                    return ToSessionResponse(pending);
                }

                if (pending.Status == PendingSignupStatuses.Abandoned)
                {
                    pending.Status = PendingSignupStatuses.EmailPending;
                    pending.OtpResendCount = 0;
                    pending.LastOtpSentAt = null;
                    pending.EmailVerifiedAt = null;
                    pending.SessionToken = Guid.NewGuid();
                }

                pending.TermsAccepted = dto.TermsAccepted;
                pending.UpdatedAtUtc = DateTime.UtcNow;
            }
            else
            {
                pending = new PendingSignup
                {
                    Id = Guid.NewGuid(),
                    SessionToken = Guid.NewGuid(),
                    Email = email,
                    Status = PendingSignupStatuses.EmailPending,
                    OtpResendCount = 0,
                    TermsAccepted = dto.TermsAccepted,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow,
                };

                await _context.PendingSignups.AddAsync(pending);
            }

            // Re-POST Start on EmailPending uses the same cooldown / resend cap as Resend.
            if (isExistingEmailPending)
            {
                await EnforceAndSendOtpAsync(pending, countAsResend: true);
            }
            else
            {
                await EnforceAndSendOtpAsync(pending, countAsResend: false);
            }

            return ToSessionResponse(pending);
        }

        public async Task<SignupSessionResponse> VerifyOtpAsync(
            VerifySignupOtpDto dto
        )
        {
            var email = dto.Email.Trim().ToLower();
            var otpCode = dto.OtpCode.Trim();

            var otpRecord = await _context.OtpVerifications.FirstOrDefaultAsync(
                x =>
                    x.Email == email
                    && x.OtpCode == otpCode
                    && x.IsUsed == false
            );

            if (otpRecord == null || otpRecord.ExpiresAt < DateTime.UtcNow)
            {
                throw new Exception("Invalid or expired OTP.");
            }

            var pending = await _context.PendingSignups.FirstOrDefaultAsync(x =>
                x.Email == email
                && x.Status == PendingSignupStatuses.EmailPending
            );

            if (pending == null)
            {
                throw new Exception("No pending signup found.");
            }

            otpRecord.IsUsed = true;
            pending.Status = PendingSignupStatuses.Verified;
            pending.EmailVerifiedAt = DateTime.UtcNow;
            pending.UpdatedAtUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return ToSessionResponse(pending);
        }

        public async Task ResendOtpAsync(string email)
        {
            email = email.Trim().ToLower();

            var pending = await _context.PendingSignups.FirstOrDefaultAsync(x =>
                x.Email == email
                && x.Status == PendingSignupStatuses.EmailPending
            );

            if (pending == null)
            {
                throw new Exception("No pending signup found.");
            }

            await EnforceAndSendOtpAsync(pending, countAsResend: true);
        }

        public async Task<SignupSessionResponse> SaveOnboardingAsync(
            Guid sessionToken,
            SaveSignupOnboardingDto dto
        )
        {
            var pending = await FindBySessionTokenAsync(sessionToken);

            if (pending.Status == PendingSignupStatuses.Complete)
            {
                return ToSessionResponse(pending);
            }

            if (
                pending.Status
                is not (
                    PendingSignupStatuses.Verified
                    or PendingSignupStatuses.OnboardingComplete
                )
            )
            {
                throw new Exception(
                    "Email must be verified before onboarding."
                );
            }

            var isSocial = !string.IsNullOrWhiteSpace(pending.AuthProvider);
            ValidateOnboardingPayload(dto, isSocial);

            if (isSocial)
            {
                pending.PasswordHash = null;
            }
            else
            {
                pending.PasswordHash = BCrypt.Net.BCrypt.HashPassword(
                    dto.Password!
                );
            }

            pending.FullName = dto.FullName.Trim();
            // One Location before payment. Multi promotion runs when more Locations
            // become Active/Paused (RestaurantAccountTypePromotion) — same as Group.
            pending.AccountType = "Single";
            pending.OnboardingJson = JsonSerializer.Serialize(
                new SignupOnboardingPayload
                {
                    FullName = pending.FullName,
                    GroupName = dto.GroupName.Trim(),
                    BusinessCategory = dto.BusinessCategory.Trim(),
                    PrimaryPhone = NormalizeOptional(dto.PrimaryPhone),
                    BusinessLink = NormalizeOptional(dto.BusinessLink),
                    Locations = dto.Locations,
                }
            );
            await ApplyChosenPlanAsync(pending, dto);

            pending.Status = PendingSignupStatuses.Provisioning;
            pending.UpdatedAtUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _provisioningService.ProvisionFromPendingAsync(pending.Id);

            await _context.Entry(pending).ReloadAsync();

            return ToSessionResponse(pending);
        }

        public async Task<SignupResumeResponse> GetBySessionAsync(
            Guid sessionToken
        )
        {
            var pending = await FindBySessionTokenAsync(sessionToken);

            SignupOnboardingPayload? profile = null;
            if (!string.IsNullOrWhiteSpace(pending.OnboardingJson))
            {
                profile = JsonSerializer.Deserialize<SignupOnboardingPayload>(
                    pending.OnboardingJson
                );
            }

            return new SignupResumeResponse
            {
                SessionToken = pending.SessionToken,
                Email = pending.Email,
                Status = pending.Status,
                LastStepHint = ResolveLastStepHint(pending),
                FullName = pending.FullName ?? profile?.FullName,
                AccountType = pending.AccountType,
                GroupName = profile?.GroupName,
                BusinessCategory = profile?.BusinessCategory,
                PrimaryPhone = profile?.PrimaryPhone,
                BusinessLink = profile?.BusinessLink,
                OnboardingJson = pending.OnboardingJson,
                PasswordHash = null,
                AuthProvider = pending.AuthProvider,
            };
        }

        public async Task RetryProvisionAsync(Guid sessionToken)
        {
            var pending = await FindBySessionTokenAsync(sessionToken);

            if (pending.Status == PendingSignupStatuses.Complete)
            {
                // Provisioned, but the paid checkout did not open: open it again.
                if (
                    IsPaidChosenPlan(pending.ChosenPlan)
                    && await ResolvePaidCheckoutFailureAsync(pending) != null
                )
                {
                    await _provisioningService.RetryPaidSignupCheckoutAsync(
                        pending.Id
                    );
                }

                return;
            }

            if (
                pending.Status
                is not (
                    PendingSignupStatuses.OnboardingComplete
                    or PendingSignupStatuses.Provisioning
                )
            )
            {
                throw new Exception(
                    "Account setup is not ready to provision."
                );
            }

            if (string.IsNullOrWhiteSpace(pending.ChosenPlan))
            {
                throw new Exception(ChoosePlanMessage);
            }
            pending.Status = PendingSignupStatuses.Provisioning;
            pending.UpdatedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _provisioningService.ProvisionFromPendingAsync(pending.Id);
        }

        public async Task<object> SessionLoginAsync(
            Guid sessionToken,
            bool continueOnFree = false
        )
        {
            var pending = await FindBySessionTokenAsync(sessionToken);

            if (pending.Status != PendingSignupStatuses.Complete)
            {
                throw new SignupConflictException(
                    "payment_pending",
                    "Payment or provisioning is not complete yet."
                );
            }

            if (pending.SessionLoginConsumedAtUtc != null)
            {
                throw new SignupConflictException(
                    "session_login_used",
                    "This signup login link was already used."
                );
            }

            var completedAt = pending.UpdatedAtUtc;
            if (DateTime.UtcNow - completedAt > SessionLoginWindow)
            {
                throw new Exception("Signup session login has expired.");
            }

            var user = await _context.Users.FirstOrDefaultAsync(row =>
                row.Email == pending.Email
            );
            if (user == null)
            {
                throw new SignupConflictException(
                    "payment_pending",
                    "Payment or provisioning is not complete yet."
                );
            }

            if (user.SelectedRestaurantId is not int restaurantId || restaurantId == 0)
            {
                throw new SignupConflictException(
                    "payment_pending",
                    "Payment or provisioning is not complete yet."
                );
            }

            var billing = await _context.BillingAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(row => row.RestaurantId == restaurantId);
            if (
                billing == null
                || !(
                    IsSessionLoginBillingReady(billing)
                    || (continueOnFree && IsFreeAfterFailedCheckout(billing))
                )
            )
            {
                throw new SignupConflictException(
                    "payment_pending",
                    "Payment or provisioning is not complete yet."
                );
            }

            await ClaimSessionLoginAsync(pending);

            var owner = user;
            if (user.SelectedRestaurantId is int rid)
            {
                var ownerId = await _context.Restaurants
                    .AsNoTracking()
                    .Where(row => row.Id == rid)
                    .Select(row => row.OwnerUserId)
                    .FirstOrDefaultAsync();
                if (ownerId != 0 && ownerId != user.Id)
                {
                    owner =
                        await _context.Users.FirstOrDefaultAsync(row =>
                            row.Id == ownerId
                        ) ?? user;
                }
            }

            var session = await IssueSessionAsync(user, owner);
            await _context.SaveChangesAsync();
            return session;
        }

        /// <summary>
        /// One-time claim. On SQL Server a conditional update makes two parallel
        /// calls race-safe: only the first one sets the column.
        /// </summary>
        private async Task ClaimSessionLoginAsync(PendingSignup pending)
        {
            var now = DateTime.UtcNow;
            if (_context.Database.IsRelational())
            {
                var updated = await _context.PendingSignups
                    .Where(row =>
                        row.Id == pending.Id
                        && row.SessionLoginConsumedAtUtc == null
                    )
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(row => row.SessionLoginConsumedAtUtc, now)
                        .SetProperty(row => row.UpdatedAtUtc, now)
                    );
                if (updated == 0)
                {
                    throw new SignupConflictException(
                        "session_login_used",
                        "This signup login link was already used."
                    );
                }

                pending.SessionLoginConsumedAtUtc = now;
                pending.UpdatedAtUtc = now;
                return;
            }

            pending.SessionLoginConsumedAtUtc = now;
            pending.UpdatedAtUtc = now;
        }

        private static bool IsFreeAfterFailedCheckout(BillingAccount billing) =>
            string.Equals(
                billing.SubscriptionPlan,
                BillingSubscriptionPlans.Free,
                StringComparison.Ordinal
            )
            && !string.IsNullOrWhiteSpace(billing.PendingPaymentFailureCode);

        private static bool IsSessionLoginBillingReady(BillingAccount billing)
        {
            if (
                string.Equals(
                    billing.SubscriptionPlan,
                    BillingSubscriptionPlans.Pilot,
                    StringComparison.Ordinal
                )
                && string.Equals(
                    billing.BillingStatus,
                    BillingStatuses.Pilot,
                    StringComparison.Ordinal
                )
            )
            {
                return true;
            }

            return string.Equals(
                    billing.BillingStatus,
                    BillingStatuses.Active,
                    StringComparison.Ordinal
                )
                && !string.Equals(
                    billing.SubscriptionPlan,
                    BillingSubscriptionPlans.Free,
                    StringComparison.Ordinal
                );
        }

        private async Task<object> IssueSessionAsync(User user, User owner)
        {
            var refresh = await RefreshTokenHelper.IssueAsync(_context, user.Id);
            var workspaceCount = await _context.RestaurantMemberships.CountAsync(
                row =>
                    row.UserId == user.Id && row.Status == MembershipStatus.Active
            );
            var token = _jwt.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );
            var accountType = user.AccountType;
            if (user.SelectedRestaurantId is int restaurantId)
            {
                var restaurantAccountType = await _context.Restaurants
                    .AsNoTracking()
                    .Where(row => row.Id == restaurantId)
                    .Select(row => row.AccountType)
                    .FirstOrDefaultAsync();
                if (!string.IsNullOrWhiteSpace(restaurantAccountType))
                {
                    accountType = restaurantAccountType;
                }
            }

            return new
            {
                token,
                refreshToken = refresh,
                accountType,
                workspaceCount,
                restaurantId = user.SelectedRestaurantId,
                selectedLocationId = user.SelectedLocationId,
                activationRequired = false,
                ownerActivation = OwnerActivationStatus(owner),
            };
        }

        private static string OwnerActivationStatus(User owner)
        {
            var subject = ActivationSubject.FromUser(owner);
            if (ActivationState.RequiresActivation(subject))
            {
                return "pending";
            }

            if (ActivationState.IsActivationExpired(subject))
            {
                return "expired";
            }

            return "ok";
        }

        public async Task<SignupProvisioningStatusResponse> GetProvisioningStatusAsync(
            Guid sessionToken
        )
        {
            var pending = await FindBySessionTokenAsync(sessionToken);
            var paymentRedirectUrl = await ResolveOpenPaymentRedirectAsync(
                pending
            );

            var status = pending.Status;
            string? paymentFailureCode = null;
            if (
                status == PendingSignupStatuses.Complete
                && IsPaidChosenPlan(pending.ChosenPlan)
            )
            {
                if (paymentRedirectUrl != null)
                {
                    status = PendingSignupStatuses.AwaitingPayment;
                }
                else
                {
                    paymentFailureCode = await ResolvePaidCheckoutFailureAsync(
                        pending
                    );
                    if (paymentFailureCode != null)
                    {
                        status = PendingSignupStatuses.PaymentFailed;
                    }
                }
            }

            return new SignupProvisioningStatusResponse
            {
                Status = status,
                Ready = pending.Status == PendingSignupStatuses.Complete,
                PaymentRedirectUrl = paymentRedirectUrl,
                PaymentFailureCode = paymentFailureCode,
            };
        }

        /// <summary>
        /// Paid signup still on Free with a recorded checkout failure and no
        /// open pay intent → the failure code; otherwise null.
        /// </summary>
        private async Task<string?> ResolvePaidCheckoutFailureAsync(
            PendingSignup pending
        )
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(row => row.Email == pending.Email);
            if (user?.SelectedRestaurantId is not int restaurantId || restaurantId == 0)
            {
                return null;
            }

            var billing = await _context.BillingAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(row => row.RestaurantId == restaurantId);
            if (
                billing == null
                || !string.Equals(
                    billing.SubscriptionPlan,
                    BillingSubscriptionPlans.Free,
                    StringComparison.Ordinal
                )
                || string.IsNullOrWhiteSpace(billing.PendingPaymentFailureCode)
            )
            {
                return null;
            }

            return billing.PendingPaymentFailureCode;
        }

        public static readonly TimeSpan PaidSignupPaySessionTtl = TimeSpan.FromHours(24);

        private async Task<string?> ResolveOpenPaymentRedirectAsync(
            PendingSignup pending
        )
        {
            if (!IsPaidChosenPlan(pending.ChosenPlan))
            {
                return null;
            }

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(row => row.Email == pending.Email);
            if (user?.SelectedRestaurantId is not int restaurantId || restaurantId == 0)
            {
                return null;
            }

            if (
                string.Equals(
                    pending.ChosenPlan,
                    BillingSubscriptionPlans.Custom,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                var customOpen = await _context.RevolutOrderIntents
                    .OrderByDescending(row => row.CreatedAtUtc)
                    .FirstOrDefaultAsync(row =>
                        row.RestaurantId == restaurantId
                        && row.Purpose == RevolutOrderIntentPurposes.CustomPlanCycle
                        && row.IsOpen
                    );
                if (customOpen == null)
                {
                    return null;
                }

                if (DateTime.UtcNow - customOpen.CreatedAtUtc >= PaidSignupPaySessionTtl)
                {
                    customOpen.IsOpen = false;
                    await _context.SaveChangesAsync();
                    return null;
                }

                return string.IsNullOrWhiteSpace(customOpen.CheckoutUrl)
                    ? null
                    : customOpen.CheckoutUrl;
            }

            var open = await _context.RevolutPendingPaySessions
                .OrderByDescending(row => row.CreatedAtUtc)
                .FirstOrDefaultAsync(row =>
                    row.RestaurantId == restaurantId && row.IsOpen
                );
            if (open == null)
            {
                return null;
            }

            if (DateTime.UtcNow - open.CreatedAtUtc >= PaidSignupPaySessionTtl)
            {
                open.IsOpen = false;
                await _context.SaveChangesAsync();
                return null;
            }

            return string.IsNullOrWhiteSpace(open.CheckoutUrl)
                ? null
                : open.CheckoutUrl;
        }

        private static bool IsPaidChosenPlan(string? chosenPlan)
        {
            if (string.IsNullOrWhiteSpace(chosenPlan))
            {
                return false;
            }

            return !string.Equals(
                    chosenPlan,
                    BillingSubscriptionPlans.Free,
                    StringComparison.OrdinalIgnoreCase
                )
                && !string.Equals(
                    chosenPlan,
                    BillingSubscriptionPlans.Pilot,
                    StringComparison.OrdinalIgnoreCase
                );
        }

        private async Task<PendingSignup> FindBySessionTokenAsync(
            Guid sessionToken
        )
        {
            var pending = await _context.PendingSignups.FirstOrDefaultAsync(x =>
                x.SessionToken == sessionToken
            );

            if (pending == null)
            {
                throw new Exception("No pending signup found.");
            }

            return pending;
        }

        private static void ValidateOnboardingPayload(
            SaveSignupOnboardingDto dto,
            bool isSocial
        )
        {
            if (!isSocial)
            {
                if (dto.Password != dto.ConfirmPassword)
                {
                    throw new Exception("Passwords do not match.");
                }

                if (
                    string.IsNullOrWhiteSpace(dto.Password)
                    || dto.Password.Length < 8
                )
                {
                    throw new Exception(
                        "Password must be at least 8 characters."
                    );
                }
            }

            if (string.IsNullOrWhiteSpace(dto.FullName))
            {
                throw new Exception("Full name is required.");
            }

            if (string.IsNullOrWhiteSpace(dto.GroupName))
            {
                throw new Exception("Group name is required.");
            }

            if (string.IsNullOrWhiteSpace(dto.BusinessCategory))
            {
                throw new Exception("Business category is required.");
            }

            if (dto.Locations == null || dto.Locations.Count != 1)
            {
                throw new Exception("Exactly one location is required.");
            }
        }

        private async Task ApplyChosenPlanAsync(
            PendingSignup pending,
            SaveSignupOnboardingDto dto
        )
        {
            var chosen = ResolveChosenPlan(dto.ChosenPlan);
            var cadence = ResolveChosenCadence(chosen, dto.ChosenCadence);

            if (
                !string.IsNullOrWhiteSpace(dto.CustomPlanConfigJson)
                && IsPaidOrCustomPlan(chosen)
            )
            {
                var decoded = DecodeCustomPlanConfigJson(dto.CustomPlanConfigJson);
                var parsed = CustomPlanConfig.TryParse(decoded);
                if (parsed != null && parsed.IsV2)
                {
                    var quote = await _customPlanQuote.QuoteAsync(
                        new CustomPlanQuoteRequest
                        {
                            Cadence = string.IsNullOrWhiteSpace(parsed.Cadence)
                                ? cadence
                                : parsed.Cadence,
                            BasePlan = parsed.BasePlan,
                            Locations = parsed.Locations
                                ?? parsed.Quantities.Locations,
                            Packs = parsed.Packs?.ToDictionary(
                                kv => kv.Key,
                                kv => (decimal)kv.Value,
                                StringComparer.Ordinal
                            ),
                        }
                    );
                    // A hash from the quote the guest saw must still match.
                    // A missing hash (saved before deploy) re-quotes and saves.
                    if (
                        !string.IsNullOrWhiteSpace(parsed.QuoteHash)
                        && !string.Equals(
                            parsed.QuoteHash,
                            quote.QuoteHash,
                            StringComparison.Ordinal
                        )
                    )
                    {
                        throw new CustomPlanQuoteException(
                            "quote_changed",
                            "Your quote has changed. Review the updated total before continuing."
                        );
                    }

                    pending.ChosenPlan = quote.StoredAsPlan;
                    pending.ChosenCadence = quote.Cadence;
                    pending.CustomPlanConfigJson = string.Equals(
                        quote.StoredAsPlan,
                        BillingSubscriptionPlans.Custom,
                        StringComparison.Ordinal
                    )
                        ? quote.ToConfig().ToJson()
                        : null;
                    return;
                }

                if (
                    string.Equals(
                        chosen,
                        BillingSubscriptionPlans.Custom,
                        StringComparison.Ordinal
                    )
                )
                {
                    throw new Exception(
                        "Custom plan config must be version 2 (base plan + packs)."
                    );
                }
            }

            if (
                string.Equals(
                    chosen,
                    BillingSubscriptionPlans.Custom,
                    StringComparison.Ordinal
                )
            )
            {
                throw new Exception("Custom plan config is required.");
            }

            pending.ChosenPlan = chosen;
            pending.ChosenCadence = cadence;
            pending.CustomPlanConfigJson = null;
        }

        private static bool IsPaidOrCustomPlan(string plan) =>
            string.Equals(plan, BillingSubscriptionPlans.Starter, StringComparison.Ordinal)
            || string.Equals(plan, BillingSubscriptionPlans.Growth, StringComparison.Ordinal)
            || string.Equals(plan, BillingSubscriptionPlans.Group, StringComparison.Ordinal)
            || string.Equals(plan, BillingSubscriptionPlans.Custom, StringComparison.Ordinal);

        private static string ResolveChosenPlan(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                throw new Exception(ChoosePlanMessage);
            }

            return raw.Trim().ToLowerInvariant() switch
            {
                "free" => throw new Exception(ChoosePlanMessage),
                "pilot" => BillingSubscriptionPlans.Pilot,
                "starter" => BillingSubscriptionPlans.Starter,
                "growth" => BillingSubscriptionPlans.Growth,
                "group" => BillingSubscriptionPlans.Group,
                "custom" => BillingSubscriptionPlans.Custom,
                _ => throw new Exception("Invalid plan selection."),
            };
        }

        private static string DecodeCustomPlanConfigJson(string raw)
        {
            var trimmed = raw.Trim();
            if (trimmed.StartsWith('{'))
            {
                return trimmed;
            }

            try
            {
                var bytes = Convert.FromBase64String(trimmed);
                return System.Text.Encoding.UTF8.GetString(bytes);
            }
            catch (FormatException)
            {
                return trimmed;
            }
        }

        private static string ResolveChosenCadence(string plan, string? raw)
        {
            if (
                string.Equals(
                    plan,
                    BillingSubscriptionPlans.Free,
                    StringComparison.Ordinal
                )
                || string.Equals(
                    plan,
                    BillingSubscriptionPlans.Pilot,
                    StringComparison.Ordinal
                )
            )
            {
                return "monthly";
            }

            if (
                string.Equals(
                    raw?.Trim(),
                    "annual",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return "annual";
            }

            return "monthly";
        }

        private static string ResolveLastStepHint(PendingSignup pending) =>
            pending.Status switch
            {
                PendingSignupStatuses.EmailPending => "verify",
                PendingSignupStatuses.Verified => !string.IsNullOrWhiteSpace(
                    pending.AuthProvider
                )
                    ? "account"
                    : string.IsNullOrWhiteSpace(pending.PasswordHash)
                        ? "create-password"
                        : "restaurant",
                PendingSignupStatuses.OnboardingComplete => "provisioning",
                PendingSignupStatuses.AwaitingPayment => "payment",
                PendingSignupStatuses.Provisioning => "provisioning",
                PendingSignupStatuses.Complete => "complete",
                PendingSignupStatuses.Abandoned => "restart",
                _ => "restart",
            };

        private static string? NormalizeOptional(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private async Task EnforceAndSendOtpAsync(
            PendingSignup pending,
            bool countAsResend
        )
        {
            var email = pending.Email;

            if (countAsResend)
            {
                if (pending.LastOtpSentAt == null)
                {
                    pending.LastOtpSentAt = DateTime.UtcNow.AddMinutes(-2);
                }

                if (pending.LastOtpSentAt.Value.AddSeconds(60) > DateTime.UtcNow)
                {
                    throw new Exception("Please wait before resending OTP.");
                }

                if (pending.OtpResendCount >= 5)
                {
                    pending.Status = PendingSignupStatuses.Abandoned;
                    pending.UpdatedAtUtc = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    throw new Exception("OTP resend limit reached.");
                }
            }

            await ReplaceUnusedOtpsAsync(email);

            var otpCode = GenerateOtp.CreateOtp();

            await _context.OtpVerifications.AddAsync(
                new OtpVerification
                {
                    Email = email,
                    OtpCode = otpCode,
                    IsUsed = false,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                    CreatedAt = DateTime.UtcNow,
                }
            );

            if (countAsResend)
            {
                pending.OtpResendCount += 1;
            }

            pending.LastOtpSentAt = DateTime.UtcNow;
            pending.UpdatedAtUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _emailService.SendOtpEmailAsync(email, otpCode);
        }

        private async Task ReplaceUnusedOtpsAsync(string email)
        {
            var oldOtps = await _context
                .OtpVerifications.Where(x =>
                    x.Email == email && x.IsUsed == false
                )
                .ToListAsync();

            if (oldOtps.Count > 0)
            {
                _context.OtpVerifications.RemoveRange(oldOtps);
            }
        }

        private static SignupSessionResponse ToSessionResponse(
            PendingSignup pending
        ) =>
            new()
            {
                SessionToken = pending.SessionToken,
                Email = pending.Email,
                Status = pending.Status,
            };
    }
}
