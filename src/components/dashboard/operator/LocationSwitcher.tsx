import { Fragment, useState } from "react"
import { Link, useLocation } from "react-router-dom"
import { ChevronDownIcon, XIcon } from "lucide-react"

import { BrandLogoMark } from "@/components/brand/BrandLogoMark"
import { OperatorSearchIcon } from "@/components/dashboard/operator/OperatorSearchIcon"
import { Badge } from "@/components/ui/badge"
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
  operatorDashboardLocationsAddPath,
  type OperatorDashboardMode,
} from "@/lib/operatorHome/operatorDashboardPaths"
import {
  OPERATOR_LOCATION_SWITCHER_COMPACT_WIDTH_CLASS,
  OPERATOR_LOCATION_SWITCHER_FULL_WIDTH_CLASS,
} from "@/lib/operatorHome/shellResponsivePresentation"
import { cn } from "@/lib/utils"
import type {
  OperatorHomeLocationOption,
  OperatorShellPresentation,
} from "@/types/operatorHome"

function operatorDashboardModeFromPathname(
  pathname: string
): OperatorDashboardMode {
  return pathname.startsWith("/multi-dashboard") ? "multi" : "single"
}

type LocationSwitcherProps = {
  locationSwitcher: OperatorShellPresentation["locationSwitcher"]
  onSelectLocation: (locationId: number) => void
  className?: string
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
export const LOCATION_SWITCHER_PANEL_WIDTH_CLASS =
  "w-[min(433px,calc(100vw-1rem))]"

/** Popover chrome shared by navbar LocationSwitcher and Shop location picker. */
export const LOCATION_SWITCHER_POPOVER_CONTENT_CLASS = cn(
  LOCATION_SWITCHER_PANEL_WIDTH_CLASS,
  "!rounded-none gap-0 p-0",
  "bg-op-background-secondary text-op-header-location-heading shadow-[0_4px_11px_rgba(0,0,0,0.06),0_18px_20px_rgba(0,0,0,0.05)]",
  "ring-0",
  "animate-none data-open:animate-none data-closed:animate-none"
)

/** Figma Cards/Border-colour — location row dividers. */
const locationDividerClass = "h-px w-full shrink-0 bg-op-border-default"

/** Figma location row subtitle: `{address} · Active|Paused` (node 3714:22095). */
function formatLocationSwitcherStatusLine(
  location: OperatorHomeLocationOption
): string {
  const status = location.showPausedBadge
    ? "Paused"
    : location.isActive
      ? "Active"
      : "Inactive"
  const address = location.address.trim()
  return address.length > 0 ? `${address} · ${status}` : status
}

export type LocationSwitcherPanelProps = {
  selectedLocationName: string
  selectedLocationId: number
  brandLogoPublicUrl: string | null
  options: OperatorHomeLocationOption[]
  addLocationPath: string
  onSelectLocation: (locationId: number) => void
  onAddLocationNavigate: () => void
}

export function LocationSwitcherPanel({
  selectedLocationName,
  selectedLocationId,
  brandLogoPublicUrl,
  options,
  addLocationPath,
  onSelectLocation,
  onAddLocationNavigate,
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
        <BrandLogoMark
          className="size-8"
          brandLogoPublicUrl={brandLogoPublicUrl}
        />
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
              "h-auto min-h-0 flex-1 rounded-none border-0 px-0 py-0 shadow-none",
              // Same fill as the search chrome — beats Input default `dark:bg-input/30`.
              "bg-op-action-secondary dark:bg-op-action-secondary",
              "text-sm font-medium text-op-header-location-heading",
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
                      className="flex w-full min-w-0 items-center gap-2 text-sm font-semibold leading-normal text-op-header-location-heading"
                      title={location.name}
                    >
                      {location.showPausedBadge ? (
                        <Badge variant="neutral" className="shrink-0">
                          Paused
                        </Badge>
                      ) : null}
                      <span className="truncate">{location.name}</span>
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
          className="h-10 min-h-10 shrink-0 self-start"
          asChild
        >
          <Link to={addLocationPath} onClick={onAddLocationNavigate}>
            Add location
          </Link>
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
  const { pathname } = useLocation()
  const addLocationPath = operatorDashboardLocationsAddPath(
    operatorDashboardModeFromPathname(pathname),
    locationSwitcher.selectedLocationId
  )
  const selectedOption = locationSwitcher.options.find(
    (location) => location.id === locationSwitcher.selectedLocationId
  )
  const selectedPaused = selectedOption?.showPausedBadge === true

  const body = (
    <span className="flex min-w-0 items-center gap-3 overflow-hidden">
      <span className="flex min-w-0 items-center gap-2 overflow-hidden">
        <span className="hidden lg:contents">
          <BrandLogoMark
            className="size-[26px]"
            brandLogoPublicUrl={locationSwitcher.brandLogoPublicUrl}
          />
        </span>
        <span className="flex min-w-0 flex-col items-start overflow-hidden text-left font-semibold">
          <span className="hidden text-[10px] leading-normal text-op-header-location-subheading lg:block">
            Restaurant
          </span>
          <span
            className="flex max-w-full items-center gap-2 text-sm leading-normal text-op-header-location-heading"
            title={locationSwitcher.selectedLocationName}
          >
            {selectedPaused ? (
              <Badge variant="neutral" className="shrink-0">
                Paused
              </Badge>
            ) : null}
            <span className="truncate">
              {locationSwitcher.selectedLocationName}
            </span>
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
        className={LOCATION_SWITCHER_POPOVER_CONTENT_CLASS}
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
              brandLogoPublicUrl={locationSwitcher.brandLogoPublicUrl}
              options={locationSwitcher.options}
              addLocationPath={addLocationPath}
              onSelectLocation={(locationId) => {
                onSelectLocation(locationId)
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
