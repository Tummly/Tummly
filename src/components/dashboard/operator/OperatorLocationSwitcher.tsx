import { Fragment, useState } from "react"
import { ChevronDownIcon, SearchIcon, XIcon } from "lucide-react"

import brandLogoPlaceholder from "@/assets/images/brand-logo-placeholder.png"
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

type OperatorLocationSwitcherProps = {
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

// Figma: same muted utility surface as search (`#ebebeb` light / `#212121` dark).
// Compact (<lg): name only, capped width. Full (≥lg): 305px Figma chrome.
const triggerSurfaceClass = cn(
  "flex h-8 min-h-8 items-center overflow-hidden rounded-[2px] px-2.5 py-1.5 lg:h-10 lg:min-h-10 lg:px-3 lg:py-2",
  "bg-[#ebebeb] dark:bg-[#212121]",
  OPERATOR_LOCATION_SWITCHER_COMPACT_WIDTH_CLASS,
  OPERATOR_LOCATION_SWITCHER_FULL_WIDTH_CLASS,
  "lg:max-w-none lg:flex-none lg:shrink-0"
)

/** Figma panel width (433px) — wider than the 305px navbar trigger; fluid on narrow viewports. */
const locationPanelWidthClass = "w-[min(433px,calc(100vw-1rem))]"

/** Figma Cards/Border-colour — location row dividers. */
const locationDividerClass = "h-px w-full shrink-0 bg-[#e5e5e5] dark:bg-[#262626]"

const panelSecondaryButtonClass = cn(
  "h-10 min-h-10 shrink-0 self-start rounded-[2px] border-transparent px-4 py-2.5",
  "text-sm font-medium leading-5 text-[#141414]",
  "bg-[#dfdfdf] hover:bg-[#dedede] hover:text-[#141414]",
  "disabled:pointer-events-none disabled:opacity-60",
  "dark:bg-[#333] dark:text-white dark:hover:bg-[#3d3d3d] dark:hover:text-white"
)

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
    "flex w-full cursor-pointer flex-col items-start gap-1 rounded-[2px] px-1.5 py-1.5 text-left",
    "transition-colors hover:bg-black/5 dark:hover:bg-white/10",
    "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#171717]/20 dark:focus-visible:ring-white/20"
  )

  return (
    <div className="flex w-full flex-col gap-px py-1">
      <div className="flex items-center gap-3 p-5">
        <BrandLogoSlot sizeClass="size-8" />
        <div className="flex min-w-0 flex-col items-start overflow-hidden">
          <span
            className="truncate text-sm font-semibold leading-normal text-[#171717] dark:text-white"
            title={selectedLocationName}
          >
            {selectedLocationName}
          </span>
          <span className="text-[10px] font-medium leading-normal text-[#969696] dark:text-[#7c7c7c]">
            Current restaurant
          </span>
        </div>
      </div>

      <div className="px-5">
        <label className="relative flex h-10 items-center gap-3 rounded-[2px] bg-[#dddddd] px-3.5 dark:bg-[#2a2a2a]">
          <SearchIcon
            className="size-4 shrink-0 text-[#707070]"
            aria-hidden
          />
          <Input
            type="text"
            value={query}
            onChange={(event) => setQuery(event.target.value)}
            placeholder="Search locations"
            aria-label="Search locations"
            autoFocus
            className={cn(
              "h-auto min-h-0 flex-1 rounded-none border-0 bg-transparent px-0 py-0",
              "text-sm font-medium text-[#171717] shadow-none",
              "placeholder:text-[#707070] focus-visible:border-0 focus-visible:ring-0",
              "dark:bg-transparent dark:text-white"
            )}
          />
          {hasSearchQuery ? (
            <button
              type="button"
              onClick={() => setQuery("")}
              aria-label="Clear search"
              className="inline-flex size-4 shrink-0 items-center justify-center text-[#707070] hover:text-[#171717] dark:hover:text-white"
            >
              <XIcon className="size-4" aria-hidden />
            </button>
          ) : null}
        </label>
      </div>

      <div className="flex flex-col gap-5 p-5">
        <div className="flex w-full flex-col gap-3">
          <p className="text-base font-medium leading-normal text-[#969696] dark:text-[#7c7c7c]">
            Locations
          </p>

          {filtered.length === 0 ? (
            hasSearchQuery ? (
              <p className="text-sm font-medium text-[#969696] dark:text-[#7c7c7c]">
                No locations found
              </p>
            ) : null
          ) : (
            filtered.map((location) => (
              <Fragment key={location.id}>
                <button
                  type="button"
                  onClick={() => onSelectLocation(location.id)}
                  className={locationRowClass}
                >
                  <span
                    className="w-full truncate text-sm font-semibold leading-normal text-[#171717] dark:text-white"
                    title={location.name}
                  >
                    {location.name}
                  </span>
                  {location.address.trim().length > 0 ? (
                    <span
                      className="w-full truncate text-xs font-medium leading-normal text-[#969696] dark:text-[#7c7c7c]"
                      title={location.address}
                    >
                      {location.address}
                    </span>
                  ) : null}
                </button>
                <div
                  role="separator"
                  aria-hidden
                  className={locationDividerClass}
                />
              </Fragment>
            ))
          )}
        </div>

        <Button
          type="button"
          variant="ghost"
          disabled
          className={panelSecondaryButtonClass}
        >
          Add location
        </Button>
      </div>
    </div>
  )
}

/** Owned-location control for the Operator navbar (Figma “Restaurant: …” chrome). */
export function OperatorLocationSwitcher({
  locationSwitcher,
  onSelectLocation,
  className,
}: OperatorLocationSwitcherProps) {
  const [open, setOpen] = useState(false)

  const body = (
    <span className="flex min-w-0 flex-1 items-center justify-between gap-2 overflow-hidden lg:gap-3">
      <span className="flex min-w-0 flex-1 items-center gap-2 overflow-hidden">
        <span className="hidden lg:contents">
          <BrandLogoSlot sizeClass="size-[26px]" />
        </span>
        <span className="flex min-w-0 flex-1 flex-col items-start overflow-hidden text-left font-semibold">
          <span className="hidden text-[10px] leading-normal text-[#9e9e9e] lg:block">
            Restaurant
          </span>
          <span
            className="block w-full truncate text-sm leading-normal text-[#141414] dark:text-white"
            title={locationSwitcher.selectedLocationName}
          >
            {locationSwitcher.selectedLocationName}
          </span>
        </span>
      </span>
      {locationSwitcher.interactive ? (
        <ChevronDownIcon
          className="size-4 shrink-0 text-[#141414] dark:text-white"
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
            "hover:bg-[#ebebeb] hover:text-foreground",
            "aria-expanded:bg-[#ebebeb] data-[state=open]:bg-[#ebebeb]",
            "dark:hover:bg-[#212121] dark:aria-expanded:bg-[#212121]",
            "dark:data-[state=open]:bg-[#212121] dark:hover:text-white",
            surfaceClass
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
          "bg-[#ebebeb] text-[#171717] shadow-[0_4px_11px_rgba(0,0,0,0.06),0_18px_20px_rgba(0,0,0,0.05)]",
          "ring-0 dark:bg-[#202020] dark:text-white",
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
