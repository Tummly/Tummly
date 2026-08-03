import { describe, expect, it } from "vitest"

import {
  canConfirmFeedbackCloseOut,
  feedbackCloseOutDialogCopy,
  feedbackClosedOutActivityLabel,
} from "./feedbackCloseOutPresentation"

describe("feedbackCloseOutPresentation", () => {
  it("gates confirm until a reason is chosen", () => {
    expect(
      canConfirmFeedbackCloseOut({ reason: null, noteDraft: "" })
    ).toBe(false)
    expect(
      canConfirmFeedbackCloseOut({
        reason: "duplicate_submission",
        noteDraft: "",
      })
    ).toBe(true)
  })

  it("requires a trimmed note when reason is Other", () => {
    expect(
      canConfirmFeedbackCloseOut({ reason: "other", noteDraft: "   " })
    ).toBe(false)
    expect(
      canConfirmFeedbackCloseOut({
        reason: "other",
        noteDraft: "Handled by phone",
      })
    ).toBe(true)
  })

  it("returns intent-specific dialog copy", () => {
    expect(feedbackCloseOutDialogCopy("mark_resolved").confirmLabel).toBe(
      "Mark resolved"
    )
    expect(
      feedbackCloseOutDialogCopy("mark_no_action_needed").confirmLabel
    ).toBe("Mark no action needed")
  })

  it("builds close-out activity label with reason and status transition", () => {
    expect(
      feedbackClosedOutActivityLabel({
        intent: "mark_resolved",
        reason: "duplicate_submission",
        fromWorkflowStatus: "new",
        toWorkflowStatus: "resolved",
      })
    ).toBe("Marked as resolved · Duplicate submission · New → Resolved")
  })
})
