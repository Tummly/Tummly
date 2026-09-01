import { createContext, useContext } from "react"

import type { OperatorPrivacyConsentPageModule } from "@/lib/operatorPrivacyConsent/createOperatorPrivacyConsentPageModule"

export const privacyConsentPageModuleContext =
  createContext<OperatorPrivacyConsentPageModule | null>(null)

export function usePrivacyConsentPageModuleApi(): OperatorPrivacyConsentPageModule {
  const value = useContext(privacyConsentPageModuleContext)
  if (value == null) {
    throw new Error("Privacy & consent page module is missing.")
  }
  return value
}
