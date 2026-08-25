using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using TummlyBackend.Helpers;

namespace TummlyBackend.Interfaces
{
    public interface IRestaurantPermissionHelper
    {
        Task<RestaurantPermissionDecision> AuthorizeAsync(
            ClaimsPrincipal user,
            string areaId,
            PermissionLevel minimum
        );
    }

    public enum RestaurantPermissionStatus
    {
        Allowed,
        Forbidden,
    }

    public sealed class RestaurantPermissionDecision
    {
        public RestaurantPermissionStatus Status { get; init; }

        public int RestaurantId { get; init; }

        public static RestaurantPermissionDecision Allow(int restaurantId)
        {
            return new RestaurantPermissionDecision
            {
                Status = RestaurantPermissionStatus.Allowed,
                RestaurantId = restaurantId,
            };
        }

        public static RestaurantPermissionDecision Deny()
        {
            return new RestaurantPermissionDecision
            {
                Status = RestaurantPermissionStatus.Forbidden,
            };
        }

        public IActionResult? ToForbiddenResult()
        {
            if (Status != RestaurantPermissionStatus.Forbidden)
            {
                return null;
            }

            return new ObjectResult(new
            {
                success = false,
                message = "You do not have access to this restaurant.",
            })
            {
                StatusCode = StatusCodes.Status403Forbidden,
            };
        }
    }
}
