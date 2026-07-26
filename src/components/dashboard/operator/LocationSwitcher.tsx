import { Fragment, useState } from "react"
import { ChevronDownIcon, XIcon } from "lucide-react"

import brandLogoPlaceholder from "@/assets/images/brand-logo-placeholder.png"
import { OperatorSearchIcon } from "@/components/dashboard/operator/OperatorSearchIcon"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import {
  Popover,
  PopoverContent,
  PopoverTitle,
  PopoverTrigger,
} from "@/components/ui/popover"
import { filterOwnedLocations } from "@/lib/operatorHome/filterOwnedLocations"
import {
  OPERATOR_LOCATION_SWITCHER_COMPACT_WIDTH_CLASS,
  OPERATOR_LOCATION_SWITCHER_FULL_WIDTH_CLASS,
} from "@/lib/operatorHome/shellResponsivePresentation"
import { cn } from "@/lib/utils"
import type {
  OperatorHomeLocationOption,
  OperatorShellPresentation,
} from "@/types/operatorHome"

type LocationSwitcherProps = {
  locationSwitcher: OperatorShellPresentation["locationSwitcher"]
  onSelectLocation: (locationId: number) => void
  className?: string
}

/** Shared Brand logo until Settings upload + blob storage exist (CONTEXT.md). */
function BrandLogoSlot({ sizeClass }: { sizeClass: string }) {
  return (
    <span
      className={cn(
        "relative shrink-0 overflow-hidden rounded-[2px]",
        sizeClass
      )}
      aria-hidden
    >
      <img
        src={brandLogoPlaceholder}
        alt=""
        className="size-full object-cover"
      />
    </span>
  )
}

// Figma: same muted utility surface as search.
// Compact (<lg): name only, capped width. Full (≥lg): content-sized beside flex search.
const triggerSurfaceClass = cn(
  "flex h-8 min-h-8 w-auto items-center overflow-hidden rounded-op-sm px-2.5 py-1.5 lg:h-10 lg:min-h-10 lg:px-3 lg:py-2",
  "bg-op-header-location-background",
  OPERATOR_LOCATION_SWITCHER_COMPACT_WIDTH_CLASS,
  OPERATOR_LOCATION_SWITCHER_FULL_WIDTH_CLASS
)

/** Figma panel width (433px) — wider than the 305px navbar trigger; fluid on narrow viewports. */
const locationPanelWidthClass = "w-[min(433px,calc(100vw-1rem))]"

/** Figma Cards/Border-colour — location row dividers. */
const locationDividerClass = "h-px w-full shrink-0 bg-op-border-default"

/** Figma location row subtitle: `{address} · Active` (node 3714:22095). */
function formatLocationSwitcherStatusLine(
  location: OperatorHomeLocationOption
): string {
  const status = location.isActive ? "Active" : "Inactive"
  const address = location.address.trim()
  return address.length > 0 ? `${address} · ${status}` : status
}

type LocationSwitcherPanelProps = {
  selectedLocationName: string
  selectedLocationId: number
  options: OperatorHomeLocationOption[]
  onSelectLocation: (locationId: number) => void
}

function LocationSwitcherPanel({
  selectedLocationName,
  selectedLocationId,
  options,
  onSelectLocation,
}: LocationSwitcherPanelProps) {
  const [query, setQuery] = useState("")
  const filtered = filterOwnedLocations(options, query).filter(
    (location) => location.id !== selectedLocationId
  )
  const hasSearchQuery = query.trim().length > 0

  const locationRowClass = cn(
    "flex w-full cursor-pointer flex-col items-start gap-1 rounded-op-sm p-0 text-left",
    "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-foreground/20"
  )

  return (
    <div className="flex w-full flex-col gap-px py-1">
      <div className="flex items-center gap-3 p-5">
        <BrandLogoSlot sizeClass="size-8" />
        <div className="flex min-w-0 flex-col items-start overflow-hidden">
          <span
            className="truncate text-sm font-semibold leading-normal text-op-header-location-heading"
            title={selectedLocationName}
          >
            {selectedLocationName}
          </span>
          <span className="text-[10px] font-medium leading-normal text-op-header-location-subheading">
            Current restaurant
          </span>
        </div>
      </div>

      <div className="px-5">
        <label className="relative flex h-10 items-center gap-3 rounded-op-sm bg-op-action-secondary px-3.5">
          <OperatorSearchIcon className="text-op-header-search-text" />
          <Input
            type="text"
            value={query}
            onChange={(event) => setQuery(event.target.value)}
            placeholder="Search locations"
            aria-label="Search locations"
            autoFocus
            className={cn(
              "h-auto min-h-0 flex-1 rounded-none border-0 bg-transparent px-0 py-0",
              "text-sm font-medium text-op-header-location-heading shadow-none",
              "placeholder:text-op-header-search-text focus-visible:border-0 focus-visible:ring-0"
            )}
          />
          {hasSearchQuery ? (
            <button
              type="button"
              onClick={() => setQuery("")}
              aria-label="Clear search"
              className="inline-flex size-4 shrink-0 items-center justify-center text-op-header-search-text hover:text-op-header-location-heading"
            >
              <XIcon className="size-4" aria-hidden />
            </button>
          ) : null}
        </label>
      </div>

      <div className="flex flex-col gap-5 p-5">
        <div className="flex w-full flex-col gap-3">
          <p className="text-base font-medium leading-normal text-op-header-location-subheading">
            Locations
          </p>

          {filtered.length === 0 ? (
            hasSearchQuery ? (
              <p className="text-sm font-medium text-op-header-location-subheading">
                No locations found
              </p>
            ) : null
          ) : (
            filtered.map((location) => {
              const statusLine = formatLocationSwitcherStatusLine(location)
              return (
                <Fragment key={location.id}>
                  <button
                    type="button"
                    onClick={() => onSelectLocation(location.id)}
                    className={locationRowClass}
                  >
                    <span
                      className="w-full truncate text-sm font-semibold leading-normal text-op-header-location-heading"
                      title={location.name}
                    >
                      {location.name}
                    </span>
                    <span
                      className="w-full truncate text-xs font-medium leading-normal text-op-header-location-subheading"
                      title={statusLine}
                    >
                      {statusLine}
                    </span>
                  </button>
                  <div
                    role="separator"
                    aria-hidden
                    className={locationDividerClass}
                  />
                </Fragment>
              )
            })
          )}
        </div>

        <Button
          type="button"
          variant="op-secondary"
          disabled
          className="h-10 min-h-10 shrink-0 self-start"
        >
          Add location
        </Button>
      </div>
    </div>
  )
}

