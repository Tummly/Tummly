import { MapPin, Pencil } from "lucide-react"

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

export function SignupLocationsList({
  locations,
  onEdit,
  className,
}: SignupLocationsListProps) {
  if (locations.length === 0) {
    return null
  }

  return (
    <div className={cn("flex w-full flex-col gap-4", className)}>
      <h2 className="m-0 font-jakarta text-lg font-semibold tracking-[-0.36px] text-[#141414]">
        Your Locations
      </h2>
      <ul className="m-0 flex list-none flex-col gap-3 p-0">
        {locations.map((location) => {
          const subtitle = locationSubtitle(location)
          return (
            <li
              key={location.id}
              className="flex items-center justify-between gap-3 rounded-[8px] border border-[#e8e8e8] bg-white px-4 py-3"
            >
              <div className="flex min-w-0 items-center gap-3">
                <span className="flex size-10 shrink-0 items-center justify-center rounded-[8px] bg-[#f3f3f3] text-[#141414]">
                  <MapPin className="size-5" aria-hidden />
                </span>
                <div className="flex min-w-0 flex-col gap-1">
                  <div className="flex flex-wrap items-center gap-2">
                    <p className="m-0 truncate font-sans text-base font-semibold text-[#141414]">
                      {location.name}
                    </p>
                    <Badge variant="ready">Ready</Badge>
                  </div>
                  {subtitle ? (
                    <p className="m-0 truncate font-sans text-sm text-[#5c5c5c]">
                      {subtitle}
                    </p>
                  ) : null}
                </div>
              </div>
              <Button
                type="button"
                variant="ghost"
                size="icon"
                aria-label={`Edit ${location.name}`}
                onClick={() => onEdit(location.id)}
                className="size-10 shrink-0 rounded-[8px] text-[#141414] hover:bg-[#f3f3f3]"
              >
                <Pencil className="size-4" aria-hidden />
              </Button>
            </li>
          )
        })}
      </ul>
    </div>
  )
}
