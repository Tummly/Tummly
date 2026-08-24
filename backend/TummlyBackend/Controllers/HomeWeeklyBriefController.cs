using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Controllers
{
    /// <summary>
    /// Weekly brief GET (read-only) and lazy POST generate for Operator Home.
    /// GET must not generate.
    /// </summary>
    [ApiController]
    [Route("api/home/weekly-brief")]
    [Authorize]
    public class HomeWeeklyBriefController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IOwnedLocationService _ownedLocation;
        private readonly IWeeklyBriefGenerateService _generate;
        private readonly IWeeklyBriefReadyNotifier _notifier;

        public HomeWeeklyBriefController(
            ApplicationDbContext context,
            IOwnedLocationService ownedLocation,
            IWeeklyBriefGenerateService generate,
            IWeeklyBriefReadyNotifier notifier
        )
        {
            _context = context;
            _ownedLocation = ownedLocation;
            _generate = generate;
            _notifier = notifier;
        }

        [HttpGet]
        public async Task<IActionResult> GetWeeklyBrief(
            [FromQuery] int locationId,
            [FromQuery] string? week = null,
            CancellationToken cancellationToken = default
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            if (locationId <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "locationId is required.",
                });
            }

            var ownedLocation =
                await _ownedLocation.ResolveAsync(userId, locationId);

            var denied =
                OwnedLocationResponses.FromResult(ownedLocation);

            if (denied != null)
            {
                return denied;
            }

            if (!TryResolveWeekKey(
                    week,
                    weekStartsOn: await ResolveWeekStartsOnAsync(
                        locationId,
                        cancellationToken
                    ),
                    out var weekKey,
                    out var weekError
                ))
            {
                return weekError!;
            }

            var row = await _context.WeeklyBriefs
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    brief =>
                        brief.LocationId == locationId
                        && brief.WeekKey == weekKey
                        && brief.Status == WeeklyBriefStatus.Succeeded,
                    cancellationToken
                );

            if (row is null)
            {
                return Ok(new
                {
                    success = true,
                    ready = false,
                    locationId,
                    week = weekKey,
                });
            }

            return ReadyEnvelopeOrStoreError(locationId, weekKey, row);
        }

        /// <summary>
        /// Lazy generate for the current closed prior week (Home — no week picker).
        /// </summary>
        [HttpPost("generate")]
        public async Task<IActionResult> GenerateWeeklyBrief(
            [FromQuery] int locationId,
            CancellationToken cancellationToken = default
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            if (locationId <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "locationId is required.",
                });
            }

            var ownedLocation =
                await _ownedLocation.ResolveAsync(userId, locationId);

            var denied =
                OwnedLocationResponses.FromResult(ownedLocation);

            if (denied != null)
            {
                return denied;
            }

            var weekStartsOn = await ResolveWeekStartsOnAsync(
                locationId,
                cancellationToken
            );

            var closedWeek = WeeklyBriefWeekKey.ForClosedPriorWeek(
                WeeklyBriefWeekKey.DefaultLocationTimeZoneId,
                DateTime.UtcNow,
                weekStartsOn
            );

            var result = await _generate.GenerateAsync(
                locationId,
                closedWeek,
                cancellationToken
            );

            if (result is WeeklyBriefGenerateResult.Failed failed)
            {
                return StatusCode(
                    StatusCodes.Status502BadGateway,
                    new
                    {
                        success = false,
                        message = failed.Message,
                        retryable = failed.Retryable,
                    }
                );
            }

            if (
                result
                is not WeeklyBriefGenerateResult.Succeeded succeeded
            )
            {
                return StatusCode(
                    StatusCodes.Status502BadGateway,
                    new
                    {
                        success = false,
                        message =
                            "Could not generate a weekly brief. Please try again.",
                        retryable = true,
                    }
                );
            }

            if (succeeded.Created)
            {
                await _notifier.NotifyGeneratedAsync(
                    locationId,
                    closedWeek,
                    cancellationToken
                );
            }

            return ReadyEnvelopeOrStoreError(
                locationId,
                closedWeek.WeekKey,
                succeeded.Brief
            );
        }

        /// <summary>
        /// Resolve workspace-week key for GET — any valid key, or closed prior when omitted.
        /// </summary>
        private static bool TryResolveWeekKey(
            string? week,
            string? weekStartsOn,
            out string weekKey,
            out IActionResult? error
        )
        {
            weekKey = string.Empty;
            error = null;

            if (string.IsNullOrWhiteSpace(week))
            {
                weekKey = WeeklyBriefWeekKey
                    .ForClosedPriorWeek(
                        WeeklyBriefWeekKey.DefaultLocationTimeZoneId,
                        DateTime.UtcNow,
                        weekStartsOn
                    )
                    .WeekKey;
                return true;
            }

            if (!WeeklyBriefWeekKey.TryNormalizeWeekKey(week, out weekKey))
            {
                error = new BadRequestObjectResult(new
                {
                    success = false,
                    message =
                        "week must be a workspace-week key (weekday:yyyy-MM-dd) or legacy ISO yyyy-Www.",
                });
                return false;
            }

            return true;
        }

        private async Task<string?> ResolveWeekStartsOnAsync(
            int locationId,
            CancellationToken cancellationToken
        )
        {
            return await _context.RestaurantLocations
                .AsNoTracking()
                .Where(l => l.Id == locationId)
                .Select(l => l.Restaurant != null ? l.Restaurant.WeekStartsOn : null)
                .FirstOrDefaultAsync(cancellationToken);
        }

        private IActionResult ReadyEnvelopeOrStoreError(
            int locationId,
            string weekKey,
            WeeklyBrief row
        )
        {
            WeeklyBriefBody? body;
            WeeklyBriefMetrics? metrics;
            try
            {
                body = JsonSerializer.Deserialize<WeeklyBriefBody>(
                    row.BodyJson,
                    WeeklyBriefStoreJson.Options
                );
                metrics = JsonSerializer.Deserialize<WeeklyBriefMetrics>(
                    row.MetricsJson,
                    WeeklyBriefStoreJson.Options
                );
            }
            catch (JsonException)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        success = false,
                        message = "Stored weekly brief could not be read.",
                    }
                );
            }

            if (body is null || metrics is null)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        success = false,
                        message = "Stored weekly brief could not be read.",
                    }
                );
            }

            return Ok(new
            {
                success = true,
                ready = true,
                locationId,
                week = weekKey,
                status = row.Status.ToWireString(),
                generatedAtUtc = row.GeneratedAtUtc,
                body,
                metrics,
            });
        }
    }
}
