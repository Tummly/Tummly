import { useEffect, useState } from "react"
import { useNavigate } from "react-router-dom"

import activateTummlyPilotHero from "@/assets/operator-home/activate-tummly-pilot-hero.webp"
import { ActivateTummlyPilotDialog } from "@/components/dashboard/operator/ActivateTummlyPilotDialog"
import { operatorDashboardBillingCreditsManagePlanPath } from "@/lib/operatorBillingCredits/billingCreditsPresentation"
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
}

export function ActivateTummlyPilotDialogHost({
  mode,
  status,
  subscriptionPlan,
  selectedLocationId,
  billingCreditsAccess,
}: ActivateTummlyPilotDialogHostProps) {
  const navigate = useNavigate()
  const [open, setOpen] = useState(false)
  const [heroReady, setHeroReady] = useState(false)

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
    const eligible = shouldOpenActivateTummlyPilotDialog({
      status,
      subscriptionPlan,
      isDismissed: readActivateTummlyPilotDialogDismissed(),
    })
    setOpen(eligible)
  }, [heroReady, status, subscriptionPlan])

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

  return (
    <ActivateTummlyPilotDialog
      open={open}
      onOpenChange={(next) => {
        if (!next) {
          dismiss()
          return
        }
        setOpen(true)
      }}
      onStartPilot={goToManagePlan}
      onViewPlans={goToManagePlan}
    />
  )
}
