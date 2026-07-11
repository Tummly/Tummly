using Microsoft.Extensions.Logging.Abstractions;
using TummlyBackend.Helpers;

namespace TummlyBackend.Tests.Helpers
{
    public class EmailDispatchTests
    {
        [Fact]
        public async Task TrySendAsync_ReturnsTrue_WhenSendSucceeds()
        {
            var sent = false;

            var result = await EmailDispatch.TrySendAsync(
                () =>
                {
                    sent = true;
                    return Task.CompletedTask;
                },
                NullLogger.Instance,
                "should not log"
            );

            Assert.True(result);
            Assert.True(sent);
        }

        [Fact]
        public async Task TrySendAsync_ReturnsFalse_WhenSendThrows()
        {
            var result = await EmailDispatch.TrySendAsync(
                () => throw new InvalidOperationException("resend down"),
                NullLogger.Instance,
                "Failed to send {Label}",
                "test"
            );

            Assert.False(result);
        }

        [Fact]
        public void WarningOrNull_ReturnsWarning_OnlyWhenDispatchFailed()
        {
            Assert.Null(EmailDispatch.WarningOrNull(true));
            Assert.Equal(
                EmailDispatch.DefaultWarning,
                EmailDispatch.WarningOrNull(false)
            );
        }
    }
}
