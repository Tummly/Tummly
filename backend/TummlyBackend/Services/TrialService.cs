using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Auth;
using TummlyBackend.DTOs.Trial;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public class TrialService : ITrialService
    {
        private readonly IEmailService _emailService;

        private readonly ApplicationDbContext _context;

        private readonly ILogger<TrialService> _logger;

        public TrialService(
            ApplicationDbContext context,
            IEmailService emailService,
            ILogger<TrialService> logger
        )
        {
            _context = context;

            _emailService = emailService;

            _logger = logger;
        }

        /*
         =========================================
         CREATE TRIAL REQUEST
         =========================================
        */

        public async Task<PendingTrialRequest> CreateTrialRequestAsync(
            TrialRequestDto dto
        )
        {
            dto.Email =
                dto.Email.Trim().ToLower();

            dto.BusinessName =
                dto.BusinessName.Trim();

            dto.BusinessLink =
                dto.BusinessLink?.Trim();

            dto.Mobile =
                PhoneNumberHelper.NormalizeOptional(dto.Mobile?.Trim())
                ?? string.Empty;

            dto.MainLocation =
                dto.MainLocation?.Trim() ?? string.Empty;

            dto.TownCity =
                dto.TownCity?.Trim() ?? string.Empty;

            dto.Postcode =
                UkPostcode.IsValidFormat(dto.Postcode)
                    ? UkPostcode.FormatForDisplay(dto.Postcode.Trim())
                    : dto.Postcode?.Trim() ?? string.Empty;

            var existingUser = await _context
                .Users
                .FirstOrDefaultAsync(x =>
                    x.Email == dto.Email
                );

            if (existingUser != null)
            {
                throw new Exception(
                    "Email already in use."
                );
            }

            var verifiedTrial = await _context
                .TrialRequests
                .FirstOrDefaultAsync(x =>
                    x.Email == dto.Email &&
                    x.IsEmailVerified == true
                );

            if (verifiedTrial != null)
            {
                throw new Exception(
                    "Trial request already verified."
                );
            }

            var pendingTrial = await _context
                .PendingTrialRequests
                .FirstOrDefaultAsync(x =>
                    x.Email == dto.Email
                );

            PendingTrialRequest trialRequest;

            if (pendingTrial != null)
            {
                pendingTrial.BusinessName =
                    dto.BusinessName;

                pendingTrial.BusinessCategory =
                    dto.BusinessCategory;

                pendingTrial.Locations =
                    dto.Locations;

                pendingTrial.BusinessLink =
                    dto.BusinessLink;

                pendingTrial.FullName =
                    dto.FullName;

                pendingTrial.Mobile =
                    dto.Mobile;

                pendingTrial.MainLocation =
                    dto.MainLocation;

                pendingTrial.TownCity =
                    dto.TownCity;

                pendingTrial.Postcode =
                    dto.Postcode;

                pendingTrial.Role =
                    dto.Role;

                pendingTrial.Goal =
                    dto.Goal;

                pendingTrial.TermsAccepted =
                    dto.TermsAccepted;

                if (pendingTrial.LastOtpSentAt == default)
                {
                    pendingTrial.LastOtpSentAt =
                        DateTime.UtcNow
                            .AddMinutes(-2);
                }

                if (pendingTrial.OtpResendCount < 0)
                {
                    pendingTrial.OtpResendCount = 0;
                }

                trialRequest = pendingTrial;
            }
            else
            {
                trialRequest =
                    new PendingTrialRequest
                    {
                        BusinessName =
                            dto.BusinessName,

                        BusinessCategory =
                            dto.BusinessCategory,

                        Locations =
                            dto.Locations,

                        BusinessLink =
                            dto.BusinessLink,

                        FullName =
                            dto.FullName,

                        Email =
                            dto.Email,

                        Mobile =
                            dto.Mobile,

                        MainLocation =
                            dto.MainLocation,

                        TownCity =
                            dto.TownCity,

                        Postcode =
                            dto.Postcode,

                        Role =
                            dto.Role,

                        Goal =
                            dto.Goal,

                        TermsAccepted =
                            dto.TermsAccepted,

                        LastOtpSentAt =
                            DateTime.UtcNow
                                .AddMinutes(-2),

                        OtpResendCount = 0,

                        CreatedAt =
                            DateTime.UtcNow
                    };

                await _context
                    .PendingTrialRequests
                    .AddAsync(trialRequest);
            }

            var oldOtps = await _context
                .OtpVerifications
                .Where(x =>
                    x.Email == dto.Email &&
                    x.IsUsed == false
                )
                .ToListAsync();

            if (oldOtps.Any())
            {
                _context
                    .OtpVerifications
                    .RemoveRange(oldOtps);
            }

            var otpCode =
                GenerateOtp.CreateOtp();

            var otpVerification =
                new OtpVerification
                {
                    Email = dto.Email,

                    OtpCode = otpCode,

                    ExpiresAt =
                        DateTime.UtcNow
                            .AddMinutes(10),

                    IsUsed = false,

                    CreatedAt =
                        DateTime.UtcNow
                };

            await _context
                .OtpVerifications
                .AddAsync(otpVerification);

            trialRequest.LastOtpSentAt =
                DateTime.UtcNow;

            await _context
                .SaveChangesAsync();

            await _emailService
                .SendOtpEmailAsync(
                    dto.Email,
                    otpCode
                );

            return trialRequest;
        }

        /*
         =========================================
         VERIFY OTP
         =========================================
        */

        public async Task<TrialVerifyOtpResult> VerifyOtpAsync(
            VerifyOtpDto dto
        )
        {
            dto.Email =
                dto.Email.Trim().ToLower();

            dto.OtpCode =
                dto.OtpCode.Trim();

            var otpRecord = await _context
                .OtpVerifications
                .FirstOrDefaultAsync(x =>
                    x.Email == dto.Email &&
                    x.OtpCode == dto.OtpCode &&
                    x.IsUsed == false
                );

            if (otpRecord == null)
            {
                return new TrialVerifyOtpResult(
                    Verified: false,
                    ConfirmationEmailSent: false
                );
            }

            if (otpRecord.ExpiresAt < DateTime.UtcNow)
            {
                return new TrialVerifyOtpResult(
                    Verified: false,
                    ConfirmationEmailSent: false
                );
            }

            otpRecord.IsUsed = true;

            var pendingTrial = await _context
                .PendingTrialRequests
                .FirstOrDefaultAsync(x =>
                    x.Email == dto.Email
                );

            if (pendingTrial == null)
            {
                return new TrialVerifyOtpResult(
                    Verified: false,
                    ConfirmationEmailSent: false
                );
            }

            var existingTrial = await _context
                .TrialRequests
                .FirstOrDefaultAsync(x =>
                    x.Email == dto.Email
                );

            if (existingTrial != null)
            {
                throw new Exception(
                    "Trial request already verified."
                );
            }

            var verifiedTrial =
                new TrialRequest
                {
                    BusinessName =
                        pendingTrial.BusinessName,

                    BusinessCategory =
                        pendingTrial.BusinessCategory,

                    Locations =
                        pendingTrial.Locations,

                    BusinessLink =
                        pendingTrial.BusinessLink,

                    FullName =
                        pendingTrial.FullName,

                    Email =
                        pendingTrial.Email,

                    Mobile =
                        pendingTrial.Mobile,

                    MainLocation =
                        pendingTrial.MainLocation,

                    TownCity =
                        pendingTrial.TownCity,

                    Postcode =
                        pendingTrial.Postcode,

                    Role =
                        pendingTrial.Role,

                    Goal =
                        pendingTrial.Goal,

                    TermsAccepted =
                        pendingTrial.TermsAccepted,

                    IsEmailVerified = true,

                    IsApproved = false,

                    Status = TrialRequestStatus.EmailVerified,

                    AccountType =
                        pendingTrial.Locations == "1"
                            ? "Single"
                            : "Multi",

                    CreatedAt =
                        DateTime.UtcNow
                };

            await _context
                .TrialRequests
                .AddAsync(verifiedTrial);

            _context
                .PendingTrialRequests
                .Remove(pendingTrial);

            await _context
                .SaveChangesAsync();

            var confirmationEmailSent = await EmailDispatch.TrySendAsync(
                () => _emailService.SendTrialRequestReceivedEmailAsync(
                    verifiedTrial.Email,
                    verifiedTrial.FullName,
                    verifiedTrial.BusinessName
                ),
                _logger,
                "Failed to send Trial request received email for {Email}",
                verifiedTrial.Email
            );

            return new TrialVerifyOtpResult(
                Verified: true,
                ConfirmationEmailSent: confirmationEmailSent
            );
        }

        /*
         =========================================
         RESEND OTP
         =========================================
        */

        public async Task ResendOtpAsync(string email)
        {
            email = email.Trim().ToLower();

            var pendingRequest = await _context
                .PendingTrialRequests
                .FirstOrDefaultAsync(x =>
                    x.Email == email
                );

            if (pendingRequest == null)
            {
                throw new Exception("No pending request found.");
            }

            if (pendingRequest.LastOtpSentAt == null)
            {
                pendingRequest.LastOtpSentAt =
                    DateTime.UtcNow
                        .AddMinutes(-2);
            }

            if (
                pendingRequest.LastOtpSentAt.Value
                    .AddSeconds(60)
                    > DateTime.UtcNow
            )
            {
                throw new Exception(
                    "Please wait before resending OTP."
                );
            }

            if (pendingRequest.OtpResendCount >= 5)
            {
                pendingRequest.IsAbandoned = true;

                await _context.SaveChangesAsync();

                throw new Exception(
                    "OTP resend limit reached."
                );
            }

            var oldOtps = await _context
                .OtpVerifications
                .Where(x =>
                    x.Email == email &&
                    x.IsUsed == false
                )
                .ToListAsync();

            if (oldOtps.Any())
            {
                _context
                    .OtpVerifications
                    .RemoveRange(oldOtps);
            }

            var otpCode =
                GenerateOtp.CreateOtp();

            var otp =
                new OtpVerification
                {
                    Email = email,

                    OtpCode = otpCode,

                    IsUsed = false,

                    ExpiresAt =
                        DateTime.UtcNow
                            .AddMinutes(10),

                    CreatedAt =
                        DateTime.UtcNow
                };

            await _context
                .OtpVerifications
                .AddAsync(otp);

            pendingRequest.OtpResendCount += 1;

            pendingRequest.LastOtpSentAt =
                DateTime.UtcNow;

            await _context
                .SaveChangesAsync();

            await _emailService
                .SendOtpEmailAsync(
                    email,
                    otpCode
                );
        }
    }
}