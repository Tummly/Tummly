using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Auth;
using TummlyBackend.DTOs.Trial;
using TummlyBackend.Exceptions;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        private readonly IProvisioningService _provisioningService;

        private readonly ApplicationDbContext _context;

        public AuthController(
            IAuthService authService,
            IProvisioningService provisioningService,
            ApplicationDbContext context
        )
        {
            _authService = authService;
            _provisioningService = provisioningService;
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
        public async Task<IActionResult> SetupAccount([FromBody] CompleteSetupDto dto)
        {
            try
            {
                await _provisioningService.ProvisionAsync(dto);

                return Ok(new
                {
                    success = true,
                    message = "Account setup successful."
                });
            }
            catch (Exception ex)
            {
                return MapProvisioningException(ex);
            }
        }

        /*
         =========================================
         VALIDATE INVITE TOKEN
         =========================================
        */

        [HttpGet("validate-invite")]
        public async Task<IActionResult> ValidateInvite([FromQuery] string token)
        {
            try
            {
                var result =
                    await _provisioningService.ValidateInviteTokenAsync(token);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return MapProvisioningException(ex);
            }
        }

        private IActionResult MapProvisioningException(Exception ex)
        {
            return ex switch
            {
                AccountAlreadyCreatedException =>
                    Conflict(new { success = false, message = ex.Message }),
                InviteTokenExpiredException or
                InviteTokenNotFoundException or
                InviteTokenNotApprovedException =>
                    BadRequest(new { success = false, message = ex.Message }),
                ArgumentException =>
                    BadRequest(new { success = false, message = ex.Message }),
                _ =>
                    BadRequest(new
                    {
                        success = false,
                        message = "Unable to process this setup request."
                    })
            };
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

        [HttpPost("send-otp")]
        public async Task<IActionResult> SendAuthOtp(
            [FromBody] SendAuthOtpDto dto
        )
        {
            try
            {
                var result =
                    await _authService.SendAuthOtpAsync(
                        dto.Email,
                        dto.Purpose
                    );

                return Ok(new
                {
                    success = true,
                    skipped = result.Skipped,
                    otpChannel = result.OtpChannel,
                    maskedPhone = result.MaskedPhone,
                    message = result.Message
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

        [HttpPost("send-otp-sms")]
        public async Task<IActionResult> SendAuthOtpSms(
            [FromBody] ResendOtpDto dto
        )
        {
            try
            {
                var result =
                    await _authService.SendAuthOtpSmsAsync(dto.Email);

                return Ok(new
                {
                    success = true,
                    skipped = result.Skipped,
                    otpChannel = result.OtpChannel,
                    maskedPhone = result.MaskedPhone,
                    message = result.Message
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
    }
}