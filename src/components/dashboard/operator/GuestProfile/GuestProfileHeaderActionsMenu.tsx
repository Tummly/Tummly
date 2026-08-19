import { Fragment } from "react"
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
  GUESTS_ROW_ACTIONS_SEPARATOR_CLASS,
  GUESTS_ROW_ACTIONS_TRIGGER_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"

type GuestProfileHeaderActionsMenuProps = {
  guestName: string
  onManageTags: () => void
  onManageMarketingPermissions: () => void
  onExportGuestRecord: () => void
  onDeleteGuestData: () => void
}

/** Guest Profile header ⋮ — Figma `4213:61228` Actions chrome. */
export function GuestProfileHeaderActionsMenu({
  guestName,
  onManageTags,
  onManageMarketingPermissions,
  onExportGuestRecord,
  onDeleteGuestData,
}: GuestProfileHeaderActionsMenuProps) {
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
          <MoreVerticalIcon className="size-6" aria-hidden />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className={GUESTS_ROW_ACTIONS_MENU_CLASS}>
        {GUEST_PROFILE_HEADER_OVERFLOW_ACTIONS.map((action, index) => (
          <Fragment key={action.id}>
            {index > 0 ? (
              <DropdownMenuSeparator
                className={GUESTS_ROW_ACTIONS_SEPARATOR_CLASS}
              />
            ) : null}
            {action.id === "manage-tags" ? (
              <DropdownMenuItem
                className={GUESTS_ROW_ACTIONS_ITEM_CLASS}
                onClick={onManageTags}
              >
                {action.label}
              </DropdownMenuItem>
            ) : action.id === "manage-marketing-permissions" ? (
              <DropdownMenuItem
                className={GUESTS_ROW_ACTIONS_ITEM_CLASS}
                onClick={onManageMarketingPermissions}
              >
                {action.label}
              </DropdownMenuItem>
            ) : action.id === "export-guest-record" ? (
              <DropdownMenuItem
                className={GUESTS_ROW_ACTIONS_ITEM_CLASS}
                onClick={onExportGuestRecord}
              >
                {action.label}
              </DropdownMenuItem>
            ) : (
              <DropdownMenuItem
                className={GUESTS_ROW_ACTIONS_ITEM_CLASS}
                onClick={onDeleteGuestData}
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
