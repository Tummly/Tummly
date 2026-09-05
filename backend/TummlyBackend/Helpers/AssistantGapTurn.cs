using System.Text.Json;

namespace TummlyBackend.Helpers
{
    public sealed class AssistantGapState
    {
        public string Target { get; set; } = AssistantGapTurn.Target;
        public string Kind { get; set; } = string.Empty;
        public string AssistantTask { get; set; } = string.Empty;
        public List<string> Options { get; set; } = [];
        public string SourceUserMessage { get; set; } = string.Empty;
        public string? LocationKind { get; set; }
        public string? OfferTermsJson { get; set; }
        public List<string> OpenRules { get; set; } = [];

        /// <summary>
        /// <see cref="AssistantGapTurn.GapKindCreation"/> (default) or
        /// <see cref="AssistantGapTurn.GapKindAdvisory"/>. Missing JSON
        /// values stay creation for back-compat.
        /// </summary>
        public string GapKind { get; set; } = AssistantGapTurn.GapKindCreation;

        public string? PartialDiagnosisNote { get; set; }

        public string? ConversationTurnId { get; set; }

        public string? AdvisoryReason { get; set; }

        /// <summary>
        /// Logging only: <see cref="AssistantGapTurn.GapSourcePreCheck"/> or
        /// <see cref="AssistantGapTurn.GapSourceModelRequested"/>.
        /// </summary>
        public string? GapSource { get; set; }
    }

    public static class AssistantGapTurn
    {
        public const string Target = "gap";
        public const string KindCreateTarget = "create-target";
        public const string KindLocation = "location";
        public const string KindOffer = "offer-title";
        public const string KindAudience = "audience";
        public const string KindChannel = "channel";
        public const string KindOfferTerms = "offer-terms";
        public const string KindCampaignTitle = "campaign-title";
        public const string KindFeedback = "feedback";

        public const string GapKindCreation = "creation";
        public const string GapKindAdvisory = "advisory";
        public const string GapSourcePreCheck = "pre-check";
        public const string GapSourceModelRequested = "model-requested";
        public const string KindAdvisoryScope = "advisory-scope";
        public const string KindAdvisoryRange = "advisory-range";
        public const string KindAdvisoryMetric = "advisory-metric";
        public const string KindAdvisoryData = "advisory-data";
        public const string KindAdvisoryModel = "advisory-model";

        public static AssistantGapState CreateTarget(
            IReadOnlyList<string> options,
            string sourceUserMessage,
            string assistantTask
        )
            => new()
            {
                Kind = KindCreateTarget,
                AssistantTask = assistantTask,
                Options = options.Distinct(StringComparer.Ordinal).ToList(),
                SourceUserMessage = sourceUserMessage,
            };

        public static AssistantGapState CreateLocation(
            string locationKind,
            IReadOnlyList<string> options,
            string sourceUserMessage,
            string assistantTask,
            string? offerTermsJson = null
        )
            => new()
            {
                Kind = KindLocation,
                LocationKind = locationKind,
                AssistantTask = assistantTask,
                Options = options.ToList(),
                SourceUserMessage = sourceUserMessage,
                OfferTermsJson = offerTermsJson,
            };

        public static AssistantGapState CreateOfferTerms(
            string sourceUserMessage,
            IReadOnlyList<string> openRules,
            string assistantTask = AssistantTask.OfferPath
        )
            => new()
            {
                Kind = KindOfferTerms,
                AssistantTask = assistantTask,
                SourceUserMessage = sourceUserMessage,
                OpenRules = openRules.ToList(),
            };

        public static AssistantGapState CreateCombinedOfferTerms(
            string sourceUserMessage,
            AssistantOfferPathTermsState terms,
            string assistantTask
        )
            => new()
            {
                Kind = KindOfferTerms,
                AssistantTask = assistantTask,
                SourceUserMessage = sourceUserMessage,
                OpenRules = AssistantOfferPathTerms.OpenRuleNames(terms).ToList(),
                OfferTermsJson = AssistantOfferPathTerms.Serialize(terms),
            };

        public static AssistantGapState CreateCampaignTitle(
            IReadOnlyList<string> options,
            string sourceUserMessage,
            string assistantTask,
            string? offerTermsJson = null
        )
            => new()
            {
                Kind = KindCampaignTitle,
                AssistantTask = assistantTask,
                Options = options.ToList(),
                SourceUserMessage = sourceUserMessage,
                OfferTermsJson = offerTermsJson,
            };

        public static AssistantGapState CreateOffer(
            IReadOnlyList<string> options,
            string sourceUserMessage,
            string assistantTask
        )
            => CreateNamed(KindOffer, options, sourceUserMessage, assistantTask);

        public static AssistantGapState CreateAudience(
            IReadOnlyList<string> options,
            string sourceUserMessage,
            string assistantTask
        )
            => CreateNamed(KindAudience, options, sourceUserMessage, assistantTask);

        public static AssistantGapState CreateChannel(
            IReadOnlyList<string> options,
            string sourceUserMessage,
            string assistantTask
        )
            => CreateNamed(KindChannel, options, sourceUserMessage, assistantTask);

        public static AssistantGapState CreateBindKind(
            string kind,
            IReadOnlyList<string> options,
            string sourceUserMessage,
            string assistantTask
        )
            => kind switch
            {
                KindOffer => CreateOffer(options, sourceUserMessage, assistantTask),
                KindAudience => CreateAudience(options, sourceUserMessage, assistantTask),
                KindChannel => CreateChannel(options, sourceUserMessage, assistantTask),
                _ => throw new InvalidOperationException(
                    $"Unknown bind Gap turn kind: {kind}"
                ),
            };

