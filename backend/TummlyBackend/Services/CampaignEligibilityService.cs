using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Campaigns;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Staged Campaign eligibility — stage-1 checks only.
    /// Soft-lock / account / Billing suppression are omitted until Billing
    /// exposes them (no invented pass/fail).
    /// </summary>
    public class CampaignEligibilityService : ICampaignEligibilityService
    {
        public const string CheckSetVersion = "stage-1-v1";

        /// <summary>
        /// Align Matched membership with Guests Smart Group windows.
        /// </summary>
        public const int NewGuestDays = 13;

        public const int DormantDays = 90;

        private static readonly HashSet<string> UnevaluableAudienceKeys =
            new(StringComparer.Ordinal)
            {
                "offer-not-redeemed",
                "recent-redeemers",
                "no-recent-tummly-activity",
                "saved-group",
            };

        private readonly ApplicationDbContext _context;

        public CampaignEligibilityService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<CampaignEligibilityDto> EvaluateAsync(
            int locationId,
            string audienceKey,
            CancellationToken cancellationToken = default
        )
        {
            var key = (audienceKey ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("audienceKey is required.");
            }

            var evaluatedAt = DateTime.UtcNow;

            if (UnevaluableAudienceKeys.Contains(key))
            {
                return Unavailable(key, evaluatedAt);
            }

            if (!CampaignProductAllowLists.IsAllowedAudienceKey(key))
            {
                throw new ArgumentException(
                    $"audienceKey '{key}' is not in the product allow-list."
                );
            }

            var scoped = GuestsListQueryComposer.ScopeToLocations(
                _context.LocationGuests.AsNoTracking().Include(lg => lg.MasterGuest),
                [locationId]
            );

            var matchedQuery = ApplyAudienceMatch(scoped, key, evaluatedAt);
            var matchedRows = await matchedQuery
                .Select(lg => new GuestEligibilityRow(
                    lg.Id,
                    lg.OffersOptOut,
                    lg.MasterGuest!.Email,
                    lg.MasterGuest.Mobile
                ))
                .ToListAsync(cancellationToken);

            return Aggregate(key, matchedRows, evaluatedAt);
        }

        public async Task<IReadOnlyList<int>> ListChannelEligibleLocationGuestIdsAsync(
            int locationId,
            string audienceKey,
            string channel,
            CancellationToken cancellationToken = default
        )
        {
            var key = (audienceKey ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("audienceKey is required.");
            }

            var channelKey = (channel ?? string.Empty).Trim().ToLowerInvariant();
            if (channelKey is not ("email" or "sms"))
            {
                throw new ArgumentException("channel must be email or sms.");
            }

            if (UnevaluableAudienceKeys.Contains(key))
            {
                return Array.Empty<int>();
            }

            if (!CampaignProductAllowLists.IsAllowedAudienceKey(key))
            {
                throw new ArgumentException(
                    $"audienceKey '{key}' is not in the product allow-list."
                );
            }

            var evaluatedAt = DateTime.UtcNow;
            var scoped = GuestsListQueryComposer.ScopeToLocations(
                _context.LocationGuests.AsNoTracking().Include(lg => lg.MasterGuest),
                [locationId]
            );

            var matchedQuery = ApplyAudienceMatch(scoped, key, evaluatedAt);
            var matchedRows = await matchedQuery
                .Select(lg => new GuestEligibilityRow(
                    lg.Id,
                    lg.OffersOptOut,
                    lg.MasterGuest!.Email,
                    lg.MasterGuest.Mobile
                ))
                .ToListAsync(cancellationToken);

            var ids = new List<int>();
            foreach (var guest in matchedRows)
            {
                if (ResolvePrimaryExclusionReason(guest) != null)
                {
                    continue;
                }

                var hasEmail = !string.IsNullOrWhiteSpace(guest.Email);
                var hasMobile = !string.IsNullOrWhiteSpace(guest.Mobile);
                if (channelKey == "email" && hasEmail)
                {
                    ids.Add(guest.LocationGuestId);
                }
                else if (channelKey == "sms" && hasMobile)
                {
                    ids.Add(guest.LocationGuestId);
                }
            }

            return ids;
        }

        private IQueryable<LocationGuest> ApplyAudienceMatch(
            IQueryable<LocationGuest> scoped,
            string audienceKey,
            DateTime nowUtc
        )
        {
            var newGuestCutoff = nowUtc.AddDays(-NewGuestDays);
            var dormantCutoff = nowUtc.AddDays(-DormantDays);

            return audienceKey switch
            {
                "all-eligible-guests" => scoped,
                "new-guests" => GuestsListQueryComposer.WhereNewGuest(
                    scoped,
                    newGuestCutoff
                ),
                "positive-feedback" => GuestsListQueryComposer.WherePositiveFeedback(
                    scoped
                ),
                "dormant-guests" => GuestsListQueryComposer.WhereDormant(
                    scoped,
                    dormantCutoff
                ),
                "completed-recovery-follow-up" => WhereCompletedRecovery(scoped),
                _ => GuestsListQueryComposer.WhereEmpty(scoped),
            };
        }

        private IQueryable<LocationGuest> WhereCompletedRecovery(
            IQueryable<LocationGuest> query
        )
        {
            return query.Where(lg =>
                _context.FeedbackRecoveryCompletions.Any(c =>
                    c.Feedback != null && c.Feedback.LocationGuestId == lg.Id
                )
            );
        }

        private static CampaignEligibilityDto Aggregate(
            string audienceKey,
            IReadOnlyList<GuestEligibilityRow> matched,
            DateTime evaluatedAt
        )
        {
            var matchedCount = matched.Count;
            var reasonCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            var currentlyEligible = 0;
            var emailEligible = 0;
            var smsEligible = 0;

            foreach (var guest in matched)
            {
                var primaryReason = ResolvePrimaryExclusionReason(guest);
                if (primaryReason != null)
                {
                    reasonCounts[primaryReason] =
                        reasonCounts.GetValueOrDefault(primaryReason) + 1;
                    continue;
                }

                currentlyEligible += 1;

                var hasEmail = !string.IsNullOrWhiteSpace(guest.Email);
                var hasMobile = !string.IsNullOrWhiteSpace(guest.Mobile);
                if (hasEmail)
                {
                    emailEligible += 1;
                }

                if (hasMobile)
                {
                    smsEligible += 1;
                }
            }

            var excluded = matchedCount - currentlyEligible;
            var excludedReasons = reasonCounts
                .OrderByDescending(pair => pair.Value)
                .ThenBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => new CampaignExcludedReasonCountDto
                {
                    Reason = pair.Key,
                    Count = pair.Value,
                })
                .ToList();

            return new CampaignEligibilityDto
            {
                AudienceKey = audienceKey,
                Evaluable = true,
                Matched = matchedCount,
                CurrentlyEligible = currentlyEligible,
                Excluded = excluded,
                EmailEligible = emailEligible,
                SmsEligible = smsEligible,
                ExcludedReasons = excludedReasons,
                CheckSetVersion = CheckSetVersion,
                EvaluatedAt = evaluatedAt,
            };
        }

        /// <summary>
        /// One primary reason per excluded guest.
        /// Priority: account → soft-lock → opt-out → suppression →
        /// invalid-contact → channel. Account / soft-lock / suppression are
        /// skipped when Billing stores are absent.
        /// </summary>
        private static string? ResolvePrimaryExclusionReason(
            GuestEligibilityRow guest
        )
        {
            if (guest.OffersOptOut)
            {
                return "opt-out";
            }

            var hasEmail = !string.IsNullOrWhiteSpace(guest.Email);
            var hasMobile = !string.IsNullOrWhiteSpace(guest.Mobile);
            if (!hasEmail && !hasMobile)
            {
                return "invalid-contact";
            }

            // Stage-1 Currently eligible = ≥1 channel with contact present.
            // Guests with contact and no opt-out are eligible — no channel reason.
            return null;
        }

        private static CampaignEligibilityDto Unavailable(
            string audienceKey,
            DateTime evaluatedAt
        )
        {
            return new CampaignEligibilityDto
            {
                AudienceKey = audienceKey,
                Evaluable = false,
                Matched = null,
                CurrentlyEligible = null,
                Excluded = null,
                EmailEligible = null,
                SmsEligible = null,
                ExcludedReasons = Array.Empty<CampaignExcludedReasonCountDto>(),
                CheckSetVersion = CheckSetVersion,
                EvaluatedAt = evaluatedAt,
            };
        }

        private readonly record struct GuestEligibilityRow(
            int LocationGuestId,
            bool OffersOptOut,
            string? Email,
            string? Mobile
        );
    }
}
