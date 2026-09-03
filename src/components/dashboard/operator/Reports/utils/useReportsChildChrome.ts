import { useEffect } from "react"
import { useStore } from "zustand"

import { useDashboardUiStoreApi } from "@/components/dashboard/operator/DashboardUiStoreProvider"
import { useReportsPageModuleApi } from "@/components/dashboard/operator/Reports/utils/reportsPageModuleContext"
import { useReportsPageModule } from "@/components/dashboard/operator/Reports/utils/useReportsPageModule"
import type { HomePerformanceDateRange } from "@/lib/operatorHome/homePerformanceDateRange"
import type { ReportsSurface } from "@/types/operatorReports"

/** Shared date range + export chrome for Reports child routes. */
export function useReportsChildChrome(surface: ReportsSurface) {
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

  return {
    dateRange: reports.snapshot.dateRange,
    exportAllowed: reports.snapshot.exportAllowed,
    openExportDialog: reports.openExportDialog,
    commitRange: (range: HomePerformanceDateRange) => {
      setReportsDateRange(range)
      void reports.reloadForReportsDateRange()
    },
  }
}
