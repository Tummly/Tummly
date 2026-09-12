using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TummlyBackend.DTOs.Search;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [Route("api/search")]
    [Authorize]
    public class GlobalSearchController : ControllerBase
    {
        private const int DefaultLimit = 5;
        private const int MinLimit = 1;
        private const int MaxLimit = 10;

        private readonly IGlobalSearchService _globalSearch;
        private readonly IOwnedLocationService _ownedLocation;
        private readonly IRestaurantPermissionHelper _permissions;

        public GlobalSearchController(
            IGlobalSearchService globalSearch,
            IOwnedLocationService ownedLocation,
            IRestaurantPermissionHelper permissions
        )
        {
            _globalSearch = globalSearch;
            _ownedLocation = ownedLocation;
            _permissions = permissions;
        }

        [HttpGet]
        public async Task<IActionResult> Search(
            [FromQuery] string? q,
            [FromQuery] int locationId,
            [FromQuery] string? types = null,
            [FromQuery] int? limit = null,
            CancellationToken cancellationToken = default
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var ownedLocation =
                await _ownedLocation.ResolveAsync(userId, locationId);
            var denied = OwnedLocationResponses.FromResult(ownedLocation);
            if (denied != null)
            {
                return denied;
            }

            var location = ownedLocation.Location!;
            var trimmedQ = (q ?? string.Empty).Trim();
            var clampedLimit = ClampLimit(limit);
            var includeGuests = ResolveIncludeGuests(types);

            if (includeGuests)
            {
                var guestsAccess = await _permissions.AuthorizeLocationAsync(
                    User,
                    OperatorAreaIds.Guests,
                    PermissionLevel.View,
                    locationId
                );

                if (guestsAccess.Status != RestaurantPermissionStatus.Allowed)
                {
                    // Owned location but Guests NoAccess: empty group (no existence leak).
                    return Ok(
                        new GlobalSearchResponse
                        {
                            Success = true,
                            Q = trimmedQ,
                            LocationId = locationId,
                            Groups =
                            [
                                new GlobalSearchGroupDto
                                {
                                    Type = "guests",
                                    Hits = [],
                                },
                            ],
                        }
                    );
                }
            }

            var result = await _globalSearch.SearchAsync(
                new GlobalSearchQuery
                {
                    Q = trimmedQ,
                    LocationId = locationId,
                    LocationName = location.LocationName,
                    Limit = clampedLimit,
                    IncludeGuests = includeGuests,
                },
                cancellationToken
            );

            return Ok(result);
        }

        private static int ClampLimit(int? limit)
        {
            var value = limit ?? DefaultLimit;
            if (value < MinLimit)
            {
                return MinLimit;
            }

            if (value > MaxLimit)
            {
                return MaxLimit;
            }

            return value;
        }

        /// <summary>
        /// Default types=guests for this ticket. Unknown tokens are ignored;
        /// empty resolved set still returns an empty groups list.
        /// </summary>
        private static bool ResolveIncludeGuests(string? types)
        {
            if (string.IsNullOrWhiteSpace(types))
            {
                return true;
            }

            var tokens = types
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(token => token.ToLowerInvariant())
                .ToHashSet(StringComparer.Ordinal);

            return tokens.Contains("guests");
        }
    }
}
