namespace TummlyBackend.Models
{
    /// <summary>
    /// Delivery channel for a Feedback guest response (Email contact → email;
    /// Phone contact → SMS).
    /// </summary>
    public enum FeedbackGuestResponseChannel
    {
        Email = 0,

        Sms = 1,
    }
}
