using Microsoft.AspNetCore.Mvc;
using TummlyBackend.DTOs.Auth;
using TummlyBackend.DTOs.Trial;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TrialController : ControllerBase
    {
        private readonly ITrialService _trialService;

        public TrialController(
            ITrialService trialService
        )
        {
            _trialService = trialService;
        }

        /*
         =========================================
         REQUEST TRIAL
         =========================================
         */

        [HttpPost("request-trial")]
        public async Task<IActionResult> RequestTrial(
            [FromBody] TrialRequestDto dto
        )
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid form data."
                    });
                }

                var result =
                    await _trialService
                        .CreateTrialRequestAsync(dto);

                return Ok(new
                {
                    success = true,
                    message = "OTP sent successfully.",
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

        /*
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
                    await _trialService
                        .VerifyOtpAsync(dto);

                if (!result.Verified)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid or expired OTP."
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Email verified successfully.",
                    confirmationEmailSent = result.ConfirmationEmailSent,
                    emailWarning = result.ConfirmationEmailSent
                        ? null
                        : "You're verified — your confirmation email may be delayed.",
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
         RESEND OTP
         =========================================
         */

        [HttpPost("resend-otp")]
        public async Task<IActionResult> ResendOtp(
            [FromBody] ResendOtpDto dto
        )
        {
            try
            {
                await _trialService
                    .ResendOtpAsync(dto.Email);

                return Ok(new
                {
                    success = true,
                    message = "OTP resent successfully."
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