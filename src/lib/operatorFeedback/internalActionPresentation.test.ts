import { describe, expect, it } from "vitest"

import {
  INTERNAL_ACTION_CATEGORY_OPTIONS,
  INTERNAL_ACTION_FOLLOW_UP_STATE_LABEL,
  INTERNAL_ACTION_FOLLOW_UP_STATUS_LABEL,
  INTERNAL_ACTION_NOTE_HELPER,
  INTERNAL_ACTION_NOTE_PLACEHOLDER,
  INTERNAL_ACTION_RECOVERY_RECORDED_STATUS_LABEL,
  INTERNAL_ACTION_USE_FOR_GUEST_RESPONSE_LABEL,
  INTERNAL_ACTION_WORKFLOW_IN_PROGRESS_LABEL,
  RECORD_INTERNAL_ONLY_REVIEW_PRIMARY_CTA,
  canContinueInternalActionRecorder,
  canContinueRespondAndRecordRecorder,
  labelForInternalActionCategory,
} from "./internalActionPresentation"

describe("internalActionPresentation", () => {
  it("exposes the seven PRD categories in order", () => {
    expect(INTERNAL_ACTION_CATEGORY_OPTIONS.map((o) => o.id)).toEqual([
      "team_briefed",
      "order_or_service_process_reviewed",
      "delivery_issue_investigated",
      "product_quality_checked",
      "cleaning_issue_addressed",
      "staff_follow_up_completed",
      "other_action",
    ])
    expect(labelForInternalActionCategory("team_briefed")).toBe("Team briefed")
    expect(labelForInternalActionCategory("other_action")).toBe("Other action")
  })

  it("maps Figma category descriptions including Other action chrome", () => {
    expect(
      INTERNAL_ACTION_CATEGORY_OPTIONS.find((o) => o.id === "team_briefed")
        ?.description
    ).toBe(
      "The relevant restaurant team has been informed about the feedback."
    )
    expect(
      INTERNAL_ACTION_CATEGORY_OPTIONS.find((o) => o.id === "other_action")
        ?.description
    ).toBe("Record a different operational action.")
  })

  it("exposes display-only follow-up chips without a recovery-status enum", () => {
    expect(INTERNAL_ACTION_FOLLOW_UP_STATE_LABEL).toBe("Mark follow-up complete")
    expect(INTERNAL_ACTION_FOLLOW_UP_STATUS_LABEL).toBe("Complete")
    expect(INTERNAL_ACTION_RECOVERY_RECORDED_STATUS_LABEL).toBe(
      "Internal action recorded"
    )
    expect(INTERNAL_ACTION_WORKFLOW_IN_PROGRESS_LABEL).toBe("In progress")
  })

  it("requires category and a non-empty note to continue", () => {
    expect(
      canContinueInternalActionRecorder({ category: null, note: "Done" })
    ).toBe(false)
    expect(
      canContinueInternalActionRecorder({
        category: "team_briefed",
        note: "   ",
      })
    ).toBe(false)
    expect(
      canContinueInternalActionRecorder({
        category: "team_briefed",
        note: "Briefed the floor team.",
      })
    ).toBe(true)
  })

  it("Respond and record Continue requires the use-for-guest-response checkbox", () => {
    expect(INTERNAL_ACTION_USE_FOR_GUEST_RESPONSE_LABEL).toBe(
      "Use this confirmed action when preparing the guest response"
    )
    expect(
      canContinueRespondAndRecordRecorder({
        category: "team_briefed",
        note: "Briefed the floor team.",
        useConfirmedActionForGuestResponse: false,
      })
    ).toBe(false)
    expect(
      canContinueRespondAndRecordRecorder({
        category: "team_briefed",
        note: "Briefed the floor team.",
        useConfirmedActionForGuestResponse: true,
      })
    ).toBe(true)
  })

  it("Record-only Review primary records the internal follow-up without guest-send phrasing", () => {
    expect(RECORD_INTERNAL_ONLY_REVIEW_PRIMARY_CTA).toBe(
      "Record internal follow-up"
    )
    expect(RECORD_INTERNAL_ONLY_REVIEW_PRIMARY_CTA).not.toMatch(/send/i)
  })

  it("uses Figma note placeholder and helper copy", () => {
    expect(INTERNAL_ACTION_NOTE_PLACEHOLDER).toBe(
      "Describe what the restaurant reviewed, changed or plans to follow up…"
    )
    expect(INTERNAL_ACTION_NOTE_HELPER).toBe(
      "This note is internal and will not be sent to the guest."
    )
  })
})
