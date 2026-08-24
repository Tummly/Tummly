using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TummlyBackend.DTOs.AccountWorkspace;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [Route("api/account-workspace")]
    [Authorize]
    public class AccountWorkspaceController : ControllerBase
    {
        private readonly IAccountWorkspaceService _accountWorkspace;
        private readonly IGuestDataExportService _guestDataExport;

        public AccountWorkspaceController(
            IAccountWorkspaceService accountWorkspace,
            IGuestDataExportService guestDataExport
        )
        {
            _accountWorkspace = accountWorkspace;
            _guestDataExport = guestDataExport;
        }

        [HttpGet]
        public async Task<IActionResult> GetDetails()
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var details = await _accountWorkspace.GetDetailsAsync(userId);

            if (details == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Restaurant not found.",
                });
            }

            return Ok(details);
        }

        [HttpPut("account-details")]
        [RequestSizeLimit(BrandLogoRules.MaxFileBytes + 64 * 1024)]
        public async Task<IActionResult> UpdateAccountDetails(
            [FromForm] string? name,
            [FromForm] IFormFile? logo
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var (details, error, statusCode) =
                await _accountWorkspace.UpdateAccountDetailsAsync(
                    userId,
                    name,
                    logo
                );

            if (error != null)
            {
                return StatusCode(
                    statusCode,
                    new
                    {
                        success = false,
                        message = error,
                    }
                );
            }

            return Ok(details);
        }

        [HttpPut("business-details")]
        public async Task<IActionResult> UpdateBusinessDetails(
            [FromBody] UpdateBusinessDetailsRequest request
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var (details, error, statusCode) =
                await _accountWorkspace.UpdateBusinessDetailsAsync(
                    userId,
                    request
                );

            if (error != null)
            {
                return StatusCode(
                    statusCode,
                    new
                    {
                        success = false,
                        message = error,
                    }
                );
            }

            return Ok(details);
        }

        [HttpPut("key-contacts")]
        public async Task<IActionResult> UpdateKeyContacts(
            [FromBody] UpdateKeyContactsRequest request
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var (details, error, statusCode) =
                await _accountWorkspace.UpdateKeyContactsAsync(
                    userId,
                    request
                );

            if (error != null)
            {
                return StatusCode(
                    statusCode,
                    new
                    {
                        success = false,
                        message = error,
                    }
                );
            }

            return Ok(details);
        }

        [HttpPut("workspace-defaults")]
        public async Task<IActionResult> UpdateWorkspaceDefaults(
            [FromBody] UpdateWorkspaceDefaultsRequest request
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var (details, error, statusCode) =
                await _accountWorkspace.UpdateWorkspaceDefaultsAsync(
                    userId,
                    request
                );

            if (error != null)
            {
                return StatusCode(
                    statusCode,
                    new
                    {
                        success = false,
                        message = error,
                    }
                );
            }

            return Ok(details);
        }

        [HttpPost("pause")]
        public async Task<IActionResult> PauseWorkspace()
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var (details, error, statusCode) =
                await _accountWorkspace.PauseWorkspaceAsync(userId);

            if (error != null)
            {
                return StatusCode(
                    statusCode,
                    new
                    {
                        success = false,
                        message = error,
                    }
                );
            }

            return Ok(details);
        }

        [HttpPost("resume")]
        public async Task<IActionResult> ResumeWorkspace()
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var (details, error, statusCode) =
                await _accountWorkspace.ResumeWorkspaceAsync(userId);

            if (error != null)
            {
                return StatusCode(
                    statusCode,
                    new
                    {
                        success = false,
                        message = error,
                    }
                );
            }

            return Ok(details);
        }

        [HttpGet("guest-data-export")]
        public async Task<IActionResult> ExportGuestData(
            [FromQuery] string format = "xlsx"
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var (result, error, statusCode) =
                await _guestDataExport.ExportAsync(userId, format);

            if (error != null)
            {
                return StatusCode(
                    statusCode,
                    new
                    {
                        success = false,
                        message = error,
                    }
                );
            }

            return File(result!.Content, result.ContentType, result.FileName);
        }

        [HttpGet("brand-logo")]
        public async Task<IActionResult> GetBrandLogo()
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var result = await _accountWorkspace.OpenBrandLogoAsync(userId);

            if (result == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Brand logo not found.",
                });
            }

            return File(result.Value.Stream, result.Value.ContentType);
        }
    }
}
