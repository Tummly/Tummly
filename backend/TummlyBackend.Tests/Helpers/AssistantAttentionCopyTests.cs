using TummlyBackend.Helpers;

namespace TummlyBackend.Tests.Helpers
{
    public class AssistantAttentionCopyTests
    {
        [Fact]
        public void ResolveWeeklyBriefEmptyTitle_Pending_NeverClaimsReadyOnGenerateDay()
        {
            Assert.Equal(
                AssistantAttentionCopy.WeeklyBriefEmptyTitlePending,
                AssistantAttentionCopy.ResolveWeeklyBriefEmptyTitle(
                    isGenerateDay: false
                )
            );
            Assert.Equal(
                AssistantAttentionCopy.WeeklyBriefEmptyTitleGenerateDay,
                AssistantAttentionCopy.ResolveWeeklyBriefEmptyTitle(
                    isGenerateDay: true
                )
            );
            Assert.DoesNotContain(
                "will be ready on Monday",
                AssistantAttentionCopy.ResolveWeeklyBriefEmptyTitle(
                    isGenerateDay: true
                ),
                StringComparison.OrdinalIgnoreCase
            );
        }

        [Fact]
        public void ResolveWeeklyBriefEmptyTitle_Pilot_SurfacesExclusion()
        {
            Assert.Equal(
                AssistantAttentionCopy.WeeklyBriefEmptyTitlePilot,
                AssistantAttentionCopy.ResolveWeeklyBriefEmptyTitle(
                    isGenerateDay: true,
                    isPilot: true
                )
            );
            Assert.Equal(
                AssistantAttentionCopy.WeeklyBriefEmptyHelperPilot,
                AssistantAttentionCopy.ResolveWeeklyBriefEmptyHelper(
                    isGenerateDay: false,
                    isPilot: true
                )
            );
        }

        [Fact]
        public void WeeklyBriefEmptyBody_UsesResolvedCopy()
        {
            var body = AssistantAttentionCopy.WeeklyBriefEmptyBody(
                "Harbour",
                "monday:2026-08-10",
                isGenerateDay: true,
                isPilot: false
            );

            Assert.Contains(
                AssistantAttentionCopy.WeeklyBriefEmptyTitleGenerateDay,
                body,
                StringComparison.Ordinal
            );
            Assert.DoesNotContain(
                "will be ready on Monday",
                body,
                StringComparison.OrdinalIgnoreCase
            );
        }
    }
}
