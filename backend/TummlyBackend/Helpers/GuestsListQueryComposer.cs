using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// EF-translatable Guests list/export filter, smart-group, search, and sort
    /// composition. Keep predicates aligned with
    /// <see cref="LocationGuestProjections"/> marketing / feedback rules.
    /// </summary>
    public static class GuestsListQueryComposer
    {
        public static IQueryable<LocationGuest> ScopeToLocations(
            IQueryable<LocationGuest> query,
            IReadOnlyList<int> locationIds
        )
        {
            var ids = locationIds as List<int> ?? locationIds.ToList();
            return query.Where(lg => ids.Contains(lg.RestaurantLocationId));
        }

        public static IQueryable<LocationGuest> WhereMarketingEligible(
            IQueryable<LocationGuest> query
        )
        {
            return query.Where(lg =>
                !lg.OffersOptOut
                && (
                    !string.IsNullOrWhiteSpace(lg.MasterGuest!.Email)
                    || !string.IsNullOrWhiteSpace(lg.MasterGuest.Mobile)
                )
            );
        }

        public static IQueryable<LocationGuest> WhereNotMarketingEligible(
            IQueryable<LocationGuest> query
        )
        {
            return query.Where(lg =>
                lg.OffersOptOut
                || (
                    string.IsNullOrWhiteSpace(lg.MasterGuest!.Email)
                    && string.IsNullOrWhiteSpace(lg.MasterGuest.Mobile)
                )
            );
        }

        public static IQueryable<LocationGuest> WhereNewGuest(
            IQueryable<LocationGuest> query,
            DateTime newGuestCutoff
        )
        {
            return query.Where(lg => lg.CreatedAt >= newGuestCutoff);
        }

        public static IQueryable<LocationGuest> WherePositiveFeedback(
            IQueryable<LocationGuest> query
        )
        {
            return query.Where(lg =>
                lg.Feedbacks
                    .Where(f =>
                        f.ClassificationStatus == ClassificationStatus.Succeeded
                    )
                    .OrderByDescending(f => f.CreatedAt)
                    .Select(f => f.Sentiment)
                    .FirstOrDefault() == FeedbackSentiment.Positive
            );
        }

        public static IQueryable<LocationGuest> WhereDormant(
            IQueryable<LocationGuest> query,
            DateTime dormantCutoff
        )
        {
            return query.Where(lg =>
                lg.Feedbacks
                    .OrderByDescending(f => f.CreatedAt)
                    .Select(f => (DateTime?)f.CreatedAt)
                    .FirstOrDefault() != null
                && lg.Feedbacks
                    .OrderByDescending(f => f.CreatedAt)
                    .Select(f => (DateTime?)f.CreatedAt)
                    .FirstOrDefault()! < dormantCutoff
            );
        }

        public static IQueryable<LocationGuest> WhereEmpty(
            IQueryable<LocationGuest> query
        )
        {
            return query.Where(_ => false);
        }

        public static IQueryable<LocationGuest> ApplySmartGroup(
            IQueryable<LocationGuest> query,
            string smartGroup,
            DateTime newGuestCutoff,
            DateTime dormantCutoff,
            IReadOnlySet<string> deferredSmartGroups
        )
        {
            if (deferredSmartGroups.Contains(smartGroup))
            {
                return WhereEmpty(query);
            }

            return smartGroup switch
            {
                "all-guests" => query,
                "new-guests" => WhereNewGuest(query, newGuestCutoff),
                "positive-feedback" => WherePositiveFeedback(query),
                "dormant-guests" => WhereDormant(query, dormantCutoff),
                _ => query,
            };
        }

        public static IQueryable<LocationGuest> ApplySearch(
            IQueryable<LocationGuest> query,
            string normalizedQuery
        )
        {
            if (string.IsNullOrWhiteSpace(normalizedQuery))
            {
                return query;
            }

            var term = normalizedQuery.ToLowerInvariant();
            return query.Where(lg =>
                lg.Name.ToLower().Contains(term)
                || (
                    lg.MasterGuest!.Email != null
                    && lg.MasterGuest.Email.ToLower().Contains(term)
                )
                || (
                    lg.MasterGuest.NormalizedEmail != null
                    && lg.MasterGuest.NormalizedEmail.ToLower().Contains(term)
                )
                || (
                    lg.MasterGuest.Mobile != null
                    && lg.MasterGuest.Mobile.ToLower().Contains(term)
                )
                || (
                    lg.MasterGuest.NormalizedPhone != null
                    && lg.MasterGuest.NormalizedPhone.ToLower().Contains(term)
                )
            );
        }

        public static IQueryable<LocationGuest> ApplyMarketingFilter(
            IQueryable<LocationGuest> query,
            IReadOnlyList<string> marketing
        )
        {
            if (marketing.Count == 0)
            {
                return query;
            }

            var options = GuestsFilterOptions.Normalize(
                marketing,
                GuestsFilterOptions.Marketing
            );
            var includeEligible = options.Contains("eligible");
            var includeNotEligible = options.Contains("not-opted-in");

            if (includeEligible && includeNotEligible)
            {
                return query;
            }

            if (includeEligible)
            {
                return WhereMarketingEligible(query);
            }

            if (includeNotEligible)
            {
                return WhereNotMarketingEligible(query);
            }

            return query;
        }

        public static IQueryable<LocationGuest> ApplyContactFilter(
            IQueryable<LocationGuest> query,
            IReadOnlyList<string> contact
        )
        {
            if (contact.Count == 0)
            {
                return query;
            }

            var options = GuestsFilterOptions.Normalize(
                contact,
                GuestsFilterOptions.Contact
            );
            var includeEmail = options.Contains("email");
            var includeMobile = options.Contains("mobile");

            if (includeEmail && includeMobile)
            {
                return query.Where(lg =>
                    !string.IsNullOrWhiteSpace(lg.MasterGuest!.Email)
                    || !string.IsNullOrWhiteSpace(lg.MasterGuest.Mobile)
                );
            }

            if (includeEmail)
            {
                return query.Where(lg =>
                    !string.IsNullOrWhiteSpace(lg.MasterGuest!.Email)
                );
            }

            if (includeMobile)
            {
                return query.Where(lg =>
                    !string.IsNullOrWhiteSpace(lg.MasterGuest!.Mobile)
                );
            }

            return query;
        }

        public static IQueryable<LocationGuest> ApplySentimentFilter(
            IQueryable<LocationGuest> query,
            IReadOnlyList<FeedbackSentiment> sentiments
        )
        {
            if (sentiments.Count == 0)
            {
                return query;
            }

            var selected = sentiments as List<FeedbackSentiment>
                ?? sentiments.ToList();
            return query.Where(lg =>
                lg.Feedbacks.Any(f =>
                    f.ClassificationStatus == ClassificationStatus.Succeeded
                    && f.Sentiment != null
                    && selected.Contains(f.Sentiment.Value)
                )
            );
        }

        public static IQueryable<LocationGuest> ApplyTagFilter(
            IQueryable<LocationGuest> query,
            IReadOnlyList<int> tagIds
        )
        {
            if (tagIds.Count == 0)
            {
                return query;
            }

            var ids = tagIds.Distinct().ToList();
            return query.Where(lg =>
                lg.GuestTags.Any(membership => ids.Contains(membership.GuestTagId))
            );
        }

        public static IQueryable<LocationGuest> ApplyDateAxisFilter(
            IQueryable<LocationGuest> query,
            string dateAxis,
            DateTime fromUtc,
            DateTime toUtc
        )
        {
            if (dateAxis == "first-captured")
            {
                return query.Where(lg =>
                    lg.CreatedAt >= fromUtc && lg.CreatedAt < toUtc
                );
            }

            return query.Where(lg =>
                lg.Feedbacks
                    .OrderByDescending(f => f.CreatedAt)
                    .Select(f => (DateTime?)f.CreatedAt)
                    .FirstOrDefault() != null
                && lg.Feedbacks
                    .OrderByDescending(f => f.CreatedAt)
                    .Select(f => (DateTime?)f.CreatedAt)
                    .FirstOrDefault()! >= fromUtc
                && lg.Feedbacks
                    .OrderByDescending(f => f.CreatedAt)
                    .Select(f => (DateTime?)f.CreatedAt)
                    .FirstOrDefault()! < toUtc
            );
        }

        public static IQueryable<LocationGuest> ApplyCapturedAtWindow(
            IQueryable<LocationGuest> query,
            DateTime fromUtc,
            DateTime toUtc
        )
        {
            return query.Where(lg =>
                lg.CreatedAt >= fromUtc && lg.CreatedAt < toUtc
            );
        }

        public static IOrderedQueryable<LocationGuest> ApplySort(
            IQueryable<LocationGuest> query,
            string sort
        )
        {
            return sort switch
            {
                "newest-guests" => query
                    .OrderByDescending(lg => lg.CreatedAt)
                    .ThenByDescending(lg => lg.Id),
                "oldest-guests" => query
                    .OrderBy(lg => lg.CreatedAt)
                    .ThenBy(lg => lg.Id),
                "guest-name-az" => query
                    .OrderBy(lg => lg.Name.ToLower())
                    .ThenBy(lg => lg.Id),
                "guest-name-za" => query
                    .OrderByDescending(lg => lg.Name.ToLower())
                    .ThenByDescending(lg => lg.Id),
                "most-feedback-submissions" => query
                    .OrderByDescending(lg => lg.Feedbacks.Count())
                    .ThenByDescending(lg =>
                        lg.Feedbacks
                            .OrderByDescending(f => f.CreatedAt)
                            .Select(f => (DateTime?)f.CreatedAt)
                            .FirstOrDefault() ?? DateTime.MinValue
                    )
                    .ThenByDescending(lg => lg.Id),
                "most-recent-redemption" or "recent-activity" => query
                    .OrderByDescending(lg =>
                        lg.Feedbacks
                            .OrderByDescending(f => f.CreatedAt)
                            .Select(f => (DateTime?)f.CreatedAt)
                            .FirstOrDefault() ?? DateTime.MinValue
                    )
                    .ThenByDescending(lg => lg.CreatedAt)
                    .ThenByDescending(lg => lg.Id),
                _ => query
                    .OrderByDescending(lg =>
                        lg.Feedbacks
                            .OrderByDescending(f => f.CreatedAt)
                            .Select(f => (DateTime?)f.CreatedAt)
                            .FirstOrDefault() ?? DateTime.MinValue
                    )
                    .ThenByDescending(lg => lg.CreatedAt)
                    .ThenByDescending(lg => lg.Id),
            };
        }
    }
}
