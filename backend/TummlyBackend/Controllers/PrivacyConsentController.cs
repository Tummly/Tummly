using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TummlyBackend.DTOs.PrivacyConsent;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Services;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [Route("api/privacy-consent")]
    [Authorize]
    public class PrivacyConsentController : ControllerBase
    {
        private readonly IRestaurantPermissionHelper _permissions;
        private readonly IPrivacyConsentSaveService _save;
        private readonly IPrivacyConsentService _privacyConsent;
        private readonly IPermissionRecordsListService _permissionRecords;

        public PrivacyConsentController(
            IRestaurantPermissionHelper permissions,
            IPrivacyConsentSaveService save,
            IPrivacyConsentService privacyConsent,
            IPermissionRecordsListService permissionRecords
        )
        {
            _permissions = permissions;
            _save = save;
            _privacyConsent = privacyConsent;
            _permissionRecords = permissionRecords;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var unauthorized = OperatorAuth.TryRequireUserId(User, out _);
            if (unauthorized != null)
            {
                return unauthorized;
            }

            var decision = await _permissions.AuthorizeAsync(
                User,
                OperatorAreaIds.PrivacyConsent,
                PermissionLevel.View
            );
            var denied = decision.ToHttpResult();
            if (denied != null)
            {
                return denied;
            }

            var actorCanManage =
                (
                    await _permissions.AuthorizeAsync(
                        User,
                        OperatorAreaIds.PrivacyConsent,
                        PermissionLevel.Manage
                    )
                ).Status == RestaurantPermissionStatus.Allowed;
            var canViewGuests =
                (
                    await _permissions.AuthorizeLocationSetAsync(
                        User,
                        OperatorAreaIds.Guests,
                        PermissionLevel.View
                    )
                ).Status == RestaurantPermissionStatus.Allowed;

            var result = await _privacyConsent.GetAsync(
                decision.RestaurantId,
                actorCanManage,
                canViewGuests
            );

            return result switch
            {
                PrivacyConsentGetResult.Ok ok => Ok(ok.Payload),
                PrivacyConsentGetResult.NotFound => NotFound(new
                {
                    success = false,
                    message = "Restaurant not found.",
                }),
                _ => StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        success = false,
                        message = "Unexpected privacy consent read result.",
                    }
                ),
            };
        }

        [HttpGet("permission-records")]
        public async Task<IActionResult> GetPermissionRecords(
            [FromQuery] string? q = null,
            [FromQuery] string[]? permission = null,
            [FromQuery] string[]? currentState = null,
            [FromQuery] string[]? location = null,
            [FromQuery] string? datePreset = null,
            [FromQuery] DateTime? dateFrom = null,
            [FromQuery] DateTime? dateTo = null,
            [FromQuery] string sort = "recent-activity",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = PermissionRecordsListService.DefaultPageSize,
            [FromQuery] int utcOffsetMinutes = 0
        )
        {
            var unauthorized = OperatorAuth.TryRequireUserId(User, out _);
            if (unauthorized != null)
            {
                return unauthorized;
            }

            var decision = await _permissions.AuthorizeLocationSetAsync(
                User,
                OperatorAreaIds.PrivacyConsent,
                PermissionLevel.View
            );
            var denied = decision.ToHttpResult();
            if (denied != null)
            {
                return denied;
            }

            try
            {
                var result = await _permissionRecords.ListAsync(
                    new PermissionRecordsListQuery
                    {
                        RestaurantId = decision.RestaurantId,
                        LocationIds = decision.LocationIds,
                        Q = q,
                        Permissions = permission ?? [],
                        CurrentStates = currentState ?? [],
                        Locations = location ?? [],
                        DatePreset = datePreset,
                        DateFrom = dateFrom,
                        DateTo = dateTo,
                        Sort = sort,
                        Page = page,
                        PageSize = pageSize,
                        UtcOffsetMinutes = utcOffsetMinutes,
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

        [HttpGet("activity")]
        public async Task<IActionResult> GetActivity()
        {
            var unauthorized = OperatorAuth.TryRequireUserId(User, out _);
            if (unauthorized != null)
            {
                return unauthorized;
            }

            var decision = await _permissions.AuthorizeAsync(
                User,
                OperatorAreaIds.PrivacyConsent,
                PermissionLevel.View
            );
            var denied = decision.ToHttpResult();
            if (denied != null)
            {
                return denied;
            }

            var result = await _privacyConsent.GetActivityAsync(
                decision.RestaurantId
            );
            return Ok(result);
        }

        [HttpPut]
        public async Task<IActionResult> Save(
            [FromBody] SavePrivacyConsentRequest request
        )
        {
            var unauthorized = OperatorAuth.TryRequireUserId(User, out var userId);
            if (unauthorized != null)
            {
                return unauthorized;
            }

            var decision = await _permissions.AuthorizeAsync(
                User,
                OperatorAreaIds.PrivacyConsent,
                PermissionLevel.Manage
            );
            var denied = decision.ToHttpResult();
            if (denied != null)
            {
                return denied;
            }

            var result = await _save.SaveAsync(
                decision.RestaurantId,
                userId,
                request
            );

            return result switch
            {
                PrivacyConsentSaveResult.Ok ok => Ok(new
                {
                    success = true,
                    privacyReady = ok.PrivacyReady,
                }),
                PrivacyConsentSaveResult.NotFound => NotFound(new
                {
                    success = false,
                    message = "Restaurant not found.",
                }),
                PrivacyConsentSaveResult.InvalidRequest invalid => BadRequest(
                    new
                    {
                        success = false,
                        message = invalid.Message,
                    }
                ),
                _ => StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        success = false,
                        message = "Unexpected privacy consent save result.",
                    }
                ),
            };
        }

        [HttpPatch]
        public async Task<IActionResult> PatchToggles(
            [FromBody] PatchPrivacyConsentTogglesRequest request
        )
        {
            var unauthorized = OperatorAuth.TryRequireUserId(User, out var userId);
            if (unauthorized != null)
            {
                return unauthorized;
            }

            var decision = await _permissions.AuthorizeAsync(
                User,
                OperatorAreaIds.PrivacyConsent,
                PermissionLevel.Manage
            );
            var denied = decision.ToHttpResult();
            if (denied != null)
            {
                return denied;
            }

            var result = await _privacyConsent.PatchTogglesAsync(
                decision.RestaurantId,
                userId,
                request
            );

            return result switch
            {
                PrivacyConsentPatchResult.Ok => Ok(new { success = true }),
                PrivacyConsentPatchResult.NotFound => NotFound(new
                {
                    success = false,
                    message = "Restaurant not found.",
                }),
                PrivacyConsentPatchResult.InvalidRequest invalid => BadRequest(
                    new
                    {
                        success = false,
                        message = invalid.Message,
                    }
                ),
                _ => StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        success = false,
                        message = "Unexpected privacy consent patch result.",
                    }
                ),
            };
        }
    }
}
