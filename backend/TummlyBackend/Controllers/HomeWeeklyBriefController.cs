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
    /// Read-only Weekly brief GET. Does not generate — lazy generate is a separate seam.
    /// </summary>
    [ApiController]
    [Route("api/home/weekly-brief")]
    [Authorize]
    public class HomeWeeklyBriefController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IOwnedLocationService _ownedLocation;

        public HomeWeeklyBriefController(
            ApplicationDbContext context,
            IOwnedLocationService ownedLocation
        )
        {
            _context = context;
            _ownedLocation = ownedLocation;
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

            string weekKey;
            if (string.IsNullOrWhiteSpace(week))
            {
                weekKey = WeeklyBriefWeekKey
                    .ForClosedPriorWeek(
                        WeeklyBriefWeekKey.DefaultLocationTimeZoneId,
                        DateTime.UtcNow
                    )
                    .WeekKey;
            }
            else if (
                !WeeklyBriefWeekKey.TryNormalizeWeekKey(week, out weekKey)
            )
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "week must be an ISO week key in the form yyyy-Www.",
                });
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
