using TummlyBackend.DTOs.Assistant;
using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    public static class AssistantLiveAnswerCopy
    {
        public const string MutateRefusalBody =
            "I cannot create, send, or change records. Ask about Feedback in this Analysis scope.";

        public const string HelpCentreRefusalBody =
            "I cannot answer Help Centre or product how-to questions. Ask about Feedback in this Analysis scope.";

        public const string MixedRefuseSentence =
            "I cannot create, send, or change records.";

        public static AssistantLiveAnswerResult.Succeeded EmptyGrounded(
            string ownedLocationName,
            string periodPhrase
        )
            => new(
                AssistantMessageClass.Grounded,
                $"No feedback at {ownedLocationName} for {periodPhrase}",
                $"There is nothing to summarise or list at {ownedLocationName} over {periodPhrase}.",
                []
            );

        public static AssistantLiveAnswerResult.Succeeded Refusal(AssistantAskKind kind)
            => new(
                AssistantMessageClass.Refusal,
                null,
                kind == AssistantAskKind.HelpCentre
                    ? HelpCentreRefusalBody
                    : MutateRefusalBody,
                []
            );

        public static AssistantLiveAnswerResult.Succeeded GroundedFromEvidence(
            string userMessage,
            string ownedLocationName,
            string periodPhrase,
            AssistantFeedbackEvidence evidence
        )
        {
            if (evidence.IsEmpty)
            {
                return EmptyGrounded(ownedLocationName, periodPhrase);
            }

            var ask = AssistantAskIntent.Classify(userMessage);
            var title = TitleFromEvidence(ownedLocationName, periodPhrase, evidence);
            var body = BodyFromEvidence(ownedLocationName, periodPhrase, evidence);
            if (ask == AssistantAskKind.Mixed)
            {
                body = $"{body} {MixedRefuseSentence}";
            }

            var actions = AssistantActionCatalog.DefaultFeedbackActions(
                userMessage,
                evidence
            );

            return new AssistantLiveAnswerResult.Succeeded(
                AssistantMessageClass.Grounded,
                title,
                body,
                actions
            );
        }

        public static AssistantLiveAnswerResult.Succeeded CompareFromEvidence(
            string userMessage,
            string periodPhrase,
            IReadOnlyList<AssistantCompareLocationEvidence> compareLocations,
            AssistantFeedbackEvidence savedScopeEvidence,
            string? droppedUnknownSentence
        )
        {
            var names = compareLocations.Select(row => row.LocationName).ToList();
            var title = $"Compare {JoinNames(names)}";
            var parts = new List<string>
            {
                $"Compare over {periodPhrase}:",
            };

            foreach (var row in compareLocations)
            {
                var status = row.CaptureStatus == CaptureLocationStatus.Paused
                    ? $" {AssistantCompareTurn.CapturePausedSentence(row.LocationName)}"
                    : "";
                if (row.Evidence.IsEmpty)
                {
                    parts.Add(
                        $"{row.LocationName} has no feedback over {periodPhrase}.{status}"
                    );
                    continue;
                }

                parts.Add(
                    $"{row.LocationName} received {row.Evidence.TotalCount} feedback item{(row.Evidence.TotalCount == 1 ? "" : "s")}.{status}"
                );
            }

            if (!string.IsNullOrWhiteSpace(droppedUnknownSentence))
            {
                parts.Add(droppedUnknownSentence);
            }

            var actions = AssistantActionCatalog.DefaultFeedbackActions(
                userMessage,
                savedScopeEvidence
            );

            return new AssistantLiveAnswerResult.Succeeded(
                AssistantMessageClass.Grounded,
                title,
                string.Join(" ", parts),
                actions
            );
        }

        public static AssistantLiveAnswerResult.Succeeded WithSentences(
            AssistantLiveAnswerResult.Succeeded answer,
            params string?[] sentences
        )
        {
            var extra = string.Join(
                " ",
                sentences.Where(sentence => !string.IsNullOrWhiteSpace(sentence))
            );
            if (extra.Length == 0)
            {
                return answer;
            }

            return answer with { Body = $"{answer.Body} {extra}" };
        }

        public static AssistantLiveAnswerResult.Succeeded Clarify(string body)
            => new(AssistantMessageClass.Clarify, null, body, []);

        private static string JoinNames(IReadOnlyList<string> names)
        {
            if (names.Count <= 1)
            {
                return names.FirstOrDefault() ?? "";
            }

            if (names.Count == 2)
            {
                return $"{names[0]} and {names[1]}";
            }

            return $"{string.Join(", ", names.Take(names.Count - 1))}, and {names[^1]}";
        }

        private static string TitleFromEvidence(
            string ownedLocationName,
            string periodPhrase,
            AssistantFeedbackEvidence evidence
        )
        {
            if (evidence.NeedsAttention > 0)
            {
                return $"Feedback that needs attention at {ownedLocationName}";
            }

            var topTag = evidence.TagCounts.FirstOrDefault()?.Tag;
            if (topTag is not null)
            {
                return $"{topTag} is the main theme over {periodPhrase}";
            }

            return $"Feedback at {ownedLocationName} over {periodPhrase}";
        }

        private static string BodyFromEvidence(
            string ownedLocationName,
            string periodPhrase,
            AssistantFeedbackEvidence evidence
        )
        {
            var parts = new List<string>
            {
                $"{ownedLocationName} received {evidence.TotalCount} feedback item{(evidence.TotalCount == 1 ? "" : "s")} over {periodPhrase}.",
            };

            if (evidence.SucceededNegative + evidence.SucceededNeutral + evidence.SucceededPositive
                > 0)
            {
                parts.Add(
                    $"Succeeded classification: {evidence.SucceededNegative} negative, {evidence.SucceededNeutral} neutral, {evidence.SucceededPositive} positive."
                );
            }

            if (evidence.NeedsAttention > 0)
            {
                parts.Add($"{evidence.NeedsAttention} item{(evidence.NeedsAttention == 1 ? "" : "s")} need attention.");
            }

            if (evidence.TagCounts.Count > 0)
            {
                var themes = string.Join(
                    ", ",
                    evidence.TagCounts.Take(3).Select(tag => $"{tag.Tag} ({tag.Count})")
                );
                parts.Add($"Top themes: {themes}.");
            }

            if (evidence.DisclosesSample)
            {
                parts.Add(
                    $"These themes come from {evidence.SampleCount} of {evidence.TotalCount} feedback items."
                );
            }

            return string.Join(" ", parts);
        }
    }
}
