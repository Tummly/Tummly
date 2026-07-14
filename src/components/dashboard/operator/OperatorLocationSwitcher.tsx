import { ChevronDownIcon } from "lucide-react"

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

/** Owned-location control for the Operator navbar (Figma “Restaurant: …” chrome). */
export function OperatorLocationSwitcher({
  locationSwitcher,
  onSelectLocation,
  className,
}: OperatorLocationSwitcherProps) {
  const body = (
    <span className="flex min-w-0 flex-1 items-center justify-between gap-3 overflow-hidden">
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
      {locationSwitcher.interactive ? (
        <ChevronDownIcon
          className="size-4 shrink-0 text-[#141414] dark:text-white"
          aria-hidden
        />
      ) : null}
    </span>
  )

  // Figma: light white pill / dark #2A2A2A, fixed 305×45, radius 4px
  const surfaceClass = cn(
    "flex h-[45px] w-[305px] shrink-0 items-center overflow-hidden rounded-[4px] px-3 py-2",
    "bg-white dark:bg-[#2a2a2a]",
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
            "hover:bg-white hover:text-foreground",
            "aria-expanded:bg-white data-[state=open]:bg-white",
            "dark:hover:bg-[#2a2a2a] dark:aria-expanded:bg-[#2a2a2a]",
            "dark:data-[state=open]:bg-[#2a2a2a] dark:hover:text-white",
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
