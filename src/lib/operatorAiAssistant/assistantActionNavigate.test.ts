import { describe, expect, it } from "vitest"

import { planAssistantActionNavigate, planAssistantSendScheduleRoute } from "./assistantActionNavigate"
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

  it("maps change-audience to Campaigns Continue editing at Audience, not Drafts", () => {
    expect(
      planAssistantActionNavigate({
        action: {
          type: "change-audience",
          label: "Change audience",
          campaignId: 41,
        },
        analysisScope: SCOPE,
        mode: "multi",
      })
    ).toEqual({
      path: "/multi-dashboard/campaigns?location=11",
      selectLocationId: 11,
      campaigns: {
        continueEditingCampaignId: 41,
        continueEditingStep: "audience",
      },
    })
  })

  it("attaches a loaded Campaign Draft on Continue editing land so Campaigns does not fetch again", () => {
    const campaign = {
      id: 41,
      locationId: 11,
      status: "draft",
      name: "Win-back",
      goalId: "re-engage-inactive",
      templateId: null,
      templateVersion: null,
      audienceKey: "all-eligible-guests",
      channel: "email",
      offerStance: "no-offer",
      offerId: null,
      messageSubject: null,
      messageBody: null,
      rowVersion: "1",
      createdAt: "2026-08-19T00:00:00Z",
      updatedAt: "2026-08-19T00:00:00Z",
    }
    expect(
      planAssistantActionNavigate({
        action: {
          type: "change-audience",
          label: "Change audience",
          campaignId: 41,
        },
        analysisScope: SCOPE,
        mode: "multi",
        campaignDraft: campaign,
      }).campaigns
    ).toEqual({
      continueEditingCampaignId: 41,
      continueEditingStep: "audience",
      campaign,
    })
  })

  it("maps add-offer to Campaigns Continue editing at Offer, not Drafts", () => {
    expect(
      planAssistantActionNavigate({
        action: {
          type: "add-offer",
          label: "Add Offer",
          campaignId: 41,
        },
        analysisScope: SCOPE,
        mode: "single",
      })
    ).toEqual({
      path: "/single-dashboard/campaigns?location=11",
      selectLocationId: 11,
      campaigns: {
        continueEditingCampaignId: 41,
        continueEditingStep: "offer",
      },
    })
  })

  it("maps review-offer to Offer Details without Drafts view or list dates", () => {
    expect(
      planAssistantActionNavigate({
        action: {
          type: "review-offer",
          label: "Review offer draft",
          offerId: 55,
        },
        analysisScope: SCOPE,
        mode: "multi",
      })
    ).toEqual({
      path: "/multi-dashboard/offers/55?location=11",
      selectLocationId: 11,
    })
  })

  it("does not map leftover Draft Action types onto Campaigns or Offers Drafts", () => {
    const leftoverOffer = planAssistantActionNavigate({
      action: { type: "draft-offer", label: "Create offer draft" },
      analysisScope: SCOPE,
      mode: "multi",
    })
    const leftoverCampaign = planAssistantActionNavigate({
      action: { type: "draft-campaign", label: "Create campaign draft" },
      analysisScope: SCOPE,
      mode: "multi",
    })
    expect(leftoverOffer.offers).toBeUndefined()
    expect(leftoverOffer.path).not.toContain("/offers")
    expect(leftoverCampaign.campaigns).toBeUndefined()
    expect(leftoverCampaign.path).not.toContain("/campaigns")
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

  it("maps later Send it now to Campaign wizard Review, not Campaign Detail", () => {
    expect(
      planAssistantSendScheduleRoute({
        route: {
          kind: "campaign",
          campaignId: 41,
          step: "review",
          scheduleMode: "send-now",
        },
        analysisScope: SCOPE,
        mode: "multi",
      })
    ).toEqual({
      path: "/multi-dashboard/campaigns?location=11",
      selectLocationId: 11,
      campaigns: {
        continueEditingCampaignId: 41,
        continueEditingStep: "review",
        scheduleMode: "send-now",
      },
    })
  })

  it("maps later schedule without datetime to Campaign wizard Schedule", () => {
    expect(
      planAssistantSendScheduleRoute({
        route: {
          kind: "campaign",
          campaignId: 41,
          step: "schedule",
          scheduleMode: "schedule-later",
        },
        analysisScope: SCOPE,
        mode: "multi",
      }).campaigns
    ).toEqual({
      continueEditingCampaignId: 41,
      continueEditingStep: "schedule",
      scheduleMode: "schedule-later",
    })
  })

  it("maps later recovery send to Feedback Review hydrate, not Campaign wizard", () => {
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
      planAssistantSendScheduleRoute({
        route: {
          kind: "recovery",
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
})
