import { createContext, useContext } from "react"

import type { OperatorGuestProfilePageModule } from "@/lib/operatorGuestProfile/createOperatorGuestProfilePageModule"

export const guestProfilePageModuleContext =
  createContext<OperatorGuestProfilePageModule | null>(null)

export function useGuestProfilePageModuleApi(): OperatorGuestProfilePageModule {
  const pageModule = useContext(guestProfilePageModuleContext)
  if (pageModule == null) {
    throw new Error(
      "useGuestProfilePageModule must be used within GuestProfilePageModuleProvider"
    )
  }
  return pageModule
}
