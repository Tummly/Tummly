using System.Text.Json;
using TummlyBackend.DTOs.BillingCredits;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    public class AssistantRetrieveToolHostTests
    {
        [Fact]
        public async Task ExecuteBatch_BillingPlan_ReturnsPlanAndCredits()
        {
            var billing = new StubBillingCredits();
            var host = CreateHost(allow: true, billingCredits: billing);
            var context = ToolContext(ownedLocationId: 1);

            var results = await host.ExecuteBatchAsync(
                context,
                [
                    new AssistantToolCallRequest(
                        "b1",
                        AssistantRetrieveToolCatalog.ReadBillingPlan,
                        "{}"
                    ),
                ]
            );

            Assert.Equal(1, billing.Calls);
            Assert.Equal(1, billing.UsageCalls);
            Assert.Equal("Growth", context.AccumulatedEvidence.Billing.SubscriptionPlan);
            Assert.Equal(42, context.AccumulatedEvidence.Billing.AiCreditsRemaining);
            Assert.True(context.AccumulatedEvidence.Billing.HasUsageSnapshot);
            Assert.Equal(12, context.AccumulatedEvidence.Billing.EmailUsedThisCycle);
            Assert.Equal(5, context.AccumulatedEvidence.Billing.SmsUsedThisCycle);
            Assert.Equal(3, context.AccumulatedEvidence.Billing.AiUsedThisCycle);
            using var doc = JsonDocument.Parse(results[0].ContentJson);
            Assert.Equal("success", doc.RootElement.GetProperty("status").GetString());
            Assert.Equal(
                "Growth",
                doc.RootElement
                    .GetProperty("evidence")
                    .GetProperty("billingSubscriptionPlan")
                    .GetString()
            );
            Assert.Equal(
                3,
                doc.RootElement
                    .GetProperty("evidence")
                    .GetProperty("billingAiUsedThisCycle")
                    .GetInt32()
            );
        }

        [Fact]
        public async Task ExecuteBatch_BillingPlan_PermissionDenied_ReturnsPermissionBlocked()
        {
            var host = CreateHost(allow: false, billingCredits: new StubBillingCredits());
            var results = await host.ExecuteBatchAsync(
                ToolContext(ownedLocationId: 1),
                [
                    new AssistantToolCallRequest(
                        "b1",
                        AssistantRetrieveToolCatalog.ReadBillingPlan,
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
            IAssistantOffersRetrieve? offersRetrieve = null,
            IBillingCreditsService? billingCredits = null
        )
            => new(
                feedbackRetrieve ?? new RecordingFeedbackRetrieve(),
                offersRetrieve ?? new RecordingOffersRetrieve(),
                new StubCampaignsRetrieve(),
                new StubCaptureRetrieve(),
                new StubHomeRetrieve(),
                new StubGuestsRetrieve(),
                new StubPermissions(allow),
                billingCredits
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
                    ? RestaurantPermissionDecision.Allow(restaurantId: 99)
                    : RestaurantPermissionDecision.Deny();
        }

        private sealed class StubBillingCredits : IBillingCreditsService
        {
            public int Calls { get; private set; }

            public int UsageCalls { get; private set; }

            public Task<BillingCreditsPageDto?> GetPageAsync(
                int userId,
                int restaurantId,
                bool actorCanManage
            )
            {
                Calls++;
                Assert.Equal(99, restaurantId);
                return Task.FromResult<BillingCreditsPageDto?>(
                    new BillingCreditsPageDto
                    {
                        PlanSubscription = new PlanSubscriptionSnapshotDto
                        {
                            SubscriptionPlan = "Growth",
                            BillingStatus = "Active",
                            EmailCreditsRemaining = 10,
                            SmsCreditsRemaining = 5,
                            AiCreditsRemaining = 42,
                            BillingCycle = "monthly",
                            RenewalDateLabel = "Renews 1 Nov 2026",
                            IsPilot = false,
                            PlanPriceNet = "£99",
                        },
                    }
                );
            }

            public Task<CreditsUsageSnapshotDto?> GetUsageAsync(int restaurantId)
            {
                UsageCalls++;
                Assert.Equal(99, restaurantId);
                return Task.FromResult<CreditsUsageSnapshotDto?>(
                    new CreditsUsageSnapshotDto
                    {
                        PeriodLabel = "1–31 Oct 2026",
                        Channels =
                        [
                            new CreditChannelUsageDto
                            {
                                Channel = CreditChannels.Email,
                                UsedThisCycle = 12,
                            },
                            new CreditChannelUsageDto
                            {
                                Channel = CreditChannels.Sms,
                                UsedThisCycle = 5,
                            },
                            new CreditChannelUsageDto
                            {
                                Channel = CreditChannels.Ai,
                                UsedThisCycle = 3,
                            },
                        ],
                    }
                );
            }

            public Task<(byte[] Content, string FileName)?> GetInvoicePdfAsync(
                int restaurantId,
                string invoiceNo
            )
                => Task.FromResult<(byte[] Content, string FileName)?>(null);

            public Task<PaymentMethodUpdateSessionDto?> CreatePaymentMethodUpdateSessionAsync(
                int restaurantId
            )
                => Task.FromResult<PaymentMethodUpdateSessionDto?>(null);

            public Task<PlanChangeResultDto?> SubmitPlanChangeAsync(
                int userId,
                int restaurantId,
                PlanChangeRequestDto request,
                string? idempotencyKey = null
            )
                => Task.FromResult<PlanChangeResultDto?>(null);

            public Task<bool> ContinuePendingPaymentOnFreeAsync(int restaurantId)
                => Task.FromResult(false);

            public Task<(bool Success, string? ErrorCode)?> ClearScheduledChangeAsync(
                int userId,
                int restaurantId
            )
                => Task.FromResult<(bool Success, string? ErrorCode)?>(null);

            public Task<(
                UpdateBillingContactsResponseDto? Response,
                string? Error,
                int StatusCode
            )> UpdateBillingContactsAsync(
                int actorUserId,
                int restaurantId,
                UpdateBillingContactsRequest request
            )
                => Task.FromResult<(
                    UpdateBillingContactsResponseDto? Response,
                    string? Error,
                    int StatusCode
                )>((null, null, 404));

            public Task<(CreditTopUpConfirmDto? Response, int StatusCode, string? ErrorMessage)>
                ConfirmCreditTopUpAsync(
                    int userId,
                    int restaurantId,
                    bool actorCanManage,
                    CreditTopUpRequestDto request
                )
                => Task.FromResult<(CreditTopUpConfirmDto?, int, string?)>((null, 404, null));

            public Task<(CreditTopUpPayDto? Response, int StatusCode, string? ErrorMessage)>
                PayCreditTopUpAsync(
                    int userId,
                    int restaurantId,
                    bool actorCanManage,
                    CreditTopUpRequestDto request,
                    string? idempotencyKey = null
                )
                => Task.FromResult<(CreditTopUpPayDto?, int, string?)>((null, 404, null));

            public Task<CancelPlanResultDto?> CancelPlanAsync(
                int userId,
                int restaurantId,
                CancelPlanRequestDto request
            )
                => Task.FromResult<CancelPlanResultDto?>(null);

            public Task<BillingActivityListDto?> GetActivityAsync(
                int restaurantId,
                int skip,
                int take
            )
                => Task.FromResult<BillingActivityListDto?>(null);
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
