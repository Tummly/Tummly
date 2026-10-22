import { Pencil, Store } from "lucide-react"

import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { cn } from "@/lib/utils"

export type SignupLocationListItem = {
  id: number
  name: string
  city: string | null
  postcode: string | null
}

type SignupLocationsListProps = {
  locations: SignupLocationListItem[]
  onEdit: (locationId: number) => void
  className?: string
}

function locationSubtitle(location: SignupLocationListItem): string {
  const parts = [location.city?.trim(), location.postcode?.trim()].filter(
    (part): part is string => Boolean(part && part.length > 0)
  )
  return parts.join(", ")
}

/** "Your Locations" list — Figma `6780:42370`. */
export function SignupLocationsList({
  locations,
  onEdit,
  className,
}: SignupLocationsListProps) {
  if (locations.length === 0) {
    return null
  }

  return (
    <div className={cn("flex w-full flex-col gap-3", className)}>
      <h2 className="m-0 !font-sans text-sm font-bold leading-5 text-[#141414]">
        Your Locations
      </h2>
      <ul className="m-0 flex list-none flex-col gap-3.5 p-0">
        {locations.map((location) => {
          const subtitle = locationSubtitle(location)
          return (
            <li
              key={location.id}
              className="flex items-center justify-between gap-3 rounded-[8px] border border-[#d2d2d2] px-[22px] py-[18px]"
            >
              <div className="flex min-w-0 items-center gap-2">
                <span className="flex size-[34px] shrink-0 items-center justify-center text-[#141414]">
                  <Store className="size-[22px]" strokeWidth={1.5} aria-hidden />
                </span>
                <div className="flex min-w-0 items-start gap-3.5">
                  <div className="flex min-w-0 flex-col font-sans leading-5 text-[#141414]">
                    <p className="m-0 truncate text-sm font-bold">
                      {location.name}
                    </p>
                    {subtitle ? (
                      <p className="m-0 truncate text-xs">{subtitle}</p>
                    ) : null}
                  </div>
                  <Badge variant="ready">Ready</Badge>
                </div>
              </div>
              <Button
                type="button"
                variant="ghost"
                size="icon-sm"
                aria-label={`Edit ${location.name}`}
                onClick={() => onEdit(location.id)}
                className="shrink-0 rounded-[4px] text-[#141414] hover:bg-[#f0f0f0]"
              >
                <Pencil className="size-4" strokeWidth={1.5} aria-hidden />
              </Button>
            </li>
          )
        })}
      </ul>
    </div>
  )
}
