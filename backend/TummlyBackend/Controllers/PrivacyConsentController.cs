using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TummlyBackend.DTOs.PrivacyConsent;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;

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

        public PrivacyConsentController(
            IRestaurantPermissionHelper permissions,
            IPrivacyConsentSaveService save,
            IPrivacyConsentService privacyConsent
        )
        {
            _permissions = permissions;
            _save = save;
            _privacyConsent = privacyConsent;
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

            var result = await _privacyConsent.GetAsync(decision.RestaurantId);

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
