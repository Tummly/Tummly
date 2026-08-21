using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Notifications;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    public class OperatorNotificationsServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly OperatorNotificationsService _service;
        private readonly RecordingRealtimePublisher _realtime = new();
        private readonly int _userId = 42;

        public OperatorNotificationsServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _context.Users.Add(new User
            {
                Id = _userId,
                Email = "op@example.com",
                PasswordHash = "x",
                FullName = "Op",
                Role = "Owner",
                CreatedAt = DateTime.UtcNow
            });
            _context.SaveChanges();

            _service = new OperatorNotificationsService(_context, _realtime);
        }

        [Fact]
        public async Task ProduceAsync_PushesNotification_WhenCreated()
        {
            var result = await _service.ProduceAsync(new ProduceNotificationRequest
            {
                UserId = _userId,
                Type = "password-changed",
                Title = "Your Tummly password was changed",
                Body = "If this wasn't you, reset your password."
            });

            Assert.Equal(ProduceNotificationStatus.Created, result.Status);
            Assert.NotNull(result.Notification);
            Assert.Single(_realtime.Published);
            Assert.Equal(result.Notification.Id, _realtime.Published[0].Id);
            Assert.Equal(_userId, _realtime.Published[0].UserId);
            Assert.Equal("password-changed", _realtime.Published[0].Type);
        }

        [Fact]
        public async Task ProduceAsync_DoesNotPush_WhenPreferenceOff()
        {
            await _service.SetPreferencesAsync(
                _userId,
                new NotificationPreferencesDto { AccountNotices = false }
            );

            var result = await _service.ProduceAsync(new ProduceNotificationRequest
            {
                UserId = _userId,
                Type = "password-changed",
                Title = "Your Tummly password was changed",
                Body = "If this wasn't you, reset your password."
            });

            Assert.Equal(
                ProduceNotificationStatus.NoOpPreferenceOff,
                result.Status
            );
            Assert.Empty(_realtime.Published);
        }

        [Fact]
        public async Task ProduceAsync_DoesNotPush_OnDedupeHit()
        {
            var request = new ProduceNotificationRequest
            {
                UserId = _userId,
                Type = "activation-ending-15-days",
                Title = "15 days",
                Body = "b",
                DedupeKey = "2026-08-01T00:00:00.0000000Z"
            };

            await _service.ProduceAsync(request);
            _realtime.Published.Clear();

            var second = await _service.ProduceAsync(request);

            Assert.Equal(ProduceNotificationStatus.NoOpDedupe, second.Status);
            Assert.Empty(_realtime.Published);
        }

        [Fact]
        public async Task ProduceAsync_CreatesNotification_ForRegisteredTipType()
        {
            var result = await _service.ProduceAsync(new ProduceNotificationRequest
            {
                UserId = _userId,
                Type = "tip-place-qr-materials",
                Title = "Place your QR where guests already pause",
                Body = "Put starter QR materials at the counter.",
                CtaLabel = "View setup guide",
                CtaHref = "/help-center/articles/getting-started"
            });

            Assert.Equal(ProduceNotificationStatus.Created, result.Status);
            Assert.NotNull(result.Notification);
            Assert.Equal(_userId, result.Notification.UserId);
            Assert.Equal("tip-place-qr-materials", result.Notification.Type);
            Assert.Equal("tips-and-playbooks", result.Notification.Category);
            Assert.Null(result.Notification.Capability);
            Assert.Null(result.Notification.ReadAt);
            Assert.Equal("View setup guide", result.Notification.CtaLabel);
            Assert.Equal(
                "/help-center/articles/getting-started",
                result.Notification.CtaHref
            );

            var stored = await _context.Notifications.SingleAsync();
            Assert.Equal(result.Notification.Id, stored.Id);
        }

        [Fact]
        public async Task ProduceAsync_RejectsUnknownType()
        {
            var result = await _service.ProduceAsync(new ProduceNotificationRequest
            {
                UserId = _userId,
                Type = "not-a-real-type",
                Title = "x",
                Body = "y"
            });

            Assert.Equal(
                ProduceNotificationStatus.RejectedUnknownType,
                result.Status
            );
            Assert.Empty(_context.Notifications);
        }

        [Fact]
        public async Task ProduceAsync_NoOps_WhenCategoryPreferenceOff()
        {
            await _service.SetPreferencesAsync(
                _userId,
                new NotificationPreferencesDto
                {
                    TipsAndPlaybooks = false
                }
            );

            var result = await _service.ProduceAsync(new ProduceNotificationRequest
            {
                UserId = _userId,
                Type = "tip-preview-guest-form",
                Title = "Preview your guest form before guests do",
                Body = "Open Preview guest form on Home."
            });

            Assert.Equal(
                ProduceNotificationStatus.NoOpPreferenceOff,
                result.Status
            );
            Assert.Empty(_context.Notifications);
        }

        [Fact]
        public async Task ProduceAsync_NoOps_OnDedupeHit()
        {
            var request = new ProduceNotificationRequest
            {
                UserId = _userId,
                Type = "weekly-brief-ready",
                Title = "Weekly brief ready",
                Body = "Your weekly summary is ready.",
                CtaLabel = "View Home",
                CtaHref = "/single-dashboard?location=1",
                DedupeKey = "1:2026-W28"
            };

            var first = await _service.ProduceAsync(request);
            var second = await _service.ProduceAsync(request);

            Assert.Equal(ProduceNotificationStatus.Created, first.Status);
            Assert.Equal(ProduceNotificationStatus.NoOpDedupe, second.Status);
            Assert.Equal(1, await _context.Notifications.CountAsync());
        }

        [Fact]
        public async Task ProduceAsync_StubCampaignType_StoresCapability()
        {
            var result = await _service.ProduceAsync(new ProduceNotificationRequest
            {
                UserId = _userId,
                Type = "campaign-update",
                Title = "Campaign updated",
                Body = "Your campaign changed.",
                DedupeKey = "camp-1"
            });

            Assert.Equal(ProduceNotificationStatus.Created, result.Status);
            Assert.Equal("campaigns:read", result.Notification!.Capability);
            Assert.Equal(
                "campaign-and-report-updates",
                result.Notification.Category
            );
        }

        [Fact]
        public async Task ProduceAsync_StubReportAndOfferTypes_AreProducible()
        {
            var report = await _service.ProduceAsync(new ProduceNotificationRequest
            {
                UserId = _userId,
                Type = "report-update",
                Title = "Report ready",
                Body = "See report.",
                DedupeKey = "r1"
            });
            var offer = await _service.ProduceAsync(new ProduceNotificationRequest
            {
                UserId = _userId,
                Type = "offer-update",
                Title = "Offer updated",
                Body = "See offer.",
                DedupeKey = "o1"
            });

            Assert.Equal(ProduceNotificationStatus.Created, report.Status);
            Assert.Equal("reports:read", report.Notification!.Capability);
            Assert.Equal(ProduceNotificationStatus.Created, offer.Status);
            Assert.Equal("offers:read", offer.Notification!.Capability);
        }

        [Fact]
        public async Task ProduceAsync_RejectsPartialCta()
        {
            var result = await _service.ProduceAsync(new ProduceNotificationRequest
            {
                UserId = _userId,
                Type = "product-operator-home-live",
                Title = "Your Operator Home is live",
                Body = "Track guest feedback.",
                CtaLabel = "Only label"
            });

            Assert.Equal(
                ProduceNotificationStatus.RejectedInvalidCta,
                result.Status
            );
            Assert.Empty(_context.Notifications);
        }

        [Fact]
        public async Task ListAsync_ReturnsNewestFirst_ForThatUserOnly()
        {
            var older = await _service.ProduceAsync(new ProduceNotificationRequest
            {
                UserId = _userId,
                Type = "product-operator-home-live",
                Title = "Older",
                Body = "b"
            });
            var newer = await _service.ProduceAsync(new ProduceNotificationRequest
            {
                UserId = _userId,
                Type = "tip-preview-guest-form",
                Title = "Newer",
                Body = "b"
            });

            var otherUser = new User
            {
                Id = 99,
                Email = "other@example.com",
                PasswordHash = "x",
                FullName = "Other",
                Role = "Owner",
                CreatedAt = DateTime.UtcNow
            };
            _context.Users.Add(otherUser);
            await _context.SaveChangesAsync();
            await _service.ProduceAsync(new ProduceNotificationRequest
            {
                UserId = 99,
                Type = "password-changed",
                Title = "Other",
                Body = "b"
            });

            // Force ordering when CreatedAt ties: bump older row earlier
            var olderEntity = await _context.Notifications
                .FirstAsync(n => n.Id == older.Notification!.Id);
            olderEntity.CreatedAt = DateTime.UtcNow.AddMinutes(-5);
            await _context.SaveChangesAsync();

            var list = await _service.ListAsync(_userId);

            Assert.Equal(2, list.Count);
            Assert.Equal(newer.Notification!.Id, list[0].Id);
            Assert.Equal(older.Notification!.Id, list[1].Id);
        }

        [Fact]
        public async Task GetUnreadCountAsync_CountsNullReadAtOnly()
        {
            var created = await _service.ProduceAsync(new ProduceNotificationRequest
            {
                UserId = _userId,
                Type = "new-sign-in",
                Title = "New sign-in",
                Body = "b"
            });
            await _service.ProduceAsync(new ProduceNotificationRequest
            {
                UserId = _userId,
                Type = "password-changed",
                Title = "Password changed",
                Body = "b"
            });
            await _service.MarkOneReadAsync(
                _userId,
                created.Notification!.Id
            );

            Assert.Equal(1, await _service.GetUnreadCountAsync(_userId));
        }

        [Fact]
        public async Task MarkOneReadAsync_SetsReadAt()
        {
            var created = await _service.ProduceAsync(new ProduceNotificationRequest
            {
                UserId = _userId,
                Type = "activation-ending-15-days",
                Title = "15 days",
                Body = "b"
            });

            var ok = await _service.MarkOneReadAsync(
                _userId,
                created.Notification!.Id
            );

            Assert.True(ok);
            var row = await _context.Notifications
                .SingleAsync(n => n.Id == created.Notification.Id);
            Assert.NotNull(row.ReadAt);
        }

        [Fact]
        public async Task MarkInboxReadAsync_MarksEntireInbox()
        {
            await _service.ProduceAsync(new ProduceNotificationRequest
            {
                UserId = _userId,
                Type = "tip-place-qr-materials",
                Title = "t1",
                Body = "b",
                CtaLabel = "View setup guide",
                CtaHref = "/help-center/articles/getting-started"
            });
            await _service.ProduceAsync(new ProduceNotificationRequest
            {
                UserId = _userId,
                Type = "tip-preview-guest-form",
                Title = "t2",
                Body = "b"
            });

            var marked = await _service.MarkInboxReadAsync(_userId);

            Assert.Equal(2, marked);
            Assert.Equal(0, await _service.GetUnreadCountAsync(_userId));
        }

        [Fact]
        public async Task MarkVisibleReadAsync_RespectsCategoryFilter()
        {
            await _service.ProduceAsync(new ProduceNotificationRequest
            {
                UserId = _userId,
                Type = "product-operator-home-live",
                Title = "product",
                Body = "b"
            });
            await _service.ProduceAsync(new ProduceNotificationRequest
            {
                UserId = _userId,
                Type = "password-changed",
                Title = "account",
                Body = "b"
            });

            var marked = await _service.MarkVisibleReadAsync(
                _userId,
                new NotificationListFilter
                {
                    Category = "product-updates",
                    UnreadOnly = true
                }
            );

            Assert.Equal(1, marked);
            Assert.Equal(1, await _service.GetUnreadCountAsync(_userId));
        }

        [Fact]
        public async Task EnsureSeedsAsync_CreatesThreeSeeds_WithLockedCopy_WhenMissing()
        {
            var result = await _service.EnsureSeedsAsync(_userId);

            Assert.Empty(result.ReToast);

            var items = await _service.ListAsync(_userId);
            Assert.Equal(3, items.Count);

            var tipQr = Assert.Single(
                items,
                n => n.Type == "tip-place-qr-materials"
            );
            Assert.Equal("tips-and-playbooks", tipQr.Category);
            Assert.Equal(
                "Place your QR where guests already pause",
                tipQr.Title
            );
            Assert.Equal(
                "Put starter QR materials at the counter, collection point, or in delivery bags so guests can scan while they wait — that’s when feedback and sign-ups stick.",
                tipQr.Body
            );
            Assert.Equal("View setup guide", tipQr.CtaLabel);
            Assert.Equal(
                "/help-center/articles/getting-started",
                tipQr.CtaHref
            );
            Assert.Null(tipQr.ReadAt);
            Assert.Equal("shell-seed", tipQr.DedupeKey);

            var tipPreview = Assert.Single(
                items,
                n => n.Type == "tip-preview-guest-form"
            );
            Assert.Equal(
                "Preview your guest form before guests do",
                tipPreview.Title
            );
            Assert.Equal(
                "Open Preview guest form on Home to walk the Smart Guest Link yourself — confirm the experience, then place your QR with confidence.",
                tipPreview.Body
            );
            Assert.Null(tipPreview.CtaLabel);
            Assert.Null(tipPreview.CtaHref);

            var product = Assert.Single(
                items,
                n => n.Type == "product-operator-home-live"
            );
            Assert.Equal("product-updates", product.Category);
            Assert.Equal("Your Operator Home is live", product.Title);
            Assert.Equal(
                "Track guest feedback in Latest activity, finish Guest Loop setup on Home, and download your QR when you’re ready to place materials.",
                product.Body
            );
            Assert.Null(product.CtaLabel);
            Assert.Null(product.CtaHref);

            Assert.Equal(3, _realtime.Published.Count);
        }

        [Fact]
        public async Task EnsureSeedsAsync_DoesNotDuplicate_OnSecondCall()
        {
            await _service.EnsureSeedsAsync(_userId);
            _realtime.Published.Clear();

            var second = await _service.EnsureSeedsAsync(_userId);

            Assert.Equal(3, await _context.Notifications.CountAsync());
            Assert.Empty(_realtime.Published);
            Assert.Equal(3, second.ReToast.Count);
            Assert.All(second.ReToast, n => Assert.Null(n.ReadAt));
        }

        [Fact]
        public async Task EnsureSeedsAsync_ReturnsUnreadExisting_ForReToast()
        {
            await _service.EnsureSeedsAsync(_userId);
            var tip = await _context.Notifications.SingleAsync(n =>
                n.Type == "tip-place-qr-materials"
            );
            tip.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            _realtime.Published.Clear();

            var result = await _service.EnsureSeedsAsync(_userId);

            Assert.Equal(3, await _context.Notifications.CountAsync());
            Assert.Empty(_realtime.Published);
            Assert.Equal(2, result.ReToast.Count);
            Assert.DoesNotContain(
                result.ReToast,
                n => n.Type == "tip-place-qr-materials"
            );
            Assert.Contains(
                result.ReToast,
                n => n.Type == "tip-preview-guest-form"
            );
            Assert.Contains(
                result.ReToast,
                n => n.Type == "product-operator-home-live"
            );
        }

        [Fact]
        public async Task EnsureSeedsAsync_SkipsCategory_WhenPreferenceOff()
        {
            await _service.SetPreferencesAsync(
                _userId,
                new NotificationPreferencesDto
                {
                    TipsAndPlaybooks = false,
                    ProductUpdates = true
                }
            );

            var result = await _service.EnsureSeedsAsync(_userId);

            var items = await _service.ListAsync(_userId);
            Assert.Single(items);
            Assert.Equal("product-operator-home-live", items[0].Type);
            Assert.Empty(result.ReToast);
            Assert.Single(_realtime.Published);
        }

        [Fact]
        public async Task EnsureSeedsAsync_DoesNotReToast_WhenPreferenceOff()
        {
            await _service.EnsureSeedsAsync(_userId);
            await _service.SetPreferencesAsync(
                _userId,
                new NotificationPreferencesDto
                {
                    TipsAndPlaybooks = false,
                    ProductUpdates = true
                }
            );
            _realtime.Published.Clear();

            var result = await _service.EnsureSeedsAsync(_userId);

            Assert.Equal(3, await _context.Notifications.CountAsync());
            Assert.Empty(_realtime.Published);
            Assert.Single(result.ReToast);
            Assert.Equal(
                "product-operator-home-live",
                result.ReToast[0].Type
            );
        }

        [Fact]
        public async Task GetPreferencesAsync_DefaultsAllOn_WhenMissing()
        {
            var prefs = await _service.GetPreferencesAsync(_userId);

            Assert.True(prefs.ProductUpdates);
            Assert.True(prefs.AccountNotices);
            Assert.True(prefs.WeeklyBriefReminders);
            Assert.True(prefs.TipsAndPlaybooks);
            Assert.True(prefs.CampaignAndReportUpdates);
        }

        [Fact]
        public async Task SetPreferencesAsync_PersistsAndRoundTrips()
        {
            await _service.SetPreferencesAsync(
                _userId,
                new NotificationPreferencesDto
                {
                    ProductUpdates = false,
                    AccountNotices = true,
                    WeeklyBriefReminders = false,
                    TipsAndPlaybooks = true,
                    CampaignAndReportUpdates = false
                }
            );

            var prefs = await _service.GetPreferencesAsync(_userId);

            Assert.False(prefs.ProductUpdates);
            Assert.True(prefs.AccountNotices);
            Assert.False(prefs.WeeklyBriefReminders);
            Assert.True(prefs.TipsAndPlaybooks);
            Assert.False(prefs.CampaignAndReportUpdates);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private sealed class RecordingRealtimePublisher
            : INotificationRealtimePublisher
        {
            public List<NotificationDto> Published { get; } = [];

            public Task PublishCreatedAsync(NotificationDto notification)
            {
                Published.Add(notification);
                return Task.CompletedTask;
            }
        }
    }
}
