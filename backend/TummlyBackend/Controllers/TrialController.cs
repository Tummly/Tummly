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

                if (!result)
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
                    message = "Email verified successfully."
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

        /*
         =========================================
         VALIDATE SETUP TOKEN
         =========================================
        */
        [HttpGet("validate-setup-token")]
        public async Task<IActionResult> ValidateSetupToken(
    [FromQuery] string token
)
        {
            try
            {
                Console.WriteLine($"TOKEN RECEIVED: {token}");

                var result =
                    await _trialService
                        .ValidateSetupTokenAsync(token);

                Console.WriteLine("TOKEN VALID SUCCESS");

                return Ok(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");

                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        /*
         =========================================
         COMPLETE ACCOUNT SETUP
         =========================================
        */

        [HttpPost("complete-setup")]
        public async Task<IActionResult> CompleteSetup(
     [FromBody] CompleteSetupDto dto
 )
        {
            try
            {
                /*
                 =========================================
                 MODEL VALIDATION
                 =========================================
                */

                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Validation failed.",
                        errors = ModelState
                            .Where(x => x.Value.Errors.Count > 0)
                            .Select(x => new
                            {
                                field = x.Key,
                                errors = x.Value.Errors
                                    .Select(e => e.ErrorMessage)
                            })
                    });
                }

                /*
                 =========================================
                 CALL SERVICE
                 =========================================
                */

                var result =
                    await _trialService
                        .CompleteAccountSetupAsync(dto);

                /*
                 =========================================
                 SUCCESS
                 =========================================
                */

                return Ok(new
                {
                    success = true,
                    message = "Account setup completed successfully."
                });
            }
            catch (Exception ex)
            {
                /*
                 =========================================
                 SHOW REAL DATABASE ERROR
                 =========================================
                */

                return BadRequest(new
                {
                    success = false,

                    message = ex.Message,

                    innerError =
                        ex.InnerException?.Message,

                    stackTrace =
                        ex.StackTrace
                });
            }
        }
    }
}