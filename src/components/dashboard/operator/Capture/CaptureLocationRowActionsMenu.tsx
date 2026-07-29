import { MoreVerticalIcon } from "lucide-react"

import { Button } from "@/components/ui/button"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import type { OperatorCaptureLocationRowAction } from "@/lib/operatorMultiCapture/createOperatorMultiCapturePageModule"
import {
  GUESTS_ROW_ACTIONS_ITEM_CLASS,
  GUESTS_ROW_ACTIONS_MENU_CLASS,
  GUESTS_ROW_ACTIONS_TRIGGER_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"

type CaptureLocationRowActionsMenuProps = {
  locationName: string
  actions: readonly OperatorCaptureLocationRowAction[]
  onViewLocationCapture: () => void
}

/** Multi Capture Location performance row ⋯ — Figma `3889:19648` annotations. */
export function CaptureLocationRowActionsMenu({
  locationName,
  actions,
  onViewLocationCapture,
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
        {actions.map((action) =>
          action.id === "view-location-capture" && action.enabled ? (
            <DropdownMenuItem
              key={action.id}
              className={GUESTS_ROW_ACTIONS_ITEM_CLASS}
              onClick={(event) => {
                event.stopPropagation()
                onViewLocationCapture()
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
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
