using System.Text.RegularExpressions;
using TummlyBackend.Services;

namespace TummlyBackend.Helpers
{
    public sealed record AssistantCombinedCreateCampaignRef(
        int Id,
        string Name,
        string Status
    );

    public abstract record AssistantCombinedCreateCampaignOutcome
    {
        public sealed record CreateNew(string? NamedTitle)
            : AssistantCombinedCreateCampaignOutcome;

        public sealed record UpdateExisting(int CampaignId, string Name)
            : AssistantCombinedCreateCampaignOutcome;

        public sealed record Gap(
            IReadOnlyList<string> Options,
            string Body
        ) : AssistantCombinedCreateCampaignOutcome;

        public sealed record RefuseInFlight(string Body)
            : AssistantCombinedCreateCampaignOutcome;
    }

    /// <summary>
    /// Named Campaign resolution for Create Campaign with Offer — create,
    /// attach-only update on one Draft, title clash Gap, or in-flight refuse.
    /// </summary>
    public static partial class AssistantCombinedCreateCampaignResolve
    {
        private static readonly HashSet<string> InFlightStatuses = new(StringComparer.Ordinal)
        {
            CampaignsListService.ScheduledStatus,
            CampaignsListService.SendingStatus,
            CampaignsListService.PausedStatus,
            CampaignsListService.SentStatus,
            CampaignsListService.PartiallySentStatus,
        };

        public static AssistantCombinedCreateCampaignOutcome Resolve(
            string userMessage,
            IReadOnlyList<AssistantCombinedCreateCampaignRef> locationCampaigns,
            string? chosenCampaignTitle = null
        )
        {
            var namedTitle = ExtractNamedCampaignTitle(userMessage);
            if (!string.IsNullOrWhiteSpace(chosenCampaignTitle))
            {
                namedTitle = chosenCampaignTitle.Trim();
            }

            if (namedTitle is null)
            {
                return new AssistantCombinedCreateCampaignOutcome.CreateNew(null);
            }

            var titleMatches = MatchTitles(userMessage, locationCampaigns, namedTitle);
            if (titleMatches.Count >= 2)
            {
                var titles = titleMatches.Select(campaign => campaign.Name).ToList();
                return new AssistantCombinedCreateCampaignOutcome.Gap(
                    titles,
                    CampaignTitleClashBody(titles)
                );
            }

            if (titleMatches.Count == 1)
            {
                var matched = titleMatches[0];
                if (IsDraft(matched.Status))
                {
                    return new AssistantCombinedCreateCampaignOutcome.UpdateExisting(
                        matched.Id,
                        matched.Name
                    );
                }

                if (IsInFlight(matched.Status))
                {
                    return new AssistantCombinedCreateCampaignOutcome.RefuseInFlight(
                        AssistantCombinedCreatePersistCopy.InFlightCampaignRefusalBody(
                            matched.Name
                        )
                    );
                }

                return new AssistantCombinedCreateCampaignOutcome.CreateNew(namedTitle);
            }

            var inFlightMatches = MatchInFlightTitles(
                userMessage,
                locationCampaigns,
                namedTitle
            );
            if (inFlightMatches.Count >= 1)
            {
                return new AssistantCombinedCreateCampaignOutcome.RefuseInFlight(
                    AssistantCombinedCreatePersistCopy.InFlightCampaignRefusalBody(
                        inFlightMatches[0].Name
                    )
                );
            }

            return new AssistantCombinedCreateCampaignOutcome.CreateNew(namedTitle);
        }

        public static string? ExtractNamedCampaignTitle(string userMessage)
        {
            var text = userMessage.Trim();
            if (text.Length == 0)
            {
                return null;
            }

            foreach (var pattern in AttachToCampaignPatterns())
            {
                var match = pattern.Match(text);
                if (!match.Success)
                {
                    continue;
                }

                var fragment = match.Groups[1].Value.Trim();
                if (IsNoiseFragment(fragment))
                {
                    continue;
                }

                return fragment;
            }

            var named = NamedCampaignRegex().Match(text);
            if (named.Success)
            {
                var fragment = named.Groups[1].Value.Trim();
                if (!IsNoiseFragment(fragment))
                {
                    return fragment;
                }
            }

            return null;
        }

