import { Fragment } from "react"
import { Link } from "react-router-dom"
import { MoreVerticalIcon } from "lucide-react"

import { Button } from "@/components/ui/button"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { helpCentreArticleUrl } from "@/config/support"
import { FEEDBACK_HEADER_OVERFLOW_ACTIONS } from "@/lib/operatorFeedback/feedbackPresentation"
import {
  GUESTS_ROW_ACTIONS_ITEM_CLASS,
  GUESTS_ROW_ACTIONS_MENU_CLASS,
  GUESTS_ROW_ACTIONS_SEPARATOR_CLASS,
  GUESTS_ROW_ACTIONS_TRIGGER_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"

type FeedbackPageHeaderActionsMenuProps = {
  locationName: string
  onExportFeedback: () => void
}

/** Feedback page header ⋮ — Export and Help only (no dead Settings row). */
export function FeedbackPageHeaderActionsMenu({
  locationName,
  onExportFeedback,
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
        {FEEDBACK_HEADER_OVERFLOW_ACTIONS.map((action, index) => (
          <Fragment key={action.id}>
            {index > 0 ? (
              <DropdownMenuSeparator
                className={GUESTS_ROW_ACTIONS_SEPARATOR_CLASS}
              />
            ) : null}
            {action.id === "view-feedback-help" ? (
              <DropdownMenuItem
                className={GUESTS_ROW_ACTIONS_ITEM_CLASS}
                asChild
              >
                <Link to={helpCentreArticleUrl("guest-feedback")}>
                  {action.label}
                </Link>
              </DropdownMenuItem>
            ) : action.id === "export-feedback" ? (
              <DropdownMenuItem
                className={GUESTS_ROW_ACTIONS_ITEM_CLASS}
                onSelect={() => {
                  onExportFeedback()
                }}
              >
                {action.label}
              </DropdownMenuItem>
            ) : null}
          </Fragment>
        ))}
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
