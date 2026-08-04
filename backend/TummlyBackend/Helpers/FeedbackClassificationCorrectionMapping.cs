using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    public static class FeedbackClassificationCorrectionMapping
    {
        public static string ToWireReason(
            FeedbackClassificationCorrectionReason reason
        )
            => reason switch
            {
                FeedbackClassificationCorrectionReason.MixedOrAmbiguous =>
                    "mixed_or_ambiguous",
                FeedbackClassificationCorrectionReason.ContextMisunderstood =>
                    "context_misunderstood",
                FeedbackClassificationCorrectionReason.LanguageOrTranslation =>
                    "language_or_translation",
                FeedbackClassificationCorrectionReason.IncorrectAiClassification =>
                    "incorrect_ai_classification",
                FeedbackClassificationCorrectionReason.Other => "other",
                _ => "other",
            };

        public static bool TryParseReason(
            string? wire,
            out FeedbackClassificationCorrectionReason reason
        )
        {
            switch (wire?.Trim().ToLowerInvariant())
            {
                case "mixed_or_ambiguous":
                    reason =
                        FeedbackClassificationCorrectionReason.MixedOrAmbiguous;
                    return true;
                case "context_misunderstood":
                    reason =
                        FeedbackClassificationCorrectionReason.ContextMisunderstood;
                    return true;
                case "language_or_translation":
                    reason =
                        FeedbackClassificationCorrectionReason.LanguageOrTranslation;
                    return true;
                case "incorrect_ai_classification":
                    reason =
                        FeedbackClassificationCorrectionReason.IncorrectAiClassification;
                    return true;
                case "other":
                    reason = FeedbackClassificationCorrectionReason.Other;
                    return true;
                default:
                    reason = default;
                    return false;
            }
        }
    }
}
