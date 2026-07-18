using System.Text.Json;
using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    public sealed record FeedbackClassificationApiFields(
        string ClassificationStatus,
        string? Sentiment,
        IReadOnlyList<string>? DetectedTags
    );

    public static class FeedbackClassificationMapping
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = null
        };

        public static string ToWireStatus(ClassificationStatus status)
            => status.ToString();

        public static string? ToWireSentiment(FeedbackSentiment? sentiment)
            => sentiment switch
            {
                FeedbackSentiment.Positive => "positive",
                FeedbackSentiment.Neutral => "neutral",
                FeedbackSentiment.Negative => "negative",
                null => null,
                _ => null
            };

        public static bool TryParseWireSentiment(
            string? wire,
            out FeedbackSentiment sentiment
        )
        {
            switch (wire?.Trim().ToLowerInvariant())
            {
                case "positive":
                    sentiment = FeedbackSentiment.Positive;
                    return true;
                case "neutral":
                    sentiment = FeedbackSentiment.Neutral;
                    return true;
                case "negative":
                    sentiment = FeedbackSentiment.Negative;
                    return true;
                default:
                    sentiment = default;
                    return false;
            }
        }

        public static string SerializeDetectedTags(
            IReadOnlyList<DetectedTag> tags
        )
            => JsonSerializer.Serialize(
                tags.Select(tag => tag.ToString()).ToArray(),
                JsonOptions
            );

        public static IReadOnlyList<string>? DeserializeDetectedTagKeys(
            string? json
        )
        {
            if (json is null)
            {
                return null;
            }

            var keys = JsonSerializer.Deserialize<string[]>(json, JsonOptions);
            return keys ?? Array.Empty<string>();
        }

        public static FeedbackClassificationApiFields ToApiFields(
            Feedback feedback
        )
        {
            var status = feedback.ClassificationStatus;
            var succeeded = status == ClassificationStatus.Succeeded;

            return new FeedbackClassificationApiFields(
                ClassificationStatus: ToWireStatus(status),
                Sentiment: succeeded
                    ? ToWireSentiment(feedback.Sentiment)
                    : null,
                DetectedTags: succeeded
                    ? DeserializeDetectedTagKeys(feedback.DetectedTagsJson)
                        ?? Array.Empty<string>()
                    : null
            );
        }
    }
}
