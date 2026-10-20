using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Per-turn timing and token counts for the Assistant. One instance lives
    /// for one turn and flows through async calls (the live-answer provider is
    /// a singleton, so it reads <see cref="Current"/>). Holds no user text and
    /// no evidence values.
    /// </summary>
    public sealed class AssistantTurnTimings
    {
        public const string RoundOne = "round1";
        public const string RoundCorrective = "corrective";
        public const string RoundForced = "forced";
        public const string RoundFinal = "final";
        public const string RoundHistoryOnly = "historyOnly";
        public const string RoundTitle = "title";

        private static readonly AsyncLocal<AssistantTurnTimings?> CurrentTurn = new();

        private readonly object _gate = new();
        private readonly long _startedAt = Stopwatch.GetTimestamp();
        private readonly List<ModelCall> _modelCalls = [];
        private readonly List<ToolCall> _toolCalls = [];
        private long? _firstModelCallStartMs;
        private long _toolPhaseMs;

        public static AssistantTurnTimings? Current => CurrentTurn.Value;

        /// <summary>
        /// Starts timings for the calling async flow. The value reverts when
        /// the calling async method returns.
        /// </summary>
        public static AssistantTurnTimings Begin()
        {
            var timings = new AssistantTurnTimings();
            CurrentTurn.Value = timings;
            return timings;
        }

        public long ElapsedMs => (long)Stopwatch.GetElapsedTime(_startedAt).TotalMilliseconds;

        public int ModelCallCount
        {
            get
            {
                lock (_gate)
                {
                    return _modelCalls.Count;
                }
            }
        }

        public long MarkModelCallStart()
        {
            var startMs = ElapsedMs;
            lock (_gate)
            {
                _firstModelCallStartMs ??= startMs;
            }

            return startMs;
        }

        public void RecordModelCall(
            string round,
            long startMs,
            int? httpStatus,
            int retries,
            string? responseJson
        )
        {
            var usage = TokenUsage.FromResponse(responseJson);
            lock (_gate)
            {
                _modelCalls.Add(
                    new ModelCall(round, ElapsedMs - startMs, httpStatus, retries, usage)
                );
            }
        }

        /// <summary>
        /// Relabels model calls recorded at or after <paramref name="fromIndex"/>.
        /// Used when a caller knows the purpose (for example the title call).
        /// </summary>
        public void RelabelModelCallsFrom(int fromIndex, string round)
        {
            lock (_gate)
            {
                for (var i = Math.Max(0, fromIndex); i < _modelCalls.Count; i++)
                {
                    _modelCalls[i] = _modelCalls[i] with { Round = round };
                }
            }
        }

        public void RecordToolCall(string name, long durationMs, string status)
        {
            lock (_gate)
            {
                _toolCalls.Add(new ToolCall(name, durationMs, status));
            }
        }

        public void AddToolPhase(long durationMs)
        {
            lock (_gate)
            {
                _toolPhaseMs += durationMs;
            }
        }

        public JsonObject ToJson()
        {
            lock (_gate)
            {
                return new JsonObject
                {
                    ["totalMs"] = ElapsedMs,
                    ["msBeforeFirstModelCall"] = _firstModelCallStartMs,
                    ["modelCallCount"] = _modelCalls.Count,
                    ["modelMs"] = _modelCalls.Sum(call => call.DurationMs),
                    ["toolPhaseMs"] = _toolPhaseMs,
                    ["promptTokens"] = _modelCalls.Sum(call => call.Usage.PromptTokens ?? 0),
                    ["completionTokens"] =
                        _modelCalls.Sum(call => call.Usage.CompletionTokens ?? 0),
                    ["cachedTokens"] = _modelCalls.Sum(call => call.Usage.CachedTokens ?? 0),
                    ["reasoningTokens"] =
                        _modelCalls.Sum(call => call.Usage.ReasoningTokens ?? 0),
                    ["modelCalls"] = new JsonArray(
                        _modelCalls
                            .Select(call => (JsonNode?)new JsonObject
                            {
                                ["round"] = call.Round,
                                ["ms"] = call.DurationMs,
                                ["status"] = call.HttpStatus,
                                ["retries"] = call.Retries,
                                ["promptTokens"] = call.Usage.PromptTokens,
                                ["completionTokens"] = call.Usage.CompletionTokens,
                                ["cachedTokens"] = call.Usage.CachedTokens,
                                ["reasoningTokens"] = call.Usage.ReasoningTokens,
                            })
                            .ToArray()
                    ),
                    ["toolCalls"] = new JsonArray(
                        _toolCalls
                            .Select(call => (JsonNode?)new JsonObject
                            {
                                ["name"] = call.Name,
                                ["ms"] = call.DurationMs,
                                ["status"] = call.Status,
                            })
                            .ToArray()
                    ),
                };
            }
        }

        public Summary Summarize()
        {
            lock (_gate)
            {
                return new Summary(
                    ElapsedMs,
                    _firstModelCallStartMs,
                    _modelCalls.Count,
                    string.Join(",", _modelCalls.Select(call => call.Round)),
                    _modelCalls.Sum(call => call.DurationMs),
                    _modelCalls.Sum(call => call.Retries),
                    _toolCalls.Count,
                    _toolPhaseMs,
                    _modelCalls.Sum(call => call.Usage.PromptTokens ?? 0),
                    _modelCalls.Sum(call => call.Usage.CompletionTokens ?? 0),
                    _modelCalls.Sum(call => call.Usage.CachedTokens ?? 0),
                    _modelCalls.Sum(call => call.Usage.ReasoningTokens ?? 0)
                );
            }
        }

        /// <summary>
        /// Reads the <c>status</c> value from a retrieve tool payload. Payloads
        /// put <c>status</c> first, so this avoids a full JSON parse.
        /// </summary>
        public static string ToolStatus(string contentJson)
        {
            const string prefix = "{\"status\":\"";
            if (contentJson.StartsWith(prefix, StringComparison.Ordinal))
            {
                var end = contentJson.IndexOf('"', prefix.Length);
                if (end > prefix.Length)
                {
                    return contentJson[prefix.Length..end];
                }
            }

            return "unknown";
        }

        public sealed record Summary(
            long TotalMs,
            long? MsBeforeFirstModelCall,
            int ModelCallCount,
            string ModelRounds,
            long ModelMs,
            int ModelRetries,
            int ToolCallCount,
            long ToolPhaseMs,
            int PromptTokens,
            int CompletionTokens,
            int CachedTokens,
            int ReasoningTokens
        );

        private sealed record ModelCall(
            string Round,
            long DurationMs,
            int? HttpStatus,
            int Retries,
            TokenUsage Usage
        );

        private sealed record ToolCall(string Name, long DurationMs, string Status);

        private sealed record TokenUsage(
            int? PromptTokens,
            int? CompletionTokens,
            int? CachedTokens,
            int? ReasoningTokens
        )
        {
            private static readonly TokenUsage None = new(null, null, null, null);

            public static TokenUsage FromResponse(string? responseJson)
            {
                if (string.IsNullOrWhiteSpace(responseJson))
                {
                    return None;
                }

                try
                {
                    using var document = JsonDocument.Parse(responseJson);
                    if (!document.RootElement.TryGetProperty("usage", out var usage)
                        || usage.ValueKind != JsonValueKind.Object)
                    {
                        return None;
                    }

                    return new TokenUsage(
                        ReadInt(usage, "prompt_tokens"),
                        ReadInt(usage, "completion_tokens"),
                        usage.TryGetProperty("prompt_tokens_details", out var prompt)
                            ? ReadInt(prompt, "cached_tokens")
                            : null,
                        usage.TryGetProperty("completion_tokens_details", out var completion)
                            ? ReadInt(completion, "reasoning_tokens")
                            : null
                    );
                }
                catch (JsonException)
                {
                    return None;
                }
            }

            private static int? ReadInt(JsonElement parent, string name)
                => parent.ValueKind == JsonValueKind.Object
                    && parent.TryGetProperty(name, out var value)
                    && value.ValueKind == JsonValueKind.Number
                    && value.TryGetInt32(out var number)
                        ? number
                        : null;
        }
    }
}
