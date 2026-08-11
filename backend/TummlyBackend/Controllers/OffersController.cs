using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TummlyBackend.DTOs.Offers;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Services;

namespace TummlyBackend.Controllers
{
    /// <summary>
    /// Offers catalog — create / list / lifecycle for Campaign attach (ticket 22).
    /// Not Feedback /recovery-offers endpoints.
    /// </summary>
    [ApiController]
    [Route("api/offers")]
    [Authorize]
    public class OffersController : ControllerBase
    {
        private readonly IOwnedLocationService _ownedLocation;
        private readonly IOffersCatalogService _offers;
        private readonly IOffersMetricsService _metrics;

        public OffersController(
            IOwnedLocationService ownedLocation,
            IOffersCatalogService offers,
            IOffersMetricsService metrics
        )
        {
            _ownedLocation = ownedLocation;
            _offers = offers;
            _metrics = metrics;
        }

        [HttpGet]
        public async Task<IActionResult> ListOffers(
            [FromQuery] int locationId,
            [FromQuery] string view = "all",
            [FromQuery] string? q = null,
            [FromQuery] string sort = "recent-activity",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = OffersCatalogService.DefaultPageSize,
            [FromQuery] string[]? status = null,
            [FromQuery] string[]? attachSource = null,
            [FromQuery] int utcOffsetMinutes = 0
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var ownedLocation =
                await _ownedLocation.ResolveAsync(userId, locationId);

            var denied =
                OwnedLocationResponses.FromResult(ownedLocation);

            if (denied != null)
            {
                return denied;
            }

            try
            {
                var response = await _offers.ListAsync(
                    new CatalogOffersListQuery
                    {
                        LocationId = locationId,
                        View = view,
                        Q = q,
                        Sort = sort,
                        Page = page,
                        PageSize = pageSize,
                        Status = status ?? Array.Empty<string>(),
                        AttachSource = attachSource ?? Array.Empty<string>(),
                        UtcOffsetMinutes = utcOffsetMinutes,
                    }
                );

                return Ok(new
                {
                    success = true,
                    items = response.Items,
                    totalCount = response.TotalCount,
                    page = response.Page,
                    pageSize = response.PageSize,
                    tabCounts = new
                    {
                        all = response.TabCounts.All,
                        needsAttention = response.TabCounts.NeedsAttention,
                        drafts = response.TabCounts.Drafts,
                        inFlight = response.TabCounts.InFlight,
                        sent = response.TabCounts.Sent,
                    },
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message,
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateOffer(
            [FromBody] CreateCatalogOfferRequest request
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var ownedLocation =
                await _ownedLocation.ResolveAsync(userId, request.LocationId);

            var denied =
                OwnedLocationResponses.FromResult(ownedLocation);

            if (denied != null)
            {
                return denied;
            }

            try
            {
                var offer = await _offers.CreateActiveAsync(request);
                return Ok(new
                {
                    success = true,
                    offer,
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message,
                });
            }
        }

        [HttpGet("{offerId:int}")]
        public async Task<IActionResult> GetOffer(
            int offerId,
            [FromQuery] int utcOffsetMinutes = 0
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var offer = await _offers.GetByIdAsync(offerId, utcOffsetMinutes);
            if (offer == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Offer not found.",
                });
            }

            var ownedLocation =
                await _ownedLocation.ResolveAsync(userId, offer.LocationId);

            var denied =
                OwnedLocationResponses.FromResult(ownedLocation);

            if (denied != null)
            {
                return denied;
            }

            return Ok(new
            {
                success = true,
                offer,
            });
        }

        [HttpGet("{offerId:int}/metrics")]
        public async Task<IActionResult> GetOfferMetrics(
            int offerId,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            if (from == null || to == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "from and to are required.",
                });
            }

            var fromUtc = EnsureUtc(from.Value);
            var toUtc = EnsureUtc(to.Value);

            if (fromUtc >= toUtc)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "from must be before to.",
                });
            }

            var inclusiveCalendarDays = (toUtc.Date - fromUtc.Date).Days;
            if (inclusiveCalendarDays > 180)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Date range cannot exceed 180 days.",
                });
            }

            var offer = await _offers.GetByIdAsync(offerId);
            if (offer == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Offer not found.",
                });
            }

            var ownedLocation =
                await _ownedLocation.ResolveAsync(userId, offer.LocationId);

            var denied =
                OwnedLocationResponses.FromResult(ownedLocation);

            if (denied != null)
            {
                return denied;
            }

            var dto = await _metrics.GetOfferMetricsAsync(
                offerId,
                fromUtc,
                toUtc
            );

            if (dto == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Offer not found.",
                });
            }

            return Ok(new
            {
                success = true,
                claims = dto.Claims,
                redemptions = dto.Redemptions,
                redemptionRate = dto.RedemptionRate,
                expiredUnused = dto.ExpiredUnused,
                failedAttempts = dto.FailedAttempts,
            });
        }

        [HttpPost("{offerId:int}/pause")]
        public Task<IActionResult> PauseOffer(int offerId)
            => ExecuteLifecycleAsync(
                offerId,
                (id, ct) => _offers.PauseAsync(id, cancellationToken: ct)
            );

        [HttpPost("{offerId:int}/resume")]
        public Task<IActionResult> ResumeOffer(int offerId)
            => ExecuteLifecycleAsync(
                offerId,
                (id, ct) => _offers.ResumeAsync(id, cancellationToken: ct)
            );

        [HttpPost("{offerId:int}/archive")]
        public Task<IActionResult> ArchiveOffer(int offerId)
            => ExecuteLifecycleAsync(
                offerId,
                (id, ct) => _offers.ArchiveAsync(id, cancellationToken: ct)
            );

        [HttpPost("{offerId:int}/duplicate")]
        public Task<IActionResult> DuplicateOffer(int offerId)
            => ExecuteLifecycleAsync(
                offerId,
                (id, ct) => _offers.DuplicateAsync(id, cancellationToken: ct)
            );

        private async Task<IActionResult> ExecuteLifecycleAsync(
            int offerId,
            Func<int, CancellationToken, Task<CatalogOfferLifecycleResult>> action
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var existing = await _offers.GetByIdAsync(offerId);
            if (existing == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Offer not found.",
                });
            }

            var ownedLocation =
                await _ownedLocation.ResolveAsync(userId, existing.LocationId);

            var denied =
                OwnedLocationResponses.FromResult(ownedLocation);

            if (denied != null)
            {
                return denied;
            }

            var result = await action(offerId, CancellationToken.None);

            return result switch
            {
                CatalogOfferLifecycleResult.Ok ok => Ok(new
                {
                    success = true,
                    offer = ok.Offer,
                }),
                CatalogOfferLifecycleResult.Duplicated duplicated => Ok(new
                {
                    success = true,
                    offer = duplicated.Offer,
                }),
                CatalogOfferLifecycleResult.NotFound => NotFound(new
                {
                    success = false,
                    message = "Offer not found.",
                }),
                CatalogOfferLifecycleResult.InvalidStatus invalid => Conflict(new
                {
                    success = false,
                    code = "invalid_status",
                    message = invalid.Message,
                }),
                _ => StatusCode(StatusCodes.Status500InternalServerError),
            };
        }

        private static DateTime EnsureUtc(DateTime value)
        {
            return value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            };
        }
    }
}
