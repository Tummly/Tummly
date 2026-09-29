import { useEffect } from "react"
import { useNavigate } from "react-router-dom"
import { toast } from "sonner"
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

  return {
    dateRange: reports.snapshot.dateRange,
    exportAllowed: reports.snapshot.exportAllowed,
    generateBusy: reports.snapshot.weeklyBrief.generateBusy,
    /** Surface-scoped export (RPT-006); guest-data kinds still open consent. */
    onExport: () => {
      void (async () => {
        const ok = await reports.exportActiveReport()
        if (ok) {
          toast.success("Your file has been downloaded")
        }
      })()
    },
    commitRange: (range: HomePerformanceDateRange) => {
      setReportsDateRange(range)
      void reports.reloadForReportsDateRange()
    },
    onGenerateBrief: () => {
      void (async () => {
        const locationId = reports.snapshot.selectedLocationId
        if (locationId == null) {
          return
        }
        const ok = await reports.ensureWeeklyBriefReady()
        if (ok) {
          navigate(operatorDashboardWeeklyBriefPath(mode, locationId))
        }
      })()
    },
  }
}
