import { useState } from "react"
import axios from "axios"
import { toast } from "sonner"

import {
  type AdminShopOrderListItem,
  correctShopOrderStatus,
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
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import { canSubmitAdminShopStatusCorrection } from "@/lib/adminShopOrderFulfilment"

type AdminShopStatusCorrectionDialogProps = {
  order: AdminShopOrderListItem | null
  onClose: () => void
  onCompleted: (updated: AdminShopOrderListItem) => void
}

function statusCorrectionErrorMessage(error: unknown): string {
  if (axios.isAxiosError(error)) {
    const message = (error.response?.data as { message?: string } | undefined)
      ?.message
    if (message) return message
  }
  return "Could not correct the status."
}

/**
 * Returns an order with no carrier evidence to its last substantiated state.
 * The cleared values stay in the audit log with the reason.
 */
export function AdminShopStatusCorrectionDialog({
  order,
  onClose,
  onCompleted,
}: AdminShopStatusCorrectionDialogProps) {
  const [reason, setReason] = useState("")
  const [busy, setBusy] = useState(false)

  const canSubmit = canSubmitAdminShopStatusCorrection(reason)

  const handleOpenChange = (nextOpen: boolean) => {
    if (busy || nextOpen) return
    setReason("")
    onClose()
  }

  const handleConfirm = async () => {
    if (!order || !canSubmit) return
    setBusy(true)
    try {
      const updated = await correctShopOrderStatus(order.id, reason)
      setReason("")
      toast.success("Status corrected")
      onCompleted(updated)
    } catch (error) {
      toast.error(statusCorrectionErrorMessage(error))
    } finally {
      setBusy(false)
    }
  }

  return (
    <AlertDialog open={order !== null} onOpenChange={handleOpenChange}>
      <AlertDialogContent className="rounded-2xl">
        <AlertDialogHeader>
          <AlertDialogTitle>Correct status?</AlertDialogTitle>
          <AlertDialogDescription>
            {order
              ? `This returns ${order.orderNumber} to its last recorded stage. The dispatch and delivery details are cleared and kept in the audit log.`
              : null}
          </AlertDialogDescription>
        </AlertDialogHeader>
        <div className="flex flex-col gap-2">
          <Label htmlFor="admin-shop-correction-reason">
            Reason (required)
          </Label>
          <Textarea
            id="admin-shop-correction-reason"
            value={reason}
            onChange={(event) => setReason(event.target.value)}
            required
            aria-required="true"
            maxLength={500}
            disabled={busy}
            className="min-h-20 rounded-[2px]"
          />
        </div>
        <AlertDialogFooter>
          <AlertDialogCancel disabled={busy}>Cancel</AlertDialogCancel>
          <AlertDialogAction
            variant="destructive-solid"
            disabled={busy || !canSubmit}
            onClick={(event) => {
              event.preventDefault()
              void handleConfirm()
            }}
          >
            {busy ? "Saving…" : "Correct status"}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}
