import { createContext, useContext } from "react"

import type { GuestFeedbacksTabModule } from "@/lib/operatorGuestProfile/createGuestFeedbacksTabModule"

export const guestFeedbacksTabModuleContext =
  createContext<GuestFeedbacksTabModule | null>(null)

export function useGuestFeedbacksTabModuleApi(): GuestFeedbacksTabModule {
  const value = useContext(guestFeedbacksTabModuleContext)
  if (value == null) {
    throw new Error(
      "useGuestFeedbacksTabModuleApi requires GuestProfilePageModuleProvider"
    )
  }
  return value
}

export const GuestFeedbacksTabModuleContextProvider =
  guestFeedbacksTabModuleContext.Provider
