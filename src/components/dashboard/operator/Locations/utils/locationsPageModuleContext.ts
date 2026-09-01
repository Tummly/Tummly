import { createContext, useContext } from "react"

import type { OperatorLocationsPageModule } from "@/lib/operatorLocations/createOperatorLocationsPageModule"

export const locationsPageModuleContext =
  createContext<OperatorLocationsPageModule | null>(null)

export function useLocationsPageModuleApi(): OperatorLocationsPageModule {
  const value = useContext(locationsPageModuleContext)
  if (value == null) {
    throw new Error("Locations page module is missing.")
  }
  return value
}
