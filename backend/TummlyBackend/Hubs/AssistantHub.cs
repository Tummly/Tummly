using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace TummlyBackend.Hubs
{
    [Authorize]
    public class AssistantHub : Hub
    {
    }
}
