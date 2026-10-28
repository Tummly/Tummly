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
        public const string CheckSetVersion = "stage-1-v3";

        /// <summary>
        /// Align Matched membership with Guests Smart Group windows.
        /// </summary>
        public const int NewGuestDays = 30;

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
        private readonly ILocationGuestPermissionLedgerService _permissions;
        private readonly IGuestContactSuppressionService _suppressions;

        /// <param name="suppressions">
        /// DI injects the configured hasher. The inline default is for tests
        /// and ad-hoc construction only.
        /// </param>
        public CampaignEligibilityService(
            ApplicationDbContext context,
            ILocationGuestPermissionLedgerService? permissions = null,
            IGuestContactSuppressionService? suppressions = null
        )
        {
            _context = context;
            _permissions =
                permissions ?? new LocationGuestPermissionLedgerService(context);
            _suppressions =
                suppressions
                ?? new GuestContactSuppressionService(
                    context,
                    new GuestContactHasher(GuestContactHasher.TestingFallbackKey)
                );
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

            if (UnevaluableAudienceKeys.Contains(key)
                || (CampaignAudienceStore.IsKey(key)
                    && UnevaluableAudienceKeys.Contains(CampaignAudienceStore.Load(_context, locationId, key).BaseAudienceKey)))
            {
                return Unavailable(key, evaluatedAt);
            }

            if (!CampaignProductAllowLists.IsAllowedAudienceKey(key))
            {
                throw new ArgumentException(
                    $"audienceKey '{key}' is not in the product allow-list."
                );
            }

            var restaurant = await LoadRestaurantForLocationAsync(
                locationId,
                cancellationToken
            );

            var scoped = GuestsListQueryComposer.ScopeToLocations(
                _context.LocationGuests.AsNoTracking().Include(lg => lg.MasterGuest),
                [locationId]
            );

            var matchedQuery = ApplyAudienceMatch(scoped, key, evaluatedAt, locationId);
            var matchedRows = await matchedQuery
                .Select(lg => new GuestEligibilityRow(
                    lg.Id,
                    lg.MarketingPreference,
                    lg.MasterGuest!.Email,
                    lg.MasterGuest.Mobile,
                    lg.MasterGuest.SmsStoppedAtUtc
                ))
                .ToListAsync(cancellationToken);

            var permissionStates = await _permissions.GetCurrentStatesBatchAsync(
                matchedRows.Select(row => row.LocationGuestId).ToList(),
                cancellationToken
            );
            var emailBlocked = await LoadEmailDeliveryBlockedIdsAsync(
                restaurant.Id,
                matchedRows,
                cancellationToken
            );

            return Aggregate(
                key,
                matchedRows,
                restaurant,
                permissionStates,
                emailBlocked,
                evaluatedAt,
                CampaignAudienceStore.IsKey(key) ? CampaignAudienceStore.Load(_context, locationId, key).Limit : null
            );
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

            if (UnevaluableAudienceKeys.Contains(key)
                || (CampaignAudienceStore.IsKey(key)
                    && UnevaluableAudienceKeys.Contains(CampaignAudienceStore.Load(_context, locationId, key).BaseAudienceKey)))
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
            var restaurant = await LoadRestaurantForLocationAsync(
                locationId,
                cancellationToken
            );

            var scoped = GuestsListQueryComposer.ScopeToLocations(
                _context.LocationGuests.AsNoTracking().Include(lg => lg.MasterGuest),
                [locationId]
            );

            var matchedQuery = ApplyAudienceMatch(scoped, key, evaluatedAt, locationId);
            var matchedRows = await matchedQuery
                .Select(lg => new GuestEligibilityRow(
                    lg.Id,
                    lg.MarketingPreference,
                    lg.MasterGuest!.Email,
                    lg.MasterGuest.Mobile,
                    lg.MasterGuest.SmsStoppedAtUtc
                ))
                .ToListAsync(cancellationToken);

            var permissionStates = await _permissions.GetCurrentStatesBatchAsync(
                matchedRows.Select(row => row.LocationGuestId).ToList(),
                cancellationToken
            );

            var emailBlocked = await LoadEmailDeliveryBlockedIdsAsync(
                restaurant.Id,
                matchedRows,
                cancellationToken
            );

            var ids = new List<int>();
            foreach (var guest in matchedRows)
            {
                var states = ResolveGuestStates(guest, permissionStates);

                if (
                    ResolvePrimaryExclusionReason(
                        guest,
                        restaurant,
                        states,
                        emailBlocked
                    ) != null
                )
                {
                    continue;
                }

                var hasEmail = !string.IsNullOrWhiteSpace(guest.Email);
                var hasMobile = !string.IsNullOrWhiteSpace(guest.Mobile);

                if (
                    channelKey == "email"
                    && hasEmail
                    && !emailBlocked.Contains(guest.LocationGuestId)
                    && LocationGuestChannelPermissionGate.CanSendOnChannel(
                        restaurant,
                        states,
                        channelKey
                    )
                )
                {
                    ids.Add(guest.LocationGuestId);
                }
                else if (
                    channelKey == "sms"
                    && hasMobile
                    && LocationGuestChannelPermissionGate.CanSendOnChannel(
                        restaurant,
                        states,
                        channelKey
                    )
                )
                {
                    ids.Add(guest.LocationGuestId);
                }
            }

            return CampaignAudienceStore.IsKey(key)
                ? ids.Take(CampaignAudienceStore.Load(_context, locationId, key).Limit ?? int.MaxValue).ToList()
                : ids;
        }

        private IQueryable<LocationGuest> ApplyAudienceMatch(
            IQueryable<LocationGuest> scoped,
            string audienceKey,
            DateTime nowUtc,
            int locationId
        )
        {
            if (CampaignAudienceStore.IsKey(audienceKey))
            {
                var rule = CampaignAudienceStore.Load(_context, locationId, audienceKey);
                var query = ApplyAudienceMatch(scoped, rule.BaseAudienceKey, nowUtc, locationId);
                var includedIds = rule.GuestIds.ToList();
                var excludedIds = rule.ExcludedGuestIds.ToList();
                if (includedIds.Count > 0) query = query.Where(g => includedIds.Contains(g.Id));
                query = query.Where(g => !excludedIds.Contains(g.Id));
                if (rule.JoinedFromUtc is DateTime from) query = query.Where(g => g.CreatedAt >= from);
                if (rule.JoinedToUtc is DateTime to) query = query.Where(g => g.CreatedAt < to);
                if (!string.IsNullOrWhiteSpace(rule.NameContains)) query = query.Where(g => g.Name.Contains(rule.NameContains));
                return rule.Order == "newest"
                    ? query.OrderByDescending(g => g.CreatedAt).ThenByDescending(g => g.Id)
                    : query.OrderByDescending(g => _context.LocationGuestActivityEvents
                        .Where(e => e.LocationGuestId == g.Id).Select(e => (DateTime?)e.OccurredAt).Max() ?? g.CreatedAt)
                        .ThenByDescending(g => g.Id);
            }
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
                "negative-feedback" => GuestsListQueryComposer.WhereNeedsRecovery(
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

        /// <summary>
        /// Guests whose email address has a permanent hard-bounce suppression
        /// (<see cref="LocationGuestPermissionKind.EmailDelivery"/>) at this
        /// restaurant. A bounce blocks marketing email to that address. It is a
        /// delivery fact, so the consent ledger is not changed.
        /// </summary>
        private async Task<IReadOnlySet<int>> LoadEmailDeliveryBlockedIdsAsync(
            int restaurantId,
            IReadOnlyList<GuestEligibilityRow> rows,
            CancellationToken cancellationToken
        )
        {
            var idsByKey = new Dictionary<string, List<int>>(StringComparer.Ordinal);
            foreach (var row in rows)
            {
                if (
                    string.IsNullOrWhiteSpace(row.Email)
                    || !GuestContactKey.TryCreate(
                        row.Email,
                        ContactType.Email,
                        out var contactKey
                    )
                )
                {
                    continue;
                }

                if (!idsByKey.TryGetValue(contactKey, out var ids))
                {
                    ids = new List<int>();
                    idsByKey[contactKey] = ids;
                }

                ids.Add(row.LocationGuestId);
            }

            var bouncedKeys = await _suppressions.FindContactKeysWithKindAsync(
                restaurantId,
                LocationGuestPermissionKind.EmailDelivery,
                idsByKey.Keys,
                cancellationToken
            );

            var blocked = new HashSet<int>();
            foreach (var contactKey in bouncedKeys)
            {
                blocked.UnionWith(idsByKey[contactKey]);
            }

            return blocked;
        }

        private static CampaignEligibilityDto Aggregate(
            string audienceKey,
            IReadOnlyList<GuestEligibilityRow> matched,
            Restaurant restaurant,
            IReadOnlyDictionary<
                int,
                IReadOnlyDictionary<
                    LocationGuestPermissionKind,
                    LocationGuestPermissionState
                >
            > permissionStates,
            IReadOnlySet<int> emailBlocked,
            DateTime evaluatedAt,
            int? limit = null
        )
        {
            var matchedCount = matched.Count;
            var reasonCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            var currentlyEligible = 0;
            var emailEligible = 0;
            var smsEligible = 0;

            foreach (var guest in matched)
            {
                var states = ResolveGuestStates(guest, permissionStates);
                var primaryReason = ResolvePrimaryExclusionReason(
                    guest,
                    restaurant,
                    states,
                    emailBlocked
                );
                if (primaryReason != null)
                {
                    reasonCounts[primaryReason] =
                        reasonCounts.GetValueOrDefault(primaryReason) + 1;
                    continue;
                }

                currentlyEligible += 1;

                var hasEmail = !string.IsNullOrWhiteSpace(guest.Email);
                var hasMobile = !string.IsNullOrWhiteSpace(guest.Mobile);
                if (
                    hasEmail
                    && !emailBlocked.Contains(guest.LocationGuestId)
                    && LocationGuestChannelPermissionGate.CanSendOnChannel(
                        restaurant,
                        states,
                        "email"
                    )
                )
                {
                    emailEligible += 1;
                }

                if (
                    hasMobile
                    && LocationGuestChannelPermissionGate.CanSendOnChannel(
                        restaurant,
                        states,
                        "sms"
                    )
                )
                {
                    smsEligible += 1;
                }
            }

            if (limit is int cap && currentlyEligible > cap)
            {
                reasonCounts["audience-limit"] = currentlyEligible - cap;
                currentlyEligible = cap;
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
                CurrentlyEligible = Math.Min(currentlyEligible, limit ?? int.MaxValue),
                Excluded = excluded,
                EmailEligible = Math.Min(emailEligible, limit ?? int.MaxValue),
                SmsEligible = Math.Min(smsEligible, limit ?? int.MaxValue),
                ExcludedReasons = excludedReasons,
                CheckSetVersion = CheckSetVersion,
                EvaluatedAt = evaluatedAt,
            };
        }

        /// <summary>
        /// One primary reason per excluded guest.
        /// Priority: invalid-contact → suppressed (restaurant off) →
        /// withdrawn → not-granted. Account / soft-lock remain omitted
        /// until Billing stores exist.
        /// </summary>
        private static string? ResolvePrimaryExclusionReason(
            GuestEligibilityRow guest,
            Restaurant restaurant,
            IReadOnlyDictionary<
                LocationGuestPermissionKind,
                LocationGuestPermissionState
            > states,
            IReadOnlySet<int> emailBlocked
        )
        {
            var hasEmail = !string.IsNullOrWhiteSpace(guest.Email);
            var hasMobile = !string.IsNullOrWhiteSpace(guest.Mobile);

            if (!hasEmail && !hasMobile)
            {
                return "invalid-contact";
            }

            // A hard bounce on this address blocks email marketing to it.
            var emailDeliveryBlocked = emailBlocked.Contains(guest.LocationGuestId);

            var emailAllowed =
                hasEmail
                && !emailDeliveryBlocked
                && LocationGuestChannelPermissionGate.CanSendOnChannel(
                    restaurant,
                    states,
                    "email"
                );
            var smsAllowed =
                hasMobile
                && LocationGuestChannelPermissionGate.CanSendOnChannel(
                    restaurant,
                    states,
                    "sms"
                );

            if (emailAllowed || smsAllowed)
            {
                return null;
            }

            var emailRestaurantOn =
                LocationGuestChannelPermissionGate.IsRestaurantPermissionEnabled(
                    restaurant,
                    LocationGuestPermissionKind.EmailMarketing
                );
            var smsRestaurantOn =
                LocationGuestChannelPermissionGate.IsRestaurantPermissionEnabled(
                    restaurant,
                    LocationGuestPermissionKind.SmsMarketing
                );

            // Suppressed when every contact the guest has is restaurant-off
            // (not only when both restaurant toggles are globally off).
            var emailReachable =
                hasEmail && emailRestaurantOn && !emailDeliveryBlocked;
            var hasReachableRestaurantChannel =
                emailReachable || (hasMobile && smsRestaurantOn);

            if (!hasReachableRestaurantChannel)
            {
                return "suppressed";
            }

            var emailState = states[LocationGuestPermissionKind.EmailMarketing];
            var smsState = states[LocationGuestPermissionKind.SmsMarketing];

            var withdrawnOnReachableChannel =
                (emailReachable
                    && emailState == LocationGuestPermissionState.Withdrawn)
                || (hasMobile
                    && smsRestaurantOn
                    && smsState == LocationGuestPermissionState.Withdrawn);

            if (withdrawnOnReachableChannel)
            {
                return "withdrawn";
            }

            return "not-granted";
        }

        private async Task<Restaurant> LoadRestaurantForLocationAsync(
            int locationId,
            CancellationToken cancellationToken
        )
        {
            var restaurantId = await _context.RestaurantLocations
                .AsNoTracking()
                .Where(row => row.Id == locationId)
                .Select(row => row.RestaurantId)
                .FirstAsync(cancellationToken);

            return await _context.Restaurants
                .AsNoTracking()
                .FirstAsync(r => r.Id == restaurantId, cancellationToken);
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

        private static IReadOnlyDictionary<
            LocationGuestPermissionKind,
            LocationGuestPermissionState
        > ResolveGuestStates(
            GuestEligibilityRow guest,
            IReadOnlyDictionary<
                int,
                IReadOnlyDictionary<
                    LocationGuestPermissionKind,
                    LocationGuestPermissionState
                >
            > permissionStates
        )
        {
            var states = LocationGuestChannelPermissionGate.ResolveEffectiveStates(
                guest.MarketingPreference,
                permissionStates[guest.LocationGuestId]
            );
            if (guest.SmsStoppedAtUtc == null)
            {
                return states;
            }

            // R4.5: a guest who sent STOP to this restaurant is excluded from SMS
            // like a withdrawn guest. Email is not affected.
            return new Dictionary<
                LocationGuestPermissionKind,
                LocationGuestPermissionState
            >(states)
            {
                [LocationGuestPermissionKind.SmsMarketing] =
                    LocationGuestPermissionState.Withdrawn,
            };
        }

        private readonly record struct GuestEligibilityRow(
            int LocationGuestId,
            LocationGuestMarketingPreference MarketingPreference,
            string? Email,
            string? Mobile,
            DateTime? SmsStoppedAtUtc
        );
    }
}
