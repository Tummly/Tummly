using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Notifications;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Services
{
    /// <summary>
    /// Seam under test: <see cref="IWeeklyBriefReadyNotifier.NotifyGeneratedAsync"/>
    /// (ticket 07 + Weekly Brief email). Preference and dedupe are observed through
    /// <see cref="IOperatorNotificationsService"/>, not notifier internals.
    /// </summary>
    public class WeeklyBriefReadyNotifierTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly OperatorNotificationsService _notifications;
        private readonly RecordingEmailService _email;
        private readonly WeeklyBriefReadyNotifier _notifier;

        private static readonly WeeklyBriefClosedWeek ClosedWeek = new(
            WeekKey: "monday:2026-08-10",
            CoverageStartUtc: new DateTime(2026, 8, 9, 23, 0, 0, DateTimeKind.Utc),
            CoverageEndUtcExclusive: new DateTime(
                2026,
                8,
                16,
                23,
                0,
                0,
                DateTimeKind.Utc
            )
        );

        public WeeklyBriefReadyNotifierTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _notifications = new OperatorNotificationsService(
                _context,
                new NullNotificationRealtimePublisher()
            );
            _email = new RecordingEmailService();
            _notifier = new WeeklyBriefReadyNotifier(
                _context,
                _notifications,
                _email
            );
        }

        [Fact]
        public async Task NotifyGeneratedAsync_ProducesWeeklyBriefReady_WithReportsWeeklyBriefCta()
        {
            var seed = await SeedOwnedLocationAsync(accountType: "Single");
            await SeedSucceededBriefAsync(seed.LocationId);

            await _notifier.NotifyGeneratedAsync(seed.LocationId, ClosedWeek);

            var list = await _notifications.ListAsync(seed.UserId);
            var notice = Assert.Single(list);
            Assert.Equal("weekly-brief-ready", notice.Type);
            Assert.Equal("weekly-brief-reminders", notice.Category);
            Assert.Equal(
                "Weekly brief ready — Harbour Kitchen",
                notice.Title
            );
            Assert.Equal(
                "Your weekly summary for Harbour Kitchen is ready.",
                notice.Body
            );
            Assert.Equal(
                $"{seed.LocationId}:monday:2026-08-10",
                notice.DedupeKey
            );
            Assert.Equal("View weekly brief", notice.CtaLabel);
            Assert.Equal(
                $"/single-dashboard/reports/weekly-brief?location={seed.LocationId}",
                notice.CtaHref
            );
        }

        [Fact]
        public async Task NotifyGeneratedAsync_UsesMultiDashboardReportsWeeklyBriefPath_WhenAccountIsMulti()
        {
            var seed = await SeedOwnedLocationAsync(accountType: "Multi");
            await SeedSucceededBriefAsync(seed.LocationId);

            await _notifier.NotifyGeneratedAsync(seed.LocationId, ClosedWeek);

            var list = await _notifications.ListAsync(seed.UserId);
            Assert.Equal(
                $"/multi-dashboard/reports/weekly-brief?location={seed.LocationId}",
                Assert.Single(list).CtaHref
            );
        }

        [Fact]
        public async Task NotifyGeneratedAsync_Dedupes_WhenJobLazyAndBackfillAllSucceed()
        {
            var seed = await SeedOwnedLocationAsync(accountType: "Single");
            await SeedSucceededBriefAsync(seed.LocationId);

            await _notifier.NotifyGeneratedAsync(seed.LocationId, ClosedWeek);
            await _notifier.NotifyGeneratedAsync(seed.LocationId, ClosedWeek);
            await _notifier.NotifyGeneratedAsync(seed.LocationId, ClosedWeek);

            Assert.Equal(1, await _context.Notifications.CountAsync());
            Assert.Single(_email.WeeklyBriefSends);
        }

        [Fact]
        public async Task NotifyGeneratedAsync_NoOps_WhenWeeklyBriefRemindersPreferenceOff()
        {
            var seed = await SeedOwnedLocationAsync(accountType: "Single");
            await SeedSucceededBriefAsync(seed.LocationId);
            await _notifications.SetPreferencesAsync(
                seed.UserId,
                new NotificationPreferencesDto { WeeklyBriefReminders = false }
            );

            await _notifier.NotifyGeneratedAsync(seed.LocationId, ClosedWeek);

            Assert.Empty(_context.Notifications);
            Assert.Empty(_email.WeeklyBriefSends);
        }

        [Fact]
        public async Task NotifyGeneratedAsync_SendsWeeklyBriefEmail_WithMetricsAndCopy()
        {
            var seed = await SeedOwnedLocationAsync(accountType: "Single");
            await SeedSucceededBriefAsync(seed.LocationId);

            await _notifier.NotifyGeneratedAsync(seed.LocationId, ClosedWeek);

            var send = Assert.Single(_email.WeeklyBriefSends);
            Assert.Equal(seed.Email, send.ToEmail);
            Assert.Equal("Alex", send.FirstName);
            Assert.Equal("Harbour Kitchen", send.LocationName);
            Assert.Equal("10–16 August", send.PeriodLabel);
            Assert.Equal(12, send.QrScans);
            Assert.Equal(5, send.FeedbackReceived);
            Assert.Equal(8, send.GuestsCaptured);
            Assert.Equal(3, send.OfferClaimed);
            Assert.Equal(2, send.Redemptions);
            Assert.Equal(40, send.CampaignEngagement);
            Assert.Contains("Steady week", send.WhatChanged);
            Assert.Equal(
                "Follow up with 2 guests",
                send.RecommendedNextStep
            );
            Assert.Equal(
                $"/single-dashboard/reports/weekly-brief?location={seed.LocationId}",
                send.WeeklyBriefUrl
            );
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private async Task<(
            int UserId,
            int LocationId,
            string Email
        )> SeedOwnedLocationAsync(string accountType)
        {
            var email = $"op-{Guid.NewGuid():N}@example.com";
            var user = new User
            {
                Email = email,
                PasswordHash = "x",
                FullName = "Alex Operator",
                Role = "Owner",
                AccountType = accountType,
                CreatedAt = DateTime.UtcNow,
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Weekly Brief Notify Restaurant",
                AccountType = accountType,
                OwnerUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
            };
            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Harbour Kitchen",
                Address = "1 Harbour Way",
                CreatedAt = DateTime.UtcNow,
            };
            _context.RestaurantLocations.Add(location);
            await _context.SaveChangesAsync();

            return (user.Id, location.Id, email);
        }

        private async Task SeedSucceededBriefAsync(int locationId)
        {
            var metrics = new WeeklyBriefMetrics(
                GuestsJoined: 8,
                QrScanEvents: 12,
                FeedbackCount: 5,
                PositiveFeedbackCount: 2,
                NeutralFeedbackCount: 1,
                NegativeFeedbackCount: 2,
                NeedsAttentionCount: 2,
                DetectedTagCounts: new Dictionary<string, int>(),
                ActiveOffers: 1,
                ClaimsInWeek: 3,
                RedemptionsInWeek: 2,
                CampaignsSentInWeek: 1,
                CampaignRecipientsReached: 40,
                UnsubscribesInWeek: 0
            );
            var body = FakeWeeklyBriefProvider.FixtureFor(metrics);
            var enrichment = FakeWeeklyBriefProvider.FixtureEnrichmentFor(
                metrics,
                body
            );

            _context.WeeklyBriefs.Add(
                new WeeklyBrief
                {
                    LocationId = locationId,
                    WeekKey = ClosedWeek.WeekKey,
                    Status = WeeklyBriefStatus.Succeeded,
                    GeneratedAtUtc = DateTime.UtcNow,
                    BodyJson = JsonSerializer.Serialize(
                        body,
                        WeeklyBriefStoreJson.Options
                    ),
                    MetricsJson = JsonSerializer.Serialize(
                        metrics,
                        WeeklyBriefStoreJson.Options
                    ),
                    EnrichmentJson = JsonSerializer.Serialize(
                        enrichment,
                        WeeklyBriefStoreJson.Options
                    ),
                }
            );
            await _context.SaveChangesAsync();
        }

        private sealed class RecordingEmailService : EmailServiceStubBase
        {
            public List<WeeklyBriefEmailSend> WeeklyBriefSends { get; } = [];

            public override Task SendWeeklyBriefEmailAsync(
                string toEmail,
                string firstName,
                string locationName,
                string periodLabel,
                int qrScans,
                int feedbackReceived,
                int guestsCaptured,
                int offerClaimed,
                int redemptions,
                int campaignEngagement,
                string whatChanged,
                string recommendedNextStep,
                string weeklyBriefUrl
            )
            {
                WeeklyBriefSends.Add(
                    new WeeklyBriefEmailSend(
                        toEmail,
                        firstName,
                        locationName,
                        periodLabel,
                        qrScans,
                        feedbackReceived,
                        guestsCaptured,
                        offerClaimed,
                        redemptions,
                        campaignEngagement,
                        whatChanged,
                        recommendedNextStep,
                        weeklyBriefUrl
                    )
                );
                return Task.CompletedTask;
            }
        }

        private sealed record WeeklyBriefEmailSend(
            string ToEmail,
            string FirstName,
            string LocationName,
            string PeriodLabel,
            int QrScans,
            int FeedbackReceived,
            int GuestsCaptured,
            int OfferClaimed,
            int Redemptions,
            int CampaignEngagement,
            string WhatChanged,
            string RecommendedNextStep,
            string WeeklyBriefUrl
        );
    }
}
