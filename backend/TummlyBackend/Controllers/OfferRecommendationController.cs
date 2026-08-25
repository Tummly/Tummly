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
        private readonly IRestaurantPermissionHelper _permissions;
        private readonly IOfferRecommendationService _offerRecommendation;

        public OfferRecommendationController(
            IRestaurantPermissionHelper permissions,
            IOfferRecommendationService offerRecommendation
        )
        {
            _permissions = permissions;
            _offerRecommendation = offerRecommendation;
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

            var ownedLocation = await _permissions.AuthorizeLocationAsync(
                User,
                OperatorAreaIds.Offers,
                PermissionLevel.Manage,
                request.LocationId
            );
            var denied = ownedLocation.ToHttpResult();
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
