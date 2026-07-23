import { MoreVerticalIcon } from "lucide-react"

import { Button } from "@/components/ui/button"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { GUEST_PROFILE_HEADER_OVERFLOW_ACTIONS } from "@/lib/operatorGuestProfile/guestProfilePresentation"
import {
  GUESTS_ROW_ACTIONS_ITEM_CLASS,
  GUESTS_ROW_ACTIONS_MENU_CLASS,
  GUESTS_ROW_ACTIONS_TRIGGER_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"

type GuestProfileHeaderActionsMenuProps = {
  guestName: string
  onManageTags: () => void
  onExportGuestRecord: () => void
  onDeleteGuestData: () => void
}

/** Guest Profile header ⋮ — Figma 3388:12934; Guests row-actions styles. */
export function GuestProfileHeaderActionsMenu({
  guestName,
  onManageTags,
  onExportGuestRecord,
  onDeleteGuestData,
}: GuestProfileHeaderActionsMenuProps) {
  const standardActions = GUEST_PROFILE_HEADER_OVERFLOW_ACTIONS.filter(
    (action) => action.id !== "delete-guest-data"
  )
  const deleteAction = GUEST_PROFILE_HEADER_OVERFLOW_ACTIONS.find(
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
          <MoreVerticalIcon className="size-4" aria-hidden />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className={GUESTS_ROW_ACTIONS_MENU_CLASS}>
        {standardActions.map((action) => {
          if (action.id === "manage-tags") {
            return (
              <DropdownMenuItem
                key={action.id}
                className={GUESTS_ROW_ACTIONS_ITEM_CLASS}
                onClick={onManageTags}
              >
                {action.label}
              </DropdownMenuItem>
            )
          }

          if (action.id === "export-guest-record") {
            return (
              <DropdownMenuItem
                key={action.id}
                className={GUESTS_ROW_ACTIONS_ITEM_CLASS}
                onClick={onExportGuestRecord}
              >
                {action.label}
              </DropdownMenuItem>
            )
          }

          return (
            <DropdownMenuItem
              key={action.id}
              disabled
              className={GUESTS_ROW_ACTIONS_ITEM_CLASS}
            >
              {action.label}
            </DropdownMenuItem>
          )
        })}
        {deleteAction ? (
          <>
            <DropdownMenuSeparator />
            <DropdownMenuItem
              variant="destructive"
              className={GUESTS_ROW_ACTIONS_ITEM_CLASS}
              onClick={onDeleteGuestData}
            >
              {deleteAction.label}
            </DropdownMenuItem>
          </>
        ) : null}
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
