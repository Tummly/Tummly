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
  OPERATOR_CAPTURE_PLACEMENT_DETAIL_COPY,
} from "@/lib/operatorCapture/capturePresentation"
import type { CapturePlacementStatus } from "@/types/dashboard"

type CapturePlacementRowActionsMenuProps = {
  placementLabel: string
  status: CapturePlacementStatus
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
  onViewDetails,
  onPause,
  onResume,
  onRotate,
  onCopyLink,
  onArchive,
}: CapturePlacementRowActionsMenuProps) {
  const isActive = status === "Active"

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
        <DropdownMenuItem
          className={CAPTURE_PLACEMENT_ROW_ACTIONS_ITEM_CLASS}
          onClick={onViewDetails}
        >
          {OPERATOR_CAPTURE_PLACEMENT_DETAIL_COPY.viewDetails}
        </DropdownMenuItem>
        <DropdownMenuItem
          className={CAPTURE_PLACEMENT_ROW_ACTIONS_ITEM_CLASS}
          onClick={() => {
            if (isActive) {
              onPause()
              return
            }
            onResume()
          }}
        >
          {isActive ? "Pause" : "Activate"}
        </DropdownMenuItem>
        <DropdownMenuItem
          className={CAPTURE_PLACEMENT_ROW_ACTIONS_ITEM_CLASS}
          onClick={onRotate}
        >
          {OPERATOR_CAPTURE_PLACEMENT_DETAIL_COPY.rotateQrCode}
        </DropdownMenuItem>
        <DropdownMenuItem
          className={CAPTURE_PLACEMENT_ROW_ACTIONS_ITEM_CLASS}
          onClick={onCopyLink}
        >
          Copy link
        </DropdownMenuItem>
        <DropdownMenuItem
          className={CAPTURE_PLACEMENT_ROW_ACTIONS_ITEM_CLASS}
          onClick={onArchive}
        >
          {OPERATOR_CAPTURE_PLACEMENT_DETAIL_COPY.archivePlacement}
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
