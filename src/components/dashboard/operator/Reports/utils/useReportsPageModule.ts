import { useSyncExternalStore } from "react"

import { useReportsPageModuleApi } from "@/components/dashboard/operator/Reports/utils/reportsPageModuleContext"
import type {
  OperatorReportsPageModule,
  OperatorReportsPageSnapshot,
} from "@/lib/operatorReports/createOperatorReportsPageModule"

export type OperatorReportsPageModuleApi = {
  snapshot: OperatorReportsPageSnapshot
  syncWorkspace: OperatorReportsPageModule["syncWorkspace"]
  setActiveSurface: OperatorReportsPageModule["setActiveSurface"]
  reloadForReportsDateRange: OperatorReportsPageModule["reloadForReportsDateRange"]
  retryHubLoad: OperatorReportsPageModule["retryHubLoad"]
  openExportDialog: OperatorReportsPageModule["openExportDialog"]
  closeExportDialog: OperatorReportsPageModule["closeExportDialog"]
}

export function useReportsPageModule(): OperatorReportsPageModuleApi {
  const pageModule = useReportsPageModuleApi()
  const snapshot = useSyncExternalStore(
    pageModule.subscribe,
    pageModule.getSnapshot,
    pageModule.getSnapshot
  )

  return {
    snapshot,
    syncWorkspace: pageModule.syncWorkspace,
    setActiveSurface: pageModule.setActiveSurface,
    reloadForReportsDateRange: pageModule.reloadForReportsDateRange,
    retryHubLoad: pageModule.retryHubLoad,
    openExportDialog: pageModule.openExportDialog,
    closeExportDialog: pageModule.closeExportDialog,
  }
}
