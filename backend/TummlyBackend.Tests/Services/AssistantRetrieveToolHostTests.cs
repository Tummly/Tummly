using System.Text.Json;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    public class AssistantRetrieveToolHostTests
    {
        [Fact]
        public async Task ExecuteBatch_UnknownTool_ReturnsUnavailable()
        {
            var host = CreateHost(allow: true);
            var results = await host.ExecuteBatchAsync(
                ToolContext(ownedLocationId: 1),
                [new AssistantToolCallRequest("c1", "not_a_tool", "{}")]
            );

            using var doc = JsonDocument.Parse(results[0].ContentJson);
            Assert.Equal("unavailable", doc.RootElement.GetProperty("status").GetString());
        }

        [Fact]
        public async Task ExecuteBatch_MissingLocation_ReturnsBlocked()
        {
            var host = CreateHost(allow: true);
            var results = await host.ExecuteBatchAsync(
                ToolContext(ownedLocationId: null),
                [
                    new AssistantToolCallRequest(
                        "c1",
                        AssistantRetrieveToolCatalog.ReadFeedbackSummary,
                        "{}"
                    ),
                ]
            );

            using var doc = JsonDocument.Parse(results[0].ContentJson);
            Assert.Equal("blocked", doc.RootElement.GetProperty("status").GetString());
        }

        [Fact]
        public async Task ExecuteBatch_PermissionDenied_ReturnsPermissionBlocked()
        {
            var host = CreateHost(allow: false);
            var results = await host.ExecuteBatchAsync(
                ToolContext(ownedLocationId: 1),
                [
                    new AssistantToolCallRequest(
                        "c1",
                        AssistantRetrieveToolCatalog.ReadFeedbackSummary,
                        "{}"
                    ),
                ]
            );

            using var doc = JsonDocument.Parse(results[0].ContentJson);
            Assert.Equal(
                "permission_blocked",
                doc.RootElement.GetProperty("status").GetString()
            );
        }

        [Fact]
        public async Task ExecuteBatch_RunsCallsInParallel_AndMergesEvidence()
        {
            var feedback = new RecordingFeedbackRetrieve();
            var offers = new RecordingOffersRetrieve();
            var host = CreateHost(
                allow: true,
                feedbackRetrieve: feedback,
                offersRetrieve: offers
            );
            var context = ToolContext(ownedLocationId: 9);

            var results = await host.ExecuteBatchAsync(
                context,
                [
                    new AssistantToolCallRequest(
                        "f1",
                        AssistantRetrieveToolCatalog.ReadFeedbackSummary,
                        "{}"
                    ),
                    new AssistantToolCallRequest(
                        "o1",
                        AssistantRetrieveToolCatalog.ReadOffers,
                        "{}"
                    ),
                ]
            );

            Assert.Equal(2, results.Count);
            Assert.Equal(1, feedback.Calls);
            Assert.Equal(1, offers.Calls);
            Assert.Equal(1, context.AccumulatedEvidence.Feedback.TotalCount);
            Assert.Equal(2, context.AccumulatedEvidence.Offers.Claims);
            Assert.All(
                results,
                result =>
                {
                    using var doc = JsonDocument.Parse(result.ContentJson);
                    Assert.Equal("success", doc.RootElement.GetProperty("status").GetString());
                }
            );
        }

        [Fact]
        public async Task ExecuteBatch_CompareAllLocations_ThinsPacksAndRecordsMeta()
        {
            var feedback = new RecordingFeedbackRetrieve();
            var host = CreateHost(allow: true, feedbackRetrieve: feedback);
            var context = ToolContext(
                ownedLocationId: 1,
                compareLocationIds: [1, 2],
                ownedLocations:
                [
                    new AssistantOwnedLocationRef(
                        1,
                        "Camden",
                        "1 High St",
                        CaptureLocationStatus.Active
                    ),
                    new AssistantOwnedLocationRef(
                        2,
                        "Soho",
                        "2 High St",
                        CaptureLocationStatus.Active
                    ),
                ],
                compareAllMode: true
            );

            var results = await host.ExecuteBatchAsync(
                context,
                [
                    new AssistantToolCallRequest(
                        "c1",
                        AssistantRetrieveToolCatalog.CompareAllLocations,
                        "{}"
                    ),
                ]
            );

            Assert.Equal(2, feedback.Calls);
            Assert.NotNull(context.CompareLocations);
            Assert.Equal(2, context.CompareLocations!.Count);
            Assert.Empty(context.FailedLocationNames);
            Assert.Empty(context.NotStartedLocationNames);
            using var doc = JsonDocument.Parse(results[0].ContentJson);
            Assert.Equal("success", doc.RootElement.GetProperty("status").GetString());
            Assert.True(doc.RootElement.GetProperty("compareAll").GetBoolean());
        }

        private static AssistantRetrieveToolHost CreateHost(
            bool allow,
            IAssistantFeedbackRetrieve? feedbackRetrieve = null,
            IAssistantOffersRetrieve? offersRetrieve = null
        )
            => new(
                feedbackRetrieve ?? new RecordingFeedbackRetrieve(),
                offersRetrieve ?? new RecordingOffersRetrieve(),
                new StubCampaignsRetrieve(),
                new StubCaptureRetrieve(),
                new StubHomeRetrieve(),
                new StubGuestsRetrieve(),
                new StubPermissions(allow)
            );

        private static AssistantRetrieveToolContext ToolContext(
            int? ownedLocationId,
            IReadOnlyList<int>? compareLocationIds = null,
            IReadOnlyList<AssistantOwnedLocationRef>? ownedLocations = null,
            bool compareAllMode = false
        )
            => new()
            {
                OwnerUserId = 7,
                OwnedLocationId = ownedLocationId,
                OwnedLocationName = "Camden",
                FromUtc = new DateTime(2026, 8, 21, 0, 0, 0, DateTimeKind.Utc),
                ToUtc = new DateTime(2026, 8, 28, 0, 0, 0, DateTimeKind.Utc),
                PeriodPhrase = "this week",
                OwnedLocations = ownedLocations
                    ?? (ownedLocationId is int id
                        ?
                        [
                            new AssistantOwnedLocationRef(
                                id,
                                "Camden",
                                "1 High St",
                                CaptureLocationStatus.Active
                            ),
                        ]
                        : []),
                CompareLocationIds = compareLocationIds
                    ?? (ownedLocationId is int compareId ? [compareId] : []),
                CompareAllMode = compareAllMode,
            };

        private sealed class StubPermissions(bool allow) : IRestaurantPermissionHelper
        {
            public Task<RestaurantPermissionDecision> AuthorizeAsync(
                System.Security.Claims.ClaimsPrincipal user,
                string areaId,
                PermissionLevel minimum
            )
                => Task.FromResult(Decision());

            public Task<RestaurantPermissionDecision> AuthorizeLocationAsync(
                System.Security.Claims.ClaimsPrincipal user,
                string areaId,
                PermissionLevel minimum,
                int locationId
            )
                => Task.FromResult(Decision());

            public Task<RestaurantPermissionDecision> AuthorizeLocationSetAsync(
                System.Security.Claims.ClaimsPrincipal user,
                string areaId,
                PermissionLevel minimum
            )
                => Task.FromResult(Decision());

            public Task<RestaurantPermissionDecision> AuthorizeNamedLocationIdsAsync(
                IReadOnlyList<int> allowedLocationIds,
                int[] namedLocationIds
            )
                => Task.FromResult(Decision());

            public Task<RestaurantPermissionDecision> AuthorizeUserAsync(
                int userId,
                string areaId,
                PermissionLevel minimum
            )
                => Task.FromResult(Decision());

            public Task<RestaurantPermissionDecision> AuthorizeLocationForUserAsync(
                int userId,
                string areaId,
                PermissionLevel minimum,
                int locationId
            )
                => Task.FromResult(Decision());

            private RestaurantPermissionDecision Decision()
                => allow
                    ? new RestaurantPermissionDecision
                    {
                        Status = RestaurantPermissionStatus.Allowed,
                    }
                    : new RestaurantPermissionDecision
                    {
                        Status = RestaurantPermissionStatus.Forbidden,
                    };
        }

        private sealed class RecordingFeedbackRetrieve : IAssistantFeedbackRetrieve
        {
            public int Calls { get; private set; }

            public Task<AssistantFeedbackRetrieveResult> RetrieveAsync(
                int ownedLocationId,
                DateTime fromUtc,
                DateTime toUtc,
                CancellationToken cancellationToken = default
            )
            {
                Calls++;
                return Task.FromResult<AssistantFeedbackRetrieveResult>(
                    new AssistantFeedbackRetrieveResult.Ok(
                        AssistantFeedbackEvidence.Empty with { TotalCount = 1 }
                    )
                );
            }

            public Task<AssistantFeedbackRetrieveResult> RetrieveIdentityAsync(
                int ownedLocationId,
                string locationName,
                DateTime fromUtc,
                DateTime toUtc,
                CancellationToken cancellationToken = default
            )
                => RetrieveAsync(ownedLocationId, fromUtc, toUtc, cancellationToken);
        }

        private sealed class RecordingOffersRetrieve : IAssistantOffersRetrieve
        {
            public int Calls { get; private set; }

            public Task<AssistantOffersRetrieveResult> RetrieveAsync(
                int ownedLocationId,
                DateTime fromUtc,
                DateTime toUtc,
                CancellationToken cancellationToken = default
            )
            {
                Calls++;
                return Task.FromResult<AssistantOffersRetrieveResult>(
                    new AssistantOffersRetrieveResult.Ok(
                        AssistantOffersEvidence.Empty with { Claims = 2 }
                    )
                );
            }
        }

        private sealed class StubCampaignsRetrieve : IAssistantCampaignsRetrieve
        {
            public Task<AssistantCampaignsRetrieveResult> RetrieveAsync(
                int ownedLocationId,
                DateTime fromUtc,
                DateTime toUtc,
                bool includeMessageCopy = false,
                CancellationToken cancellationToken = default
            )
                => Task.FromResult<AssistantCampaignsRetrieveResult>(
                    new AssistantCampaignsRetrieveResult.Ok(AssistantCampaignsEvidence.Empty)
                );
        }

        private sealed class StubCaptureRetrieve : IAssistantCaptureRetrieve
        {
            public Task<AssistantCaptureRetrieveResult> RetrieveAsync(
                int ownedLocationId,
                DateTime fromUtc,
                DateTime toUtc,
                CancellationToken cancellationToken = default
            )
                => Task.FromResult<AssistantCaptureRetrieveResult>(
                    new AssistantCaptureRetrieveResult.Ok(AssistantCaptureEvidence.Empty)
                );
        }

        private sealed class StubHomeRetrieve : IAssistantHomeKpiRetrieve
        {
            public Task<AssistantHomeKpiRetrieveResult> RetrieveAsync(
                int ownedLocationId,
                DateTime fromUtc,
                DateTime toUtc,
                CancellationToken cancellationToken = default
            )
                => Task.FromResult<AssistantHomeKpiRetrieveResult>(
                    new AssistantHomeKpiRetrieveResult.Ok(AssistantHomeKpiEvidence.Empty)
                );
        }

        private sealed class StubGuestsRetrieve : IAssistantGuestsRetrieve
        {
            public Task<AssistantGuestsRetrieveResult> RetrieveAsync(
                int ownedLocationId,
                CancellationToken cancellationToken = default
            )
                => Task.FromResult<AssistantGuestsRetrieveResult>(
                    new AssistantGuestsRetrieveResult.Ok(AssistantGuestsEvidence.Empty)
                );
        }
    }
}
