import { MoreVerticalIcon } from "lucide-react"

import { Button } from "@/components/ui/button"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import type { OperatorCaptureLocationRowAction } from "@/lib/operatorMultiCapture/createOperatorMultiCapturePageModule"
import type { OperatorCaptureLocationRowActionId } from "@/lib/operatorCapture/capturePresentation"
import {
  GUESTS_ROW_ACTIONS_ITEM_CLASS,
  GUESTS_ROW_ACTIONS_MENU_CLASS,
  GUESTS_ROW_ACTIONS_TRIGGER_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"

type CaptureLocationRowActionsMenuProps = {
  locationName: string
  actions: readonly OperatorCaptureLocationRowAction[]
  onAction: (actionId: OperatorCaptureLocationRowActionId) => void
}

/** Multi Capture Location performance row ⋯ — Figma `3889:19648` / grilling 11. */
export function CaptureLocationRowActionsMenu({
  locationName,
  actions,
  onAction,
}: CaptureLocationRowActionsMenuProps) {
  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button
          type="button"
          variant="ghost"
          size="icon"
          aria-label={`Actions for ${locationName}`}
          className={GUESTS_ROW_ACTIONS_TRIGGER_CLASS}
          onClick={(event) => {
            event.stopPropagation()
          }}
        >
          <MoreVerticalIcon className="size-4" aria-hidden />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className={GUESTS_ROW_ACTIONS_MENU_CLASS}>
        {actions.map((action) => (
          <DropdownMenuItem
            key={action.id}
            disabled={!action.enabled}
            className={GUESTS_ROW_ACTIONS_ITEM_CLASS}
            onClick={(event) => {
              event.stopPropagation()
              if (!action.enabled) {
                return
              }
              onAction(action.id)
            }}
          >
            {action.label}
          </DropdownMenuItem>
        ))}
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
