import type { FeedbackWorkflowStatus } from "@/types/dashboard"

export type FeedbackInboxRowActionId =
  | "start-recovery"
  | "view-feedback"
  | "reopen"
  | "mark-resolved"
  | "mark-no-action-needed"

export type FeedbackInboxRowAction = {
  id: FeedbackInboxRowActionId
  label: string
  /** When false, the item stays visible but is non-interactive. */
  enabled: boolean
  /** When false, the item is omitted from the menu. */
  visible: boolean
}

/**
 * PRD inbox row ⋮ chrome — order, labels, and enable/hide rules.
 * Start recovery stays gated (enabled when not Resolved) but need not open yet.
 */
export function buildFeedbackInboxRowActions(
  workflowStatus: FeedbackWorkflowStatus
): FeedbackInboxRowAction[] {
  const isResolved = workflowStatus === "resolved"

  return [
    {
      id: "start-recovery",
      label: "Start recovery",
      enabled: !isResolved,
      visible: true,
    },
    {
      id: "view-feedback",
      label: "View feedback",
      enabled: true,
      visible: true,
    },
    {
      id: "reopen",
      label: "Reopen",
      enabled: isResolved,
      visible: isResolved,
    },
    {
      id: "mark-resolved",
      label: "Mark resolved",
      enabled: !isResolved,
      visible: !isResolved,
    },
    {
      id: "mark-no-action-needed",
      label: "Mark no action needed",
      enabled: !isResolved,
      visible: !isResolved,
    },
  ]
}
