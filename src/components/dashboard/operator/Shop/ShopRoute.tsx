import { useMemo } from "react"
import { useOutletContext } from "react-router-dom"
import type { DashboardOutletContext } from "@/components/dashboard/operator/Dashboard"
import { ShopPage } from "@/components/dashboard/operator/Shop/ShopPage"
import { resolveShopPaidWriteChrome } from "@/lib/operatorShop/shopPaidWriteChrome"

export function ShopRoute() {
  const {
    selectedLocationId,
    locations,
    mode,
    selectLocation,
    billingStatus,
    subscriptionPlan,
    permissionRole,
    billingCreditsAccess,
  } = useOutletContext<DashboardOutletContext>()

  const paidWriteChrome = useMemo(
    () =>
      resolveShopPaidWriteChrome({
        billingStatus,
        subscriptionPlan,
        accessLevel: billingCreditsAccess,
        permissionRole,
        mode,
        locationId: selectedLocationId,
      }),
    [
      billingCreditsAccess,
      billingStatus,
      mode,
      permissionRole,
      selectedLocationId,
      subscriptionPlan,
    ]
  )

  return (
    <ShopPage
      selectedLocationId={selectedLocationId}
      locations={locations}
      mode={mode}
      onSelectLocation={selectLocation}
      paidWriteChrome={paidWriteChrome}
    />
  )
}
