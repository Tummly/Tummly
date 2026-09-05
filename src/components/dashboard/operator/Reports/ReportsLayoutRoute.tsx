import { Outlet, useOutletContext } from "react-router-dom"

import type { DashboardOutletContext } from "@/components/dashboard/operator/Dashboard"
import { ReportsPageModuleProvider } from "@/components/dashboard/operator/Reports/ReportsPageModuleProvider"

/**
 * Reports layout — nests hub + report sub-pages under one route tree.
 * Owns the Reports page module + one Export dialog mount.
 */
export function ReportsLayoutRoute() {
  const dashboardContext = useOutletContext<DashboardOutletContext | undefined>()

  return (
    <ReportsPageModuleProvider>
      <Outlet context={dashboardContext} />
    </ReportsPageModuleProvider>
  )
}
