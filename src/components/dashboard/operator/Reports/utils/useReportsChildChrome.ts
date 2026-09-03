import { useEffect } from "react"
import { useNavigate } from "react-router-dom"
import { useStore } from "zustand"

import { useDashboardUiStoreApi } from "@/components/dashboard/operator/DashboardUiStoreProvider"
import { useReportsPageModuleApi } from "@/components/dashboard/operator/Reports/utils/reportsPageModuleContext"
import { useReportsPageModule } from "@/components/dashboard/operator/Reports/utils/useReportsPageModule"
import type { HomePerformanceDateRange } from "@/lib/operatorHome/homePerformanceDateRange"
import { operatorDashboardWeeklyBriefPath } from "@/lib/operatorHome/operatorDashboardPaths"
import type { DashboardProps } from "@/components/dashboard/operator/Dashboard"
import type { ReportsSurface } from "@/types/operatorReports"

/** Shared date range + export + Generate brief chrome for Reports child routes. */
export function useReportsChildChrome(
  surface: ReportsSurface,
  mode: DashboardProps["mode"] = "single"
) {
  const navigate = useNavigate()
  const pageModule = useReportsPageModuleApi()
  const reports = useReportsPageModule()
  const dashboardUiStore = useDashboardUiStoreApi()
  const setReportsDateRange = useStore(
    dashboardUiStore,
    (state) => state.setReportsDateRange
  )

  useEffect(() => {
    pageModule.setActiveSurface(surface)
  }, [pageModule, surface])

  const locationId = reports.snapshot.selectedLocationId ?? 1

  return {
    dateRange: reports.snapshot.dateRange,
    exportAllowed: reports.snapshot.exportAllowed,
    generateBusy: reports.snapshot.weeklyBrief.generateBusy,
    openExportDialog: reports.openExportDialog,
    commitRange: (range: HomePerformanceDateRange) => {
      setReportsDateRange(range)
      void reports.reloadForReportsDateRange()
    },
    onGenerateBrief: () => {
      void (async () => {
        const ok = await reports.ensureWeeklyBriefReady()
        if (ok) {
          navigate(operatorDashboardWeeklyBriefPath(mode, locationId))
        }
      })()
    },
  }
}
