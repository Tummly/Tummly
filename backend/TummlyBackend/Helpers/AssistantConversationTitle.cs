using System.Text.RegularExpressions;
using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Server gates for the generated Assistant conversation title.
    /// The model emits wording. This helper accepts or rejects. Over-length
    /// is cut, not rejected. Empty / Markdown / email / phone / live-answer
    /// title match keep the first-message fallback.
    /// </summary>
    public static class AssistantConversationTitle
    {
        public const int GeneratedMaxLength = 60;

        private static readonly Regex EmailPattern = new(
            @"[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}",
            RegexOptions.CultureInvariant | RegexOptions.Compiled
        );

        private static readonly Regex PhoneShapedPattern = new(
            @"(?:\+44\s?\d{10}|0\d{10})",
            RegexOptions.CultureInvariant | RegexOptions.Compiled
        );

        public static string? TryAccept(
            string? proposedConversationTitle,
            string? liveAnswerMessageTitle
        )
        {
            var firstLine = proposedConversationTitle?
                .Replace("\r\n", "\n", StringComparison.Ordinal)
                .Split('\n')[0]
                .Trim()
                ?? string.Empty;
            if (firstLine.Length == 0)
            {
                return null;
            }

            if (ContainsMarkdown(firstLine)
                || ContainsEmail(firstLine)
                || ContainsPhone(firstLine))
            {
                return null;
            }

            var liveAnswerTitle = liveAnswerMessageTitle?.Trim();
            if (liveAnswerTitle is { Length: > 0 }
                && EchoesLiveAnswerTitle(firstLine, liveAnswerTitle))
            {
                return null;
            }

            var cut = CutToMax(firstLine);
            if (liveAnswerTitle is { Length: > 0 }
                && EchoesLiveAnswerTitle(cut, liveAnswerTitle))
            {
                return null;
            }

            return cut;
        }

        public static void TryApply(
            AssistantConversation conversation,
            AssistantMessage assistantMessage,
            string? proposedConversationTitle
        )
        {
            if (assistantMessage.Class == AssistantMessageClass.Failure)
            {
                return;
            }

            var userTurns = conversation.Messages.Count(
                message => message.Role == AssistantMessageRole.User
            );
            if (userTurns != 1)
            {
                return;
            }

            var accepted = TryAccept(
                proposedConversationTitle,
                assistantMessage.Title
            );
            if (accepted is not null)
            {
                conversation.Title = accepted;
            }
        }

        /// <summary>
        /// Generated title duplicates the live-answer title, including after
        /// empty-grounded replacement shortens the displayed title.
        /// </summary>
        private static bool EchoesLiveAnswerTitle(string proposed, string liveAnswerTitle)
        {
            if (string.Equals(proposed, liveAnswerTitle, StringComparison.Ordinal))
            {
                return true;
            }

            var cutLive = CutToMax(liveAnswerTitle);
            if (string.Equals(proposed, cutLive, StringComparison.Ordinal)
                || string.Equals(CutToMax(proposed), cutLive, StringComparison.Ordinal))
            {
                return true;
            }

            return proposed.StartsWith(liveAnswerTitle, StringComparison.Ordinal)
                || liveAnswerTitle.StartsWith(proposed, StringComparison.Ordinal);
        }

        private static string CutToMax(string trimmed)
        {
            if (trimmed.Length <= GeneratedMaxLength)
            {
                return trimmed;
            }

            var prefix = trimmed[..GeneratedMaxLength];
            var lastSpace = prefix.LastIndexOf(' ');
            if (lastSpace >= 1)
            {
                return prefix[..lastSpace].TrimEnd();
            }

            return prefix;
        }

        private static bool ContainsMarkdown(string value)
        {
            if (value.Contains("**", StringComparison.Ordinal)
                || value.Contains("__", StringComparison.Ordinal)
                || value.Contains('`')
                || value.Contains("](", StringComparison.Ordinal)
                || value.Contains("![", StringComparison.Ordinal))
            {
                return true;
            }

            if (value.StartsWith('#') || value.StartsWith('*') || value.StartsWith('-'))
            {
                return true;
            }

            return false;
        }

        private static bool ContainsEmail(string value)
            => EmailPattern.IsMatch(value);

        private static bool ContainsPhone(string value)
        {
            if (PhoneShapedPattern.IsMatch(value))
            {
                return true;
            }

            return PhoneNumberHelper.TryNormalizeToE164(
                value,
                PhoneNumberHelper.DefaultRegion,
                out _
            );
        }
    }
}
