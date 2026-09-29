using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Tests.Services
{
    /// <summary>
    /// Local latency harness for retrieve-tools vs eager path.
    /// Uses Fake live-answer + simulated per-domain retrieve delay (InMemory is
    /// otherwise too fast). Azure credentials are not present locally, so LLM
    /// rounds are simulated with Fake.Delay / SecondRoundDelay.
    /// </summary>
    public partial class AssistantConversationServiceTests
    {
        [Fact(Skip = "Manual latency harness — run explicitly when measuring.")]
        public async Task Measure_RetrieveToolsLatency_WritesReport()
        {
            const int iterations = 21; // odd → clean median; first = warm-up
            const int domainDelayMs = 50;
            const int llmRoundMs = 600;

            var locationId = await SeedLocationAsync(ownerUserId: 8801, "Camden");
            await SeedAiGrantAsync(
                await RestaurantIdForLocationAsync(locationId),
                quantity: 500
            );

            SetDomainDelays(TimeSpan.FromMilliseconds(domainDelayMs));

            var cases = new (string Name, string Ask)[]
            {
                ("FeedbackOnly", "Summarise recent feedback"),
                ("OffersOnly", "Show offers performance and redemptions"),
                ("Mixed", "How are we doing — summarise feedback, offers, and campaigns"),
            };

            var report = new StringBuilder();
            report.AppendLine("# Assistant retrieve-tools latency (local harness)");
            report.AppendLine();
            report.AppendLine($"Date: {DateTime.UtcNow:O}");
            report.AppendLine(
                $"Assumptions: domain retrieve delay={domainDelayMs}ms each; "
                    + $"LLM round={llmRoundMs}ms (Fake). Azure endpoint not configured."
            );
            report.AppendLine(
                "Eager path: 1 LLM round. Tools path: 2 LLM rounds (tool choose + final)."
            );
            report.AppendLine(
                $"Iterations per cell: {iterations - 1} (after 1 warm-up)."
            );
            report.AppendLine();
            report.AppendLine(
                "| Ask | Path | p50 turn ms | p95 turn ms | median feedback calls | median offers calls |"
            );
            report.AppendLine(
                "| --- | --- | ---: | ---: | ---: | ---: |"
            );

            foreach (var (name, ask) in cases)
            {
                var eager = await MeasurePathAsync(
                    locationId,
                    ask,
                    toolsEnabled: false,
                    llmRoundMs,
                    iterations
                );
                var tools = await MeasurePathAsync(
                    locationId,
                    ask,
                    toolsEnabled: true,
                    llmRoundMs,
                    iterations
                );

                report.AppendLine(
                    $"| {name} | eager | {eager.P50:F0} | {eager.P95:F0} | {eager.MedianFeedbackCalls} | {eager.MedianOffersCalls} |"
                );
                report.AppendLine(
                    $"| {name} | tools | {tools.P50:F0} | {tools.P95:F0} | {tools.MedianFeedbackCalls} | {tools.MedianOffersCalls} |"
                );
                report.AppendLine(
                    $"| {name} | delta (tools-eager) | {tools.P50 - eager.P50:F0} | {tools.P95 - eager.P95:F0} |  |  |"
                );
            }

            report.AppendLine();
            report.AppendLine("## Verdict (this harness)");
            report.AppendLine(
                "With AskFocus already limiting eager domains, single-domain asks "
                    + "do not save retrieve wall time vs tools. Tools add a second "
                    + "LLM round, so projected turn time is higher unless real Azure "
                    + "prompt stuffing cost exceeds one extra round."
            );
            report.AppendLine(
                "Re-run against Azure OpenAI (set FeedbackClassification Endpoint/ApiKey) "
                    + "before flipping AssistantRetrieveToolsEnabled."
            );

            var outDir = Path.GetFullPath(
                Path.Combine(
                    AppContext.BaseDirectory,
                    "..",
                    "..",
                    "..",
                    "..",
                    "..",
                    ".scratch",
                    "assistant-retrieve-tools-latency"
                )
            );
            Directory.CreateDirectory(outDir);
            var outPath = Path.Combine(
                outDir,
                $"latency-{DateTime.UtcNow:yyyyMMdd-HHmmss}.md"
            );
            await File.WriteAllTextAsync(outPath, report.ToString());

            // Also emit to test output for the agent/user.
            Console.WriteLine(report.ToString());
            Console.WriteLine($"Wrote {outPath}");

            SetDomainDelays(TimeSpan.Zero);
            _fake.Delay = TimeSpan.Zero;
            _fake.SecondRoundDelay = TimeSpan.Zero;

            Assert.True(File.Exists(outPath));
        }

        private void SetDomainDelays(TimeSpan delay)
        {
            _retrieve.ArtificialDelay = delay;
            _offersRetrieve.ArtificialDelay = delay;
            _campaignsRetrieve.ArtificialDelay = delay;
            _captureRetrieve.ArtificialDelay = delay;
            _homeRetrieve.ArtificialDelay = delay;
            _guestsRetrieve.ArtificialDelay = delay;
        }

        private async Task<LatencyStats> MeasurePathAsync(
            int locationId,
            string ask,
            bool toolsEnabled,
            int llmRoundMs,
            int iterations
        )
        {
            var service = CreateConversationService(
                liveAnswerOptions: Options.Create(
                    new FeedbackClassificationSettings
                    {
                        AssistantRetrieveToolsEnabled = toolsEnabled,
                    }
                )
            );

            _fake.Delay = TimeSpan.FromMilliseconds(llmRoundMs);
            _fake.SecondRoundDelay = toolsEnabled
                ? TimeSpan.FromMilliseconds(llmRoundMs)
                : TimeSpan.Zero;

            var samples = new List<double>(iterations);
            var feedbackCalls = new List<int>(iterations);
            var offersCalls = new List<int>(iterations);

            for (var i = 0; i < iterations; i++)
            {
                _retrieve.Calls.Clear();
                _offersRetrieve.Calls.Clear();
                var sw = Stopwatch.StartNew();
                var outcome = await service.SendTurnAsync(
                    ownerUserId: 8801,
                    FirstSendRequest(locationId, ask)
                );
                sw.Stop();
                Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
                samples.Add(sw.Elapsed.TotalMilliseconds);
                feedbackCalls.Add(_retrieve.Calls.Count);
                offersCalls.Add(_offersRetrieve.Calls.Count);
            }

            // Drop warm-up.
            samples = samples.Skip(1).ToList();
            feedbackCalls = feedbackCalls.Skip(1).ToList();
            offersCalls = offersCalls.Skip(1).ToList();

            return new LatencyStats(
                Percentile(samples, 0.50),
                Percentile(samples, 0.95),
                (int)Math.Round(Median(feedbackCalls.Select(v => (double)v))),
                (int)Math.Round(Median(offersCalls.Select(v => (double)v)))
            );
        }

        private static double Percentile(List<double> values, double p)
        {
            var sorted = values.OrderBy(v => v).ToList();
            if (sorted.Count == 0)
            {
                return 0;
            }

            var index = (int)Math.Ceiling(p * sorted.Count) - 1;
            index = Math.Clamp(index, 0, sorted.Count - 1);
            return sorted[index];
        }

        private static double Median(IEnumerable<double> values)
        {
            var sorted = values.OrderBy(v => v).ToList();
            if (sorted.Count == 0)
            {
                return 0;
            }

            var mid = sorted.Count / 2;
            return sorted.Count % 2 == 0
                ? (sorted[mid - 1] + sorted[mid]) / 2.0
                : sorted[mid];
        }

        private readonly record struct LatencyStats(
            double P50,
            double P95,
            int MedianFeedbackCalls,
            int MedianOffersCalls
        );
    }
}
