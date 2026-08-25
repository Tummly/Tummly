using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TummlyBackend.DTOs.Guests;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [Route("api/guests")]
    [Authorize]
    public class GuestsController : ControllerBase
    {
        private static readonly HashSet<string> DeferredFilterParams =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "recovery",
                "engagement",
                "unsubscribed",
                "suppressed",
                "invalid-contact",
                "invalidContact",
                "email-and-mobile",
                "emailAndMobile",
            };

        private readonly IRestaurantPermissionHelper _permissions;
        private readonly IGuestsEffectiveLocationService _effectiveLocations;
        private readonly IGuestsListService _guestsList;
        private readonly IGuestsExportService _guestsExport;
        private readonly IGuestProfileService _guestProfile;
        private readonly IGuestTaggingService _guestTagging;
        private readonly IGuestActivityListService _guestActivity;
        private readonly IGuestFeedbacksListService _guestFeedbacks;
        private readonly IGuestNotesService _guestNotes;
        private readonly IGuestIdentityUpdateService _guestIdentity;
        private readonly IGuestMarketingPreferenceUpdateService _guestMarketingPreference;
        private readonly ILocationGuestDeleteService _guestDelete;

        public GuestsController(
            IRestaurantPermissionHelper permissions,
            IGuestsEffectiveLocationService effectiveLocations,
            IGuestsListService guestsList,
            IGuestsExportService guestsExport,
            IGuestProfileService guestProfile,
            IGuestTaggingService guestTagging,
            IGuestActivityListService guestActivity,
            IGuestFeedbacksListService guestFeedbacks,
            IGuestNotesService guestNotes,
            IGuestIdentityUpdateService guestIdentity,
            IGuestMarketingPreferenceUpdateService guestMarketingPreference,
            ILocationGuestDeleteService guestDelete
        )
        {
            _permissions = permissions;
            _effectiveLocations = effectiveLocations;
            _guestsList = guestsList;
            _guestsExport = guestsExport;
            _guestProfile = guestProfile;
            _guestTagging = guestTagging;
            _guestActivity = guestActivity;
            _guestFeedbacks = guestFeedbacks;
            _guestNotes = guestNotes;
            _guestIdentity = guestIdentity;
            _guestMarketingPreference = guestMarketingPreference;
            _guestDelete = guestDelete;
        }

        [HttpGet]
        public async Task<IActionResult> GetGuests(
            [FromQuery] int locationId,
            [FromQuery] string smartGroup = "all-guests",
            [FromQuery] string? q = null,
            [FromQuery] string sort = "recent-activity",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 25,
            [FromQuery] string[]? marketing = null,
            [FromQuery] string[]? contact = null,
            [FromQuery] string[]? sentiment = null,
            [FromQuery] int[]? tagIds = null,
            [FromQuery] string? dateAxis = null,
            [FromQuery] string? datePreset = null,
            [FromQuery] DateTime? dateFrom = null,
            [FromQuery] DateTime? dateTo = null,
            [FromQuery] string? locationScope = null,
            [FromQuery] int[]? locationIds = null,
            [FromQuery] string? overviewDatePreset = null,
            [FromQuery] DateTime? overviewDateFrom = null,
            [FromQuery] DateTime? overviewDateTo = null,
            [FromQuery] int utcOffsetMinutes = 0,
            [FromQuery] bool includeAggregates = true
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var deferred = DeferredFilterParams
                .FirstOrDefault(key => Request.Query.ContainsKey(key));

            if (deferred != null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = $"Filter '{deferred}' is not supported.",
                });
            }

            var ownedLocation = await GateLocationAsync(
                locationId
            );
            var denied = ownedLocation.ToHttpResult();
            if (denied != null)
            {
                return denied;
            }

            try
            {
                var effectiveLocations = await _effectiveLocations.ResolveAsync(
                    ownedLocation.LocationIds,
                    ownedLocation.Location!,
                    locationScope,
                    locationIds
                );

                if (effectiveLocations.Status
                    == GuestsEffectiveLocationStatus.Forbidden)
                {
                    return StatusCode(
                        StatusCodes.Status403Forbidden,
                        new
                        {
                            success = false,
                            message = effectiveLocations.ErrorMessage,
                        }
                    );
                }

                var result = await _guestsList.GetListAsync(
                    new GuestsListQuery
                    {
                        LocationIds = effectiveLocations.LocationIds!,
                        LocationNamesById = effectiveLocations.LocationNamesById!,
                        ShellLocationId = locationId,
                        RestaurantId = ownedLocation.Location!.RestaurantId,
                        SmartGroup = smartGroup,
                        Q = q,
                        Sort = sort,
                        Page = page,
                        PageSize = pageSize,
                        Marketing = marketing ?? Array.Empty<string>(),
                        Contact = contact ?? Array.Empty<string>(),
                        Sentiment = sentiment ?? Array.Empty<string>(),
                        TagIds = tagIds ?? Array.Empty<int>(),
                        DateAxis = dateAxis,
                        DatePreset = datePreset,
                        DateFrom = dateFrom,
                        DateTo = dateTo,
                        OverviewDatePreset = overviewDatePreset,
                        OverviewDateFrom = overviewDateFrom,
                        OverviewDateTo = overviewDateTo,
                        UtcOffsetMinutes = utcOffsetMinutes,
                        IncludeAggregates = includeAggregates,
                    }
                );

                return Ok(result);
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

        [HttpGet("export")]
        public async Task<IActionResult> ExportGuests(
            [FromQuery] int locationId,
            [FromQuery] string smartGroup = "all-guests",
            [FromQuery] string? q = null,
            [FromQuery] string sort = "recent-activity",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 25,
            [FromQuery] string[]? marketing = null,
            [FromQuery] string[]? contact = null,
            [FromQuery] string[]? sentiment = null,
            [FromQuery] int[]? tagIds = null,
            [FromQuery] string? dateAxis = null,
            [FromQuery] string? datePreset = null,
            [FromQuery] DateTime? dateFrom = null,
            [FromQuery] DateTime? dateTo = null,
            [FromQuery] string? locationScope = null,
            [FromQuery] int[]? locationIds = null,
            [FromQuery] int utcOffsetMinutes = 0
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            IReadOnlyList<int>? selectedGuestIds = null;
            if (Request.Query.ContainsKey("guestIds"))
            {
                var parsed = new List<int>();
                foreach (var value in Request.Query["guestIds"])
                {
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        continue;
                    }

                    if (!int.TryParse(value, out var guestId))
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = "guestIds must be integers.",
                        });
                    }

                    parsed.Add(guestId);
                }

                if (parsed.Count == 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "guestIds is required for selected export.",
                    });
                }

                selectedGuestIds = parsed;
            }

            var ownedLocation = await GateLocationAsync(
                locationId
            );
            var denied = ownedLocation.ToHttpResult();
            if (denied != null)
            {
                return denied;
            }

            try
            {
                // Selected mode: guestIds are the scope — do not apply list
                // Filters / Location override. Shell locationId is auth only.
                if (selectedGuestIds != null)
                {
                    var selectedResult = await _guestsExport.ExportAsync(
                        new GuestsExportQuery
                        {
                            LocationIds = new[] { locationId },
                            LocationNamesById = new Dictionary<int, string>
                            {
                                [locationId] =
                                    ownedLocation.Location!.LocationName,
                            },
                            ShellLocationId = locationId,
                            RestaurantId = ownedLocation.Location!.RestaurantId,
                            OwnerUserId = userId,
                            GuestIds = selectedGuestIds,
                            LocationScopeToken = locationId.ToString(),
                        }
                    );

                    return File(
                        selectedResult.Content,
                        selectedResult.ContentType,
                        selectedResult.FileName
                    );
                }

                var deferred = DeferredFilterParams
                    .FirstOrDefault(key => Request.Query.ContainsKey(key));

                if (deferred != null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = $"Filter '{deferred}' is not supported.",
                    });
                }

                var effectiveLocations = await _effectiveLocations.ResolveAsync(
                    ownedLocation.LocationIds,
                    ownedLocation.Location!,
                    locationScope,
                    locationIds
                );

                if (effectiveLocations.Status
                    == GuestsEffectiveLocationStatus.Forbidden)
                {
                    return StatusCode(
                        StatusCodes.Status403Forbidden,
                        new
                        {
                            success = false,
                            message = effectiveLocations.ErrorMessage,
                        }
                    );
                }

                var scopeToken = _effectiveLocations.ResolveScopeToken(
                    locationScope,
                    locationIds,
                    locationId
                );

                // page/pageSize accepted on the wire but ignored for export.
                _ = page;
                _ = pageSize;

                var result = await _guestsExport.ExportAsync(
                    new GuestsExportQuery
                    {
                        LocationIds = effectiveLocations.LocationIds!,
                        LocationNamesById = effectiveLocations.LocationNamesById!,
                        ShellLocationId = locationId,
                        RestaurantId = ownedLocation.Location!.RestaurantId,
                        OwnerUserId = userId,
                        GuestIds = null,
                        SmartGroup = smartGroup,
                        Q = q,
                        Sort = sort,
                        Marketing = marketing ?? Array.Empty<string>(),
                        Contact = contact ?? Array.Empty<string>(),
                        Sentiment = sentiment ?? Array.Empty<string>(),
                        TagIds = tagIds ?? Array.Empty<int>(),
                        DateAxis = dateAxis,
                        DatePreset = datePreset,
                        DateFrom = dateFrom,
                        DateTo = dateTo,
                        UtcOffsetMinutes = utcOffsetMinutes,
                        LocationScopeToken = scopeToken,
                    }
                );

                return File(
                    result.Content,
                    result.ContentType,
                    result.FileName
                );
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

        [HttpGet("tags")]
        public async Task<IActionResult> ListGuestTags(
            [FromQuery] int locationId,
            [FromQuery] string? locationScope = null,
            [FromQuery] int[]? locationIds = null
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var ownedLocation = await GateLocationAsync(
                locationId
            );
            var denied = ownedLocation.ToHttpResult();
            if (denied != null)
            {
                return denied;
            }

            try
            {
                var effectiveLocations = await _effectiveLocations.ResolveAsync(
                    ownedLocation.LocationIds,
                    ownedLocation.Location!,
                    locationScope,
                    locationIds
                );

                if (effectiveLocations.Status
                    == GuestsEffectiveLocationStatus.Forbidden)
                {
                    return StatusCode(
                        StatusCodes.Status403Forbidden,
                        new
                        {
                            success = false,
                            message = effectiveLocations.ErrorMessage,
                        }
                    );
                }

                var restaurantId = ownedLocation.Location!.RestaurantId;
                var tags = await _guestTagging.ListForLocationScopeAsync(
                    restaurantId,
                    effectiveLocations.LocationIds!
                );

                return Ok(new
                {
                    success = true,
                    tags = tags.Select(t => new
                    {
                        id = t.Id,
                        name = t.Name,
                        guestCount = t.GuestCount,
                        aiSourced = t.AiSourced,
                    }),
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

        [HttpGet("tags/memberships")]
        public async Task<IActionResult> ListGuestTagMemberships(
            [FromQuery] int locationId,
            [FromQuery] int[]? guestIds = null
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var ownedLocation = await GateLocationAsync(
                locationId
            );
            var denied = ownedLocation.ToHttpResult();
            if (denied != null)
            {
                return denied;
            }

            if (guestIds == null || guestIds.Length == 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Guest ids are required.",
                });
            }

            try
            {
                var ownedLocationIds = ownedLocation.LocationIds;

                var memberships = await _guestTagging.GetMembershipsForGuestsAsync(
                    ownedLocation.Location!.RestaurantId,
                    ownedLocationIds,
                    guestIds
                );

                return Ok(new
                {
                    success = true,
                    memberships = memberships.Select(pair => new
                    {
                        guestId = pair.Key,
                        tagIds = pair.Value,
                    }),
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

        [HttpPost("tags")]
        public async Task<IActionResult> CreateGuestTag(
            [FromQuery] int locationId,
            [FromBody] CreateGuestTagDto dto
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var ownedLocation = await GateLocationAsync(
                locationId
            );
            var denied = ownedLocation.ToHttpResult();
            if (denied != null)
            {
                return denied;
            }

            try
            {
                var tag = await _guestTagging.CreateByNameAsync(
                    ownedLocation.Location!.RestaurantId,
                    dto.Name
                );

                return Ok(new
                {
                    success = true,
                    tag = new
                    {
                        id = tag.Id,
                        name = tag.DisplayName,
                        aiSourced = tag.AiSourced,
                    },
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

        [HttpPost("tags/apply")]
        public async Task<IActionResult> ApplyGuestTags(
            [FromQuery] int locationId,
            [FromBody] ApplyGuestTagsDto dto
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var ownedLocation = await GateLocationAsync(
                locationId
            );
            var denied = ownedLocation.ToHttpResult();
            if (denied != null)
            {
                return denied;
            }

            if (
                dto.GuestIds == null
                || dto.GuestIds.Count == 0
                || dto.TagIds == null
                || dto.TagIds.Count == 0
            )
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Guest ids and tag ids are required.",
                });
            }

            try
            {
                var ownedLocationIds = ownedLocation.LocationIds;

                await _guestTagging.ApplyAdditiveAsync(
                    ownedLocation.Location!.RestaurantId,
                    ownedLocationIds,
                    dto.GuestIds,
                    dto.TagIds
                );

                return Ok(new { success = true });
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

        [HttpPost("tags/sync")]
        public async Task<IActionResult> SyncGuestTags(
            [FromQuery] int locationId,
            [FromBody] SyncGuestTagsDto dto
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var ownedLocation = await GateLocationAsync(
                locationId
            );
            var denied = ownedLocation.ToHttpResult();
            if (denied != null)
            {
                return denied;
            }

            if (dto.GuestIds == null || dto.GuestIds.Count == 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Guest ids are required.",
                });
            }

            if (dto.TagIds == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Tag ids are required.",
                });
            }

            try
            {
                var ownedLocationIds = ownedLocation.LocationIds;

                await _guestTagging.SyncMembershipsAsync(
                    ownedLocation.Location!.RestaurantId,
                    ownedLocationIds,
                    dto.GuestIds,
                    dto.TagIds
                );

                return Ok(new { success = true });
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

        [HttpGet("{guestId:int}")]
        public async Task<IActionResult> GetGuestProfile(
            int guestId,
            [FromQuery] int locationId
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var ownedLocation = await GateLocationAsync(
                locationId
            );
            var denied = ownedLocation.ToHttpResult();
            if (denied != null)
            {
                return denied;
            }

            var result = await _guestProfile.GetDetailAsync(
                guestId,
                locationId,
                ownedLocation.Location!.LocationName
            );

            if (result == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Guest not found.",
                });
            }

            return Ok(result);
        }

        [HttpPatch("{guestId:int}")]
        public async Task<IActionResult> PatchGuestIdentity(
            int guestId,
            [FromQuery] int locationId,
            [FromBody] PatchGuestIdentityRequest request
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var ownedLocation = await GateLocationAsync(
                locationId
            );
            var denied = ownedLocation.ToHttpResult();
            if (denied != null)
            {
                return denied;
            }

            var outcome = await _guestIdentity.UpdateAsync(
                guestId,
                locationId,
                request
            );

            return outcome.Status switch
            {
                GuestIdentityUpdateStatus.Updated => Ok(outcome.Result),
                GuestIdentityUpdateStatus.NotFound => NotFound(new
                {
                    success = false,
                    message = outcome.ErrorMessage,
                }),
                GuestIdentityUpdateStatus.ValidationError => BadRequest(new
                {
                    success = false,
                    message = outcome.ErrorMessage,
                }),
                GuestIdentityUpdateStatus.IdentityCollision => Conflict(new
                {
                    success = false,
                    message = outcome.ErrorMessage,
                }),
                _ => StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        success = false,
                        message = "Unexpected identity update status.",
                    }
                ),
            };
        }

        [HttpPatch("{guestId:int}/marketing-preference")]
        public async Task<IActionResult> PatchGuestMarketingPreference(
            int guestId,
            [FromQuery] int locationId,
            [FromBody] PatchGuestMarketingPreferenceRequest request
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var ownedLocation = await GateLocationAsync(
                locationId
            );
            var denied = ownedLocation.ToHttpResult();
            if (denied != null)
            {
                return denied;
            }

            var outcome = await _guestMarketingPreference.UpdateAsync(
                guestId,
                locationId,
                userId,
                request
            );

            return outcome.Status switch
            {
                GuestMarketingPreferenceUpdateStatus.Updated => Ok(outcome.Result),
                GuestMarketingPreferenceUpdateStatus.NotFound => NotFound(new
                {
                    success = false,
                    message = outcome.ErrorMessage,
                }),
                GuestMarketingPreferenceUpdateStatus.ValidationError => BadRequest(new
                {
                    success = false,
                    message = outcome.ErrorMessage,
                }),
                _ => StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        success = false,
                        message = "Unexpected marketing preference update status.",
                    }
                ),
            };
        }

        [HttpDelete("{guestId:int}")]
        public async Task<IActionResult> DeleteGuest(
            int guestId,
            [FromQuery] int locationId
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var outcome = await _guestDelete.DeleteAsync(
                userId,
                guestId,
                locationId
            );

            return outcome.Status switch
            {
                LocationGuestDeleteStatus.Deleted => NoContent(),
                LocationGuestDeleteStatus.NotFound => NotFound(new
                {
                    success = false,
                    message = outcome.ErrorMessage,
                }),
                LocationGuestDeleteStatus.Forbidden => StatusCode(
                    StatusCodes.Status403Forbidden,
                    new
                    {
                        success = false,
                        message = outcome.ErrorMessage,
                    }
                ),
                _ => StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        success = false,
                        message = "Unexpected guest delete status.",
                    }
                ),
            };
        }

        [HttpGet("{guestId:int}/feedbacks")]
        public async Task<IActionResult> GetGuestFeedbacks(
            int guestId,
            [FromQuery] int locationId,
            [FromQuery] string? q = null,
            [FromQuery] string[]? sentiment = null,
            [FromQuery] string[]? detectedTags = null,
            [FromQuery] string? datePreset = null,
            [FromQuery] DateTime? dateFrom = null,
            [FromQuery] DateTime? dateTo = null,
            [FromQuery] string sort = "recent-activity",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 25,
            [FromQuery] int utcOffsetMinutes = 0
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var ownedLocation = await GateLocationAsync(
                locationId
            );
            var denied = ownedLocation.ToHttpResult();
            if (denied != null)
            {
                return denied;
            }

            try
            {
                var result = await _guestFeedbacks.ListAsync(
                    guestId,
                    locationId,
                    ownedLocation.Location!.LocationName,
                    q,
                    sentiment,
                    detectedTags,
                    datePreset,
                    dateFrom,
                    dateTo,
                    sort,
                    page,
                    pageSize,
                    utcOffsetMinutes
                );

                if (result == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Guest not found.",
                    });
                }

                return Ok(new
                {
                    items = result.Items,
                    totalCount = result.TotalCount,
                    page = result.Page,
                    pageSize = result.PageSize,
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

        [HttpGet("{guestId:int}/activity")]
        public async Task<IActionResult> GetGuestActivity(
            int guestId,
            [FromQuery] int locationId,
            [FromQuery] string[]? type = null,
            [FromQuery] string? datePreset = null,
            [FromQuery] DateTime? dateFrom = null,
            [FromQuery] DateTime? dateTo = null,
            [FromQuery] string sort = "recent-activity",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 25,
            [FromQuery] int utcOffsetMinutes = 0
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var ownedLocation = await GateLocationAsync(
                locationId
            );
            var denied = ownedLocation.ToHttpResult();
            if (denied != null)
            {
                return denied;
            }

            try
            {
                var result = await _guestActivity.ListAsync(
                    guestId,
                    locationId,
                    ownedLocation.Location!.LocationName,
                    type,
                    datePreset,
                    dateFrom,
                    dateTo,
                    sort,
                    page,
                    pageSize,
                    utcOffsetMinutes
                );

                if (result == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Guest not found.",
                    });
                }

                return Ok(new
                {
                    items = result.Items,
                    totalCount = result.TotalCount,
                    page = result.Page,
                    pageSize = result.PageSize,
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

        [HttpGet("{guestId:int}/notes")]
        public async Task<IActionResult> GetGuestNotes(
            int guestId,
            [FromQuery] int locationId,
            [FromQuery] int limit = 50
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var ownedLocation = await GateLocationAsync(
                locationId
            );
            var denied = ownedLocation.ToHttpResult();
            if (denied != null)
            {
                return denied;
            }

            var result = await _guestNotes.ListAsync(
                guestId,
                locationId,
                limit
            );

            if (result == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Guest not found.",
                });
            }

            return Ok(new
            {
                items = result.Items,
                totalCount = result.TotalCount,
            });
        }

        [HttpPost("{guestId:int}/notes")]
        public async Task<IActionResult> CreateGuestNote(
            int guestId,
            [FromQuery] int locationId,
            [FromBody] CreateGuestNoteRequest request
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var ownedLocation = await GateLocationAsync(
                locationId
            );
            var denied = ownedLocation.ToHttpResult();
            if (denied != null)
            {
                return denied;
            }

            try
            {
                var note = await _guestNotes.CreateAsync(
                    guestId,
                    locationId,
                    userId,
                    request.Body
                );

                if (note == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Guest not found.",
                    });
                }

                return Ok(new
                {
                    success = true,
                    note,
                });
            }
            catch (InvalidOperationException ex)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = ex.Message,
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

        [HttpPut("{guestId:int}/notes/{noteId:int}")]
        public async Task<IActionResult> UpdateGuestNote(
            int guestId,
            int noteId,
            [FromQuery] int locationId,
            [FromBody] UpdateGuestNoteRequest request
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var ownedLocation = await GateLocationAsync(
                locationId
            );
            var denied = ownedLocation.ToHttpResult();
            if (denied != null)
            {
                return denied;
            }

            try
            {
                var note = await _guestNotes.UpdateAsync(
                    guestId,
                    locationId,
                    noteId,
                    userId,
                    request.Body
                );

                if (note == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Note not found.",
                    });
                }

                return Ok(new
                {
                    success = true,
                    note,
                });
            }
            catch (InvalidOperationException ex)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = ex.Message,
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

        [HttpDelete("{guestId:int}/notes/{noteId:int}")]
        public async Task<IActionResult> SoftDeleteGuestNote(
            int guestId,
            int noteId,
            [FromQuery] int locationId
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var ownedLocation = await GateLocationAsync(
                locationId
            );
            var denied = ownedLocation.ToHttpResult();
            if (denied != null)
            {
                return denied;
            }

            try
            {
                var deleted = await _guestNotes.SoftDeleteAsync(
                    guestId,
                    locationId,
                    noteId,
                    userId
                );

                if (deleted == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Note not found.",
                    });
                }

                return Ok(new
                {
                    success = true,
                    deletedAt = deleted.DeletedAt,
                    deletedByDisplayName = deleted.DeletedByDisplayName,
                });
            }
            catch (InvalidOperationException ex)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = ex.Message,
                });
            }
        }

        private async Task<RestaurantPermissionDecision> GateLocationAsync(
            int locationId
        )
        {
            var minimum = HttpMethods.IsGet(Request.Method)
                ? PermissionLevel.View
                : PermissionLevel.Manage;
            return await _permissions.AuthorizeLocationAsync(
                User,
                OperatorAreaIds.Guests,
                minimum,
                locationId
            );
        }
    }
}
