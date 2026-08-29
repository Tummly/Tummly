import { useOutletContext } from "react-router-dom"
import type { DashboardOutletContext } from "@/components/dashboard/operator/Dashboard"
import { ShopPage } from "@/components/dashboard/operator/Shop/ShopPage"

export function ShopRoute() {
  const { selectedLocationId, locations, mode, selectLocation } =
    useOutletContext<DashboardOutletContext>()

  return (
    <ShopPage
      selectedLocationId={selectedLocationId}
      locations={locations}
      mode={mode}
      onSelectLocation={selectLocation}
    />
  )
}
