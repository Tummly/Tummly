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
        private readonly IRestaurantPermissionHelper _permissions;
        private readonly IWeeklyBriefGenerateService _generate;
        private readonly IWeeklyBriefReadyNotifier _notifier;

        public HomeWeeklyBriefController(
            ApplicationDbContext context,
            IRestaurantPermissionHelper permissions,
            IWeeklyBriefGenerateService generate,
            IWeeklyBriefReadyNotifier notifier
        )
        {
            _context = context;
            _permissions = permissions;
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
                OperatorAuth.TryRequireUserId(User, out _);

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

            var reports = await GateReportsViewAsync(locationId);
            var denied = reports.ToHttpResult();

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

            return await ReadyEnvelopeOrStoreErrorAsync(
                locationId,
                weekKey,
                row,
                cancellationToken
            );
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
                OperatorAuth.TryRequireUserId(User, out _);

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

            var reports = await GateReportsViewAsync(locationId);
            var denied = reports.ToHttpResult();

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

            return await ReadyEnvelopeOrStoreErrorAsync(
                locationId,
                closedWeek.WeekKey,
                succeeded.Brief,
                cancellationToken
            );
        }

        /// <summary>
        /// Mark the location+week Weekly brief as reviewed (annotation; Soft lock allowed).
        /// Re-mark refreshes <c>reviewedAtUtc</c> / <c>reviewedByUserId</c>.
        /// </summary>
        [HttpPost("mark-reviewed")]
        public async Task<IActionResult> MarkWeeklyBriefReviewed(
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

            var reports = await GateReportsViewAsync(locationId);
            var denied = reports.ToHttpResult();

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
                .FirstOrDefaultAsync(
                    brief =>
                        brief.LocationId == locationId
                        && brief.WeekKey == weekKey
                        && brief.Status == WeeklyBriefStatus.Succeeded,
                    cancellationToken
                );

            if (row is null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Weekly brief is not ready for this location and week.",
                });
            }

            row.ReviewedAtUtc = DateTime.UtcNow;
            row.ReviewedByUserId = userId;
            await _context.SaveChangesAsync(cancellationToken);

            return await ReadyEnvelopeOrStoreErrorAsync(
                locationId,
                weekKey,
                row,
                cancellationToken
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

        private async Task<IActionResult> ReadyEnvelopeOrStoreErrorAsync(
            int locationId,
            string weekKey,
            WeeklyBrief row,
            CancellationToken cancellationToken
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

            var priorMetrics = await TryLoadPriorMetricsAsync(
                locationId,
                weekKey,
                cancellationToken
            );
            var phase1 = WeeklyBriefPhase1Meta.Build(
                body,
                metrics,
                weekKey,
                priorMetrics
            );

            DateTime? coverageFromUtc = null;
            DateTime? coverageToUtc = null;
            if (
                WeeklyBriefWeekKey.TryCoverageWindow(
                    weekKey,
                    WeeklyBriefWeekKey.DefaultLocationTimeZoneId,
                    out var fromUtc,
                    out var toUtc
                )
            )
            {
                coverageFromUtc = fromUtc;
                coverageToUtc = toUtc;
            }

            var recommendedActions =
                await WeeklyBriefRecommendedActions.BuildFactsAsync(
                    _context,
                    locationId,
                    metrics,
                    coverageFromUtc,
                    coverageToUtc,
                    cancellationToken
                );

            WeeklyBriefRecommendedActions.SuggestedCampaignDto? suggestedCampaign =
                null;
            if (coverageFromUtc is DateTime windowFrom && coverageToUtc is DateTime windowTo)
            {
                suggestedCampaign =
                    await WeeklyBriefRecommendedActions.FindSuggestedCampaignAsync(
                        _context,
                        locationId,
                        windowFrom,
                        windowTo,
                        cancellationToken
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
                meta = phase1.Meta,
                executiveSummary = phase1.ExecutiveSummary,
                whatChanged = phase1.WhatChanged,
                feedbackSummary = phase1.FeedbackSummary,
                recommendedActions,
                suggestedCampaign,
                reviewedAtUtc = row.ReviewedAtUtc,
                reviewedByUserId = row.ReviewedByUserId,
            });
        }

        private async Task<WeeklyBriefMetrics?> TryLoadPriorMetricsAsync(
            int locationId,
            string weekKey,
            CancellationToken cancellationToken
        )
        {
            if (!WeeklyBriefWeekKey.TryPriorWeekKey(weekKey, out var priorKey))
            {
                return null;
            }

            var priorRow = await _context.WeeklyBriefs
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    brief =>
                        brief.LocationId == locationId
                        && brief.WeekKey == priorKey
                        && brief.Status == WeeklyBriefStatus.Succeeded,
                    cancellationToken
                );

            if (priorRow is null)
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<WeeklyBriefMetrics>(
                    priorRow.MetricsJson,
                    WeeklyBriefStoreJson.Options
                );
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private Task<RestaurantPermissionDecision> GateReportsViewAsync(
            int locationId
        )
        {
            return _permissions.AuthorizeLocationAsync(
                User,
                OperatorAreaIds.Reports,
                PermissionLevel.View,
                locationId
            );
        }
    }
}
