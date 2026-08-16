using System.Text.Json;
using System.Text.RegularExpressions;
using TummlyBackend.DTOs.Assistant;
using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    public sealed class AssistantCampaignDraftState
    {
        public string Target { get; set; } = "campaign";
        public string? Name { get; set; }
        public string? GoalId { get; set; }
        public string? GoalLabel { get; set; }
        public string? TemplateId { get; set; }
        public int? TemplateVersion { get; set; }
        public string? AudienceKey { get; set; }
        public string? AudienceLabel { get; set; }
        public string? Channel { get; set; }
        public string? OfferStance { get; set; }
        public string? OfferLabel { get; set; }
        public int? OfferId { get; set; }
        public bool UsefulOptionalsSkipped { get; set; }
        public string? MessageSubject { get; set; }
        public string? MessageBody { get; set; }
    }

    public sealed record AssistantCampaignDraftTurn(
        AssistantCampaignDraftState State,
        string Title,
        string Body,
        bool IsReady
    );

    public static partial class AssistantCampaignDraftInterview
    {
        private static readonly (string Id, string Label)[] Goals =
        [
            ("thank-recent-guests", "Thank recent guests"),
            ("boost-quieter-time", "Boost a quieter time"),
            ("re-engage-inactive", "Re-engage inactive guests"),
            ("promote-something-new", "Promote something new"),
            ("follow-up-completed-recovery", "Follow up after completed recovery"),
            ("custom-campaign", "Custom campaign"),
        ];

        private static readonly (string Id, string Label)[] Audiences =
        [
            ("all-eligible-guests", "All eligible guests"),
            ("new-guests", "New guests"),
            ("positive-feedback", "Positive feedback"),
            ("offer-not-redeemed", "Offer not redeemed"),
            ("recent-redeemers", "Recent redeemers"),
            ("no-recent-tummly-activity", "No recent Tummly activity"),
            ("completed-recovery-follow-up", "Completed recovery follow-up"),
            ("dormant-guests", "Dormant guests"),
        ];

        public static bool IsCampaignDraftAsk(string message)
        {
            var lower = message.Trim().ToLowerInvariant();
            return lower.Contains("campaign", StringComparison.Ordinal)
                && ContainsAny(
                    lower,
                    "draft",
                    "create",
                    "prepare",
                    "make",
                    "build",
                    "set up",
                    "write"
                );
        }

        public static IReadOnlyList<string> DetectDraftTargets(string message)
        {
            var lower = message.Trim().ToLowerInvariant();
            var targets = new List<string>();
            if (IsCampaignDraftAsk(message))
            {
                targets.Add("Campaign");
            }
            if (AssistantOfferDraftInterview.IsOfferDraftAsk(message))
            {
                targets.Add("Offer");
            }
            if (AssistantRecoveryDraftInterview.IsRecoveryDraftAsk(message))
            {
                targets.Add("Feedback recovery");
            }

            if (targets.Count == 0
                && ContainsAny(
                    lower,
                    "help me draft something",
                    "help me create something",
                    "help me prepare something",
                    "what can you draft"
                ))
            {
                targets.AddRange(["Campaign", "Offer", "Feedback recovery"]);
            }
            else if (targets.Count == 0
                && ContainsAny(
                    lower,
                    "help me follow up",
                    "help me reach out"
                ))
            {
                targets.AddRange(["Campaign", "Feedback recovery"]);
            }

            return targets;
        }

        public static bool IsClearCancel(string message)
        {
            var lower = message.Trim().ToLowerInvariant();
            return ContainsAny(
                lower,
                "never mind the draft",
                "nevermind the draft",
                "cancel the draft",
                "cancel draft",
                "stop the draft",
                "forget the draft"
            );
        }

        public static bool LooksLikeInterviewFieldReply(string message)
        {
            var text = message.Trim();
            if (text.Length == 0)
            {
                return false;
            }

            var lower = text.ToLowerInvariant();
            if (CampaignNameRegex().IsMatch(text)
                || TemplateRegex().IsMatch(text)
                || SubjectRegex().IsMatch(text)
                || BodyRegex().IsMatch(text))
            {
                return true;
            }

            if (ContainsAny(
                    lower,
                    "email",
                    "sms",
                    "text message",
                    "no offer",
                    "without an offer",
                    "existing offer",
                    "draft it now",
                    "skip the rest"
                ))
            {
                return true;
            }

            if (Goals.Count(option =>
                    text.Contains(option.Label, StringComparison.OrdinalIgnoreCase)
                    || text.Contains(option.Id, StringComparison.OrdinalIgnoreCase)) == 1)
            {
                return true;
            }

            if (Audiences.Count(option =>
                    text.Contains(option.Label, StringComparison.OrdinalIgnoreCase)
                    || text.Contains(option.Id, StringComparison.OrdinalIgnoreCase)) == 1)
            {
                return true;
            }

            return false;
        }

        public static string InterviewAnswerPortion(string message)
        {
            var text = message.Trim();
            if (text.Length == 0)
            {
                return text;
            }

            var withoutRetrieve = RetrieveClauseRegex().Replace(text, " ");
            withoutRetrieve = Regex.Replace(
                withoutRetrieve,
                @"\s{2,}",
                " "
            );
            withoutRetrieve = Regex.Replace(
                withoutRetrieve,
                @"\s*([.,;])\s*",
                "$1 "
            );
            return withoutRetrieve.Trim().Trim(',', ';', '.', ' ');
        }

        [GeneratedRegex(
            "(?:^|[.,;]|\\band\\b|\\balso\\b)\\s*(?:please\\s+)?(?:summarise|summarize|show|list)\\b[^.!?]*(?:feedback|guests?)\\b[^.!?]*[.!?]?",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
        )]
        private static partial Regex RetrieveClauseRegex();

        public static AssistantCampaignDraftState? Parse(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                var state = JsonSerializer.Deserialize<AssistantCampaignDraftState>(json);
                return string.Equals(
                    state?.Target,
                    "campaign",
                    StringComparison.OrdinalIgnoreCase
                )
                    ? state
                    : null;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        public static string Serialize(AssistantCampaignDraftState state)
            => JsonSerializer.Serialize(state);

        public static AssistantCampaignDraftTurn Apply(
            AssistantCampaignDraftState? current,
            string message,
            AssistantOffersEvidence offers
        )
        {
            var state = current ?? new AssistantCampaignDraftState();
            var text = message.Trim();
            var lower = text.ToLowerInvariant();
            var freeTextGoalMatch = FreeTextGoalRegex().Match(text);

            ApplyNamedOption(text, Goals, (id, label) =>
            {
                state.GoalId = id;
                state.GoalLabel = label;
            });
            if (state.GoalId is null
                && freeTextGoalMatch.Success
                && !string.IsNullOrWhiteSpace(
                    freeTextGoalMatch.Groups["goal"].Value
                ))
            {
                state.GoalId = "custom-campaign";
                state.GoalLabel = "Custom campaign";
            }
            ApplyNamedOption(text, Audiences, (id, label) =>
            {
                state.AudienceKey = id;
                state.AudienceLabel = label;
            });
            ApplyNaturalAudience(state, lower);

            var nameMatch = CampaignNameRegex().Match(text);
            if (!nameMatch.Success)
            {
                nameMatch = NamePrefixRegex().Match(text);
            }
            if (nameMatch.Success)
            {
                state.Name = nameMatch.Groups["name"].Value
                    .Trim()
                    .TrimEnd('.', ',', ';');
            }

            var templateMatch = TemplateRegex().Match(text);
            if (templateMatch.Success)
            {
                state.TemplateId = templateMatch.Groups["template"].Value.Trim();
                if (int.TryParse(
                        templateMatch.Groups["version"].Value,
                        out var templateVersion))
                {
                    state.TemplateVersion = templateVersion;
                }
            }

            var subjectMatch = SubjectRegex().Match(text);
            if (subjectMatch.Success)
            {
                state.MessageSubject = subjectMatch.Groups["subject"].Value.Trim();
            }
            var bodyMatch = BodyRegex().Match(text);
            if (bodyMatch.Success)
            {
                state.MessageBody = bodyMatch.Groups["body"].Value.Trim();
            }

            if (ContainsAny(lower, "email", "mail them"))
            {
                state.Channel = "email";
            }
            else if (ContainsAny(lower, "sms", "text message", "text them", "text "))
            {
                state.Channel = "sms";
            }

            if (lower.Contains("no offer", StringComparison.Ordinal)
                || lower.Contains("without an offer", StringComparison.Ordinal)
                || lower.Contains("do not include an offer", StringComparison.Ordinal))
            {
                state.OfferStance = "no-offer";
                state.OfferLabel = "No offer";
                state.OfferId = null;
            }
            else if (lower.Contains("existing offer", StringComparison.Ordinal)
                || lower.Contains("attach an offer", StringComparison.Ordinal)
                || lower.Contains("use an offer", StringComparison.Ordinal))
            {
                state.OfferStance = "existing-offer";
                state.OfferLabel = "Existing offer";
                ResolveOffer(state, text, offers);
            }
            else if (state.OfferStance == "existing-offer" && state.OfferId is null)
            {
                ResolveOffer(state, text, offers);
            }

            if (lower.Contains("draft it now", StringComparison.Ordinal)
                || lower.Contains("skip the rest", StringComparison.Ordinal))
            {
                state.UsefulOptionalsSkipped = true;
            }

            if (current is not null && state.Name is null)
            {
                state.Name = InferNameFromInterviewReply(
                    text,
                    freeTextGoalMatch
                );
            }

            if (state.Name is null && state.GoalLabel is not null)
            {
                state.Name = state.GoalLabel;
            }
            if (state.Name is null && state.TemplateId is not null)
            {
                state.Name = state.TemplateId;
            }

            if (state.Name is null && state.GoalId is null)
            {
                return new AssistantCampaignDraftTurn(
                    state,
                    "Campaign draft details",
                    AssistantDraftCatalogueCopy.Ask(
                        "What should this Campaign be called? You can also choose a goal by replying with one exact label.",
                        "Campaign goal catalogue",
                        Goals.Select(goal => goal.Label)
                    ),
                    false
                );
            }

            var usefulMissing = new List<string>();
            if (state.AudienceKey is null) usefulMissing.Add("audience");
            if (state.Channel is null) usefulMissing.Add("channel");
            if (state.OfferStance is null) usefulMissing.Add("offer");
            if (!state.UsefulOptionalsSkipped && usefulMissing.Count > 0)
            {
                var sections = new List<string>
                {
                    $"Please choose the remaining useful fields: {string.Join(", ", usefulMissing)}.",
                };
                if (state.AudienceKey is null)
                {
                    sections.Add(
                        AssistantDraftCatalogueCopy.Ask(
                            "Choose an audience.",
                            "Audience catalogue",
                            Audiences.Select(item => item.Label)
                        )
                    );
                }
                if (state.Channel is null)
                {
                    sections.Add(
                        AssistantDraftCatalogueCopy.Ask(
                            "Choose a channel.",
                            "Channel catalogue",
                            ["Email", "SMS"]
                        )
                    );
                }
                if (state.OfferStance is null)
                {
                    sections.Add(
                        AssistantDraftCatalogueCopy.Ask(
                            "Choose an offer stance.",
                            "Offer catalogue",
                            ["No offer", "Existing offer"]
                        )
                    );
                }
                sections.Add(
                    "You can also say “Draft it now” to skip these optional fields."
                );
                return new AssistantCampaignDraftTurn(
                    state,
                    "Campaign draft details",
                    string.Join("\n\n", sections),
                    false
                );
            }

            if (state.OfferStance == "existing-offer" && state.OfferId is null)
            {
                var candidates = offers.Catalog
                    .Where(offer => offer.Status.Equals("active", StringComparison.OrdinalIgnoreCase))
                    .Take(5)
                    .Select(offer => offer.Title)
                    .ToList();
                return new AssistantCampaignDraftTurn(
                    state,
                    "Campaign draft details",
                    AssistantDraftCatalogueCopy.AskCandidates(
                        "Which existing offer should this Campaign use?",
                        "Active offer catalogue",
                        candidates,
                        "There are no Active offers to attach."
                    ),
                    false
                );
            }

            var summary = new List<string>
            {
                $"- **Name:** {state.Name}",
            };
            if (state.GoalLabel is not null) summary.Add($"- **Goal:** {state.GoalLabel}");
            if (state.AudienceLabel is not null) summary.Add($"- **Audience:** {state.AudienceLabel}");
            if (state.Channel is not null) summary.Add($"- **Channel:** {state.Channel.ToUpperInvariant()}");
            if (state.OfferLabel is not null) summary.Add($"- **Offer:** {state.OfferLabel}");
            var selectedOffer = offers.Catalog.FirstOrDefault(offer => offer.Id == state.OfferId);
            if (selectedOffer is not null) summary.Add($"- **Existing offer:** {selectedOffer.Title}");

            return new AssistantCampaignDraftTurn(
                state,
                "Campaign draft ready",
                string.Join("\n", summary)
                    + "\n\nSelect **Create campaign draft** to save it. The Campaign will stay as a draft and will not be published.",
                true
            );
        }

        public static AssistantCampaignDraftPayloadDto ToPayload(
            AssistantCampaignDraftState state,
            int locationId
        )
            => new()
            {
                LocationId = locationId,
                Name = state.Name!,
                GoalId = state.GoalId,
                TemplateId = state.TemplateId,
                TemplateVersion = state.TemplateVersion,
                AudienceKey = state.AudienceKey,
                Channel = state.Channel,
                OfferStance = state.OfferStance,
                OfferId = state.OfferId,
                MessageSubject = state.MessageSubject,
                MessageBody = state.MessageBody,
            };

        public static bool IsReady(AssistantCampaignDraftState state)
            => state.Name is not null
                && (state.UsefulOptionalsSkipped
                    || (state.AudienceKey is not null
                        && state.Channel is not null
                        && state.OfferStance is not null))
                && (state.OfferStance != "existing-offer" || state.OfferId is not null);

        private static void ApplyNamedOption(
            string text,
            IEnumerable<(string Id, string Label)> options,
            Action<string, string> apply
        )
        {
            var matches = options
                .Where(option =>
                    text.Contains(option.Label, StringComparison.OrdinalIgnoreCase)
                    || text.Contains(option.Id, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (matches.Count == 1)
            {
                apply(matches[0].Id, matches[0].Label);
            }
        }

        private static void ResolveOffer(
            AssistantCampaignDraftState state,
            string text,
            AssistantOffersEvidence offers
        )
        {
            var matches = offers.Catalog
                .Where(offer =>
                    offer.Status.Equals("active", StringComparison.OrdinalIgnoreCase)
                    && (text.Contains(offer.Title, StringComparison.OrdinalIgnoreCase)
                        || offer.Title.Contains(text, StringComparison.OrdinalIgnoreCase)))
                .ToList();
            if (matches.Count == 1)
            {
                state.OfferId = matches[0].Id;
            }
        }

        private static void ApplyNaturalAudience(
            AssistantCampaignDraftState state,
            string lower
        )
        {
            if (state.AudienceKey is not null)
            {
                return;
            }

            (string Id, string Label)? match = lower switch
            {
                _ when ContainsAny(lower, "everyone", "all guests", "every eligible guest")
                    => ("all-eligible-guests", "All eligible guests"),
                _ when ContainsAny(lower, "new customers", "first-time guests", "first time guests")
                    => ("new-guests", "New guests"),
                _ when ContainsAny(lower, "happy guests", "positive responders", "good feedback")
                    => ("positive-feedback", "Positive feedback"),
                _ when ContainsAny(lower, "did not redeem", "haven't redeemed", "not redeemed")
                    => ("offer-not-redeemed", "Offer not redeemed"),
                _ when ContainsAny(lower, "redeemed recently", "recently redeemed")
                    => ("recent-redeemers", "Recent redeemers"),
                _ when ContainsAny(lower, "no recent activity", "inactive guests", "inactive customers")
                    => ("no-recent-tummly-activity", "No recent Tummly activity"),
                _ when ContainsAny(lower, "finished recovery", "recovery is complete")
                    => ("completed-recovery-follow-up", "Completed recovery follow-up"),
                _ when ContainsAny(lower, "lapsed guests", "lapsed customers")
                    => ("dormant-guests", "Dormant guests"),
                _ => null,
            };
            if (match is not null)
            {
                state.AudienceKey = match.Value.Id;
                state.AudienceLabel = match.Value.Label;
            }
        }

        private static string? InferNameFromInterviewReply(
            string text,
            Match freeTextGoalMatch
        )
        {
            if (freeTextGoalMatch.Success && freeTextGoalMatch.Index > 0)
            {
                var leadingName = text[..freeTextGoalMatch.Index]
                    .Trim()
                    .TrimEnd('.', ',', ';');
                return string.IsNullOrWhiteSpace(leadingName)
                    ? null
                    : leadingName;
            }

            // A reply that supplied another known field is not also a free name.
            var lower = text.ToLowerInvariant();
            if (freeTextGoalMatch.Success
                || Goals.Any(option =>
                    text.Contains(option.Label, StringComparison.OrdinalIgnoreCase)
                    || text.Contains(option.Id, StringComparison.OrdinalIgnoreCase))
                || Audiences.Any(option =>
                    text.Contains(option.Label, StringComparison.OrdinalIgnoreCase)
                    || text.Contains(option.Id, StringComparison.OrdinalIgnoreCase))
                || TemplateRegex().IsMatch(text)
                || SubjectRegex().IsMatch(text)
                || BodyRegex().IsMatch(text)
                || ContainsAny(
                    lower,
                    "email",
                    "sms",
                    "text message",
                    "no offer",
                    "without an offer",
                    "existing offer",
                    "draft it now",
                    "skip the rest"
                )
                || text.Length > 120)
            {
                return null;
            }

            var candidate = text.Trim().TrimEnd('.', ',', ';');
            return string.IsNullOrWhiteSpace(candidate) ? null : candidate;
        }

        private static bool ContainsAny(string haystack, params string[] needles)
            => needles.Any(needle =>
                haystack.Contains(needle, StringComparison.Ordinal)
            );

        [GeneratedRegex(
            "(?:call(?:ed)?\\s+it|name(?:d)?\\s+(?:it\\s+)?|campaign\\s+(?:called|named))\\s+[\\\"']?(?<name>[^\\\"'\\n,;]+)",
            RegexOptions.IgnoreCase
        )]
        private static partial Regex CampaignNameRegex();

        [GeneratedRegex(
            "^(?:name|called)\\s*:?[\\s\\\"']*(?<name>[^\\\"'\\n,;]+)",
            RegexOptions.IgnoreCase
        )]
        private static partial Regex NamePrefixRegex();

        [GeneratedRegex(
            "(?:^|[.,;]\\s*|\\band\\s+)(?:the\\s+)?goal\\s*(?:would\\s+be|is|:)?\\s*(?:to\\s+)?(?<goal>[^.!?]+)",
            RegexOptions.IgnoreCase
        )]
        private static partial Regex FreeTextGoalRegex();

        [GeneratedRegex(
            "(?:template)\\s+[\\\"']?(?<template>[a-z0-9][a-z0-9 _-]*?)[\\\"']?(?:\\s+version\\s+(?<version>\\d+))?(?:[.,;]|$)",
            RegexOptions.IgnoreCase
        )]
        private static partial Regex TemplateRegex();

        [GeneratedRegex(
            "(?:subject)\\s*:\\s*(?<subject>[^\\n;]+)",
            RegexOptions.IgnoreCase
        )]
        private static partial Regex SubjectRegex();

        [GeneratedRegex(
            "(?:message body|body)\\s*:\\s*(?<body>[^\\n]+)",
            RegexOptions.IgnoreCase
        )]
        private static partial Regex BodyRegex();
    }
}
