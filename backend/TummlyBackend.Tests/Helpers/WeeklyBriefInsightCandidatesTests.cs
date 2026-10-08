using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Helpers
{
    public class WeeklyBriefInsightCandidatesTests : IDisposable
    {
        private readonly ApplicationDbContext _context;

        public WeeklyBriefInsightCandidatesTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);
        }

        public void Dispose()
            => _context.Dispose();

        [Fact]
        public void Emit_ThinWeek_SoleNoMaterialChange()
        {
            var current = EmptyMetrics() with { GuestsJoined = 1 };

            var bag = WeeklyBriefInsightCandidates.Emit(
                current,
                prior: null,
                priorPrior: null,
                controlSignals: [],
                dataQualityIssue: false
            );

            Assert.Equal(WeeklyBriefInsightCandidates.ThresholdVersion, bag.ThresholdVersion);
            Assert.Single(bag.Candidates);
            Assert.Equal(
                WeeklyBriefInsightCandidates.TypeNoMaterialChange,
                bag.Candidates[0].Type
            );
            Assert.False(bag.Candidates[0].Evidence.CausalEvidence);
        }

        [Fact]
        public void Emit_MeaningfulChange_WhenPriorAtLeastFiveAndDeltaAtLeast20()
        {
            var current = EmptyMetrics() with { QrScanEvents = 12 };
            var prior = EmptyMetrics() with { QrScanEvents = 10 };

            var bag = WeeklyBriefInsightCandidates.Emit(
                current,
                prior,
                priorPrior: null,
                controlSignals: [],
                dataQualityIssue: false
            );

            var change = Assert.Single(
                bag.Candidates,
                c => c.Type == WeeklyBriefInsightCandidates.TypeMeaningfulChange
            );
            Assert.Equal(WeeklyBriefInsightCandidates.MetricQrScanEvents, change.MetricKey);
            Assert.Equal("pct", change.ChangeKind);
            Assert.Equal(20, change.DeltaPercent);
            Assert.Equal("up", change.Direction);
            Assert.Contains(
                WeeklyBriefInsightCandidates.MetricQrScanEvents,
                change.Evidence.MetricKeys
            );
        }

        [Fact]
        public void Emit_MeaningfulChange_New_WhenPriorZeroAndCurrentPositive()
        {
            var current = EmptyMetrics() with { FeedbackCount = 4 };
            var prior = EmptyMetrics() with { FeedbackCount = 0 };

            var bag = WeeklyBriefInsightCandidates.Emit(
                current,
                prior,
                priorPrior: null,
                controlSignals: [],
                dataQualityIssue: false
            );

            var change = Assert.Single(
                bag.Candidates,
                c => c.Type == WeeklyBriefInsightCandidates.TypeMeaningfulChange
            );
            Assert.Equal("new", change.ChangeKind);
            Assert.Null(change.DeltaPercent);
        }

        [Fact]
        public void Emit_SkipsPercentMeaningfulChange_WhenPriorBetweenOneAndFour()
        {
            var current = EmptyMetrics() with { GuestsJoined = 10 };
            var prior = EmptyMetrics() with { GuestsJoined = 3 };

            var bag = WeeklyBriefInsightCandidates.Emit(
                current,
                prior,
                priorPrior: null,
                controlSignals: [],
                dataQualityIssue: false
            );

            Assert.DoesNotContain(
                bag.Candidates,
                c => c.Type == WeeklyBriefInsightCandidates.TypeMeaningfulChange
            );
            Assert.Equal(
                WeeklyBriefInsightCandidates.TypeNoMaterialChange,
                Assert.Single(bag.Candidates).Type
            );
        }

        [Fact]
        public void Emit_SkipsAllWow_WhenPriorUnavailable()
        {
            var current = EmptyMetrics() with { QrScanEvents = 100 };

            var bag = WeeklyBriefInsightCandidates.Emit(
                current,
                prior: null,
                priorPrior: null,
                controlSignals: [],
                dataQualityIssue: false
            );

            Assert.DoesNotContain(
                bag.Candidates,
                c =>
                    c.Type == WeeklyBriefInsightCandidates.TypeMeaningfulChange
                    || c.Type == WeeklyBriefInsightCandidates.TypeSustainedTrend
            );
        }

        [Fact]
        public void Emit_ControlSignal_AndFunnelDropAlias_ForUnderperformQr()
        {
            var current = EmptyMetrics() with
            {
                GuestsJoined = 20,
                QrScanEvents = 20,
                FeedbackCount = 10,
            };
            var signals = new[]
            {
                new WeeklyBriefControlSignalInput(
                    WeeklyBriefInsightCandidates.ActionUnderperformQr,
                    new Dictionary<string, object> { ["scans"] = 1, ["qrCodeId"] = 9 }
                ),
            };

            var bag = WeeklyBriefInsightCandidates.Emit(
                current,
                prior: null,
                priorPrior: null,
                signals,
                dataQualityIssue: false
            );

            Assert.Contains(
                bag.Candidates,
                c =>
                    c.Type == WeeklyBriefInsightCandidates.TypeControlSignal
                    && c.ActionKind
                        == WeeklyBriefInsightCandidates.ActionUnderperformQr
            );
            Assert.Contains(
                bag.Candidates,
                c =>
                    c.Type == WeeklyBriefInsightCandidates.TypeFunnelDrop
                    && c.ActionKind
                        == WeeklyBriefInsightCandidates.ActionUnderperformQr
            );
        }

        [Fact]
        public void Emit_EmergingTheme_WhenTopTagClearsAbsoluteThreshold()
        {
            var current = EmptyMetrics() with
            {
                FeedbackCount = 12,
                DetectedTagCounts = new Dictionary<string, int>
                {
                    ["Slow service"] = 4,
                    ["Cold food"] = 1,
                },
            };

            var bag = WeeklyBriefInsightCandidates.Emit(
                current,
                prior: null,
                priorPrior: null,
                controlSignals: [],
                dataQualityIssue: false
            );

            var theme = Assert.Single(
                bag.Candidates,
                c => c.Type == WeeklyBriefInsightCandidates.TypeEmergingTheme
            );
            Assert.Equal("Slow service", theme.ThemeLabel);
        }

        [Fact]
        public void Emit_DataQualityIssue_WhenFlagged()
        {
            var current = EmptyMetrics() with { GuestsJoined = 1 };

            var bag = WeeklyBriefInsightCandidates.Emit(
                current,
                prior: null,
                priorPrior: null,
                controlSignals: [],
                dataQualityIssue: true
            );

            Assert.Equal(
                WeeklyBriefInsightCandidates.TypeDataQualityIssue,
                Assert.Single(bag.Candidates).Type
            );
        }

        [Fact]
        public void Emit_SustainedTrend_WhenSameMetricDirectionTwoWeeks()
        {
            var current = EmptyMetrics() with { RedemptionsInWeek = 15 };
            var prior = EmptyMetrics() with { RedemptionsInWeek = 10 };
            var priorPrior = EmptyMetrics() with { RedemptionsInWeek = 5 };

            var bag = WeeklyBriefInsightCandidates.Emit(
                current,
                prior,
                priorPrior,
                controlSignals: [],
                dataQualityIssue: false
            );

            Assert.Contains(
                bag.Candidates,
                c =>
                    c.Type == WeeklyBriefInsightCandidates.TypeSustainedTrend
                    && c.MetricKey
                        == WeeklyBriefInsightCandidates.MetricRedemptionsInWeek
            );
        }

        [Fact]
        public void Emit_CapsAtFive_ExcludingNoMaterialChange()
        {
            var current = EmptyMetrics() with
            {
                GuestsJoined = 20,
                QrScanEvents = 20,
                FeedbackCount = 20,
                RedemptionsInWeek = 20,
                UnsubscribesInWeek = 20,
                DetectedTagCounts = new Dictionary<string, int>
                {
                    ["Theme"] = 10,
                },
            };
            var prior = EmptyMetrics() with
            {
                GuestsJoined = 10,
                QrScanEvents = 10,
                FeedbackCount = 10,
                RedemptionsInWeek = 10,
                UnsubscribesInWeek = 10,
            };
            var signals = new[]
            {
                new WeeklyBriefControlSignalInput(
                    WeeklyBriefInsightCandidates.ActionFeedbackNeedsAttention,
                    new Dictionary<string, object> { ["needsAttentionCount"] = 3 }
                ),
                new WeeklyBriefControlSignalInput(
                    WeeklyBriefInsightCandidates.ActionUnderperformQr,
                    new Dictionary<string, object> { ["scans"] = 1 }
                ),
                new WeeklyBriefControlSignalInput(
                    WeeklyBriefInsightCandidates.ActionRepeatedInvalid,
                    new Dictionary<string, object> { ["count"] = 2 }
                ),
            };

            var bag = WeeklyBriefInsightCandidates.Emit(
                current,
                prior,
                priorPrior: null,
                signals,
                dataQualityIssue: true
            );

            Assert.Equal(5, bag.Candidates.Count);
            Assert.DoesNotContain(
                bag.Candidates,
                c => c.Type == WeeklyBriefInsightCandidates.TypeNoMaterialChange
            );
            Assert.Equal(
                WeeklyBriefInsightCandidates.TypeControlSignal,
                bag.Candidates[0].Type
            );
        }

        [Fact]
        public void IsDataQualityIssue_True_ForThinMetrics()
        {
            Assert.True(
                WeeklyBriefInsightCandidates.IsDataQualityIssue(
                    EmptyMetrics() with { GuestsJoined = 1 }
                )
            );
        }

        [Fact]
        public async Task LoadPriorWeek_FreshAggregate_WithoutPriorBriefRow()
        {
            var locationId = await SeedLocationAsync();
            var weekKey = "monday:2026-07-13";
            var priorStart = new DateTime(2026, 7, 6, 0, 0, 0, DateTimeKind.Utc);
            for (var i = 0; i < 3; i++)
            {
                _context.LocationGuests.Add(
                    new LocationGuest
                    {
                        RestaurantLocationId = locationId,
                        Name = $"Guest {i}",
                        CreatedAt = priorStart.AddHours(i + 1),
                    }
                );
            }

            await _context.SaveChangesAsync();

            Assert.False(
                await _context.WeeklyBriefs.AnyAsync(b =>
                    b.LocationId == locationId
                )
            );

            var prior = await WeeklyBriefMetricsLoader.LoadPriorWeekAsync(
                _context,
                locationId,
                weekKey,
                WeeklyBriefWeekKey.DefaultLocationTimeZoneId
            );

            Assert.NotNull(prior);
            Assert.Equal(3, prior!.GuestsJoined);
        }

        [Fact]
        public async Task BuildAsync_EmitsControlSignal_FromNeedsAttention()
        {
            var locationId = await SeedLocationAsync();
            var weekKey = "monday:2026-07-13";
            Assert.True(
                WeeklyBriefWeekKey.TryCoverageWindow(
                    weekKey,
                    WeeklyBriefWeekKey.DefaultLocationTimeZoneId,
                    out var from,
                    out var to
                )
            );
            var metrics = EmptyMetrics() with
            {
                GuestsJoined = 20,
                QrScanEvents = 20,
                FeedbackCount = 10,
                NeedsAttentionCount = 4,
            };

            var bag = await WeeklyBriefInsightCandidateBuild.BuildAsync(
                _context,
                locationId,
                weekKey,
                from,
                to,
                metrics
            );

            Assert.Contains(
                bag.Candidates,
                c =>
                    c.Type == WeeklyBriefInsightCandidates.TypeControlSignal
                    && c.ActionKind
                        == WeeklyBriefInsightCandidates.ActionFeedbackNeedsAttention
            );
        }

        private async Task<int> SeedLocationAsync()
        {
            var restaurant = new Restaurant
            {
                Name = "Insight Emit Co",
                OwnerUserId = 1,
            };
            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Main",
            };
            _context.RestaurantLocations.Add(location);
            await _context.SaveChangesAsync();
            return location.Id;
        }

        private static WeeklyBriefMetrics EmptyMetrics()
            => new(
                GuestsJoined: 0,
                QrScanEvents: 0,
                FeedbackCount: 0,
                PositiveFeedbackCount: 0,
                NeutralFeedbackCount: 0,
                NegativeFeedbackCount: 0,
                NeedsAttentionCount: 0,
                DetectedTagCounts: new Dictionary<string, int>(),
                ActiveOffers: 0,
                ClaimsInWeek: 0,
                RedemptionsInWeek: 0,
                CampaignsSentInWeek: 0,
                CampaignRecipientsReached: 0,
                UnsubscribesInWeek: 0
            );
    }
}
