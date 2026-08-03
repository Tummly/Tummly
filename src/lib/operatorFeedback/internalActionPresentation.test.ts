import { describe, expect, it } from "vitest"

import {
  INTERNAL_ACTION_CATEGORY_OPTIONS,
  INTERNAL_ACTION_USE_FOR_GUEST_RESPONSE_LABEL,
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
})
