import { useEffect, useRef, useSyncExternalStore } from "react"

import {
  getChecklistAcks,
  getFeedback,
  getFeedbackDetails,
  setChecklistAcks,
} from "@/api/dashboardApi"
import { connectFeedbackHomeHub } from "@/lib/operatorHome/connectFeedbackHomeHub"
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
  openFeedbackDetails: OperatorHomePageModule["openFeedbackDetails"]
  closeFeedbackDetails: OperatorHomePageModule["closeFeedbackDetails"]
  retryFeedbackDetails: OperatorHomePageModule["retryFeedbackDetails"]
}

function openSmartGuestLink(url: string): void {
  window.open(url, "_blank", "noopener,noreferrer")
}

export function useOperatorHomePageModule(): OperatorHomePageModuleApi {
  const moduleRef = useRef<OperatorHomePageModule | null>(null)

  if (moduleRef.current == null) {
    moduleRef.current = createOperatorHomePageModule({
      getFeedback,
      getFeedbackDetails,
      getChecklistAcks,
      setChecklistAcks,
      downloadQr: downloadSelectedLocationQr,
      openSmartGuestLink,
      connectRealtime: connectFeedbackHomeHub,
    })
  }

  const pageModule = moduleRef.current
  const snapshot = useSyncExternalStore(
    pageModule.subscribe,
    pageModule.getSnapshot,
    pageModule.getSnapshot
  )

  const connectRef = useRef(pageModule.connect)
  const disconnectRef = useRef(pageModule.disconnect)
  connectRef.current = pageModule.connect
  disconnectRef.current = pageModule.disconnect

  useEffect(() => {
    void connectRef.current()
    return () => {
      void disconnectRef.current()
    }
  }, [])

  return {
    snapshot,
    syncWorkspace: pageModule.syncWorkspace,
    retryLoad: pageModule.retryLoad,
    previewGuestForm: pageModule.previewGuestForm,
    downloadQr: pageModule.downloadQr,
    openFeedbackDetails: pageModule.openFeedbackDetails,
    closeFeedbackDetails: pageModule.closeFeedbackDetails,
    retryFeedbackDetails: pageModule.retryFeedbackDetails,
  }
}