        public static string CampaignTitleClashBody(IReadOnlyList<string> titles)
            => AssistantGapAsk.ForBind(AssistantGapTurn.KindCampaignTitle, titles);

        private static List<AssistantCombinedCreateCampaignRef> MatchTitles(
            string userMessage,
            IReadOnlyList<AssistantCombinedCreateCampaignRef> campaigns,
            string namedTitle
        )
        {
            var collected = new List<AssistantCombinedCreateCampaignRef>();
            foreach (var campaign in campaigns)
            {
                if (!IsDraft(campaign.Status) || campaign.Name.Length == 0)
                {
                    continue;
                }

                if (TitleMatches(userMessage, campaign.Name, namedTitle))
                {
                    collected.Add(campaign);
                }
            }

            return CollapseLongerTitle(collected, userMessage);
        }

        private static List<AssistantCombinedCreateCampaignRef> MatchInFlightTitles(
            string userMessage,
            IReadOnlyList<AssistantCombinedCreateCampaignRef> campaigns,
            string namedTitle
        )
        {
            return campaigns
                .Where(campaign =>
                    IsInFlight(campaign.Status)
                    && campaign.Name.Length > 0
                    && TitleMatches(userMessage, campaign.Name, namedTitle))
                .ToList();
        }

        private static bool TitleMatches(
            string userMessage,
            string storedName,
            string namedTitle
        )
        {
            if (storedName.Equals(namedTitle, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (ContainsPhrase(userMessage, storedName))
            {
                return true;
            }

            return ContainsPhrase(storedName, namedTitle)
                || ContainsPhrase(namedTitle, storedName);
        }

        private static List<AssistantCombinedCreateCampaignRef> CollapseLongerTitle(
            List<AssistantCombinedCreateCampaignRef> matches,
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
                            || other.Name.Length <= candidate.Name.Length
                            || !other.Name.Contains(
                                candidate.Name,
                                StringComparison.OrdinalIgnoreCase
                            ))
                        {
                            return true;
                        }

                        return !ContainsPhrase(text, other.Name);
                    }))
                .ToList();
        }

        private static bool IsDraft(string status)
            => string.Equals(
                status,
                CampaignDraftService.DraftStatus,
                StringComparison.Ordinal
            );

        private static bool IsInFlight(string status)
            => InFlightStatuses.Contains(status);

        private static bool IsNoiseFragment(string fragment)
            => fragment.Equals("this", StringComparison.OrdinalIgnoreCase)
                || fragment.Equals("the", StringComparison.OrdinalIgnoreCase)
                || fragment.Equals("that", StringComparison.OrdinalIgnoreCase)
                || fragment.Equals("my", StringComparison.OrdinalIgnoreCase)
                || fragment.Equals("a", StringComparison.OrdinalIgnoreCase)
                || fragment.Equals("an", StringComparison.OrdinalIgnoreCase);

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

        private static IEnumerable<Regex> AttachToCampaignPatterns()
        {
            yield return AttachToNamedCampaignRegex();
            yield return AttachItToRegex();
        }

        [GeneratedRegex(
            @"attach(?:\s+it)?\s+to\s+(?:the\s+)?(.+?)\s+campaign\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
        )]
        private static partial Regex AttachToNamedCampaignRegex();

        [GeneratedRegex(
            @"attach(?:\s+it)?\s+to\s+(?:the\s+)?(.+?)(?:\s*[—–-]\s*if|\s*,|\s*\.|\s*$)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
        )]
        private static partial Regex AttachItToRegex();

        [GeneratedRegex(
            @"\bthe\s+(.+?)\s+campaign\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
        )]
        private static partial Regex NamedCampaignRegex();
    }
}
