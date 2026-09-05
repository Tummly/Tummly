import type {
  WeeklyBriefBody,
  WeeklyBriefMetrics,
} from "@/types/operatorHome"

export const REPORTS_WEEKLY_BRIEF_LOAD_ERROR_MESSAGE =
  "Could not load your weekly brief. Please try again."

export const REPORTS_HUB_GUEST_LOOP_COPY = {
  sectionTitle: "This week's guest loop",
  emptyHelper:
    "Generate a weekly brief to see a short summary of last week's guest loop.",
  viewWeeklyBrief: "View weekly brief",
  generateBrief: "Generate brief",
  createCampaign: "Create campaign",
  retry: "Retry",
} as const

const DOMAIN_ORDER = [
  "feedback",
  "capture",
  "offers",
  "campaigns",
] as const

/**
 * Hub card secondary — short line from a domain summary or metrics.
 * Not the full four-domain Home layout (lock 02).
 */
export function buildReportsWeeklyBriefHubSecondary(
  body: WeeklyBriefBody,
  metrics: WeeklyBriefMetrics
): string {
  for (const key of DOMAIN_ORDER) {
    const section = body[key]
    const summary = section.summary?.trim()
    if (section.hasData && summary) {
      return summary.length > 160 ? `${summary.slice(0, 157)}…` : summary
    }
  }

  return `${metrics.feedbackCount} feedback · ${metrics.guestsJoined} guests joined · ${metrics.qrScanEvents} scans`
}
