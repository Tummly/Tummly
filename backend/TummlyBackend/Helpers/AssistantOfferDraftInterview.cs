using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using TummlyBackend.DTOs.Assistant;

namespace TummlyBackend.Helpers
{
    public sealed class AssistantOfferDraftState
    {
        public string Target { get; set; } = "offer";
        public string? OfferType { get; set; }
        public string? OfferTypeLabel { get; set; }
        public decimal? DiscountPercentage { get; set; }
        public decimal? DiscountAmount { get; set; }
        public string? FreeItemText { get; set; }
        public string? PurchaseRequirement { get; set; }
        public decimal? MinimumSpend { get; set; }
        public string? ReplacementItemText { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string Validity { get; set; } = "7_days_after_issue";
        public string? ExpiryDate { get; set; }
        public string? StaffInstructions { get; set; }
        public bool UsefulOptionalsSkipped { get; set; }
    }

    public sealed record AssistantOfferDraftTurn(
        AssistantOfferDraftState State,
        string Title,
        string Body,
        bool IsReady
    );

    public static partial class AssistantOfferDraftInterview
    {
        private static readonly (string Id, string Label, string[] Aliases)[] OfferTypes =
        [
            ("percentage_discount", "Percentage discount", ["percentage discount", "percent discount"]),
            ("fixed_discount", "Fixed discount", ["fixed discount", "amount discount"]),
            ("free_item", "Free item", ["free item"]),
            ("replacement_item", "Replacement item", ["replacement item", "buy one get one"]),
        ];

        public static bool IsOfferDraftAsk(string message)
        {
            var lower = message.Trim().ToLowerInvariant();
            return ContainsAny(
                lower,
                "draft an offer",
                "draft offer",
                "offer draft",
                "create an offer",
                "create offer",
                "prepare an offer",
                "prepare offer",
                "make an offer",
                "make offer",
                "build an offer",
                "build offer",
                "set up an offer",
                "set up offer",
                "write an offer",
                "write offer"
            );
        }

