import { describe, expect, it } from "vitest"

import { planAssistantActionNavigate } from "./assistantActionNavigate"
import type { OperatorAiAssistantAnalysisScope } from "./createOperatorAiAssistantModule"

const SCOPE: OperatorAiAssistantAnalysisScope = {
  ownedLocationId: 11,
  ownedLocationName: "Camden",
  reportingPeriod: { kind: "preset", presetId: "last7" },
}

describe("planAssistantActionNavigate", () => {
  it("maps view-feedback-set to Feedback with Family A and one inbox filter", () => {
    expect(
      planAssistantActionNavigate({
        action: {
          type: "view-feedback-set",
          label: "View 6 feedback items",
          count: 6,
          sentiment: "negative",
        },
        analysisScope: SCOPE,
        mode: "multi",
      })
    ).toEqual({
      path: "/multi-dashboard/feedback?location=11",
      selectLocationId: 11,
      feedbackDateRange: { kind: "preset", presetId: "last7" },
      feedbackInbox: { sentiment: "negative" },
    })
  })

  it("maps prepare-recovery to Feedback Needs attention and Family A", () => {
    expect(
      planAssistantActionNavigate({
        action: {
          type: "prepare-recovery",
          label: "Prepare recovery responses",
          tab: "needs-attention",
        },
        analysisScope: SCOPE,
        mode: "single",
      })
    ).toEqual({
      path: "/single-dashboard/feedback?location=11",
      selectLocationId: 11,
      feedbackDateRange: { kind: "preset", presetId: "last7" },
      feedbackInbox: { tab: "needs-attention" },
    })
  })

  it("maps next-step campaign and offer Actions to those pages", () => {
    expect(
      planAssistantActionNavigate({
        action: { type: "view-campaigns", label: "Open Campaigns" },
        analysisScope: SCOPE,
        mode: "multi",
      }).path
    ).toBe("/multi-dashboard/campaigns?location=11")
    expect(
      planAssistantActionNavigate({
        action: { type: "view-offers", label: "Open Offers" },
        analysisScope: SCOPE,
        mode: "multi",
      }).path
    ).toBe("/multi-dashboard/offers?location=11")
  })
})
