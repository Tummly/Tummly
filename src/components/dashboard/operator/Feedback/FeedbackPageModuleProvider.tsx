import { createElement, useState, type ReactNode } from "react"

import { getFeedbackSummary } from "@/api/dashboardApi"
import { feedbackPageModuleContext } from "@/components/dashboard/operator/Feedback/utils/feedbackPageModuleContext"
import { useDashboardUiStoreApi } from "@/components/dashboard/operator/DashboardUiStoreProvider"
import { createOperatorFeedbackPageModule } from "@/lib/operatorFeedback/createOperatorFeedbackPageModule"

export function FeedbackPageModuleProvider({
  children,
}: {
  children: ReactNode
}) {
  const dashboardUiStore = useDashboardUiStoreApi()
  const [pageModule] = useState(() =>
    createOperatorFeedbackPageModule({
      getFeedbackSummary: async ({ locationId, from, to }) =>
        getFeedbackSummary(locationId, from, to),
      getFeedbackPageDateRange: () =>
        dashboardUiStore.getState().feedbackPageDateRange,
    })
  )

  return createElement(
    feedbackPageModuleContext.Provider,
    { value: pageModule },
    children
  )
}
