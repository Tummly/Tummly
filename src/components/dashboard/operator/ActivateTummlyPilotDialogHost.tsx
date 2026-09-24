import { useEffect, useState } from "react"
import { useNavigate } from "react-router-dom"
import { toast } from "sonner"

import activateTummlyPilotHero from "@/assets/operator-home/activate-tummly-pilot-hero.webp"
import { AccountWorkspaceConfirmDialog } from "@/components/dashboard/operator/AccountWorkspace/AccountWorkspaceConfirmDialog"
import { ActivateTummlyPilotDialog } from "@/components/dashboard/operator/ActivateTummlyPilotDialog"
import { useDashboardUiStore } from "@/components/dashboard/operator/DashboardUiStoreProvider"
import { operatorDashboardBillingCreditsManagePlanPath } from "@/lib/operatorBillingCredits/billingCreditsPresentation"
import {
  ACTIVATE_PILOT_FROM_FREE_CONFIRM,
  submitActivatePilotFromFree,
} from "@/lib/operatorHome/activatePilotFromFree"
import {
  markActivateTummlyPilotDialogDismissed,
  readActivateTummlyPilotDialogDismissed,
  shouldOpenActivateTummlyPilotDialog,
} from "@/lib/operatorHome/activateTummlyPilotDialogGate"
import type { OperatorDashboardMode } from "@/lib/operatorHome/operatorDashboardPaths"
import type { BillingCreditsAccess } from "@/lib/operatorHome/parseOperatorProfile"
import { preloadImage } from "@/lib/operatorHome/preloadImage"

type ActivateTummlyPilotDialogHostProps = {
  mode: OperatorDashboardMode
  status: string
  subscriptionPlan: string
  selectedLocationId: number | null
  billingCreditsAccess: BillingCreditsAccess
  reloadWorkspace: () => Promise<void>
  /** When set, suppress activate dialog (pending paid signup checkout). */
  pendingPaymentCheckoutUrl?: string | null
}

export function ActivateTummlyPilotDialogHost({
  mode,
  status,
  subscriptionPlan,
  selectedLocationId,
  billingCreditsAccess,
  reloadWorkspace,
  pendingPaymentCheckoutUrl = null,
}: ActivateTummlyPilotDialogHostProps) {
  const navigate = useNavigate()
  const [open, setOpen] = useState(false)
  const [heroReady, setHeroReady] = useState(false)
  const [confirmOpen, setConfirmOpen] = useState(false)
  const [confirmBusy, setConfirmBusy] = useState(false)
  const activateDialogNonce = useDashboardUiStore(
    (state) => state.activateDialogNonce
  )

  useEffect(() => {
    let cancelled = false
    void preloadImage(activateTummlyPilotHero).then(() => {
      if (!cancelled) {
        setHeroReady(true)
      }
    })
    return () => {
      cancelled = true
    }
  }, [])

  useEffect(() => {
    if (!heroReady) {
      return
    }
    if (
      pendingPaymentCheckoutUrl != null
      && pendingPaymentCheckoutUrl.length > 0
    ) {
      setOpen(false)
      return
    }
    const eligible = shouldOpenActivateTummlyPilotDialog({
      status,
      subscriptionPlan,
      isDismissed: readActivateTummlyPilotDialogDismissed(),
    })
    setOpen(eligible)
  }, [heroReady, status, subscriptionPlan, pendingPaymentCheckoutUrl])

  useEffect(() => {
    if (activateDialogNonce === 0) {
      return
    }
    if (
      pendingPaymentCheckoutUrl != null
      && pendingPaymentCheckoutUrl.length > 0
    ) {
      return
    }
    if (subscriptionPlan !== "Free" && subscriptionPlan !== "Pilot") {
      return
    }
    setOpen(true)
  }, [activateDialogNonce, subscriptionPlan, pendingPaymentCheckoutUrl])

  const dismiss = () => {
    markActivateTummlyPilotDialogDismissed()
    setOpen(false)
  }

  const goToManagePlan = () => {
    dismiss()
    if (selectedLocationId == null) {
      return
    }
    if (billingCreditsAccess === "none") {
      return
    }
    navigate(
      operatorDashboardBillingCreditsManagePlanPath(mode, selectedLocationId)
    )
  }

  const requestStartPilot = () => {
    if (subscriptionPlan === "Free") {
      setConfirmOpen(true)
      return
    }
    goToManagePlan()
  }

  const confirmStartPilot = async () => {
    setConfirmBusy(true)
    try {
      await submitActivatePilotFromFree()
      markActivateTummlyPilotDialogDismissed()
      setConfirmOpen(false)
      setOpen(false)
      await reloadWorkspace()
    } catch {
      toast.error("Could not start Pilot. Please try again.")
    } finally {
      setConfirmBusy(false)
    }
  }

  return (
    <>
      <ActivateTummlyPilotDialog
        open={open && !confirmOpen}
        subscriptionPlan={subscriptionPlan}
        onOpenChange={(next) => {
          if (!next) {
            dismiss()
            return
          }
          setOpen(true)
        }}
        onStartPilot={requestStartPilot}
        onViewPlans={goToManagePlan}
      />
      <AccountWorkspaceConfirmDialog
        open={confirmOpen}
        title={ACTIVATE_PILOT_FROM_FREE_CONFIRM.title}
        body={ACTIVATE_PILOT_FROM_FREE_CONFIRM.body}
        primaryLabel={ACTIVATE_PILOT_FROM_FREE_CONFIRM.primaryLabel}
        busy={confirmBusy}
        onOpenChange={(next) => {
          if (!next && !confirmBusy) {
            setConfirmOpen(false)
          }
        }}
        onPrimary={() => {
          void confirmStartPilot()
        }}
        onCancel={() => {
          if (!confirmBusy) {
            setConfirmOpen(false)
          }
        }}
      />
    </>
  )
}
