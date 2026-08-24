using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TummlyBackend.DTOs.Offers;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [Route("api/offers/{offerId:int}/recommendation")]
    [Authorize]
    public class OfferRecommendationController : ControllerBase
    {
        private readonly IOfferRecommendationService _offerRecommendation;
        private readonly IOwnedLocationService _ownedLocation;

        public OfferRecommendationController(
            IOfferRecommendationService offerRecommendation,
            IOwnedLocationService ownedLocation
        )
        {
            _offerRecommendation = offerRecommendation;
            _ownedLocation = ownedLocation;
        }

        [HttpPost]
        public async Task<IActionResult> Recommend(
            int offerId,
            [FromBody] OfferRecommendationRequest request
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
                var result = await _offerRecommendation.RecommendAsync(
                    userId,
                    offerId,
                    request
                );

                return result switch
                {
                    OfferRecommendationServiceResult.Ok ok => Ok(new
                    {
                        success = true,
                        recommendation = ok.Recommendation,
                    }),
                    OfferRecommendationServiceResult.NotFound => NotFound(new
                    {
                        success = false,
                        message = "Offer not found.",
                        retryable = false,
                    }),
                    OfferRecommendationServiceResult.WrongLocation => BadRequest(new
                    {
                        success = false,
                        message = "Offer is not at this location.",
                        retryable = false,
                    }),
                    OfferRecommendationServiceResult.Failed failed => StatusCode(
                        StatusCodes.Status502BadGateway,
                        new
                        {
                            success = false,
                            message = failed.Message,
                            retryable = failed.Retryable,
                        }
                    ),
                    _ => StatusCode(
                        StatusCodes.Status500InternalServerError,
                        new
                        {
                            success = false,
                            message = "Unexpected offer recommendation result.",
                            retryable = true,
                        }
                    ),
                };
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message,
                    retryable = false,
                });
            }
        }
    }
}
