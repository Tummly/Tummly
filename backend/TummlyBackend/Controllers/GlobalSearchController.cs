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
            [FromQuery] int utcOffsetMinutes = 0,
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
            var requested = ResolveTypes(types);

            var searchGuests = false;
            var searchFeedback = false;
            var searchCampaigns = false;
            var searchOffers = false;
            var searchQrCodes = false;
            var emptyGuests = false;
            var emptyFeedback = false;
            var emptyCampaigns = false;
            var emptyOffers = false;
            var emptyQrCodes = false;

            if (requested.IncludeGuests)
            {
                var guestsAccess = await _permissions.AuthorizeLocationAsync(
                    User,
                    OperatorAreaIds.Guests,
                    PermissionLevel.View,
                    locationId
                );

                if (guestsAccess.Status == RestaurantPermissionStatus.Allowed)
                {
                    searchGuests = true;
                }
                else
                {
                    // Owned location but Guests NoAccess: empty group (no existence leak).
                    emptyGuests = true;
                }
            }

            if (requested.IncludeFeedback)
            {
                var feedbackAccess = await _permissions.AuthorizeLocationAsync(
                    User,
                    OperatorAreaIds.Feedback,
                    PermissionLevel.View,
                    locationId
                );

                if (feedbackAccess.Status == RestaurantPermissionStatus.Allowed)
                {
                    searchFeedback = true;
                }
                else
                {
                    emptyFeedback = true;
                }
            }

            if (requested.IncludeCampaigns)
            {
                var campaignsAccess = await _permissions.AuthorizeLocationAsync(
                    User,
                    OperatorAreaIds.Campaigns,
                    PermissionLevel.View,
                    locationId
                );

                if (campaignsAccess.Status == RestaurantPermissionStatus.Allowed)
                {
                    searchCampaigns = true;
                }
                else
                {
                    emptyCampaigns = true;
                }
            }

            if (requested.IncludeOffers)
            {
                var offersAccess = await _permissions.AuthorizeLocationAsync(
                    User,
                    OperatorAreaIds.Offers,
                    PermissionLevel.View,
                    locationId
                );

                if (offersAccess.Status == RestaurantPermissionStatus.Allowed)
                {
                    searchOffers = true;
                }
                else
                {
                    emptyOffers = true;
                }
            }

            if (requested.IncludeQrCodes)
            {
                var captureAccess = await _permissions.AuthorizeLocationAsync(
                    User,
                    OperatorAreaIds.Capture,
                    PermissionLevel.View,
                    locationId
                );

                if (captureAccess.Status == RestaurantPermissionStatus.Allowed)
                {
                    searchQrCodes = true;
                }
                else
                {
                    emptyQrCodes = true;
                }
            }

            var result = await _globalSearch.SearchAsync(
                new GlobalSearchQuery
                {
                    Q = trimmedQ,
                    LocationId = locationId,
                    LocationName = location.LocationName,
                    Limit = clampedLimit,
                    IncludeGuests = searchGuests,
                    IncludeFeedback = searchFeedback,
                    IncludeCampaigns = searchCampaigns,
                    IncludeOffers = searchOffers,
                    IncludeQrCodes = searchQrCodes,
                    UtcOffsetMinutes = utcOffsetMinutes,
                },
                cancellationToken
            );

            var groups = new List<GlobalSearchGroupDto>();
            if (emptyGuests)
            {
                groups.Add(new GlobalSearchGroupDto { Type = "guests", Hits = [] });
            }

            if (emptyFeedback)
            {
                groups.Add(
                    new GlobalSearchGroupDto { Type = "feedback", Hits = [] }
                );
            }

            if (emptyCampaigns)
            {
                groups.Add(
                    new GlobalSearchGroupDto { Type = "campaigns", Hits = [] }
                );
            }

            if (emptyOffers)
            {
                groups.Add(new GlobalSearchGroupDto { Type = "offers", Hits = [] });
            }

            if (emptyQrCodes)
            {
                groups.Add(
                    new GlobalSearchGroupDto { Type = "qr-codes", Hits = [] }
                );
            }

            groups.AddRange(result.Groups);

            // Stable product order: guests, feedback, campaigns, offers, qr-codes.
            groups = groups
                .OrderBy(group => group.Type switch
                {
                    "guests" => 0,
                    "feedback" => 1,
                    "campaigns" => 2,
                    "offers" => 3,
                    "qr-codes" => 4,
                    _ => 99,
                })
                .ToList();

            return Ok(
                new GlobalSearchResponse
                {
                    Success = true,
                    Q = trimmedQ,
                    LocationId = locationId,
                    Groups = groups,
                }
            );
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
        /// Empty <c>types</c> defaults to guests, feedback, campaigns, offers,
        /// and qr-codes. Unknown tokens are ignored; empty resolved set still
        /// returns an empty groups list.
        /// </summary>
        private static RequestedTypes ResolveTypes(string? types)
        {
            if (string.IsNullOrWhiteSpace(types))
            {
                return new RequestedTypes(
                    IncludeGuests: true,
                    IncludeFeedback: true,
                    IncludeCampaigns: true,
                    IncludeOffers: true,
                    IncludeQrCodes: true
                );
            }

            var tokens = types
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(token => token.ToLowerInvariant())
                .ToHashSet(StringComparer.Ordinal);

            return new RequestedTypes(
                IncludeGuests: tokens.Contains("guests"),
                IncludeFeedback: tokens.Contains("feedback"),
                IncludeCampaigns: tokens.Contains("campaigns"),
                IncludeOffers: tokens.Contains("offers"),
                IncludeQrCodes: tokens.Contains("qr-codes")
            );
        }

        private sealed record RequestedTypes(
            bool IncludeGuests,
            bool IncludeFeedback,
            bool IncludeCampaigns,
            bool IncludeOffers,
            bool IncludeQrCodes
        );
    }
}
