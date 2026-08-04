namespace TummlyBackend.DTOs.Feedback
{
    public class CorrectFeedbackClassificationDto
    {
        public string Sentiment { get; set; } = string.Empty;

        public string Reason { get; set; } = string.Empty;

        public string? Note { get; set; }
    }
}
