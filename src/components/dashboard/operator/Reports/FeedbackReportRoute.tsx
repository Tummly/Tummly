import { useOutletContext } from "react-router-dom"
import { FeedbackReportPage } from "@/components/dashboard/operator/Reports/FeedbackReportPage"
import type { DashboardOutletContext } from "@/components/dashboard/operator/Dashboard"

export function FeedbackReportRoute() {
  const context = useOutletContext<DashboardOutletContext | undefined>()

  const selectedLocationId = context?.selectedLocationId ?? 1
  const locations = context?.locations ?? []
  const mode = context?.mode ?? "single"

  const selectedLocation =
    locations.find((l) => l.id === selectedLocationId) ?? locations[0]
  const locationName = selectedLocation?.locationName ?? "Mehmet's Grill"

  return (
    <FeedbackReportPage
      selectedLocationId={selectedLocationId}
      selectedLocationName={locationName}
      locations={locations}
      mode={mode}
      isEmpty={false}
    />
  )
}
