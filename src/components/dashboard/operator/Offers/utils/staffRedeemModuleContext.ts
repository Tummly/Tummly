import { createContext, useContext } from "react"

import type { StaffRedeemModule } from "@/lib/operatorOffers/createStaffRedeemModule"

/**
 * Details entry hook: call `useStaffRedeemModuleApi().open(locationId)`
 * when Offer Details mounts under the same Offers provider tree.
 */
export const staffRedeemModuleContext =
  createContext<StaffRedeemModule | null>(null)

export function useStaffRedeemModuleApi(): StaffRedeemModule {
  const staffRedeem = useContext(staffRedeemModuleContext)
  if (staffRedeem == null) {
    throw new Error(
      "useStaffRedeemModuleApi must be used within OffersPageModuleProvider"
    )
  }
  return staffRedeem
}
