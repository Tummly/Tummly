using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Configurations;
using TummlyBackend.Data;
using TummlyBackend.DTOs.GuestPreferences;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Controllers
{
    /// <summary>
    /// Anonymous guest preference centre endpoints (spec §7). Session endpoints prove a
    /// contact and return a <see cref="GuestPreferencesSessionToken"/>.
    /// </summary>
    [ApiController]
    [Route("api/public/preferences")]
    [AllowAnonymous]
    public class PublicGuestPreferencesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IGuestPreferencesVerificationService _verification;
        private readonly ITummlyGuestIdentityService _identity;
        private readonly IGuestPreferenceService _preferences;
        private readonly IConfiguration _configuration;
        private readonly TimeProvider _timeProvider;
        private readonly IGuestInitiatedMarketingWithdrawService _withdraw;
        private readonly ILogger<PublicGuestPreferencesController> _logger;

        public PublicGuestPreferencesController(
            ApplicationDbContext context,
            IGuestPreferencesVerificationService verification,
            ITummlyGuestIdentityService identity,
            IGuestPreferenceService preferences,
            IConfiguration configuration,
            TimeProvider timeProvider,
            IGuestInitiatedMarketingWithdrawService withdraw,
            ILogger<PublicGuestPreferencesController> logger
        )
        {
            _context = context;
            _verification = verification;
            _identity = identity;
            _preferences = preferences;
            _configuration = configuration;
            _timeProvider = timeProvider;
            _withdraw = withdraw;
            _logger = logger;
        }

        /// <summary>Tummly Guest id proven by the session token (set by <see cref="GuestPreferencesSessionFilter"/>).</summary>
        private int SessionGuestId =>
            (int)HttpContext.Items[GuestPreferencesSessionFilter.TummlyGuestIdItemKey]!;

        /// <summary>Signed link from a marketing email (<c>UnsubscribeToken</c>).</summary>
        [HttpPost("session/link")]
        [EnableRateLimiting(GuestPreferencesRateLimit.PolicyName)]
        public async Task<IActionResult> SessionLink(
            [FromBody] GuestPreferencesSessionLinkRequest? request,
            CancellationToken cancellationToken
        )
        {
            var secret = UnsubscribeLink.ResolveSigningSecret(_configuration);
            if (
                string.IsNullOrEmpty(secret)
                || !UnsubscribeToken.TryVerify(
                    request?.Token ?? string.Empty,
                    secret,
                    out var locationGuestId,
                    out var restaurantId,
                    out _
                )
            )
            {
                return Ok(new { valid = false });
            }

            var email = await _context.LocationGuests
                .AsNoTracking()
                .Where(lg =>
                    lg.Id == locationGuestId
                    && lg.RestaurantLocation != null
                    && lg.RestaurantLocation.RestaurantId == restaurantId
                    && lg.MasterGuest != null
                )
                .Select(lg => lg.MasterGuest!.NormalizedEmail)
                .FirstOrDefaultAsync(cancellationToken);

            if (string.IsNullOrEmpty(email))
            {
                return Ok(new { valid = false });
            }

            var proven = await _identity.FindOrCreateByContactAsync(
                TummlyGuestContactTypes.Email,
                email,
                cancellationToken
            );

            return Ok(new { sessionToken = IssueSessionToken(proven) });
        }

        /// <summary>Always 202. Same body whether or not the contact is on file.</summary>
        [HttpPost("session/start")]
        [EnableRateLimiting(GuestPreferencesRateLimit.PolicyName)]
        public async Task<IActionResult> SessionStart(
            [FromBody] GuestPreferencesSessionStartRequest? request,
            CancellationToken cancellationToken
        )
        {
            await _verification.StartAsync(
                request?.Contact ?? string.Empty,
                null,
                cancellationToken
            );

            return Accepted(new { sent = true });
        }

        [HttpPost("session/verify-code")]
        [EnableRateLimiting(GuestPreferencesRateLimit.PolicyName)]
        public async Task<IActionResult> SessionVerifyCode(
            [FromBody] GuestPreferencesSessionVerifyCodeRequest? request,
            CancellationToken cancellationToken
        )
        {
            var (outcome, proven) = await _verification.VerifyCodeAsync(
                request?.Contact ?? string.Empty,
                request?.Code ?? string.Empty,
                null,
                cancellationToken
            );

            return SessionResult(outcome, proven);
        }

        /// <summary>
        /// Entry links only. An add-contact link is rejected here, without consuming it. It is
        /// checked by the add-contact method (<c>contacts/verify-email</c>, Task 7), which issues no session.
        /// </summary>
        [HttpPost("session/verify-email")]
        [EnableRateLimiting(GuestPreferencesRateLimit.PolicyName)]
        public async Task<IActionResult> SessionVerifyEmail(
            [FromBody] GuestPreferencesSessionVerifyEmailRequest? request,
            CancellationToken cancellationToken
        )
        {
            var (outcome, proven) = await _verification.VerifyEntryEmailLinkAsync(
                request?.V ?? string.Empty,
                cancellationToken
            );

            return SessionResult(outcome, proven);
        }

        /// <summary>Guest preference overview for the session's guest.</summary>
        [HttpGet]
        [GuestPreferencesSession]
        public async Task<IActionResult> GetOverview(CancellationToken cancellationToken)
        {
            try
            {
                return Ok(await _preferences.GetOverviewAsync(SessionGuestId, cancellationToken));
            }
            catch (KeyNotFoundException)
            {
                // The guest was merged away after the session was issued.
                return Unauthorized(new { error = "session" });
            }
        }

        [HttpPut("name")]
        [GuestPreferencesSession]
        public async Task<IActionResult> SetName(
            [FromBody] GuestPreferenceNameRequest? request,
            CancellationToken cancellationToken
        )
        {
            try
            {
                await _preferences.SetFirstNameAsync(
                    SessionGuestId,
                    request?.FirstName ?? string.Empty,
                    cancellationToken
                );
                return NoContent();
            }
            catch (ArgumentException)
            {
                return BadRequest(new { error = "invalid" });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { error = "not-found" });
            }
        }

        [HttpPut("channels")]
        [GuestPreferencesSession]
        public Task<IActionResult> SetChannels(
            [FromBody] GuestPreferenceChannelsRequest? request,
            CancellationToken cancellationToken
        ) =>
            WriteOrNotFoundAsync(() =>
                _preferences.SetChannelsAsync(
                    SessionGuestId,
                    request ?? new GuestPreferenceChannelsRequest(null, null),
                    cancellationToken
                )
            );

        [HttpPost("stop-all")]
        [GuestPreferencesSession]
        public Task<IActionResult> StopAll(CancellationToken cancellationToken) =>
            WriteOrNotFoundAsync(() => _preferences.StopAllAsync(SessionGuestId, cancellationToken));

        [HttpGet("restaurants")]
        [GuestPreferencesSession]
        public async Task<IActionResult> ListRestaurants(
            [FromQuery] string? q,
            CancellationToken cancellationToken
        )
        {
            try
            {
                return Ok(
                    await _preferences.ListRestaurantsAsync(SessionGuestId, q, cancellationToken)
                );
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { error = "not-found" });
            }
        }

        [HttpPut("locations/{locationId:int}/mute")]
        [GuestPreferencesSession]
        public Task<IActionResult> SetLocationMute(
            int locationId,
            [FromBody] GuestPreferenceMuteRequest? request,
            CancellationToken cancellationToken
        )
        {
            if (request?.Muted is not { } muted)
            {
                return Task.FromResult<IActionResult>(BadRequest(new { error = "invalid" }));
            }

            return WriteOrNotFoundAsync(() =>
                _preferences.SetLocationMuteAsync(
                    SessionGuestId,
                    locationId,
                    muted,
                    cancellationToken
                )
            );
        }

        [HttpPut("restaurants/{restaurantId:int}/mute")]
        [GuestPreferencesSession]
        public Task<IActionResult> SetRestaurantMute(
            int restaurantId,
            [FromBody] GuestPreferenceMuteRequest? request,
            CancellationToken cancellationToken
        )
        {
            if (request?.Muted is not { } muted)
            {
                return Task.FromResult<IActionResult>(BadRequest(new { error = "invalid" }));
            }

            return WriteOrNotFoundAsync(() =>
                _preferences.SetRestaurantMuteAsync(
                    SessionGuestId,
                    restaurantId,
                    muted,
                    cancellationToken
                )
            );
        }

        /// <summary>Always 202 with the same body, whether or not the contact is valid or known.</summary>
        [HttpPost("contacts")]
        [EnableRateLimiting(GuestPreferencesRateLimit.PolicyName)]
        [GuestPreferencesSession]
        public async Task<IActionResult> AddContact(
            [FromBody] GuestPreferencesSessionStartRequest? request,
            CancellationToken cancellationToken
        )
        {
            await _verification.StartAsync(
                request?.Contact ?? string.Empty,
                SessionGuestId,
                cancellationToken
            );

            return Accepted(new { sent = true });
        }

        [HttpPost("contacts/verify-code")]
        [EnableRateLimiting(GuestPreferencesRateLimit.PolicyName)]
        [GuestPreferencesSession]
        public async Task<IActionResult> VerifyAddContactCode(
            [FromBody] GuestPreferencesSessionVerifyCodeRequest? request,
            CancellationToken cancellationToken
        )
        {
            var (outcome, proven) = await _verification.VerifyCodeAsync(
                request?.Contact ?? string.Empty,
                request?.Code ?? string.Empty,
                SessionGuestId,
                cancellationToken
            );

            return LinkedResult(outcome, proven);
        }

        /// <summary>
        /// Add-contact email link. Needs no session: the link is the proof. Returns only
        /// <c>{ linked: true }</c>, never a session token or any ids.
        /// </summary>
        [HttpPost("contacts/verify-email")]
        [EnableRateLimiting(GuestPreferencesRateLimit.PolicyName)]
        public async Task<IActionResult> VerifyAddContactEmail(
            [FromBody] GuestPreferencesSessionVerifyEmailRequest? request,
            CancellationToken cancellationToken
        )
        {
            var (outcome, proven) = await _verification.VerifyAddContactEmailLinkAsync(
                request?.V ?? string.Empty,
                cancellationToken
            );

            return LinkedResult(outcome, proven);
        }

        /// <summary>
        /// RFC 8058 one-click unsubscribe, POSTed by mailbox providers from the
        /// <c>List-Unsubscribe</c> header. The signed <c>t</c> token is the only proof. There is no
        /// session. It withdraws Email marketing at the token's location only, and always returns
        /// 200 (a bad or missing token changes nothing). The body is ignored, so a form-urlencoded,
        /// multipart or empty body all work. It is exempt from the per-IP limit: providers share
        /// sending IPs, and a 429 here would break the always-200 contract.
        /// </summary>
        [HttpPost("one-click")]
        [DisableRateLimiting]
        public async Task<IActionResult> OneClick(
            [FromQuery] string? t,
            CancellationToken cancellationToken
        )
        {
            var secret = UnsubscribeLink.ResolveSigningSecret(_configuration) ?? string.Empty;
            if (
                UnsubscribeToken.TryVerify(
                    t ?? string.Empty,
                    secret,
                    out var locationGuestId,
                    out var restaurantId,
                    out _
                )
            )
            {
                try
                {
                    await _withdraw.WithdrawForLocationGuestAsync(
                        locationGuestId,
                        restaurantId,
                        LocationGuestPermissionKind.EmailMarketing,
                        LocationGuestPermissionLedgerSources.EmailUnsubscribe,
                        cancellationToken
                    );
                }
                catch (Exception ex)
                {
                    // Still 200: a provider that sees an error may retry or flag the sender.
                    // Ids only in the log, never a contact.
                    _logger.LogError(
                        ex,
                        "One-click withdraw failed for location guest {LocationGuestId} at restaurant {RestaurantId}",
                        locationGuestId,
                        restaurantId
                    );
                }
            }

            return Ok();
        }

        private IActionResult LinkedResult(GuestVerifyOutcome outcome, ProvenContact? proven)
        {
            if (outcome == GuestVerifyOutcome.Ok && proven is not null)
            {
                return Ok(new { linked = true });
            }

            return VerifyFailed(outcome);
        }

        private IActionResult SessionResult(GuestVerifyOutcome outcome, ProvenContact? proven)
        {
            if (outcome == GuestVerifyOutcome.Ok && proven is not null)
            {
                return Ok(new { sessionToken = IssueSessionToken(proven) });
            }

            return VerifyFailed(outcome);
        }

        private IActionResult VerifyFailed(GuestVerifyOutcome outcome) =>
            BadRequest(
                new
                {
                    error = outcome == GuestVerifyOutcome.Expired ? "expired" : "invalid",
                }
            );

        /// <summary>
        /// Runs a write. Unknown guest or a location/restaurant not on the guest's list → 404.
        /// </summary>
        private async Task<IActionResult> WriteOrNotFoundAsync(Func<Task> write)
        {
            try
            {
                await write();
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { error = "not-found" });
            }
        }

        private string IssueSessionToken(ProvenContact proven)
        {
            var secret = GuestPreferencesSessionToken.ResolveSecret(_configuration);
            if (string.IsNullOrEmpty(secret))
            {
                throw new InvalidOperationException(
                    "Guest preferences session signing secret is not configured."
                );
            }

            return GuestPreferencesSessionToken.Create(
                proven.TummlyGuestId,
                proven.ContactId,
                _timeProvider.GetUtcNow().UtcDateTime,
                secret
            );
        }
    }
}