        public static AssistantGapState CreateFeedback(
            IReadOnlyList<string> options,
            string sourceUserMessage
        )
            => new()
            {
                Kind = KindFeedback,
                GapKind = GapKindCreation,
                AssistantTask = AssistantTask.RecoveryPath,
                Options = options.ToList(),
                SourceUserMessage = sourceUserMessage,
            };

        public static AssistantGapState CreateAdvisory(
            AdvisoryGap gap,
            string sourceUserMessage,
            string gapSource = GapSourcePreCheck
        )
            => new()
            {
                Kind = KindForAdvisoryReason(gap.Reason),
                GapKind = GapKindAdvisory,
                AssistantTask = AssistantTask.Retrieve,
                Options = gap.CandidateOptions.ToList(),
                SourceUserMessage = sourceUserMessage,
                PartialDiagnosisNote = gap.PartialDiagnosisNote,
                ConversationTurnId = gap.ConversationTurnId,
                AdvisoryReason = gap.Reason.ToString(),
                GapSource = gapSource,
            };

        public static bool IsAdvisoryGap(AssistantGapState gapState)
            => string.Equals(
                gapState.GapKind,
                GapKindAdvisory,
                StringComparison.Ordinal
            );

        public static string AdvisoryGapBody(AssistantGapState gapState)
        {
            var reason = Enum.TryParse<AdvisoryGapReason>(
                gapState.AdvisoryReason,
                ignoreCase: true,
                out var parsed
            )
                ? parsed
                : AdvisoryGapReason.ModelRequested;
            return AssistantAdvisoryIntent.GapQuestionBody(
                new AdvisoryGap(
                    reason,
                    gapState.Options.ToArray(),
                    gapState.PartialDiagnosisNote,
                    gapState.ConversationTurnId ?? string.Empty
                )
            );
        }

        private static string KindForAdvisoryReason(AdvisoryGapReason reason)
            => reason switch
            {
                AdvisoryGapReason.ScopeUnresolved => KindAdvisoryScope,
                AdvisoryGapReason.RangeAmbiguous => KindAdvisoryRange,
                AdvisoryGapReason.MetricAmbiguous => KindAdvisoryMetric,
                AdvisoryGapReason.InsufficientData => KindAdvisoryData,
                AdvisoryGapReason.ModelRequested => KindAdvisoryModel,
                _ => KindAdvisoryModel,
            };

        private static AssistantGapState CreateNamed(
            string kind,
            IReadOnlyList<string> options,
            string sourceUserMessage,
            string assistantTask
        )
            => new()
            {
                Kind = kind,
                GapKind = GapKindCreation,
                AssistantTask = assistantTask,
                Options = options.ToList(),
                SourceUserMessage = sourceUserMessage,
            };

        public static bool IsBindKind(string kind)
            => kind is KindOffer or KindAudience or KindChannel;

        public static bool IsOfferPathGap(AssistantGapState gapState)
            => string.Equals(
                    gapState.AssistantTask,
                    AssistantTask.OfferPath,
                    StringComparison.Ordinal
                )
                && gapState.Kind is KindOfferTerms or KindLocation;

        private static bool IsKnownKind(string kind)
            => kind is KindCreateTarget or KindLocation
                or KindOffer or KindAudience or KindChannel
                or KindOfferTerms or KindCampaignTitle or KindFeedback
                or KindAdvisoryScope or KindAdvisoryRange or KindAdvisoryMetric
                or KindAdvisoryData or KindAdvisoryModel;

        public static AssistantGapState? Parse(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                var state = JsonSerializer.Deserialize<AssistantGapState>(json);
                if (state is null
                    || !string.Equals(state.Target, Target, StringComparison.Ordinal)
                    || !IsKnownKind(state.Kind))
                {
                    return null;
                }

                if (string.IsNullOrWhiteSpace(state.GapKind))
                {
                    state.GapKind = GapKindCreation;
                }

                return state;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        public static string Serialize(AssistantGapState state)
            => JsonSerializer.Serialize(state);

        public static string CreateTargetBody(IReadOnlyList<string> options)
        {
            var labels = options.Count == 0
                ? AssistantCreateTargets.UnnamedOptions
                : options;
            return $"{AssistantGapAsk.CreateTargetAskPrefix} {AssistantCreateLocationGap.Join(labels)}?";
        }

        public static bool LooksLikeContinueAnswer(string message)
        {
            var normalized = message.Trim().Trim('.', ',', ';', ':').ToLowerInvariant();
            return normalized is "ok"
                or "okay"
                or "yes"
                or "y"
                or "yep"
                or "sure"
                or "continue"
                or "go ahead"
                or "proceed";
        }

        public static string RepeatLocationBody(AssistantGapState state)
            => AssistantCreateLocationGap.RepeatBody(
                state.LocationKind,
                state.Options,
                LocationDraftNoun(state.AssistantTask)
            );

        public static string RepeatBindBody(AssistantGapState state)
            => AssistantGapAsk.ExplainBind(state.Kind, state.Options);

        public static string LocationDraftNoun(string? assistantTask)
            => string.Equals(
                assistantTask,
                AssistantTask.OfferPath,
                StringComparison.Ordinal
            )
                ? "Offers catalog Draft"
                : "Campaign Draft";
    }
}
