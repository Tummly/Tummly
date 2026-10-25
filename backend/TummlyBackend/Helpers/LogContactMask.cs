using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Twilio.Exceptions;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Masks email and phone values for application logs (G3.5 / R4).
    /// </summary>
    public static class LogContactMask
    {
        private static readonly Regex EmailLike = new(
            @"[A-Z0-9._%+\-]+@[A-Z0-9.\-]+\.[A-Z]{2,}",
            RegexOptions.IgnoreCase
                | RegexOptions.CultureInvariant
                | RegexOptions.Compiled
        );

        /// <summary>
        /// Optional +, then at least 7 digits with common phone separators between them.
        /// </summary>
        private static readonly Regex PhoneLike = new(
            @"\+?\d(?:[\s\-().]*\d){6,}",
            RegexOptions.CultureInvariant | RegexOptions.Compiled
        );

        public static string Email(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return "••••";
            }

            var trimmed = email.Trim();
            var at = trimmed.IndexOf('@');
            if (at <= 0 || at >= trimmed.Length - 1)
            {
                return "••••";
            }

            return trimmed[0] + "•••@" + trimmed[(at + 1)..];
        }

        public static string Phone(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                return "••••";
            }

            var digits = new string(phone.Where(char.IsDigit).ToArray());
            if (digits.Length < 4)
            {
                return "••••";
            }

            return "••••" + digits[^4..];
        }

        public static string Scrub(string? text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            var withoutEmails = EmailLike.Replace(
                text,
                match => Email(match.Value)
            );

            return PhoneLike.Replace(
                withoutEmails,
                match =>
                {
                    var digits = new string(
                        match.Value.Where(char.IsDigit).ToArray()
                    );
                    if (digits.Length < 7)
                    {
                        return match.Value;
                    }

                    return Phone(digits);
                }
            );
        }
    }

    /// <summary>
    /// Shared log shape for email / SMS provider send failures (R4.3):
    /// exception type, optional provider code, scrubbed message — never the
    /// exception object (which may embed contact data in <c>Message</c>).
    /// </summary>
    public static class LogProviderSendFailureExtensions
    {
        public static void LogProviderSendFailure(
            this ILogger logger,
            Exception exception,
            string messageTemplate,
            params object?[] args
        )
        {
            logger.LogProviderSendFailure(
                LogLevel.Error,
                exception,
                messageTemplate,
                args
            );
        }

        public static void LogProviderSendFailure(
            this ILogger logger,
            LogLevel logLevel,
            Exception exception,
            string messageTemplate,
            params object?[] args
        )
        {
            ArgumentNullException.ThrowIfNull(logger);
            ArgumentNullException.ThrowIfNull(exception);
            ArgumentNullException.ThrowIfNull(messageTemplate);

            object? providerCode = exception is ApiException apiException
                ? apiException.Code
                : null;

            var allArgs = new object?[(args?.Length ?? 0) + 3];
            if (args is { Length: > 0 })
            {
                Array.Copy(args, allArgs, args.Length);
            }

            allArgs[^3] = exception.GetType().Name;
            allArgs[^2] = providerCode;
            allArgs[^1] = LogContactMask.Scrub(exception.Message);

            logger.Log(
                logLevel,
                messageTemplate
                    + " ExceptionType={ExceptionType} ProviderCode={ProviderCode} Message={ScrubbedMessage}",
                allArgs
            );
        }
    }
}
