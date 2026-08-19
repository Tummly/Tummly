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
      })
    ).toEqual({
      path: "/multi-dashboard/campaigns?location=11",
      selectLocationId: 11,
    })
    expect(
      planAssistantActionNavigate({
        action: { type: "view-offers", label: "Open Offers" },
        analysisScope: SCOPE,
        mode: "multi",
      })
    ).toEqual({
      path: "/multi-dashboard/offers?location=11",
      selectLocationId: 11,
    })
  })

  it("maps review-campaign to Campaigns with Campaign Detail id, not Drafts view", () => {
    expect(
      planAssistantActionNavigate({
        action: {
          type: "review-campaign",
          label: "Review campaign draft",
          campaignId: 41,
        },
        analysisScope: SCOPE,
        mode: "multi",
      })
    ).toEqual({
      path: "/multi-dashboard/campaigns?location=11",
      selectLocationId: 11,
      campaigns: { previewCampaignId: 41 },
    })
  })

  it("maps draft-offer to Offers Drafts without a view query", () => {
    expect(
      planAssistantActionNavigate({
        action: { type: "draft-offer", label: "Create offer draft" },
        analysisScope: SCOPE,
        mode: "multi",
      })
    ).toEqual({
      path: "/multi-dashboard/offers?location=11",
      selectLocationId: 11,
      offers: { view: "drafts" },
    })
  })

  it("maps open-recovery to Feedback with one-shot recovery draft payload", () => {
    const recoveryDraft = {
      feedbackId: 42,
      intent: "respond-to-guest" as const,
      channel: "email" as const,
      purpose: "acknowledge_feedback" as const,
      tone: "warm_and_apologetic" as const,
      includeNotes: "",
      subject: "Hi",
      message: "Thanks",
    }
    expect(
      planAssistantActionNavigate({
        action: {
          type: "open-recovery",
          label: "Review recovery",
          feedbackId: 42,
          intent: "respond-to-guest",
        },
        analysisScope: SCOPE,
        mode: "multi",
        recoveryDraft,
      })
    ).toEqual({
      path: "/multi-dashboard/feedback?location=11",
      selectLocationId: 11,
      recoveryDraft,
    })
  })

  it("maps view-offer to Offer Details without list dates", () => {
    expect(
      planAssistantActionNavigate({
        action: { type: "view-offer", label: "View offer", offerId: 22 },
        analysisScope: SCOPE,
        mode: "multi",
      })
    ).toEqual({
      path: "/multi-dashboard/offers/22?location=11",
      selectLocationId: 11,
    })
  })

  it("maps view-capture in multi to the nested location page and Family A", () => {
    expect(
      planAssistantActionNavigate({
        action: { type: "view-capture", label: "View Capture" },
        analysisScope: SCOPE,
        mode: "multi",
      })
    ).toEqual({
      path: "/multi-dashboard/capture/locations/11?location=11",
      selectLocationId: 11,
      captureDateRange: { kind: "preset", presetId: "last7" },
    })
  })

  it("maps view-capture in single to the location Capture page and Family A", () => {
    expect(
      planAssistantActionNavigate({
        action: { type: "view-capture", label: "View Capture" },
        analysisScope: SCOPE,
        mode: "single",
      })
    ).toEqual({
      path: "/single-dashboard/capture?location=11",
      selectLocationId: 11,
      captureDateRange: { kind: "preset", presetId: "last7" },
    })
  })

  it("maps view-guests to Guests with location and optional filters, not a date range", () => {
    expect(
      planAssistantActionNavigate({
        action: {
          type: "view-guests",
          label: "View guests",
          marketingEligible: true,
          smartGroup: "positive-feedback",
        },
        analysisScope: SCOPE,
        mode: "multi",
      })
    ).toEqual({
      path: "/multi-dashboard/guests?location=11",
      selectLocationId: 11,
      guests: {
        smartGroup: "positive-feedback",
        marketingEligible: true,
      },
    })
  })

  it("maps view-guest to Guest Profile with location and no overview date range", () => {
    expect(
      planAssistantActionNavigate({
        action: {
          type: "view-guest",
          label: "View guest",
          guestId: 42,
        },
        analysisScope: SCOPE,
        mode: "single",
      })
    ).toEqual({
      path: "/single-dashboard/guests/42?location=11",
      selectLocationId: 11,
    })
  })
})
