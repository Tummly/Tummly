import { WeeklyBriefPage } from "@/components/dashboard/operator/Reports/WeeklyBriefPage"
import { useOutletContext } from "react-router-dom"

import type { DashboardOutletContext } from "@/components/dashboard/operator/Dashboard"

export function WeeklyBriefRoute() {
  const context = useOutletContext<DashboardOutletContext | undefined>()
  const mode = context?.mode ?? "single"

  return <WeeklyBriefPage mode={mode} />
}
