using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Hubs
{
    [Authorize]
    public class AssistantHub : Hub
    {
        private readonly IRestaurantPermissionHelper _permissions;

        public AssistantHub(IRestaurantPermissionHelper permissions)
        {
            _permissions = permissions;
        }

        public override async Task OnConnectedAsync()
        {
            var decision = await _permissions.AuthorizeAsync(
                Context.User,
                OperatorAreaIds.AiAssistant,
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
