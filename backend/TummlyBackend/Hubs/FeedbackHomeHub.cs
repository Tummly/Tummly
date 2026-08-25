using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Hubs
{
    /// <summary>
    /// Operator Home Feedback realtime hub — thin classification-terminal signals only.
    /// Does not carry Notifications inbox/toast events.
    /// </summary>
    [Authorize]
    public class FeedbackHomeHub : Hub
    {
        private readonly IRestaurantPermissionHelper _permissions;

        public FeedbackHomeHub(IRestaurantPermissionHelper permissions)
        {
            _permissions = permissions;
        }

        public override async Task OnConnectedAsync()
        {
            var decision = await _permissions.AuthorizeAsync(
                Context.User,
                OperatorAreaIds.Feedback,
                PermissionLevel.View
            );
            if (decision.Status != RestaurantPermissionStatus.Allowed)
            {
                Context.Abort();
                return;
            }

            await base.OnConnectedAsync();
        }
    }
}
