using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    public static class AssistantCompareAll
    {
        public static readonly TimeSpan RetrieveBudget = TimeSpan.FromSeconds(20);

        public const int ExcerptSampleCap = 5;

        public const string PartialRankingSentence = "This ranking is partial.";

        public static string EmptyDomainSentence(
            string domain,
            string locationName,
            string periodPhrase
        )
            => $"No {domain} at {locationName} for {periodPhrase}.";

        public static string FailedLoadSentence(string locationName)
            => $"Could not load data for {locationName}.";

        public static string NotStartedSentence(IReadOnlyList<string> names)
            => $"Not retrieved this turn: {string.Join(", ", names)}. "
                + "Retry this send, or name up to 3 locations.";

        public static string CommentSampleSentence(
            int sample,
            int total,
            string locationName
        )
            => $"Comment samples are {sample} of {total} at {locationName}.";

        public static AssistantRetrievedEvidence Thin(AssistantRetrievedEvidence evidence)
        {
            var rows = evidence.Feedback.Rows
                .OrderByDescending(row => row.CreatedAt)
                .Take(ExcerptSampleCap)
                .ToList();
            return evidence with
            {
                Feedback = evidence.Feedback with
                {
                    Rows = rows,
                    SampleCount = rows.Count,
                    GuestRows = [],
                    Placeholder4GuestRows = [],
                },
                Offers = evidence.Offers with
                {
                    CatalogSampleCount = 0,
                    Catalog = [],
                    PerOfferMetrics = [],
                    LinkedCampaigns = [],
                    ClaimLogs = [],
                    RedemptionLogs = [],
                },
                Campaigns = evidence.Campaigns with
                {
                    ListSampleCount = 0,
                    Rows = [],
                    Eligibility = [],
                    Details = [],
                },
                Capture = evidence.Capture with { QrRows = [] },
                Guests = evidence.Guests with { Rows = [], SampleCount = 0 },
            };
        }
    }
}
