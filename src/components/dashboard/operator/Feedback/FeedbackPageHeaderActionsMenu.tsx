import { Link } from "react-router-dom"
import { MoreVerticalIcon } from "lucide-react"

import { Button } from "@/components/ui/button"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { helpCentreArticleUrl } from "@/config/support"
import { FEEDBACK_HEADER_OVERFLOW_ACTIONS } from "@/lib/operatorFeedback/feedbackPresentation"
import {
  GUESTS_ROW_ACTIONS_ITEM_CLASS,
  GUESTS_ROW_ACTIONS_MENU_CLASS,
  GUESTS_ROW_ACTIONS_TRIGGER_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"

type FeedbackPageHeaderActionsMenuProps = {
  locationName: string
}

/** Feedback page header ⋮ — Export disabled; Manage settings disabled; Help live. */
export function FeedbackPageHeaderActionsMenu({
  locationName,
}: FeedbackPageHeaderActionsMenuProps) {
  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button
          type="button"
          variant="ghost"
          size="icon"
          aria-label={`Actions for Feedback at ${locationName}`}
          className={GUESTS_ROW_ACTIONS_TRIGGER_CLASS}
        >
          <MoreVerticalIcon className="size-6" aria-hidden />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className={GUESTS_ROW_ACTIONS_MENU_CLASS}>
        {FEEDBACK_HEADER_OVERFLOW_ACTIONS.map((action) => {
          if (action.id === "view-feedback-help") {
            return (
              <DropdownMenuItem
                key={action.id}
                className={GUESTS_ROW_ACTIONS_ITEM_CLASS}
                asChild
              >
                <Link to={helpCentreArticleUrl("guest-feedback")}>
                  {action.label}
                </Link>
              </DropdownMenuItem>
            )
          }

          return (
            <DropdownMenuItem
              key={action.id}
              disabled
              aria-disabled
              aria-label={`${action.label} (unavailable)`}
              title={`${action.label} is unavailable`}
              className={GUESTS_ROW_ACTIONS_ITEM_CLASS}
            >
              {action.label}
            </DropdownMenuItem>
          )
        })}
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
