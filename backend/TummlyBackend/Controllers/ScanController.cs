using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Scan;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [Route("api/scan")]
    public class ScanController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ISmartGuestLinkService _smartGuestLink;
        private readonly IMemoryCache _cache;

        public ScanController(
            ApplicationDbContext context,
            ISmartGuestLinkService smartGuestLink,
            IMemoryCache cache
        )
        {
            _context = context;
            _smartGuestLink = smartGuestLink;
            _cache = cache;
        }

        /*
         =========================================
         GET LOCATION METADATA BY TOKEN
         =========================================
        */

        [HttpGet("{token}")]
        public async Task<IActionResult> GetLocationByToken(
            string token
        )
        {
            var normalizedToken = token?.Trim();

            if (string.IsNullOrWhiteSpace(normalizedToken))
            {
                return NotFound(new
                {
                    success = false,
                    message = "Invalid link."
                });
            }

            var location = await _smartGuestLink.ResolveForGuestAsync(normalizedToken);

            if (location == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Link not found."
                });
            }

            return Ok(new
            {
                success = true,
                restaurantName = location.RestaurantName,
                locationName = location.LocationName
            });
        }

        /*
         =========================================
         SUBMIT FEEDBACK BY TOKEN
         =========================================
        */

        [HttpPost("{token}/feedback")]
        public async Task<IActionResult> SubmitFeedback(
            string token,
            [FromBody] FeedbackSubmissionDto dto
        )
        {
            var normalizedToken = token?.Trim();

            if (string.IsNullOrWhiteSpace(normalizedToken))
            {
                return NotFound(new
                {
                    success = false,
                    message = "Invalid link."
                });
            }

            var location = await _smartGuestLink
                .ResolveLocationForWriteAsync(normalizedToken);

            if (location == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Link not found."
                });
            }

            /*
             =========================================
             FIELD VALIDATION
             =========================================
            */

            if (string.IsNullOrWhiteSpace(dto?.GuestName))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Name is required."
                });
            }

            if (dto!.GuestName.Length > 150)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Name must be 150 characters or fewer."
                });
            }

            if (string.IsNullOrWhiteSpace(dto.GuestContact))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Contact is required."
                });
            }

            if (dto.GuestContact.Length > 100)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Contact must be 100 characters or fewer."
                });
            }

            if (string.IsNullOrWhiteSpace(dto.Comment))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Message is required."
                });
            }

            if (dto.Comment.Length > 1000)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Message must be 1000 characters or fewer."
                });
            }

            /*
             =========================================
             PER-TOKEN RATE LIMIT (10 submissions / hour)
             =========================================
            */

            var rateLimitKey = $"feedback_rate:{normalizedToken}";

            var submissions = _cache.GetOrCreate(
                rateLimitKey,
                entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow =
                        TimeSpan.FromHours(1);

                    return new List<DateTime>();
                }
            );

            if (submissions!.Count >= 10)
            {
                return StatusCode(
                    StatusCodes.Status429TooManyRequests,
                    new
                    {
                        success = false,
                        message = "Too many submissions from this link. Please try again later."
                    }
                );
            }

            submissions.Add(DateTime.UtcNow);

            /*
             =========================================
             CREATE FEEDBACK ROW
             =========================================
            */

            var feedback = new Feedback
            {
                RestaurantLocationId = location.Id,
                GuestName = dto.GuestName.Trim(),
                GuestContact = dto.GuestContact.Trim(),
                ContactType = DetectContactType(
                    dto.GuestContact
                ),
                Comment = dto.Comment.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _context.Feedbacks.Add(feedback);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Feedback submitted successfully."
            });
        }

        /*
         =========================================
         CONTACT TYPE HEURISTIC
         =========================================
        */

        private static ContactType DetectContactType(
            string contact
        )
        {
            var trimmed = contact.Trim();

            if (trimmed.Contains('@'))
            {
                return ContactType.Email;
            }

            var digitsOnly = new string(
                trimmed.Where(char.IsDigit).ToArray()
            );

            if (
                digitsOnly.Length >= 7 &&
                digitsOnly == trimmed.Replace(" ", "")
                                    .Replace("-", "")
                                    .Replace("(", "")
                                    .Replace(")", "")
                                    .Replace("+", "")
            )
            {
                return ContactType.Phone;
            }

            return ContactType.Unknown;
        }
    }
}
