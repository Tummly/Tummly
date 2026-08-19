using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using TummlyBackend.DTOs.Offers;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Operator-controlled Offer path terms. Does not start a Draft interview
    /// and does not default validity.
    /// </summary>
    public sealed class AssistantOfferPathTermsState
    {
        public string? OfferType { get; set; }

        public decimal? DiscountPercentage { get; set; }

        public decimal? DiscountAmount { get; set; }

        public string? FreeItemText { get; set; }

        public string? PurchaseRequirement { get; set; }

        public decimal? MinimumSpend { get; set; }

        public string? ReplacementItemText { get; set; }

        public string? Validity { get; set; }

        public string? ExpiryDate { get; set; }

        public string? Title { get; set; }

        public string? Description { get; set; }

        public bool OperatorDelegatedTerms { get; set; }

        public List<string> ConflictingBenefits { get; set; } = [];

        public bool WantsActivate { get; set; }
    }

    public static partial class AssistantOfferPathTerms
    {
        public static AssistantOfferPathTermsState Parse(
            string message,
            DateTime? utcNow = null
        )
        {
            var state = new AssistantOfferPathTermsState();
            Apply(state, message, utcNow ?? DateTime.UtcNow);
            return state;
        }

        public static AssistantOfferPathTermsState Merge(
            AssistantOfferPathTermsState? prior,
            string message,
            DateTime? utcNow = null
        )
        {
            var state = Clone(prior) ?? new AssistantOfferPathTermsState();
            Apply(state, message, utcNow ?? DateTime.UtcNow);
            return state;
        }

        public static AssistantOfferPathTermsState? FromJson(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<AssistantOfferPathTermsState>(json);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        public static string Serialize(AssistantOfferPathTermsState state)
            => JsonSerializer.Serialize(state);

        public static IReadOnlyList<string> MissingFields(AssistantOfferPathTermsState state)
        {
            if (state.OperatorDelegatedTerms && !HasAnyAuthorisedBenefit(state))
            {
                return ["type", "value", "required usage", "validity"];
            }

            if (state.ConflictingBenefits.Count >= 2)
            {
                return [];
            }

            var missing = new List<string>();
            if (state.OfferType is null)
            {
                missing.Add("type");
                missing.Add("value");
            }
            else if (state.OfferType == "percentage_discount"
                && state.DiscountPercentage is null)
            {
                missing.Add("value");
            }
            else if (state.OfferType == "fixed_discount"
                && state.DiscountAmount is null)
            {
                missing.Add("value");
            }
            else if (state.OfferType == "free_item"
                && string.IsNullOrWhiteSpace(state.FreeItemText))
            {
                missing.Add("value");
            }
            else if (state.OfferType == "replacement_item"
                && string.IsNullOrWhiteSpace(state.ReplacementItemText))
            {
                missing.Add("value");
            }

            if (state.OfferType == "free_item")
            {
                if (state.PurchaseRequirement is null)
                {
                    missing.Add("required usage");
                }
                else if (state.PurchaseRequirement == "with_minimum_spend"
                    && state.MinimumSpend is null)
                {
                    missing.Add("required usage");
                }
            }

            if (state.Validity is null
                || (state.Validity == "choose_expiry_date"
                    && string.IsNullOrWhiteSpace(state.ExpiryDate)))
            {
                missing.Add("validity");
            }

            return missing;
        }

        public static bool IsComplete(AssistantOfferPathTermsState state)
            => !state.OperatorDelegatedTerms
                && state.ConflictingBenefits.Count < 2
                && MissingFields(state).Count == 0
                && state.OfferType is not null
                && state.Validity is not null;

        public static string GapBody(AssistantOfferPathTermsState state)
        {
            if (state.ConflictingBenefits.Count >= 2)
            {
                return "Which authorised benefit should this Offers catalog Draft use: "
                    + AssistantCreateLocationGap.Join(state.ConflictingBenefits)
                    + "?";
            }

            if (state.OperatorDelegatedTerms && !HasAnyAuthorisedBenefit(state))
            {
                return "Name the Offer type, value, required usage, and validity. I will not invent terms.";
            }

            var missing = MissingFields(state);
            if (missing.Count == 0)
            {
                return "Name the missing Offer terms.";
            }

            if (missing.Count == 1)
            {
                return $"Which {missing[0]} should this Offers catalog Draft use?";
            }

            if (missing.Count == 2)
            {
                return $"Which {missing[0]} and {missing[1]} should this Offers catalog Draft use?";
            }

            var head = string.Join(", ", missing.Take(missing.Count - 1));
            return $"Which {head}, and {missing[^1]} should this Offers catalog Draft use?";
        }

        public static void ProposeCopy(AssistantOfferPathTermsState state)
        {
            if (!IsComplete(state))
            {
                return;
            }

            state.Title ??= state.OfferType switch
            {
                "percentage_discount" => $"{state.DiscountPercentage:0.##}% off",
                "fixed_discount" => $"£{state.DiscountAmount:0.##} off",
                "free_item" => $"Enjoy a free {state.FreeItemText}",
                _ => $"Replacement {state.ReplacementItemText}",
            };
            state.Description ??= state.OfferType switch
            {
                "percentage_discount" => $"Save {state.DiscountPercentage:0.##}%.",
                "fixed_discount" => $"Save £{state.DiscountAmount:0.##}.",
                "free_item" => $"Enjoy a free {state.FreeItemText}.",
                _ => $"Receive a replacement {state.ReplacementItemText}.",
            };
        }

        public static CreateCatalogOfferRequest ToCreateRequest(
            AssistantOfferPathTermsState state,
            int locationId
        )
        {
            ProposeCopy(state);
            return new CreateCatalogOfferRequest
            {
                LocationId = locationId,
                OfferType = state.OfferType ?? string.Empty,
                Title = state.Title ?? string.Empty,
                Description = state.Description ?? string.Empty,
                Validity = state.Validity ?? string.Empty,
                ExpiryDate = state.ExpiryDate,
                DiscountPercentage = state.DiscountPercentage,
                DiscountAmount = state.DiscountAmount,
                FreeItemText = state.FreeItemText,
                PurchaseRequirement = state.PurchaseRequirement,
                MinimumSpend = state.MinimumSpend,
                ReplacementItemText = state.ReplacementItemText,
                StaffInstructions = null,
                AdditionalExclusions = null,
            };
        }

        public static string TypeLabel(string? offerType)
            => offerType switch
            {
                "percentage_discount" => "Percentage discount",
                "fixed_discount" => "Fixed discount",
                "free_item" => "Free item",
                "replacement_item" => "Replacement item",
                _ => offerType ?? "Offer",
            };

        public static string ValueLabel(AssistantOfferPathTermsState state)
            => state.OfferType switch
            {
                "percentage_discount" => $"{state.DiscountPercentage:0.##}%",
                "fixed_discount" => $"£{state.DiscountAmount:0.##}",
                "free_item" => state.FreeItemText ?? string.Empty,
                "replacement_item" => state.ReplacementItemText ?? string.Empty,
                _ => string.Empty,
            };

        public static string ValidityLabel(AssistantOfferPathTermsState state)
            => state.Validity switch
            {
                "7_days_after_issue" => "7 days after issue",
                "14_days_after_issue" => "14 days after issue",
                "30_days_after_issue" => "30 days after issue",
                "choose_expiry_date" when state.ExpiryDate is not null
                    => state.ExpiryDate,
                _ => state.Validity ?? string.Empty,
            };

        private static void Apply(
            AssistantOfferPathTermsState state,
            string message,
            DateTime utcNow
        )
        {
            var text = message.Trim();
            var lower = text.ToLowerInvariant();
            if (text.Length == 0)
            {
                return;
            }

            if (LooksLikeDelegatedTerms(lower))
            {
                state.OperatorDelegatedTerms = true;
            }

            if (LooksLikeActivate(lower))
            {
                state.WantsActivate = true;
            }

            ApplyBenefits(state, text, lower);
            ApplyValidity(state, lower, utcNow);
            ApplyPurchaseRequirement(state, lower, text);

            if (HasAnyAuthorisedBenefit(state) || state.Validity is not null)
            {
                state.OperatorDelegatedTerms = false;
            }
        }

        private static void ApplyBenefits(
            AssistantOfferPathTermsState state,
            string text,
            string lower
        )
        {
            var found = new List<(string Type, string Label)>();

            var percent = PercentRegex().Match(text);
            if (percent.Success
                && decimal.TryParse(
                    percent.Groups["value"].Value,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out var pct))
            {
                found.Add(("percentage_discount", $"{pct:0.##}% off"));
                state.DiscountPercentage ??= pct;
            }
            else if (ContainsAny(lower, "percent off", "percentage off", "percentage discount"))
            {
                found.Add(("percentage_discount", "percentage discount"));
            }

            var pounds = PoundsRegex().Match(text);
            if (pounds.Success
                && decimal.TryParse(
                    pounds.Groups["value"].Value,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out var amount))
            {
                found.Add(("fixed_discount", $"£{amount:0.##} off"));
                state.DiscountAmount ??= amount;
            }
            else if (ContainsAny(lower, "money off", "amount off", "pounds off", "fixed discount"))
            {
                found.Add(("fixed_discount", "fixed discount"));
            }

            var freeItem = FreeItemRegex().Match(text);
            if (freeItem.Success)
            {
                var item = Clean(freeItem.Groups["item"].Value);
                found.Add(("free_item", $"free {item}"));
                state.FreeItemText ??= item;
            }
            else if (ContainsAny(lower, "complimentary", "free item", "free "))
            {
                found.Add(("free_item", "free item"));
            }

            var replacement = ReplacementItemRegex().Match(text);
            if (replacement.Success)
            {
                var item = Clean(replacement.Groups["item"].Value);
                found.Add(("replacement_item", $"replacement {item}"));
                state.ReplacementItemText ??= item;
            }
            else if (ContainsAny(lower, "replace the", "swap the", "replacement"))
            {
                found.Add(("replacement_item", "replacement item"));
            }

            var distinct = found
                .Select(item => item.Type)
                .Distinct(StringComparer.Ordinal)
                .ToList();
            if (distinct.Count >= 2)
            {
                state.ConflictingBenefits = found
                    .Select(item => item.Label)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                state.OfferType = null;
                return;
            }

            if (distinct.Count == 1)
            {
                var resolvingConflict = state.ConflictingBenefits.Count >= 2;
                state.OfferType = distinct[0];
                state.ConflictingBenefits = [];
                if (resolvingConflict)
                {
                    ClearOtherBenefitFields(state);
                }
            }
        }

        private static void ApplyValidity(
            AssistantOfferPathTermsState state,
            string lower,
            DateTime utcNow
        )
        {
            if (ContainsAny(
                    lower,
                    "until the end of the year",
                    "end of the year",
                    "year-end",
                    "year end"
                ))
            {
                state.Validity = "choose_expiry_date";
                state.ExpiryDate = new DateTime(utcNow.Year, 12, 31)
                    .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                return;
            }

            if (lower.Contains("30 days", StringComparison.Ordinal))
            {
                state.Validity = "30_days_after_issue";
                state.ExpiryDate = null;
                return;
            }

            if (lower.Contains("14 days", StringComparison.Ordinal))
            {
                state.Validity = "14_days_after_issue";
                state.ExpiryDate = null;
                return;
            }

            if (lower.Contains("7 days", StringComparison.Ordinal))
            {
                state.Validity = "7_days_after_issue";
                state.ExpiryDate = null;
            }
        }

        private static void ApplyPurchaseRequirement(
            AssistantOfferPathTermsState state,
            string lower,
            string text
        )
        {
            if (lower.Contains("no purchase", StringComparison.Ordinal)
                || ContainsAny(lower, "nothing else required", "without buying"))
            {
                state.PurchaseRequirement = "no_purchase_required";
            }
            else if (ContainsAny(lower, "minimum spend", "spend at least", "minimum order"))
            {
                state.PurchaseRequirement = "with_minimum_spend";
                var number = NumberRegex().Match(text);
                if (number.Success
                    && decimal.TryParse(
                        number.Groups["value"].Value,
                        NumberStyles.Number,
                        CultureInfo.InvariantCulture,
                        out var spend)
                    && state.OfferType != "percentage_discount"
                    && state.OfferType != "fixed_discount")
                {
                    state.MinimumSpend ??= spend;
                }
            }
            else if (ContainsAny(lower, "any purchase", "buy anything", "any order"))
            {
                state.PurchaseRequirement = "with_any_purchase";
            }
        }

        private static void ClearOtherBenefitFields(AssistantOfferPathTermsState state)
        {
            switch (state.OfferType)
            {
                case "percentage_discount":
                    state.DiscountAmount = null;
                    state.FreeItemText = null;
                    state.ReplacementItemText = null;
                    break;
                case "fixed_discount":
                    state.DiscountPercentage = null;
                    state.FreeItemText = null;
                    state.ReplacementItemText = null;
                    break;
                case "free_item":
                    state.DiscountPercentage = null;
                    state.DiscountAmount = null;
                    state.ReplacementItemText = null;
                    break;
                case "replacement_item":
                    state.DiscountPercentage = null;
                    state.DiscountAmount = null;
                    state.FreeItemText = null;
                    break;
            }
        }

        private static bool HasAnyAuthorisedBenefit(AssistantOfferPathTermsState state)
            => state.OfferType is not null
                || state.DiscountPercentage is not null
                || state.DiscountAmount is not null
                || !string.IsNullOrWhiteSpace(state.FreeItemText)
                || !string.IsNullOrWhiteSpace(state.ReplacementItemText)
                || state.ConflictingBenefits.Count > 0;

        private static bool LooksLikeDelegatedTerms(string lower)
            => ContainsAny(
                lower,
                "you choose",
                "standard offer",
                "whatever you think"
            );

        private static bool LooksLikeActivate(string lower)
            => ContainsAny(
                lower,
                "activate",
                "make live",
                "make it live",
                "make it active",
                "issue now",
                "issue it now"
            );

        private static AssistantOfferPathTermsState? Clone(AssistantOfferPathTermsState? prior)
        {
            if (prior is null)
            {
                return null;
            }

            return new AssistantOfferPathTermsState
            {
                OfferType = prior.OfferType,
                DiscountPercentage = prior.DiscountPercentage,
                DiscountAmount = prior.DiscountAmount,
                FreeItemText = prior.FreeItemText,
                PurchaseRequirement = prior.PurchaseRequirement,
                MinimumSpend = prior.MinimumSpend,
                ReplacementItemText = prior.ReplacementItemText,
                Validity = prior.Validity,
                ExpiryDate = prior.ExpiryDate,
                Title = prior.Title,
                Description = prior.Description,
                OperatorDelegatedTerms = prior.OperatorDelegatedTerms,
                ConflictingBenefits = [.. prior.ConflictingBenefits],
                WantsActivate = prior.WantsActivate,
            };
        }

        private static bool ContainsAny(string haystack, params string[] needles)
            => needles.Any(needle => haystack.Contains(needle, StringComparison.Ordinal));

        private static string Clean(string value)
            => value.Trim().TrimEnd('.', ',', ';');

        [GeneratedRegex(@"(?<value>\d+(?:\.\d+)?)\s*%")]
        private static partial Regex PercentRegex();

        [GeneratedRegex(@"£\s*(?<value>\d+(?:\.\d+)?)")]
        private static partial Regex PoundsRegex();

        [GeneratedRegex(@"(?<value>\d+(?:\.\d+)?)")]
        private static partial Regex NumberRegex();

        [GeneratedRegex(
            @"(?:(?:free item|item)\s*(?:is|:)\s*|(?:complimentary|give them a free|free(?!\s+item))\s+)(?<item>[^,;\n]+)",
            RegexOptions.IgnoreCase
        )]
        private static partial Regex FreeItemRegex();

        [GeneratedRegex(
            @"(?:replacement item|replace)\s*(?:is|:)?\s*(?<item>[^,;\n]+)",
            RegexOptions.IgnoreCase
        )]
        private static partial Regex ReplacementItemRegex();
    }
}
