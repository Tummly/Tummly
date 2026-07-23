import { createContext, useContext } from "react"

import type { OperatorGuestEditPageModule } from "@/lib/operatorGuestProfile/createOperatorGuestEditPageModule"

export const guestEditPageModuleContext =
  createContext<OperatorGuestEditPageModule | null>(null)

export function useGuestEditPageModuleApi(): OperatorGuestEditPageModule {
  const value = useContext(guestEditPageModuleContext)
  if (value == null) {
    throw new Error(
      "useGuestEditPageModuleApi must be used within GuestEditPageModuleProvider"
    )
  }
  return value
}
