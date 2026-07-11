using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TummlyBackend.DTOs.HelpCentre;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [Route("api/support")]
    [Authorize(Roles = "Support")]
    public class SupportController : ControllerBase
    {
        private readonly ISupportService _supportService;

        public SupportController(ISupportService supportService)
        {
            _supportService = supportService;
        }

        [HttpGet("queries")]
        public async Task<IActionResult> ListQueries(
            [FromQuery] string? status,
            [FromQuery] string? topic,
            [FromQuery] string? q,
            [FromQuery] string? type,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20
        )
        {
            try
            {
                var result = await _supportService.ListQueriesAsync(
                    status,
                    topic,
                    q,
                    type,
                    page,
                    pageSize
                );

                return Ok(new
                {
                    success = true,
                    data = result,
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

        [HttpGet("queries/{id:int}")]
        public async Task<IActionResult> GetQuery(int id)
        {
            var result = await _supportService.GetQueryAsync(id);

            if (result == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Query not found.",
                });
            }

            return Ok(new
            {
                success = true,
                data = result,
            });
        }

        [HttpPost("queries/{id:int}/replies")]
        public async Task<IActionResult> AddReply(
            int id,
            [FromBody] SupportReplyDto dto
        )
        {
            var staffId = GetStaffId();

            if (staffId == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid token.",
                });
            }

            try
            {
                var result = await _supportService.AddSupportReplyAsync(
                    staffId.Value,
                    id,
                    dto
                );

                return Ok(new
                {
                    success = true,
                    data = result,
                });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Query not found.",
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message,
                });
            }
        }

        [HttpPatch("queries/{id:int}/status")]
        public async Task<IActionResult> UpdateStatus(
            int id,
            [FromBody] UpdateQueryStatusDto dto
        )
        {
            var staffId = GetStaffId();

            if (staffId == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid token.",
                });
            }

            try
            {
                var result = await _supportService.UpdateStatusAsync(
                    staffId.Value,
                    id,
                    dto
                );

                return Ok(new
                {
                    success = true,
                    data = result,
                });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Query not found.",
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

        [HttpGet("queries/{id:int}/attachments/{attachmentId:int}")]
        public async Task<IActionResult> DownloadQueryAttachment(
            int id,
            int attachmentId
        )
        {
            try
            {
                var result = await _supportService.GetQueryAttachmentAsync(
                    id,
                    attachmentId
                );

                if (result == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Attachment not found.",
                    });
                }

                return File(
                    result.Value.Stream,
                    result.Value.ContentType,
                    result.Value.FileName
                );
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    new
                    {
                        success = false,
                        message = ex.Message,
                    }
                );
            }
        }

        private int? GetStaffId()
        {
            var staffIdClaim =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (
                string.IsNullOrEmpty(staffIdClaim)
                || !int.TryParse(staffIdClaim, out var staffId)
            )
            {
                return null;
            }

            return staffId;
        }
    }
}
