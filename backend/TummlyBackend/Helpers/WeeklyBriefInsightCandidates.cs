using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Deterministic Weekly brief insight candidate emitter (locks 02 / 04 / 06).
    /// Pure: no DB, no model. Cap and priority are server-side.
    /// </summary>
    public static class WeeklyBriefInsightCandidates
    {
        public const string ThresholdVersion = "wb-insight-2026-10-a";
        public const int Cap = 5;
        public const int MeaningfulChangePercentMin = 20;
        public const int MeaningfulChangePriorFloor = 5;
        public const int EmergingThemeMinCount = 3;
        public const double EmergingThemeMinShare = 0.25;
        public const int EmergingThemePriorLift = 2;

        public const string TypeMeaningfulChange = "meaningful-change";
        public const string TypeControlSignal = "control-signal";
        public const string TypeEmergingTheme = "emerging-theme";
        public const string TypeFunnelDrop = "funnel-drop";
        public const string TypeSustainedTrend = "sustained-trend";
        public const string TypeDataQualityIssue = "data-quality-issue";
        public const string TypeNoMaterialChange = "no-material-change";

        public const string ActionFeedbackNeedsAttention = "feedback-needs-attention";
        public const string ActionUnderperformQr = "underperform-qr";
        public const string ActionRepeatedInvalid = "repeated-invalid";
        public const string ActionLowRedemption = "low-redemption";

        public const string MetricQrScanEvents = "qrScanEvents";
        public const string MetricFeedbackCount = "feedbackCount";
        public const string MetricGuestsJoined = "guestsJoined";
        public const string MetricRedemptionsInWeek = "redemptionsInWeek";
        public const string MetricUnsubscribesInWeek = "unsubscribesInWeek";

        private static readonly string[] WowMetricKeys =
        [
            MetricQrScanEvents,
            MetricFeedbackCount,
            MetricGuestsJoined,
            MetricRedemptionsInWeek,
            MetricUnsubscribesInWeek,
        ];

        private static readonly string[] ControlSignalPriority =
        [
            ActionFeedbackNeedsAttention,
            ActionUnderperformQr,
            ActionRepeatedInvalid,
            ActionLowRedemption,
        ];

        private static readonly string[] TypePriority =
        [
            TypeControlSignal,
            TypeDataQualityIssue,
            TypeMeaningfulChange,
            TypeFunnelDrop,
            TypeEmergingTheme,
            TypeSustainedTrend,
        ];

        /// <summary>
        /// Emit the capped candidate bag from metrics + resolved control signals.
        /// </summary>
        /// <param name="prior">
        /// Prior closed-week metrics, or null when the prior window is unavailable.
        /// </param>
        /// <param name="priorPrior">
        /// Week before prior; required for <c>sustained-trend</c>.
        /// </param>
        public static WeeklyBriefInsightCandidateBag Emit(
            WeeklyBriefMetrics current,
            WeeklyBriefMetrics? prior,
            WeeklyBriefMetrics? priorPrior,
            IReadOnlyList<WeeklyBriefControlSignalInput> controlSignals,
            bool dataQualityIssue
        )
        {
            var raw = new List<WeeklyBriefInsightCandidate>();

            AppendControlSignals(raw, controlSignals);
            if (dataQualityIssue)
            {
                raw.Add(DataQualityCandidate(current));
            }

            if (prior is not null)
            {
                AppendMeaningfulChanges(raw, current, prior);
            }

            AppendFunnelDropAlias(raw, controlSignals);
            AppendEmergingTheme(raw, current, prior);

            if (prior is not null && priorPrior is not null)
            {
                AppendSustainedTrends(raw, current, prior, priorPrior);
            }

            if (raw.Count == 0)
            {
                return new WeeklyBriefInsightCandidateBag(
                    ThresholdVersion,
                    [NoMaterialChangeCandidate(current)]
                );
            }

            var capped = raw
                .OrderBy(c => TypeRank(c.Type))
                .ThenBy(c => ControlRank(c))
                .ThenBy(c => c.Id, StringComparer.Ordinal)
                .Take(Cap)
                .ToList();

            return new WeeklyBriefInsightCandidateBag(ThresholdVersion, capped);
        }

        private static void AppendControlSignals(
            List<WeeklyBriefInsightCandidate> raw,
            IReadOnlyList<WeeklyBriefControlSignalInput> controlSignals
        )
        {
            foreach (var kind in ControlSignalPriority)
            {
                var signal = controlSignals.FirstOrDefault(s =>
                    string.Equals(s.ActionKind, kind, StringComparison.Ordinal)
                );
                if (signal is null)
                {
                    continue;
                }

                raw.Add(
                    new WeeklyBriefInsightCandidate(
                        Id: $"{TypeControlSignal}:{kind}",
                        Type: TypeControlSignal,
                        Evidence: new WeeklyBriefInsightEvidence(
                            MetricKeys: signal.Snapshot.Keys.ToList(),
                            Snapshot: signal.Snapshot,
                            CausalEvidence: false
                        ),
                        ActionKind: kind
                    )
                );
            }
        }

        private static void AppendFunnelDropAlias(
            List<WeeklyBriefInsightCandidate> raw,
            IReadOnlyList<WeeklyBriefControlSignalInput> controlSignals
        )
        {
            var underperform = controlSignals.FirstOrDefault(s =>
                string.Equals(
                    s.ActionKind,
                    ActionUnderperformQr,
                    StringComparison.Ordinal
                )
            );
            if (underperform is null)
            {
                return;
            }

            raw.Add(
                new WeeklyBriefInsightCandidate(
                    Id: $"{TypeFunnelDrop}:{ActionUnderperformQr}",
                    Type: TypeFunnelDrop,
                    Evidence: new WeeklyBriefInsightEvidence(
                        MetricKeys: underperform.Snapshot.Keys.ToList(),
                        Snapshot: underperform.Snapshot,
                        CausalEvidence: false
                    ),
                    ActionKind: ActionUnderperformQr
                )
            );
        }

        private static void AppendMeaningfulChanges(
            List<WeeklyBriefInsightCandidate> raw,
            WeeklyBriefMetrics current,
            WeeklyBriefMetrics prior
        )
        {
            foreach (var key in WowMetricKeys)
            {
                if (!TryMeaningfulChange(current, prior, key, out var candidate))
                {
                    continue;
                }

                raw.Add(candidate);
            }
        }

        private static void AppendSustainedTrends(
            List<WeeklyBriefInsightCandidate> raw,
            WeeklyBriefMetrics current,
            WeeklyBriefMetrics prior,
            WeeklyBriefMetrics priorPrior
        )
        {
            foreach (var key in WowMetricKeys)
            {
                if (
                    !TryWowMateriality(
                        MetricValue(current, key),
                        MetricValue(prior, key),
                        out var currentDirection,
                        out _
                    )
                    || !TryWowMateriality(
                        MetricValue(prior, key),
                        MetricValue(priorPrior, key),
                        out var priorDirection,
                        out _
                    )
                )
                {
                    continue;
                }

                if (
                    !string.Equals(
                        currentDirection,
                        priorDirection,
                        StringComparison.Ordinal
                    )
                )
                {
                    continue;
                }

                var cur = MetricValue(current, key);
                var prev = MetricValue(prior, key);
                var prevPrev = MetricValue(priorPrior, key);
                raw.Add(
                    new WeeklyBriefInsightCandidate(
                        Id: $"{TypeSustainedTrend}:{key}",
                        Type: TypeSustainedTrend,
                        Evidence: SnapshotEvidence(
                            key,
                            ("current", cur),
                            ("prior", prev),
                            ("priorPrior", prevPrev),
                            ("direction", currentDirection)
                        ),
                        MetricKey: key,
                        Direction: currentDirection
                    )
                );
            }
        }

        private static void AppendEmergingTheme(
            List<WeeklyBriefInsightCandidate> raw,
            WeeklyBriefMetrics current,
            WeeklyBriefMetrics? prior
        )
        {
            if (current.DetectedTagCounts.Count == 0)
            {
                return;
            }

            var top = current.DetectedTagCounts
                .OrderByDescending(pair => pair.Value)
                .ThenBy(pair => pair.Key, StringComparer.Ordinal)
                .First();

            var share =
                current.FeedbackCount <= 0
                    ? 0d
                    : (double)top.Value / current.FeedbackCount;

            var absoluteClears =
                top.Value >= EmergingThemeMinCount
                && share >= EmergingThemeMinShare;

            var priorCount = 0;
            var priorClears = false;
            if (
                prior is not null
                && prior.DetectedTagCounts.TryGetValue(top.Key, out priorCount)
            )
            {
                priorClears = top.Value >= priorCount + EmergingThemePriorLift;
            }
            else if (prior is not null)
            {
                priorClears = top.Value >= EmergingThemePriorLift;
            }

            if (!absoluteClears && !priorClears)
            {
                return;
            }

            raw.Add(
                new WeeklyBriefInsightCandidate(
                    Id: $"{TypeEmergingTheme}:{top.Key}",
                    Type: TypeEmergingTheme,
                    Evidence: SnapshotEvidence(
                        "detectedTagCount",
                        ("tag", top.Key),
                        ("count", top.Value),
                        ("feedbackCount", current.FeedbackCount),
                        ("share", Math.Round(share, 4)),
                        ("priorCount", priorCount)
                    ),
                    ThemeLabel: top.Key
                )
            );
        }

        private static bool TryMeaningfulChange(
            WeeklyBriefMetrics current,
            WeeklyBriefMetrics prior,
            string key,
            out WeeklyBriefInsightCandidate candidate
        )
        {
            candidate = null!;
            var cur = MetricValue(current, key);
            var prev = MetricValue(prior, key);
            if (!TryWowMateriality(cur, prev, out var direction, out var change))
            {
                return false;
            }

            var pairs = new List<(string Key, object Value)>
            {
                ("current", cur),
                ("prior", prev),
                ("changeKind", change.Kind),
                ("direction", direction),
            };
            if (change.DeltaPercent is int delta)
            {
                pairs.Add(("deltaPercent", delta));
            }

            candidate = new WeeklyBriefInsightCandidate(
                Id: $"{TypeMeaningfulChange}:{key}",
                Type: TypeMeaningfulChange,
                Evidence: SnapshotEvidence(key, pairs.ToArray()),
                MetricKey: key,
                ChangeKind: change.Kind,
                DeltaPercent: change.DeltaPercent,
                Direction: direction
            );
            return true;
        }

        /// <summary>
        /// Lock 04 materiality for one metric pair.
        /// </summary>
        public static bool TryWowMateriality(
            int current,
            int prior,
            out string direction,
            out (string Kind, int? DeltaPercent) change
        )
        {
            direction = current >= prior ? "up" : "down";
            change = default;

            if (prior == 0)
            {
                if (current <= 0)
                {
                    return false;
                }

                change = ("new", null);
                direction = "up";
                return true;
            }

            if (prior < MeaningfulChangePriorFloor)
            {
                return false;
            }

            var deltaPercent = (int)Math.Round(
                100d * (current - prior) / prior,
                MidpointRounding.AwayFromZero
            );
            if (Math.Abs(deltaPercent) < MeaningfulChangePercentMin)
            {
                return false;
            }

            change = ("pct", deltaPercent);
            direction = deltaPercent >= 0 ? "up" : "down";
            return true;
        }

        private static WeeklyBriefInsightCandidate DataQualityCandidate(
            WeeklyBriefMetrics current
        )
            => new(
                Id: TypeDataQualityIssue,
                Type: TypeDataQualityIssue,
                Evidence: SnapshotEvidence(
                    "activityScore",
                    ("activityScore", WeeklyBriefPhase1Meta.ActivityScore(current)),
                    ("guestsJoined", current.GuestsJoined),
                    ("qrScanEvents", current.QrScanEvents),
                    ("feedbackCount", current.FeedbackCount)
                )
            );

        private static WeeklyBriefInsightCandidate NoMaterialChangeCandidate(
            WeeklyBriefMetrics current
        )
            => new(
                Id: TypeNoMaterialChange,
                Type: TypeNoMaterialChange,
                Evidence: SnapshotEvidence(
                    "activityScore",
                    ("activityScore", WeeklyBriefPhase1Meta.ActivityScore(current))
                )
            );

        private static WeeklyBriefInsightEvidence SnapshotEvidence(
            string primaryKey,
            params (string Key, object Value)[] pairs
        )
        {
            var snapshot = new Dictionary<string, object>(StringComparer.Ordinal);
            foreach (var (key, value) in pairs)
            {
                snapshot[key] = value;
            }

            return new WeeklyBriefInsightEvidence(
                MetricKeys: [primaryKey],
                Snapshot: snapshot,
                CausalEvidence: false
            );
        }

        private static int MetricValue(WeeklyBriefMetrics metrics, string key)
            => key switch
            {
                MetricQrScanEvents => metrics.QrScanEvents,
                MetricFeedbackCount => metrics.FeedbackCount,
                MetricGuestsJoined => metrics.GuestsJoined,
                MetricRedemptionsInWeek => metrics.RedemptionsInWeek,
                MetricUnsubscribesInWeek => metrics.UnsubscribesInWeek,
                _ => throw new ArgumentOutOfRangeException(nameof(key), key, null),
            };

        private static int TypeRank(string type)
        {
            var index = Array.IndexOf(TypePriority, type);
            return index < 0 ? int.MaxValue : index;
        }

        private static int ControlRank(WeeklyBriefInsightCandidate candidate)
        {
            if (
                candidate.Type != TypeControlSignal
                && candidate.Type != TypeFunnelDrop
            )
            {
                return 0;
            }

            var index = Array.IndexOf(
                ControlSignalPriority,
                candidate.ActionKind ?? string.Empty
            );
            return index < 0 ? int.MaxValue : index;
        }

        /// <summary>
        /// True when phase-1 confidence is low for metrics-shaped domain flags.
        /// </summary>
        public static bool IsDataQualityIssue(WeeklyBriefMetrics metrics)
        {
            var body = MetricsShapedBody(metrics);
            var (level, _) = WeeklyBriefPhase1Meta.ResolveConfidence(body, metrics);
            return string.Equals(level, "low", StringComparison.Ordinal);
        }

        private static WeeklyBriefBody MetricsShapedBody(WeeklyBriefMetrics metrics)
        {
            var captureHasData =
                metrics.GuestsJoined > 0 || metrics.QrScanEvents > 0;
            var feedbackHasData = metrics.FeedbackCount > 0;
            var offersHasData =
                metrics.ActiveOffers > 0
                || metrics.ClaimsInWeek > 0
                || metrics.RedemptionsInWeek > 0;
            var campaignsHasData =
                metrics.CampaignsSentInWeek > 0
                || metrics.CampaignRecipientsReached > 0;

            return new WeeklyBriefBody(
                Headline: "insight-candidate-confidence",
                Capture: new WeeklyBriefSection(captureHasData, "x", null),
                Feedback: new WeeklyBriefSection(feedbackHasData, "x", null),
                Offers: new WeeklyBriefSection(offersHasData, "x", null),
                Campaigns: new WeeklyBriefSection(campaignsHasData, "x", null),
                WatchNext: []
            );
        }
    }
}
