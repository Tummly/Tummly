import { ChevronDownIcon } from "lucide-react"

import brandLogoPlaceholder from "@/assets/images/brand-logo-placeholder.png"
import { Button } from "@/components/ui/button"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuGroup,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { cn } from "@/lib/utils"
import type { OperatorShellPresentation } from "@/types/operatorHome"

type OperatorLocationSwitcherProps = {
  locationSwitcher: OperatorShellPresentation["locationSwitcher"]
  onSelectLocation: (locationId: number) => void
  className?: string
}

/** Shared Brand logo until Settings upload + blob storage exist (CONTEXT.md). */
function BrandLogoSlot() {
  return (
    <span
      className="relative size-[26px] shrink-0 overflow-hidden rounded-[2px]"
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

/** Owned-location control for the Operator navbar (Figma “Restaurant: …” chrome). */
export function OperatorLocationSwitcher({
  locationSwitcher,
  onSelectLocation,
  className,
}: OperatorLocationSwitcherProps) {
  const body = (
    <span className="flex min-w-0 flex-1 items-center justify-between gap-3 overflow-hidden">
      <span className="flex min-w-0 flex-1 items-center gap-2 overflow-hidden">
        <BrandLogoSlot />
        <span className="flex min-w-0 flex-1 flex-col items-start overflow-hidden text-left font-semibold">
          <span className="text-[10px] leading-normal text-[#9e9e9e]">
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

  // Figma: same muted utility surface as search (`#212121` dark / black/5 light).
  const surfaceClass = cn(
    "flex h-10 min-h-10 w-[305px] shrink-0 items-center overflow-hidden rounded-[2px] px-3 py-2",
    "bg-black/5 dark:bg-[#212121]",
    className
  )

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
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button
          type="button"
          variant="ghost"
          className={cn(
            "flex justify-start overflow-hidden whitespace-normal text-foreground",
            "hover:bg-black/5 hover:text-foreground",
            "aria-expanded:bg-black/5 data-[state=open]:bg-black/5",
            "dark:hover:bg-[#212121] dark:aria-expanded:bg-[#212121]",
            "dark:data-[state=open]:bg-[#212121] dark:hover:text-white",
            surfaceClass
          )}
          aria-label="Select Owned location"
        >
          {body}
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent
        align="start"
        className="min-w-[12rem] w-[var(--radix-dropdown-menu-trigger-width)]"
      >
        <DropdownMenuGroup>
          {locationSwitcher.options.map((location) => (
            <DropdownMenuItem
              key={location.id}
              onClick={() => onSelectLocation(location.id)}
              data-active={
                location.id === locationSwitcher.selectedLocationId
                  ? "true"
                  : undefined
              }
            >
              {location.name}
            </DropdownMenuItem>
          ))}
        </DropdownMenuGroup>
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
