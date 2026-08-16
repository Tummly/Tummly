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
                && (lower.Contains("draft", StringComparison.Ordinal)
                    || lower.Contains("create", StringComparison.Ordinal));
        }

        public static AssistantCampaignDraftState? Parse(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                var state = JsonSerializer.Deserialize<AssistantCampaignDraftState>(json);
                return state?.Target == "campaign" ? state : null;
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

            ApplyNamedOption(text, Goals, (id, label) =>
            {
                state.GoalId = id;
                state.GoalLabel = label;
            });
            ApplyNamedOption(text, Audiences, (id, label) =>
            {
                state.AudienceKey = id;
                state.AudienceLabel = label;
            });

            var nameMatch = CampaignNameRegex().Match(text);
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

            if (lower.Contains("email", StringComparison.Ordinal))
            {
                state.Channel = "email";
            }
            else if (lower.Contains("sms", StringComparison.Ordinal)
                || lower.Contains("text message", StringComparison.Ordinal))
            {
                state.Channel = "sms";
            }

            if (lower.Contains("no offer", StringComparison.Ordinal)
                || lower.Contains("without an offer", StringComparison.Ordinal))
            {
                state.OfferStance = "no-offer";
                state.OfferLabel = "No offer";
                state.OfferId = null;
            }
            else if (lower.Contains("existing offer", StringComparison.Ordinal))
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
                    "What should this Campaign be called, or what is its goal? Goals: "
                        + string.Join(", ", Goals.Select(goal => goal.Label))
                        + ".",
                    false
                );
            }

            var usefulMissing = new List<string>();
            if (state.AudienceKey is null) usefulMissing.Add("audience");
            if (state.Channel is null) usefulMissing.Add("channel");
            if (state.OfferStance is null) usefulMissing.Add("offer");
            if (!state.UsefulOptionalsSkipped && usefulMissing.Count > 0)
            {
                return new AssistantCampaignDraftTurn(
                    state,
                    "Campaign draft details",
                    $"Please choose the remaining useful fields: {string.Join(", ", usefulMissing)}. "
                        + $"Audiences: {string.Join(", ", Audiences.Select(item => item.Label))}. "
                        + "Channels: Email or SMS. Offer: No offer or Existing offer. "
                        + "You can also say “Draft it now” to skip these optional fields.",
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
                var candidateCopy = candidates.Count == 0
                    ? "There are no Active offers to attach."
                    : $"Choose one Active offer: {string.Join(", ", candidates)}.";
                return new AssistantCampaignDraftTurn(
                    state,
                    "Campaign draft details",
                    $"Which existing offer should this Campaign use? {candidateCopy}",
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
                string.Join("\n", summary),
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

        [GeneratedRegex(
            "(?:call(?:ed)?\\s+it|name(?:d)?\\s+(?:it\\s+)?|campaign\\s+(?:called|named))\\s+[\\\"']?(?<name>[^\\\"'\\n,;]+)",
            RegexOptions.IgnoreCase
        )]
        private static partial Regex CampaignNameRegex();

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
