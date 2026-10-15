using System.Globalization;
using System.Text.RegularExpressions;

namespace TummlyBackend.Helpers
{
    public sealed record AssistantCatalogOfferRef(
        int Id,
        string Title,
        string Status,
        bool Attachable,
        decimal? DiscountPercentage,
        decimal? DiscountAmount,
        string? FreeItemText
    );

    public sealed record AssistantCampaignTemplateRef(string Id, string Title);

    public sealed record AssistantCampaignDraftBindFields(
        string Channel,
        string ChannelLabel,
        string AudienceKey,
        string AudienceLabel,
        string GoalId,
        string? TemplateId,
        string Name,
        string OfferStance,
        int? OfferId,
        string OfferLabel,
        string? OfferNote,
        string? LimitNote = null,
        string? AudienceRequest = null
    );

    public sealed record AssistantCampaignDraftBindChoice(
        string? OfferTitle = null,
        string? AudienceLabel = null,
        string? ChannelLabel = null,
        string? CampaignTitle = null
    )
    {
        public static readonly AssistantCampaignDraftBindChoice Empty = new();

        public bool HasValue
            => OfferTitle is not null
                || AudienceLabel is not null
                || ChannelLabel is not null
                || CampaignTitle is not null;

        public static AssistantCampaignDraftBindChoice FromGapKind(
            string kind,
            string value
        )
            => kind switch
            {
                AssistantGapTurn.KindOffer => new(OfferTitle: value),
                AssistantGapTurn.KindAudience => new(AudienceLabel: value),
                AssistantGapTurn.KindChannel => new(ChannelLabel: value),
                AssistantGapTurn.KindCampaignTitle => new(CampaignTitle: value),
                _ => Empty,
            };
    }

    public abstract record AssistantCampaignDraftBindOutcome
    {
        public sealed record Bound(AssistantCampaignDraftBindFields Fields)
            : AssistantCampaignDraftBindOutcome;

        public sealed record Gap(
            string Kind,
            IReadOnlyList<string> Options,
            string Body
        ) : AssistantCampaignDraftBindOutcome;

        public sealed record UnevaluableAudience(string Body)
            : AssistantCampaignDraftBindOutcome;
    }

    /// <summary>
    /// Server bind for a Create Campaign Draft ask: audience, channel, goal,
    /// Offer attach, template, and generated name. Clash kinds are Gap turns.
    /// </summary>
    public static partial class AssistantCampaignDraftBind
    {
        public const string AudienceAllEligible = "all-eligible-guests";
        public const string AudienceNewGuests = "new-guests";
        public const string AudiencePositive = "positive-feedback";
        public const string AudienceNegative = "negative-feedback";
        public const string AudienceOfferNotRedeemed = "offer-not-redeemed";
        public const string AudienceRecentRedeemers = "recent-redeemers";
        public const string AudienceNoRecentActivity = "no-recent-tummly-activity";
        public const string AudienceDormant = "dormant-guests";
        public const string AudienceRecovery = "completed-recovery-follow-up";

        public static readonly IReadOnlyDictionary<string, string> AudienceLabels =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [AudienceAllEligible] = "All eligible guests",
                [AudienceNewGuests] = "New guests",
                [AudiencePositive] = "Positive feedback",
                [AudienceNegative] = "Negative feedback",
                [AudienceOfferNotRedeemed] = "Offer not redeemed",
                [AudienceRecentRedeemers] = "Recent redeemers",
                [AudienceNoRecentActivity] = "No recent Tummly activity",
                [AudienceDormant] = "Dormant guests",
                [AudienceRecovery] = "Completed recovery follow-up",
            };

