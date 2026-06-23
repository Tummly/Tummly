using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [Route("api/address")]
    public class AddressController : ControllerBase
    {
        private readonly IAddressLookupService _addressLookupService;
        private readonly IMemoryCache _cache;
        private readonly IdealPostcodesSettings _settings;

        public AddressController(
            IAddressLookupService addressLookupService,
            IMemoryCache cache,
            IOptions<IdealPostcodesSettings> settings
        )
        {
            _addressLookupService = addressLookupService;
            _cache = cache;
            _settings = settings.Value;
        }

        [HttpGet("suggest")]
        public async Task<IActionResult> Suggest(
            [FromQuery] string? q,
            CancellationToken cancellationToken
        )
        {
            if (IsRateLimited("suggest", _settings.SuggestRateLimitPerWindow))
            {
                return StatusCode(
                    StatusCodes.Status429TooManyRequests,
                    new
                    {
                        success = false,
                        message = "Too many address lookup requests. Please try again shortly.",
                    }
                );
            }

            var query = q?.Trim() ?? string.Empty;

            if (query.Length < 4)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Enter at least 4 characters to search for an address.",
                });
            }

            if (query.Length > 120)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Address search query is too long.",
                });
            }

            try
            {
                var suggestions = await _addressLookupService.SuggestAsync(
                    query,
                    cancellationToken
                );

                return Ok(new
                {
                    success = true,
                    suggestions,
                });
            }
            catch (Exception)
            {
                return StatusCode(
                    StatusCodes.Status502BadGateway,
                    new
                    {
                        success = false,
                        message = "Unable to fetch address suggestions right now.",
                    }
                );
            }
        }

        [HttpGet("resolve-suggestion")]
        public async Task<IActionResult> ResolveSuggestion(
            [FromQuery] string? id,
            CancellationToken cancellationToken
        )
        {
            if (IsRateLimited("resolve-suggestion", _settings.ResolveRateLimitPerWindow))
            {
                return StatusCode(
                    StatusCodes.Status429TooManyRequests,
                    new
                    {
                        success = false,
                        message = "Too many address lookup requests. Please try again shortly.",
                    }
                );
            }

            var suggestionId = id?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(suggestionId))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Address suggestion id is required.",
                });
            }

            if (suggestionId.Length > 200)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Address suggestion id is too long.",
                });
            }

            try
            {
                var result = await _addressLookupService.ResolveSuggestionAsync(
                    suggestionId,
                    cancellationToken
                );

                if (result is null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Address suggestion not found.",
                    });
                }

                return Ok(new
                {
                    success = true,
                    address = result.Address,
                    postcode = result.Postcode,
                });
            }
            catch (Exception)
            {
                return StatusCode(
                    StatusCodes.Status502BadGateway,
                    new
                    {
                        success = false,
                        message = "Unable to resolve this address suggestion right now.",
                    }
                );
            }
        }

        [HttpGet("resolve")]
        public async Task<IActionResult> Resolve(
            [FromQuery] string? postcode,
            [FromQuery] string? addressHint,
            CancellationToken cancellationToken
        )
        {
            if (IsRateLimited("resolve", _settings.ResolveRateLimitPerWindow))
            {
                return StatusCode(
                    StatusCodes.Status429TooManyRequests,
                    new
                    {
                        success = false,
                        message = "Too many postcode lookup requests. Please try again shortly.",
                    }
                );
            }

            var normalizedPostcode = postcode?.Trim() ?? string.Empty;

            if (!UkPostcode.IsValidFormat(normalizedPostcode))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Please enter a valid UK postcode.",
                });
            }

            try
            {
                var result = await _addressLookupService.ResolvePostcodeAsync(
                    normalizedPostcode,
                    addressHint?.Trim(),
                    cancellationToken
                );

                if (result is null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Postcode not found.",
                    });
                }

                return Ok(new
                {
                    success = true,
                    postcode = result.Postcode,
                    address = result.Address,
                    premises = result.Premises,
                    multiplePremises = result.MultiplePremises,
                    usedBestMatch = result.UsedBestMatch,
                });
            }
            catch (Exception)
            {
                return StatusCode(
                    StatusCodes.Status502BadGateway,
                    new
                    {
                        success = false,
                        message = "Unable to resolve this postcode right now.",
                    }
                );
            }
        }

        private bool IsRateLimited(string endpoint, int maxRequests)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var key = $"address_rate:{endpoint}:{ip}";
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
