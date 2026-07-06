using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;
using TummlyBackend.DTOs.HelpCentre;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [Route("api/help-centre")]
    public class HelpCentreController : ControllerBase
    {
        private readonly IHelpCentreService _helpCentreService;
        private readonly IMemoryCache _cache;
        private readonly HelpCentreSettings _settings;

        public HelpCentreController(
            IHelpCentreService helpCentreService,
            IMemoryCache cache,
            IOptions<HelpCentreSettings> settings
        )
        {
            _helpCentreService = helpCentreService;
            _cache = cache;
            _settings = settings.Value;
        }

        [HttpGet("contact-prefill")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> GetContactPrefill()
        {
            var userId = GetUserId();

            if (userId == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid token.",
                });
            }

            var result = await _helpCentreService.GetContactPrefillAsync(
                userId.Value
            );

            if (result == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "User not found.",
                });
            }

            return Ok(new
            {
                success = true,
                data = result,
            });
        }

        [HttpPost("queries")]
        [AllowAnonymous]
        [RequestSizeLimit(55_000_000)]
        public async Task<IActionResult> CreateQuery(
            [FromForm] CreateHelpCentreQueryDto dto,
            [FromForm] List<IFormFile>? attachments
        )
        {
            if (IsRateLimited())
            {
                return StatusCode(
                    StatusCodes.Status429TooManyRequests,
                    new
                    {
                        success = false,
                        message =
                            "Too many submissions. Please try again later.",
                    }
                );
            }

            int? userId = null;

            if (User.Identity?.IsAuthenticated == true)
            {
                var userIdClaim =
                    User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (
                    int.TryParse(userIdClaim, out var parsedUserId)
                    && string.Equals(
                        User.FindFirstValue(ClaimTypes.Role),
                        "Owner",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    userId = parsedUserId;
                }
            }

            try
            {
                var result = await _helpCentreService.CreateQueryAsync(
                    dto,
                    userId,
                    attachments
                );

                RecordSubmission();

                return Ok(new
                {
                    success = true,
                    data = result,
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message,
                });
            }
        }

        [HttpGet("my-queries")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> ListMyQueries()
        {
            var userId = GetUserId();

            if (userId == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid token.",
                });
            }

            var result = await _helpCentreService.ListMyQueriesAsync(
                userId.Value
            );

            return Ok(new
            {
                success = true,
                data = result,
            });
        }

        [HttpGet("my-queries/{id:int}")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> GetMyQuery(int id)
        {
            var userId = GetUserId();

            if (userId == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid token.",
                });
            }

            var result = await _helpCentreService.GetMyQueryAsync(
                userId.Value,
                id
            );

            if (result == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Query not found.",
                });
            }

            return Ok(new
            {
                success = true,
                data = result,
            });
        }

        [HttpPost("my-queries/{id:int}/replies")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> AddOperatorReply(
            int id,
            [FromBody] OperatorReplyDto dto
        )
        {
            var userId = GetUserId();

            if (userId == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid token.",
                });
            }

            try
            {
                var result = await _helpCentreService.AddOperatorReplyAsync(
                    userId.Value,
                    id,
                    dto
                );

                return Ok(new
                {
                    success = true,
                    data = result,
                });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Query not found.",
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message,
                });
            }
        }

        [HttpGet("my-queries/{queryId:int}/attachments/{attachmentId:int}")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> DownloadMyQueryAttachment(
            int queryId,
            int attachmentId
        )
        {
            var userId = GetUserId();

            if (userId == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid token.",
                });
            }

            try
            {
                var result = await _helpCentreService.GetMyQueryAttachmentAsync(
                    userId.Value,
                    queryId,
                    attachmentId
                );

                if (result == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Attachment not found.",
                    });
                }

                return File(
                    result.Value.Stream,
                    result.Value.ContentType,
                    result.Value.FileName
                );
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    new
                    {
                        success = false,
                        message = ex.Message,
                    }
                );
            }
        }

        private int? GetUserId()
        {
            var userIdClaim =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (
                string.IsNullOrEmpty(userIdClaim)
                || !int.TryParse(userIdClaim, out var userId)
            )
            {
                return null;
            }

            return userId;
        }

        private bool IsRateLimited()
        {
            var ip = GetClientIp();
            var key = $"help_centre_contact:{ip}";
            var window = TimeSpan.FromMinutes(_settings.RateLimitWindowMinutes);

            var timestamps = _cache.GetOrCreate(
                key,
                entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = window;
                    return new List<DateTime>();
                }
            )!;

            lock (timestamps)
            {
                var cutoff = DateTime.UtcNow - window;
                timestamps.RemoveAll(timestamp => timestamp < cutoff);

                return timestamps.Count >= _settings.ContactFormRateLimitPerWindow;
            }
        }

        private void RecordSubmission()
        {
            var ip = GetClientIp();
            var key = $"help_centre_contact:{ip}";
            var window = TimeSpan.FromMinutes(_settings.RateLimitWindowMinutes);

            var timestamps = _cache.GetOrCreate(
                key,
                entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = window;
                    return new List<DateTime>();
                }
            )!;

            lock (timestamps)
            {
                timestamps.Add(DateTime.UtcNow);
            }
        }

        private string GetClientIp()
        {
            var forwardedFor =
                HttpContext.Request.Headers["X-Forwarded-For"]
                    .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(forwardedFor))
            {
                var firstIp = forwardedFor.Split(',')[0].Trim();

                if (!string.IsNullOrEmpty(firstIp))
                {
                    return firstIp;
                }
            }

            return HttpContext.Connection.RemoteIpAddress?.ToString()
                ?? "unknown";
        }
    }
}
