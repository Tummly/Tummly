import { createContext, useContext } from "react"

import type { OperatorMultiCapturePageModule } from "@/lib/operatorMultiCapture/createOperatorMultiCapturePageModule"

export const multiCapturePageModuleContext =
  createContext<OperatorMultiCapturePageModule | null>(null)

export function useMultiCapturePageModuleApi(): OperatorMultiCapturePageModule {
  const pageModule = useContext(multiCapturePageModuleContext)
  if (pageModule == null) {
    throw new Error(
      "useMultiCapturePageModule must be used within MultiCapturePageModuleProvider"
    )
  }
  return pageModule
}
