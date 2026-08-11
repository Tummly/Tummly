import { useSyncExternalStore } from "react"

import { useStaffRedeemModuleApi } from "@/components/dashboard/operator/Offers/utils/staffRedeemModuleContext"
import type {
  StaffRedeemModule,
  StaffRedeemSnapshot,
} from "@/lib/operatorOffers/createStaffRedeemModule"

export type StaffRedeemModuleApi = StaffRedeemModule & {
  snapshot: StaffRedeemSnapshot
}

/** Main + Details share this hook once Details mounts under OffersPageModuleProvider. */
export function useStaffRedeemModule(): StaffRedeemModuleApi {
  const staffRedeem = useStaffRedeemModuleApi()
  const snapshot = useSyncExternalStore(
    staffRedeem.subscribe,
    staffRedeem.getSnapshot,
    staffRedeem.getSnapshot
  )

  return {
    snapshot,
    getSnapshot: staffRedeem.getSnapshot,
    subscribe: staffRedeem.subscribe,
    open: staffRedeem.open,
    close: staffRedeem.close,
    setCode: staffRedeem.setCode,
    checkOffer: staffRedeem.checkOffer,
    cancelConfirm: staffRedeem.cancelConfirm,
    markAsRedeemed: staffRedeem.markAsRedeemed,
    applyScannedCode: staffRedeem.applyScannedCode,
  }
}
