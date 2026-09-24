import { useCallback } from "react"
import { useOutletContext } from "react-router-dom"

import type { DashboardOutletContext } from "@/components/dashboard/operator/Dashboard"
import { useDashboardUiStore } from "@/components/dashboard/operator/DashboardUiStoreProvider"
import { shouldGateFreeProductWrite } from "@/lib/operatorHome/freeProductWriteGate"

/**
 * Free accounts: re-open the activate dialog instead of starting product writes.
 * Returns true when the write was blocked.
 */
export function useGateFreeProductWrite(): (run: () => void) => boolean {
  const { subscriptionPlan } = useOutletContext<DashboardOutletContext>()
  const requestActivateDialog = useDashboardUiStore(
    (state) => state.requestActivateDialog
  )

  return useCallback(
    (run) => {
      if (shouldGateFreeProductWrite(subscriptionPlan)) {
        requestActivateDialog()
        return true
      }
      run()
      return false
    },
    [subscriptionPlan, requestActivateDialog]
  )
}
