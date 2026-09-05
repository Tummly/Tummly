import { createContext, useContext } from "react"

import type { OperatorReportsPageModule } from "@/lib/operatorReports/createOperatorReportsPageModule"

export const reportsPageModuleContext =
  createContext<OperatorReportsPageModule | null>(null)

export function useReportsPageModuleApi(): OperatorReportsPageModule {
  const pageModule = useContext(reportsPageModuleContext)
  if (pageModule == null) {
    throw new Error(
      "useReportsPageModuleApi must be used within ReportsPageModuleProvider"
    )
  }
  return pageModule
}
