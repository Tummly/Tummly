import { describe, expect, it } from "vitest"

import {
  parseRecoveryDraftActionRouterState,
  recoveryDraftActionGateToast,
  RECOVERY_DRAFT_ACTION_TOASTS,
} from "./recoveryDraftAction"

describe("recoveryDraftAction", () => {
  it("parses one-shot router state and rejects bad payloads", () => {
    expect(
      parseRecoveryDraftActionRouterState({
        recoveryDraft: {
          feedbackId: 12,
          intent: "respond-to-guest",
          channel: "email",
          purpose: "acknowledge_feedback",
          tone: "warm_and_apologetic",
          message: "Thanks",
        },
      })
    ).toMatchObject({
      feedbackId: 12,
      intent: "respond-to-guest",
      channel: "email",
      message: "Thanks",
    })
    expect(parseRecoveryDraftActionRouterState(null)).toBeNull()
    expect(
      parseRecoveryDraftActionRouterState({
        recoveryDraft: { feedbackId: 1, intent: "invented" },
      })
    ).toBeNull()
  })

  it("maps Start recovery gates to Draft Action toasts", () => {
    expect(
      recoveryDraftActionGateToast({
        intent: "respond-to-guest",
        workflowStatus: "resolved",
        contactType: "Email",
        guestContact: "a@b.com",
        guestOffersOptOut: false,
      })
    ).toBe(RECOVERY_DRAFT_ACTION_TOASTS.resolved)

    expect(
      recoveryDraftActionGateToast({
        intent: "respond-to-guest",
        workflowStatus: "new",
        contactType: "Unknown",
        guestContact: "",
        guestOffersOptOut: false,
      })
    ).toBe(RECOVERY_DRAFT_ACTION_TOASTS.noContact)

    expect(
      recoveryDraftActionGateToast({
        intent: "respond-with-recovery-offer",
        workflowStatus: "in_progress",
        contactType: "Email",
        guestContact: "a@b.com",
        guestOffersOptOut: true,
      })
    ).toBe(RECOVERY_DRAFT_ACTION_TOASTS.offersOptOut)

    expect(
      recoveryDraftActionGateToast({
        intent: "respond-to-guest",
        workflowStatus: "in_progress",
        contactType: "Email",
        guestContact: "a@b.com",
        guestOffersOptOut: false,
      })
    ).toBeNull()
  })
})
