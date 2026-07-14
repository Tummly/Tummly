using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace TummlyBackend.Hubs
{
    /// <summary>
    /// Operator Notifications realtime hub for the Operator dashboard shell visit.
    /// Does not carry Latest activity or other Home live data.
    /// </summary>
    [Authorize]
    public class NotificationsHub : Hub
    {
    }
}
