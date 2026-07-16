using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace TummlyBackend.Hubs
{
    /// <summary>
    /// Operator Home Feedback realtime hub — thin classification-terminal signals only.
    /// Does not carry Notifications inbox/toast events.
    /// </summary>
    [Authorize]
    public class FeedbackHomeHub : Hub
    {
    }
}
