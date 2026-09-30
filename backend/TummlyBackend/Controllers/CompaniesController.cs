using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/companies")]
    public class CompaniesController : ControllerBase
    {
        private readonly ICompaniesHouseSearchService _companiesHouseSearchService;
        private readonly IMemoryCache _cache;
        private readonly CompaniesHouseSettings _settings;

        public CompaniesController(
            ICompaniesHouseSearchService companiesHouseSearchService,
            IMemoryCache cache,
            IOptions<CompaniesHouseSettings> settings
        )
        {
            _companiesHouseSearchService = companiesHouseSearchService;
            _cache = cache;
            _settings = settings.Value;
        }

        [HttpGet("suggest")]
        public async Task<IActionResult> Suggest(
            [FromQuery] string? q,
            CancellationToken cancellationToken
        )
        {
            if (IsRateLimited(_settings.SuggestRateLimitPerWindow))
            {
                return StatusCode(
                    StatusCodes.Status429TooManyRequests,
                    new
                    {
                        success = false,
                        message =
                            "Too many company search requests. Please try again shortly.",
                    }
                );
            }

            if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            {
                return StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    new
                    {
                        success = false,
                        message = "Company search is temporarily unavailable.",
                    }
                );
            }

            var query = q?.Trim() ?? string.Empty;

            if (query.Length < 4)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Enter at least 4 characters to search for a company.",
                });
            }

            if (query.Length > 120)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Company search query is too long.",
                });
            }

            try
            {
                var suggestions = await _companiesHouseSearchService.SuggestAsync(
                    query,
                    cancellationToken
                );

                return Ok(new
                {
                    success = true,
                    suggestions,
                });
            }
            catch (InvalidOperationException)
            {
                return StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    new
                    {
                        success = false,
                        message = "Company search is temporarily unavailable.",
                    }
                );
            }
            catch (Exception)
            {
                return StatusCode(
                    StatusCodes.Status502BadGateway,
                    new
                    {
                        success = false,
                        message = "Unable to fetch company suggestions right now.",
                    }
                );
            }
        }

        private bool IsRateLimited(int maxRequests)
        {
            var subject =
                User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? HttpContext.Connection.RemoteIpAddress?.ToString()
                ?? "unknown";
            var key = $"companies_rate:suggest:{subject}";
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

                if (timestamps.Count >= maxRequests)
                {
                    return true;
                }

                timestamps.Add(DateTime.UtcNow);
            }

            return false;
        }
    }
}
