using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.PublicUnsubscribe;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [Route("api/public/unsubscribe")]
    [AllowAnonymous]
    public class PublicUnsubscribeController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IGuestInitiatedMarketingWithdrawService _withdraw;
        private readonly IConfiguration _configuration;

        public PublicUnsubscribeController(
            ApplicationDbContext context,
            IGuestInitiatedMarketingWithdrawService withdraw,
            IConfiguration configuration
        )
        {
            _context = context;
            _withdraw = withdraw;
            _configuration = configuration;
        }

        [HttpGet("preview")]
        public async Task<IActionResult> Preview(
            [FromQuery] string? t,
            CancellationToken cancellationToken
        )
        {
            if (
                !TryVerifyToken(
                    t,
                    out var locationGuestId,
                    out var restaurantId
                )
            )
            {
                return Ok(new { valid = false });
            }

            var restaurantName = await _context.Restaurants
                .AsNoTracking()
                .Where(r => r.Id == restaurantId)
                .Select(r => r.Name)
                .FirstOrDefaultAsync(cancellationToken);

            if (string.IsNullOrEmpty(restaurantName))
            {
                return Ok(new { valid = false });
            }

            var guestExists = await _context.LocationGuests
                .AsNoTracking()
                .AnyAsync(
                    lg =>
                        lg.Id == locationGuestId
                        && lg.RestaurantLocation != null
                        && lg.RestaurantLocation.RestaurantId == restaurantId,
                    cancellationToken
                );

            if (!guestExists)
            {
                return Ok(new { valid = false });
            }

            return Ok(
                new
                {
                    valid = true,
                    restaurantName,
                }
            );
        }

        [HttpPost("confirm")]
        public async Task<IActionResult> Confirm(
            [FromBody] UnsubscribeConfirmRequest? request,
            CancellationToken cancellationToken
        )
        {
            if (
                TryVerifyToken(
                    request?.Token,
                    out var locationGuestId,
                    out var restaurantId
                )
            )
            {
                await _withdraw.WithdrawForLocationGuestAsync(
                    locationGuestId,
                    restaurantId,
                    LocationGuestPermissionKind.EmailMarketing,
                    LocationGuestPermissionLedgerSources.EmailUnsubscribe,
                    cancellationToken
                );
            }

            return Ok(new { success = true });
        }

        [HttpPost("form")]
        public async Task<IActionResult> Form(
            [FromBody] UnsubscribeFormRequest? request,
            CancellationToken cancellationToken
        )
        {
            if (request != null && request.RestaurantId > 0)
            {
                var normalized = (request.Email ?? string.Empty)
                    .Trim()
                    .ToLowerInvariant();

                if (normalized.Length > 0)
                {
                    await _withdraw.WithdrawForRestaurantContactAsync(
                        request.RestaurantId,
                        normalized,
                        isEmail: true,
                        LocationGuestPermissionKind.EmailMarketing,
                        LocationGuestPermissionLedgerSources.EmailUnsubscribe,
                        cancellationToken
                    );
                }
            }

            return Ok(new { success = true });
        }

        private bool TryVerifyToken(
            string? token,
            out int locationGuestId,
            out int restaurantId
        )
        {
            locationGuestId = 0;
            restaurantId = 0;

            var secret = UnsubscribeLink.ResolveSigningSecret(_configuration);
            if (string.IsNullOrEmpty(secret))
            {
                return false;
            }

            return UnsubscribeToken.TryVerify(
                token ?? string.Empty,
                secret,
                out locationGuestId,
                out restaurantId,
                out _
            );
        }
    }
}
