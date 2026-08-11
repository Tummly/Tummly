import { useSyncExternalStore } from "react"

import { useOffersPageModuleApi } from "@/components/dashboard/operator/Offers/utils/offersPageModuleContext"
import type {
  OperatorOffersPageModule,
  OperatorOffersPageSnapshot,
} from "@/lib/operatorOffers/createOperatorOffersPageModule"

export type OperatorOffersPageModuleApi = {
  snapshot: OperatorOffersPageSnapshot
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
  }
}

export type { OperatorOffersPageModule, OperatorOffersPageSnapshot }
