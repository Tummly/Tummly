import { useSyncExternalStore } from "react"

import { useOffersRedemptionLogPageModuleApi } from "@/components/dashboard/operator/Offers/utils/offersRedemptionLogPageModuleContext"
import type {
  OperatorOffersRedemptionLogModule,
  OperatorOffersRedemptionLogSnapshot,
} from "@/lib/operatorOffers/createOperatorOffersRedemptionLogModule"

export type OperatorOffersRedemptionLogPageModuleApi = {
  snapshot: OperatorOffersRedemptionLogSnapshot
  retryLoad: OperatorOffersRedemptionLogModule["retryLoad"]
}

export function useOffersRedemptionLogPageModule(): OperatorOffersRedemptionLogPageModuleApi {
  const pageModule = useOffersRedemptionLogPageModuleApi()
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
