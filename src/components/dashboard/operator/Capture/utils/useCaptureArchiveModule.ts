import { useSyncExternalStore } from "react"

import { useCapturePageModuleApi } from "@/components/dashboard/operator/Capture/utils/capturePageModuleContext"
import type {
  CaptureArchiveModule,
  CaptureArchiveSnapshot,
  ConfirmRestoreResult,
} from "@/lib/operatorCapture/createCaptureArchiveModule"
import type { OperatorCapturePageModule } from "@/lib/operatorCapture/createOperatorCapturePageModule"

export type CaptureArchiveModuleApi = {
  snapshot: CaptureArchiveSnapshot
  enter: CaptureArchiveModule["enter"]
  reload: CaptureArchiveModule["reload"]
  setSearchQuery: CaptureArchiveModule["setSearchQuery"]
  setFilters: CaptureArchiveModule["setFilters"]
  setSort: CaptureArchiveModule["setSort"]
  setPage: CaptureArchiveModule["setPage"]
  goToPreviousPage: CaptureArchiveModule["goToPreviousPage"]
  goToNextPage: CaptureArchiveModule["goToNextPage"]
  clearSearchAndFilters: CaptureArchiveModule["clearSearchAndFilters"]
  requestRestore: CaptureArchiveModule["requestRestore"]
  cancelRestoreConfirm: CaptureArchiveModule["cancelRestoreConfirm"]
  requestDuplicateAsNew: CaptureArchiveModule["requestDuplicateAsNew"]
  clearCreatePrefill: CaptureArchiveModule["clearCreatePrefill"]
  /** Page-module orchestration: opens live Placement Detail for an archived row. */
  openArchivePlacementDetail: OperatorCapturePageModule["openArchivePlacementDetail"]
  /** Archive restore + live Placement Detail open on success. */
  confirmRestore: () => Promise<ConfirmRestoreResult>
  createDigitalGuestLink: OperatorCapturePageModule["createDigitalGuestLink"]
}

/** Subscribe to the Capture Archive module without notifying live Capture subscribers. */
export function useCaptureArchiveModule(): CaptureArchiveModuleApi {
  const pageModule = useCapturePageModuleApi()
  const archiveModule = pageModule.getArchiveModule()
  const snapshot = useSyncExternalStore(
    archiveModule.subscribe,
    archiveModule.getSnapshot,
    archiveModule.getSnapshot
  )

  return {
    snapshot,
    enter: archiveModule.enter,
    reload: archiveModule.reload,
    setSearchQuery: archiveModule.setSearchQuery,
    setFilters: archiveModule.setFilters,
    setSort: archiveModule.setSort,
    setPage: archiveModule.setPage,
    goToPreviousPage: archiveModule.goToPreviousPage,
    goToNextPage: archiveModule.goToNextPage,
    clearSearchAndFilters: archiveModule.clearSearchAndFilters,
    requestRestore: archiveModule.requestRestore,
    cancelRestoreConfirm: archiveModule.cancelRestoreConfirm,
    requestDuplicateAsNew: archiveModule.requestDuplicateAsNew,
    clearCreatePrefill: archiveModule.clearCreatePrefill,
    openArchivePlacementDetail: pageModule.openArchivePlacementDetail,
    confirmRestore: () => pageModule.confirmRestore(),
    createDigitalGuestLink: pageModule.createDigitalGuestLink,
  }
}
