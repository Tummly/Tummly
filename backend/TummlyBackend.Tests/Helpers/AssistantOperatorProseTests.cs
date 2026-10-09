using TummlyBackend.Helpers;

namespace TummlyBackend.Tests.Helpers
{
    public class AssistantOperatorProseTests
    {
        [Fact]
        public void Scrub_RemovesActionDumpAndFieldNames()
        {
            const string body =
                """
                Feedback and guests: feedbackTotalCount and guestsTotalCount are zero.

                Actions

                review-capture (tab: null)
                prepare-recovery (tab: null)
                view-offers (tab: null)
                """;

            var cleaned = AssistantOperatorProse.Scrub(body);

            Assert.Contains("the feedback count", cleaned, StringComparison.Ordinal);
            Assert.Contains("the guest count", cleaned, StringComparison.Ordinal);
            Assert.DoesNotContain("feedbackTotalCount", cleaned, StringComparison.Ordinal);
            Assert.DoesNotContain("guestsTotalCount", cleaned, StringComparison.Ordinal);
            Assert.DoesNotContain("review-capture", cleaned, StringComparison.Ordinal);
            Assert.DoesNotContain("prepare-recovery", cleaned, StringComparison.Ordinal);
            Assert.DoesNotContain("tab:", cleaned, StringComparison.Ordinal);
            Assert.DoesNotContain("Actions", cleaned, StringComparison.Ordinal);
        }

        [Fact]
        public void AlignPeriod_ReplacesLastSevenDays()
        {
            var cleaned = AssistantOperatorProse.AlignPeriod(
                "Camden received 4 feedback items over the last 7 days.",
                "3 October 2026"
            );

            Assert.Contains("3 October 2026", cleaned, StringComparison.Ordinal);
            Assert.DoesNotContain("last 7 days", cleaned, StringComparison.OrdinalIgnoreCase);
        }
    }
}
