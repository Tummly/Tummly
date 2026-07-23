using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public class GuestTaggingService : IGuestTaggingService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILocationGuestActivityRecorder _recorder;

        public GuestTaggingService(
            ApplicationDbContext context,
            ILocationGuestActivityRecorder recorder
        )
        {
            _context = context;
            _recorder = recorder;
        }

        public async Task<GuestTag> CreateByNameAsync(
            int restaurantId,
            string name,
            CancellationToken cancellationToken = default
        )
        {
            var displayName = GuestTagNaming.FormatDisplayName(name);
            if (displayName.Length == 0)
            {
                throw new ArgumentException(
                    "Tag name is required.",
                    nameof(name)
                );
            }

            var normalized = GuestTagNaming.NormalizeName(displayName);

            var existing = await _context.GuestTags
                .FirstOrDefaultAsync(
                    t =>
                        t.RestaurantId == restaurantId
                        && t.NormalizedName == normalized,
                    cancellationToken
                );

            if (existing != null)
            {
                return existing;
            }

            var created = new GuestTag
            {
                RestaurantId = restaurantId,
                DisplayName = displayName,
                NormalizedName = normalized,
                DetectedTagKey = null,
                AiSourced = false,
                CreatedAt = DateTime.UtcNow,
            };

            _context.GuestTags.Add(created);
            await _context.SaveChangesAsync(cancellationToken);
            return created;
        }

        public async Task<IReadOnlyList<GuestTagPickerItem>> ListForLocationScopeAsync(
            int restaurantId,
            IReadOnlyList<int> locationIds,
            CancellationToken cancellationToken = default
        )
        {
            if (locationIds.Count == 0)
            {
                return Array.Empty<GuestTagPickerItem>();
            }

            var locationIdSet = locationIds.ToHashSet();

            var countByTagId = await _context.LocationGuestTags
                .AsNoTracking()
                .Where(m =>
                    m.GuestTag!.RestaurantId == restaurantId
                    && locationIdSet.Contains(
                        m.LocationGuest!.RestaurantLocationId
                    )
                )
                .GroupBy(m => m.GuestTagId)
                .Select(g => new { TagId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(
                    g => g.TagId,
                    g => g.Count,
                    cancellationToken
                );

            var tags = await _context.GuestTags
                .AsNoTracking()
                .Where(t => t.RestaurantId == restaurantId)
                .OrderBy(t => t.DisplayName)
                .ToListAsync(cancellationToken);

            return tags
                .Select(t => new GuestTagPickerItem(
                    t.Id,
                    t.DisplayName,
                    countByTagId.GetValueOrDefault(t.Id),
                    t.AiSourced
                ))
                .ToList();
        }

        public async Task ApplyAdditiveAsync(
            int restaurantId,
            IReadOnlyList<int> locationIds,
            IReadOnlyList<int> locationGuestIds,
            IReadOnlyList<int> guestTagIds,
            CancellationToken cancellationToken = default
        )
        {
            if (locationGuestIds.Count == 0 || guestTagIds.Count == 0)
            {
                return;
            }

            var locationIdSet = locationIds.ToHashSet();
            var distinctGuestIds = locationGuestIds.Distinct().ToList();
            var distinctTagIds = guestTagIds.Distinct().ToList();

            var ownedGuestIds = await _context.LocationGuests
                .AsNoTracking()
                .Where(lg =>
                    distinctGuestIds.Contains(lg.Id)
                    && locationIdSet.Contains(lg.RestaurantLocationId)
                )
                .Select(lg => lg.Id)
                .ToListAsync(cancellationToken);

            if (ownedGuestIds.Count != distinctGuestIds.Count)
            {
                throw new ArgumentException(
                    "One or more guests are missing or outside the location scope."
                );
            }

            var ownedTagIds = await _context.GuestTags
                .AsNoTracking()
                .Where(t =>
                    distinctTagIds.Contains(t.Id)
                    && t.RestaurantId == restaurantId
                )
                .Select(t => t.Id)
                .ToListAsync(cancellationToken);

            if (ownedTagIds.Count != distinctTagIds.Count)
            {
                throw new ArgumentException(
                    "One or more tags are missing or not in this restaurant catalog."
                );
            }

            var existingPairs = await _context.LocationGuestTags
                .AsNoTracking()
                .Where(m =>
                    ownedGuestIds.Contains(m.LocationGuestId)
                    && ownedTagIds.Contains(m.GuestTagId)
                )
                .Select(m => new { m.LocationGuestId, m.GuestTagId })
                .ToListAsync(cancellationToken);

            var existing = existingPairs
                .Select(p => (p.LocationGuestId, p.GuestTagId))
                .ToHashSet();

            var tagNames = await _context.GuestTags
                .AsNoTracking()
                .Where(t => ownedTagIds.Contains(t.Id))
                .ToDictionaryAsync(
                    t => t.Id,
                    t => t.DisplayName,
                    cancellationToken
                );

            var now = DateTime.UtcNow;
            foreach (var guestId in ownedGuestIds)
            {
                foreach (var tagId in ownedTagIds)
                {
                    if (existing.Contains((guestId, tagId)))
                    {
                        continue;
                    }

                    _context.LocationGuestTags.Add(
                        new LocationGuestTag
                        {
                            LocationGuestId = guestId,
                            GuestTagId = tagId,
                            CreatedAt = now,
                        }
                    );

                    _recorder.RecordTagApplied(
                        guestId,
                        tagId,
                        tagNames[tagId],
                        now
                    );
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task SyncMembershipsAsync(
            int restaurantId,
            IReadOnlyList<int> locationIds,
            IReadOnlyList<int> locationGuestIds,
            IReadOnlyList<int> guestTagIds,
            CancellationToken cancellationToken = default
        )
        {
            if (locationGuestIds.Count == 0)
            {
                return;
            }

            var locationIdSet = locationIds.ToHashSet();
            var distinctGuestIds = locationGuestIds.Distinct().ToList();
            var distinctTagIds = guestTagIds.Distinct().ToList();

            var ownedGuestIds = await _context.LocationGuests
                .AsNoTracking()
                .Where(lg =>
                    distinctGuestIds.Contains(lg.Id)
                    && locationIdSet.Contains(lg.RestaurantLocationId)
                )
                .Select(lg => lg.Id)
                .ToListAsync(cancellationToken);

            if (ownedGuestIds.Count != distinctGuestIds.Count)
            {
                throw new ArgumentException(
                    "One or more guests are missing or outside the location scope."
                );
            }

            if (distinctTagIds.Count > 0)
            {
                var ownedTagIds = await _context.GuestTags
                    .AsNoTracking()
                    .Where(t =>
                        distinctTagIds.Contains(t.Id)
                        && t.RestaurantId == restaurantId
                    )
                    .Select(t => t.Id)
                    .ToListAsync(cancellationToken);

                if (ownedTagIds.Count != distinctTagIds.Count)
                {
                    throw new ArgumentException(
                        "One or more tags are missing or not in this restaurant catalog."
                    );
                }
            }

            var desiredTagIdSet = distinctTagIds.ToHashSet();

            var existingMemberships = await _context.LocationGuestTags
                .Where(m =>
                    ownedGuestIds.Contains(m.LocationGuestId)
                    && m.GuestTag!.RestaurantId == restaurantId
                )
                .Include(m => m.GuestTag)
                .ToListAsync(cancellationToken);

            var tagNames = existingMemberships
                .Where(m => m.GuestTag != null)
                .GroupBy(m => m.GuestTagId)
                .ToDictionary(
                    g => g.Key,
                    g => g.First().GuestTag!.DisplayName
                );

            var missingNameIds = desiredTagIdSet
                .Where(id => !tagNames.ContainsKey(id))
                .ToList();

            if (missingNameIds.Count > 0)
            {
                var fetched = await _context.GuestTags
                    .AsNoTracking()
                    .Where(t => missingNameIds.Contains(t.Id))
                    .ToDictionaryAsync(
                        t => t.Id,
                        t => t.DisplayName,
                        cancellationToken
                    );

                foreach (var pair in fetched)
                {
                    tagNames[pair.Key] = pair.Value;
                }
            }

            var now = DateTime.UtcNow;
            var existingByGuest = existingMemberships
                .GroupBy(m => m.LocationGuestId)
                .ToDictionary(
                    g => g.Key,
                    g => g.ToList()
                );

            foreach (var guestId in ownedGuestIds)
            {
                existingByGuest.TryGetValue(guestId, out var current);
                current ??= new List<LocationGuestTag>();

                var currentTagIds = current
                    .Select(m => m.GuestTagId)
                    .ToHashSet();

                foreach (var membership in current)
                {
                    if (desiredTagIdSet.Contains(membership.GuestTagId))
                    {
                        continue;
                    }

                    _context.LocationGuestTags.Remove(membership);
                    _recorder.RecordTagRemoved(
                        guestId,
                        membership.GuestTagId,
                        tagNames.GetValueOrDefault(
                            membership.GuestTagId,
                            string.Empty
                        ),
                        now
                    );
                }

                foreach (var tagId in desiredTagIdSet)
                {
                    if (currentTagIds.Contains(tagId))
                    {
                        continue;
                    }

                    _context.LocationGuestTags.Add(
                        new LocationGuestTag
                        {
                            LocationGuestId = guestId,
                            GuestTagId = tagId,
                            CreatedAt = now,
                        }
                    );

                    _recorder.RecordTagApplied(
                        guestId,
                        tagId,
                        tagNames[tagId],
                        now
                    );
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<IReadOnlyDictionary<int, IReadOnlyList<int>>> GetMembershipsForGuestsAsync(
            int restaurantId,
            IReadOnlyList<int> locationIds,
            IReadOnlyList<int> locationGuestIds,
            CancellationToken cancellationToken = default
        )
        {
            if (locationGuestIds.Count == 0 || locationIds.Count == 0)
            {
                return new Dictionary<int, IReadOnlyList<int>>();
            }

            var locationIdSet = locationIds.ToHashSet();
            var distinctGuestIds = locationGuestIds.Distinct().ToList();

            var ownedGuestIds = await _context.LocationGuests
                .AsNoTracking()
                .Where(lg =>
                    distinctGuestIds.Contains(lg.Id)
                    && locationIdSet.Contains(lg.RestaurantLocationId)
                )
                .Select(lg => lg.Id)
                .ToListAsync(cancellationToken);

            if (ownedGuestIds.Count != distinctGuestIds.Count)
            {
                throw new ArgumentException(
                    "One or more guests are missing or outside the location scope."
                );
            }

            var memberships = await _context.LocationGuestTags
                .AsNoTracking()
                .Where(m =>
                    ownedGuestIds.Contains(m.LocationGuestId)
                    && m.GuestTag!.RestaurantId == restaurantId
                )
                .Select(m => new { m.LocationGuestId, m.GuestTagId })
                .ToListAsync(cancellationToken);

            var byGuest = memberships
                .GroupBy(m => m.LocationGuestId)
                .ToDictionary(
                    group => group.Key,
                    group =>
                        (IReadOnlyList<int>)
                            group
                                .Select(m => m.GuestTagId)
                                .OrderBy(id => id)
                                .ToList()
                );

            foreach (var guestId in ownedGuestIds)
            {
                if (!byGuest.ContainsKey(guestId))
                {
                    byGuest[guestId] = Array.Empty<int>();
                }
            }

            return byGuest;
        }

        public async Task<GuestTag> EnsureFromDetectedTagAsync(
            int restaurantId,
            DetectedTag detectedTag,
            CancellationToken cancellationToken = default
        )
        {
            var catalog = await LoadCatalogMapsAsync(
                restaurantId,
                cancellationToken
            );
            var tag = ResolveOrStageFromDetectedTag(
                restaurantId,
                detectedTag,
                catalog
            );

            if (_context.Entry(tag).State == EntityState.Added)
            {
                await _context.SaveChangesAsync(cancellationToken);
            }

            return tag;
        }

        public async Task UnionDetectedTagsFromFeedbackAsync(
            Feedback feedback,
            CancellationToken cancellationToken = default
        )
        {
            if (
                feedback.ClassificationStatus != ClassificationStatus.Succeeded
                || feedback.LocationGuestId is null
            )
            {
                return;
            }

            var keys = FeedbackClassificationMapping.DeserializeDetectedTagKeys(
                feedback.DetectedTagsJson
            );

            if (keys is null || keys.Count == 0)
            {
                return;
            }

            var restaurantId = await ResolveRestaurantIdAsync(
                feedback,
                cancellationToken
            );
            if (restaurantId == 0)
            {
                return;
            }

            await ApplyDetectedTagUnionAsync(
                feedback.LocationGuestId.Value,
                restaurantId,
                keys,
                allowCatalogConflictRetry: true,
                cancellationToken
            );
        }

        private async Task ApplyDetectedTagUnionAsync(
            int locationGuestId,
            int restaurantId,
            IReadOnlyList<string> keys,
            bool allowCatalogConflictRetry,
            CancellationToken cancellationToken
        )
        {
            var catalog = await LoadCatalogMapsAsync(
                restaurantId,
                cancellationToken
            );

            var resolved = new List<GuestTag>();
            foreach (var key in keys)
            {
                if (!DetectedTagLabels.TryParseKey(key, out var detectedTag))
                {
                    continue;
                }

                resolved.Add(
                    ResolveOrStageFromDetectedTag(
                        restaurantId,
                        detectedTag,
                        catalog
                    )
                );
            }

            if (resolved.Count == 0)
            {
                return;
            }

            var stagedCatalog = resolved
                .Where(t => _context.Entry(t).State == EntityState.Added)
                .Distinct()
                .ToList();

            if (stagedCatalog.Count > 0)
            {
                try
                {
                    // Flush new catalog rows first so activity payloads and
                    // membership FKs get real GuestTag ids (one batch save).
                    await _context.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateException) when (allowCatalogConflictRetry)
                {
                    foreach (var tag in stagedCatalog)
                    {
                        var entry = _context.Entry(tag);
                        if (entry.State != EntityState.Detached)
                        {
                            entry.State = EntityState.Detached;
                        }
                    }

                    await ApplyDetectedTagUnionAsync(
                        locationGuestId,
                        restaurantId,
                        keys,
                        allowCatalogConflictRetry: false,
                        cancellationToken
                    );
                    return;
                }
            }

            var memberTagIds = (
                await _context.LocationGuestTags
                    .AsNoTracking()
                    .Where(m => m.LocationGuestId == locationGuestId)
                    .Select(m => m.GuestTagId)
                    .ToListAsync(cancellationToken)
            ).ToHashSet();

            var now = DateTime.UtcNow;
            var stagedMembership = false;

            foreach (var catalogTag in resolved.DistinctBy(t => t.Id))
            {
                if (memberTagIds.Contains(catalogTag.Id))
                {
                    continue;
                }

                _context.LocationGuestTags.Add(
                    new LocationGuestTag
                    {
                        LocationGuestId = locationGuestId,
                        GuestTagId = catalogTag.Id,
                        CreatedAt = now,
                    }
                );

                _recorder.RecordTagApplied(
                    locationGuestId,
                    catalogTag.Id,
                    catalogTag.DisplayName,
                    now
                );

                memberTagIds.Add(catalogTag.Id);
                stagedMembership = true;
            }

            if (stagedMembership)
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        private async Task<int> ResolveRestaurantIdAsync(
            Feedback feedback,
            CancellationToken cancellationToken
        )
        {
            var restaurantId = await _context.LocationGuests
                .AsNoTracking()
                .Where(lg => lg.Id == feedback.LocationGuestId!.Value)
                .Select(lg => lg.RestaurantLocation!.RestaurantId)
                .FirstOrDefaultAsync(cancellationToken);

            if (restaurantId != 0)
            {
                return restaurantId;
            }

            return await _context.RestaurantLocations
                .AsNoTracking()
                .Where(l => l.Id == feedback.RestaurantLocationId)
                .Select(l => l.RestaurantId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        private async Task<GuestTagCatalogMaps> LoadCatalogMapsAsync(
            int restaurantId,
            CancellationToken cancellationToken
        )
        {
            var tags = await _context.GuestTags
                .Where(t => t.RestaurantId == restaurantId)
                .ToListAsync(cancellationToken);

            var byKey = new Dictionary<string, GuestTag>(
                StringComparer.Ordinal
            );
            var byNormalized = new Dictionary<string, GuestTag>(
                StringComparer.Ordinal
            );

            foreach (var tag in tags)
            {
                byNormalized[tag.NormalizedName] = tag;
                if (!string.IsNullOrEmpty(tag.DetectedTagKey))
                {
                    byKey[tag.DetectedTagKey] = tag;
                }
            }

            return new GuestTagCatalogMaps(byKey, byNormalized);
        }

        /// <summary>
        /// Stage-only ensure: match DetectedTagKey, else normalized name
        /// (operator-created wins — no flip/rename/stamp), else add AI-sourced
        /// catalog row. Caller owns SaveChanges.
        /// </summary>
        private GuestTag ResolveOrStageFromDetectedTag(
            int restaurantId,
            DetectedTag detectedTag,
            GuestTagCatalogMaps catalog
        )
        {
            var key = detectedTag.ToString();
            var displayName = DetectedTagLabels.For(detectedTag);
            var normalized = GuestTagNaming.NormalizeName(displayName);

            if (catalog.ByKey.TryGetValue(key, out var byKey))
            {
                return byKey;
            }

            if (catalog.ByNormalizedName.TryGetValue(normalized, out var byName))
            {
                // Operator-created (or prior) wins — do not flip AI-sourced,
                // rename, or stamp DetectedTagKey.
                return byName;
            }

            var created = new GuestTag
            {
                RestaurantId = restaurantId,
                DisplayName = displayName,
                NormalizedName = normalized,
                DetectedTagKey = key,
                AiSourced = true,
                CreatedAt = DateTime.UtcNow,
            };

            _context.GuestTags.Add(created);
            catalog.ByKey[key] = created;
            catalog.ByNormalizedName[normalized] = created;
            return created;
        }

        private sealed class GuestTagCatalogMaps(
            Dictionary<string, GuestTag> byKey,
            Dictionary<string, GuestTag> byNormalizedName
        )
        {
            public Dictionary<string, GuestTag> ByKey { get; } = byKey;

            public Dictionary<string, GuestTag> ByNormalizedName { get; } =
                byNormalizedName;
        }
    }
}
