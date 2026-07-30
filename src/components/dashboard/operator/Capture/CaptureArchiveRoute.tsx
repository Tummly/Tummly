import { useMemo } from "react"
import { useLocation, useOutletContext, useSearchParams } from "react-router-dom"

import { CaptureArchivePage } from "@/components/dashboard/operator/Capture/CaptureArchivePage"
import type { DashboardOutletContext } from "@/components/dashboard/operator/Dashboard"
import {
  operatorDashboardNavPath,
  type OperatorDashboardMode,
} from "@/lib/operatorHome/operatorDashboardPaths"

type CaptureArchiveRouteProps = {
  mode: OperatorDashboardMode
}

/** Archive route — loads account-wide archived placements via Capture page module. */
export function CaptureArchiveRoute({ mode }: CaptureArchiveRouteProps) {
  const { selectedLocationId, locations } =
    useOutletContext<DashboardOutletContext>()
  const location = useLocation()
  const [searchParams] = useSearchParams()

  const defaultReturnPath = useMemo(() => {
    if (mode === "single") {
      return operatorDashboardNavPath(
        "single",
        "capture",
        selectedLocationId ?? locations[0]?.id ?? 0
      )
    }
    return "/multi-dashboard/capture"
  }, [locations, mode, selectedLocationId])

  const fromParam = searchParams.get("from")
  const stateFrom = (location.state as { from?: string } | null)?.from

  return (
    <CaptureArchivePage
      mode={mode}
      locations={locations.map((l) => ({
        id: l.id,
        locationName: l.locationName,
      }))}
      defaultReturnPath={fromParam ?? stateFrom ?? defaultReturnPath}
    />
  )
}
