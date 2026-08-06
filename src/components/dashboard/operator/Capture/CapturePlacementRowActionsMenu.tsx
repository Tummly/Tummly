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
  OPERATOR_CAPTURE_PLACEMENT_DETAIL_COPY,
} from "@/lib/operatorCapture/capturePresentation"
import type { CapturePlacementStatus } from "@/types/dashboard"

type CapturePlacementRowActionsMenuProps = {
  placementLabel: string
  status: CapturePlacementStatus
  pauseActivateEnabled?: boolean
  onViewDetails: () => void
  onPause: () => void
  onResume: () => void
  onRotate: () => void
  onCopyLink: () => void
  onArchive: () => void
}

export function CapturePlacementRowActionsMenu({
  placementLabel,
  status,
  pauseActivateEnabled = true,
  onViewDetails,
  onPause,
  onResume,
  onRotate,
  onCopyLink,
  onArchive,
}: CapturePlacementRowActionsMenuProps) {
  const isActive = status === "Active"

  const items = [
    {
      id: "view-details",
      label: OPERATOR_CAPTURE_PLACEMENT_DETAIL_COPY.viewDetails,
      disabled: false,
      onClick: onViewDetails,
    },
    {
      id: "pause-activate",
      label: isActive ? "Pause" : "Activate",
      disabled: !pauseActivateEnabled,
      onClick: () => {
        if (!pauseActivateEnabled) {
          return
        }
        if (isActive) {
          onPause()
          return
        }
        onResume()
      },
    },
    {
      id: "rotate",
      label: OPERATOR_CAPTURE_PLACEMENT_DETAIL_COPY.rotateQrCode,
      disabled: false,
      onClick: onRotate,
    },
    {
      id: "copy-link",
      label: "Copy link",
      disabled: false,
      onClick: onCopyLink,
    },
    {
      id: "archive",
      label: OPERATOR_CAPTURE_PLACEMENT_DETAIL_COPY.archivePlacement,
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
          aria-label={`Actions for ${placementLabel}`}
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
