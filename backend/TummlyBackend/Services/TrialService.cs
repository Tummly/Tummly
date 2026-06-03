
using Microsoft.EntityFrameworkCore;
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

        public TrialService(
            ApplicationDbContext context,
            IEmailService emailService
        )
        {
            _context = context;

            _emailService = emailService;
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
                dto.BusinessName.Trim().ToLower();

            dto.BusinessLink =
                dto.BusinessLink?.Trim().ToLower();

            dto.Mobile =
                dto.Mobile.Trim();

            var verifiedTrial = await _context
                .TrialRequests
                .FirstOrDefaultAsync(x =>
                    x.Email == dto.Email &&
                    x.IsEmailVerified == true
                );

            if (verifiedTrial != null)
            {
                throw new Exception(
                    "This email is already registered."
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

        public async Task<bool> VerifyOtpAsync(
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
                return false;
            }

            if (otpRecord.ExpiresAt < DateTime.UtcNow)
            {
                return false;
            }

            otpRecord.IsUsed = true;

            var pendingTrial = await _context
                .PendingTrialRequests
                .FirstOrDefaultAsync(x =>
                    x.Email == dto.Email
                );

            if (pendingTrial == null)
            {
                return false;
            }

            var existingTrial = await _context
                .TrialRequests
                .FirstOrDefaultAsync(x =>
                    x.Email == dto.Email
                );

            if (existingTrial != null)
            {
                throw new Exception(
                    "Email already verified."
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

                    Role =
                        pendingTrial.Role,

                    Goal =
                        pendingTrial.Goal,

                    TermsAccepted =
                        pendingTrial.TermsAccepted,

                    IsEmailVerified = true,

                    IsApproved = false,

                    Status = "Email Verified",

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

            return true;
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

        /*
         =========================================
         VALIDATE SETUP TOKEN
         =========================================
        */

        public async Task<object> ValidateSetupTokenAsync(
            string token
        )
        {
            try
            {
                Console.WriteLine("=================================");
                Console.WriteLine($"TOKEN RECEIVED: {token}");

                if (string.IsNullOrWhiteSpace(token))
                {
                    throw new Exception(
                        "Setup token is missing."
                    );
                }

                token = token.Trim();

                var trialRequest = await _context
                    .TrialRequests
                    .FirstOrDefaultAsync(x =>
                        x.ApprovalToken != null &&
                        x.ApprovalToken.Trim() == token
                    );

                Console.WriteLine(
                    $"TRIAL REQUEST FOUND: {trialRequest != null}"
                );

                if (trialRequest == null)
                {
                    throw new Exception(
                        "Invalid setup link or token does not exist."
                    );
                }

                Console.WriteLine(
                    $"IsApproved: {trialRequest.IsApproved}"
                );

                Console.WriteLine(
                    $"IsAccountCreated: {trialRequest.IsAccountCreated}"
                );

                Console.WriteLine(
                    $"InviteExpiresAt: {trialRequest.InviteExpiresAt}"
                );

                if (!trialRequest.IsApproved)
                {
                    throw new Exception(
                        "Request not approved yet."
                    );
                }

                if (trialRequest.IsAccountCreated)
                {
                    throw new Exception(
                        "This invitation link has already been used."
                    );
                }

                if (
                    trialRequest.InviteExpiresAt.HasValue &&
                    trialRequest.InviteExpiresAt.Value < DateTime.UtcNow
                )
                {
                    throw new Exception(
                        "Invitation link has expired."
                    );
                }

                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(x =>
                        x.Email == trialRequest.Email
                    );

                Console.WriteLine(
                    $"EXISTING USER FOUND: {existingUser != null}"
                );

                if (existingUser != null)
                {
                    throw new Exception(
                        "An account with this email already exists."
                    );
                }

                Console.WriteLine("TOKEN VALID SUCCESS");

                return new
                {
                    success = true,

                    message = "Token is valid.",

                    data = new
                    {
                        trialRequestId =
                            trialRequest.Id,

                        email =
                            trialRequest.Email,

                        businessName =
                            trialRequest.BusinessName,

                        fullName =
                            trialRequest.FullName,

                        accountType =
                            trialRequest.AccountType
                    }
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"VALIDATE TOKEN ERROR: {ex.Message}"
                );

                throw;
            }
        }

        /*
         =========================================
         COMPLETE ACCOUNT SETUP
         =========================================
        */

        public async Task<bool> CompleteAccountSetupAsync(
            CompleteSetupDto dto
        )
        {
            if (dto.Password != dto.ConfirmPassword)
            {
                throw new Exception("Passwords do not match.");
            }

            if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 8)
            {
                throw new Exception("Password must be at least 8 characters.");
            }

            var trialRequest = await _context
                .TrialRequests
                .FirstOrDefaultAsync(x =>
                    x.ApprovalToken == dto.Token
                );

            if (trialRequest == null)
            {
                throw new Exception(
                    "Invalid setup link or token does not exist."
                );
            }

            if (!trialRequest.IsApproved)
            {
                throw new Exception(
                    "Request not approved yet."
                );
            }

            if (trialRequest.IsAccountCreated)
            {
                throw new Exception(
                    "Account has already been created using this token."
                );
            }

            if (
                trialRequest.InviteExpiresAt.HasValue &&
                trialRequest.InviteExpiresAt.Value < DateTime.UtcNow
            )
            {
                throw new Exception(
                    "Invitation link has expired."
                );
            }

            var existingUser = await _context.Users
                .FirstOrDefaultAsync(x =>
                    x.Email == trialRequest.Email
                );

            if (existingUser != null)
            {
                throw new Exception(
                    "An account with this email already exists."
                );
            }

            string salt = BCrypt.Net.BCrypt.GenerateSalt(12);

            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(
                dto.Password,
                salt
            );

            var newUser = new User
            {
                FullName = trialRequest.FullName,

                Email = trialRequest.Email,

                PasswordHash = hashedPassword,

                PhoneNumber = trialRequest.Mobile,

                Role = "Owner",

                AccountType = trialRequest.AccountType,

                IsEmailVerified = true,

                IsApprovedByAdmin = true,

                CreatedAt = DateTime.UtcNow
            };

            await _context.Users.AddAsync(newUser);
            await _context.SaveChangesAsync();

            var newRestaurant = new Restaurant
            {
                Name = trialRequest.BusinessName,
                AccountType = trialRequest.AccountType,
                OwnerUserId = newUser.Id,
                BusinessCategory = trialRequest.BusinessCategory,
                BusinessLink = trialRequest.BusinessLink,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Restaurants.AddAsync(newRestaurant);
            await _context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = newRestaurant.Id,
                LocationName = trialRequest.BusinessName + " Main Branch",
                Address = "",
                CreatedAt = DateTime.UtcNow
            };

            await _context.RestaurantLocations.AddAsync(location);

            var guestLoop = new GuestLoopSetup
            {
                RestaurantId = newRestaurant.Id,
                SendPhysicalQrMaterials = false,
                AutoSendReviewRequests = true,
                CreatedAt = DateTime.UtcNow
            };

            await _context.GuestLoopSetups.AddAsync(guestLoop);

            trialRequest.IsAccountCreated = true;
            trialRequest.AccountCreatedAt = DateTime.UtcNow;
            trialRequest.Status = "Account Active";

            await _context.SaveChangesAsync();

            return true;
        }
    }
}