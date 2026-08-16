import { describe, expect, it, vi } from "vitest"

import type { FeedbackDetailsResponse } from "@/types/dashboard"
import { createRecoveryWizardsModule } from "./createRecoveryWizardsModule"

const sampleDetails: FeedbackDetailsResponse = {
  success: true,
  id: 2418,
  guestName: "Mohamed M.",
  guestContact: "mohamed@email.com",
  contactType: "Email",
  comment: "Food was cold.",
  createdAt: "2026-07-14T11:48:00.000Z",
  locationName: "Camden",
  address: "12 High Street",
  qrSource: "Delivery insert",
  classificationStatus: "Succeeded",
  sentiment: "negative",
  detectedTags: ["FoodQuality"],
  locationGuestId: 501,
  workflowStatus: "in_progress",
  guestOffersOptOut: false,
  internalNotes: [],
  activityHistory: [],
}

describe("createRecoveryWizardsModule Draft Action Back", () => {
  it("does not reopen Start recovery after Draft Action Back from first compose", async () => {
    const getFeedbackDetails = vi.fn(async () => sampleDetails)
    const module = createRecoveryWizardsModule({
      getFeedbackDetails,
      setWorkflowStatus: async () => ({
        workflowStatus: "in_progress" as const,
        needsAttention: true,
      }),
      getRecoveryOfferAttach: async () => null,
      setRecoveryOfferAttach: async () => {},
      sendGuestResponse: async () =>
        ({
          workflowStatus: "in_progress",
          needsAttention: true,
          activityEvent: {
            kind: "guest_response_sent",
            at: "2026-08-03T12:00:00.000Z",
            actorDisplayName: "Alex",
            channel: "email",
            maskedDestination: "m••••@email.com",
          },
        }) as never,
      sendGuestPreviewTest: async () => {},
      completeRecovery: async () =>
        ({
          workflowStatus: "resolved",
          needsAttention: false,
        }) as never,
      prepareRecoveryDraft: async () => ({
        status: "succeeded",
        body: "Thanks",
        subject: "Hi",
        channel: "email",
      }),
      recordInternalAction: async () =>
        ({
          workflowStatus: "in_progress",
          needsAttention: true,
          activityEvent: {
            kind: "internal_action_recorded",
            at: "2026-08-03T12:00:00.000Z",
            actorDisplayName: "Alex",
            category: "team_briefed",
            categoryLabel: "Team briefed",
            note: "Briefed",
          },
        }) as never,
      sendAndRecord: async () => ({}) as never,
      sendAndIssueRecoveryOffer: async () => ({}) as never,
      prepareRecoveryOfferDraft: async () => ({
        status: "succeeded",
        body: "Thanks",
        subject: "Hi",
        channel: "email",
      }),
    })

    await module.openFromDraftAction({
      feedbackId: 2418,
      intent: "respond-to-guest",
      channel: "email",
      purpose: "acknowledge_feedback",
      tone: "warm_and_apologetic",
      includeNotes: "",
      subject: "Following up",
      message: "Thanks for telling us.",
    })
    expect(module.getSnapshot().respondToGuest.step).toBe("review")
    expect(module.getSnapshot().respondToGuest.openedFromDraftAction).toBe(true)

    module.respondToGuest.back()
    module.respondToGuest.back()
    getFeedbackDetails.mockClear()
    const result = module.respondToGuest.back()
    expect(result).toBe("return-to-shell")
    expect(module.getSnapshot().startRecovery.isOpen).toBe(false)
    expect(getFeedbackDetails).not.toHaveBeenCalled()
  })
})
