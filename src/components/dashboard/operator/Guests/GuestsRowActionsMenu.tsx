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
  GUESTS_ROW_ACTIONS_TRIGGER_CLASS,
  OPERATOR_GUEST_ROW_ACTIONS,
} from "@/lib/operatorGuests/guestsPresentation"

type GuestsRowActionsMenuProps = {
  guestId: string
  guestName: string
  onManageTags: (guestId: string) => void
}

/** Figma Guests table Actions menu — node 3388:14467; chrome matches Sort select. */
export function GuestsRowActionsMenu({
  guestId,
  guestName,
  onManageTags,
}: GuestsRowActionsMenuProps) {
  const standardActions = OPERATOR_GUEST_ROW_ACTIONS.filter(
    (action) => action.id !== "delete-guest-data"
  )
  const deleteAction = OPERATOR_GUEST_ROW_ACTIONS.find(
    (action) => action.id === "delete-guest-data"
  )

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
        {standardActions.map((action) =>
          action.id === "manage-tags" ? (
            <DropdownMenuItem
              key={action.id}
              className={GUESTS_ROW_ACTIONS_ITEM_CLASS}
              onClick={() => {
                onManageTags(guestId)
              }}
            >
              {action.label}
            </DropdownMenuItem>
          ) : (
            <DropdownMenuItem
              key={action.id}
              disabled
              className={GUESTS_ROW_ACTIONS_ITEM_CLASS}
            >
              {action.label}
            </DropdownMenuItem>
          )
        )}
        {deleteAction ? (
          <>
            <DropdownMenuSeparator />
            <DropdownMenuItem
              variant="destructive"
              disabled
              className={GUESTS_ROW_ACTIONS_ITEM_CLASS}
            >
              {deleteAction.label}
            </DropdownMenuItem>
          </>
        ) : null}
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
