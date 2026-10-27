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

        public const string SessionExpiredCode = "signup_session_expired";

        public const string SessionExpiredMessage =
            "Your signup session has expired. Enter your email to continue.";

        private static readonly TimeSpan SessionLoginWindow = TimeSpan.FromHours(24);

        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IProvisioningService _provisioningService;
        private readonly ICustomPlanQuoteService _customPlanQuote;
        private readonly IJwtService _jwt;
        private readonly IOtpChallengeService _otp;
        private readonly TimeProvider _clock;
        private readonly ILogger<SignupService> _logger;

        public SignupService(
            ApplicationDbContext context,
            IEmailService emailService,
            IProvisioningService provisioningService,
            ICustomPlanQuoteService customPlanQuote,
            IJwtService jwt,
            IOtpChallengeService otp,
            TimeProvider clock,
            ILogger<SignupService> logger
        )
        {
            _context = context;
            _emailService = emailService;
            _provisioningService = provisioningService;
            _customPlanQuote = customPlanQuote;
            _jwt = jwt;
            _otp = otp;
            _clock = clock;
            _logger = logger;
        }

        // A List, not an array: array Contains binds to the span overload, which EF cannot translate.
        private static readonly List<string> LiveSignupStatuses =
        [
            PendingSignupStatuses.EmailPending,
            PendingSignupStatuses.Verified,
            PendingSignupStatuses.OnboardingComplete,
            PendingSignupStatuses.AwaitingPayment,
            PendingSignupStatuses.Provisioning,
        ];

        /// <summary>Resends allowed on one EmailPending row before it becomes Abandoned.</summary>
        private const int ResendLimit = 5;

        private const string ExpiredCodeMessage =
            "That code has expired. Resend a new code to continue.";

        private const string InvalidCodeMessage =
            "That code doesn't match. Check your email and try again.";

        public async Task<SignupCodeSentResponse> StartAsync(StartSignupDto dto)
        {
            var email = NormalizeEmail(dto.Email);

            if (!dto.TermsAccepted)
            {
                throw Invalid("Terms must be accepted.");
            }

            if (await _context.Users.AnyAsync(x => x.Email == email))
            {
                _logger.LogInformation(
                    "signup.code_suppressed reason=account_exists email={MaskedEmail}",
                    LogContactMask.Email(email)
                );
                return CodeSent(email);
            }

            var pending = await _context.PendingSignups.FirstOrDefaultAsync(x =>
                x.Email == email
            );

            if (pending == null)
            {
                pending = new PendingSignup
                {
                    Id = Guid.NewGuid(),
                    SessionToken = Guid.NewGuid(),
                    Email = email,
                    Status = PendingSignupStatuses.EmailPending,
                    OtpResendCount = 0,
                    TermsAccepted = dto.TermsAccepted,
                    CreatedAtUtc = _clock.GetUtcNow().UtcDateTime,
                    UpdatedAtUtc = _clock.GetUtcNow().UtcDateTime,
                };

                await _context.PendingSignups.AddAsync(pending);
                await EnforceAndSendOtpAsync(pending, countAsResend: false);
                return CodeSent(email);
            }

            if (pending.Status == PendingSignupStatuses.Complete)
            {
                _logger.LogInformation(
                    "signup.code_suppressed reason=complete email={MaskedEmail}",
                    LogContactMask.Email(email)
                );
                return CodeSent(email);
            }

            if (pending.Status == PendingSignupStatuses.Abandoned)
            {
                pending.Status = PendingSignupStatuses.EmailPending;
                pending.OtpResendCount = 0;
                pending.LastOtpSentAt = null;
                pending.EmailVerifiedAt = null;
                pending.SessionToken = Guid.NewGuid();
                pending.TermsAccepted = dto.TermsAccepted;

                // Window counters are kept on restart (abuse limits are per email, not per row state).
                await EnforceAndSendOtpAsync(pending, countAsResend: false);
                return CodeSent(email);
            }

            if (!LiveSignupStatuses.Contains(pending.Status))
            {
                return CodeSent(email);
            }

            if (pending.Status == PendingSignupStatuses.EmailPending)
            {
                pending.TermsAccepted = dto.TermsAccepted;
            }

            // Mid-flow rows get a fresh code too, but keep their status and session token.
            await EnforceAndSendOtpAsync(pending, countAsResend: true);
            return CodeSent(email);
        }

        public async Task<SignupSessionResponse> VerifyOtpAsync(
            VerifySignupOtpDto dto
        )
        {
            var email = NormalizeEmail(dto.Email);
            var code = (dto.OtpCode ?? string.Empty).Trim();
            var now = _clock.GetUtcNow().UtcDateTime;

            var pending = await FindLiveSignupAsync(email);
            if (pending == null)
            {
                _logger.LogInformation(
                    "signup.otp_rejected reason=no_live_signup email={MaskedEmail}",
                    LogContactMask.Email(email)
                );
                throw new SignupFlowException("otp_invalid", InvalidCodeMessage);
            }

            EnsureAbuseWindow(pending, now);
            if (IsVerifyLocked(pending))
            {
                await _context.SaveChangesAsync();
                throw ChallengeLocked(pending, email);
            }

            var result = await _otp.VerifyAsync(
                new OtpVerifyRequest(
                    email,
                    OtpPurposes.Signup,
                    SubjectFor(pending),
                    pending.CodeGeneration,
                    code
                )
            );

            if (result.Outcome != OtpVerifyOutcome.Verified)
            {
                pending.FailedVerifiesInWindow += 1;
                pending.UpdatedAtUtc = now;
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "signup.otp_rejected reason={Outcome} email={MaskedEmail} failedInWindow={FailedInWindow}",
                    result.Outcome,
                    LogContactMask.Email(email),
                    pending.FailedVerifiesInWindow
                );
                throw VerificationFailure(result.Outcome);
            }

            if (pending.Status == PendingSignupStatuses.EmailPending)
            {
                pending.Status = PendingSignupStatuses.Verified;
            }

            // Each successful Verify is a fresh proof of the email. The old token stops working.
            pending.EmailVerifiedAt = now;
            pending.SessionToken = Guid.NewGuid();
            pending.SessionTokenIssuedAtUtc = now;
            pending.UpdatedAtUtc = now;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "signup.verified email={MaskedEmail} status={Status}",
                LogContactMask.Email(email),
                pending.Status
            );

            return ToSessionResponse(pending);
        }

        public async Task<SignupCodeSentResponse> ResendOtpAsync(string email)
        {
            var normalised = NormalizeEmail(email);

            var pending = await FindLiveSignupAsync(normalised);
            if (pending == null)
            {
                _logger.LogInformation(
                    "signup.code_suppressed reason=no_live_signup email={MaskedEmail}",
                    LogContactMask.Email(normalised)
                );
                return CodeSent(normalised);
            }

            await EnforceAndSendOtpAsync(pending, countAsResend: true);
            return CodeSent(normalised);
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
                throw Invalid("Email must be verified before onboarding.");
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
            // Complete rows answer with the status only: the profile and email are not needed after payment.
            var pending = await FindBySessionTokenAsync(sessionToken);

            if (pending.Status == PendingSignupStatuses.Complete)
            {
                return new SignupResumeResponse { Status = pending.Status };
            }

            SignupOnboardingPayload? profile = null;
            if (!string.IsNullOrWhiteSpace(pending.OnboardingJson))
            {
                profile = JsonSerializer.Deserialize<SignupOnboardingPayload>(
                    pending.OnboardingJson
                );
            }

            return new SignupResumeResponse
            {
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

                // Provisioned, but the Pilot start may not have run yet.
                await _provisioningService.EnsurePilotForPendingAsync(pending.Id);

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
                throw Invalid("Account setup is not ready to provision.");
            }

            if (string.IsNullOrWhiteSpace(pending.ChosenPlan))
            {
                throw Invalid(ChoosePlanMessage);
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
                throw Invalid("Signup session login has expired.");
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
            // The claim also revokes the session token: a second session-login gets the 401.
            if (_context.Database.IsRelational())
            {
                var updated = await _context.PendingSignups
                    .Where(row =>
                        row.Id == pending.Id
                        && row.SessionLoginConsumedAtUtc == null
                    )
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(row => row.SessionLoginConsumedAtUtc, now)
                        .SetProperty(row => row.SessionTokenIssuedAtUtc, (DateTime?)null)
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
                pending.SessionTokenIssuedAtUtc = null;
                pending.UpdatedAtUtc = now;
                return;
            }

            pending.SessionLoginConsumedAtUtc = now;
            pending.SessionTokenIssuedAtUtc = null;
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

        /// <summary>
        /// The only place that resolves a signup session token. A refused token logs its
        /// reason and gets the same 401 as an expired session. The token is never logged.
        /// Complete rows stay readable (status, retry, idempotent onboarding, session-login):
        /// the session-login claim is what revokes the token.
        /// </summary>
        private async Task<PendingSignup> FindBySessionTokenAsync(Guid sessionToken)
        {
            var pending = await _context.PendingSignups.FirstOrDefaultAsync(x =>
                x.SessionToken == sessionToken
            );

            if (pending == null)
            {
                throw RejectSessionToken("missing");
            }

            if (pending.SessionTokenIssuedAtUtc is not { } issuedAtUtc)
            {
                throw RejectSessionToken("legacy");
            }

            var now = _clock.GetUtcNow().UtcDateTime;
            if (now - issuedAtUtc > SignupAbuseLimits.TokenLifetime)
            {
                throw RejectSessionToken("expired");
            }

            if (pending.Status == PendingSignupStatuses.Abandoned)
            {
                throw RejectSessionToken("abandoned");
            }

            if (pending.SessionLoginConsumedAtUtc != null)
            {
                throw RejectSessionToken("claimed");
            }

            return pending;
        }

        /// <summary>
        /// Logs the refusal reason and returns the 401 that every refused token gets.
        /// </summary>
        private SignupFlowException RejectSessionToken(string reason)
        {
            _logger.LogInformation("signup.token_rejected reason={Reason}", reason);
            return new SignupFlowException(
                SessionExpiredCode,
                SessionExpiredMessage,
                401
            );
        }

        private static SignupFlowException Invalid(string message) =>
            new("signup_invalid_request", message);

        private static void ValidateOnboardingPayload(
            SaveSignupOnboardingDto dto,
            bool isSocial
        )
        {
            if (!isSocial)
            {
                if (dto.Password != dto.ConfirmPassword)
                {
                    throw Invalid("Passwords do not match.");
                }

                if (
                    string.IsNullOrWhiteSpace(dto.Password)
                    || dto.Password.Length < 8
                )
                {
                    throw Invalid("Password must be at least 8 characters.");
                }
            }

            if (string.IsNullOrWhiteSpace(dto.FullName))
            {
                throw Invalid("Full name is required.");
            }

            if (string.IsNullOrWhiteSpace(dto.GroupName))
            {
                throw Invalid("Group name is required.");
            }

            if (string.IsNullOrWhiteSpace(dto.BusinessCategory))
            {
                throw Invalid("Business category is required.");
            }

            if (dto.Locations == null || dto.Locations.Count != 1)
            {
                throw Invalid("Exactly one location is required.");
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
                    throw Invalid("Custom plan config must be version 2 (base plan + packs).");
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
                throw Invalid("Custom plan config is required.");
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
                throw Invalid(ChoosePlanMessage);
            }

            return raw.Trim().ToLowerInvariant() switch
            {
                "free" => throw Invalid(ChoosePlanMessage),
                "pilot" => BillingSubscriptionPlans.Pilot,
                "starter" => BillingSubscriptionPlans.Starter,
                "growth" => BillingSubscriptionPlans.Growth,
                "group" => BillingSubscriptionPlans.Group,
                "custom" => BillingSubscriptionPlans.Custom,
                _ => throw Invalid("Invalid plan selection."),
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
            var now = _clock.GetUtcNow().UtcDateTime;
            var email = pending.Email;
            var isEmailPending = pending.Status == PendingSignupStatuses.EmailPending;

            EnsureAbuseWindow(pending, now);
            if (IsSendLocked(pending))
            {
                await _context.SaveChangesAsync();
                throw ChallengeLocked(pending, email);
            }

            if (countAsResend)
            {
                if (
                    pending.LastOtpSentAt is { } lastSent
                    && lastSent.Add(SignupAbuseLimits.ResendCooldown) > now
                )
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation(
                        "signup.code_suppressed reason=resend_cooldown email={MaskedEmail}",
                        LogContactMask.Email(email)
                    );
                    throw new SignupFlowException(
                        "resend_cooldown",
                        "Please wait before requesting another code.",
                        429
                    );
                }

                if (isEmailPending && pending.OtpResendCount >= ResendLimit)
                {
                    pending.Status = PendingSignupStatuses.Abandoned;
                    pending.UpdatedAtUtc = now;
                    await _context.SaveChangesAsync();
                    _logger.LogInformation(
                        "signup.resend_limit_reached email={MaskedEmail}",
                        LogContactMask.Email(email)
                    );
                    throw Invalid("OTP resend limit reached.");
                }
            }

            pending.CodeGeneration += 1;
            pending.CodesSentInWindow += 1;
            if (countAsResend && isEmailPending)
            {
                pending.OtpResendCount += 1;
            }
            pending.LastOtpSentAt = now;
            pending.UpdatedAtUtc = now;

            // IssueAsync saves the row and the counters above in one change set.
            var issued = await _otp.IssueAsync(
                new OtpIssueRequest(
                    email,
                    OtpPurposes.Signup,
                    SubjectFor(pending),
                    pending.CodeGeneration
                )
            );

            try
            {
                await _emailService.SendOtpEmailAsync(email, issued.Code);
            }
            catch (Exception ex)
            {
                // The code was never delivered, so it must not verify and must not count.
                await _otp.InvalidateAsync(issued.OtpVerificationId);
                pending.CodesSentInWindow -= 1;
                if (countAsResend && isEmailPending)
                {
                    pending.OtpResendCount -= 1;
                }
                await _context.SaveChangesAsync();

                // Do not log the exception itself. Provider messages can echo the address or body.
                _logger.LogWarning(
                    "signup.code_delivery_failed email={MaskedEmail} error={ErrorType}",
                    LogContactMask.Email(email),
                    ex.GetType().Name
                );
                throw new SignupFlowException(
                    "code_delivery_failed",
                    "We could not send the code. Try again in a minute.",
                    503
                );
            }

            _logger.LogInformation(
                "signup.code_sent email={MaskedEmail} generation={Generation}",
                LogContactMask.Email(email),
                pending.CodeGeneration
            );
        }

        private async Task<PendingSignup?> FindLiveSignupAsync(string email) =>
            await _context.PendingSignups.FirstOrDefaultAsync(x =>
                x.Email == email && LiveSignupStatuses.Contains(x.Status)
            );

        /// <summary>
        /// Starts a new 24 hour window when none is open or the open one has expired.
        /// </summary>
        private static void EnsureAbuseWindow(PendingSignup pending, DateTime now)
        {
            if (
                pending.AbuseWindowStartUtc is { } windowStart
                && now - windowStart < SignupAbuseLimits.Window
            )
            {
                return;
            }

            pending.AbuseWindowStartUtc = now;
            pending.CodesSentInWindow = 0;
            pending.FailedVerifiesInWindow = 0;
        }

        /// <summary>
        /// Sending stops when the window has used all codes or all wrong verifies.
        /// </summary>
        private static bool IsSendLocked(PendingSignup pending) =>
            pending.CodesSentInWindow >= SignupAbuseLimits.MaxCodesPerWindow
            || IsVerifyLocked(pending);

        /// <summary>
        /// Verifying stops only when the window has used all wrong verifies. A correct
        /// code that was already sent must still verify after the send limit is reached.
        /// </summary>
        private static bool IsVerifyLocked(PendingSignup pending) =>
            pending.FailedVerifiesInWindow >= SignupAbuseLimits.MaxFailedVerifiesPerWindow;

        private SignupFlowException ChallengeLocked(PendingSignup pending, string email)
        {
            _logger.LogWarning(
                "signup.challenge_locked email={MaskedEmail} codesInWindow={CodesInWindow} failedInWindow={FailedInWindow}",
                LogContactMask.Email(email),
                pending.CodesSentInWindow,
                pending.FailedVerifiesInWindow
            );

            return new SignupFlowException(
                "challenge_locked",
                "Too many attempts for this email. Try again later.",
                429,
                pending.AbuseWindowStartUtc!.Value.Add(SignupAbuseLimits.Window)
            );
        }

        private static SignupFlowException VerificationFailure(OtpVerifyOutcome outcome) =>
            outcome switch
            {
                OtpVerifyOutcome.Expired => new SignupFlowException(
                    "otp_expired",
                    ExpiredCodeMessage
                ),
                OtpVerifyOutcome.AttemptsExceeded => new SignupFlowException(
                    "otp_attempts_exceeded",
                    "Too many wrong codes. Request a new code."
                ),
                _ => new SignupFlowException("otp_invalid", InvalidCodeMessage),
            };

        private static string SubjectFor(PendingSignup pending) => pending.Id.ToString("N");

        private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

        private static SignupCodeSentResponse CodeSent(string email) =>
            new() { Email = email, Status = "code_sent" };

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
