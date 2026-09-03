import { describe, expect, it } from "vitest"

import { buildReportsWeeklyBriefHubSecondary } from "@/lib/operatorReports/reportsWeeklyBriefPresentation"
import type { WeeklyBriefBody, WeeklyBriefMetrics } from "@/types/operatorHome"

const metrics: WeeklyBriefMetrics = {
  guestsJoined: 28,
  qrScanEvents: 72,
  feedbackCount: 42,
  positiveFeedbackCount: 30,
  neutralFeedbackCount: 8,
  negativeFeedbackCount: 4,
  needsAttentionCount: 2,
  detectedTagCounts: {},
  activeOffers: 3,
  claimsInWeek: 10,
  redemptionsInWeek: 4,
  campaignsSentInWeek: 1,
  campaignRecipientsReached: 40,
}

describe("buildReportsWeeklyBriefHubSecondary", () => {
  it("prefers the first domain summary with data", () => {
    const body: WeeklyBriefBody = {
      headline: "Headline",
      capture: { hasData: false, summary: "", echoedCounts: null },
      feedback: {
        hasData: true,
        summary: "Guests praised service speed.",
        echoedCounts: null,
      },
      offers: { hasData: false, summary: "", echoedCounts: null },
      campaigns: { hasData: false, summary: "", echoedCounts: null },
      watchNext: [],
    }
    expect(buildReportsWeeklyBriefHubSecondary(body, metrics)).toBe(
      "Guests praised service speed."
    )
  })

  it("falls back to metrics when no domain has data", () => {
    const body: WeeklyBriefBody = {
      headline: "Headline",
      capture: { hasData: false, summary: "", echoedCounts: null },
      feedback: { hasData: false, summary: "", echoedCounts: null },
      offers: { hasData: false, summary: "", echoedCounts: null },
      campaigns: { hasData: false, summary: "", echoedCounts: null },
      watchNext: [],
    }
    expect(buildReportsWeeklyBriefHubSecondary(body, metrics)).toBe(
      "42 feedback · 28 guests joined · 72 scans"
    )
  })
})
