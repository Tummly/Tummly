import { createContext, useContext } from "react"

import type { OperatorGuestsPageModule } from "@/lib/operatorGuests/createOperatorGuestsPageModule"

export const guestsPageModuleContext =
  createContext<OperatorGuestsPageModule | null>(null)

export function useGuestsPageModuleApi(): OperatorGuestsPageModule {
  const pageModule = useContext(guestsPageModuleContext)
  if (pageModule == null) {
    throw new Error(
      "useGuestsPageModule must be used within GuestsPageModuleProvider"
    )
  }
  return pageModule
}
