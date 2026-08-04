using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    public static class FeedbackCloseOutMapping
    {
        public static string ToWireIntent(FeedbackCloseOutIntent intent)
            => intent switch
            {
                FeedbackCloseOutIntent.MarkResolved => "mark_resolved",
                FeedbackCloseOutIntent.MarkNoActionNeeded =>
                    "mark_no_action_needed",
                _ => "mark_resolved",
            };

        public static bool TryParseIntent(
            string? wire,
            out FeedbackCloseOutIntent intent
        )
        {
            switch (wire?.Trim().ToLowerInvariant())
            {
                case "mark_resolved":
                    intent = FeedbackCloseOutIntent.MarkResolved;
                    return true;
                case "mark_no_action_needed":
                    intent = FeedbackCloseOutIntent.MarkNoActionNeeded;
                    return true;
                default:
                    intent = default;
                    return false;
            }
        }

        public static string ToWireReason(FeedbackCloseOutReason reason)
            => reason switch
            {
                FeedbackCloseOutReason.PositiveNoFollowUp =>
                    "positive_no_follow_up",
                FeedbackCloseOutReason.DuplicateSubmission =>
                    "duplicate_submission",
                FeedbackCloseOutReason.TestOrInvalid => "test_or_invalid",
                FeedbackCloseOutReason.AlreadyHandledOutside =>
                    "already_handled_outside",
                FeedbackCloseOutReason.NoAppropriateFollowUp =>
                    "no_appropriate_follow_up",
                FeedbackCloseOutReason.Other => "other",
                _ => "other",
            };

        public static bool TryParseReason(
            string? wire,
            out FeedbackCloseOutReason reason
        )
        {
            switch (wire?.Trim().ToLowerInvariant())
            {
                case "positive_no_follow_up":
                    reason = FeedbackCloseOutReason.PositiveNoFollowUp;
                    return true;
                case "duplicate_submission":
                    reason = FeedbackCloseOutReason.DuplicateSubmission;
                    return true;
                case "test_or_invalid":
                    reason = FeedbackCloseOutReason.TestOrInvalid;
                    return true;
                case "already_handled_outside":
                    reason = FeedbackCloseOutReason.AlreadyHandledOutside;
                    return true;
                case "no_appropriate_follow_up":
                    reason = FeedbackCloseOutReason.NoAppropriateFollowUp;
                    return true;
                case "other":
                    reason = FeedbackCloseOutReason.Other;
                    return true;
                default:
                    reason = default;
                    return false;
            }
        }
    }
}
