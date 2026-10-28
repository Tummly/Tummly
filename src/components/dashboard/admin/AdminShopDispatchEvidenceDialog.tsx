import { useState } from "react"
import axios from "axios"
import { toast } from "sonner"

import {
  type AdminShopOrderListItem,
  recordShopOrderDispatchEvidence,
} from "@/api/adminShopOrdersApi"
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import {
  adminShopDispatchedAtToIso,
  canSubmitAdminShopDispatchEvidence,
} from "@/lib/adminShopOrderFulfilment"

type AdminShopDispatchEvidenceDialogProps = {
  order: AdminShopOrderListItem | null
  onClose: () => void
  onCompleted: (updated: AdminShopOrderListItem) => void
}

function dispatchEvidenceErrorMessage(error: unknown): string {
  if (axios.isAxiosError(error)) {
    const message = (error.response?.data as { message?: string } | undefined)
      ?.message
    if (message) return message
  }
  return "Could not record dispatch evidence."
}

/**
 * Backfills carrier evidence for a legacy dispatch. Admin enters the carrier
 * or supplier name, the reference and the actual dispatch time. The order
 * keeps its status and tracking URL.
 */
export function AdminShopDispatchEvidenceDialog({
  order,
  onClose,
  onCompleted,
}: AdminShopDispatchEvidenceDialogProps) {
  const [carrierName, setCarrierName] = useState("")
  const [carrierReference, setCarrierReference] = useState("")
  const [dispatchedAt, setDispatchedAt] = useState("")
  const [busy, setBusy] = useState(false)

  const canSubmit = canSubmitAdminShopDispatchEvidence({
    carrierName,
    carrierReference,
    dispatchedAt,
  })

  const resetFields = () => {
    setCarrierName("")
    setCarrierReference("")
    setDispatchedAt("")
  }

  const handleOpenChange = (nextOpen: boolean) => {
    if (busy || nextOpen) return
    resetFields()
    onClose()
  }

  const handleConfirm = async () => {
    const dispatchedAtUtc = adminShopDispatchedAtToIso(dispatchedAt)
    if (!order || !canSubmit || dispatchedAtUtc === null) return
    setBusy(true)
    try {
      const updated = await recordShopOrderDispatchEvidence(order.id, {
        carrierName,
        carrierReference,
        dispatchedAtUtc,
      })
      resetFields()
      toast.success("Dispatch evidence recorded")
      onCompleted(updated)
    } catch (error) {
      toast.error(dispatchEvidenceErrorMessage(error))
    } finally {
      setBusy(false)
    }
  }

  return (
    <AlertDialog open={order !== null} onOpenChange={handleOpenChange}>
      <AlertDialogContent className="rounded-2xl">
        <AlertDialogHeader>
          <AlertDialogTitle>Record dispatch evidence?</AlertDialogTitle>
          <AlertDialogDescription>
            {order
              ? `This records the carrier or supplier handoff for ${order.orderNumber}. The order keeps its status and tracking URL.`
              : null}
          </AlertDialogDescription>
        </AlertDialogHeader>
        <div className="flex flex-col gap-4">
          <div className="flex flex-col gap-2">
            <Label htmlFor="admin-shop-evidence-carrier-name">
              Carrier or supplier name (required)
            </Label>
            <Input
              id="admin-shop-evidence-carrier-name"
              value={carrierName}
              onChange={(event) => setCarrierName(event.target.value)}
              required
              aria-required="true"
              disabled={busy}
              className="rounded-[2px]"
            />
          </div>
          <div className="flex flex-col gap-2">
            <Label htmlFor="admin-shop-evidence-carrier-reference">
              Carrier or supplier reference (required)
            </Label>
            <Input
              id="admin-shop-evidence-carrier-reference"
              value={carrierReference}
              onChange={(event) => setCarrierReference(event.target.value)}
              required
              aria-required="true"
              disabled={busy}
              className="rounded-[2px]"
            />
          </div>
          <div className="flex flex-col gap-2">
            <Label htmlFor="admin-shop-evidence-dispatched-at">
              Actual dispatch time (required)
            </Label>
            <Input
              id="admin-shop-evidence-dispatched-at"
              type="datetime-local"
              value={dispatchedAt}
              onChange={(event) => setDispatchedAt(event.target.value)}
              required
              aria-required="true"
              disabled={busy}
              className="rounded-[2px]"
            />
          </div>
        </div>
        <AlertDialogFooter>
          <AlertDialogCancel disabled={busy}>Cancel</AlertDialogCancel>
          <AlertDialogAction
            disabled={busy || !canSubmit}
            onClick={(event) => {
              event.preventDefault()
              void handleConfirm()
            }}
          >
            {busy ? "Saving…" : "Record evidence"}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}