        public static AssistantOfferDraftState? Parse(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                var state = JsonSerializer.Deserialize<AssistantOfferDraftState>(json);
                return state?.Target == "offer" ? state : null;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        public static string Serialize(AssistantOfferDraftState state)
            => JsonSerializer.Serialize(state);

        public static AssistantOfferDraftTurn Apply(
            AssistantOfferDraftState? current,
            string message
        )
        {
            var state = current ?? new AssistantOfferDraftState();
            var text = message.Trim();
            var lower = text.ToLowerInvariant();

            foreach (var type in OfferTypes)
            {
                if (type.Aliases.Count(alias => lower.Contains(alias, StringComparison.Ordinal)) == 0
                    && !lower.Contains(type.Id, StringComparison.Ordinal))
                {
                    continue;
                }
                state.OfferType = type.Id;
                state.OfferTypeLabel = type.Label;
                break;
            }

            ApplyValueFields(state, text, lower, typeAlreadyLocked: current?.OfferType is not null);
            ApplyCopyFields(state, text);
            ApplyValidity(state, text, lower);

            if (lower.Contains("draft it now", StringComparison.Ordinal)
                || lower.Contains("skip the rest", StringComparison.Ordinal))
            {
                state.UsefulOptionalsSkipped = true;
            }

            if (state.OfferType is null)
            {
                return Ask(
                    state,
                    "Which offer type should I use? Choose Percentage discount, Fixed discount, Free item, or Replacement item."
                );
            }

            var requiredQuestion = RequiredTypeQuestion(state);
            if (requiredQuestion is not null)
            {
                return Ask(state, requiredQuestion);
            }

            ProposeCopy(state);

            if (state.Validity == "choose_expiry_date" && state.ExpiryDate is null)
            {
                return Ask(state, "What expiry date should I use? Enter it as YYYY-MM-DD.");
            }

            if (!state.UsefulOptionalsSkipped && state.StaffInstructions is null)
            {
                return Ask(
                    state,
                    "What staff instructions should I add? You can also say “Draft it now” to skip this optional field."
                );
            }

            return new AssistantOfferDraftTurn(
                state,
                "Offer draft ready",
                Summary(state),
                true
            );
        }

        public static bool IsReady(AssistantOfferDraftState state)
            => state.OfferType is not null
                && RequiredTypeQuestion(state) is null
                && !string.IsNullOrWhiteSpace(state.Title)
                && !string.IsNullOrWhiteSpace(state.Description)
                && (state.Validity != "choose_expiry_date"
                    || !string.IsNullOrWhiteSpace(state.ExpiryDate))
                && (state.UsefulOptionalsSkipped || state.StaffInstructions is not null);

        public static AssistantOfferDraftPayloadDto ToPayload(
            AssistantOfferDraftState state,
            int locationId
        )
            => new()
            {
                LocationId = locationId,
                OfferType = state.OfferType!,
                Title = state.Title!,
                Description = state.Description!,
                Validity = state.Validity,
                ExpiryDate = state.ExpiryDate,
                DiscountPercentage = state.DiscountPercentage,
                DiscountAmount = state.DiscountAmount,
                FreeItemText = state.FreeItemText,
                PurchaseRequirement = state.PurchaseRequirement,
                MinimumSpend = state.MinimumSpend,
                ReplacementItemText = state.ReplacementItemText,
                StaffInstructions = state.StaffInstructions,
            };

        private static AssistantOfferDraftTurn Ask(
            AssistantOfferDraftState state,
            string body
        )
            => new(state, "Offer draft details", body, false);

        private static void ApplyValueFields(
            AssistantOfferDraftState state,
            string text,
            string lower,
            bool typeAlreadyLocked
        )
        {
            var priorTypeComplete =
                typeAlreadyLocked && RequiredTypeQuestion(state) is null;

            var number = NumberRegex().Match(text);
            if (number.Success
                && decimal.TryParse(
                    number.Groups["value"].Value,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out var value))
            {
                // Ask-back replies may be bare numbers once the type cluster is locked.
                if (state.OfferType == "percentage_discount"
                    && state.DiscountPercentage is null)
                {
                    state.DiscountPercentage = value;
                }
                else if (state.OfferType == "fixed_discount"
                    && state.DiscountAmount is null)
                {
                    state.DiscountAmount = value;
                }
                else if (state.OfferType == "free_item"
                    && state.PurchaseRequirement == "with_minimum_spend"
                    && state.MinimumSpend is null)
                {
                    state.MinimumSpend = value;
                }
            }

            var freeItem = FreeItemRegex().Match(text);
            if (freeItem.Success)
            {
                state.FreeItemText = Clean(freeItem.Groups["item"].Value);
            }
            else if (typeAlreadyLocked
                && state.OfferType == "free_item"
                && string.IsNullOrWhiteSpace(state.FreeItemText)
                && !IsSkipPhrase(lower)
                && !LooksLikePurchaseRequirement(lower)
                && !number.Success)
            {
                state.FreeItemText = Clean(text);
            }

            var replacementItem = ReplacementItemRegex().Match(text);
            if (replacementItem.Success)
            {
                state.ReplacementItemText = Clean(replacementItem.Groups["item"].Value);
            }
            else if (typeAlreadyLocked
                && state.OfferType == "replacement_item"
                && string.IsNullOrWhiteSpace(state.ReplacementItemText)
                && !IsSkipPhrase(lower)
                && !number.Success)
            {
                state.ReplacementItemText = Clean(text);
            }

            if (lower.Contains("no purchase", StringComparison.Ordinal))
            {
                state.PurchaseRequirement = "no_purchase_required";
            }
            else if (lower.Contains("minimum spend", StringComparison.Ordinal))
            {
                state.PurchaseRequirement = "with_minimum_spend";
            }
            else if (lower.Contains("any purchase", StringComparison.Ordinal))
            {
                state.PurchaseRequirement = "with_any_purchase";
            }

            var staff = StaffInstructionsRegex().Match(text);
            if (staff.Success)
            {
                state.StaffInstructions = Clean(staff.Groups["value"].Value);
            }
            else if (priorTypeComplete
                && state.StaffInstructions is null
                && !IsSkipPhrase(lower)
                && !LooksLikeValidityAnswer(lower)
                && TitleRegex().Match(text) is { Success: false }
                && DescriptionRegex().Match(text) is { Success: false })
            {
                // Useful-ask reply may be bare staff copy after persist-minimum is met.
                state.StaffInstructions = Clean(text);
            }
        }

        private static void ApplyCopyFields(AssistantOfferDraftState state, string text)
        {
            var title = TitleRegex().Match(text);
            if (title.Success)
            {
                state.Title = Clean(title.Groups["value"].Value);
            }
            var description = DescriptionRegex().Match(text);
            if (description.Success)
            {
                state.Description = Clean(description.Groups["value"].Value);
            }
        }

        private static void ApplyValidity(
            AssistantOfferDraftState state,
            string text,
            string lower
        )
        {
            if (lower.Contains("choose expiry", StringComparison.Ordinal)
                || lower.Contains("specific date", StringComparison.Ordinal))
            {
                state.Validity = "choose_expiry_date";
            }
            else if (lower.Contains("30 days", StringComparison.Ordinal))
            {
                state.Validity = "30_days_after_issue";
            }
            else if (lower.Contains("14 days", StringComparison.Ordinal))
            {
                state.Validity = "14_days_after_issue";
            }
            else if (lower.Contains("7 days", StringComparison.Ordinal))
            {
                state.Validity = "7_days_after_issue";
            }

            var expiry = IsoDateRegex().Match(text);
            if (expiry.Success)
            {
                state.Validity = "choose_expiry_date";
                state.ExpiryDate = expiry.Value;
            }
        }

        private static string? RequiredTypeQuestion(AssistantOfferDraftState state)
        {
            return state.OfferType switch
            {
                "percentage_discount" when state.DiscountPercentage is null
                    => "What discount percentage should the Offer give?",
                "fixed_discount" when state.DiscountAmount is null
                    => "What fixed discount amount should the Offer give?",
                "free_item" when string.IsNullOrWhiteSpace(state.FreeItemText)
                    => "Which free item should the Offer give?",
                "free_item" when state.PurchaseRequirement is null
                    => "What purchase requirement should apply: No purchase required, With any purchase, or With a minimum spend?",
                "free_item" when state.PurchaseRequirement == "with_minimum_spend"
                    && state.MinimumSpend is null
                    => "What minimum spend should apply?",
                "replacement_item" when string.IsNullOrWhiteSpace(state.ReplacementItemText)
                    => "Which item can the guest replace?",
                _ => null,
            };
        }

        private static void ProposeCopy(AssistantOfferDraftState state)
        {
            if (state.Title is null)
            {
                state.Title = state.OfferType switch
                {
                    "percentage_discount" => $"{state.DiscountPercentage:0.##}% off your next visit",
                    "fixed_discount" => $"£{state.DiscountAmount:0.##} off your next order",
                    "free_item" => $"Enjoy a free {state.FreeItemText}",
                    _ => $"Replacement {state.ReplacementItemText}",
                };
            }
            if (state.Description is null)
            {
                state.Description = state.OfferType switch
                {
                    "percentage_discount" => $"Save {state.DiscountPercentage:0.##}% on your next visit.",
                    "fixed_discount" => $"Save £{state.DiscountAmount:0.##} on your next order.",
                    "free_item" => $"Enjoy a free {state.FreeItemText} on your next visit.",
                    _ => $"Receive a replacement {state.ReplacementItemText}.",
                };
            }
        }

        private static bool ContainsAny(string haystack, params string[] needles)
            => needles.Any(needle =>
                haystack.Contains(needle, StringComparison.Ordinal)
            );

        private static string Summary(AssistantOfferDraftState state)
        {
            var rows = new List<string>
            {
                $"- **Offer type:** {state.OfferTypeLabel}",
                $"- **Title:** {state.Title}",
                $"- **Description:** {state.Description}",
                $"- **Validity:** {ValidityLabel(state.Validity)}",
            };
            if (state.DiscountPercentage is not null)
                rows.Add($"- **Discount:** {state.DiscountPercentage:0.##}%");
            if (state.DiscountAmount is not null)
                rows.Add($"- **Discount:** £{state.DiscountAmount:0.##}");
            if (state.FreeItemText is not null)
                rows.Add($"- **Free item:** {state.FreeItemText}");
            if (state.PurchaseRequirement is not null)
                rows.Add(
                    $"- **Purchase requirement:** {PurchaseRequirementLabel(state.PurchaseRequirement)}"
                );
            if (state.MinimumSpend is not null)
                rows.Add($"- **Minimum spend:** £{state.MinimumSpend:0.##}");
            if (state.ReplacementItemText is not null)
                rows.Add($"- **Replacement item:** {state.ReplacementItemText}");
            if (state.ExpiryDate is not null)
                rows.Add($"- **Expiry date:** {state.ExpiryDate}");
            if (state.StaffInstructions is not null)
                rows.Add($"- **Staff instructions:** {state.StaffInstructions}");
            return string.Join("\n", rows);
        }

        private static string ValidityLabel(string validity)
            => validity switch
            {
                "7_days_after_issue" => "7 days after issue",
                "14_days_after_issue" => "14 days after issue",
                "30_days_after_issue" => "30 days after issue",
                "choose_expiry_date" => "Choose an expiry date",
                _ => validity,
            };

        private static string PurchaseRequirementLabel(string requirement)
            => requirement switch
            {
                "no_purchase_required" => "No purchase required",
                "with_any_purchase" => "With any purchase",
                "with_minimum_spend" => "With a minimum spend",
                _ => requirement,
            };

        private static bool IsSkipPhrase(string lower)
            => lower.Contains("draft it now", StringComparison.Ordinal)
                || lower.Contains("skip the rest", StringComparison.Ordinal);

        private static bool LooksLikePurchaseRequirement(string lower)
            => lower.Contains("no purchase", StringComparison.Ordinal)
                || lower.Contains("minimum spend", StringComparison.Ordinal)
                || lower.Contains("any purchase", StringComparison.Ordinal);

        private static bool LooksLikeValidityAnswer(string lower)
            => lower.Contains("choose expiry", StringComparison.Ordinal)
                || lower.Contains("specific date", StringComparison.Ordinal)
                || lower.Contains("30 days", StringComparison.Ordinal)
                || lower.Contains("14 days", StringComparison.Ordinal)
                || lower.Contains("7 days", StringComparison.Ordinal)
                || IsoDateRegex().IsMatch(lower);

        private static string Clean(string value)
            => value.Trim().TrimEnd('.', ',', ';');

        [GeneratedRegex(@"(?<value>\d+(?:\.\d+)?)")]
        private static partial Regex NumberRegex();

        [GeneratedRegex(@"(?:free item|item)\s*(?:is|:)?\s*(?<item>[^,;\n]+)", RegexOptions.IgnoreCase)]
        private static partial Regex FreeItemRegex();

        [GeneratedRegex(@"(?:replacement item|replace)\s*(?:is|:)?\s*(?<item>[^,;\n]+)", RegexOptions.IgnoreCase)]
        private static partial Regex ReplacementItemRegex();

        [GeneratedRegex(@"(?:title)\s*:\s*(?<value>[^;\n]+)", RegexOptions.IgnoreCase)]
        private static partial Regex TitleRegex();

        [GeneratedRegex(@"(?:description)\s*:\s*(?<value>[^;\n]+)", RegexOptions.IgnoreCase)]
        private static partial Regex DescriptionRegex();

        [GeneratedRegex(@"(?:staff instructions?)\s*:\s*(?<value>[^;\n]+)", RegexOptions.IgnoreCase)]
        private static partial Regex StaffInstructionsRegex();

        [GeneratedRegex(@"\b\d{4}-\d{2}-\d{2}\b")]
        private static partial Regex IsoDateRegex();
    }
}
