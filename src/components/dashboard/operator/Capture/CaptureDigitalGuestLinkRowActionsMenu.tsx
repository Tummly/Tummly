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
  CAPTURE_PLACEMENT_ROW_ACTIONS_ITEM_CLASS,
  CAPTURE_PLACEMENT_ROW_ACTIONS_MENU_CLASS,
  CAPTURE_PLACEMENT_ROW_ACTIONS_SEPARATOR_CLASS,
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

  const items = [
    {
      id: "view-details",
      label: copy.viewDetails,
      disabled: false,
      onClick: onViewDetails,
    },
    {
      id: "preview",
      label: copy.preview,
      disabled: false,
      onClick: onPreview,
    },
    {
      id: "pause-activate",
      label: isActive ? copy.pause : copy.activate,
      disabled: !pauseActivateEnabled,
      onClick: () => {
        if (!pauseActivateEnabled) {
          return
        }
        if (isActive) {
          onPause()
          return
        }
        onActivate()
      },
    },
    {
      id: "copy-link",
      label: copy.copyLink,
      disabled: false,
      onClick: onCopyLink,
    },
    {
      id: "archive",
      label: copy.archive,
      disabled: false,
      onClick: onArchive,
    },
  ] as const

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
        {items.map((item, index) => (
          <Fragment key={item.id}>
            {index > 0 ? (
              <DropdownMenuSeparator
                className={CAPTURE_PLACEMENT_ROW_ACTIONS_SEPARATOR_CLASS}
              />
            ) : null}
            <DropdownMenuItem
              className={CAPTURE_PLACEMENT_ROW_ACTIONS_ITEM_CLASS}
              disabled={item.disabled}
              onClick={item.onClick}
            >
              {item.label}
            </DropdownMenuItem>
          </Fragment>
        ))}
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
