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
} from "@/lib/operatorCapture/capturePresentation"
import type { CapturePlacementStatus } from "@/types/dashboard"

type CapturePlacementRowActionsMenuProps = {
  placementLabel: string
  status: CapturePlacementStatus
  onPause: () => void
  onResume: () => void
  onCopyLink: () => void
}

export function CapturePlacementRowActionsMenu({
  placementLabel,
  status,
  onPause,
  onResume,
  onCopyLink,
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
          onClick={() => {
            if (isActive) {
              onPause()
              return
            }
            onResume()
          }}
        >
          {isActive ? "Pause" : "Resume"}
        </DropdownMenuItem>
        <DropdownMenuItem
          className={CAPTURE_PLACEMENT_ROW_ACTIONS_ITEM_CLASS}
          onClick={onCopyLink}
        >
          Copy link
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
