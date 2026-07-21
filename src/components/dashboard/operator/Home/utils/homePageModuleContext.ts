import { createContext, useContext } from "react"

import type { OperatorHomePageModule } from "@/lib/operatorHome/createOperatorHomePageModule"

export const homePageModuleContext =
  createContext<OperatorHomePageModule | null>(null)

export function useHomePageModuleApi(): OperatorHomePageModule {
  const pageModule = useContext(homePageModuleContext)
  if (pageModule == null) {
    throw new Error(
      "useHomePageModule must be used within HomePageModuleProvider"
    )
  }
  return pageModule
}
