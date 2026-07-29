import { MapPinIcon, ChevronDownIcon } from "lucide-react"

import { Button } from "@/components/ui/button"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuRadioGroup,
  DropdownMenuRadioItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { PERFORMANCE_DATE_BUTTON_CLASS } from "@/lib/operatorHome/performanceOverviewPresentation"
import {
  OPERATOR_SHELL_MENU_ITEM_CLASS,
  OPERATOR_SHELL_MENU_ITEM_SELECTED_CLASS,
  OPERATOR_SHELL_MENU_PANEL_CLASS,
} from "@/lib/operatorHome/shellResponsivePresentation"
import { cn } from "@/lib/utils"

type CaptureLocationOption = {
  id: number
  locationName: string
}

type CaptureLocationControlProps = {
  locations: readonly CaptureLocationOption[]
  selectedLocationId: number
  selectedLocationName: string
  onSelectLocation: (locationId: number) => void
}

/** Compact Owned-location control for multi nested Capture header (Figma date-button chrome). */
export function CaptureLocationControl({
  locations,
  selectedLocationId,
  selectedLocationName,
  onSelectLocation,
}: CaptureLocationControlProps) {
  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button
          type="button"
          variant="outline"
          className={cn(PERFORMANCE_DATE_BUTTON_CLASS, "gap-1.5")}
          aria-label="Select Owned location"
        >
          <MapPinIcon className="size-3.5 shrink-0" aria-hidden />
          <span className="max-w-[10rem] truncate">{selectedLocationName}</span>
          <ChevronDownIcon className="size-3.5 shrink-0" aria-hidden />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent
        align="end"
        className={OPERATOR_SHELL_MENU_PANEL_CLASS}
      >
        <DropdownMenuRadioGroup
          value={String(selectedLocationId)}
          onValueChange={(value) => {
            const nextId = Number.parseInt(value, 10)
            if (Number.isFinite(nextId)) {
              onSelectLocation(nextId)
            }
          }}
        >
          {locations.map((location) => (
            <DropdownMenuRadioItem
              key={location.id}
              value={String(location.id)}
              className={cn(
                OPERATOR_SHELL_MENU_ITEM_CLASS,
                location.id === selectedLocationId &&
                  OPERATOR_SHELL_MENU_ITEM_SELECTED_CLASS
              )}
            >
              {location.locationName}
            </DropdownMenuRadioItem>
          ))}
        </DropdownMenuRadioGroup>
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
