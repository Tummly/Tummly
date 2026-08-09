using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TummlyBackend.DTOs.Offers;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Controllers
{
    /// <summary>
    /// Offers catalog — create Active definitions for Campaign attach (ticket 22).
    /// Not Feedback /recovery-offers endpoints.
    /// </summary>
    [ApiController]
    [Route("api/offers")]
    [Authorize]
    public class OffersController : ControllerBase
    {
        private readonly IOwnedLocationService _ownedLocation;
        private readonly IOffersCatalogService _offers;

        public OffersController(
            IOwnedLocationService ownedLocation,
            IOffersCatalogService offers
        )
        {
            _ownedLocation = ownedLocation;
            _offers = offers;
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
        public async Task<IActionResult> GetOffer(int offerId)
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
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

            return Ok(new
            {
                success = true,
                offer,
            });
        }
    }
}
