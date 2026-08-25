import { createContext, useContext } from "react"

import type { OperatorTeamPermissionsPageModule } from "@/lib/operatorTeamPermissions/createOperatorTeamPermissionsPageModule"

export const teamPermissionsPageModuleContext =
  createContext<OperatorTeamPermissionsPageModule | null>(null)

export function useTeamPermissionsPageModuleApi(): OperatorTeamPermissionsPageModule {
  const value = useContext(teamPermissionsPageModuleContext)
  if (value == null) {
    throw new Error("Team permissions page module is missing.")
  }
  return value
}
