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

        public HomeWeeklyBriefController(
            ApplicationDbContext context,
            IRestaurantPermissionHelper permissions,
            IWeeklyBriefGenerateService generate
        )
        {
            _context = context;
            _permissions = permissions;
            _generate = generate;
        }

        [HttpGet]
        public async Task<IActionResult> GetWeeklyBrief(
            [FromQuery] int locationId,
            [FromQuery] string? week = null,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
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

            var reports = await ReportsQueryGate.AuthorizeReportsViewAsync(
                _permissions,
                User,
                locationId
            );
            var denied = reports.ToHttpResult();

            if (denied != null)
            {
                return denied;
            }

            if (!TryResolveTargetWeek(
                    week,
                    from,
                    to,
                    weekStartsOn: await ResolveWeekStartsOnAsync(
                        locationId,
                        cancellationToken
                    ),
                    out var weekKey,
                    out var noClosedOverlap,
                    out var weekError
                ))
            {
                return weekError!;
            }

            if (noClosedOverlap)
            {
                return Ok(new
                {
                    success = true,
                    ready = false,
                    locationId,
                    week = weekKey,
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

            return await ReadyEnvelopeOrStoreErrorAsync(
                locationId,
                weekKey,
                row,
                cancellationToken
            );
        }

        /// <summary>
        /// Manual / lazy generate for a closed workspace week. Home omits
        /// <c>from</c>/<c>to</c> (closed prior). Reports may pass a KPI window;
        /// the API snaps to the most recent closed week overlapping that range.
        /// Available any day; <see cref="WeeklyBriefWeekKey.IsGenerateDay"/>
        /// gates only the scheduled job. Does not produce
        /// <c>weekly-brief-ready</c>; notify stays on the Monday job seam.
        /// </summary>
        [HttpPost("generate")]
        public async Task<IActionResult> GenerateWeeklyBrief(
            [FromQuery] int locationId,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
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

            var reports = await ReportsQueryGate.AuthorizeReportsViewAsync(
                _permissions,
                User,
                locationId
            );
            var denied = reports.ToHttpResult();

            if (denied != null)
            {
                return denied;
            }

            var locationMeta = await _context.RestaurantLocations
                .AsNoTracking()
                .Where(l => l.Id == locationId)
                .Select(l => new
                {
                    l.CreatedAt,
                    WeekStartsOn = l.Restaurant != null
                        ? l.Restaurant.WeekStartsOn
                        : null,
                    SubscriptionPlan = l.Restaurant != null
                        && l.Restaurant.BillingAccount != null
                            ? l.Restaurant.BillingAccount.SubscriptionPlan
                            : null,
                })
                .FirstOrDefaultAsync(cancellationToken);

            var weekStartsOn = locationMeta?.WeekStartsOn;
            var utcNow = DateTime.UtcNow;

            if (!TryResolveClosedWeekForGenerate(
                    from,
                    to,
                    weekStartsOn,
                    utcNow,
                    out var closedWeek,
                    out var noClosedOverlap,
                    out var rangeError
                ))
            {
                return rangeError!;
            }

            if (noClosedOverlap)
            {
                return Ok(
                    new
                    {
                        success = true,
                        ready = false,
                        locationId,
                        week = string.Empty,
                        reason = WeeklyBriefNotReadyReasons.NoClosedOverlap,
                    }
                );
            }

            // Soft not-ready: missing location, created at/after closed-week end,
            // or Pilot. Day-of-week is not a gate here (manual Generate any day).
            // Does not notify — weekly-brief-ready stays on the Monday job seam.
            // `reason` lets Reports show a soft empty helper (e.g. location too new).
            if (locationMeta is null)
            {
                return Ok(
                    new
                    {
                        success = true,
                        ready = false,
                        locationId,
                        week = closedWeek.WeekKey,
                        reason = WeeklyBriefNotReadyReasons.LocationMissing,
                    }
                );
            }

            if (
                !WeeklyBriefWeekKey.LocationExistedBeforeClosedWeek(
                    locationMeta.CreatedAt,
                    closedWeek
                )
            )
            {
                return Ok(
                    new
                    {
                        success = true,
                        ready = false,
                        locationId,
                        week = closedWeek.WeekKey,
                        reason = WeeklyBriefNotReadyReasons.LocationTooNew,
                    }
                );
            }

            if (WeeklyBriefWeekKey.IsPilotPlan(locationMeta.SubscriptionPlan))
            {
                return Ok(
                    new
                    {
                        success = true,
                        ready = false,
                        locationId,
                        week = closedWeek.WeekKey,
                        reason = WeeklyBriefNotReadyReasons.Pilot,
                    }
                );
            }

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

            var reports = await ReportsQueryGate.AuthorizeReportsViewAsync(
                _permissions,
                User,
                locationId
            );
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
        /// Sync Weekly brief PDF download (same sections as ready Figma page).
        /// Soft lock / Dormant / chargeback → 403 (paid-write gate).
        /// </summary>
        [HttpGet("pdf")]
        public async Task<IActionResult> DownloadWeeklyBriefPdf(
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

            var reports = await ReportsQueryGate.AuthorizeReportsViewAsync(
                _permissions,
                User,
                locationId
            );
            var denied = reports.ToHttpResult();

            if (denied != null)
            {
                return denied;
            }

            try
            {
                await OperatorBillingLockGate.EnsurePaidWriteAllowedForLocationAsync(
                    _context,
                    locationId,
                    cancellationToken
                );
            }
            catch (OperatorBillingLockedException ex)
            {
                return OperatorBillingLockGate.Forbidden(ex.Code);
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
                return NotFound(new
                {
                    success = false,
                    message = "Weekly brief is not ready for this location and week.",
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

            var locationName = await _context.RestaurantLocations
                .AsNoTracking()
                .Where(l => l.Id == locationId)
                .Select(l => l.LocationName)
                .FirstOrDefaultAsync(cancellationToken)
                ?? "Location";

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

            var enrichment = WeeklyBriefEnrichmentApply.TryDeserialize(
                row.EnrichmentJson
            );
            var executiveSummary = WeeklyBriefEnrichmentApply.ResolveExecutiveSummary(
                phase1.ExecutiveSummary,
                enrichment
            );
            var feedbackSummary = WeeklyBriefEnrichmentApply.ResolveFeedbackSummary(
                phase1.FeedbackSummary,
                metrics,
                enrichment
            );
            recommendedActions = WeeklyBriefEnrichmentApply.ApplyActionWording(
                recommendedActions,
                enrichment
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

            var document = new WeeklyBriefPdfWriter.Document(
                LocationName: locationName,
                Period: phase1.Meta.Period,
                DataSources: phase1.Meta.DataSources,
                Confidence: phase1.Meta.Confidence,
                GeneratedAtLabel: LondonDateFormat.DMmmYyyy(row.GeneratedAtUtc),
                ExecutiveSummary: executiveSummary,
                WhatChanged: phase1.WhatChanged
                    .Select(change => new WeeklyBriefPdfWriter.WhatChangedRow(
                        change.Area,
                        change.Change,
                        change.Meaning
                    ))
                    .ToList(),
                FeedbackSummary: feedbackSummary is null
                    ? null
                    : new WeeklyBriefPdfWriter.FeedbackSummary(
                        feedbackSummary.Text,
                        feedbackSummary.Subtitle
                    ),
                RecommendedActionLines: FormatRecommendedActionLines(
                    recommendedActions
                ),
                SuggestedCampaign: suggestedCampaign is null
                    ? null
                    : new WeeklyBriefPdfWriter.SuggestedCampaign(
                        suggestedCampaign.Name,
                        suggestedCampaign.AudienceKey
                    )
            );

            var (content, fileName) = WeeklyBriefPdfWriter.Render(
                document,
                locationId,
                DateTime.UtcNow
            );
            return File(content, WeeklyBriefPdfWriter.ContentType, fileName);
        }

        private static IReadOnlyList<string> FormatRecommendedActionLines(
            IReadOnlyList<object> facts
        )
        {
            var lines = new List<string>(facts.Count);
            foreach (var fact in facts)
            {
                lines.Add(WeeklyBriefEnrichmentApply.FormatRecommendedActionLine(fact));
            }

            return lines;
        }

        /// <summary>
        /// Resolve workspace-week key for GET: explicit <paramref name="week"/>,
        /// else snap from <paramref name="from"/>/<paramref name="to"/>, else
        /// closed prior. When the range has no closed overlap,
        /// <paramref name="noClosedOverlap"/> is true and <paramref name="weekKey"/>
        /// is empty.
        /// </summary>
        private static bool TryResolveTargetWeek(
            string? week,
            DateTime? from,
            DateTime? to,
            string? weekStartsOn,
            out string weekKey,
            out bool noClosedOverlap,
            out IActionResult? error
        )
        {
            weekKey = string.Empty;
            noClosedOverlap = false;
            error = null;

            if (!string.IsNullOrWhiteSpace(week))
            {
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

            if (from != null || to != null)
            {
                if (!TryResolveClosedWeekForGenerate(
                        from,
                        to,
                        weekStartsOn,
                        DateTime.UtcNow,
                        out var snapped,
                        out noClosedOverlap,
                        out error
                    ))
                {
                    return false;
                }

                weekKey = noClosedOverlap
                    ? string.Empty
                    : snapped.WeekKey;
                return true;
            }

            weekKey = WeeklyBriefWeekKey
                .ForClosedPriorWeek(
                    WeeklyBriefWeekKey.DefaultLocationTimeZoneId,
                    DateTime.UtcNow,
                    weekStartsOn
                )
                .WeekKey;
            return true;
        }

        /// <summary>
        /// Resolve closed week for POST generate: snap from from/to when both
        /// present; otherwise closed prior. One-sided from/to is a bad request.
        /// </summary>
        private static bool TryResolveClosedWeekForGenerate(
            DateTime? from,
            DateTime? to,
            string? weekStartsOn,
            DateTime utcNow,
            out WeeklyBriefClosedWeek closedWeek,
            out bool noClosedOverlap,
            out IActionResult? error
        )
        {
            closedWeek = default;
            noClosedOverlap = false;
            error = null;

            if (from == null && to == null)
            {
                closedWeek = WeeklyBriefWeekKey.ForClosedPriorWeek(
                    WeeklyBriefWeekKey.DefaultLocationTimeZoneId,
                    utcNow,
                    weekStartsOn
                );
                return true;
            }

            if (from == null || to == null)
            {
                error = new BadRequestObjectResult(new
                {
                    success = false,
                    message = "from and to are required together.",
                });
                return false;
            }

            var fromUtc = GuestsDateWindows.EnsureUtc(from.Value);
            var toUtc = GuestsDateWindows.EnsureUtc(to.Value);
            if (fromUtc >= toUtc)
            {
                error = new BadRequestObjectResult(new
                {
                    success = false,
                    message = "from must be before to.",
                });
                return false;
            }

            if (
                !WeeklyBriefWeekKey.TryMostRecentClosedWeekOverlapping(
                    WeeklyBriefWeekKey.DefaultLocationTimeZoneId,
                    utcNow,
                    weekStartsOn,
                    fromUtc,
                    toUtc,
                    out closedWeek
                )
            )
            {
                noClosedOverlap = true;
                closedWeek = default;
                return true;
            }

            return true;
        }

        /// <summary>
        /// Resolve workspace-week key for mark-reviewed / PDF — any valid key,
        /// or closed prior when omitted.
        /// </summary>
        private static bool TryResolveWeekKey(
            string? week,
            string? weekStartsOn,
            out string weekKey,
            out IActionResult? error
        )
        {
            return TryResolveTargetWeek(
                week,
                from: null,
                to: null,
                weekStartsOn,
                out weekKey,
                out _,
                out error
            );
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

            var enrichment = WeeklyBriefEnrichmentApply.TryDeserialize(
                row.EnrichmentJson
            );
            var executiveSummary = WeeklyBriefEnrichmentApply.ResolveExecutiveSummary(
                phase1.ExecutiveSummary,
                enrichment
            );
            var feedbackSummary = WeeklyBriefEnrichmentApply.ResolveFeedbackSummary(
                phase1.FeedbackSummary,
                metrics,
                enrichment
            );
            recommendedActions = WeeklyBriefEnrichmentApply.ApplyActionWording(
                recommendedActions,
                enrichment
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
                executiveSummary,
                whatChanged = phase1.WhatChanged,
                feedbackSummary,
                recommendedActions,
                suggestedCampaign,
                insightNarratives = enrichment?.InsightNarratives,
                insightCandidates = enrichment?.InsightCandidates,
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

    }
}
