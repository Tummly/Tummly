import { MapPin, ChevronDown } from "lucide-react"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { Button } from "@/components/ui/button"
import { cn } from "@/lib/utils"

type ShopHeaderProps = {
  selectedLocationName: string
  locations: Array<{ id: number; locationName: string; address: string }>
  onSelectLocation?: (locationId: number) => void
}

export function ShopHeader({
  selectedLocationName,
  locations,
  onSelectLocation,
}: ShopHeaderProps) {
  return (
    <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
      <div className="flex flex-col gap-1">
        <h1 className="text-2xl font-bold tracking-tight text-foreground sm:text-3xl">
          Tummly Shop
        </h1>
        <p className="text-sm font-normal text-muted-foreground">
          Materials ordered here will be branded and connected to the guest experience for this location.
        </p>
      </div>

      <div className="flex items-center self-start sm:self-center">
        {locations.length > 1 && onSelectLocation ? (
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button
                variant="outline"
                size="sm"
                className="h-8 gap-1.5 rounded-md border border-op-border-default bg-op-surface-secondary px-3 text-xs font-medium text-foreground hover:bg-op-card-background"
              >
                <MapPin className="size-3.5 text-muted-foreground" />
                <span className="max-w-[120px] truncate">{selectedLocationName}</span>
                <ChevronDown className="size-3 text-muted-foreground" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end" className="w-48 bg-op-surface-secondary">
              {locations.map((loc) => (
                <DropdownMenuItem
                  key={loc.id}
                  onClick={() => onSelectLocation(loc.id)}
                  className={cn(
                    "cursor-pointer text-xs",
                    loc.locationName === selectedLocationName && "font-semibold text-op-action-primary"
                  )}
                >
                  {loc.locationName}
                </DropdownMenuItem>
              ))}
            </DropdownMenuContent>
          </DropdownMenu>
        ) : (
          <div className="inline-flex h-8 items-center gap-1.5 rounded-md border border-op-border-default bg-op-surface-secondary px-3 text-xs font-medium text-foreground">
            <MapPin className="size-3.5 text-muted-foreground" />
            <span className="max-w-[140px] truncate">{selectedLocationName}</span>
          </div>
        )}
      </div>
    </div>
  )
}
