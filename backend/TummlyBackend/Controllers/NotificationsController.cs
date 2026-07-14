using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TummlyBackend.DTOs.Notifications;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly IOperatorNotificationsService _notifications;

        public NotificationsController(
            IOperatorNotificationsService notifications
        )
        {
            _notifications = notifications;
        }

        [HttpGet]
        public async Task<IActionResult> List()
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var items = await _notifications.ListAsync(userId);
            var unreadCount =
                await _notifications.GetUnreadCountAsync(userId);

            return Ok(new
            {
                success = true,
                unreadCount,
                items
            });
        }

        [HttpPost("{id:int}/read")]
        public async Task<IActionResult> MarkOneRead(int id)
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var found = await _notifications.MarkOneReadAsync(userId, id);
            if (!found)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Notification not found."
                });
            }

            return Ok(new { success = true });
        }

        [HttpPost("read-all")]
        public async Task<IActionResult> MarkInboxRead()
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var marked = await _notifications.MarkInboxReadAsync(userId);
            return Ok(new { success = true, marked });
        }

        [HttpPost("read-visible")]
        public async Task<IActionResult> MarkVisibleRead(
            [FromBody] NotificationListFilter filter
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var marked = await _notifications.MarkVisibleReadAsync(
                userId,
                filter ?? new NotificationListFilter()
            );

            return Ok(new { success = true, marked });
        }

        [HttpGet("preferences")]
        public async Task<IActionResult> GetPreferences()
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var preferences =
                await _notifications.GetPreferencesAsync(userId);

            return Ok(new { success = true, preferences });
        }

        [HttpPut("preferences")]
        public async Task<IActionResult> SetPreferences(
            [FromBody] NotificationPreferencesDto preferences
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var saved = await _notifications.SetPreferencesAsync(
                userId,
                preferences
            );

            return Ok(new { success = true, preferences = saved });
        }

        [HttpPost("ensure-seeds")]
        public async Task<IActionResult> EnsureSeeds()
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var result = await _notifications.EnsureSeedsAsync(userId);
            return Ok(new { success = true, reToast = result.ReToast });
        }
    }
}
