import { useOutletContext } from "react-router-dom"

import { ReportsPage } from "@/components/dashboard/operator/Reports/ReportsPage"
import type { DashboardOutletContext } from "@/components/dashboard/operator/Dashboard"

export function ReportsRoute() {
  const context = useOutletContext<DashboardOutletContext | undefined>()
  const mode = context?.mode ?? "single"

  return <ReportsPage mode={mode} />
}
