namespace TummlyBackend.Models
{
    /// <summary>
    /// Operator reason for correcting AI classification sentiment.
    /// </summary>
    public enum FeedbackClassificationCorrectionReason
    {
        MixedOrAmbiguous = 0,

        ContextMisunderstood = 1,

        LanguageOrTranslation = 2,

        IncorrectAiClassification = 3,

        Other = 4,
    }
}
