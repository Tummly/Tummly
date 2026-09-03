import { useState } from "react"
import { ChevronDown, MapPin } from "lucide-react"

import {
  LOCATION_SWITCHER_POPOVER_CONTENT_CLASS,
  LocationSwitcherPanel,
} from "@/components/dashboard/operator/LocationSwitcher"
import { Button } from "@/components/ui/button"
import {
  Popover,
  PopoverContent,
  PopoverTitle,
  PopoverTrigger,
} from "@/components/ui/popover"
import {
  operatorDashboardLocationsAddPath,
  type OperatorDashboardMode,
} from "@/lib/operatorHome/operatorDashboardPaths"
import { cn } from "@/lib/utils"
import type { OperatorHomeLocationOption } from "@/types/operatorHome"

export type ShopLocationOption = {
  id: number
  locationName: string
  address: string
  lifecycleStatus?: "active" | "paused"
}

type ShopLocationPickerProps = {
  variant: "chip" | "change"
  selectedLocationId: number
  selectedLocationName: string
  locations: ShopLocationOption[]
  brandLogoPublicUrl: string | null
  mode: OperatorDashboardMode
  onSelectLocation?: (locationId: number) => void
  /** Optional toast / side-effect after a location is chosen (Checkout). */
  onAfterSelectLocation?: (location: ShopLocationOption) => void
  /** Chip height / padding tweaks — Orders uses a slightly taller chip. */
  chipClassName?: string
  changeClassName?: string
  align?: "start" | "end"
}

function toSwitcherOptions(
  locations: ShopLocationOption[]
): OperatorHomeLocationOption[] {
  return locations.map((location) => {
    const paused = location.lifecycleStatus === "paused"
    return {
      id: location.id,
      name: location.locationName,
      address: location.address,
      isActive: !paused,
      showPausedBadge: paused,
    }
  })
}

/**
 * Shop location control — keeps existing Shop trigger chrome; opens the same
 * LocationSwitcher panel as the Dashboard navbar.
 */
export function ShopLocationPicker({
  variant,
  selectedLocationId,
  selectedLocationName,
  locations,
  brandLogoPublicUrl,
  mode,
  onSelectLocation,
  onAfterSelectLocation,
  chipClassName,
  changeClassName,
  align = variant === "chip" ? "end" : "start",
}: ShopLocationPickerProps) {
  const [open, setOpen] = useState(false)
  const interactive = locations.length > 1 && onSelectLocation != null

  const chipStaticClass = cn(
    "inline-flex h-[42px] items-center gap-1.5 rounded-op-sm border border-op-button-tertiary-border bg-transparent px-op-4 py-op-3 text-op-sm font-medium text-op-button-tertiary-text",
    chipClassName
  )

  if (!interactive) {
    if (variant === "change") {
      return null
    }

    return (
      <div className={chipStaticClass}>
        <MapPin className="size-3.5" />
        <span className="max-w-[140px] truncate">{selectedLocationName}</span>
      </div>
    )
  }

  const addLocationPath = operatorDashboardLocationsAddPath(
    mode,
    selectedLocationId
  )
  const options = toSwitcherOptions(locations)

  const trigger =
    variant === "chip" ? (
      <Button
        type="button"
        variant="op-tertiary"
        className={cn("h-[42px] gap-1.5", chipClassName)}
        aria-label="Select Owned location"
      >
        <MapPin className="size-3.5" />
        <span className="max-w-[120px] truncate">{selectedLocationName}</span>
        <ChevronDown className="size-3" />
      </Button>
    ) : (
      <Button
        type="button"
        variant="op-tertiary"
        className={changeClassName}
      >
        Change location
      </Button>
    )

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>{trigger}</PopoverTrigger>
      <PopoverContent
        align={align}
        sideOffset={8}
        className={LOCATION_SWITCHER_POPOVER_CONTENT_CLASS}
        onOpenAutoFocus={(event) => {
          event.preventDefault()
          const root = event.currentTarget
          if (!(root instanceof HTMLElement)) return
          root
            .querySelector<HTMLInputElement>('[aria-label="Search locations"]')
            ?.focus()
        }}
      >
        {open ? (
          <>
            <PopoverTitle className="sr-only">Select Owned location</PopoverTitle>
            <LocationSwitcherPanel
              selectedLocationName={selectedLocationName}
              selectedLocationId={selectedLocationId}
              brandLogoPublicUrl={brandLogoPublicUrl}
              options={options}
              addLocationPath={addLocationPath}
              onSelectLocation={(locationId) => {
                onSelectLocation(locationId)
                const selected = locations.find((loc) => loc.id === locationId)
                if (selected) {
                  onAfterSelectLocation?.(selected)
                }
                setOpen(false)
              }}
              onAddLocationNavigate={() => {
                setOpen(false)
              }}
            />
          </>
        ) : null}
      </PopoverContent>
    </Popover>
  )
}
