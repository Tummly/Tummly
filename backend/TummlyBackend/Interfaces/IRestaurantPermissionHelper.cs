using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using TummlyBackend.Helpers;
using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    public interface IRestaurantPermissionHelper
    {
        Task<RestaurantPermissionDecision> AuthorizeAsync(
            ClaimsPrincipal user,
            string areaId,
            PermissionLevel minimum
        );

        Task<RestaurantPermissionDecision> AuthorizeLocationAsync(
            ClaimsPrincipal user,
            string areaId,
            PermissionLevel minimum,
            int locationId
        );

        Task<RestaurantPermissionDecision> AuthorizeLocationSetAsync(
            ClaimsPrincipal user,
            string areaId,
            PermissionLevel minimum
        );

        Task<RestaurantPermissionDecision> AuthorizeNamedLocationIdsAsync(
            IReadOnlyList<int> allowedLocationIds,
            int[] namedLocationIds
        );

        Task<RestaurantPermissionDecision> AuthorizeUserAsync(
            int userId,
            string areaId,
            PermissionLevel minimum
        );

        Task<RestaurantPermissionDecision> AuthorizeLocationForUserAsync(
            int userId,
            string areaId,
            PermissionLevel minimum,
            int locationId
        );
    }

    public enum RestaurantPermissionStatus
    {
        Allowed,
        Forbidden,
        NotFound,
    }

    public sealed class RestaurantPermissionDecision
    {
        public RestaurantPermissionStatus Status { get; init; }

        public int RestaurantId { get; init; }

        public RestaurantLocation? Location { get; init; }

        public IReadOnlyList<int> LocationIds { get; init; } = [];

        public string Message { get; init; } =
            "You do not have access to this restaurant.";

        public static RestaurantPermissionDecision Allow(int restaurantId)
        {
            return new RestaurantPermissionDecision
            {
                Status = RestaurantPermissionStatus.Allowed,
                RestaurantId = restaurantId,
            };
        }

        public static RestaurantPermissionDecision AllowLocation(
            int restaurantId,
            RestaurantLocation location,
            IReadOnlyList<int> locationIds
        )
        {
            return new RestaurantPermissionDecision
            {
                Status = RestaurantPermissionStatus.Allowed,
                RestaurantId = restaurantId,
                Location = location,
                LocationIds = locationIds,
            };
        }

        public static RestaurantPermissionDecision AllowSet(
            int restaurantId,
            IReadOnlyList<int> locationIds
        )
        {
            return new RestaurantPermissionDecision
            {
                Status = RestaurantPermissionStatus.Allowed,
                RestaurantId = restaurantId,
                LocationIds = locationIds,
            };
        }

        public static RestaurantPermissionDecision Deny()
        {
            return new RestaurantPermissionDecision
            {
                Status = RestaurantPermissionStatus.Forbidden,
            };
        }

        public static RestaurantPermissionDecision DenyLocation()
        {
            return new RestaurantPermissionDecision
            {
                Status = RestaurantPermissionStatus.Forbidden,
                Message = "You do not have access to this location.",
            };
        }

        public static RestaurantPermissionDecision NotFoundLocation()
        {
            return new RestaurantPermissionDecision
            {
                Status = RestaurantPermissionStatus.NotFound,
                Message = "Location not found.",
            };
        }

        public IActionResult? ToForbiddenResult()
        {
            if (Status != RestaurantPermissionStatus.Forbidden)
            {
                return null;
            }

            return ForbiddenResult(Message);
        }

        public IActionResult? ToHttpResult()
        {
            return Status switch
            {
                RestaurantPermissionStatus.Allowed => null,
                RestaurantPermissionStatus.Forbidden => ForbiddenResult(Message),
                RestaurantPermissionStatus.NotFound => new NotFoundObjectResult(new
                {
                    success = false,
                    message = Message,
                }),
                _ => ForbiddenResult(Message),
            };
        }

        private static ObjectResult ForbiddenResult(string message)
        {
            return new ObjectResult(new
            {
                success = false,
                message,
            })
            {
                StatusCode = StatusCodes.Status403Forbidden,
            };
        }
    }
}
