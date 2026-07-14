using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Notifications;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Notifications;

namespace TummlyBackend.Services
{
    public class OperatorNotificationsService : IOperatorNotificationsService
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationRealtimePublisher _realtime;

        public OperatorNotificationsService(
            ApplicationDbContext context,
            INotificationRealtimePublisher realtime
        )
        {
            _context = context;
            _realtime = realtime;
        }

        public async Task<ProduceNotificationResult> ProduceAsync(
            ProduceNotificationRequest request
        )
        {
            if (!NotificationTypeRegistry.TryGet(request.Type, out var def))
            {
                return new ProduceNotificationResult
                {
                    Status = ProduceNotificationStatus.RejectedUnknownType
                };
            }

            var hasLabel = !string.IsNullOrWhiteSpace(request.CtaLabel);
            var hasHref = !string.IsNullOrWhiteSpace(request.CtaHref);
            if (hasLabel != hasHref)
            {
                return new ProduceNotificationResult
                {
                    Status = ProduceNotificationStatus.RejectedInvalidCta
                };
            }

            var prefs = await ResolvePreferencesAsync(request.UserId);
            if (!IsCategoryEnabled(prefs, def.Category))
            {
                return new ProduceNotificationResult
                {
                    Status = ProduceNotificationStatus.NoOpPreferenceOff
                };
            }

            if (!string.IsNullOrEmpty(request.DedupeKey))
            {
                var dedupeHit = await _context.Notifications.AnyAsync(n =>
                    n.UserId == request.UserId
                    && n.Type == request.Type
                    && n.DedupeKey == request.DedupeKey
                );

                if (dedupeHit)
                {
                    return new ProduceNotificationResult
                    {
                        Status = ProduceNotificationStatus.NoOpDedupe
                    };
                }
            }

            var entity = new Notification
            {
                UserId = request.UserId,
                Category = def.Category,
                Type = def.Type,
                Title = request.Title,
                Body = request.Body,
                CreatedAt = DateTime.UtcNow,
                ReadAt = null,
                CtaLabel = hasLabel ? request.CtaLabel : null,
                CtaHref = hasHref ? request.CtaHref : null,
                Capability = def.Capability,
                DedupeKey = string.IsNullOrEmpty(request.DedupeKey)
                    ? null
                    : request.DedupeKey
            };

            _context.Notifications.Add(entity);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException) when (!string.IsNullOrEmpty(request.DedupeKey))
            {
                _context.Entry(entity).State = EntityState.Detached;

                var stillExists = await _context.Notifications.AnyAsync(n =>
                    n.UserId == request.UserId
                    && n.Type == request.Type
                    && n.DedupeKey == request.DedupeKey
                );

                if (stillExists)
                {
                    return new ProduceNotificationResult
                    {
                        Status = ProduceNotificationStatus.NoOpDedupe
                    };
                }

                throw;
            }

            var dto = ToDto(entity);
            await _realtime.PublishCreatedAsync(dto);

