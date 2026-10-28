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
    [EnableRateLimiting(GuestPreferencesRateLimit.PolicyName)]
    public class PublicGuestPreferencesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IGuestPreferencesVerificationService _verification;
        private readonly ITummlyGuestIdentityService _identity;
        private readonly IConfiguration _configuration;
        private readonly TimeProvider _timeProvider;

        public PublicGuestPreferencesController(
            ApplicationDbContext context,
            IGuestPreferencesVerificationService verification,
            ITummlyGuestIdentityService identity,
            IConfiguration configuration,
            TimeProvider timeProvider
        )
        {
            _context = context;
            _verification = verification;
            _identity = identity;
            _configuration = configuration;
            _timeProvider = timeProvider;
        }

        /// <summary>Signed link from a marketing email (<c>UnsubscribeToken</c>).</summary>
        [HttpPost("session/link")]
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

        private IActionResult SessionResult(GuestVerifyOutcome outcome, ProvenContact? proven)
        {
            if (outcome == GuestVerifyOutcome.Ok && proven is not null)
            {
                return Ok(new { sessionToken = IssueSessionToken(proven) });
            }

            return BadRequest(
                new
                {
                    error = outcome == GuestVerifyOutcome.Expired ? "expired" : "invalid",
                }
            );
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