        private static readonly IReadOnlyDictionary<string, string[]> AudienceNeedles =
            new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                [AudienceNewGuests] =
                    ["new guests", "new guest", "first-time", "first time"],
                [AudiencePositive] = ["positive", "happy"],
                [AudienceNegative] =
                [
                    "negative feedback",
                    "poor feedback",
                    "unhappy",
                    "submitted negative",
                ],
                [AudienceOfferNotRedeemed] =
                [
                    "offer not redeemed",
                    "offer-not-redeemed",
                    "unredeemed offer",
                    "unredeemed",
                    "haven't redeemed",
                    "have not redeemed",
                    "did not redeem",
                    "not redeemed",
                ],
                [AudienceRecentRedeemers] =
                [
                    "recent redeemers",
                    "recent-redeemers",
                    "recently redeemed",
                    "recent redeemer",
                ],
                [AudienceNoRecentActivity] =
                [
                    "no recent tummly activity",
                    "no-recent-tummly-activity",
                    "no recent activity",
                ],
                [AudienceDormant] =
                [
                    "dormant",
                    "lapsed",
                    "90 days",
                    "90-day",
                    "90 day",
                    "three months",
                    "3 months",
                    "haven't seen",
                    "have not seen",
                    "haven't visited",
                    "have not visited",
                ],
                [AudienceRecovery] =
                [
                    "completed recovery follow-up",
                    "completed recovery",
                    "recovery follow-up",
                ],
            };

        private static readonly HashSet<string> ReservedWords =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "all", "eligible", "guests", "guest", "new", "positive", "negative",
                "feedback", "dormant", "recent", "redeemers", "unhappy", "email", "sms",
                "campaign", "draft", "recovery", "camden", "brixton", "shoreditch", "soho",
                "clapham", "angel", "tummly", "offer", "offers", "not", "redeemed", "activity",
                "everyone", "people", "diners", "customers", "most", "latest", "first",
                "please", "edit", "change", "send", "to", "for", "only", "the", "and",
                "with", "without", "excluding", "except", "bring", "back", "inactive", "boost",
                "thank", "quiet", "time", "winback", "win-back", "update", "save",
                "from", "since", "before", "after", "yesterday", "today"
            };

        public static AssistantCampaignDraftBindOutcome Resolve(
            string userMessage,
            string locationName,
            IReadOnlyList<AssistantCatalogOfferRef> locationOffers,
            IReadOnlyList<AssistantCampaignTemplateRef> templates,
            AssistantCampaignDraftBindChoice? choice = null,
            IReadOnlyList<AssistantCatalogOfferRef>? otherLocationOffers = null
        )
        {
            var text = userMessage.Trim();
            var lower = text.ToLowerInvariant();
            var bindChoice = choice ?? AssistantCampaignDraftBindChoice.Empty;

            if (IsUnevaluableAudience(lower) && bindChoice.AudienceLabel is null)
            {
                return new AssistantCampaignDraftBindOutcome.UnevaluableAudience(
                    UnevaluableAudienceBody()
                );
            }

            var offer = ResolveOffer(
                text,
                locationOffers,
                bindChoice.OfferTitle,
                otherLocationOffers
            );
            if (offer.ClashTitles is { } offerClash && bindChoice.OfferTitle is null)
            {
                return new AssistantCampaignDraftBindOutcome.Gap(
                    AssistantGapTurn.KindOffer,
                    offerClash,
                    OfferClashBody(offerClash)
                );
            }

            // Specific named-guest selection: e.g. "Send to Mohamed and Sophie only"
            if (TryExtractNamedGuests(text, out var namedGuests))
            {
                var channel = ResolveChannel(lower, bindChoice.ChannelLabel);
                if (channel.Clash && bindChoice.ChannelLabel is null)
                {
                    return new AssistantCampaignDraftBindOutcome.Gap(
                        AssistantGapTurn.KindChannel,
                        ["Email", "SMS"],
                        ChannelClashBody()
                    );
                }

                var goalId = InferGoal(lower);
                var templateId = ResolveTemplateId(text, templates);
                var audienceLabel = $"Named guests: {string.Join(", ", namedGuests)}";
                var limitNote = $"The audience is limited to specific named guests: {string.Join(", ", namedGuests)}.";
                var name = AssistantCampaignDraftName.Compose(
                    goalId,
                    channel.Id,
                    locationName,
                    AudienceAllEligible
                );

                return new AssistantCampaignDraftBindOutcome.Bound(
                    new AssistantCampaignDraftBindFields(
                        Channel: channel.Id,
                        ChannelLabel: channel.Label,
                        AudienceKey: AudienceAllEligible,
                        AudienceLabel: audienceLabel,
                        GoalId: goalId,
                        TemplateId: templateId,
                        Name: name,
                        OfferStance: offer.Stance,
                        OfferId: offer.OfferId,
                        OfferLabel: offer.Label,
                        OfferNote: offer.Note,
                        LimitNote: limitNote,
                        AudienceRequest: text
                    )
                );
            }

            // A targeted multi-guest request must not become all eligible when
            // the name parser cannot bind its complete selection.
            if (bindChoice.AudienceLabel is null
                && ResolveAudiences(lower, null).Count == 0
                && Regex.IsMatch(text, @"\b(?:campaign|capaign|send)\b.*\b(?:for|to)\b.+(?:\band\b|&|,).+",
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            {
                return new AssistantCampaignDraftBindOutcome.Gap(
                    AssistantGapTurn.KindAudience, [],
                    "Which guests should receive this Campaign? Enter their exact names. I have not selected all eligible guests.");
            }

            var audiences = ResolveAudiences(lower, bindChoice.AudienceLabel);
            if (audiences.Count >= 2 && bindChoice.AudienceLabel is null)
            {
                var labels = audiences
                    .Select(key => AudienceLabels[key])
                    .ToList();
                return new AssistantCampaignDraftBindOutcome.Gap(
                    AssistantGapTurn.KindAudience,
                    labels,
                    AudienceClashBody(labels)
                );
            }

            var guestCap = TryReadGuestCap(lower);
            if (guestCap is int cap)
            {
                // Explicit selection rule specified (e.g. "10 most recent", "10 newest", or chosen from Gap)
                if (TryResolveSelectionRule(lower, cap, bindChoice.AudienceLabel, out var ruleKey, out var ruleLabel))
                {
                    var channel = ResolveChannel(lower, bindChoice.ChannelLabel);
                    if (channel.Clash && bindChoice.ChannelLabel is null)
                    {
                        return new AssistantCampaignDraftBindOutcome.Gap(
                            AssistantGapTurn.KindChannel,
                            ["Email", "SMS"],
                            ChannelClashBody()
                        );
                    }

                    var goalId = InferGoal(lower);
                    var templateId = ResolveTemplateId(text, templates);
                    var audienceKey = audiences.Count == 1 ? audiences[0] : AudienceAllEligible;
                    var name = AssistantCampaignDraftName.Compose(
                        goalId,
                        channel.Id,
                        locationName,
                        audienceKey
                    );
                    var limitNote = $"The audience is limited to {ruleLabel.ToLowerInvariant()}.";

                    return new AssistantCampaignDraftBindOutcome.Bound(
                        new AssistantCampaignDraftBindFields(
                            Channel: channel.Id,
                            ChannelLabel: channel.Label,
                            AudienceKey: audienceKey,
                            AudienceLabel: ruleLabel,
                            GoalId: goalId,
                            TemplateId: templateId,
                            Name: name,
                            OfferStance: offer.Stance,
                            OfferId: offer.OfferId,
                            OfferLabel: offer.Label,
                            OfferNote: offer.Note,
                            LimitNote: limitNote,
                        AudienceRequest: text
                        )
                    );
                }

                // Count is ambiguous without selection rule: ask user which selection rule to use
                return new AssistantCampaignDraftBindOutcome.Gap(
                    AssistantGapTurn.KindAudience,
                    [
                        $"The {cap} most recent eligible guests",
                        $"The {cap} newest guests",
                    ],
                    $"Which {cap} guests should receive this Campaign? For example, the {cap} most recent eligible guests or the {cap} newest guests."
                );
            }

            var channelNormal = ResolveChannel(lower, bindChoice.ChannelLabel);
            if (channelNormal.Clash && bindChoice.ChannelLabel is null)
            {
                return new AssistantCampaignDraftBindOutcome.Gap(
                    AssistantGapTurn.KindChannel,
                    ["Email", "SMS"],
                    ChannelClashBody()
                );
            }

            TryExtractExclusions(text, out var exclusions);

            var normalGoalId = InferGoal(lower);
            var normalAudienceKey = audiences.Count == 1
                ? audiences[0]
                : AudienceForUnnamedGoal(normalGoalId);
            var normalAudienceLabel = AudienceLabels[normalAudienceKey];
            string? normalLimitNote = null;

            if (exclusions.Count > 0)
            {
                normalAudienceLabel = $"{normalAudienceLabel} (excluding {string.Join(", ", exclusions)})";
                normalLimitNote = $"Excluding: {string.Join(", ", exclusions)}.";
            }

            var normalTemplateId = ResolveTemplateId(text, templates);
            var normalName = AssistantCampaignDraftName.Compose(
                normalGoalId,
                channelNormal.Id,
                locationName,
                normalAudienceKey
            );

            return new AssistantCampaignDraftBindOutcome.Bound(
                new AssistantCampaignDraftBindFields(
                    Channel: channelNormal.Id,
                    ChannelLabel: channelNormal.Label,
                    AudienceKey: normalAudienceKey,
                    AudienceLabel: normalAudienceLabel,
                    GoalId: normalGoalId,
                    TemplateId: normalTemplateId,
                    Name: normalName,
                    OfferStance: offer.Stance,
                    OfferId: offer.OfferId,
                    OfferLabel: offer.Label,
                    OfferNote: offer.Note,
                    LimitNote: normalLimitNote,
                    AudienceRequest: text
                )
            );
        }

        public static IReadOnlyList<AssistantCatalogOfferRef> MatchAttachable(
            string userMessage,
            IReadOnlyList<AssistantCatalogOfferRef> locationOffers
        )
        {
            var titleMatches = MatchTitles(userMessage, locationOffers)
                .Where(offer => offer.Attachable)
                .ToList();
            if (titleMatches.Count > 0)
            {
                return titleMatches;
            }

            return MatchCommercialTerms(userMessage, locationOffers);
        }

        public static string OfferClashBody(IReadOnlyList<string> titles)
            => AssistantGapAsk.ForBind(AssistantGapTurn.KindOffer, titles);

        public static string AudienceClashBody(IReadOnlyList<string> labels)
            => AssistantGapAsk.ForBind(AssistantGapTurn.KindAudience, labels);

        public static string ChannelClashBody()
            => AssistantGapAsk.ChannelAsk;

        public static string UnevaluableAudienceBody()
            => "Counts are unavailable for this audience. I did not create a Draft.";

        public static string GuestCountIsNotAudienceBody(int count)
            => $"A count of {count} guests is not a Campaign audience. "
                + "I did not save a Campaign Draft. "
                + "Name the guests, for example guests with negative feedback, "
                + "new guests, or all eligible guests.";

        public static string GuestCountNotAppliedNote(int count)
            => $"I cannot limit this Draft to {count} guests. "
                + "The audience is every eligible guest in the selected audience.";

        /// <summary>
        /// A guest-count phrase such as "10 guests" or "10 Email guests".
        /// A percent, a price, or a day count does not match.
        /// </summary>
        public static bool NamesExplicitAudience(string message)
        {
            var lower = message.Trim().ToLowerInvariant();
            return ResolveAudiences(lower, null).Count > 0
                || NamesAllEligiblePhrase(lower)
                || TryExtractNamedGuests(message, out _)
                || TryReadGuestCap(lower) is not null
                || TryExtractExclusions(message, out _)
                || lower.Contains("saved audience")
                || lower.Contains("joined in the last");
        }

        public static bool NamesExplicitChannel(string message)
        {
            var named = NamedChannels(message.Trim().ToLowerInvariant());
            return named.Email || named.Sms;
        }

        public static bool LooksLikeShortenSubject(string message)
        {
            var lower = message.Trim().ToLowerInvariant();
            return lower.Contains("subject", StringComparison.Ordinal)
                && lower.Contains("short", StringComparison.Ordinal);
        }

        /// <summary>
        /// Keeps the first half of the words, and at least one fewer word.
        /// </summary>
        public static string ShortenSubject(string subject)
        {
            var words = subject.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length <= 1)
            {
                return subject.Trim();
            }

            var keep = Math.Max(1, words.Length / 2);
            if (keep >= words.Length)
            {
                keep = words.Length - 1;
            }

            return string.Join(' ', words.Take(keep));
        }

        public static bool LooksLikeContinueEarlier(string message)
        {
            var lower = message.Trim().ToLowerInvariant();
            return lower.Contains("already said", StringComparison.Ordinal)
                || lower.Contains("i told you", StringComparison.Ordinal)
                || lower.Contains("as i said", StringComparison.Ordinal)
                || lower.Contains("like i said", StringComparison.Ordinal)
                || lower.Contains("same as before", StringComparison.Ordinal);
        }

        public static int? TryReadGuestCap(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return null;
            }

            var match = GuestCapRegex().Match(message);
            if (!match.Success)
            {
                return null;
            }

            for (var i = 1; i < match.Groups.Count; i++)
            {
                if (match.Groups[i].Success
                    && int.TryParse(
                        match.Groups[i].Value,
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out var count
                    )
                    && count >= 1)
                {
                    return count;
                }
            }

            return null;
        }

        public static bool TryResolveSelectionRule(
            string lower,
            int count,
            string? chosenLabel,
            out string ruleKey,
            out string ruleLabel
        )
        {
            ruleKey = "most-recent";
            ruleLabel = $"The {count} most recent eligible guests";

            if (!string.IsNullOrWhiteSpace(chosenLabel))
            {
                if (chosenLabel.Contains("newest", StringComparison.OrdinalIgnoreCase))
                {
                    ruleKey = "newest";
                    ruleLabel = $"The {count} newest guests";
                    return true;
                }

                if (chosenLabel.Contains("recent", StringComparison.OrdinalIgnoreCase))
                {
                    ruleKey = "most-recent";
                    ruleLabel = $"The {count} most recent eligible guests";
                    return true;
                }

                if (chosenLabel.Contains(count.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal))
                {
                    ruleKey = "most-recent";
                    ruleLabel = chosenLabel.Trim();
                    return true;
                }
            }

            if (ContainsAny(lower, "newest", "recently joined", "new guests"))
            {
                ruleKey = "newest";
                ruleLabel = $"The {count} newest guests";
                return true;
            }

            if (ContainsAny(lower, "most recent", "recently visited", "latest", "recent"))
            {
                ruleKey = "most-recent";
                ruleLabel = $"The {count} most recent eligible guests";
                return true;
            }

            return false;
        }

        public static bool TryExtractNamedGuests(
            string message,
            out List<string> names
        )
        {
            names = [];
            if (string.IsNullOrWhiteSpace(message))
            {
                return false;
            }

            var patterns = new[]
            {
                @"^(?:please\s+)?(?:make|create|draft|prepare)\s+(?:me\s+)?(?:an?\s+)?(?:email\s+|sms\s+)?(?:campaign|capaign)(?:\s+draft)?\s+(?:to|for)\s+(?<who>[\p{L}\s,'\u2019&-]+?)(?:\s+only)?(?:\s+at\s+[\p{L}\s]+)?$",

                @"^(?:send\s+to|draft\s+(?:an?\s+)?(?:email\s+|sms\s+)?campaign\s+(?:to|for)|audience\s+(?:to|is)|change\s+(?:the\s+)?audience\s+to|to)\s+(?<who>[\p{L}\s,'\u2019&-]+?)(?:\s+only)?(?:\s+at\s+[\p{L}\s]+)?$",
                @"^(?<who>[\p{L}\s,'\u2019&-]+?)\s+only(?:\s+at\s+[\p{L}\s]+)?$",
                @"\b(?:send\s+to|audience\s+(?:to|is))\s+(?<who>[\p{L}\s,'\u2019&-]+?)(?:\s+only)?(?:\s+at\s+[\p{L}\s]+)?$"
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(
                    message.Trim().TrimEnd('.', '!', '?').TrimEnd(),
                    pattern,
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
                );
                if (!match.Success)
                {
                    continue;
                }

                var rawWho = match.Groups["who"].Value.Trim();
                if (string.IsNullOrWhiteSpace(rawWho))
                {
                    continue;
                }

                var parts = Regex.Split(rawWho, @"\s+(?:and|&)\s+|,", RegexOptions.IgnoreCase)
                    .Select(part => part.Trim()).Where(part => part.Length > 0).ToArray();

                var candidateNames = new List<string>();
                var allValid = parts.Length > 0;

                foreach (var part in parts)
                {
                    var cleaned = part.Trim();
                    if (cleaned.Length < 2 || cleaned.Length > 40)
                    {
                        allValid = false;
                        break;
                    }

                    var words = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (words.Any(w => ReservedWords.Contains(w)))
                    {
                        allValid = false;
                        break;
                    }

                    if (!words.All(w => w.All(c => char.IsLetter(c) || c == '\'' || c == '\u2019' || c == '-')))
                    {
                        allValid = false;
                        break;
                    }

                    candidateNames.Add(cleaned);
                }

                if (allValid && candidateNames.Count > 0)
                {
                    names = candidateNames;
                    return true;
                }
            }

            return false;
        }

        public static bool TryExtractExclusions(
            string message,
            out List<string> exclusions
        )
        {
            exclusions = [];
            if (string.IsNullOrWhiteSpace(message))
            {
                return false;
            }

            var match = Regex.Match(
                message.Trim().TrimEnd('.', '!', '?').TrimEnd(),
                @"\b(?:excluding|except|without)\s+(?<excluded>[\p{L}\s,'\u2019&-]+?)(?:\s+at\s+[\p{L}\s]+)?$",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
            );
            if (!match.Success)
            {
                return false;
            }

            var raw = match.Groups["excluded"].Value.Trim();
            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            var parts = Regex.Split(raw, @"\s+(?:and|&)\s+|,", RegexOptions.IgnoreCase)
                .Select(part => part.Trim()).Where(part => part.Length > 0);

            foreach (var part in parts)
            {
                var cleaned = part.Trim();
                if (cleaned.Length >= 2 && !exclusions.Contains(cleaned, StringComparer.OrdinalIgnoreCase))
                {
                    exclusions.Add(cleaned);
                }
            }

            return exclusions.Count > 0;
        }

        /// <summary>
        /// One channel named across the open task and earlier operator turns.
        /// Both channels, or neither channel, returns null.
        /// </summary>
        public static string? ResolveSingleChannelLabel(IEnumerable<string> texts)
        {
            var email = false;
            var sms = false;
            foreach (var text in texts)
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                var named = NamedChannels(text.ToLowerInvariant());
                email |= named.Email;
                sms |= named.Sms;
            }

            if (email == sms)
            {
                return null;
            }

            return email ? "Email" : "SMS";
        }

        public static string? ResolveNamedChoice(
            IReadOnlyList<string> options,
            string message
        )
        {
            var text = message.Trim();
            if (text.Length == 0)
            {
                return null;
            }

            var exact = options
                .Where(option =>
                    option.Equals(text, StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (exact.Count == 1)
            {
                return options.First(option =>
                    option.Equals(exact[0], StringComparison.OrdinalIgnoreCase));
            }

            var contained = UniqueNaturalMatches(options, text);
            if (contained.Count == 1)
            {
                return options.First(option =>
                    option.Equals(contained[0], StringComparison.OrdinalIgnoreCase));
            }

            if (contained.Count >= 2)
            {
                return null;
            }

            var normalized = text.Trim('.', ',', ';', ':').ToLowerInvariant();
            return AssistantGapOptionOrdinal.TryBind(options, normalized);
        }

        public static IReadOnlyList<string> UniqueNaturalMatches(
            IReadOnlyList<string> options,
            string message
        )
        {
            var text = message.Trim();
            if (text.Length == 0 || options.Count == 0)
            {
                return [];
            }

            var contained = options
                .Where(option =>
                    ContainsPhrase(text, option) || ContainsPhrase(option, text))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(option => option.Length)
                .ToList();
            if (contained.Count >= 2
                && contained[0].Length > contained[1].Length)
            {
                return [contained[0]];
            }

            return contained;
        }

        private static ChannelBind ResolveChannel(string lower, string? chosenLabel)
        {
            if (!string.IsNullOrWhiteSpace(chosenLabel))
            {
                return chosenLabel.Equals("SMS", StringComparison.OrdinalIgnoreCase)
                    ? new ChannelBind("sms", "SMS", false)
                    : new ChannelBind("email", "Email", false);
            }

            var named = NamedChannels(lower);
            var namesSms = named.Sms;
            var namesEmail = named.Email;

            if (namesSms && namesEmail)
            {
                return new ChannelBind("email", "Email", true);
            }

            if (namesSms)
            {
                return new ChannelBind("sms", "SMS", false);
            }

            return new ChannelBind("email", "Email", false);
        }

        private static (bool Email, bool Sms) NamedChannels(string lower)
        {
            var scan = EmailEligibleRegex().Replace(lower, " ");
            var namesSms = ContainsAny(
                scan,
                "sms",
                "text message",
                "text them"
            );
            var namesEmail = ContainsPhrase(scan, "email")
                || ContainsPhrase(scan, "mail them");
            return (namesEmail, namesSms);
        }

        private static bool NamesAllEligiblePhrase(string lower)
            => ContainsAny(
                lower,
                "email-eligible",
                "email eligible",
                "eligible guests",
                "everyone",
                "all guests",
                "all currently"
            );

        private static List<string> ResolveAudiences(string lower, string? chosenLabel)
        {
            if (!string.IsNullOrWhiteSpace(chosenLabel))
            {
                var chosen = AudienceLabels
                    .FirstOrDefault(pair =>
                        pair.Value.Equals(chosenLabel, StringComparison.OrdinalIgnoreCase));
                if (chosen.Key is not null)
                {
                    return [chosen.Key];
                }
            }

            var named = new List<string>();
            void Add(string key)
            {
                if (!named.Contains(key, StringComparer.Ordinal))
                {
                    named.Add(key);
                }
            }

            if (NamesAudience(lower, AudienceRecovery))
            {
                Add(AudienceRecovery);
            }

            if (NamesAudience(lower, AudienceDormant))
            {
                Add(AudienceDormant);
            }

            if (NamesAudience(lower, AudienceNewGuests))
            {
                Add(AudienceNewGuests);
            }

            if (NamesAudience(lower, AudiencePositive))
            {
                Add(AudiencePositive);
            }

            if (NamesAudience(lower, AudienceNegative))
            {
                Add(AudienceNegative);
            }

            if (NamesAudience(lower, AudienceOfferNotRedeemed))
            {
                Add(AudienceOfferNotRedeemed);
            }

            if (NamesAudience(lower, AudienceRecentRedeemers))
            {
                Add(AudienceRecentRedeemers);
            }

            if (NamesAudience(lower, AudienceNoRecentActivity))
            {
                Add(AudienceNoRecentActivity);
            }

            var namedTighter = named
                .Where(key => key != AudienceAllEligible)
                .OrderBy(key => IndexOfAudience(lower, key))
                .ToList();
            if (namedTighter.Count >= 1)
            {
                return namedTighter;
            }

            if (ContainsAny(
                    lower,
                    "email-eligible",
                    "email eligible",
                    "eligible guests",
                    "everyone",
                    "all guests",
                    "all currently"
                ))
            {
                return [AudienceAllEligible];
            }

            return [];
        }

        private static bool NamesAudience(string lower, string key)
        {
            if (key == AudiencePositive)
            {
                return ContainsPhrase(lower, "positive")
                    || ContainsPhrase(lower, "happy");
            }

            return AudienceNeedles.TryGetValue(key, out var needles)
                && ContainsAny(lower, needles);
        }

        private static int IndexOfAudience(string lower, string key)
        {
            if (!AudienceNeedles.TryGetValue(key, out var needles))
            {
                return int.MaxValue;
            }

            var index = needles
                .Select(needle => lower.IndexOf(needle, StringComparison.Ordinal))
                .Where(value => value >= 0)
                .DefaultIfEmpty(int.MaxValue)
                .Min();
            return index;
        }

        private static bool IsUnevaluableAudience(string lower)
            => ContainsAny(
                lower,
                "offer-not-redeemed",
                "offer not redeemed",
                "not redeemed",
                "haven't redeemed",
                "have not redeemed",
                "did not redeem",
                "recent-redeemers",
                "recent redeemers",
                "recently redeemed",
                "no-recent-tummly-activity",
                "no recent tummly activity",
                "no recent activity",
                "saved group",
                "saved-group"
            );

        /// <summary>
        /// Thank-recent uses the governed New guests audience.
        /// A vague "inactive" ask stays all eligible.
        /// </summary>
        private static string AudienceForUnnamedGoal(string goalId)
            => goalId == "thank-recent-guests"
                ? AudienceNewGuests
                : AudienceAllEligible;

        private static string InferGoal(string lower)
        {
            if (ContainsAny(
                    lower,
                    "completed recovery follow-up",
                    "completed recovery",
                    "recovery follow-up"
                ))
            {
                return "follow-up-completed-recovery";
            }

            if (ContainsAny(lower, "quiet time", "quieter time", "quiet-time"))
            {
                return "boost-quieter-time";
            }

            if (ContainsAny(
                    lower,
                    "bring back",
                    "win back",
                    "win-back",
                    "re-engage",
                    "reengage",
                    "inactive"
                ))
            {
                return "re-engage-inactive";
            }

            if (ContainsPhrase(lower, "thank"))
            {
                return "thank-recent-guests";
            }

            if (ContainsAny(
                    lower,
                    "promote",
                    "something new",
                    "new item",
                    "new menu",
                    "announce"
                )
                || ContainsPhrase(lower, "new"))
            {
                return "promote-something-new";
            }

            return "custom-campaign";
        }

        private static string? ResolveTemplateId(
            string text,
            IReadOnlyList<AssistantCampaignTemplateRef> templates
        )
        {
            var matches = templates
                .Where(template =>
                    template.Title.Length > 0 && ContainsPhrase(text, template.Title))
                .ToList();
            return matches.Count == 1 ? matches[0].Id : null;
        }

        private static OfferBind ResolveOffer(
            string text,
            IReadOnlyList<AssistantCatalogOfferRef> locationOffers,
            string? chosenTitle,
            IReadOnlyList<AssistantCatalogOfferRef>? otherLocationOffers
        )
        {
            var lower = text.ToLowerInvariant();
            if (ContainsAny(
                    lower,
                    "no offer",
                    "without an offer",
                    "do not include an offer"
                ))
            {
                return OfferBind.None();
            }

            if (!string.IsNullOrWhiteSpace(chosenTitle))
            {
                var chosen = locationOffers
                    .Where(offer =>
                        offer.Attachable
                        && offer.Title.Equals(chosenTitle, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                if (chosen.Count == 1)
                {
                    return OfferBind.Attached(chosen[0]);
                }

                var fragment = locationOffers
                    .Where(offer =>
                        offer.Attachable && ContainsPhrase(offer.Title, chosenTitle))
                    .ToList();
                return fragment.Count == 1
                    ? OfferBind.Attached(fragment[0])
                    : OfferBind.None(
                        $"{chosenTitle} was not attached because it is not attachable at this Owned location."
                    );
            }

            var titleMatches = MatchTitles(text, locationOffers);
            var attachableTitles = titleMatches.Where(offer => offer.Attachable).ToList();
            if (attachableTitles.Count >= 2)
            {
                return OfferBind.Clash(
                    attachableTitles.Select(offer => offer.Title).ToList()
                );
            }

            if (attachableTitles.Count == 1)
            {
                return OfferBind.Attached(attachableTitles[0]);
            }

            if (titleMatches.Count >= 1)
            {
                var named = titleMatches[0].Title;
                return OfferBind.None(
                    $"{named} was not attached because it is not attachable at this Owned location."
                );
            }

            var termMatches = MatchCommercialTerms(text, locationOffers);
            if (termMatches.Count >= 2)
            {
                return OfferBind.Clash(
                    termMatches.Select(offer => offer.Title).ToList()
                );
            }

            if (termMatches.Count == 1)
            {
                return OfferBind.Attached(termMatches[0]);
            }

            var otherTitles = MatchTitles(text, otherLocationOffers ?? []);
            if (otherTitles.Count >= 1)
            {
                var named = otherTitles[0].Title;
                return OfferBind.None(
                    $"{named} was not attached because it is not attachable at this Owned location."
                );
            }

            if (LooksLikeNamedMissingOffer(lower))
            {
                return OfferBind.None(
                    "No matching Active Offer at this Owned location."
                );
            }

            return OfferBind.None();
        }

        private static List<AssistantCatalogOfferRef> MatchTitles(
            string text,
            IReadOnlyList<AssistantCatalogOfferRef> offers
        )
            => CollectTitleMatches(
                text,
                offers,
                (offer, ask) => ContainsPhrase(ask, offer.Title)
            );

        private static List<AssistantCatalogOfferRef> CollectTitleMatches(
            string text,
            IReadOnlyList<AssistantCatalogOfferRef> offers,
            Func<AssistantCatalogOfferRef, string, bool> matches
        )
        {
            var collected = new List<AssistantCatalogOfferRef>();
            foreach (var offer in offers)
            {
                if (offer.Title.Length == 0)
                {
                    continue;
                }

                if (matches(offer, text))
                {
                    collected.Add(offer);
                }
            }

            return CollapseLongerTitle(collected, text);
        }

        private static List<AssistantCatalogOfferRef> CollapseLongerTitle(
            List<AssistantCatalogOfferRef> matches,
            string text
        )
        {
            if (matches.Count < 2)
            {
                return matches;
            }

            return matches
                .Where(candidate =>
                    matches.TrueForAll(other =>
                    {
                        if (other.Id == candidate.Id
                            || other.Title.Length <= candidate.Title.Length
                            || !other.Title.Contains(
                                candidate.Title,
                                StringComparison.OrdinalIgnoreCase
                            ))
                        {
                            return true;
                        }

                        return !ContainsPhrase(text, other.Title);
                    }))
                .ToList();
        }

        private static List<AssistantCatalogOfferRef> MatchCommercialTerms(
            string text,
            IReadOnlyList<AssistantCatalogOfferRef> offers
        )
        {
            var attachable = offers.Where(offer => offer.Attachable).ToList();
            var matches = new List<AssistantCatalogOfferRef>();
            void AddUnique(AssistantCatalogOfferRef offer)
            {
                if (matches.TrueForAll(existing => existing.Id != offer.Id))
                {
                    matches.Add(offer);
                }
            }

            foreach (Match match in PercentRegex().Matches(text))
            {
                if (!decimal.TryParse(
                        match.Groups[1].Value,
                        NumberStyles.Number,
                        CultureInfo.InvariantCulture,
                        out var percent
                    ))
                {
                    continue;
                }

                foreach (var offer in attachable)
                {
                    if (offer.DiscountPercentage is decimal value && value == percent)
                    {
                        AddUnique(offer);
                    }
                }

                if (matches.Count == 0)
                {
                    foreach (var offer in offers)
                    {
                        if (offer.DiscountPercentage is decimal value && value == percent)
                        {
                            AddUnique(offer);
                        }
                    }
                }
            }

            foreach (Match match in AmountRegex().Matches(text))
            {
                var raw = FirstCapturedGroup(match, 1, 2, 3);
                if (raw is null
                    || !decimal.TryParse(
                        raw,
                        NumberStyles.Number,
                        CultureInfo.InvariantCulture,
                        out var amount
                    ))
                {
                    continue;
                }

                foreach (var offer in attachable)
                {
                    if (offer.DiscountAmount is decimal value && value == amount)
                    {
                        AddUnique(offer);
                    }
                }
            }

            foreach (var offer in attachable)
            {
                var freeItem = offer.FreeItemText?.Trim();
                if (!string.IsNullOrWhiteSpace(freeItem)
                    && freeItem.Length >= 4
                    && ContainsPhrase(text, freeItem))
                {
                    AddUnique(offer);
                }
            }

            return matches;
        }

        private static bool LooksLikeNamedMissingOffer(string lower)
            => ContainsAny(lower, "offer called", "offer named")
                && !ContainsAny(
                    lower,
                    "use an offer",
                    "include an offer",
                    "with an offer",
                    "no offer"
                );

        private static bool ContainsPhrase(string text, string phrase)
        {
            if (phrase.Length == 0)
            {
                return false;
            }

            var index = text.IndexOf(phrase, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                return false;
            }

            var after = index + phrase.Length;
            if (index > 0 && char.IsLetterOrDigit(text[index - 1]))
            {
                return false;
            }

            if (after < text.Length && char.IsLetterOrDigit(text[after]))
            {
                return false;
            }

            return true;
        }

        private static string? FirstCapturedGroup(Match match, params int[] indexes)
        {
            foreach (var index in indexes)
            {
                var group = match.Groups[index];
                if (group.Success && !string.IsNullOrWhiteSpace(group.Value))
                {
                    return group.Value;
                }
            }

            return null;
        }

        private static bool ContainsAny(string lower, params string[] needles)
            => needles.Any(needle => lower.Contains(needle, StringComparison.Ordinal));

        private readonly record struct ChannelBind(string Id, string Label, bool Clash);

        private readonly record struct OfferBind(
            string Stance,
            int? OfferId,
            string Label,
            string? Note,
            IReadOnlyList<string>? ClashTitles
        )
        {
            public static OfferBind None(string? note = null)
                => new("no-offer", null, "No Offer", note, null);

            public static OfferBind Attached(AssistantCatalogOfferRef offer)
                => new("existing-offer", offer.Id, offer.Title, null, null);

            public static OfferBind Clash(IReadOnlyList<string> titles)
                => new("no-offer", null, "No Offer", null, titles);
        }

        [GeneratedRegex(
            @"email[-\s]+eligible",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
        )]
        private static partial Regex EmailEligibleRegex();

        [GeneratedRegex(
            @"\b(?:audience\s+to\s+|limit\s+(?:to\s+)?|cap\s+(?:at\s+)?|only\s+)?(\d+)\s+(?:email\s+|sms\s+)?(?:(?:positive|negative)\s+feedback\s+)?guests?\b|\baudience\s+to\s+(\d+)(?:\s+only)?\b|\b(?:only\s+|to\s+)(\d+)\s+only\b|\b(\d+)\s+(?:most\s+recent|newest)\b|\bonly\s+(\d+)\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
        )]
        private static partial Regex GuestCapRegex();

        [GeneratedRegex(
            @"(\d+(?:\.\d+)?)\s*(?:%|percent(?:age)?s?\b)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
        )]
        private static partial Regex PercentRegex();

        [GeneratedRegex(
            @"£\s*(\d+(?:\.\d+)?)|\$\s*(\d+(?:\.\d+)?)|(?:^|[^\d])(\d+(?:\.\d+)?)\s*(?:pounds?|gbp|quid|dollars?)\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
        )]
        private static partial Regex AmountRegex();
    }
}
