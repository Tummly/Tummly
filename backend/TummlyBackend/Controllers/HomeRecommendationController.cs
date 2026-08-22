using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TummlyBackend.DTOs.OperatorHome;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [Route("api/home/recommendation")]
    [Authorize]
    public class HomeRecommendationController : ControllerBase
    {
        private readonly IHomeRecommendationService _homeRecommendation;
        private readonly IOwnedLocationService _ownedLocation;

        public HomeRecommendationController(
            IHomeRecommendationService homeRecommendation,
            IOwnedLocationService ownedLocation
        )
        {
            _homeRecommendation = homeRecommendation;
            _ownedLocation = ownedLocation;
        }

        [HttpPost]
        public async Task<IActionResult> Recommend(
            [FromBody] HomeRecommendationRequest request
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
                var result = await _homeRecommendation.RecommendAsync(
                    userId,
                    request
                );

                return result switch
                {
                    HomeRecommendationServiceResult.Ok ok => Ok(new
                    {
                        success = true,
                        recommendation = ok.Recommendation,
                    }),
                    HomeRecommendationServiceResult.Failed failed => StatusCode(
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
                            message = "Unexpected home recommendation result.",
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
                });
            }
        }
    }
}
