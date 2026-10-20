using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Feedback;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [Route("api/feedback/bulk-recovery-batches")]
    [Authorize]
    public class BulkRecoveryBatchesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IRestaurantPermissionHelper _permissions;
        private readonly IBulkRecoveryBatchService _batches;

        public BulkRecoveryBatchesController(
            ApplicationDbContext context,
            IRestaurantPermissionHelper permissions,
            IBulkRecoveryBatchService batches
        )
        {
            _context = context;
            _permissions = permissions;
            _batches = batches;
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateBulkRecoveryBatchRequest dto,
            CancellationToken cancellationToken
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);
            if (unauthorized != null)
            {
                return unauthorized;
            }

            if (!FeedbackGuestResponseMapping.TryParseChannel(
                    dto.Channel,
                    out var channel
                ))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Channel must be email or sms.",
                });
            }

            if (!FeedbackGuestResponseMapping.TryParseIntent(
                    dto.Intent,
                    out var intent
                )
                || intent != FeedbackRecoveryIntent.RespondToGuest)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Intent must be respond_to_guest.",
                });
            }

            var owned = await GateLocationAsync(dto.LocationId);
            var denied = owned.ToHttpResult();
            if (denied != null)
            {
                return denied;
            }

            var location = await _context.RestaurantLocations
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    l => l.Id == dto.LocationId,
                    cancellationToken
                );
            if (location is null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Location not found.",
                });
            }

            try
            {
                var batch = await _batches.CreateDraftAsync(
                    location.RestaurantId,
                    dto.LocationId,
                    userId,
                    dto.FeedbackIds,
                    channel,
                    intent,
                    dto.Subject,
                    dto.BodyTemplate,
                    dto.Purpose,
                    dto.Tone,
                    dto.IncludeNotes,
                    cancellationToken
                );
                if (batch is null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Location not found.",
                    });
                }

                return Ok(new { success = true, batch });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message,
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

        [HttpGet("{batchId:int}")]
        public async Task<IActionResult> Get(
            int batchId,
            CancellationToken cancellationToken
        )
        {
            var unauthorized = OperatorAuth.TryRequireUserId(User, out _);
            if (unauthorized != null)
            {
                return unauthorized;
            }

            var batch = await _batches.GetAsync(batchId, cancellationToken);
            if (batch is null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Batch not found.",
                });
            }

            var owned = await GateLocationAsync(batch.LocationId);
            var denied = owned.ToHttpResult();
            if (denied != null)
            {
                return denied;
            }

            return Ok(new { success = true, batch });
        }

        [HttpPatch("{batchId:int}")]
        public async Task<IActionResult> UpdateDraft(
            int batchId,
            [FromBody] UpdateBulkRecoveryBatchDraftRequest dto,
            CancellationToken cancellationToken
        )
        {
            var unauthorized = OperatorAuth.TryRequireUserId(User, out _);
            if (unauthorized != null)
            {
                return unauthorized;
            }

            var existing = await _batches.GetAsync(batchId, cancellationToken);
            if (existing is null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Batch not found.",
                });
            }

            var owned = await GateLocationAsync(existing.LocationId);
            var denied = owned.ToHttpResult();
            if (denied != null)
            {
                return denied;
            }

            try
            {
                var batch = await _batches.UpdateDraftAsync(
                    batchId,
                    dto.Subject,
                    dto.BodyTemplate,
                    dto.Purpose,
                    dto.Tone,
                    dto.IncludeNotes,
                    cancellationToken
                );
                return Ok(new { success = true, batch });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message,
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

        [HttpPost("{batchId:int}/send")]
        public async Task<IActionResult> Send(
            int batchId,
            CancellationToken cancellationToken
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);
            if (unauthorized != null)
            {
                return unauthorized;
            }

            var existing = await _batches.GetAsync(batchId, cancellationToken);
            if (existing is null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Batch not found.",
                });
            }

            var owned = await GateLocationAsync(existing.LocationId);
            var denied = owned.ToHttpResult();
            if (denied != null)
            {
                return denied;
            }

            try
            {
                var result = await _batches.SendAsync(
                    batchId,
                    userId,
                    cancellationToken
                );
                if (result is null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Batch not found.",
                    });
                }

                return Ok(new { success = true, result });
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

        private async Task<RestaurantPermissionDecision> GateLocationAsync(
            int locationId
        )
        {
            var minimum = HttpMethods.IsGet(Request.Method)
                ? PermissionLevel.View
                : PermissionLevel.Manage;
            return await _permissions.AuthorizeLocationAsync(
                User,
                OperatorAreaIds.Feedback,
                minimum,
                locationId
            );
        }
    }
}
