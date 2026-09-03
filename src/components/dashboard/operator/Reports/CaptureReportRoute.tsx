import { useOutletContext } from "react-router-dom"

import { CaptureReportPage } from "@/components/dashboard/operator/Reports/CaptureReportPage"
import type { DashboardOutletContext } from "@/components/dashboard/operator/Dashboard"

export function CaptureReportRoute() {
  const context = useOutletContext<DashboardOutletContext | undefined>()
  const mode = context?.mode ?? "single"

  return <CaptureReportPage mode={mode} />
}
