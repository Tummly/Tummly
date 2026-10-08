using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// First-turn “Explain performance” chip / ask — Home KPI Data + Interpretation
    /// when advisory Reason is not available.
    /// </summary>
    public static class AssistantExplainPerformance
    {
        public const string Interpretation =
            "These Home Performance overview facts are for the Analysis scope "
            + "Reporting period. They do not invent causes.";

        private static readonly string[] Needles =
        [
            "explain performance",
            "explain our performance",
            "explain the performance",
            "explain my performance",
        ];

        public static bool LooksLike(string userMessage)
        {
            var lower = userMessage.Trim().ToLowerInvariant();
            if (lower.Length == 0)
            {
                return false;
            }

            return Needles.Any(needle =>
                lower.Contains(needle, StringComparison.Ordinal)
            );
        }

        public static string GroundedBody(
            string ownedLocationName,
            string periodPhrase,
            AssistantHomeKpiEvidence evidence
        )
        {
            var data =
                $"- **Location:** {ownedLocationName}\n"
                + $"- **Period:** {periodPhrase}\n"
                + $"- **Feedback submitted:** {evidence.FeedbackSubmitted}\n"
                + $"- **Guests joined:** {evidence.GuestsJoined}\n"
                + $"- **QR scans:** {evidence.QrScans}";

            return $"{AssistantExplainWhyFollowUp.DataHeading}\n\n{data}\n\n"
                + $"{AssistantExplainWhyFollowUp.InterpretationHeading}\n\n"
                + Interpretation;
        }
    }
}
