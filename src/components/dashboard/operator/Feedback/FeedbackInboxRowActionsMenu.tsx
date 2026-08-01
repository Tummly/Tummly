import { MoreVertical } from "lucide-react"

import { Button } from "@/components/ui/button"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { feedbackWorkflowStatusLabel } from "@/lib/operatorFeedback/createFeedbackDetailsModule"
import {
  GUESTS_ROW_ACTIONS_ITEM_CLASS,
  GUESTS_ROW_ACTIONS_MENU_CLASS,
  GUESTS_ROW_ACTIONS_TRIGGER_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"
import type { FeedbackWorkflowStatus } from "@/types/dashboard"

const WORKFLOW_STATUS_OPTIONS: FeedbackWorkflowStatus[] = [
  "new",
  "in_progress",
  "resolved",
]

type FeedbackInboxRowActionsMenuProps = {
  feedbackId: number
  guestName: string
  workflowStatus: FeedbackWorkflowStatus
  canReopen: boolean
  canMarkNoActionNeeded: boolean
  onSetWorkflowStatus: (
    feedbackId: number,
    status: FeedbackWorkflowStatus
  ) => void
  onReopen: (feedbackId: number) => void
  onMarkNoActionNeeded: (feedbackId: number) => void
}

/** Feedback inbox row ⋮ — live workflow shortcuts aligned with the details drawer. */
export function FeedbackInboxRowActionsMenu({
  feedbackId,
  guestName,
  workflowStatus,
  canReopen,
  canMarkNoActionNeeded,
  onSetWorkflowStatus,
  onReopen,
  onMarkNoActionNeeded,
}: FeedbackInboxRowActionsMenuProps) {
  const statusShortcuts = WORKFLOW_STATUS_OPTIONS.filter(
    (status) => status !== workflowStatus
  )

  return (
    <DropdownMenu modal={false}>
      <DropdownMenuTrigger asChild>
        <Button
          type="button"
          variant="ghost"
          size="icon"
          aria-label={`Actions for feedback from ${guestName}`}
          className={GUESTS_ROW_ACTIONS_TRIGGER_CLASS}
        >
          <MoreVertical className="size-4" aria-hidden />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className={GUESTS_ROW_ACTIONS_MENU_CLASS}>
        {statusShortcuts.map((status) => (
          <DropdownMenuItem
            key={status}
            className={GUESTS_ROW_ACTIONS_ITEM_CLASS}
            onClick={() => {
              onSetWorkflowStatus(feedbackId, status)
            }}
          >
            Set status: {feedbackWorkflowStatusLabel(status)}
          </DropdownMenuItem>
        ))}
        {statusShortcuts.length > 0 && (canReopen || canMarkNoActionNeeded) ? (
          <DropdownMenuSeparator className="mx-0" />
        ) : null}
        {canReopen ? (
          <DropdownMenuItem
            className={GUESTS_ROW_ACTIONS_ITEM_CLASS}
            onClick={() => {
              onReopen(feedbackId)
            }}
          >
            Reopen
          </DropdownMenuItem>
        ) : null}
        {canMarkNoActionNeeded ? (
          <DropdownMenuItem
            className={GUESTS_ROW_ACTIONS_ITEM_CLASS}
            onClick={() => {
              onMarkNoActionNeeded(feedbackId)
            }}
          >
            Mark no action needed
          </DropdownMenuItem>
        ) : null}
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
