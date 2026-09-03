import { useOutletContext } from "react-router-dom"

import { FeedbackReportPage } from "@/components/dashboard/operator/Reports/FeedbackReportPage"
import type { DashboardOutletContext } from "@/components/dashboard/operator/Dashboard"

export function FeedbackReportRoute() {
  const context = useOutletContext<DashboardOutletContext | undefined>()
  const mode = context?.mode ?? "single"

  return <FeedbackReportPage mode={mode} />
}
