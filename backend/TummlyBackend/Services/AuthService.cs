using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Auth;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
namespace TummlyBackend.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;

        private readonly IJwtService _jwtService;

        private readonly IEmailService _emailService;

        private readonly ISmsService _smsService;

        private readonly IConfiguration _configuration;

        private readonly ISignInMetadataResolver _signInMetadataResolver;

        private readonly ILogger<AuthService> _logger;

        private readonly IMemoryCache _cache;

        private readonly IActivationGate _activationGate;

        private const int MaxActivationVerifyAttempts = 5;

        private static readonly TimeSpan ActivationVerifyLockout =
            TimeSpan.FromMinutes(15);

        public AuthService(
    ApplicationDbContext context,
    IJwtService jwtService,
    IEmailService emailService,
    ISmsService smsService,
    IConfiguration configuration,
    ISignInMetadataResolver signInMetadataResolver,
    ILogger<AuthService> logger,
    IMemoryCache cache,
    IActivationGate activationGate
)
        {
            _context = context;
            _jwtService = jwtService;
            _emailService = emailService;
            _smsService = smsService;
            _configuration = configuration;
            _signInMetadataResolver = signInMetadataResolver;
            _logger = logger;
            _cache = cache;
            _activationGate = activationGate;
        }

        /*
         =========================================
         ADMIN LOGIN
         =========================================
        */

        public async Task<string> AdminLoginAsync(
            AdminLoginDto dto
        )
        {
            dto.Email =
                dto.Email.Trim().ToLower();

            var admin =
                await _context.Admins
                    .FirstOrDefaultAsync(x =>
                        x.Email == dto.Email &&
                        x.IsActive == true
                    );

            if (admin == null)
            {
                throw new Exception(
                    "Invalid email or password."
                );
            }

            if (admin.IsLocked)
            {
                throw new Exception(
                    "Account is locked."
                );
            }

            bool isPasswordValid =
                BCrypt.Net.BCrypt.Verify(
                    dto.Password,
                    admin.PasswordHash
                );

            if (!isPasswordValid)
            {
                admin.FailedLoginAttempts++;

                if (admin.FailedLoginAttempts >= 5)
                {
                    admin.IsLocked = true;
                }

                await _context.SaveChangesAsync();

                throw new Exception(
                    "Invalid email or password."
                );
            }

            admin.FailedLoginAttempts = 0;

            await _context.SaveChangesAsync();

            var token =
                _jwtService.GenerateAdminToken(admin);

            return token;
        }

        private async Task ValidateStaffLoginAsync(
            Admin admin,
            string password
        )
        {
            if (!admin.IsActive)
            {
                throw new Exception(
                    "Invalid email or password."
                );
            }

            if (admin.IsLocked)
            {
                throw new Exception(
                    "Account is locked."
                );
            }

            bool isPasswordValid =
                BCrypt.Net.BCrypt.Verify(
                    password,
                    admin.PasswordHash
                );

            if (!isPasswordValid)
            {
                admin.FailedLoginAttempts++;

                if (admin.FailedLoginAttempts >= 5)
                {
                    admin.IsLocked = true;
                }

                await _context.SaveChangesAsync();

                throw new Exception(
                    "Invalid email or password."
                );
            }

            admin.FailedLoginAttempts = 0;
            await _context.SaveChangesAsync();
        }

        /*
         =========================================
         USER LOGIN
         =========================================
        */

        public async Task<string> UserLoginAsync(
            UserLoginDto dto
        )

        {
            dto.Email =
                dto.Email.Trim().ToLower();

            var user =
                await _context.Users
                    .FirstOrDefaultAsync(x =>
                        x.Email == dto.Email
                    );

            if (user == null)
            {
                throw new Exception(
                    "Invalid email or password."
                );
            }

            if (user.IsLocked)
            {
                throw new Exception(
                    "Account is locked."
                );
            }

            bool isPasswordValid =
                BCrypt.Net.BCrypt.Verify(
                    dto.Password,
                    user.PasswordHash
                );

            if (!isPasswordValid)
            {
                user.FailedLoginAttempts++;

                if (user.FailedLoginAttempts >= 5)
                {
                    user.IsLocked = true;
                }

                await _context.SaveChangesAsync();

                throw new Exception(
                    "Invalid email or password."
                );
            }

            user.FailedLoginAttempts = 0;

            await _context.SaveChangesAsync();

            if (!user.IsEmailVerified)
            {
                throw new Exception(
                    "Email is not verified."
                );
            }

            if (!user.IsApprovedByAdmin)
            {
                throw new Exception(
                    "Account is not approved."
                );
            }

            await SendSignInOtpAsync(user);

            return "OTP_SENT";
        }

        public async Task<SendOtpResultDto> SendAuthOtpAsync(
            string email,
            string purpose
        )
        {
            email = email.Trim().ToLower();
            purpose = string.IsNullOrWhiteSpace(purpose)
                ? "resend"
                : purpose.Trim().ToLowerInvariant();

            var user = await GetUserForOtpDeliveryAsync(email);

            if (purpose == "switch-to-email")
            {
                var activeOtp = await GetActiveOtpAsync(email);

                if (
                    activeOtp != null &&
                    activeOtp.ExpiresAt > DateTime.UtcNow
                )
                {
                    return new SendOtpResultDto
                    {
                        Skipped = true,
                        OtpChannel = activeOtp.Channel,
                        Message =
                            "Your current verification code is still valid.",
                        MaskedPhone = GetMaskedPhoneIfVerified(user),
                    };
                }

                await EnforceOtpResendCooldownAsync(email);
                await SendOtpAsync(user, OtpVerification.ChannelEmail);

                return new SendOtpResultDto
                {
                    Skipped = false,
                    OtpChannel = OtpVerification.ChannelEmail,
                    Message = "OTP sent successfully.",
                    MaskedPhone = GetMaskedPhoneIfVerified(user),
                };
            }

            await EnforceOtpResendCooldownAsync(email);

            var activeChannel =
                (await GetActiveOtpAsync(email))?.Channel
                ?? OtpVerification.ChannelEmail;

            await SendOtpAsync(user, activeChannel);

            return new SendOtpResultDto
            {
                Skipped = false,
                OtpChannel = activeChannel,
                Message = "OTP sent successfully.",
                MaskedPhone = GetMaskedPhoneIfVerified(user),
            };
        }

        public async Task<SendOtpResultDto> SendAuthOtpSmsAsync(
            string email
        )
        {
            email = email.Trim().ToLower();

            var user = await GetUserForOtpDeliveryAsync(email);

            if (!UserHasVerifiedPhone(user))
            {
                throw new Exception(
                    "No verified phone number is on file."
                );
            }

            var activeOtp = await GetActiveOtpAsync(email);

            // Switching from email → SMS replaces the active code; only
            // re-sending on the SMS channel is rate-limited.
            if (activeOtp?.Channel == OtpVerification.ChannelSms)
            {
                await EnforceOtpResendCooldownAsync(email);
            }

            await SendOtpAsync(user, OtpVerification.ChannelSms);

            return new SendOtpResultDto
            {
                Skipped = false,
                OtpChannel = OtpVerification.ChannelSms,
                Message = "OTP sent successfully.",
                MaskedPhone = GetMaskedPhoneIfVerified(user),
            };
        }

        private async Task<User> GetUserForOtpDeliveryAsync(
            string email
        )
        {
            var user =
                await _context.Users
                    .FirstOrDefaultAsync(x =>
                        x.Email == email
                    );

            if (user == null)
            {
                throw new Exception(
                    "User not found."
                );
            }

            if (!user.IsEmailVerified)
            {
                throw new Exception(
                    "Email is not verified."
                );
            }

            if (!user.IsApprovedByAdmin)
            {
                throw new Exception(
                    "Account is not approved."
                );
            }

            return user;
        }

        private async Task<OtpVerification?> GetActiveOtpAsync(
            string email
        )
        {
            return await _context.OtpVerifications
                .Where(x =>
                    x.Email == email &&
                    x.IsUsed == false &&
                    x.ExpiresAt > DateTime.UtcNow
                )
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();
        }

        private async Task EnforceOtpResendCooldownAsync(
            string email
        )
        {
            var latestOtp =
                await _context.OtpVerifications
                    .Where(x => x.Email == email)
                    .OrderByDescending(x => x.CreatedAt)
                    .FirstOrDefaultAsync();

            if (
                latestOtp != null &&
                latestOtp.CreatedAt.AddSeconds(60) > DateTime.UtcNow
            )
            {
                throw new Exception(
                    "Please wait before resending OTP."
                );
            }
        }

        private static bool UserHasVerifiedPhone(User user)
        {
            return user.IsApprovedByAdmin &&
                !string.IsNullOrWhiteSpace(user.PhoneNumber);
        }

        private static string MaskPhone(string phoneNumber)
        {
            var digits =
                new string(
                    phoneNumber
                        .Where(char.IsDigit)
                        .ToArray()
                );

            if (digits.Length < 4)
            {
                return "••••";
            }

            return $"••••{digits[^4..]}";
        }

        private static string? GetMaskedPhoneIfVerified(User user)
        {
            return UserHasVerifiedPhone(user)
                ? MaskPhone(user.PhoneNumber)
                : null;
        }

        private async Task SendOtpAsync(
            User user,
            string channel
        )
        {
            var oldOtps =
                await _context.OtpVerifications
                    .Where(x =>
                        x.Email == user.Email &&
                        x.IsUsed == false
                    )
                    .ToListAsync();

            if (oldOtps.Any())
            {
                _context.OtpVerifications
                    .RemoveRange(oldOtps);
            }

            string otpCode;
            if (channel == OtpVerification.ChannelSms)
            {
                await _smsService.SendOtpSmsAsync(
                    user.PhoneNumber
                );
                otpCode = OtpVerification.TwilioManagedCode;
            }
            else
            {
                otpCode =
                    new Random()
                        .Next(100000, 999999)
                        .ToString();
            }

            var otpRecord =
                new OtpVerification
                {
                    UserId = user.Id,
                    Email = user.Email,
                    OtpCode = otpCode,
                    Channel = channel,
                    IsUsed = false,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(10)
                };

            await _context.OtpVerifications
                .AddAsync(otpRecord);

            await _context.SaveChangesAsync();

            if (channel == OtpVerification.ChannelEmail)
            {
                await _emailService.SendOtpEmailAsync(
                    user.Email,
                    otpCode
                );
            }
        }

        private async Task SendSignInOtpAsync(User user)
        {
            await SendOtpAsync(
                user,
                OtpVerification.ChannelEmail
            );
        }

        /*
         =========================================
         FORGOT PASSWORD
         =========================================
        */

        public async Task ForgotPasswordAsync(
            ForgotPasswordDto dto
        )
        {
            dto.Email =
                dto.Email.Trim().ToLower();

            /*
             =========================================
             FIND USER
             =========================================
            */

            var user =
                await _context.Users
                    .FirstOrDefaultAsync(x =>
                        x.Email == dto.Email
                    );

            if (user == null)
            {
                throw new Exception(
                    "Email not found."
                );
            }

            /*
             =========================================
             REMOVE OLD RESET TOKENS
             =========================================
            */

            var oldTokens =
                await _context.PasswordResets
                    .Where(x =>
                        x.UserId == user.Id &&
                        x.IsUsed == false
                    )
                    .ToListAsync();

            if (oldTokens.Any())
            {
                _context.PasswordResets
                    .RemoveRange(oldTokens);
            }

            /*
             =========================================
             GENERATE RESET TOKEN
             =========================================
            */

            var resetToken =
                Guid.NewGuid().ToString("N");

            /*
             =========================================
             CREATE RESET RECORD
             =========================================
            */

            var passwordReset =
                new PasswordReset
                {
                    UserId =
                        user.Id,

                    ResetToken =
                        resetToken,

                    IsUsed =
                        false,

                    ExpiryTime =
                        DateTime.UtcNow
                            .AddMinutes(30),

                    CreatedAt =
                        DateTime.UtcNow
                };

            /*
             =========================================
             SAVE RESET TOKEN
             =========================================
            */

            await _context.PasswordResets
                .AddAsync(passwordReset);

            await _context.SaveChangesAsync();

            var frontendBaseUrl =
                _configuration["Frontend:BaseUrl"]
                ?? throw new InvalidOperationException(
                    "Frontend:BaseUrl is not configured."
                );

            var resetLink =
                $"{frontendBaseUrl.TrimEnd('/')}/reset-password?token={resetToken}";

            /*
             =========================================
             SEND RESET EMAIL
             =========================================
            */

            await _emailService.SendResetPasswordEmailAsync(
                user.Email,
                resetLink
            );
        }

        /*
         =========================================
         RESET PASSWORD
         =========================================
        */

        public async Task ResetPasswordAsync(
            ResetPasswordDto dto
        )
        {
            /*
             =========================================
             PASSWORD MATCH CHECK
             =========================================
            */

            if (dto.NewPassword != dto.ConfirmPassword)
            {
                throw new Exception(
                    "Passwords do not match."
                );
            }

            /*
             =========================================
             FIND RESET TOKEN
             =========================================
            */

            var resetRecord =
                await _context.PasswordResets
                    .FirstOrDefaultAsync(x =>
                        x.ResetToken == dto.Token &&
                        x.IsUsed == false
                    );

            if (resetRecord == null)
            {
                throw new Exception(
                    "Invalid reset token."
                );
            }

            /*
             =========================================
             CHECK EXPIRY
             =========================================
            */

            if (resetRecord.ExpiryTime < DateTime.UtcNow)
            {
                throw new Exception(
                    "Reset token expired."
                );
            }

            /*
             =========================================
             FIND USER
             =========================================
            */

            var user =
                await _context.Users
                    .FirstOrDefaultAsync(x =>
                        x.Id == resetRecord.UserId
                    );

            if (user == null)
            {
                throw new Exception(
                    "User not found."
                );
            }



            /*
             =========================================
             UPDATE PASSWORD
             =========================================
            */

            user.PasswordHash =
                BCrypt.Net.BCrypt.HashPassword(
                    dto.NewPassword
                );

            /*
             =========================================
             MARK TOKEN USED
             =========================================
            */

            resetRecord.IsUsed = true;

            await _context.SaveChangesAsync();

            var firstName =
                SignInMetadataResolver.ExtractFirstName(user.FullName);

            await _emailService.SendPasswordChangedEmailAsync(
                user.Email,
                firstName
            );
        }

        /*
         =========================================
         VERIFY OTP
         =========================================
        */

        public async Task<object> VerifyOtpAsync(
            VerifyOtpDto dto,
            SignInContext? signInContext = null
        )

        {
            dto.Email =
                dto.Email.Trim().ToLower();

            dto.OtpCode =
                dto.OtpCode.Trim();

            var otpRecord =
                await _context.OtpVerifications
                    .Where(x =>
                        x.Email == dto.Email &&
                        x.IsUsed == false &&
                        x.ExpiresAt > DateTime.UtcNow
                    )
                    .OrderByDescending(x => x.CreatedAt)
                    .FirstOrDefaultAsync();

            if (otpRecord == null)
            {
                throw new Exception(
                    "Invalid OTP."
                );
            }

            var user =
                await _context.Users
                    .FirstOrDefaultAsync(x =>
                        x.Email == dto.Email
                    );

            if (user == null)
            {
                throw new Exception(
                    "User not found."
                );
            }

            var isValid =
                otpRecord.Channel == OtpVerification.ChannelSms
                    ? await _smsService.VerifyOtpSmsAsync(
                        user.PhoneNumber,
                        dto.OtpCode
                    )
                    : otpRecord.OtpCode == dto.OtpCode;

            if (!isValid)
            {
                throw new Exception(
                    "Invalid OTP."
                );
            }

            EnsureOperatorCanSignIn(user);

            otpRecord.IsUsed = true;

            var isFirstSignIn = !user.HasCompletedFirstSignIn;

            user.HasCompletedFirstSignIn = true;

            string? issuedDeviceToken = null;

            if (dto.RememberDevice)
            {
                issuedDeviceToken =
                    await TrustedDeviceHelper.IssueTrustedDeviceAsync(
                        _context,
                        user.Id
                    );
            }

            await _context.SaveChangesAsync();

            if (!isFirstSignIn && signInContext != null)
            {
                await SendNewDeviceSignInAlertAsync(user, signInContext);
            }

            var token =
                _jwtService.GenerateToken(
                    user.Id.ToString(),
                    user.Email,
                    user.Role
                );

            return await BuildUserSessionPayloadAsync(
                user,
                token,
                issuedDeviceToken
            );

        }

        private async Task<bool> RequiresWorkspaceSetupAsync(User user)
        {
            if (user.SelectedLocationId != null)
            {
                return false;
            }

            var ownedRestaurantCount =
                await _context.Restaurants
                    .CountAsync(r => r.OwnerUserId == user.Id);

            return ownedRestaurantCount >= 2;
        }

        private async Task<object> BuildUserSessionPayloadAsync(
            User user,
            string token,
            string? deviceToken = null
        )
        {
            var workspaceSetupRequired =
                await RequiresWorkspaceSetupAsync(user);

            var activationRequired =
                ActivationState.RequiresActivation(ActivationSubject.FromUser(user));

            if (deviceToken == null)
            {
                return new
                {
                    token,
                    accountType = user.AccountType,
                    workspaceSetupRequired,
                    selectedLocationId = user.SelectedLocationId,
                    activationRequired,
                    activationExpiresAt = user.ActivationExpiresAt,
                };
            }

            return new
            {
                token,
                accountType = user.AccountType,
                workspaceSetupRequired,
                selectedLocationId = user.SelectedLocationId,
                activationRequired,
                activationExpiresAt = user.ActivationExpiresAt,
                deviceToken,
            };
        }

        private async Task<SessionRoutingFields> BuildSessionRoutingFieldsAsync(
            User user
        )
        {
            return new SessionRoutingFields
            {
                AccountType = user.AccountType,
                WorkspaceSetupRequired =
                    await RequiresWorkspaceSetupAsync(user),
                SelectedLocationId = user.SelectedLocationId,
                ActivationRequired =
                    ActivationState.RequiresActivation(ActivationSubject.FromUser(user)),
                ActivationExpiresAt = user.ActivationExpiresAt,
            };
        }

        public async Task<SessionRoutingFields> GetCurrentUserRoutingAsync(
            int userId
        )
        {
            var user =
                await _context.Users.FirstOrDefaultAsync(x =>
                    x.Id == userId
                );

            if (user == null)
            {
                throw new Exception("User not found.");
            }

            return await BuildSessionRoutingFieldsAsync(user);
        }

        public async Task<SessionRoutingFields> ActivateAccountAsync(
            int userId,
            string activationCode
        )
        {
            var user =
                await _context.Users.FirstOrDefaultAsync(x =>
                    x.Id == userId
                );

            if (user == null)
            {
                throw new Exception("User not found.");
            }

            if (!ActivationState.RequiresActivation(ActivationSubject.FromUser(user)))
            {
                throw new Exception(
                    "This account does not require activation."
                );
            }

            var cacheKey = $"activation-verify:{userId}";

            if (
                _cache.TryGetValue(cacheKey, out int failures)
                && failures >= MaxActivationVerifyAttempts
            )
            {
                throw new Exception(
                    "Too many activation attempts. Try again later."
                );
            }

            var normalizedCode =
                ActivationCodeHelper.Normalize(activationCode);

            if (!ActivationCodeHelper.IsValidFormat(normalizedCode))
            {
                throw new Exception("Enter a valid activation code.");
            }

            if (
                user.ActivationCodeHash == null
                || !ActivationCodeHelper.VerifyCode(
                    normalizedCode,
                    user.ActivationCodeHash
                )
            )
            {
                var nextFailures =
                    _cache.TryGetValue(cacheKey, out failures)
                        ? failures + 1
                        : 1;

                _cache.Set(
                    cacheKey,
                    nextFailures,
                    ActivationVerifyLockout
                );

                throw new Exception(
                    "That activation code is incorrect."
                );
            }

            _cache.Remove(cacheKey);

            var activatedAt = DateTime.UtcNow;
            user.ActivatedAt = activatedAt;
            user.ActivationExpiresAt =
                ActivationCodeHelper.ComputeActivationExpiresAt(
                    activatedAt
                );

            await _context.SaveChangesAsync();

            return await BuildSessionRoutingFieldsAsync(user);
        }

        private void EnsureOperatorCanSignIn(User user)
        {
            var decision = _activationGate.Decide(
                ActivationSubject.FromUser(user),
                ActivationIntent.SignIn
            );

            if (decision.Outcome == ActivationOutcome.Block)
            {
                throw new ActivationExpiredException(decision.Message);
            }
        }

        private object BuildOtpChallengePayload(User user)
        {
            return new
            {
                loginType = "USER",
                otpChannel = OtpVerification.ChannelEmail,
                hasVerifiedPhone = UserHasVerifiedPhone(user),
                maskedPhone = GetMaskedPhoneIfVerified(user),
            };
        }

        private async Task<User> ValidateUserCredentialsAsync(
            UserLoginDto dto
        )
        {
            dto.Email = dto.Email.Trim().ToLower();

            var user =
                await _context.Users
                    .FirstOrDefaultAsync(x =>
                        x.Email == dto.Email
                    );

            if (user == null)
            {
                throw new Exception(
                    "Invalid email or password."
                );
            }

            if (user.IsLocked)
            {
                throw new Exception(
                    "Account is locked."
                );
            }

            bool isPasswordValid =
                BCrypt.Net.BCrypt.Verify(
                    dto.Password,
                    user.PasswordHash
                );

            if (!isPasswordValid)
            {
                user.FailedLoginAttempts++;

                if (user.FailedLoginAttempts >= 5)
                {
                    user.IsLocked = true;
                }

                await _context.SaveChangesAsync();

                throw new Exception(
                    "Invalid email or password."
                );
            }

            user.FailedLoginAttempts = 0;

            await _context.SaveChangesAsync();

            if (!user.IsEmailVerified)
            {
                throw new Exception(
                    "Email is not verified."
                );
            }

            if (!user.IsApprovedByAdmin)
            {
                throw new Exception(
                    "Account is not approved."
                );
            }

            EnsureOperatorCanSignIn(user);

            return user;
        }

        public async Task<object> UniversalLoginAsync(
    UserLoginDto dto
)
        {
            dto.Email = dto.Email.Trim().ToLower();

            var admin = await _context.Admins
                .FirstOrDefaultAsync(x => x.Email == dto.Email);

            if (admin != null)
            {
                await ValidateStaffLoginAsync(admin, dto.Password);

                var loginType =
                    string.Equals(
                        admin.Role,
                        "Support",
                        StringComparison.OrdinalIgnoreCase
                    )
                        ? "SUPPORT"
                        : "ADMIN";

                var token =
                    _jwtService.GenerateAdminToken(admin);

                return new
                {
                    loginType,
                    token,
                };
            }

            var user = await ValidateUserCredentialsAsync(dto);

            var hasValidTrust =
                user.HasCompletedFirstSignIn &&
                await TrustedDeviceHelper.IsTrustedAsync(
                    _context,
                    user.Id,
                    dto.DeviceToken
                );

            if (hasValidTrust)
            {
                var sessionToken =
                    _jwtService.GenerateToken(
                        user.Id.ToString(),
                        user.Email,
                        user.Role
                    );

                var workspaceSetupRequired =
                    await RequiresWorkspaceSetupAsync(user);

                return new
                {
                    loginType = "USER",
                    token = sessionToken,
                    accountType = user.AccountType,
                    workspaceSetupRequired,
                    selectedLocationId = user.SelectedLocationId,
                    activationRequired =
                        ActivationState.RequiresActivation(ActivationSubject.FromUser(user)),
                    activationExpiresAt = user.ActivationExpiresAt,
                };
            }

            await SendSignInOtpAsync(user);

            return BuildOtpChallengePayload(user);
        }

        private async Task SendNewDeviceSignInAlertAsync(
            User user,
            SignInContext signInContext
        )
        {
            try
            {
                var details = await _signInMetadataResolver.ResolveAsync(
                    user,
                    signInContext
                );

                await _emailService.SendNewDeviceSignInEmailAsync(
                    user.Email,
                    details
                );
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to send new device sign-in email to {Email}",
                    user.Email
                );
            }
        }

    }
}
