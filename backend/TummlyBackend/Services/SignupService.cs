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
                    "signup.code_suppressed reason=account_exists purpose={Purpose} email={MaskedEmail}",
                    OtpPurposes.Signup,
                    LogContactMask.Email(email)
                );
                return CodeSent(email);
            }

            var pending = await _context.PendingSignups.FirstOrDefaultAsync(x =>
                x.Email == email
            );

            if (pending == null)
            {
                var now = _clock.GetUtcNow().UtcDateTime;
                var created = new PendingSignup
                {
                    Id = Guid.NewGuid(),
                    SessionToken = Guid.NewGuid(),
                    Email = email,
                    Status = PendingSignupStatuses.EmailPending,
                    OtpResendCount = 0,
                    TermsAccepted = dto.TermsAccepted,
                    AbuseWindowStartUtc = now,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                };

                // The row is saved first: the send counters are updated with conditional
                // UPDATEs that need an existing row.
                _context.PendingSignups.Add(created);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException) when (_context.Database.IsRelational())
                {
                    // Another request created the row for this email first (unique index).
                    _context.Entry(created).State = EntityState.Detached;
                    pending = await _context.PendingSignups.FirstOrDefaultAsync(x =>
                        x.Email == email
                    );
                    if (pending == null)
                    {
                        throw;
                    }
                }

                if (pending == null)
                {
                    await SendCodeAsync(created, countAsResend: false);
                    return CodeSent(email);
                }
            }

            if (pending.Status == PendingSignupStatuses.Complete)
            {
                _logger.LogInformation(
                    "signup.code_suppressed reason=complete pending_id={PendingId} purpose={Purpose} email={MaskedEmail}",
                    pending.Id,
                    OtpPurposes.Signup,
                    LogContactMask.Email(email)
                );
                return CodeSent(email);
            }

            if (pending.Status == PendingSignupStatuses.Abandoned)
            {
                RestartAbandoned(pending);
                pending.TermsAccepted = dto.TermsAccepted;

                // Window counters are kept on restart (abuse limits are per email, not per row state).
                await SendCodeAsync(pending, countAsResend: false);
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
            await SendCodeAsync(pending, countAsResend: true);
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
                    "signup.verify_failed reason=no_live_signup purpose={Purpose} email={MaskedEmail}",
                    OtpPurposes.Signup,
                    LogContactMask.Email(email)
                );
                throw new SignupFlowException("otp_invalid", InvalidCodeMessage);
            }

            await EnsureAbuseWindowAsync(pending, now);

            // One window failure is reserved before the code is compared, atomically. A burst of
            // parallel guesses can then not run past the window limit. A right code also uses
            // one attempt: it is not given back.
            if (!await TryReserveVerifyAttemptAsync(pending, now))
            {
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
                _logger.LogInformation(
                    "signup.verify_failed reason={Outcome} pending_id={PendingId} purpose={Purpose} generation={Generation} email={MaskedEmail} failedInWindow={FailedInWindow}",
                    result.Outcome,
                    pending.Id,
                    OtpPurposes.Signup,
                    pending.CodeGeneration,
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
                "signup.token_issued pending_id={PendingId} purpose={Purpose} generation={Generation} email={MaskedEmail} status={Status}",
                pending.Id,
                OtpPurposes.Signup,
                pending.CodeGeneration,
                LogContactMask.Email(email),
                pending.Status
            );

            return ToSessionResponse(pending);
        }

        public async Task<SignupCodeSentResponse> ResendOtpAsync(string email)
        {
            var normalised = NormalizeEmail(email);

            var pending = await _context.PendingSignups.FirstOrDefaultAsync(x =>
                x.Email == normalised
                && (
                    LiveSignupStatuses.Contains(x.Status)
                    || x.Status == PendingSignupStatuses.Abandoned
                )
            );
            if (pending == null)
            {
                _logger.LogInformation(
                    "signup.code_suppressed reason=no_live_signup purpose={Purpose} email={MaskedEmail}",
                    OtpPurposes.Signup,
                    LogContactMask.Email(normalised)
                );
                return CodeSent(normalised);
            }

            if (pending.Status == PendingSignupStatuses.Abandoned)
            {
                // An Abandoned row restarts like Start does. An email that already has an account never does.
                if (await _context.Users.AnyAsync(x => x.Email == normalised))
                {
                    _logger.LogInformation(
                        "signup.code_suppressed reason=account_exists purpose={Purpose} email={MaskedEmail}",
                        OtpPurposes.Signup,
                        LogContactMask.Email(normalised)
                    );
                    return CodeSent(normalised);
                }

                RestartAbandoned(pending);
                await SendCodeAsync(pending, countAsResend: false);
                return CodeSent(normalised);
            }

            await SendCodeAsync(pending, countAsResend: true);
            return CodeSent(normalised);
        }

        public async Task<SignupSessionResponse> SaveOnboardingAsync(
            Guid sessionToken,
            SaveSignupOnboardingDto dto
        )
        {
            var pending = await FindBySessionTokenAsync(sessionToken);

            // Complete rows answer with the status only. The token and email are not returned again.
            if (pending.Status == PendingSignupStatuses.Complete)
            {
                return new SignupSessionResponse { Status = pending.Status };
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
                ChosenPlan = pending.ChosenPlan,
                ChosenCadence = pending.ChosenCadence,
                PlanConfig = ResolveResumePlanConfig(pending.CustomPlanConfigJson),
            };
        }

        /// <summary>
        /// The saved Custom config (base plan, Locations, extras). Null for a catalogue plan
        /// or when the stored JSON has no v2 base plan.
        /// </summary>
        private static SignupResumePlanConfig? ResolveResumePlanConfig(string? json)
        {
            var parsed = CustomPlanConfig.TryParse(json);
            if (parsed == null || !parsed.IsV2 || string.IsNullOrWhiteSpace(parsed.BasePlan))
            {
                return null;
            }

            return new SignupResumePlanConfig
            {
                BasePlan = parsed.BasePlan,
                Locations = parsed.Locations ?? parsed.Quantities.Locations,
                Packs = parsed.Packs == null
                    ? []
                    : new Dictionary<string, int>(parsed.Packs, StringComparer.Ordinal),
                QuoteHash = parsed.QuoteHash,
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
            var pending = await FindBySessionTokenAsync(sessionToken, allowClaimed: true);

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
            // The claim also revokes the session token: any other signup call with it gets the 401.
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
                // The saved choices come back only when the quote needs a new review,
                // so the guest sees them again with a fresh server quote.
                SavedPlan = IsQuoteReviewCode(paymentFailureCode)
                    ? new SignupSavedPlanDto
                    {
                        ChosenPlan = pending.ChosenPlan,
                        ChosenCadence = pending.ChosenCadence,
                        CustomPlanConfigJson = pending.CustomPlanConfigJson,
                    }
                    : null,
            };
        }

        /// <summary>
        /// The paid checkout did not open because the saved quote is stale
        /// (<c>quote_changed</c>) or names a retired pack (<c>legacy_pack_key</c>).
        /// </summary>
        private static bool IsQuoteReviewCode(string? code) =>
            code is "quote_changed" or "legacy_pack_key";

        public async Task UpdatePlanAfterFailedCheckoutAsync(
            Guid sessionToken,
            UpdateSignupPlanDto dto
        )
        {
            var pending = await FindBySessionTokenAsync(sessionToken);
            if (
                pending.Status != PendingSignupStatuses.Complete
                || !IsPaidChosenPlan(pending.ChosenPlan)
                || await ResolvePaidCheckoutFailureAsync(pending) == null
            )
            {
                throw Invalid("There is no unpaid plan to review.");
            }

            if (!IsPaidChosenPlan(ResolveChosenPlan(dto.ChosenPlan)))
            {
                throw Invalid("Choose a paid plan to continue.");
            }

            // The hash of the quote the guest reviewed must match the current price.
            // A stale or retired choice throws and starts no order.
            await ApplyChosenPlanAsync(
                pending,
                dto.ChosenPlan,
                dto.ChosenCadence,
                dto.CustomPlanConfigJson
            );
            pending.UpdatedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _provisioningService.RetryPaidSignupCheckoutAsync(pending.Id);
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
        private async Task<PendingSignup> FindBySessionTokenAsync(
            Guid sessionToken,
            bool allowClaimed = false
        )
        {
            var pending = await _context.PendingSignups.FirstOrDefaultAsync(x =>
                x.SessionToken == sessionToken
            );

            if (pending == null)
            {
                throw RejectSessionToken("missing", null);
            }

            // The claim is checked first: it clears the issued time, so a replay of a claimed
            // token would otherwise look like a legacy token. Session-login passes
            // allowClaimed and answers a replay with 409 session_login_used.
            if (pending.SessionLoginConsumedAtUtc != null)
            {
                if (allowClaimed)
                {
                    return pending;
                }

                throw RejectSessionToken("claimed", pending.Id);
            }

            if (pending.SessionTokenIssuedAtUtc is not { } issuedAtUtc)
            {
                throw RejectSessionToken("legacy", pending.Id);
            }

            var now = _clock.GetUtcNow().UtcDateTime;
            if (now - issuedAtUtc > SignupAbuseLimits.TokenLifetime)
            {
                throw RejectSessionToken("expired", pending.Id);
            }

            if (pending.Status == PendingSignupStatuses.Abandoned)
            {
                throw RejectSessionToken("abandoned", pending.Id);
            }

            return pending;
        }

        /// <summary>
        /// Logs the refusal reason and returns the 401 that every refused token gets.
        /// </summary>
        private SignupFlowException RejectSessionToken(string reason, Guid? pendingId)
        {
            _logger.LogInformation(
                "signup.token_rejected reason={Reason} pending_id={PendingId}",
                reason,
                pendingId
            );
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
        ) =>
            await ApplyChosenPlanAsync(
                pending,
                dto.ChosenPlan,
                dto.ChosenCadence,
                dto.CustomPlanConfigJson
            );

        private async Task ApplyChosenPlanAsync(
            PendingSignup pending,
            string? chosenPlan,
            string? chosenCadence,
            string? customPlanConfigJson
        )
        {
            var chosen = ResolveChosenPlan(chosenPlan);
            var cadence = ResolveChosenCadence(chosen, chosenCadence);

            if (
                !string.IsNullOrWhiteSpace(customPlanConfigJson)
                && string.Equals(
                    chosen,
                    BillingSubscriptionPlans.Pilot,
                    StringComparison.Ordinal
                )
                && CustomPlanConfig
                    .TryParse(DecodeCustomPlanConfigJson(customPlanConfigJson))
                    ?.Packs?.Any(kv =>
                        kv.Value > 0
                        && (
                            CustomPlanPacks.IsCapacityPackKey(kv.Key)
                            || CustomPlanPacks.LegacyCapacityPackKeys.ContainsKey(kv.Key)
                        )
                    ) == true
            )
            {
                throw new CustomPlanQuoteException(
                    "pilot_no_capacity_addons",
                    "Pilot has no paid capacity add-ons."
                );
            }

            if (
                !string.IsNullOrWhiteSpace(customPlanConfigJson)
                && IsPaidOrCustomPlan(chosen)
            )
            {
                var decoded = DecodeCustomPlanConfigJson(customPlanConfigJson);
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
                    // The hash of the quote the guest saw must match. A missing hash
                    // (saved before the hash was required) is not confirmed either, so
                    // the guest reviews the current price again.
                    if (
                        string.IsNullOrWhiteSpace(parsed.QuoteHash)
                        || !string.Equals(
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

        /// <summary>
        /// Puts an Abandoned row back to EmailPending with a fresh session token. The send
        /// cooldown and the window counters are kept, so a restart never bypasses them.
        /// </summary>
        private static void RestartAbandoned(PendingSignup pending)
        {
            pending.Status = PendingSignupStatuses.EmailPending;
            pending.OtpResendCount = 0;
            pending.EmailVerifiedAt = null;
            pending.SessionToken = Guid.NewGuid();
        }

        /// <summary>
        /// Sends one code. The cooldown, the resend cap and the per-email window limits are
        /// enforced by one conditional UPDATE (see <see cref="TryReserveSendAsync"/>), so a
        /// parallel burst of Start and Resend sends at most one code per cooldown. A refused
        /// send answers like a sent one and sends nothing: the earlier code stays valid and
        /// the response cannot tell whether the email has an account.
        /// </summary>
        private async Task SendCodeAsync(PendingSignup pending, bool countAsResend)
        {
            var now = _clock.GetUtcNow().UtcDateTime;
            var email = pending.Email;
            var capApplies = countAsResend && pending.Status == PendingSignupStatuses.EmailPending;

            // Row changes made by the caller (restart, terms) are saved before conditional UPDATEs run.
            await _context.SaveChangesAsync();
            await EnsureAbuseWindowAsync(pending, now);

            if (!await TryReserveSendAsync(pending, now, capApplies))
            {
                await SuppressSendAsync(pending, now, capApplies);
                return;
            }

            // The reserve reloaded the row: CodeGeneration is the generation of this send.
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
                await ReleaseSendAsync(pending, capApplies);

                // Do not log the exception itself. Provider messages can echo the address or body.
                _logger.LogWarning(
                    "signup.code_delivery_failed pending_id={PendingId} purpose={Purpose} generation={Generation} email={MaskedEmail} error={ErrorType}",
                    pending.Id,
                    OtpPurposes.Signup,
                    pending.CodeGeneration,
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
                "signup.code_sent pending_id={PendingId} purpose={Purpose} generation={Generation} email={MaskedEmail}",
                pending.Id,
                OtpPurposes.Signup,
                pending.CodeGeneration,
                LogContactMask.Email(email)
            );
        }

        /// <summary>
        /// Takes the right to send one code. On a relational provider this is one conditional
        /// UPDATE: it succeeds only while the window has codes and wrong verifies left, the
        /// cooldown has passed and (for a counted resend) the cap is not reached. It then adds
        /// one to the codes sent and the generation and stamps the send time. On success the
        /// tracked row is reloaded so it holds the new generation.
        /// </summary>
        private async Task<bool> TryReserveSendAsync(
            PendingSignup pending,
            DateTime now,
            bool capApplies
        )
        {
            var cutoff = now - SignupAbuseLimits.ResendCooldown;

            if (_context.Database.IsRelational())
            {
                var resendIncrement = capApplies ? 1 : 0;
                var reserved = await _context.PendingSignups
                    .Where(row =>
                        row.Id == pending.Id
                        && row.CodesSentInWindow < SignupAbuseLimits.MaxCodesPerWindow
                        && row.FailedVerifiesInWindow < SignupAbuseLimits.MaxFailedVerifiesPerWindow
                        && (row.LastOtpSentAt == null || row.LastOtpSentAt <= cutoff)
                        && (!capApplies || row.OtpResendCount < ResendLimit)
                    )
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(row => row.CodesSentInWindow, row => row.CodesSentInWindow + 1)
                        .SetProperty(row => row.CodeGeneration, row => row.CodeGeneration + 1)
                        .SetProperty(row => row.OtpResendCount, row => row.OtpResendCount + resendIncrement)
                        .SetProperty(row => row.LastOtpSentAt, (DateTime?)now)
                        .SetProperty(row => row.UpdatedAtUtc, now)
                    );
                if (reserved == 0)
                {
                    return false;
                }

                await _context.Entry(pending).ReloadAsync();
                return true;
            }

            if (
                pending.CodesSentInWindow >= SignupAbuseLimits.MaxCodesPerWindow
                || pending.FailedVerifiesInWindow >= SignupAbuseLimits.MaxFailedVerifiesPerWindow
                || (pending.LastOtpSentAt is { } lastSent && lastSent > cutoff)
                || (capApplies && pending.OtpResendCount >= ResendLimit)
            )
            {
                return false;
            }

            pending.CodesSentInWindow += 1;
            pending.CodeGeneration += 1;
            if (capApplies)
            {
                pending.OtpResendCount += 1;
            }
            pending.LastOtpSentAt = now;
            pending.UpdatedAtUtc = now;
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// A refused send: logs the reason, and on the resend cap marks the row Abandoned.
        /// Nothing is thrown and nothing is sent. The caller answers like a sent code.
        /// </summary>
        private async Task SuppressSendAsync(PendingSignup pending, DateTime now, bool capApplies)
        {
            await _context.Entry(pending).ReloadAsync();

            string reason;
            if (
                pending.CodesSentInWindow >= SignupAbuseLimits.MaxCodesPerWindow
                || pending.FailedVerifiesInWindow >= SignupAbuseLimits.MaxFailedVerifiesPerWindow
            )
            {
                reason = "send_locked";
            }
            else if (
                pending.LastOtpSentAt is { } lastSent
                && lastSent.Add(SignupAbuseLimits.ResendCooldown) > now
            )
            {
                reason = "resend_cooldown";
            }
            else if (capApplies && pending.OtpResendCount >= ResendLimit)
            {
                reason = "resend_cap";
                pending.Status = PendingSignupStatuses.Abandoned;
                pending.UpdatedAtUtc = now;
                await _context.SaveChangesAsync();
            }
            else
            {
                // Another request took the send between the read and the update.
                reason = "send_taken";
            }

            _logger.LogInformation(
                "signup.code_suppressed reason={Reason} pending_id={PendingId} purpose={Purpose} email={MaskedEmail}",
                reason,
                pending.Id,
                OtpPurposes.Signup,
                LogContactMask.Email(pending.Email)
            );
        }

        /// <summary>
        /// Gives back the counts of a send whose email was not delivered. The generation and
        /// the send time stay: the cooldown still applies after a delivery failure.
        /// </summary>
        private async Task ReleaseSendAsync(PendingSignup pending, bool capApplies)
        {
            var resendDecrement = capApplies ? 1 : 0;

            if (_context.Database.IsRelational())
            {
                await _context.PendingSignups
                    .Where(row => row.Id == pending.Id && row.CodesSentInWindow > 0)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(row => row.CodesSentInWindow, row => row.CodesSentInWindow - 1)
                        .SetProperty(
                            row => row.OtpResendCount,
                            row => row.OtpResendCount >= resendDecrement
                                ? row.OtpResendCount - resendDecrement
                                : row.OtpResendCount
                        )
                    );
                await _context.Entry(pending).ReloadAsync();
                return;
            }

            if (pending.CodesSentInWindow > 0)
            {
                pending.CodesSentInWindow -= 1;
            }
            if (capApplies && pending.OtpResendCount > 0)
            {
                pending.OtpResendCount -= 1;
            }
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Reserves one wrong-verify attempt in the window before a code is compared.
        /// Succeeds only while the window has attempts left. On a relational provider the
        /// check and the increment are one conditional UPDATE, so parallel guesses all count.
        /// </summary>
        private async Task<bool> TryReserveVerifyAttemptAsync(PendingSignup pending, DateTime now)
        {
            if (_context.Database.IsRelational())
            {
                var reserved = await _context.PendingSignups
                    .Where(row =>
                        row.Id == pending.Id
                        && row.FailedVerifiesInWindow < SignupAbuseLimits.MaxFailedVerifiesPerWindow
                    )
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(
                            row => row.FailedVerifiesInWindow,
                            row => row.FailedVerifiesInWindow + 1
                        )
                        .SetProperty(row => row.UpdatedAtUtc, now)
                    );
                await _context.Entry(pending).ReloadAsync();
                return reserved == 1;
            }

            if (IsVerifyLocked(pending))
            {
                return false;
            }

            pending.FailedVerifiesInWindow += 1;
            pending.UpdatedAtUtc = now;
            await _context.SaveChangesAsync();
            return true;
        }

        private async Task<PendingSignup?> FindLiveSignupAsync(string email) =>
            await _context.PendingSignups.FirstOrDefaultAsync(x =>
                x.Email == email && LiveSignupStatuses.Contains(x.Status)
            );

        /// <summary>
        /// Starts a new 24 hour window when none is open or the open one has expired. On a
        /// relational provider this is one conditional UPDATE, so parallel calls reset once.
        /// </summary>
        private async Task EnsureAbuseWindowAsync(PendingSignup pending, DateTime now)
        {
            var windowCutoff = now - SignupAbuseLimits.Window;

            if (_context.Database.IsRelational())
            {
                var reset = await _context.PendingSignups
                    .Where(row =>
                        row.Id == pending.Id
                        && (row.AbuseWindowStartUtc == null || row.AbuseWindowStartUtc <= windowCutoff)
                    )
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(row => row.AbuseWindowStartUtc, (DateTime?)now)
                        .SetProperty(row => row.CodesSentInWindow, 0)
                        .SetProperty(row => row.FailedVerifiesInWindow, 0)
                    );
                if (reset > 0)
                {
                    await _context.Entry(pending).ReloadAsync();
                }
                return;
            }

            if (pending.AbuseWindowStartUtc is { } windowStart && windowStart > windowCutoff)
            {
                return;
            }

            pending.AbuseWindowStartUtc = now;
            pending.CodesSentInWindow = 0;
            pending.FailedVerifiesInWindow = 0;
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Verifying stops only when the window has used all wrong verifies. A correct
        /// code that was already sent must still verify after the send limit is reached.
        /// </summary>
        private static bool IsVerifyLocked(PendingSignup pending) =>
            pending.FailedVerifiesInWindow >= SignupAbuseLimits.MaxFailedVerifiesPerWindow;

        private SignupFlowException ChallengeLocked(PendingSignup pending, string email)
        {
            _logger.LogWarning(
                "signup.challenge_locked pending_id={PendingId} purpose={Purpose} email={MaskedEmail} codesInWindow={CodesInWindow} failedInWindow={FailedInWindow}",
                pending.Id,
                OtpPurposes.Signup,
                LogContactMask.Email(email),
                pending.CodesSentInWindow,
                pending.FailedVerifiesInWindow
            );

            // SQL Server returns Kind=Unspecified. The value is UTC, and the JSON needs the "Z".
            return new SignupFlowException(
                "challenge_locked",
                "Too many attempts for this email. Try again later.",
                429,
                DateTime.SpecifyKind(
                    pending.AbuseWindowStartUtc!.Value.Add(SignupAbuseLimits.Window),
                    DateTimeKind.Utc
                )
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
