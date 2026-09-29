using System.Text.Json;
using System.Text.Json.Nodes;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Executes retrieve tool calls against existing IAssistant*Retrieve seams.
    /// Scope and permissions come from <see cref="AssistantRetrieveToolContext"/>.
    /// </summary>
    public sealed class AssistantRetrieveToolHost : IAssistantRetrieveToolHost
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = false,
        };

        private readonly IAssistantFeedbackRetrieve _feedbackRetrieve;
        private readonly IAssistantOffersRetrieve _offersRetrieve;
        private readonly IAssistantCampaignsRetrieve _campaignsRetrieve;
        private readonly IAssistantCaptureRetrieve _captureRetrieve;
        private readonly IAssistantHomeKpiRetrieve _homeRetrieve;
        private readonly IAssistantGuestsRetrieve _guestsRetrieve;
        private readonly IRestaurantPermissionHelper _permissions;
        private readonly TimeProvider _clock;

        public AssistantRetrieveToolHost(
            IAssistantFeedbackRetrieve feedbackRetrieve,
            IAssistantOffersRetrieve offersRetrieve,
            IAssistantCampaignsRetrieve campaignsRetrieve,
            IAssistantCaptureRetrieve captureRetrieve,
            IAssistantHomeKpiRetrieve homeRetrieve,
            IAssistantGuestsRetrieve guestsRetrieve,
            IRestaurantPermissionHelper permissions,
            TimeProvider? timeProvider = null
        )
        {
            _feedbackRetrieve = feedbackRetrieve;
            _offersRetrieve = offersRetrieve;
            _campaignsRetrieve = campaignsRetrieve;
            _captureRetrieve = captureRetrieve;
            _homeRetrieve = homeRetrieve;
            _guestsRetrieve = guestsRetrieve;
            _permissions = permissions;
            _clock = timeProvider ?? TimeProvider.System;
        }

        public async Task<IReadOnlyList<AssistantToolCallResult>> ExecuteBatchAsync(
            AssistantRetrieveToolContext context,
            IReadOnlyList<AssistantToolCallRequest> calls,
            CancellationToken cancellationToken = default
        )
        {
            if (calls.Count == 0)
            {
                return [];
            }

            var tasks = calls
                .Select(call => ExecuteOneAsync(context, call, cancellationToken))
                .ToArray();
            return await Task.WhenAll(tasks);
        }

        private async Task<AssistantToolCallResult> ExecuteOneAsync(
            AssistantRetrieveToolContext context,
            AssistantToolCallRequest call,
            CancellationToken cancellationToken
        )
        {
            if (!AssistantRetrieveToolCatalog.IsKnown(call.Name))
            {
                return Result(
                    call,
                    StatusPayload("unavailable", "Unknown retrieve tool.")
                );
            }

            try
            {
                return call.Name switch
                {
                    AssistantRetrieveToolCatalog.ReadFeedbackSummary
                        => await ReadFeedbackAsync(context, call, cancellationToken),
                    AssistantRetrieveToolCatalog.ReadOffers
                        => await ReadOffersAsync(context, call, cancellationToken),
                    AssistantRetrieveToolCatalog.ReadCampaigns
                        => await ReadCampaignsAsync(context, call, cancellationToken),
                    AssistantRetrieveToolCatalog.ReadCapturePerformance
                        => await ReadCaptureAsync(context, call, cancellationToken),
                    AssistantRetrieveToolCatalog.ReadHomeKpis
                        => await ReadHomeAsync(context, call, cancellationToken),
                    AssistantRetrieveToolCatalog.ReadGuests
                        => await ReadGuestsAsync(context, call, cancellationToken),
                    AssistantRetrieveToolCatalog.CompareLocations
                        => await CompareLocationsAsync(context, call, cancellationToken),
                    AssistantRetrieveToolCatalog.CompareAllLocations
                        => await CompareAllLocationsAsync(context, call, cancellationToken),
                    _ => Result(
                        call,
                        StatusPayload("unavailable", "Unknown retrieve tool.")
                    ),
                };
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                return Result(
                    call,
                    StatusPayload("unavailable", "Retrieve tool failed.")
                );
            }
        }

        private async Task<AssistantToolCallResult> ReadFeedbackAsync(
            AssistantRetrieveToolContext context,
            AssistantToolCallRequest call,
            CancellationToken cancellationToken
        )
        {
            if (context.OwnedLocationId is not int locationId)
            {
                return Result(
                    call,
                    StatusPayload("blocked", "Owned location scope is required.")
                );
            }

            if (!await CanViewAsync(
                    context.OwnerUserId,
                    OperatorAreaIds.Feedback,
                    locationId
                ))
            {
                return Result(call, StatusPayload("permission_blocked", null));
            }

            var retrieved = await _feedbackRetrieve.RetrieveAsync(
                locationId,
                context.FromUtc,
                context.ToUtc,
                cancellationToken
            );
            if (retrieved is AssistantFeedbackRetrieveResult.Failed)
            {
                return Result(call, StatusPayload("unavailable", null));
            }

            var evidence = retrieved is AssistantFeedbackRetrieveResult.Ok ok
                ? ok.Evidence
                : AssistantFeedbackEvidence.Empty;
            MergeEvidence(context, feedback: evidence);
            return Result(
                call,
                SuccessPayload(
                    AssistantLiveAnswerStructuredOutput.BuildEvidencePayload(
                        context.AccumulatedEvidence with
                        {
                            Feedback = evidence,
                            Offers = AssistantOffersEvidence.Empty,
                            Campaigns = AssistantCampaignsEvidence.Empty,
                            Capture = AssistantCaptureEvidence.Empty,
                            Home = AssistantHomeKpiEvidence.Empty,
                            Guests = AssistantGuestsEvidence.Empty,
                        }
                    )
                )
            );
        }

        private async Task<AssistantToolCallResult> ReadOffersAsync(
            AssistantRetrieveToolContext context,
            AssistantToolCallRequest call,
            CancellationToken cancellationToken
        )
        {
            if (context.OwnedLocationId is not int locationId)
            {
                return Result(
                    call,
                    StatusPayload("blocked", "Owned location scope is required.")
                );
            }

            if (!await CanViewAsync(
                    context.OwnerUserId,
                    OperatorAreaIds.Offers,
                    locationId
                ))
            {
                return Result(call, StatusPayload("permission_blocked", null));
            }

            var retrieved = await _offersRetrieve.RetrieveAsync(
                locationId,
                context.FromUtc,
                context.ToUtc,
                cancellationToken
            );
            if (retrieved is AssistantOffersRetrieveResult.Failed)
            {
                return Result(call, StatusPayload("unavailable", null));
            }

            var evidence = retrieved is AssistantOffersRetrieveResult.Ok ok
                ? ok.Evidence
                : AssistantOffersEvidence.Empty;
            MergeEvidence(context, offers: evidence);
            return Result(
                call,
                SuccessPayload(
                    AssistantLiveAnswerStructuredOutput.BuildEvidencePayload(
                        AssistantRetrievedEvidence.Empty with { Offers = evidence }
                    )
                )
            );
        }

        private async Task<AssistantToolCallResult> ReadCampaignsAsync(
            AssistantRetrieveToolContext context,
            AssistantToolCallRequest call,
            CancellationToken cancellationToken
        )
        {
            if (context.OwnedLocationId is not int locationId)
            {
                return Result(
                    call,
                    StatusPayload("blocked", "Owned location scope is required.")
                );
            }

            if (!await CanViewAsync(
                    context.OwnerUserId,
                    OperatorAreaIds.Campaigns,
                    locationId
                ))
            {
                return Result(call, StatusPayload("permission_blocked", null));
            }

            var includeCopy = context.IncludeCampaignCopy
                || ReadIncludeCampaignCopy(call.ArgumentsJson);
            var retrieved = await _campaignsRetrieve.RetrieveAsync(
                locationId,
                context.FromUtc,
                context.ToUtc,
                includeCopy,
                cancellationToken
            );
            if (retrieved is AssistantCampaignsRetrieveResult.Failed)
            {
                return Result(call, StatusPayload("unavailable", null));
            }

            var evidence = retrieved is AssistantCampaignsRetrieveResult.Ok ok
                ? ok.Evidence
                : AssistantCampaignsEvidence.Empty;
            MergeEvidence(context, campaigns: evidence);
            return Result(
                call,
                SuccessPayload(
                    AssistantLiveAnswerStructuredOutput.BuildEvidencePayload(
                        AssistantRetrievedEvidence.Empty with { Campaigns = evidence }
                    )
                )
            );
        }

        private async Task<AssistantToolCallResult> ReadCaptureAsync(
            AssistantRetrieveToolContext context,
            AssistantToolCallRequest call,
            CancellationToken cancellationToken
        )
        {
            if (context.OwnedLocationId is not int locationId)
            {
                return Result(
                    call,
                    StatusPayload("blocked", "Owned location scope is required.")
                );
            }

            if (!await CanViewAsync(
                    context.OwnerUserId,
                    OperatorAreaIds.Capture,
                    locationId
                ))
            {
                return Result(call, StatusPayload("permission_blocked", null));
            }

            var retrieved = await _captureRetrieve.RetrieveAsync(
                locationId,
                context.FromUtc,
                context.ToUtc,
                cancellationToken
            );
            if (retrieved is AssistantCaptureRetrieveResult.Failed)
            {
                return Result(call, StatusPayload("unavailable", null));
            }

            var evidence = retrieved is AssistantCaptureRetrieveResult.Ok ok
                ? ok.Evidence
                : AssistantCaptureEvidence.Empty;
            MergeEvidence(context, capture: evidence);
            return Result(
                call,
                SuccessPayload(
                    AssistantLiveAnswerStructuredOutput.BuildEvidencePayload(
                        AssistantRetrievedEvidence.Empty with { Capture = evidence }
                    )
                )
            );
        }

        private async Task<AssistantToolCallResult> ReadHomeAsync(
            AssistantRetrieveToolContext context,
            AssistantToolCallRequest call,
            CancellationToken cancellationToken
        )
        {
            if (context.OwnedLocationId is not int locationId)
            {
                return Result(
                    call,
                    StatusPayload("blocked", "Owned location scope is required.")
                );
            }

            // Home KPIs follow Capture/Feedback view; use Capture area as gate.
            if (!await CanViewAsync(
                    context.OwnerUserId,
                    OperatorAreaIds.Capture,
                    locationId
                )
                && !await CanViewAsync(
                    context.OwnerUserId,
                    OperatorAreaIds.Feedback,
                    locationId
                ))
            {
                return Result(call, StatusPayload("permission_blocked", null));
            }

            var retrieved = await _homeRetrieve.RetrieveAsync(
                locationId,
                context.FromUtc,
                context.ToUtc,
                cancellationToken
            );
            if (retrieved is AssistantHomeKpiRetrieveResult.Failed)
            {
                return Result(call, StatusPayload("unavailable", null));
            }

            var evidence = retrieved is AssistantHomeKpiRetrieveResult.Ok ok
                ? ok.Evidence
                : AssistantHomeKpiEvidence.Empty;
            MergeEvidence(context, home: evidence);
            return Result(
                call,
                SuccessPayload(
                    AssistantLiveAnswerStructuredOutput.BuildEvidencePayload(
                        AssistantRetrievedEvidence.Empty with { Home = evidence }
                    )
                )
            );
        }

        private async Task<AssistantToolCallResult> ReadGuestsAsync(
            AssistantRetrieveToolContext context,
            AssistantToolCallRequest call,
            CancellationToken cancellationToken
        )
        {
            if (context.OwnedLocationId is not int locationId)
            {
                return Result(
                    call,
                    StatusPayload("blocked", "Owned location scope is required.")
                );
            }

            if (!await CanViewAsync(
                    context.OwnerUserId,
                    OperatorAreaIds.Guests,
                    locationId
                ))
            {
                return Result(call, StatusPayload("permission_blocked", null));
            }

            var retrieved = await _guestsRetrieve.RetrieveAsync(
                locationId,
                cancellationToken
            );
            if (retrieved is AssistantGuestsRetrieveResult.Failed)
            {
                return Result(call, StatusPayload("unavailable", null));
            }

            var evidence = retrieved is AssistantGuestsRetrieveResult.Ok ok
                ? ok.Evidence
                : AssistantGuestsEvidence.Empty;
            MergeEvidence(context, guests: evidence);
            return Result(
                call,
                SuccessPayload(
                    AssistantLiveAnswerStructuredOutput.BuildEvidencePayload(
                        AssistantRetrievedEvidence.Empty with { Guests = evidence }
                    )
                )
            );
        }

        private async Task<AssistantToolCallResult> CompareLocationsAsync(
            AssistantRetrieveToolContext context,
            AssistantToolCallRequest call,
            CancellationToken cancellationToken
        )
        {
            var locationIds = context.CompareLocationIds
                .Where(id => context.OwnedLocations.Any(location => location.Id == id))
                .Distinct()
                .ToList();
            if (locationIds.Count == 0)
            {
                return Result(
                    call,
                    StatusPayload("blocked", "No authorised compare Locations.")
                );
            }

            var byId = context.OwnedLocations.ToDictionary(location => location.Id);
            var tasks = locationIds
                .Select(async locationId =>
                {
                    var pack = await RetrieveAllDomainsAsync(
                        context,
                        locationId,
                        cancellationToken
                    );
                    if (pack is null || !byId.TryGetValue(locationId, out var locationRef))
                    {
                        return null;
                    }

                    return new AssistantCompareLocationEvidence(
                        locationId,
                        locationRef.Name,
                        locationRef.CaptureStatus,
                        pack
                    );
                })
                .ToArray();

            var rows = (await Task.WhenAll(tasks))
                .Where(row => row is not null)
                .Cast<AssistantCompareLocationEvidence>()
                .ToList();
            if (rows.Count == 0)
            {
                return Result(call, StatusPayload("unavailable", null));
            }

            context.CompareLocations = rows;
            if (context.OwnedLocationId is int savedId)
            {
                var saved = rows.FirstOrDefault(row => row.OwnedLocationId == savedId);
                if (saved is not null)
                {
                    context.AccumulatedEvidence = saved.Evidence;
                }
            }

            var payload = new JsonObject
            {
                ["status"] = "success",
                ["compareLocations"] = new JsonArray(
                    rows.Select(row =>
                        {
                            var node = AssistantLiveAnswerStructuredOutput
                                .BuildEvidencePayload(row.Evidence);
                            node["ownedLocationName"] = row.LocationName;
                            return (JsonNode?)node;
                        })
                        .ToArray()
                ),
            };
            return Result(call, payload.ToJsonString(JsonOptions));
        }

        private async Task<AssistantToolCallResult> CompareAllLocationsAsync(
            AssistantRetrieveToolContext context,
            AssistantToolCallRequest call,
            CancellationToken cancellationToken
        )
        {
            var locationIds = context.CompareLocationIds
                .Where(id => context.OwnedLocations.Any(location => location.Id == id))
                .Distinct()
                .ToList();
            if (locationIds.Count == 0)
            {
                return Result(
                    call,
                    StatusPayload("blocked", "No authorised compare Locations.")
                );
            }

            var byId = context.OwnedLocations.ToDictionary(location => location.Id);
            var landed = new List<AssistantCompareLocationEvidence>();
            var failedNames = new List<string>();
            var notStartedNames = new List<string>();
            var startedAt = _clock.GetUtcNow();

            foreach (var locationId in locationIds)
            {
                if (!byId.TryGetValue(locationId, out var locationRef))
                {
                    continue;
                }

                if (_clock.GetUtcNow() - startedAt >= AssistantCompareAll.RetrieveBudget)
                {
                    notStartedNames.Add(locationRef.Name);
                    continue;
                }

                var pack = await RetrieveAllDomainsAsync(
                    context,
                    locationId,
                    cancellationToken
                );
                if (pack is null)
                {
                    failedNames.Add(locationRef.Name);
                    continue;
                }

                landed.Add(
                    new AssistantCompareLocationEvidence(
                        locationId,
                        locationRef.Name,
                        locationRef.CaptureStatus,
                        AssistantCompareAll.Thin(pack)
                    )
                );
            }

            if (landed.Count == 0)
            {
                context.FailedLocationNames = failedNames;
                context.NotStartedLocationNames = notStartedNames;
                return Result(call, StatusPayload("unavailable", null));
            }

            context.CompareLocations = landed;
            context.FailedLocationNames = failedNames;
            context.NotStartedLocationNames = notStartedNames;
            if (context.OwnedLocationId is int savedId)
            {
                var saved = landed.FirstOrDefault(row => row.OwnedLocationId == savedId);
                if (saved is not null)
                {
                    context.AccumulatedEvidence = saved.Evidence;
                }
            }

            var payload = new JsonObject
            {
                ["status"] = "success",
                ["compareAll"] = true,
                ["failedLocationNames"] = new JsonArray(
                    failedNames.Select(name => (JsonNode?)name).ToArray()
                ),
                ["notStartedLocationNames"] = new JsonArray(
                    notStartedNames.Select(name => (JsonNode?)name).ToArray()
                ),
                ["compareLocations"] = new JsonArray(
                    landed.Select(row =>
                        {
                            var node = AssistantLiveAnswerStructuredOutput
                                .BuildEvidencePayload(row.Evidence);
                            node["ownedLocationName"] = row.LocationName;
                            return (JsonNode?)node;
                        })
                        .ToArray()
                ),
            };
            return Result(call, payload.ToJsonString(JsonOptions));
        }

        private async Task<AssistantRetrievedEvidence?> RetrieveAllDomainsAsync(
            AssistantRetrieveToolContext context,
            int locationId,
            CancellationToken cancellationToken
        )
        {
            var feedbackTask = ReadFeedbackEvidenceAsync(
                context,
                locationId,
                cancellationToken
            );
            var offersTask = ReadOffersEvidenceAsync(context, locationId, cancellationToken);
            var campaignsTask = ReadCampaignsEvidenceAsync(
                context,
                locationId,
                cancellationToken
            );
            var captureTask = ReadCaptureEvidenceAsync(context, locationId, cancellationToken);
            var homeTask = ReadHomeEvidenceAsync(context, locationId, cancellationToken);
            var guestsTask = ReadGuestsEvidenceAsync(context, locationId, cancellationToken);

            await Task.WhenAll(
                feedbackTask,
                offersTask,
                campaignsTask,
                captureTask,
                homeTask,
                guestsTask
            );

            var feedback = await feedbackTask;
            var offers = await offersTask;
            var campaigns = await campaignsTask;
            var capture = await captureTask;
            var home = await homeTask;
            var guests = await guestsTask;
            if (feedback is null
                || offers is null
                || campaigns is null
                || capture is null
                || home is null
                || guests is null)
            {
                return null;
            }

            return new AssistantRetrievedEvidence(
                feedback,
                offers,
                campaigns,
                capture,
                home,
                guests
            );
        }

        private async Task<AssistantFeedbackEvidence?> ReadFeedbackEvidenceAsync(
            AssistantRetrieveToolContext context,
            int locationId,
            CancellationToken cancellationToken
        )
        {
            if (!await CanViewAsync(
                    context.OwnerUserId,
                    OperatorAreaIds.Feedback,
                    locationId
                ))
            {
                return AssistantFeedbackEvidence.Empty;
            }

            var result = await _feedbackRetrieve.RetrieveAsync(
                locationId,
                context.FromUtc,
                context.ToUtc,
                cancellationToken
            );
            if (result is AssistantFeedbackRetrieveResult.Failed)
            {
                return null;
            }

            return result is AssistantFeedbackRetrieveResult.Ok ok
                ? ok.Evidence
                : AssistantFeedbackEvidence.Empty;
        }

        private async Task<AssistantOffersEvidence?> ReadOffersEvidenceAsync(
            AssistantRetrieveToolContext context,
            int locationId,
            CancellationToken cancellationToken
        )
        {
            if (!await CanViewAsync(
                    context.OwnerUserId,
                    OperatorAreaIds.Offers,
                    locationId
                ))
            {
                return AssistantOffersEvidence.Empty;
            }

            var result = await _offersRetrieve.RetrieveAsync(
                locationId,
                context.FromUtc,
                context.ToUtc,
                cancellationToken
            );
            if (result is AssistantOffersRetrieveResult.Failed)
            {
                return null;
            }

            return result is AssistantOffersRetrieveResult.Ok ok
                ? ok.Evidence
                : AssistantOffersEvidence.Empty;
        }

        private async Task<AssistantCampaignsEvidence?> ReadCampaignsEvidenceAsync(
            AssistantRetrieveToolContext context,
            int locationId,
            CancellationToken cancellationToken
        )
        {
            if (!await CanViewAsync(
                    context.OwnerUserId,
                    OperatorAreaIds.Campaigns,
                    locationId
                ))
            {
                return AssistantCampaignsEvidence.Empty;
            }

            var result = await _campaignsRetrieve.RetrieveAsync(
                locationId,
                context.FromUtc,
                context.ToUtc,
                context.IncludeCampaignCopy,
                cancellationToken
            );
            if (result is AssistantCampaignsRetrieveResult.Failed)
            {
                return null;
            }

            return result is AssistantCampaignsRetrieveResult.Ok ok
                ? ok.Evidence
                : AssistantCampaignsEvidence.Empty;
        }

        private async Task<AssistantCaptureEvidence?> ReadCaptureEvidenceAsync(
            AssistantRetrieveToolContext context,
            int locationId,
            CancellationToken cancellationToken
        )
        {
            if (!await CanViewAsync(
                    context.OwnerUserId,
                    OperatorAreaIds.Capture,
                    locationId
                ))
            {
                return AssistantCaptureEvidence.Empty;
            }

            var result = await _captureRetrieve.RetrieveAsync(
                locationId,
                context.FromUtc,
                context.ToUtc,
                cancellationToken
            );
            if (result is AssistantCaptureRetrieveResult.Failed)
            {
                return null;
            }

            return result is AssistantCaptureRetrieveResult.Ok ok
                ? ok.Evidence
                : AssistantCaptureEvidence.Empty;
        }

        private async Task<AssistantHomeKpiEvidence?> ReadHomeEvidenceAsync(
            AssistantRetrieveToolContext context,
            int locationId,
            CancellationToken cancellationToken
        )
        {
            var result = await _homeRetrieve.RetrieveAsync(
                locationId,
                context.FromUtc,
                context.ToUtc,
                cancellationToken
            );
            if (result is AssistantHomeKpiRetrieveResult.Failed)
            {
                return null;
            }

            return result is AssistantHomeKpiRetrieveResult.Ok ok
                ? ok.Evidence
                : AssistantHomeKpiEvidence.Empty;
        }

        private async Task<AssistantGuestsEvidence?> ReadGuestsEvidenceAsync(
            AssistantRetrieveToolContext context,
            int locationId,
            CancellationToken cancellationToken
        )
        {
            if (!await CanViewAsync(
                    context.OwnerUserId,
                    OperatorAreaIds.Guests,
                    locationId
                ))
            {
                return AssistantGuestsEvidence.Empty;
            }

            var result = await _guestsRetrieve.RetrieveAsync(
                locationId,
                cancellationToken
            );
            if (result is AssistantGuestsRetrieveResult.Failed)
            {
                return null;
            }

            return result is AssistantGuestsRetrieveResult.Ok ok
                ? ok.Evidence
                : AssistantGuestsEvidence.Empty;
        }

        private async Task<bool> CanViewAsync(int userId, string areaId, int locationId)
        {
            var decision = await _permissions.AuthorizeLocationForUserAsync(
                userId,
                areaId,
                PermissionLevel.View,
                locationId
            );
            return decision.Status == RestaurantPermissionStatus.Allowed;
        }

        private static void MergeEvidence(
            AssistantRetrieveToolContext context,
            AssistantFeedbackEvidence? feedback = null,
            AssistantOffersEvidence? offers = null,
            AssistantCampaignsEvidence? campaigns = null,
            AssistantCaptureEvidence? capture = null,
            AssistantHomeKpiEvidence? home = null,
            AssistantGuestsEvidence? guests = null
        )
        {
            var current = context.AccumulatedEvidence;
            context.AccumulatedEvidence = new AssistantRetrievedEvidence(
                feedback ?? current.Feedback,
                offers ?? current.Offers,
                campaigns ?? current.Campaigns,
                capture ?? current.Capture,
                home ?? current.Home,
                guests ?? current.Guests
            );
        }

        private static bool ReadIncludeCampaignCopy(string argumentsJson)
        {
            if (string.IsNullOrWhiteSpace(argumentsJson))
            {
                return false;
            }

            try
            {
                using var document = JsonDocument.Parse(argumentsJson);
                if (document.RootElement.TryGetProperty(
                        "includeCampaignCopy",
                        out var value
                    )
                    && value.ValueKind is JsonValueKind.True or JsonValueKind.False)
                {
                    return value.GetBoolean();
                }
            }
            catch (JsonException)
            {
                // Ignore malformed args; server defaults apply.
            }

            return false;
        }

        private static AssistantToolCallResult Result(
            AssistantToolCallRequest call,
            string contentJson
        )
            => new(call.Id, call.Name, contentJson);

        private static string StatusPayload(string status, string? message)
        {
            var payload = new JsonObject { ["status"] = status };
            if (!string.IsNullOrWhiteSpace(message))
            {
                payload["message"] = message;
            }

            return payload.ToJsonString(JsonOptions);
        }

        private static string SuccessPayload(JsonObject evidence)
        {
            var payload = new JsonObject
            {
                ["status"] = "success",
                ["evidence"] = evidence,
            };
            return payload.ToJsonString(JsonOptions);
        }
    }
}
