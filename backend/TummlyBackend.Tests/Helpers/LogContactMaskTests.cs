using Microsoft.Extensions.Logging;
using Twilio.Exceptions;
using TummlyBackend.Helpers;

namespace TummlyBackend.Tests.Helpers
{
    public class LogContactMaskTests
    {
        [Theory]
        [InlineData("alice@example.com", "a•••@example.com")]
        [InlineData("A@b.co", "A•••@b.co")]
        [InlineData("  bob@example.org  ", "b•••@example.org")]
        public void Email_MasksLocalPartKeepingFirstCharAndDomain(
            string input,
            string expected
        )
        {
            Assert.Equal(expected, LogContactMask.Email(input));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("not-an-email")]
        [InlineData("@missing-local.com")]
        [InlineData("missing-domain@")]
        public void Email_BlankOrInvalid_ReturnsFourBullets(string? input)
        {
            Assert.Equal("••••", LogContactMask.Email(input));
        }

        [Theory]
        [InlineData("+447911123456", "••••3456")]
        [InlineData("07911123456", "••••3456")]
        [InlineData("+15005550006", "••••0006")]
        [InlineData("1234", "••••1234")]
        public void Phone_KeepsLastFourDigits(string input, string expected)
        {
            Assert.Equal(expected, LogContactMask.Phone(input));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("12")]
        [InlineData("abc")]
        public void Phone_BlankOrFewerThanFourDigits_ReturnsFourBullets(
            string? input
        )
        {
            Assert.Equal("••••", LogContactMask.Phone(input));
        }

        [Fact]
        public void Scrub_ReplacesEmailAndPhoneLikeSubstrings()
        {
            var raw =
                "Failed for alice@example.com to +447911123456 (code 21211)";

            var scrubbed = LogContactMask.Scrub(raw);

            Assert.DoesNotContain("alice@example.com", scrubbed);
            Assert.DoesNotContain("+447911123456", scrubbed);
            Assert.DoesNotContain("447911123456", scrubbed);
            Assert.Contains("a•••@example.com", scrubbed);
            Assert.Contains("••••3456", scrubbed);
            Assert.Contains("21211", scrubbed);
        }

        [Theory]
        [InlineData(null, "")]
        [InlineData("", "")]
        public void Scrub_NullOrEmpty_ReturnsEmpty(string? input, string expected)
        {
            Assert.Equal(expected, LogContactMask.Scrub(input));
        }

        [Fact]
        public void LogProviderSendFailure_OmitsExceptionObject_AndScrubsMessage()
        {
            var logger = new CapturingLogger();
            var ex = new InvalidOperationException(
                "Resend rejected alice@example.com +447911123456"
            );

            logger.LogProviderSendFailure(
                ex,
                "Provider send failed for {GuestResponseId}",
                42
            );

            Assert.Single(logger.Entries);
            var entry = logger.Entries[0];
            Assert.Equal(LogLevel.Error, entry.Level);
            Assert.Null(entry.Exception);
            Assert.DoesNotContain("alice@example.com", entry.Message);
            Assert.DoesNotContain("+447911123456", entry.Message);
            Assert.DoesNotContain("447911123456", entry.Message);
            Assert.Contains("a•••@example.com", entry.Message);
            Assert.Contains("••••3456", entry.Message);
            Assert.Contains("InvalidOperationException", entry.Message);
            Assert.Contains("42", entry.Message);
        }

        [Fact]
        public void LogProviderSendFailure_IncludesTwilioProviderCode()
        {
            var logger = new CapturingLogger();
            var ex = new ApiException(
                21211,
                400,
                "Unable to create record for +447911123456",
                moreInfo: null,
                details: null,
                exception: null
            );

            logger.LogProviderSendFailure(
                LogLevel.Warning,
                ex,
                "Twilio Verify check failed for {MaskedPhone}",
                LogContactMask.Phone("+447911123456")
            );

            Assert.Single(logger.Entries);
            var entry = logger.Entries[0];
            Assert.Equal(LogLevel.Warning, entry.Level);
            Assert.Null(entry.Exception);
            Assert.Contains("21211", entry.Message);
            Assert.Contains("ApiException", entry.Message);
            Assert.DoesNotContain("+447911123456", entry.Message);
            Assert.Contains("••••3456", entry.Message);
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
    }
}
