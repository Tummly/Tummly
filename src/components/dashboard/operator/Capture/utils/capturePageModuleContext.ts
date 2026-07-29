import { createContext, useContext } from "react"

import type { OperatorCapturePageModule } from "@/lib/operatorCapture/createOperatorCapturePageModule"

export const capturePageModuleContext =
  createContext<OperatorCapturePageModule | null>(null)

export function useCapturePageModuleApi(): OperatorCapturePageModule {
  const pageModule = useContext(capturePageModuleContext)
  if (pageModule == null) {
    throw new Error(
      "useCapturePageModule must be used within CapturePageModuleProvider"
    )
  }
  return pageModule
}
