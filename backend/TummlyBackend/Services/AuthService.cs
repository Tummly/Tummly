using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Auth;
using TummlyBackend.DTOs.Trial;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
namespace TummlyBackend.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;

        private readonly IJwtService _jwtService;

        private readonly IEmailService _emailService;

        public AuthService(
    ApplicationDbContext context,
    IJwtService jwtService,
    IEmailService emailService
)
        {
            _context = context;
            _jwtService = jwtService;
            _emailService = emailService;
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

            string otp =
                new Random()
                    .Next(100000, 999999)
                    .ToString();

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

            var otpRecord =
                new OtpVerification
                {
                    UserId = user.Id,
                    Email = user.Email,
                    OtpCode = otp,
                    IsUsed = false,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(10)
                };

            await _context.OtpVerifications
                .AddAsync(otpRecord);

            await _context.SaveChangesAsync();

            await _emailService.SendOtpEmailAsync(
                user.Email,
                otp
            );

            return "OTP_SENT";
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
                    "User not found."
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
                            .AddMinutes(15),

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

            /*
             =========================================
             SEND RESET EMAIL (TEMP CONSOLE)
             =========================================
            */

            Console.WriteLine(
                "RESET LINK: https://yourfrontend.com/reset-password?token="
                + resetToken
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
        }

        /*
         =========================================
         VERIFY OTP
         =========================================
        */

        public async Task<object> VerifyOtpAsync(
            VerifyOtpDto dto
        )

        {
            dto.Email =
                dto.Email.Trim().ToLower();

            var otpRecord =
                await _context.OtpVerifications
                    .FirstOrDefaultAsync(x =>
                        x.Email == dto.Email &&
                       x.OtpCode == dto.OtpCode &&
                        x.IsUsed == false
                    );

            if (otpRecord == null)
            {
                throw new Exception(
                    "Invalid OTP."
                );
            }

            if (
                otpRecord.ExpiresAt <
                DateTime.UtcNow
            )
            {
                throw new Exception(
                    "OTP expired."
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

            otpRecord.IsUsed = true;

            await _context.SaveChangesAsync();

            var token =
                _jwtService.GenerateToken(
                    user.Id.ToString(),
                    user.Email,
                    user.Role
                );

            return new
            {
                Token = token,
                AccountType =
                    user.AccountType


            };

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
                bool adminPasswordValid =
                    BCrypt.Net.BCrypt.Verify(
                        dto.Password,
                        admin.PasswordHash
                    );

                if (!adminPasswordValid)
                {
                    throw new Exception(
                        "Invalid email or password."
                    );
                }

                var token =
                    _jwtService.GenerateAdminToken(admin);

                return new
                {
                    loginType = "ADMIN",
                    token = token
                };
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(x =>
                    x.Email == dto.Email
                );

            if (user == null)
            {
                throw new Exception(
                    "Invalid email or password."
                );
            }

            bool userPasswordValid =
                BCrypt.Net.BCrypt.Verify(
                    dto.Password,
                    user.PasswordHash
                );

            if (!userPasswordValid)
            {
                throw new Exception(
                    "Invalid email or password."
                );
            }

            await UserLoginAsync(dto);

            return new
            {
                loginType = "USER"
            };
        }
        public async Task<bool> CompleteAccountSetupAsync(
    CompleteSetupDto dto
)
        {
            return true;
        }

    }


    }
