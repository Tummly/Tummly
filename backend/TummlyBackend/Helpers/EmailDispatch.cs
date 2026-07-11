using Microsoft.Extensions.Logging;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Soft-dispatch helper for side-effect notification emails.
    /// Primary actions stay saved; callers surface <c>emailDispatched: false</c> to the client.
    /// </summary>
    public static class EmailDispatch
    {
        public const string DefaultWarning =
            "Saved, but the notification email could not be sent.";

        public static async Task<bool> TrySendAsync(
            Func<Task> send,
            ILogger logger,
            string failureMessage,
            params object?[] args
        )
        {
            try
            {
                await send();
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, failureMessage, args);
                return false;
            }
        }

        public static string? WarningOrNull(bool emailDispatched) =>
            emailDispatched ? null : DefaultWarning;
    }
}
