import { Fragment } from "react"
import { MoreVertical } from "lucide-react"

import { Button } from "@/components/ui/button"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import {
  GUESTS_ROW_ACTIONS_ITEM_CLASS,
  GUESTS_ROW_ACTIONS_MENU_CLASS,
  GUESTS_ROW_ACTIONS_SEPARATOR_CLASS,
  GUESTS_ROW_ACTIONS_TRIGGER_CLASS,
  OPERATOR_GUEST_ROW_ACTIONS,
} from "@/lib/operatorGuests/guestsPresentation"

type GuestsRowActionsMenuProps = {
  guestId: string
  guestName: string
  onManageTags: (guestId: string) => void
  onViewGuest: (guestId: string) => void
}

/** Figma Guests table Actions menu — node `4213:61228`. */
export function GuestsRowActionsMenu({
  guestId,
  guestName,
  onManageTags,
  onViewGuest,
}: GuestsRowActionsMenuProps) {
  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button
          type="button"
          variant="ghost"
          size="icon"
          aria-label={`Actions for ${guestName}`}
          className={GUESTS_ROW_ACTIONS_TRIGGER_CLASS}
        >
          <MoreVertical className="size-4" aria-hidden />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className={GUESTS_ROW_ACTIONS_MENU_CLASS}>
        {OPERATOR_GUEST_ROW_ACTIONS.map((action, index) => (
          <Fragment key={action.id}>
            {index > 0 ? (
              <DropdownMenuSeparator
                className={GUESTS_ROW_ACTIONS_SEPARATOR_CLASS}
              />
            ) : null}
            {action.id === "manage-tags" ? (
              <DropdownMenuItem
                className={GUESTS_ROW_ACTIONS_ITEM_CLASS}
                onClick={() => {
                  onManageTags(guestId)
                }}
              >
                {action.label}
              </DropdownMenuItem>
            ) : action.id === "view-guest" ? (
              <DropdownMenuItem
                className={GUESTS_ROW_ACTIONS_ITEM_CLASS}
                onClick={() => {
                  onViewGuest(guestId)
                }}
              >
                {action.label}
              </DropdownMenuItem>
            ) : (
              <DropdownMenuItem
                disabled
                className={GUESTS_ROW_ACTIONS_ITEM_CLASS}
              >
                {action.label}
              </DropdownMenuItem>
            )}
          </Fragment>
        ))}
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
