import { describe, expect, it } from "vitest"

import {
  INTERNAL_ACTION_CATEGORY_OPTIONS,
  canContinueInternalActionRecorder,
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
})
