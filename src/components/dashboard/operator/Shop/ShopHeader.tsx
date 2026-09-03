import {
  ShopLocationPicker,
  type ShopLocationOption,
} from "@/components/dashboard/operator/Shop/ShopLocationPicker"
import type { OperatorDashboardMode } from "@/lib/operatorHome/operatorDashboardPaths"

type ShopHeaderProps = {
  selectedLocationId: number
  selectedLocationName: string
  locations: ShopLocationOption[]
  brandLogoPublicUrl: string | null
  mode: OperatorDashboardMode
  onSelectLocation?: (locationId: number) => void
}

export function ShopHeader({
  selectedLocationId,
  selectedLocationName,
  locations,
  brandLogoPublicUrl,
  mode,
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
        <ShopLocationPicker
          variant="chip"
          selectedLocationId={selectedLocationId}
          selectedLocationName={selectedLocationName}
          locations={locations}
          brandLogoPublicUrl={brandLogoPublicUrl}
          mode={mode}
          onSelectLocation={onSelectLocation}
        />
      </div>
    </div>
  )
}
