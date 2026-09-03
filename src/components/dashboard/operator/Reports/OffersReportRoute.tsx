import { useOutletContext, useSearchParams } from "react-router-dom"
import { OffersReportPage } from "@/components/dashboard/operator/Reports/OffersReportPage"
import type { DashboardOutletContext } from "@/components/dashboard/operator/Dashboard"

export function OffersReportRoute() {
  const context = useOutletContext<DashboardOutletContext | undefined>()
  const [searchParams] = useSearchParams()

  const selectedLocationId = context?.selectedLocationId ?? 1
  const locations = context?.locations ?? []
  const mode = context?.mode ?? "single"

  const selectedLocation =
    locations.find((l) => l.id === selectedLocationId) ?? locations[0]
  const locationName = selectedLocation?.locationName ?? "Mehmet's Grill"

  const isEmpty = searchParams.get("empty") === "true"

  return (
    <OffersReportPage
      selectedLocationId={selectedLocationId}
      selectedLocationName={locationName}
      locations={locations}
      mode={mode}
      isEmpty={isEmpty}
    />
  )
}
