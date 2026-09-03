import { Outlet, useOutletContext } from "react-router-dom"

import type { DashboardOutletContext } from "@/components/dashboard/operator/Dashboard"

/**
 * Reports layout — nests hub + report sub-pages under one route tree.
 * Forwards Dashboard outlet context so child routes keep location / mode.
 */
export function ReportsLayoutRoute() {
  const dashboardContext = useOutletContext<DashboardOutletContext | undefined>()

  return <Outlet context={dashboardContext} />
}
