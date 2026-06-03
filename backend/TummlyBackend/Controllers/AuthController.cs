using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Auth;
using TummlyBackend.DTOs.Trial;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        private readonly ApplicationDbContext _context;

        public AuthController(
            IAuthService authService,
            ApplicationDbContext context
        )
        {
            _authService = authService;

            _context = context;
        }

        /*
         =========================================
         ADMIN LOGIN
         =========================================
        */

        [HttpPost("admin-login")]
        public async Task<IActionResult>
            AdminLogin(
                AdminLoginDto dto
            )
        {
            var token =
                await _authService
                    .AdminLoginAsync(dto);

            return Ok(new
            {
                success = true,

                message =
                    "Admin login successful.",

                token
            });
        }



        /*
         =========================================
         USER LOGIN
         =========================================
        */

        [HttpPost("login")]
        public async Task<IActionResult>
 UserLogin(
     UserLoginDto dto
 )
        {
            try
            {
                await _authService
                    .UserLoginAsync(dto);

                return Ok(new
                {
                    success = true,
                    message = "OTP sent to email."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpPost("universal-login")]
        public async Task<IActionResult> UniversalLogin(
     UserLoginDto dto
 )
        {
            try
            {
                var result =
                    await _authService
                        .UniversalLoginAsync(dto);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }
        /*
         =========================================
         SETUP ACCOUNT
         =========================================
        */

        [HttpPost("setup-account")]
        public async Task<IActionResult>
            SetupAccount(
                SetupAccountDto dto
            )
        {
            /*
             =========================================
             PASSWORD MATCH CHECK
             =========================================
            */
            if (dto.Password != dto.ConfirmPassword)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Passwords do not match."
                });
            }

            if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 8)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Password must be at least 8 characters."
                });
            }

            if (dto.Password != dto.ConfirmPassword)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Passwords do not match."
                });
            }

            if (dto.Password.Length < 8)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Password must be at least 8 characters."
                });
            }

            if (string.IsNullOrWhiteSpace(dto.GroupName))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Group name is required."
                });
            }

            if (string.IsNullOrWhiteSpace(dto.BusinessCategory))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Business category is required."
                });
            }

            if (string.IsNullOrWhiteSpace(dto.PrimaryPhone))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Primary phone is required."
                });
            }

            if (dto.Locations == null || !dto.Locations.Any())
            {
                return BadRequest(new
                {
                    success = false,
                    message = "At least one location is required."
                });
            }

            /*
             =========================================
             FIND TRIAL REQUEST
             =========================================
            */

            var trialRequest =
                await _context
                    .TrialRequests
                    .FirstOrDefaultAsync(x =>
                        x.ApprovalToken ==
                            dto.Token
                    );

            /*
             =========================================
             INVALID TOKEN
             =========================================
            */

            if (trialRequest == null)
            {
                return BadRequest(new
                {
                    success = false,

                    message =
                        "Invalid invite token."
                });
            }

            /*
             =========================================
             EXPIRED TOKEN
             =========================================
            */

            if (
                trialRequest.InviteExpiresAt
                    < DateTime.UtcNow
            )
            {
                return BadRequest(new
                {
                    success = false,

                    message =
                        "Invite link expired."
                });
            }

            /*
             =========================================
             ACCOUNT ALREADY CREATED
             =========================================
            */

            if (trialRequest.IsAccountCreated)
            {
                return BadRequest(new
                {
                    success = false,

                    message =
                        "Account already created."
                });
            }

            /*
             =========================================
             CHECK USER ALREADY EXISTS
             =========================================
            */

            var existingUser =
                await _context.Users
                    .FirstOrDefaultAsync(x =>
                        x.Email ==
                        trialRequest.Email
                    );

            if (existingUser != null)
            {
                return BadRequest(new
                {
                    success = false,

                    message =
                        "User already exists."
                });
            }

            /*
             =========================================
             CREATE USER
             =========================================
            */

            var user = new User
            {
                FullName =
                    trialRequest.FullName,

                Email =
                    trialRequest.Email,

                PasswordHash =
                    BCrypt.Net.BCrypt.HashPassword(
                        dto.Password
                    ),

                PhoneNumber =
                    trialRequest.Mobile,

                Role =
                    trialRequest.Role,

                AccountType =
                    trialRequest.AccountType,

                IsEmailVerified = true,

                IsApprovedByAdmin = true,

                IsLocked = false,

                FailedLoginAttempts = 0
            };

            /*
             =========================================
             SAVE USER
             =========================================
            */

            _context.Users.Add(user);

            /*
             =========================================
             UPDATE TRIAL REQUEST
             =========================================
            */

            trialRequest.IsAccountCreated =
                true;

            trialRequest.Status =
                "Account Created";

            /*
             =========================================
             SAVE DATABASE
             =========================================
            */

            await _context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = dto.GroupName,

                AccountType = trialRequest.AccountType,

                OwnerUserId = user.Id,

                BusinessCategory = dto.BusinessCategory,

                BusinessLink = dto.BusinessLink,

                PublicPhoneNumber = dto.PrimaryPhone,

                CreatedAt = DateTime.UtcNow
            };
            _context.Restaurants.Add(restaurant);

            await _context.SaveChangesAsync();

            foreach (var item in dto.Locations)
            {
                var location = new RestaurantLocation
                {
                    RestaurantId = restaurant.Id,

                    LocationName = item.LocationName ?? "",

                    Address = item.Address ?? "",

                    Postcode = item.Postcode,

                    LocationPhone = item.LocationPhone,

                    LocalContact = item.LocalContact,

                    IncludeInRollout = item.IncludeInRollout,

                    CreatedAt = DateTime.UtcNow
                };

                _context.RestaurantLocations.Add(location);
            }

            await _context.SaveChangesAsync();

            var guestLoop = new GuestLoopSetup
            {
                RestaurantId = restaurant.Id,

                SendPhysicalQrMaterials = false,

                AutoSendReviewRequests = true,

                Touchpoints = dto.RolloutApproach,

                ThankYouMessage = dto.ThankYouMessage,

                OfferHeadline = dto.OfferTitle,

                OfferDetails = dto.OfferMessage,

                OfferExpiry = dto.OfferExpiry,

                OfferRedemption = dto.RedemptionMethod,

                OfferUsageLimit = dto.UsageLimit,

                CreatedAt = DateTime.UtcNow
            };


            _context.GuestLoopSetups.Add(guestLoop);

            await _context.SaveChangesAsync();

            /*
             =========================================
             SUCCESS RESPONSE
             =========================================
            */

            return Ok(new
            {
                success = true,

                message =
                    "Account setup successful."
            });
        }

        /*
         =========================================
         VALIDATE INVITE TOKEN
         =========================================
        */

        [HttpPost("validate-invite")]
        public async Task<IActionResult>
            ValidateInvite(
                ValidateInviteDto dto
            )
        {
            /*
             =========================================
             FIND TRIAL REQUEST
             =========================================
            */

            var trialRequest =
                await _context
                    .TrialRequests
                    .FirstOrDefaultAsync(x =>
                        x.ApprovalToken ==
                            dto.Token
                    );

            /*
             =========================================
             INVALID TOKEN
             =========================================
            */

            if (trialRequest == null)
            {
                return BadRequest(new
                {
                    success = false,

                    message =
                        "Invalid invite token."
                });
            }

            /*
             =========================================
             EXPIRED TOKEN
             =========================================
            */

            if (
                trialRequest.InviteExpiresAt
                    < DateTime.UtcNow
            )
            {
                return BadRequest(new
                {
                    success = false,

                    message =
                        "Invite link expired."
                });
            }

            /*
             =========================================
             ACCOUNT ALREADY CREATED
             =========================================
            */

            if (trialRequest.IsAccountCreated)
            {
                return BadRequest(new
                {
                    success = false,

                    message =
                        "Account already created."
                });
            }

            /*
             =========================================
             SUCCESS RESPONSE
             =========================================
            */

            return Ok(new
            {
                success = true,

                businessName =
                    trialRequest.BusinessName,

                fullName =
                    trialRequest.FullName,

                email =
                    trialRequest.Email,

                accountType =
                    trialRequest.AccountType,

                role =
                    trialRequest.Role
            });
        }

        /*
         =========================================
         CURRENT USER
         =========================================
        */

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult>
            GetCurrentUser()
        {
            /*
             =========================================
             GET USER ID FROM JWT
             =========================================
            */

            var userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier
                );

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new
                {
                    success = false,

                    message =
                        "Invalid token."
                });
            }

            /*
             =========================================
             FIND USER
             =========================================
            */

            var user =
                await _context.Users
                    .FirstOrDefaultAsync(x =>
                        x.Id.ToString() ==
                            userId
                    );

            if (user == null)
            {
                return Unauthorized(new
                {
                    success = false,

                    message =
                        "User not found."
                });
            }

            /*
             =========================================
             SUCCESS RESPONSE
             =========================================
            */

            return Ok(new
            {
                success = true,

                data = new
                {
                    user.Id,
                    user.FullName,
                    user.Email,
                    user.Role,
                    user.AccountType
                }
            });
        }

        /*
 =========================================
 FORGOT PASSWORD
 =========================================
*/

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(
            [FromBody] ForgotPasswordDto dto
        )
        {
            try
            {
                await _authService.ForgotPasswordAsync(dto);

                return Ok(new
                {
                    success = true,
                    message = "Reset link sent successfully."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        /*
         =========================================
         RESET PASSWORD
         =========================================
        */

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(
             [FromBody] ResetPasswordDto dto
        )
        {
            try
            {
                await _authService.ResetPasswordAsync(dto);

                return Ok(new
                {
                    success = true,
                    message = "Password reset successful."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }
        /*[HttpPost("setup-account")]

 =========================================
 VERIFY OTP
 =========================================
*/

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp(
            [FromBody] VerifyOtpDto dto
        )
        {
            try
            {
                var result =
                    await _authService
                        .VerifyOtpAsync(dto);

                return Ok(new
                {
                    success = true,
                    message = "OTP verified successfully.",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }


        }
        [HttpPost("complete-setup")]
        public async Task<IActionResult> CompleteSetup(
    CompleteSetupDto dto
)
        {
            try
            {
                await _authService
                    .CompleteAccountSetupAsync(dto);

                return Ok(new
                {
                    success = true,
                    message = "Account setup completed successfully."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }
    }
}