            return new ProduceNotificationResult
            {
                Status = ProduceNotificationStatus.Created,
                Notification = dto
            };
        }

        public async Task<IReadOnlyList<NotificationDto>> ListAsync(int userId)
        {
            var rows = await _context.Notifications
                .AsNoTracking()
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ThenByDescending(n => n.Id)
                .ToListAsync();

            return rows.Select(ToDto).ToList();
        }

        public Task<int> GetUnreadCountAsync(int userId)
        {
            return _context.Notifications.CountAsync(n =>
                n.UserId == userId && n.ReadAt == null
            );
        }

        public async Task<bool> MarkOneReadAsync(
            int userId,
            int notificationId
        )
        {
            var row = await _context.Notifications.FirstOrDefaultAsync(n =>
                n.Id == notificationId && n.UserId == userId
            );

            if (row == null)
            {
                return false;
            }

            if (row.ReadAt == null)
            {
                row.ReadAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return true;
        }

        public async Task<int> MarkInboxReadAsync(int userId)
        {
            return await MarkMatchingReadAsync(
                userId,
                category: null,
                unreadOnly: true
            );
        }

        public Task<int> MarkVisibleReadAsync(
            int userId,
            NotificationListFilter filter
        )
        {
            return MarkMatchingReadAsync(
                userId,
                filter.Category,
                filter.UnreadOnly
            );
        }

        public async Task<NotificationPreferencesDto> GetPreferencesAsync(
            int userId
        )
        {
            return await ResolvePreferencesAsync(userId);
        }

        public async Task<NotificationPreferencesDto> SetPreferencesAsync(
            int userId,
            NotificationPreferencesDto preferences
        )
        {
            var row = await _context.NotificationPreferences
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (row == null)
            {
                row = new NotificationPreference { UserId = userId };
                _context.NotificationPreferences.Add(row);
            }

            row.ProductUpdates = preferences.ProductUpdates;
            row.AccountNotices = preferences.AccountNotices;
            row.WeeklyBriefReminders = preferences.WeeklyBriefReminders;
            row.TipsAndPlaybooks = preferences.TipsAndPlaybooks;
            row.CampaignAndReportUpdates =
                preferences.CampaignAndReportUpdates;

            await _context.SaveChangesAsync();

            return ToPreferencesDto(row);
        }

        public async Task<EnsureSeedsResult> EnsureSeedsAsync(int userId)
        {
            const string seedDedupeKey = "shell-seed";
            var reToast = new List<NotificationDto>();
            var prefs = await ResolvePreferencesAsync(userId);

            foreach (var seed in NotificationSeeds.All)
            {
                if (!NotificationTypeRegistry.TryGet(seed.Type, out var def))
                {
                    continue;
                }

                if (!IsCategoryEnabled(prefs, def.Category))
                {
                    continue;
                }

                var existing = await _context.Notifications
                    .AsNoTracking()
                    .FirstOrDefaultAsync(n =>
                        n.UserId == userId && n.Type == seed.Type
                    );

                if (existing == null)
                {
                    // DedupeKey makes concurrent shell connects race-safe.
                    // Created rows toast via SignalR; NoOpDedupe means another
                    // ensure won — no client re-toast for that create.
                    await ProduceAsync(
                        new ProduceNotificationRequest
                        {
                            UserId = userId,
                            Type = seed.Type,
                            Title = seed.Title,
                            Body = seed.Body,
                            CtaLabel = seed.CtaLabel,
                            CtaHref = seed.CtaHref,
                            DedupeKey = seedDedupeKey
                        }
                    );
                    continue;
                }

                if (existing.ReadAt == null)
                {
                    reToast.Add(ToDto(existing));
                }
            }

            return new EnsureSeedsResult { ReToast = reToast };
        }

        private async Task<int> MarkMatchingReadAsync(
            int userId,
            string? category,
            bool unreadOnly
        )
        {
            var query = _context.Notifications.Where(n => n.UserId == userId);

            if (unreadOnly)
            {
                query = query.Where(n => n.ReadAt == null);
            }

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(n => n.Category == category);
            }

            var rows = await query.ToListAsync();
            var unread = rows.Where(r => r.ReadAt == null).ToList();
            if (unread.Count == 0)
            {
                return 0;
            }

            var now = DateTime.UtcNow;
            foreach (var row in unread)
            {
                row.ReadAt = now;
            }

            await _context.SaveChangesAsync();
            return unread.Count;
        }

        private async Task<NotificationPreferencesDto> ResolvePreferencesAsync(
            int userId
        )
        {
            var row = await _context.NotificationPreferences
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (row == null)
            {
                return new NotificationPreferencesDto();
            }

            return ToPreferencesDto(row);
        }

        private static bool IsCategoryEnabled(
            NotificationPreferencesDto prefs,
            string category
        ) =>
            category switch
            {
                "product-updates" => prefs.ProductUpdates,
                "account-notices" => prefs.AccountNotices,
                "weekly-brief-reminders" => prefs.WeeklyBriefReminders,
                "tips-and-playbooks" => prefs.TipsAndPlaybooks,
                "campaign-and-report-updates" =>
                    prefs.CampaignAndReportUpdates,
                _ => false
            };

        private static NotificationPreferencesDto ToPreferencesDto(
            NotificationPreference row
        ) =>
            new()
            {
                ProductUpdates = row.ProductUpdates,
                AccountNotices = row.AccountNotices,
                WeeklyBriefReminders = row.WeeklyBriefReminders,
                TipsAndPlaybooks = row.TipsAndPlaybooks,
                CampaignAndReportUpdates = row.CampaignAndReportUpdates
            };

        private static NotificationDto ToDto(Notification n) =>
            new()
            {
                Id = n.Id,
                UserId = n.UserId,
                Category = n.Category,
                Type = n.Type,
                Title = n.Title,
                Body = n.Body,
                CreatedAt = n.CreatedAt,
                ReadAt = n.ReadAt,
                CtaLabel = n.CtaLabel,
                CtaHref = n.CtaHref,
                Capability = n.Capability,
                DedupeKey = n.DedupeKey
            };
    }
}
