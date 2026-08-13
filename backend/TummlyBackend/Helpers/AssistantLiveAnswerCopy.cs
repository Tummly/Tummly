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

        public const int NamedRowCap = 5;

        public const int SummariseExcerptCap = 3;

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
            var grounded = AssistantAskIntent.ClassifyGrounded(userMessage);
            var title = TitleFromEvidence(
                grounded,
                ownedLocationName,
                periodPhrase,
                evidence
            );
            var body = BodyFromEvidence(
                grounded,
                ownedLocationName,
                periodPhrase,
                evidence
            );
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
            AssistantGroundedAsk grounded,
            string ownedLocationName,
            string periodPhrase,
            AssistantFeedbackEvidence evidence
        )
        {
            return grounded switch
            {
                AssistantGroundedAsk.ListGuests =>
                    $"Location Guests at {ownedLocationName}",
                AssistantGroundedAsk.Placeholder4 =>
                    $"Marketing eligible guests with negative Feedback at {ownedLocationName}",
                AssistantGroundedAsk.ListFeedback =>
                    $"Feedback at {ownedLocationName} over {periodPhrase}",
                _ when evidence.NeedsAttention > 0 =>
                    $"Feedback that needs attention at {ownedLocationName}",
                _ => evidence.TagCounts.FirstOrDefault()?.Tag is string topTag
                    ? $"{topTag} is the main theme over {periodPhrase}"
                    : $"Feedback at {ownedLocationName} over {periodPhrase}",
            };
        }

        private static string BodyFromEvidence(
            AssistantGroundedAsk grounded,
            string ownedLocationName,
            string periodPhrase,
            AssistantFeedbackEvidence evidence
        )
        {
            return grounded switch
            {
                AssistantGroundedAsk.ListFeedback => ListFeedbackBody(
                    ownedLocationName,
                    periodPhrase,
                    evidence
                ),
                AssistantGroundedAsk.ListGuests => ListGuestsBody(
                    ownedLocationName,
                    periodPhrase,
                    evidence
                ),
                AssistantGroundedAsk.Placeholder4 => Placeholder4Body(
                    ownedLocationName,
                    periodPhrase,
                    evidence
                ),
                _ => SummariseBody(ownedLocationName, periodPhrase, evidence),
            };
        }

        private static string SummariseBody(
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

            var excerpts = evidence.Rows
                .Select(row => row.Excerpt)
                .Where(excerpt => excerpt.Length > 0)
                .Take(SummariseExcerptCap)
                .Select(excerpt => $"\"{excerpt}\"")
                .ToList();
            if (excerpts.Count > 0)
            {
                parts.Add($"Excerpts: {string.Join("; ", excerpts)}.");
            }

            if (evidence.DisclosesSample)
            {
                parts.Add(
                    $"These themes come from {evidence.SampleCount} of {evidence.TotalCount} feedback items."
                );
            }

            return string.Join(" ", parts);
        }

        private static string ListFeedbackBody(
            string ownedLocationName,
            string periodPhrase,
            AssistantFeedbackEvidence evidence
        )
        {
            var parts = new List<string>
            {
                $"{ownedLocationName} has {evidence.TotalCount} feedback item{(evidence.TotalCount == 1 ? "" : "s")} over {periodPhrase}.",
            };

            var named = evidence.Rows.Take(NamedRowCap).Select(FormatFeedbackRow).ToList();
            if (named.Count > 0)
            {
                parts.Add(string.Join(" ", named));
            }

            var remaining = evidence.TotalCount - Math.Min(NamedRowCap, evidence.Rows.Count);
            if (evidence.TotalCount > NamedRowCap && remaining > 0)
            {
                parts.Add($"and {remaining} more.");
            }

            return string.Join(" ", parts);
        }

        private static string ListGuestsBody(
            string ownedLocationName,
            string periodPhrase,
            AssistantFeedbackEvidence evidence
        )
        {
            if (evidence.GuestRows.Count == 0)
            {
                return $"No Location Guests to list at {ownedLocationName} over {periodPhrase}. Unlinked Feedback is not a Location Guest.";
            }

            var named = evidence.GuestRows
                .Take(NamedRowCap)
                .Select(FormatGuestRow)
                .ToList();
            var remaining = evidence.GuestRows.Count - named.Count;
            var more = remaining > 0 ? $" and {remaining} more" : string.Empty;
            return $"Location Guests at {ownedLocationName} over {periodPhrase}: {string.Join("; ", named)}{more}.";
        }

        private static string Placeholder4Body(
            string ownedLocationName,
            string periodPhrase,
            AssistantFeedbackEvidence evidence
        )
        {
            var guests = evidence.Placeholder4GuestRows;
            if (guests.Count == 0)
            {
                return $"No Location Guests currently Marketing eligible also left Succeeded Negative Feedback at {ownedLocationName} over {periodPhrase}. Marketing eligible is the current state, not a fact inside the Reporting period.";
            }

            var named = guests.Take(NamedRowCap).Select(FormatGuestRow).ToList();
            var remaining = guests.Count - named.Count;
            var more = remaining > 0 ? $" and {remaining} more" : string.Empty;
            return $"{guests.Count} Location Guest{(guests.Count == 1 ? "" : "s")} currently Marketing eligible also left Succeeded Negative Feedback at {ownedLocationName} over {periodPhrase}: {string.Join("; ", named)}{more}. Marketing eligible is the current state, not a fact inside the Reporting period.";
        }

        private static string FormatFeedbackRow(AssistantFeedbackEvidenceRow row)
        {
            var bits = new List<string> { row.GuestName };
            if (row.Sentiment is not null)
            {
                bits.Add(row.Sentiment);
            }
            else
            {
                bits.Add(row.ClassificationStatus);
            }

            if (row.MarketingStatus is not null)
            {
                bits.Add(row.MarketingStatus);
            }

            if (row.GuestTags.Count > 0)
            {
                bits.Add(string.Join(", ", row.GuestTags));
            }

            if (row.DetectedTags.Count > 0)
            {
                bits.Add(string.Join(", ", row.DetectedTags));
            }

            if (row.Excerpt.Length > 0)
            {
                bits.Add($"\"{row.Excerpt}\"");
            }

            bits.Add(row.CreatedAt.ToString("u"));
            bits.Add(row.WorkflowStatus);
            if (row.NeedsAttention)
            {
                bits.Add("Needs attention");
            }

            if (row.QrSource is not null)
            {
                bits.Add(row.QrSource);
            }

            bits.Add(row.ContactType);
            bits.Add(row.FeedbackReference);
            return string.Join(" — ", bits) + ".";
        }

        private static string FormatGuestRow(AssistantGuestEvidenceRow guest)
        {
            var bits = new List<string>
            {
                guest.Name,
                guest.MarketingStatus,
            };
            if (guest.GuestTags.Count > 0)
            {
                bits.Add(string.Join(", ", guest.GuestTags));
            }

            return string.Join(" — ", bits);
        }
    }
}
