import { createContext, useContext } from "react"

import type { OperatorCampaignsPageModule } from "@/lib/operatorCampaigns/createOperatorCampaignsPageModule"

export const campaignsPageModuleContext =
  createContext<OperatorCampaignsPageModule | null>(null)

export function useCampaignsPageModuleApi(): OperatorCampaignsPageModule {
  const pageModule = useContext(campaignsPageModuleContext)
  if (pageModule == null) {
    throw new Error(
      "useCampaignsPageModule must be used within CampaignsPageModuleProvider"
    )
  }
  return pageModule
}
