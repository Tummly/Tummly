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
                var allArgs = new object?[args.Length + 2];
                Array.Copy(args, allArgs, args.Length);
                allArgs[^2] = ex.GetType().Name;
                allArgs[^1] = LogContactMask.Scrub(ex.Message);
                logger.LogError(
                    failureMessage
                        + " ExceptionType={ExceptionType} Message={ScrubbedMessage}",
                    allArgs
                );
                return false;
            }
        }

        public static string? WarningOrNull(bool emailDispatched) =>
            emailDispatched ? null : DefaultWarning;
    }
}
