import { createElement, useEffect, useState, type ReactNode } from "react"
import { useOutletContext } from "react-router-dom"

import {
  downloadWeeklyBriefPdf,
  generateWeeklyBrief,
  getReportsCapture,
  getReportsFeedback,
  getReportsOffers,
  getReportsCampaigns,
  getReportsOverview,
  getWeeklyBrief,
  markWeeklyBriefReviewed,
  triggerBrowserDownload,
} from "@/api/dashboardApi"
import type { DashboardOutletContext } from "@/components/dashboard/operator/Dashboard"
import { useDashboardUiStoreApi } from "@/components/dashboard/operator/DashboardUiStoreProvider"
import { ReportsExportDialog } from "@/components/dashboard/operator/Reports/ReportsExportDialog"
import { reportsPageModuleContext } from "@/components/dashboard/operator/Reports/utils/reportsPageModuleContext"
import { useReportsPageModule } from "@/components/dashboard/operator/Reports/utils/useReportsPageModule"
import { createOperatorReportsPageModule } from "@/lib/operatorReports/createOperatorReportsPageModule"

function ReportsExportDialogHost() {
  const reports = useReportsPageModule()
  return (
    <ReportsExportDialog
      open={reports.snapshot.exportDialogOpen}
      onOpenChange={(open) => {
        if (open) {
          reports.openExportDialog()
        } else {
          reports.closeExportDialog()
        }
      }}
      locationName={reports.snapshot.selectedLocationName ?? "Location"}
      dateRangeLabel={reports.snapshot.dateRangeLabel}
    />
  )
}

export function ReportsPageModuleProvider({
  children,
}: {
  children: ReactNode
}) {
  const dashboardUiStore = useDashboardUiStoreApi()
  const dashboardContext = useOutletContext<DashboardOutletContext | undefined>()
  const [pageModule] = useState(() =>
    createOperatorReportsPageModule({
      getOverview: getReportsOverview,
      getCapture: getReportsCapture,
      getFeedback: getReportsFeedback,
      getOffers: getReportsOffers,
      getCampaigns: getReportsCampaigns,
      getWeeklyBrief,
      generateWeeklyBrief,
      markWeeklyBriefReviewed,
      downloadWeeklyBriefPdf,
      triggerBrowserDownload,
      getReportsDateRange: () => dashboardUiStore.getState().reportsDateRange,
    })
  )

  useEffect(() => {
    if (dashboardContext == null) {
      return
    }
    void pageModule.syncWorkspace({
      selectedLocationId: dashboardContext.selectedLocationId,
      locations: dashboardContext.locations,
      billingStatus: dashboardContext.billingStatus,
      chargebackRestricted: dashboardContext.chargebackRestricted,
    })
  }, [
    dashboardContext,
    dashboardContext?.selectedLocationId,
    dashboardContext?.billingStatus,
    dashboardContext?.chargebackRestricted,
    dashboardContext?.locations,
    pageModule,
  ])

  return createElement(
    reportsPageModuleContext.Provider,
    { value: pageModule },
    children,
    createElement(ReportsExportDialogHost)
  )
}
