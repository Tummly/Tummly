import { useContext } from "react"
import { createContext } from "react"

import type { OperatorLocationDetailPageModule } from "@/lib/operatorLocations/createOperatorLocationDetailPageModule"

export const locationDetailPageModuleContext =
  createContext<OperatorLocationDetailPageModule | null>(null)

export function useLocationDetailPageModuleApi() {
  const value = useContext(locationDetailPageModuleContext)
  if (value == null) {
    throw new Error(
      "useLocationDetailPageModuleApi requires LocationDetailPageModuleProvider."
    )
  }
  return value
}
