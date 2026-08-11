import { useSyncExternalStore } from "react"

import { useOffersPageModuleApi } from "@/components/dashboard/operator/Offers/utils/offersPageModuleContext"
import type {
  OperatorOffersPageModule,
  OperatorOffersPageSnapshot,
} from "@/lib/operatorOffers/createOperatorOffersPageModule"
import type { HomePerformanceDateRange } from "@/lib/operatorHome/homePerformanceDateRange"

export type OperatorOffersPageModuleApi = {
  snapshot: OperatorOffersPageSnapshot
  pageModule: OperatorOffersPageModule
  setPerformanceDateRange: (range: HomePerformanceDateRange) => void
}

export function useOffersPageModule(): OperatorOffersPageModuleApi {
  const pageModule = useOffersPageModuleApi()
  const snapshot = useSyncExternalStore(
    pageModule.subscribe,
    pageModule.getSnapshot,
    pageModule.getSnapshot
  )

  return {
    snapshot,
    pageModule,
    setPerformanceDateRange: pageModule.setPerformanceDateRange,
  }
}

export type { OperatorOffersPageModule, OperatorOffersPageSnapshot }
