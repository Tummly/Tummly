import { MoreVertical } from "lucide-react"

import { Button } from "@/components/ui/button"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import {
  CAPTURE_PLACEMENT_ROW_ACTIONS_ITEM_CLASS,
  CAPTURE_PLACEMENT_ROW_ACTIONS_MENU_CLASS,
  CAPTURE_PLACEMENT_ROW_ACTIONS_TRIGGER_CLASS,
  OPERATOR_CAPTURE_DIGITAL_GUEST_LINK_ROW_ACTIONS,
} from "@/lib/operatorCapture/capturePresentation"
import type { CapturePlacementStatus } from "@/types/dashboard"

type CaptureDigitalGuestLinkRowActionsMenuProps = {
  guestLinkLabel: string
  status: CapturePlacementStatus
  pauseActivateEnabled?: boolean
  onViewDetails: () => void
  onPreview: () => void
  onPause: () => void
  onActivate: () => void
  onCopyLink: () => void
  onArchive: () => void
}

export function CaptureDigitalGuestLinkRowActionsMenu({
  guestLinkLabel,
  status,
  pauseActivateEnabled = true,
  onViewDetails,
  onPreview,
  onPause,
  onActivate,
  onCopyLink,
  onArchive,
}: CaptureDigitalGuestLinkRowActionsMenuProps) {
  const isActive = status === "Active"
  const copy = OPERATOR_CAPTURE_DIGITAL_GUEST_LINK_ROW_ACTIONS

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button
          type="button"
          variant="op-ghost"
          size="icon"
          aria-label={`Actions for ${guestLinkLabel}`}
          className={CAPTURE_PLACEMENT_ROW_ACTIONS_TRIGGER_CLASS}
        >
          <MoreVertical className="size-4" aria-hidden />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent
        align="end"
        className={CAPTURE_PLACEMENT_ROW_ACTIONS_MENU_CLASS}
      >
        <DropdownMenuItem
          className={CAPTURE_PLACEMENT_ROW_ACTIONS_ITEM_CLASS}
          onClick={onViewDetails}
        >
          {copy.viewDetails}
        </DropdownMenuItem>
        <DropdownMenuItem
          className={CAPTURE_PLACEMENT_ROW_ACTIONS_ITEM_CLASS}
          onClick={onPreview}
        >
          {copy.preview}
        </DropdownMenuItem>
        <DropdownMenuItem
          className={CAPTURE_PLACEMENT_ROW_ACTIONS_ITEM_CLASS}
          disabled={!pauseActivateEnabled}
          onClick={() => {
            if (!pauseActivateEnabled) {
              return
            }
            if (isActive) {
              onPause()
              return
            }
            onActivate()
          }}
        >
          {isActive ? copy.pause : copy.activate}
        </DropdownMenuItem>
        <DropdownMenuItem
          className={CAPTURE_PLACEMENT_ROW_ACTIONS_ITEM_CLASS}
          onClick={onCopyLink}
        >
          {copy.copyLink}
        </DropdownMenuItem>
        <DropdownMenuItem
          className={CAPTURE_PLACEMENT_ROW_ACTIONS_ITEM_CLASS}
          onClick={onArchive}
        >
          {copy.archive}
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
