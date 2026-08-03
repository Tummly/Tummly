import { MoreVertical } from "lucide-react"

import { Button } from "@/components/ui/button"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import {
  buildFeedbackInboxRowActions,
  type FeedbackInboxRowActionId,
} from "@/lib/operatorFeedback/feedbackInboxRowActions"
import {
  GUESTS_ROW_ACTIONS_ITEM_CLASS,
  GUESTS_ROW_ACTIONS_MENU_CLASS,
  GUESTS_ROW_ACTIONS_TRIGGER_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"
import type { FeedbackWorkflowStatus } from "@/types/dashboard"

type FeedbackInboxRowActionsMenuProps = {
  guestName: string
  workflowStatus: FeedbackWorkflowStatus
  onStartRecovery?: () => void
  onViewFeedback: () => void
  onMarkResolved: () => void
  onMarkNoActionNeeded: () => void
}

/** Feedback inbox row ⋮ — PRD order via buildFeedbackInboxRowActions. */
export function FeedbackInboxRowActionsMenu({
  guestName,
  workflowStatus,
  onStartRecovery,
  onViewFeedback,
  onMarkResolved,
  onMarkNoActionNeeded,
}: FeedbackInboxRowActionsMenuProps) {
  const actions = buildFeedbackInboxRowActions(workflowStatus).filter(
    (action) => action.visible
  )

  const handleAction = (id: FeedbackInboxRowActionId) => {
    switch (id) {
      case "start-recovery":
        onStartRecovery?.()
        break
      case "view-feedback":
        onViewFeedback()
        break
      case "mark-resolved":
        onMarkResolved()
        break
      case "mark-no-action-needed":
        onMarkNoActionNeeded()
        break
    }
  }

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
        {actions.map((action) => (
          <DropdownMenuItem
            key={action.id}
            className={GUESTS_ROW_ACTIONS_ITEM_CLASS}
            disabled={!action.enabled}
            onClick={() => {
              if (!action.enabled) {
                return
              }
              handleAction(action.id)
            }}
          >
            {action.label}
          </DropdownMenuItem>
        ))}
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
