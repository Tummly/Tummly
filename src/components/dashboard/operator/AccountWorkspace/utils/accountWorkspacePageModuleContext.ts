import { createContext, useContext } from "react"

import type { OperatorAccountWorkspacePageModule } from "@/lib/operatorAccountWorkspace/createOperatorAccountWorkspacePageModule"

const accountWorkspacePageModuleContext =
  createContext<OperatorAccountWorkspacePageModule | null>(null)

export function useAccountWorkspacePageModuleApi(): OperatorAccountWorkspacePageModule {
  const value = useContext(accountWorkspacePageModuleContext)
  if (value == null) {
    throw new Error(
      "useAccountWorkspacePageModuleApi requires AccountWorkspacePageModuleProvider"
    )
  }
  return value
}

export { accountWorkspacePageModuleContext }
