using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TummlyBackend.Configurations;
using TummlyBackend.DTOs.Auth;
using TummlyBackend.DTOs.Signup;
using TummlyBackend.Interfaces;
using TummlyBackend.Services;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SignupController : ControllerBase
    {
        private const string CodeSentMessage =
            "If this email can be used, we sent a 6-digit code.";

        private const string SignupErrorMessage = "Something went wrong. Try again.";

        /// <summary>The header that carries the signup session token. Query strings never carry it.</summary>
        private const string SessionHeader = "X-Signup-Session";

        private const string QueryTokenName = "sessionToken";

        private readonly ISignupService _signupService;
        private readonly ILogger<SignupController> _logger;

        public SignupController(
            ISignupService signupService,
            ILogger<SignupController> logger
        )
        {
            _signupService = signupService;
            _logger = logger;
        }

        [EnableRateLimiting(SignupRateLimitSettings.PolicyName)]
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
                        code = "code_sent",
                        message = CodeSentMessage,
                        data = result,
                    }
                );
            }
            catch (Exception ex)
            {
                return ToError(ex);
            }
        }

        [EnableRateLimiting(SignupRateLimitSettings.PolicyName)]
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
                return ToError(ex);
            }
        }

        [EnableRateLimiting(SignupRateLimitSettings.PolicyName)]
        [HttpPost("resend-otp")]
        public async Task<IActionResult> ResendOtp(
            [FromBody] ResendOtpDto dto
        )
        {
            try
            {
                var result = await _signupService.ResendOtpAsync(dto.Email);

                return Ok(
                    new
                    {
                        success = true,
                        code = "code_sent",
                        message = CodeSentMessage,
                        data = result,
                    }
                );
            }
            catch (Exception ex)
            {
                return ToError(ex);
            }
        }

        [EnableRateLimiting(SignupRateLimitSettings.TokenPolicyName)]
        [HttpPost("onboarding")]
        public async Task<IActionResult> SaveOnboarding(
            [FromHeader(Name = SessionHeader)] Guid? signupSession,
            [FromBody] SaveSignupOnboardingDto dto
        )
        {
            if (QueryTokenPresent() || !TryReadSessionHeader(signupSession, out var sessionToken))
            {
                return SessionExpired();
            }

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
                return ToError(ex);
            }
        }

        [EnableRateLimiting(SignupRateLimitSettings.TokenPolicyName)]
        [HttpGet("session")]
        public async Task<IActionResult> GetSession(
            [FromHeader(Name = SessionHeader)] Guid? signupSession
        )
        {
            if (QueryTokenPresent() || !TryReadSessionHeader(signupSession, out var sessionToken))
            {
                return SessionExpired();
            }

            try
            {
                var result = await _signupService.GetBySessionAsync(sessionToken);

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
                return ToError(ex);
            }
        }

        [EnableRateLimiting(SignupRateLimitSettings.TokenPolicyName)]
        [HttpPost("retry-provision")]
        public async Task<IActionResult> RetryProvision(
            [FromHeader(Name = SessionHeader)] Guid? signupSession
        )
        {
            if (QueryTokenPresent() || !TryReadSessionHeader(signupSession, out var sessionToken))
            {
                return SessionExpired();
            }

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
                return ToError(ex);
            }
        }

        [EnableRateLimiting(SignupRateLimitSettings.TokenPolicyName)]
        [HttpGet("provisioning-status")]
        public async Task<IActionResult> GetProvisioningStatus(
            [FromHeader(Name = SessionHeader)] Guid? signupSession
        )
        {
            if (QueryTokenPresent() || !TryReadSessionHeader(signupSession, out var sessionToken))
            {
                return SessionExpired();
            }

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
                return ToError(ex);
            }
        }

        [EnableRateLimiting(SignupRateLimitSettings.TokenPolicyName)]
        [HttpPost("session-login")]
        public async Task<IActionResult> SessionLogin(
            [FromBody] SignupSessionLoginDto dto
        )
        {
            if (QueryTokenPresent())
            {
                return SessionExpired();
            }

            try
            {
                var data = await _signupService.SessionLoginAsync(
                    dto.SessionToken,
                    dto.ContinueOnFree
                );
                return Ok(
                    new
                    {
                        success = true,
                        message = "Signed in.",
                        data,
                    }
                );
            }
            catch (Exception ex)
            {
                return ToError(ex);
            }
        }

        /// <summary>
        /// The one mapper for every signup action. Only signup-owned refusals reach the body;
        /// anything else is logged and returned as a fixed 500 with no exception text.
        /// </summary>
        private IActionResult ToError(Exception ex)
        {
            switch (ex)
            {
                case SignupFlowException flow:
                    return FlowError(flow.StatusCode, flow.Code, flow.Message, flow.RetryAfterUtc);
                case SignupConflictException conflict:
                    return FlowError(StatusCodes.Status409Conflict, conflict.Code, conflict.Message, null);
                case CustomPlanQuoteException quote:
                    return FlowError(StatusCodes.Status400BadRequest, quote.Code, quote.Message, null);
                default:
                    // Log the exception for operators. Never put its message in the response.
                    _logger.LogError(ex, "signup.unexpected_error");
                    return FlowError(
                        StatusCodes.Status500InternalServerError,
                        "signup_error",
                        SignupErrorMessage,
                        null
                    );
            }
        }

        private IActionResult FlowError(
            int statusCode,
            string code,
            string message,
            DateTime? retryAfterUtc
        )
        {
            var body = new Dictionary<string, object?>
            {
                ["success"] = false,
                ["code"] = code,
                ["message"] = message,
            };
            if (retryAfterUtc is { } retryAt)
            {
                body["retryAfterUtc"] = retryAt;
            }

            return StatusCode(statusCode, body);
        }

        private IActionResult SessionExpired() =>
            FlowError(
                StatusCodes.Status401Unauthorized,
                SignupService.SessionExpiredCode,
                SignupService.SessionExpiredMessage,
                null
            );

        /// <summary>A token sent in the query string is refused, even when the header is also valid.</summary>
        private bool QueryTokenPresent()
        {
            if (!Request.Query.ContainsKey(QueryTokenName))
            {
                return false;
            }

            _logger.LogInformation("signup.token_rejected reason=query_string");
            return true;
        }

        private bool TryReadSessionHeader(Guid? header, out Guid sessionToken)
        {
            if (header is { } value && value != Guid.Empty)
            {
                sessionToken = value;
                return true;
            }

            _logger.LogInformation("signup.token_rejected reason=missing");
            sessionToken = Guid.Empty;
            return false;
        }
    }
}
