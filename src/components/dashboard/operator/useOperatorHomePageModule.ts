import { useRef, useSyncExternalStore } from "react"

import {
  getChecklistAcks,
  getFeedback,
  setChecklistAcks,
} from "@/api/dashboardApi"
import {
  createOperatorHomePageModule,
  type OperatorHomePageModule,
  type OperatorHomePageSnapshot,
} from "@/lib/operatorHome/createOperatorHomePageModule"
import { downloadSelectedLocationQr } from "@/lib/operatorHome/homeActions"

export type OperatorHomePageModuleApi = {
  snapshot: OperatorHomePageSnapshot
  syncWorkspace: OperatorHomePageModule["syncWorkspace"]
  retryLoad: OperatorHomePageModule["retryLoad"]
  previewGuestForm: OperatorHomePageModule["previewGuestForm"]
  downloadQr: OperatorHomePageModule["downloadQr"]
}

function openSmartGuestLink(url: string): void {
  window.open(url, "_blank", "noopener,noreferrer")
}

export function useOperatorHomePageModule(): OperatorHomePageModuleApi {
  const moduleRef = useRef<OperatorHomePageModule | null>(null)

  if (moduleRef.current == null) {
    moduleRef.current = createOperatorHomePageModule({
      getFeedback,
      getChecklistAcks,
      setChecklistAcks,
      downloadQr: downloadSelectedLocationQr,
      openSmartGuestLink,
    })
  }

  const pageModule = moduleRef.current
  const snapshot = useSyncExternalStore(
    pageModule.subscribe,
    pageModule.getSnapshot,
    pageModule.getSnapshot
  )

  return {
    snapshot,
    syncWorkspace: pageModule.syncWorkspace,
    retryLoad: pageModule.retryLoad,
    previewGuestForm: pageModule.previewGuestForm,
    downloadQr: pageModule.downloadQr,
  }
}
