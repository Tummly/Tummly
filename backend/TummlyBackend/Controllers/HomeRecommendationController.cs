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
        private readonly IRestaurantPermissionHelper _permissions;

        public HomeRecommendationController(
            IHomeRecommendationService homeRecommendation,
            IRestaurantPermissionHelper permissions
        )
        {
            _homeRecommendation = homeRecommendation;
            _permissions = permissions;
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

            var restaurant = await _permissions.AuthorizeLocationSetAsync(
                User,
                OperatorAreaIds.AccountWorkspace,
                PermissionLevel.View
            );
            var deniedRestaurant = restaurant.ToHttpResult();
            if (deniedRestaurant != null)
            {
                return deniedRestaurant;
            }

            var location = await _permissions.AuthorizeNamedLocationIdsAsync(
                restaurant.LocationIds,
                [request.LocationId]
            );
            var deniedLocation = location.ToHttpResult();
            if (deniedLocation != null)
            {
                return deniedLocation;
            }

            var allowedTypes = await AllowedRecommendationTypesAsync(
                request.LocationId
            );

            try
            {
                var result = await _homeRecommendation.RecommendAsync(
                    userId,
                    request,
                    allowedTypes
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

        private async Task<HashSet<string>> AllowedRecommendationTypesAsync(
            int locationId
        )
        {
            var allowed = new HashSet<string>(StringComparer.Ordinal);
            foreach (var type in HomeRecommendationContract.NativeTypes.Concat(
                HomeRecommendationContract.CampaignTypes
            ))
            {
                var areaId = HomeRecommendationContract.SourceAreaId(type);
                if (areaId == null)
                {
                    continue;
                }

                var decision = await _permissions.AuthorizeLocationAsync(
                    User,
                    areaId,
                    PermissionLevel.View,
                    locationId
                );
                if (decision.Status == RestaurantPermissionStatus.Allowed)
                {
                    allowed.Add(type);
                }
            }

            return allowed;
        }
    }
}
