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
  retryWeeklyBrief: OperatorReportsPageModule["retryWeeklyBrief"]
  ensureWeeklyBriefReady: OperatorReportsPageModule["ensureWeeklyBriefReady"]
  generateWeeklyBriefInPlace: OperatorReportsPageModule["generateWeeklyBriefInPlace"]
  markWeeklyBriefAsReviewed: OperatorReportsPageModule["markWeeklyBriefAsReviewed"]
  downloadWeeklyBriefPdf: OperatorReportsPageModule["downloadWeeklyBriefPdf"]
  retryCaptureLoad: OperatorReportsPageModule["retryCaptureLoad"]
  retryFeedbackLoad: OperatorReportsPageModule["retryFeedbackLoad"]
  retryOffersLoad: OperatorReportsPageModule["retryOffersLoad"]
  retryCampaignsLoad: OperatorReportsPageModule["retryCampaignsLoad"]
  openExportDialog: OperatorReportsPageModule["openExportDialog"]
  closeExportDialog: OperatorReportsPageModule["closeExportDialog"]
  requestExport: OperatorReportsPageModule["requestExport"]
  setCsvConsentChecked: OperatorReportsPageModule["setCsvConsentChecked"]
  confirmCsvExport: OperatorReportsPageModule["confirmCsvExport"]
  cancelCsvConsent: OperatorReportsPageModule["cancelCsvConsent"]
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
    retryWeeklyBrief: pageModule.retryWeeklyBrief,
    ensureWeeklyBriefReady: pageModule.ensureWeeklyBriefReady,
    generateWeeklyBriefInPlace: pageModule.generateWeeklyBriefInPlace,
    markWeeklyBriefAsReviewed: pageModule.markWeeklyBriefAsReviewed,
    downloadWeeklyBriefPdf: pageModule.downloadWeeklyBriefPdf,
    retryCaptureLoad: pageModule.retryCaptureLoad,
    retryFeedbackLoad: pageModule.retryFeedbackLoad,
    retryOffersLoad: pageModule.retryOffersLoad,
    retryCampaignsLoad: pageModule.retryCampaignsLoad,
    openExportDialog: pageModule.openExportDialog,
    closeExportDialog: pageModule.closeExportDialog,
    requestExport: pageModule.requestExport,
    setCsvConsentChecked: pageModule.setCsvConsentChecked,
    confirmCsvExport: pageModule.confirmCsvExport,
    cancelCsvConsent: pageModule.cancelCsvConsent,
  }
}
