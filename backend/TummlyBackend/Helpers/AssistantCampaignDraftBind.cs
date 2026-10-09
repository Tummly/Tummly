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
        string? LimitNote = null
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
        public const string AudienceDormant = "dormant-guests";
        public const string AudienceRecovery = "completed-recovery-follow-up";

        public static readonly IReadOnlyDictionary<string, string> AudienceLabels =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [AudienceAllEligible] = "All eligible guests",
                [AudienceNewGuests] = "New guests",
                [AudiencePositive] = "Positive feedback",
                [AudienceNegative] = "Negative feedback",
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

            var channel = ResolveChannel(lower, bindChoice.ChannelLabel);
            if (channel.Clash && bindChoice.ChannelLabel is null)
            {
                return new AssistantCampaignDraftBindOutcome.Gap(
                    AssistantGapTurn.KindChannel,
                    ["Email", "SMS"],
                    ChannelClashBody()
                );
            }

            var guestCap = TryReadGuestCap(lower);
            if (audiences.Count == 0
                && guestCap is int blockedCap
                && bindChoice.AudienceLabel is null
                && !NamesAllEligiblePhrase(lower))
            {
                return new AssistantCampaignDraftBindOutcome.UnevaluableAudience(
                    GuestCountIsNotAudienceBody(blockedCap)
                );
            }

            var goalId = InferGoal(lower);
            var audienceKey = audiences.Count == 1
                ? audiences[0]
                : AudienceForUnnamedGoal(goalId);
            var limitNote = guestCap is int appliedCap
                ? GuestCountNotAppliedNote(appliedCap)
                : null;
            var templateId = ResolveTemplateId(text, templates);
            var name = AssistantCampaignDraftName.Compose(
                goalId,
                channel.Id,
                locationName,
                audienceKey
            );

            return new AssistantCampaignDraftBindOutcome.Bound(
                new AssistantCampaignDraftBindFields(
                    Channel: channel.Id,
                    ChannelLabel: channel.Label,
                    AudienceKey: audienceKey,
                    AudienceLabel: AudienceLabels[audienceKey],
                    GoalId: goalId,
                    TemplateId: templateId,
                    Name: name,
                    OfferStance: offer.Stance,
                    OfferId: offer.OfferId,
                    OfferLabel: offer.Label,
                    OfferNote: offer.Note,
                    LimitNote: limitNote
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
            => "This audience cannot be evaluated yet. I did not save a Campaign Draft.";

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
                || NamesAllEligiblePhrase(lower);
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
            if (!match.Success
                || !int.TryParse(
                    match.Groups[1].Value,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var count
                )
                || count < 1)
            {
                return null;
            }

            return count;
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
            @"\b(\d+)\s+(?:email\s+|sms\s+)?guests?\b",
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
