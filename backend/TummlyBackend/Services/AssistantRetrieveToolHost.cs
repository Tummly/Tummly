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
    /// <remarks>
    /// With an <see cref="IServiceScopeFactory"/> (the DI path), reads run in
    /// parallel. Each read gets its own DI scope, so each has its own
    /// <c>DbContext</c>. Reads only return data; the context is changed on
    /// the calling thread after all reads end, in call order. Without a scope
    /// factory, reads run one at a time on the injected services.
    /// </remarks>
    public sealed class AssistantRetrieveToolHost : IAssistantRetrieveToolHost
    {
        /// <summary>Max reads open at the same time in one turn (SQL pool).</summary>
        private const int MaxConcurrentReads = 6;

        /// <summary>Max compare Locations read at the same time.</summary>
        private const int MaxConcurrentCompareLocations = 3;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = false,
        };

        private readonly Readers _readers;
        private readonly IServiceScopeFactory? _scopeFactory;
        private readonly SemaphoreSlim _readGate = new(MaxConcurrentReads);
        private readonly TimeProvider _clock;

        public AssistantRetrieveToolHost(
            IAssistantFeedbackRetrieve feedbackRetrieve,
            IAssistantOffersRetrieve offersRetrieve,
            IAssistantCampaignsRetrieve campaignsRetrieve,
            IAssistantCaptureRetrieve captureRetrieve,
            IAssistantHomeKpiRetrieve homeRetrieve,
            IAssistantGuestsRetrieve guestsRetrieve,
            IRestaurantPermissionHelper permissions,
            IBillingCreditsService? billingCredits = null,
            TimeProvider? timeProvider = null,
            IServiceScopeFactory? scopeFactory = null
        )
        {
            _readers = new Readers(
                feedbackRetrieve,
                offersRetrieve,
                campaignsRetrieve,
                captureRetrieve,
                homeRetrieve,
                guestsRetrieve,
                permissions,
                billingCredits
            );
            _clock = timeProvider ?? TimeProvider.System;
            _scopeFactory = scopeFactory;
        }

        private bool RunsInParallel => _scopeFactory is not null;

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

            var timings = AssistantTurnTimings.Current;
            var outcomes = await RunAllAsync(
                calls,
                async call =>
                {
                    var startMs = timings?.ElapsedMs ?? 0;
                    var apply = await ExecuteOneAsync(context, call, cancellationToken);
                    return (Apply: apply, Ms: (timings?.ElapsedMs ?? 0) - startMs);
                }
            );

            // Merge on this thread in call order: the model gets results and
            // evidence in the same order as a one-at-a-time run.
            var results = new List<AssistantToolCallResult>(calls.Count);
            for (var i = 0; i < calls.Count; i++)
            {
                var result = Result(calls[i], outcomes[i].Apply(context));
                results.Add(result);
                timings?.RecordToolCall(
                    calls[i].Name,
                    outcomes[i].Ms,
                    AssistantTurnTimings.ToolStatus(result.ContentJson)
                );
            }

            return results;
        }

        private async Task<ToolApply> ExecuteOneAsync(
            AssistantRetrieveToolContext context,
            AssistantToolCallRequest call,
            CancellationToken cancellationToken
        )
        {
            if (!AssistantRetrieveToolCatalog.IsKnown(call.Name))
            {
                return Fixed(StatusPayload("unavailable", "Unknown retrieve tool."));
            }

            try
            {
                return call.Name switch
                {
                    AssistantRetrieveToolCatalog.ReadFeedbackSummary
                        => await ReadFeedbackAsync(context, cancellationToken),
                    AssistantRetrieveToolCatalog.ReadOffers
                        => await ReadOffersAsync(context, cancellationToken),
                    AssistantRetrieveToolCatalog.ReadCampaigns
                        => await ReadCampaignsAsync(context, call, cancellationToken),
                    AssistantRetrieveToolCatalog.ReadCapturePerformance
                        => await ReadCaptureAsync(context, cancellationToken),
                    AssistantRetrieveToolCatalog.ReadHomeKpis
                        => await ReadHomeAsync(context, cancellationToken),
                    AssistantRetrieveToolCatalog.ReadGuests
                        => await ReadGuestsAsync(context, cancellationToken),
                    AssistantRetrieveToolCatalog.ReadBillingPlan
                        => await ReadBillingAsync(context, cancellationToken),
                    AssistantRetrieveToolCatalog.CompareLocations
                        => await CompareLocationsAsync(context, cancellationToken),
                    AssistantRetrieveToolCatalog.CompareAllLocations
                        => await CompareAllLocationsAsync(context, cancellationToken),
                    _ => Fixed(StatusPayload("unavailable", "Unknown retrieve tool.")),
                };
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                return Fixed(StatusPayload("unavailable", "Retrieve tool failed."));
            }
        }

        private Task<ToolApply> ReadFeedbackAsync(
            AssistantRetrieveToolContext context,
            CancellationToken cancellationToken
        )
            => ReadOwnedLocationAsync(
                context,
                async (readers, locationId) =>
                {
                    if (!await CanViewAsync(
                            context,
                            readers,
                            OperatorAreaIds.Feedback,
                            locationId
                        ))
                    {
                        return Fixed(StatusPayload("permission_blocked", null));
                    }

                    var retrieved = await readers.Feedback.RetrieveAsync(
                        locationId,
                        context.FromUtc,
                        context.ToUtc,
                        cancellationToken
                    );
                    if (retrieved is AssistantFeedbackRetrieveResult.Failed)
                    {
                        return Fixed(StatusPayload("unavailable", null));
                    }

                    var evidence = retrieved is AssistantFeedbackRetrieveResult.Ok ok
                        ? ok.Evidence
                        : AssistantFeedbackEvidence.Empty;
                    return target =>
                    {
                        MergeEvidence(target, feedback: evidence);
                        return SuccessPayload(
                            AssistantLiveAnswerStructuredOutput.BuildEvidencePayload(
                                target.AccumulatedEvidence with
                                {
                                    Feedback = evidence,
                                    Offers = AssistantOffersEvidence.Empty,
                                    Campaigns = AssistantCampaignsEvidence.Empty,
                                    Capture = AssistantCaptureEvidence.Empty,
                                    Home = AssistantHomeKpiEvidence.Empty,
                                    Guests = AssistantGuestsEvidence.Empty,
                                }
                            )
                        );
                    };
                },
                cancellationToken
            );

        private Task<ToolApply> ReadOffersAsync(
            AssistantRetrieveToolContext context,
            CancellationToken cancellationToken
        )
            => ReadOwnedLocationAsync(
                context,
                async (readers, locationId) =>
                {
                    if (!await CanViewAsync(
                            context,
                            readers,
                            OperatorAreaIds.Offers,
                            locationId
                        ))
                    {
                        return Fixed(StatusPayload("permission_blocked", null));
                    }

                    var retrieved = await readers.Offers.RetrieveAsync(
                        locationId,
                        context.FromUtc,
                        context.ToUtc,
                        cancellationToken
                    );
                    if (retrieved is AssistantOffersRetrieveResult.Failed)
                    {
                        return Fixed(StatusPayload("unavailable", null));
                    }

                    var evidence = retrieved is AssistantOffersRetrieveResult.Ok ok
                        ? ok.Evidence
                        : AssistantOffersEvidence.Empty;
                    return target =>
                    {
                        MergeEvidence(target, offers: evidence);
                        return SuccessPayload(
                            AssistantLiveAnswerStructuredOutput.BuildEvidencePayload(
                                AssistantRetrievedEvidence.Empty with { Offers = evidence }
                            )
                        );
                    };
                },
                cancellationToken
            );

        private Task<ToolApply> ReadCampaignsAsync(
            AssistantRetrieveToolContext context,
            AssistantToolCallRequest call,
            CancellationToken cancellationToken
        )
            => ReadOwnedLocationAsync(
                context,
                async (readers, locationId) =>
                {
                    if (!await CanViewAsync(
                            context,
                            readers,
                            OperatorAreaIds.Campaigns,
                            locationId
                        ))
                    {
                        return Fixed(StatusPayload("permission_blocked", null));
                    }

                    var includeCopy = context.IncludeCampaignCopy
                        || ReadIncludeCampaignCopy(call.ArgumentsJson);
                    var retrieved = await readers.Campaigns.RetrieveAsync(
                        locationId,
                        context.FromUtc,
                        context.ToUtc,
                        includeCopy,
                        cancellationToken
                    );
                    if (retrieved is AssistantCampaignsRetrieveResult.Failed)
                    {
                        return Fixed(StatusPayload("unavailable", null));
                    }

                    var evidence = retrieved is AssistantCampaignsRetrieveResult.Ok ok
                        ? ok.Evidence
                        : AssistantCampaignsEvidence.Empty;
                    return target =>
                    {
                        MergeEvidence(target, campaigns: evidence);
                        return SuccessPayload(
                            AssistantLiveAnswerStructuredOutput.BuildEvidencePayload(
                                AssistantRetrievedEvidence.Empty with { Campaigns = evidence }
                            )
                        );
                    };
                },
                cancellationToken
            );

        private Task<ToolApply> ReadCaptureAsync(
            AssistantRetrieveToolContext context,
            CancellationToken cancellationToken
        )
            => ReadOwnedLocationAsync(
                context,
                async (readers, locationId) =>
                {
                    if (!await CanViewAsync(
                            context,
                            readers,
                            OperatorAreaIds.Capture,
                            locationId
                        ))
                    {
                        return Fixed(StatusPayload("permission_blocked", null));
                    }

                    var retrieved = await readers.Capture.RetrieveAsync(
                        locationId,
                        context.FromUtc,
                        context.ToUtc,
                        cancellationToken
                    );
                    if (retrieved is AssistantCaptureRetrieveResult.Failed)
                    {
                        return Fixed(StatusPayload("unavailable", null));
                    }

                    var evidence = retrieved is AssistantCaptureRetrieveResult.Ok ok
                        ? ok.Evidence
                        : AssistantCaptureEvidence.Empty;
                    return target =>
                    {
                        MergeEvidence(target, capture: evidence);
                        return SuccessPayload(
                            AssistantLiveAnswerStructuredOutput.BuildEvidencePayload(
                                AssistantRetrievedEvidence.Empty with { Capture = evidence }
                            )
                        );
                    };
                },
                cancellationToken
            );

        private Task<ToolApply> ReadHomeAsync(
            AssistantRetrieveToolContext context,
            CancellationToken cancellationToken
        )
            => ReadOwnedLocationAsync(
                context,
                async (readers, locationId) =>
                {
                    // Home KPIs follow Capture/Feedback view; use Capture area as gate.
                    if (!await CanViewAsync(
                            context,
                            readers,
                            OperatorAreaIds.Capture,
                            locationId
                        )
                        && !await CanViewAsync(
                            context,
                            readers,
                            OperatorAreaIds.Feedback,
                            locationId
                        ))
                    {
                        return Fixed(StatusPayload("permission_blocked", null));
                    }

                    var retrieved = await readers.Home.RetrieveAsync(
                        locationId,
                        context.FromUtc,
                        context.ToUtc,
                        cancellationToken
                    );
                    if (retrieved is AssistantHomeKpiRetrieveResult.Failed)
                    {
                        return Fixed(StatusPayload("unavailable", null));
                    }

                    var evidence = retrieved is AssistantHomeKpiRetrieveResult.Ok ok
                        ? ok.Evidence
                        : AssistantHomeKpiEvidence.Empty;
                    return target =>
                    {
                        MergeEvidence(target, home: evidence);
                        return SuccessPayload(
                            AssistantLiveAnswerStructuredOutput.BuildEvidencePayload(
                                AssistantRetrievedEvidence.Empty with { Home = evidence }
                            )
                        );
                    };
                },
                cancellationToken
            );

        private Task<ToolApply> ReadGuestsAsync(
            AssistantRetrieveToolContext context,
            CancellationToken cancellationToken
        )
            => ReadOwnedLocationAsync(
                context,
                async (readers, locationId) =>
                {
                    if (!await CanViewAsync(
                            context,
                            readers,
                            OperatorAreaIds.Guests,
                            locationId
                        ))
                    {
                        return Fixed(StatusPayload("permission_blocked", null));
                    }

                    var retrieved = await readers.Guests.RetrieveAsync(
                        locationId,
                        cancellationToken
                    );
                    if (retrieved is AssistantGuestsRetrieveResult.Failed)
                    {
                        return Fixed(StatusPayload("unavailable", null));
                    }

                    var evidence = retrieved is AssistantGuestsRetrieveResult.Ok ok
                        ? ok.Evidence
                        : AssistantGuestsEvidence.Empty;
                    return target =>
                    {
                        MergeEvidence(target, guests: evidence);
                        return SuccessPayload(
                            AssistantLiveAnswerStructuredOutput.BuildEvidencePayload(
                                AssistantRetrievedEvidence.Empty with { Guests = evidence }
                            )
                        );
                    };
                },
                cancellationToken
            );

        private Task<ToolApply> ReadBillingAsync(
            AssistantRetrieveToolContext context,
            CancellationToken cancellationToken
        )
        {
            if (_readers.Billing is null)
            {
                return Task.FromResult(
                    Fixed(StatusPayload("unavailable", "Billing retrieve is not configured."))
                );
            }

            return WithReadersAsync(
                async readers =>
                {
                    var billingCredits = readers.Billing!;
                    var view = await readers.Permissions.AuthorizeUserAsync(
                        context.OwnerUserId,
                        OperatorAreaIds.BillingCredits,
                        PermissionLevel.View
                    );
                    if (view.Status != RestaurantPermissionStatus.Allowed)
                    {
                        return Fixed(StatusPayload("permission_blocked", null));
                    }

                    var manage = await readers.Permissions.AuthorizeUserAsync(
                        context.OwnerUserId,
                        OperatorAreaIds.BillingCredits,
                        PermissionLevel.Manage
                    );
                    var actorCanManage = manage.Status == RestaurantPermissionStatus.Allowed;

                    var page = await billingCredits.GetPageAsync(
                        context.OwnerUserId,
                        view.RestaurantId,
                        actorCanManage
                    );
                    if (page is null)
                    {
                        return Fixed(StatusPayload("unavailable", null));
                    }

                    var plan = page.PlanSubscription;
                    var usage = await billingCredits.GetUsageAsync(view.RestaurantId);
                    var emailUsed = 0;
                    var smsUsed = 0;
                    var aiUsed = 0;
                    string? usagePeriodLabel = null;
                    var hasUsage = false;
                    if (usage is not null)
                    {
                        hasUsage = true;
                        usagePeriodLabel = usage.PeriodLabel;
                        foreach (var channel in usage.Channels)
                        {
                            if (string.Equals(
                                    channel.Channel,
                                    CreditChannels.Email,
                                    StringComparison.OrdinalIgnoreCase
                                ))
                            {
                                emailUsed = channel.UsedThisCycle;
                            }
                            else if (string.Equals(
                                    channel.Channel,
                                    CreditChannels.Sms,
                                    StringComparison.OrdinalIgnoreCase
                                ))
                            {
                                smsUsed = channel.UsedThisCycle;
                            }
                            else if (string.Equals(
                                    channel.Channel,
                                    CreditChannels.Ai,
                                    StringComparison.OrdinalIgnoreCase
                                ))
                            {
                                aiUsed = channel.UsedThisCycle;
                            }
                        }
                    }

                    var evidence = new AssistantBillingEvidence(
                        plan.SubscriptionPlan,
                        plan.BillingStatus,
                        plan.EmailCreditsRemaining,
                        plan.SmsCreditsRemaining,
                        plan.AiCreditsRemaining,
                        plan.BillingCycle,
                        plan.RenewalDateLabel,
                        plan.IsPilot,
                        plan.ScheduledChangeLine,
                        plan.PlanPriceNet,
                        emailUsed,
                        smsUsed,
                        aiUsed,
                        usagePeriodLabel,
                        hasUsage
                    );
                    return (ToolApply)(target =>
                    {
                        MergeEvidence(target, billing: evidence);
                        return SuccessPayload(
                            AssistantLiveAnswerStructuredOutput.BuildEvidencePayload(
                                AssistantRetrievedEvidence.Empty with { Billing = evidence }
                            )
                        );
                    });
                },
                cancellationToken
            );
        }

        private async Task<ToolApply> CompareLocationsAsync(
            AssistantRetrieveToolContext context,
            CancellationToken cancellationToken
        )
        {
            var locationIds = context.CompareLocationIds
                .Where(id => context.OwnedLocations.Any(location => location.Id == id))
                .Distinct()
                .ToList();
            if (locationIds.Count == 0)
            {
                return Fixed(StatusPayload("blocked", "No authorised compare Locations."));
            }

            var byId = context.OwnedLocations.ToDictionary(location => location.Id);
            var packs = new AssistantRetrievedEvidence?[locationIds.Count];
            await RunLimitedAsync(
                locationIds.Count,
                MaxConcurrentCompareLocations,
                async index => packs[index] = await RetrieveAllDomainsAsync(
                    context,
                    locationIds[index],
                    cancellationToken
                ),
                cancellationToken
            );

            var rows = new List<AssistantCompareLocationEvidence>();
            for (var i = 0; i < locationIds.Count; i++)
            {
                if (packs[i] is not { } pack
                    || !byId.TryGetValue(locationIds[i], out var locationRef))
                {
                    continue;
                }

                rows.Add(
                    new AssistantCompareLocationEvidence(
                        locationIds[i],
                        locationRef.Name,
                        locationRef.CaptureStatus,
                        pack
                    )
                );
            }

            if (rows.Count == 0)
            {
                return Fixed(StatusPayload("unavailable", null));
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
            }.ToJsonString(JsonOptions);
            return target =>
            {
                target.CompareLocations = rows;
                if (target.OwnedLocationId is int savedId)
                {
                    var saved = rows.FirstOrDefault(row => row.OwnedLocationId == savedId);
                    if (saved is not null)
                    {
                        target.AccumulatedEvidence = saved.Evidence;
                    }
                }

                return payload;
            };
        }

        private async Task<ToolApply> CompareAllLocationsAsync(
            AssistantRetrieveToolContext context,
            CancellationToken cancellationToken
        )
        {
            var locationIds = context.CompareLocationIds
                .Where(id => context.OwnedLocations.Any(location => location.Id == id))
                .Distinct()
                .ToList();
            if (locationIds.Count == 0)
            {
                return Fixed(StatusPayload("blocked", "No authorised compare Locations."));
            }

            var byId = context.OwnedLocations.ToDictionary(location => location.Id);
            var locations = locationIds
                .Where(byId.ContainsKey)
                .Select(id => byId[id])
                .ToList();
            var started = new bool[locations.Count];
            var packs = new AssistantRetrievedEvidence?[locations.Count];
            var startedAt = _clock.GetUtcNow();

            // A Location that has not started when the budget ends is "not
            // started". Running Locations finish.
            await RunLimitedAsync(
                locations.Count,
                MaxConcurrentCompareLocations,
                async index =>
                {
                    if (_clock.GetUtcNow() - startedAt >= AssistantCompareAll.RetrieveBudget)
                    {
                        return;
                    }

                    started[index] = true;
                    packs[index] = await RetrieveAllDomainsAsync(
                        context,
                        locations[index].Id,
                        cancellationToken
                    );
                },
                cancellationToken
            );

            var landed = new List<AssistantCompareLocationEvidence>();
            var failedNames = new List<string>();
            var notStartedNames = new List<string>();
            for (var i = 0; i < locations.Count; i++)
            {
                var locationRef = locations[i];
                if (!started[i])
                {
                    notStartedNames.Add(locationRef.Name);
                }
                else if (packs[i] is not { } pack)
                {
                    failedNames.Add(locationRef.Name);
                }
                else
                {
                    landed.Add(
                        new AssistantCompareLocationEvidence(
                            locationRef.Id,
                            locationRef.Name,
                            locationRef.CaptureStatus,
                            AssistantCompareAll.Thin(pack)
                        )
                    );
                }
            }

            if (landed.Count == 0)
            {
                var unavailable = StatusPayload("unavailable", null);
                return target =>
                {
                    target.FailedLocationNames = failedNames;
                    target.NotStartedLocationNames = notStartedNames;
                    return unavailable;
                };
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
            }.ToJsonString(JsonOptions);
            return target =>
            {
                target.CompareLocations = landed;
                target.FailedLocationNames = failedNames;
                target.NotStartedLocationNames = notStartedNames;
                if (target.OwnedLocationId is int savedId)
                {
                    var saved = landed.FirstOrDefault(row => row.OwnedLocationId == savedId);
                    if (saved is not null)
                    {
                        target.AccumulatedEvidence = saved.Evidence;
                    }
                }

                return payload;
            };
        }

        private async Task<AssistantRetrievedEvidence?> RetrieveAllDomainsAsync(
            AssistantRetrieveToolContext context,
            int locationId,
            CancellationToken cancellationToken
        )
        {
            Task<T> Read<T>(
                Func<
                    AssistantRetrieveToolContext,
                    Readers,
                    int,
                    CancellationToken,
                    Task<T>
                > read
            )
                => WithReadersAsync(
                    readers => read(context, readers, locationId, cancellationToken),
                    cancellationToken
                );

            AssistantFeedbackEvidence? feedback;
            AssistantOffersEvidence? offers;
            AssistantCampaignsEvidence? campaigns;
            AssistantCaptureEvidence? capture;
            AssistantHomeKpiEvidence? home;
            AssistantGuestsEvidence? guests;
            if (RunsInParallel)
            {
                var feedbackTask = Read(ReadFeedbackEvidenceAsync);
                var offersTask = Read(ReadOffersEvidenceAsync);
                var campaignsTask = Read(ReadCampaignsEvidenceAsync);
                var captureTask = Read(ReadCaptureEvidenceAsync);
                var homeTask = Read(ReadHomeEvidenceAsync);
                var guestsTask = Read(ReadGuestsEvidenceAsync);
                await Task.WhenAll(
                    feedbackTask,
                    offersTask,
                    campaignsTask,
                    captureTask,
                    homeTask,
                    guestsTask
                );
                feedback = feedbackTask.Result;
                offers = offersTask.Result;
                campaigns = campaignsTask.Result;
                capture = captureTask.Result;
                home = homeTask.Result;
                guests = guestsTask.Result;
            }
            else
            {
                feedback = await Read(ReadFeedbackEvidenceAsync);
                offers = await Read(ReadOffersEvidenceAsync);
                campaigns = await Read(ReadCampaignsEvidenceAsync);
                capture = await Read(ReadCaptureEvidenceAsync);
                home = await Read(ReadHomeEvidenceAsync);
                guests = await Read(ReadGuestsEvidenceAsync);
            }

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

        private static async Task<AssistantFeedbackEvidence?> ReadFeedbackEvidenceAsync(
            AssistantRetrieveToolContext context,
            Readers readers,
            int locationId,
            CancellationToken cancellationToken
        )
        {
            if (!await CanViewAsync(
                    context,
                    readers,
                    OperatorAreaIds.Feedback,
                    locationId
                ))
            {
                return AssistantFeedbackEvidence.Empty;
            }

            var result = await readers.Feedback.RetrieveAsync(
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

        private static async Task<AssistantOffersEvidence?> ReadOffersEvidenceAsync(
            AssistantRetrieveToolContext context,
            Readers readers,
            int locationId,
            CancellationToken cancellationToken
        )
        {
            if (!await CanViewAsync(
                    context,
                    readers,
                    OperatorAreaIds.Offers,
                    locationId
                ))
            {
                return AssistantOffersEvidence.Empty;
            }

            var result = await readers.Offers.RetrieveAsync(
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

        private static async Task<AssistantCampaignsEvidence?> ReadCampaignsEvidenceAsync(
            AssistantRetrieveToolContext context,
            Readers readers,
            int locationId,
            CancellationToken cancellationToken
        )
        {
            if (!await CanViewAsync(
                    context,
                    readers,
                    OperatorAreaIds.Campaigns,
                    locationId
                ))
            {
                return AssistantCampaignsEvidence.Empty;
            }

            var result = await readers.Campaigns.RetrieveAsync(
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

        private static async Task<AssistantCaptureEvidence?> ReadCaptureEvidenceAsync(
            AssistantRetrieveToolContext context,
            Readers readers,
            int locationId,
            CancellationToken cancellationToken
        )
        {
            if (!await CanViewAsync(
                    context,
                    readers,
                    OperatorAreaIds.Capture,
                    locationId
                ))
            {
                return AssistantCaptureEvidence.Empty;
            }

            var result = await readers.Capture.RetrieveAsync(
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

        private static async Task<AssistantHomeKpiEvidence?> ReadHomeEvidenceAsync(
            AssistantRetrieveToolContext context,
            Readers readers,
            int locationId,
            CancellationToken cancellationToken
        )
        {
            var result = await readers.Home.RetrieveAsync(
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

        private static async Task<AssistantGuestsEvidence?> ReadGuestsEvidenceAsync(
            AssistantRetrieveToolContext context,
            Readers readers,
            int locationId,
            CancellationToken cancellationToken
        )
        {
            if (!await CanViewAsync(
                    context,
                    readers,
                    OperatorAreaIds.Guests,
                    locationId
                ))
            {
                return AssistantGuestsEvidence.Empty;
            }

            var result = await readers.Guests.RetrieveAsync(
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

        /// <summary>
        /// One permission query per <c>(area, location)</c> per turn. Parallel
        /// reads that ask for the same key wait for the same query.
        /// </summary>
        private static Task<bool> CanViewAsync(
            AssistantRetrieveToolContext context,
            Readers readers,
            string areaId,
            int locationId
        )
            => context.ViewPermissions
                .GetOrAdd(
                    (areaId, locationId),
                    key => new Lazy<Task<bool>>(
                        () => AuthorizeViewAsync(
                            readers.Permissions,
                            context.OwnerUserId,
                            key.AreaId,
                            key.LocationId
                        )
                    )
                )
                .Value;

        private static async Task<bool> AuthorizeViewAsync(
            IRestaurantPermissionHelper permissions,
            int userId,
            string areaId,
            int locationId
        )
        {
            var decision = await permissions.AuthorizeLocationForUserAsync(
                userId,
                areaId,
                PermissionLevel.View,
                locationId
            );
            return decision.Status == RestaurantPermissionStatus.Allowed;
        }

        private Task<ToolApply> ReadOwnedLocationAsync(
            AssistantRetrieveToolContext context,
            Func<Readers, int, Task<ToolApply>> read,
            CancellationToken cancellationToken
        )
        {
            if (context.OwnedLocationId is not int locationId)
            {
                return Task.FromResult(
                    Fixed(StatusPayload("blocked", "Owned location scope is required."))
                );
            }

            return WithReadersAsync(readers => read(readers, locationId), cancellationToken);
        }

        /// <summary>
        /// Runs one read. In parallel mode the read gets its own DI scope (own
        /// <c>DbContext</c>) and a slot in the per-turn read gate. Only leaf
        /// reads call this, so a slot is never held while waiting for another.
        /// </summary>
        private async Task<T> WithReadersAsync<T>(
            Func<Readers, Task<T>> read,
            CancellationToken cancellationToken
        )
        {
            if (_scopeFactory is null)
            {
                return await read(_readers);
            }

            await _readGate.WaitAsync(cancellationToken);
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var services = scope.ServiceProvider;
                return await read(
                    new Readers(
                        services.GetRequiredService<IAssistantFeedbackRetrieve>(),
                        services.GetRequiredService<IAssistantOffersRetrieve>(),
                        services.GetRequiredService<IAssistantCampaignsRetrieve>(),
                        services.GetRequiredService<IAssistantCaptureRetrieve>(),
                        services.GetRequiredService<IAssistantHomeKpiRetrieve>(),
                        services.GetRequiredService<IAssistantGuestsRetrieve>(),
                        services.GetRequiredService<IRestaurantPermissionHelper>(),
                        _readers.Billing is null
                            ? null
                            : services.GetService<IBillingCreditsService>()
                    )
                );
            }
            finally
            {
                _readGate.Release();
            }
        }

        /// <summary>
        /// Runs <paramref name="work"/> for each item and returns results in
        /// input order: all at once in parallel mode, else one at a time.
        /// </summary>
        private async Task<TOut[]> RunAllAsync<TIn, TOut>(
            IReadOnlyList<TIn> items,
            Func<TIn, Task<TOut>> work
        )
        {
            if (RunsInParallel)
            {
                return await Task.WhenAll(items.Select(work));
            }

            var results = new TOut[items.Count];
            for (var i = 0; i < items.Count; i++)
            {
                results[i] = await work(items[i]);
            }

            return results;
        }

        /// <summary>
        /// Runs <paramref name="work"/> for indexes 0..count-1, at most
        /// <paramref name="maxConcurrent"/> at a time, starting in index order.
        /// One at a time when not in parallel mode.
        /// </summary>
        private async Task RunLimitedAsync(
            int count,
            int maxConcurrent,
            Func<int, Task> work,
            CancellationToken cancellationToken
        )
        {
            if (!RunsInParallel || maxConcurrent <= 1)
            {
                for (var i = 0; i < count; i++)
                {
                    await work(i);
                }

                return;
            }

            using var slots = new SemaphoreSlim(maxConcurrent);
            var running = new List<Task>(count);
            for (var i = 0; i < count; i++)
            {
                // Wait for a slot before starting, so Locations start in order.
                await slots.WaitAsync(cancellationToken);
                var index = i;
                running.Add(RunAndReleaseAsync(work, index, slots));
            }

            await Task.WhenAll(running);
        }

        private static async Task RunAndReleaseAsync(
            Func<int, Task> work,
            int index,
            SemaphoreSlim slots
        )
        {
            try
            {
                await work(index);
            }
            finally
            {
                slots.Release();
            }
        }

        private static void MergeEvidence(
            AssistantRetrieveToolContext context,
            AssistantFeedbackEvidence? feedback = null,
            AssistantOffersEvidence? offers = null,
            AssistantCampaignsEvidence? campaigns = null,
            AssistantCaptureEvidence? capture = null,
            AssistantHomeKpiEvidence? home = null,
            AssistantGuestsEvidence? guests = null,
            AssistantBillingEvidence? billing = null
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
            )
            {
                Billing = billing ?? current.Billing,
            };
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

        private static ToolApply Fixed(string contentJson) => _ => contentJson;

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

        /// <summary>
        /// Applies one finished read to the turn context (merge evidence) and
        /// returns the tool result JSON. Runs on the calling thread, in order.
        /// </summary>
        private delegate string ToolApply(AssistantRetrieveToolContext context);

        private sealed record Readers(
            IAssistantFeedbackRetrieve Feedback,
            IAssistantOffersRetrieve Offers,
            IAssistantCampaignsRetrieve Campaigns,
            IAssistantCaptureRetrieve Capture,
            IAssistantHomeKpiRetrieve Home,
            IAssistantGuestsRetrieve Guests,
            IRestaurantPermissionHelper Permissions,
            IBillingCreditsService? Billing
        );
    }
}
