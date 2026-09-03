import { useOutletContext, useSearchParams } from "react-router-dom"

import { CaptureReportPage } from "@/components/dashboard/operator/Reports/CaptureReportPage"
import type { DashboardOutletContext } from "@/components/dashboard/operator/Dashboard"

export function CaptureReportRoute() {
  const context = useOutletContext<DashboardOutletContext | undefined>()
  const [searchParams] = useSearchParams()

  const selectedLocationId = context?.selectedLocationId ?? 1
  const locations = context?.locations ?? []
  const mode = context?.mode ?? "single"

  const selectedLocation =
    locations.find((l) => l.id === selectedLocationId) ?? locations[0]
  const locationName = selectedLocation?.locationName ?? "Mehmet's Grill"

  return (
    <CaptureReportPage
      selectedLocationId={selectedLocationId}
      selectedLocationName={locationName}
      locations={locations}
      mode={mode}
      isEmpty={searchParams.get("empty") === "true"}
    />
  )
}