/** Owned-location control for the Operator navbar (Figma “Restaurant: …” chrome). */
export function LocationSwitcher({
  locationSwitcher,
  onSelectLocation,
  className,
}: LocationSwitcherProps) {
  const [open, setOpen] = useState(false)

  const body = (
    <span className="flex min-w-0 items-center gap-3 overflow-hidden">
      <span className="flex min-w-0 items-center gap-2 overflow-hidden">
        <span className="hidden lg:contents">
          <BrandLogoSlot sizeClass="size-[26px]" />
        </span>
        <span className="flex min-w-0 flex-col items-start overflow-hidden text-left font-semibold">
          <span className="hidden text-[10px] leading-normal text-op-header-location-subheading lg:block">
            Restaurant
          </span>
          <span
            className="block max-w-full truncate text-sm leading-normal text-op-header-location-heading"
            title={locationSwitcher.selectedLocationName}
          >
            {locationSwitcher.selectedLocationName}
          </span>
        </span>
      </span>
      {locationSwitcher.interactive ? (
        <ChevronDownIcon
          className="size-4 shrink-0 text-op-header-location-heading"
          aria-hidden
        />
      ) : null}
    </span>
  )

  const surfaceClass = cn(triggerSurfaceClass, className)

  if (!locationSwitcher.interactive) {
    return (
      <div
        className={surfaceClass}
        title={locationSwitcher.selectedLocationName}
      >
        {body}
      </div>
    )
  }

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <Button
          type="button"
          variant="ghost"
          className={cn(
            "flex justify-start overflow-hidden whitespace-normal text-foreground",
            "hover:bg-op-header-location-background hover:text-foreground",
            "aria-expanded:bg-op-header-location-background data-[state=open]:bg-op-header-location-background",
            surfaceClass,
            // Beat Button default size `h-[38px] min-h-[38px]` (must come last for twMerge).
            "h-8! min-h-8! gap-0 px-2.5! py-1.5 lg:h-10! lg:min-h-10! lg:px-3! lg:py-2"
          )}
          aria-label="Select Owned location"
        >
          {body}
        </Button>
      </PopoverTrigger>
      <PopoverContent
        align="start"
        sideOffset={8}
        className={cn(
          locationPanelWidthClass,
          "!rounded-none gap-0 p-0",
          "bg-op-background-secondary text-op-header-location-heading shadow-[0_4px_11px_rgba(0,0,0,0.06),0_18px_20px_rgba(0,0,0,0.05)]",
          "ring-0",
          "animate-none data-open:animate-none data-closed:animate-none"
        )}
        onOpenAutoFocus={(event) => {
          // Prefer the search field over the first location control.
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
            <PopoverTitle className="sr-only">
              Select Owned location
            </PopoverTitle>
            <LocationSwitcherPanel
              selectedLocationName={locationSwitcher.selectedLocationName}
              selectedLocationId={locationSwitcher.selectedLocationId}
              options={locationSwitcher.options}
              onSelectLocation={(locationId) => {
                onSelectLocation(locationId)
                setOpen(false)
              }}
            />
          </>
        ) : null}
      </PopoverContent>
    </Popover>
  )
}
