using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    public static class AssistantLiveAnswerCopy
    {
        public const string MutateRefusalBody =
            "I cannot create, send, or change records. Ask about Feedback, offers, Campaigns, Capture, or Performance overview in this Analysis scope.";

        public const string HelpCentreRefusalBody =
            "I cannot answer Help Centre, Capture overview, Campaign templates, or Latest activity questions. Ask about Feedback, offers, Campaigns, Capture, or Performance overview in this Analysis scope.";

        public const string MixedRefuseSentence =
            "I cannot create, send, or change records.";

        public const string MixedOutOfAllowListSentence =
            "I cannot answer Capture overview, Campaign templates, Latest activity, or Help Centre questions.";

        public const int NamedRowCap = 5;

        public const int SummariseExcerptCap = 3;

        public static AssistantLiveAnswerResult.Succeeded EmptyGrounded(
            string ownedLocationName,
            string periodPhrase
        )
            => new(
                AssistantMessageClass.Grounded,
                $"No facts at {ownedLocationName} for {periodPhrase}",
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
            AssistantRetrievedEvidence evidence
        )
        {
            var grounded = AssistantAskIntent.ClassifyGrounded(userMessage);
            if (evidence.IsEmpty && grounded != AssistantGroundedAsk.ListGuests)
            {
                return EmptyGrounded(ownedLocationName, periodPhrase);
            }

            var ask = AssistantAskIntent.Classify(userMessage);
            var title = TitleFromEvidence(
                grounded,
                ownedLocationName,
                periodPhrase,
                evidence
            );
            var body = BodyFromEvidence(
                userMessage,
                grounded,
                ownedLocationName,
                periodPhrase,
                evidence
            );
            if (ask == AssistantAskKind.Mixed)
            {
                var refuse = ContainsMutate(userMessage)
                    ? MixedRefuseSentence
                    : MixedOutOfAllowListSentence;
                body = $"{body} {refuse}";
            }

            var actions = AssistantActionCatalog.DefaultActions(
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
            AssistantRetrievedEvidence savedScopeEvidence,
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
                parts.Add(CompareLocationSentence(row, periodPhrase) + status);
            }

            if (!string.IsNullOrWhiteSpace(droppedUnknownSentence))
            {
                parts.Add(droppedUnknownSentence);
            }

            var actions = AssistantActionCatalog.DefaultActions(
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

        private static string CompareLocationSentence(
            AssistantCompareLocationEvidence row,
            string periodPhrase
        )
        {
            var evidence = row.Evidence;
            var bits = new List<string>();
            var feedback = evidence.Feedback;
            if (feedback.IsEmpty)
            {
                bits.Add($"{row.LocationName} has no feedback over {periodPhrase}.");
            }
            else
            {
                bits.Add(
                    $"{row.LocationName} received {feedback.TotalCount} feedback item{(feedback.TotalCount == 1 ? "" : "s")}."
                );
            }

            if (evidence.Offers.HasCatalogFacts || evidence.Offers.HasPerformanceFacts)
            {
                bits.Add(
                    $"{row.LocationName} has {evidence.Offers.CatalogTotalCount} catalog offer{(evidence.Offers.CatalogTotalCount == 1 ? "" : "s")}."
                );
                if (evidence.Offers.DisclosesSample)
                {
                    bits.Add(
                        $"Offer catalog facts come from {evidence.Offers.CatalogSampleCount} of {evidence.Offers.CatalogTotalCount}."
                    );
                }
            }

            if (evidence.Campaigns.HasCampaignFacts)
            {
                bits.Add(
                    $"{row.LocationName} has {evidence.Campaigns.ListTotalCount} Campaign{(evidence.Campaigns.ListTotalCount == 1 ? "" : "s")}."
                );
                if (evidence.Campaigns.DisclosesSample)
                {
                    bits.Add(
                        $"Campaign facts come from {evidence.Campaigns.ListSampleCount} of {evidence.Campaigns.ListTotalCount}."
                    );
                }
            }

            if (evidence.Capture.HasSnapshotFacts)
            {
                bits.Add(
                    $"{row.LocationName} Capture: {evidence.Capture.QrScans} QR scans."
                );
            }

            if (!evidence.Home.IsEmpty)
            {
                bits.Add(
                    $"{row.LocationName} Performance overview: {evidence.Home.FeedbackSubmitted} feedbackSubmitted, {evidence.Home.GuestsJoined} guestsJoined, {evidence.Home.QrScans} qrScans."
                );
            }

            if (!evidence.Guests.IsEmpty)
            {
                bits.Add(
                    $"{row.LocationName} has {evidence.Guests.TotalCount} Location Guest{(evidence.Guests.TotalCount == 1 ? "" : "s")} (current state)."
                );
                if (evidence.Guests.DisclosesSample)
                {
                    bits.Add(
                        $"Location Guest names come from {evidence.Guests.SampleCount} of {evidence.Guests.TotalCount}."
                    );
                }
            }

            return string.Join(" ", bits);
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

        private static bool ContainsMutate(string userMessage)
        {
            var lower = userMessage.ToLowerInvariant();
            return lower.Contains("create a campaign", StringComparison.Ordinal)
                || lower.Contains("create an offer", StringComparison.Ordinal)
                || lower.Contains("send an email", StringComparison.Ordinal)
                || lower.Contains("send a message", StringComparison.Ordinal)
                || lower.Contains("change the record", StringComparison.Ordinal)
                || lower.Contains("delete the", StringComparison.Ordinal)
                || lower.Contains("mark this resolved", StringComparison.Ordinal)
                || lower.Contains("mark as resolved", StringComparison.Ordinal);
        }

        private static string TitleFromEvidence(
            AssistantGroundedAsk grounded,
            string ownedLocationName,
            string periodPhrase,
            AssistantRetrievedEvidence evidence
        )
        {
            var feedback = evidence.Feedback;
            if (grounded == AssistantGroundedAsk.ListGuests)
            {
                return $"Location Guests at {ownedLocationName}";
            }

            if (grounded == AssistantGroundedAsk.Placeholder4)
            {
                return $"Marketing eligible guests with negative Feedback at {ownedLocationName}";
            }

            if (grounded == AssistantGroundedAsk.ListFeedback)
            {
                return $"Feedback at {ownedLocationName} over {periodPhrase}";
            }

            if (!feedback.IsEmpty && feedback.NeedsAttention > 0)
            {
                return $"Feedback that needs attention at {ownedLocationName}";
            }

            if (!feedback.IsEmpty)
            {
                return feedback.TagCounts.FirstOrDefault()?.Tag is string topTag
                    ? $"{topTag} is the main theme over {periodPhrase}"
                    : $"Feedback at {ownedLocationName} over {periodPhrase}";
            }

            if (evidence.Offers.HasCatalogFacts || evidence.Offers.HasPerformanceFacts)
            {
                return $"Offers at {ownedLocationName}";
            }

            if (evidence.Campaigns.HasCampaignFacts)
            {
                return $"Campaigns at {ownedLocationName}";
            }

            if (evidence.Capture.HasSnapshotFacts)
            {
                return $"Capture at {ownedLocationName} over {periodPhrase}";
            }

            return $"Performance overview at {ownedLocationName} over {periodPhrase}";
        }

        private static string BodyFromEvidence(
            string userMessage,
            AssistantGroundedAsk grounded,
            string ownedLocationName,
            string periodPhrase,
            AssistantRetrievedEvidence evidence
        )
        {
            if (grounded == AssistantGroundedAsk.ListGuests)
            {
                return ListGuestsBody(ownedLocationName, evidence.Guests);
            }

            var parts = new List<string>();
            var feedback = evidence.Feedback;

            if (grounded == AssistantGroundedAsk.Placeholder4)
            {
                parts.Add(
                    Placeholder4Body(ownedLocationName, periodPhrase, feedback)
                );
            }
            else if (!feedback.IsEmpty)
            {
                parts.Add(
                    FeedbackBodyFromAsk(
                        grounded,
                        ownedLocationName,
                        periodPhrase,
                        feedback
                    )
                );
            }

            var lower = userMessage.ToLowerInvariant();
            if (evidence.Offers.HasCatalogFacts
                || evidence.Offers.HasPerformanceFacts
                || ContainsAny(
                    lower,
                    "offer",
                    "claim",
                    "redemption",
                    "catalog",
                    "catalogue"
                ))
            {
                parts.AddRange(OffersParts(ownedLocationName, periodPhrase, evidence.Offers));
            }

            if (evidence.Campaigns.HasCampaignFacts
                || ContainsAny(lower, "campaign", "in-flight", "in flight", "eligibility"))
            {
                parts.AddRange(
                    CampaignsParts(ownedLocationName, periodPhrase, evidence.Campaigns)
                );
            }

            if (evidence.Capture.HasSnapshotFacts
                || ContainsAny(lower, "capture", "qr scan", "qr scans"))
            {
                parts.AddRange(CaptureParts(ownedLocationName, periodPhrase, evidence.Capture));
            }

            if (!evidence.Home.IsEmpty
                || ContainsAny(lower, "performance", "guests joined", "feedback submitted"))
            {
                parts.AddRange(HomeParts(ownedLocationName, periodPhrase, evidence.Home));
            }

            if (AssistantAskIntent.LooksLikeStubCounts(userMessage))
            {
                if (evidence.Offers.HasPerformanceFacts)
                {
                    parts.Add(
                        "Claim and redemption counts come from Offers Performance, not Home or Capture stubs."
                    );
                }
                else
                {
                    parts.Add(
                        "I cannot use Home offer redemptions or Capture offerClaims stub zeros."
                    );
                }
            }

            if (parts.Count == 0)
            {
                return $"There is nothing to summarise or list at {ownedLocationName} over {periodPhrase}.";
            }

            return string.Join(" ", parts);
        }

        private static string FeedbackBodyFromAsk(
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

        private static IEnumerable<string> OffersParts(
            string ownedLocationName,
            string periodPhrase,
            AssistantOffersEvidence evidence
        )
        {
            if (evidence.HasCatalogFacts)
            {
                var titles = string.Join(
                    ", ",
                    evidence.Catalog.Take(5).Select(offer => offer.Title)
                );
                yield return $"{ownedLocationName} has {evidence.CatalogTotalCount} catalog offer{(evidence.CatalogTotalCount == 1 ? "" : "s")}: {titles}.";
            }

            if (evidence.HasPerformanceFacts)
            {
                yield return
                    $"Offers Performance over {periodPhrase}: {evidence.Claims} claims, {evidence.Redemptions} redemptions, {evidence.OffersIssued} issued.";
            }

            foreach (var metric in evidence.PerOfferMetrics.Where(row =>
                row.Claims > 0 || row.Redemptions > 0))
            {
                yield return
                    $"{metric.Title} metrics over {periodPhrase}: {metric.Claims} claims, {metric.Redemptions} redemptions.";
            }

            foreach (var linked in evidence.LinkedCampaigns.Take(5))
            {
                yield return
                    $"{linked.CampaignName} is a linked Campaign for that offer.";
            }

            foreach (var log in evidence.ClaimLogs.Take(5))
            {
                yield return
                    $"Claim {log.ClaimCode} for {log.Title} is inside the Reporting period.";
            }

            foreach (var log in evidence.RedemptionLogs.Take(5))
            {
                yield return
                    $"Redemption {log.ClaimCode} for {log.Title} is inside the Reporting period.";
            }

            if (evidence.DisclosesSample)
            {
                yield return
                    $"These catalog facts come from {evidence.CatalogSampleCount} of {evidence.CatalogTotalCount} offers.";
            }
        }

        private static IEnumerable<string> CampaignsParts(
            string ownedLocationName,
            string periodPhrase,
            AssistantCampaignsEvidence evidence
        )
        {
            if (evidence.ListTotalCount > 0)
            {
                var names = string.Join(
                    ", ",
                    evidence.Rows.Take(5).Select(row => row.Name)
                );
                yield return $"{ownedLocationName} has {evidence.ListTotalCount} Campaign{(evidence.ListTotalCount == 1 ? "" : "s")}: {names}.";
            }

            if (evidence.InFlightScheduled + evidence.InFlightSending > 0)
            {
                yield return
                    $"{evidence.InFlightScheduled} Campaigns are scheduled and {evidence.InFlightSending} are sending.";
            }

            if (evidence.MessagesSentAccepted > 0)
            {
                yield return
                    $"{evidence.MessagesSentAccepted} Campaign messages were accepted over {periodPhrase}.";
            }

            foreach (var row in evidence.Eligibility.Take(3))
            {
                if (row.Evaluable && row.CurrentlyEligible is int eligible)
                {
                    yield return
                        $"Audience {row.AudienceKey} is currently eligible for {eligible} guests.";
                }
            }

            foreach (var detail in evidence.Details.Where(row =>
                !string.IsNullOrWhiteSpace(row.MessageBody)).Take(1))
            {
                yield return $"Campaign message body: {detail.MessageBody}.";
            }

            if (evidence.DisclosesSample)
            {
                yield return
                    $"These Campaign facts come from {evidence.ListSampleCount} of {evidence.ListTotalCount} Campaigns.";
            }
        }

        private static IEnumerable<string> CaptureParts(
            string ownedLocationName,
            string periodPhrase,
            AssistantCaptureEvidence evidence
        )
        {
            if (!evidence.HasSnapshotFacts)
            {
                yield break;
            }

            yield return
                $"Capture at {ownedLocationName} over {periodPhrase}: {evidence.QrScans} QR scans, {evidence.FeedbackSubmitted} feedback submitted, {evidence.MarketingOptIns} marketing opt-ins. Previous window: {evidence.QrScansPrevious} QR scans, {evidence.FeedbackSubmittedPrevious} feedback submitted.";

            foreach (var row in evidence.QrRows.Take(5))
            {
                yield return
                    $"{row.QrType} had {row.QrScans} QR scans and {row.FeedbackSubmitted} feedback submitted over {periodPhrase}.";
            }
        }

        private static IEnumerable<string> HomeParts(
            string ownedLocationName,
            string periodPhrase,
            AssistantHomeKpiEvidence evidence
        )
        {
            if (evidence.IsEmpty)
            {
                yield break;
            }

            yield return
                $"Performance overview at {ownedLocationName} over {periodPhrase}: {evidence.FeedbackSubmitted} feedbackSubmitted, {evidence.GuestsJoined} guestsJoined, {evidence.QrScans} qrScans.";
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
            AssistantGuestsEvidence guests
        )
        {
            if (guests.Rows.Count == 0)
            {
                return $"No Location Guests to list at {ownedLocationName}. Unlinked Feedback is not a Location Guest.";
            }

            var named = guests.Rows
                .Take(NamedRowCap)
                .Select(FormatGuestRow)
                .ToList();
            var remaining = guests.Rows.Count - named.Count;
            var more = remaining > 0 ? $" and {remaining} more" : string.Empty;
            var disclose = guests.DisclosesSample
                ? $" Names come from {guests.SampleCount} of {guests.TotalCount}."
                : string.Empty;
            return $"Location Guests at {ownedLocationName} (current state, not inside the Reporting period): {string.Join("; ", named)}{more}.{disclose}";
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

        private static bool ContainsAny(string haystack, params string[] needles)
        {
            foreach (var needle in needles)
            {
                if (haystack.Contains(needle, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
