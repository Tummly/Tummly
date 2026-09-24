import { useEffect, useState } from "react"
import { toast } from "sonner"

import { AccountWorkspaceConfirmDialog } from "@/components/dashboard/operator/AccountWorkspace/AccountWorkspaceConfirmDialog"
import axiosInstance from "@/api/axiosInstance"
import { PENDING_PAYMENT_INTERSTITIAL_DISMISS_KEY } from "@/lib/operatorHome/pendingPaymentInterstitialDismiss"

export const PENDING_PAYMENT_INTERSTITIAL_COPY = {
  title: "Complete your plan payment",
  body: "You chose a paid plan during signup. Complete payment to unlock it, or continue on Free for now.",
  completePayment: "Complete payment",
  continueOnFree: "Continue on Free",
} as const

type PendingPaymentInterstitialHostProps = {
  status: string
  pendingPaymentCheckoutUrl: string | null
  reloadWorkspace: () => Promise<void>
}

export function PendingPaymentInterstitialHost({
  status,
  pendingPaymentCheckoutUrl,
  reloadWorkspace,
}: PendingPaymentInterstitialHostProps) {
  const [open, setOpen] = useState(false)
  const [busy, setBusy] = useState(false)

  useEffect(() => {
    if (status !== "loaded") {
      return
    }
    if (
      pendingPaymentCheckoutUrl == null
      || pendingPaymentCheckoutUrl.length === 0
    ) {
      setOpen(false)
      return
    }
    if (
      sessionStorage.getItem(PENDING_PAYMENT_INTERSTITIAL_DISMISS_KEY) === "1"
    ) {
      setOpen(false)
      return
    }
    setOpen(true)
  }, [status, pendingPaymentCheckoutUrl])

  const continueOnFree = async () => {
    setBusy(true)
    try {
      await axiosInstance.post(
        "/billing-credits/pending-payment/continue-on-free"
      )
      sessionStorage.setItem(PENDING_PAYMENT_INTERSTITIAL_DISMISS_KEY, "1")
      setOpen(false)
      await reloadWorkspace()
    } catch {
      toast.error("Could not continue on Free. Please try again.")
    } finally {
      setBusy(false)
    }
  }

  return (
    <AccountWorkspaceConfirmDialog
      open={open}
      title={PENDING_PAYMENT_INTERSTITIAL_COPY.title}
      body={PENDING_PAYMENT_INTERSTITIAL_COPY.body}
      primaryLabel={PENDING_PAYMENT_INTERSTITIAL_COPY.completePayment}
      cancelLabel={PENDING_PAYMENT_INTERSTITIAL_COPY.continueOnFree}
      busy={busy}
      onOpenChange={(next) => {
        // Backdrop close = remind later (do not abandon payment or dismiss).
        if (!next && !busy) {
          setOpen(false)
        }
      }}
      onPrimary={() => {
        if (pendingPaymentCheckoutUrl == null) {
          return
        }
        window.location.assign(pendingPaymentCheckoutUrl)
      }}
      onCancel={() => {
        void continueOnFree()
      }}
    />
  )
}
