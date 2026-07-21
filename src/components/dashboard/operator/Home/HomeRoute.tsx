import { useOutletContext } from "react-router-dom"

import type { DashboardOutletContext } from "@/components/dashboard/operator/Dashboard"
import { HomePage } from "@/components/dashboard/operator/Home/HomePage"

export function HomeRoute() {
  const { activationPeriodBadge } =
    useOutletContext<DashboardOutletContext>()

  return <HomePage activationPeriodBadge={activationPeriodBadge} />
}
