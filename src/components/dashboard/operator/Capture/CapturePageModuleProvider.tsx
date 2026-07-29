import { createElement, useState, type ReactNode } from "react"
import { toast } from "sonner"

import {
  getCapturePerformance,
  getCapturePlacements,
  pauseCapturePlacement,
  resumeCapturePlacement,
} from "@/api/dashboardApi"
import { capturePageModuleContext } from "@/components/dashboard/operator/Capture/utils/capturePageModuleContext"
import { useDashboardUiStoreApi } from "@/components/dashboard/operator/DashboardUiStoreProvider"
import { createOperatorCapturePageModule } from "@/lib/operatorCapture/createOperatorCapturePageModule"

async function copyText(
  text: string
): Promise<{ ok: true } | { ok: false; error: string }> {
  try {
    await navigator.clipboard.writeText(text)
    return { ok: true }
  } catch {
    return {
      ok: false,
      error: "Could not copy link. Please try again.",
    }
  }
}

export function CapturePageModuleProvider({
  children,
}: {
  children: ReactNode
}) {
  const dashboardUiStore = useDashboardUiStoreApi()
  const [pageModule] = useState(() =>
    createOperatorCapturePageModule({
      getCapturePerformance,
      getCapturePlacements,
      pauseCapturePlacement,
      resumeCapturePlacement,
      copyText,
      getCapturePerformanceDateRange: () =>
        dashboardUiStore.getState().capturePerformanceDateRange,
      onPerformanceLoadError: (message) => {
        toast.error(message)
      },
      onPlacementsLoadError: (message) => {
        toast.error(message)
      },
      onPlacementActionError: (message) => {
        toast.error(message)
      },
      onCopyPlacementLinkError: (message) => {
        toast.error(message)
      },
    })
  )

  return createElement(
    capturePageModuleContext.Provider,
    { value: pageModule },
    children
  )
}
