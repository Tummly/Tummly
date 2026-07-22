import { useSyncExternalStore } from "react"

import { useGuestProfilePageModuleApi } from "@/components/dashboard/operator/GuestProfile/utils/guestProfilePageModuleContext"
import type {
  OperatorGuestProfilePageModule,
  OperatorGuestProfilePageSnapshot,
} from "@/lib/operatorGuestProfile/createOperatorGuestProfilePageModule"

export type OperatorGuestProfilePageModuleApi = {
  snapshot: OperatorGuestProfilePageSnapshot
  retryLoad: OperatorGuestProfilePageModule["retryLoad"]
}

export function useGuestProfilePageModule(): OperatorGuestProfilePageModuleApi {
  const pageModule = useGuestProfilePageModuleApi()
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
