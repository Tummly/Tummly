import { createContext, useContext } from "react"

import type { GuestActivityTabModule } from "@/lib/operatorGuestProfile/createGuestActivityTabModule"

export const guestActivityTabModuleContext =
  createContext<GuestActivityTabModule | null>(null)

export function useGuestActivityTabModuleApi(): GuestActivityTabModule {
  const value = useContext(guestActivityTabModuleContext)
  if (value == null) {
    throw new Error(
      "useGuestActivityTabModuleApi requires GuestProfilePageModuleProvider"
    )
  }
  return value
}

export const GuestActivityTabModuleContextProvider =
  guestActivityTabModuleContext.Provider
