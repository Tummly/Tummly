import { useSyncExternalStore } from "react"

import { useCampaignsPageModuleApi } from "@/components/dashboard/operator/Campaigns/utils/campaignsPageModuleContext"
import type {
  OperatorCampaignsPageModule,
  OperatorCampaignsPageSnapshot,
} from "@/lib/operatorCampaigns/createOperatorCampaignsPageModule"

export type OperatorCampaignsPageModuleApi = {
  snapshot: OperatorCampaignsPageSnapshot
  retryLoad: OperatorCampaignsPageModule["retryLoad"]
}

export function useCampaignsPageModule(): OperatorCampaignsPageModuleApi {
  const pageModule = useCampaignsPageModuleApi()
  const snapshot = useSyncExternalStore(
    pageModule.subscribe,
    pageModule.getSnapshot,
    pageModule.getSnapshot
  )

  return {
    snapshot,
    retryLoad: pageModule.retryLoad,
  }
}

export type { OperatorCampaignsPageSnapshot }
