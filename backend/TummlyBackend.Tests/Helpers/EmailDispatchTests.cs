using Microsoft.Extensions.Logging;
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
        public async Task TrySendAsync_FailureLog_OmitsExceptionObject_AndScrubsContact()
        {
            var logger = new CapturingLogger();

            var result = await EmailDispatch.TrySendAsync(
                () =>
                    throw new InvalidOperationException(
                        "Resend rejected alice@example.com"
                    ),
                logger,
                "Failed to send {Label}",
                "invite"
            );

            Assert.False(result);
            Assert.Single(logger.Entries);
            var entry = logger.Entries[0];
            Assert.Equal(LogLevel.Error, entry.Level);
            Assert.Null(entry.Exception);
            Assert.DoesNotContain("alice@example.com", entry.Message);
            Assert.Contains("a•••@example.com", entry.Message);
            Assert.Contains("InvalidOperationException", entry.Message);
        }

        private sealed class CapturingLogger : ILogger
        {
            public List<(
                LogLevel Level,
                Exception? Exception,
                string Message
            )> Entries { get; } = new();

            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter
            )
            {
                Entries.Add((logLevel, exception, formatter(state, exception)));
            }
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
