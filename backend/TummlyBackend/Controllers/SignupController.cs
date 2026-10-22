using Microsoft.AspNetCore.Mvc;
using TummlyBackend.DTOs.Auth;
using TummlyBackend.DTOs.Signup;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SignupController : ControllerBase
    {
        private readonly ISignupService _signupService;

        public SignupController(ISignupService signupService)
        {
            _signupService = signupService;
        }

        [HttpPost("start")]
        public async Task<IActionResult> Start([FromBody] StartSignupDto dto)
        {
            try
            {
                var result = await _signupService.StartAsync(dto);

                return Ok(
                    new
                    {
                        success = true,
                        message = "OTP sent successfully.",
                        data = result,
                    }
                );
            }
            catch (Exception ex)
            {
                return BadRequest(
                    new { success = false, message = ex.Message }
                );
            }
        }

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp(
            [FromBody] VerifySignupOtpDto dto
        )
        {
            try
            {
                var result = await _signupService.VerifyOtpAsync(dto);

                return Ok(
                    new
                    {
                        success = true,
                        message = "Email verified successfully.",
                        data = result,
                    }
                );
            }
            catch (Exception ex)
            {
                return BadRequest(
                    new { success = false, message = ex.Message }
                );
            }
        }

        [HttpPost("resend-otp")]
        public async Task<IActionResult> ResendOtp(
            [FromBody] ResendOtpDto dto
        )
        {
            try
            {
                await _signupService.ResendOtpAsync(dto.Email);

                return Ok(
                    new
                    {
                        success = true,
                        message = "OTP resent successfully.",
                    }
                );
            }
            catch (Exception ex)
            {
                return BadRequest(
                    new { success = false, message = ex.Message }
                );
            }
        }

        [HttpPost("onboarding")]
        public async Task<IActionResult> SaveOnboarding(
            [FromQuery] Guid sessionToken,
            [FromBody] SaveSignupOnboardingDto dto
        )
        {
            try
            {
                var result = await _signupService.SaveOnboardingAsync(
                    sessionToken,
                    dto
                );

                return Ok(
                    new
                    {
                        success = true,
                        message = "Onboarding saved successfully.",
                        data = result,
                    }
                );
            }
            catch (Exception ex)
            {
                return BadRequest(
                    new { success = false, message = ex.Message }
                );
            }
        }

        [HttpGet("session")]
        public async Task<IActionResult> GetSession(
            [FromQuery] Guid sessionToken
        )
        {
            try
            {
                var result = await _signupService.GetBySessionAsync(
                    sessionToken
                );

                return Ok(
                    new
                    {
                        success = true,
                        message = "Signup session loaded.",
                        data = result,
                    }
                );
            }
            catch (Exception ex)
            {
                return BadRequest(
                    new { success = false, message = ex.Message }
                );
            }
        }

        [HttpPost("retry-provision")]
        public async Task<IActionResult> RetryProvision(
            [FromQuery] Guid sessionToken
        )
        {
            try
            {
                await _signupService.RetryProvisionAsync(sessionToken);

                return Ok(
                    new
                    {
                        success = true,
                        message = "Provisioning restarted.",
                    }
                );
            }
            catch (Exception ex)
            {
                return BadRequest(
                    new { success = false, message = ex.Message }
                );
            }
        }

        [HttpGet("provisioning-status")]
        public async Task<IActionResult> GetProvisioningStatus(
            [FromQuery] Guid sessionToken
        )
        {
            try
            {
                var result = await _signupService.GetProvisioningStatusAsync(
                    sessionToken
                );

                return Ok(
                    new
                    {
                        success = true,
                        message = "Provisioning status loaded.",
                        data = result,
                    }
                );
            }
            catch (Exception ex)
            {
                return BadRequest(
                    new { success = false, message = ex.Message }
                );
            }
        }
    }
}
