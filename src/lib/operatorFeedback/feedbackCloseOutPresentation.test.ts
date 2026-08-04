import { describe, expect, it } from "vitest"

import {
  canConfirmFeedbackCloseOut,
  feedbackCloseOutDialogCopy,
  feedbackClosedOutActivityLabel,
  feedbackCloseOutHighRiskCallout,
  feedbackCloseOutReasonPlaceholder,
  feedbackCloseOutRequiresAcknowledgment,
  feedbackCloseOutShowsHighRiskCallout,
  FEEDBACK_CLOSE_OUT_ACKNOWLEDGMENT_LABEL,
} from "./feedbackCloseOutPresentation"

describe("feedbackCloseOutPresentation", () => {
  it("gates confirm until a reason is chosen", () => {
    expect(
      canConfirmFeedbackCloseOut({
        intent: "mark_resolved",
        reason: null,
        noteDraft: "",
        acknowledged: false,
      })
    ).toBe(false)
    expect(
      canConfirmFeedbackCloseOut({
        intent: "mark_resolved",
        reason: "duplicate_submission",
        noteDraft: "",
        acknowledged: false,
      })
    ).toBe(true)
  })

  it("requires a trimmed note when reason is Other", () => {
    expect(
      canConfirmFeedbackCloseOut({
        intent: "mark_resolved",
        reason: "other",
        noteDraft: "   ",
        acknowledged: false,
      })
    ).toBe(false)
    expect(
      canConfirmFeedbackCloseOut({
        intent: "mark_resolved",
        reason: "other",
        noteDraft: "Handled by phone",
        acknowledged: false,
      })
    ).toBe(true)
  })

  it("requires acknowledgment for mark no action needed", () => {
    expect(feedbackCloseOutRequiresAcknowledgment("mark_no_action_needed")).toBe(
      true
    )
    expect(feedbackCloseOutRequiresAcknowledgment("mark_resolved")).toBe(false)

    expect(
      canConfirmFeedbackCloseOut({
        intent: "mark_no_action_needed",
        reason: "duplicate_submission",
        noteDraft: "",
        acknowledged: false,
      })
    ).toBe(false)
    expect(
      canConfirmFeedbackCloseOut({
        intent: "mark_no_action_needed",
        reason: "duplicate_submission",
        noteDraft: "",
        acknowledged: true,
      })
    ).toBe(true)
  })

  it("shows high-risk callout only for mark no action needed when negative", () => {
    expect(
      feedbackCloseOutShowsHighRiskCallout("mark_no_action_needed", "negative")
    ).toBe(true)
    expect(
      feedbackCloseOutShowsHighRiskCallout("mark_no_action_needed", "positive")
    ).toBe(false)
    expect(
      feedbackCloseOutShowsHighRiskCallout("mark_no_action_needed", "neutral")
    ).toBe(false)
    expect(
      feedbackCloseOutShowsHighRiskCallout("mark_no_action_needed", null)
    ).toBe(false)
    expect(
      feedbackCloseOutShowsHighRiskCallout("mark_resolved", "negative")
    ).toBe(false)
  })

  it("returns intent-specific dialog copy and Figma chrome strings", () => {
    expect(feedbackCloseOutDialogCopy("mark_resolved").confirmLabel).toBe(
      "Mark resolved"
    )
    expect(
      feedbackCloseOutDialogCopy("mark_no_action_needed").confirmLabel
    ).toBe("Mark no action needed")
    expect(feedbackCloseOutReasonPlaceholder).toBe("Select")
    expect(feedbackCloseOutHighRiskCallout).toBe(
      "This feedback contains a possible high-risk issue. Additional permission and an internal note are required before it can be closed."
    )
    expect(FEEDBACK_CLOSE_OUT_ACKNOWLEDGMENT_LABEL).toBe(
      "I have reviewed this feedback and confirmed that no further action is required."
    )
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
