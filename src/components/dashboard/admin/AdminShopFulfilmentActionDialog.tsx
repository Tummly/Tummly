import { useState } from "react"
import axios from "axios"
import { toast } from "sonner"

import {
  type AdminShopDispatchBody,
  type AdminShopOrderListItem,
  markShopOrderDelivered,
  markShopOrderDispatched,
  markShopOrderPreparationStarted,
  markShopOrderProductionStarted,
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
import { Textarea } from "@/components/ui/textarea"
import {
  ADMIN_SHOP_TRANSITION_NOTE_MAX_LENGTH,
  type AdminShopFulfilmentAction,
  canSubmitAdminShopFulfilmentAction,
  normaliseAdminShopTransitionNote,
} from "@/lib/adminShopOrderFulfilment"

type AdminShopFulfilmentActionDialogProps = {
  order: AdminShopOrderListItem | null
  action: AdminShopFulfilmentAction | null
  onClose: () => void
  onCompleted: (updated: AdminShopOrderListItem) => void
}

function runFulfilmentAction(
  orderId: string,
  action: AdminShopFulfilmentAction,
  dispatch: AdminShopDispatchBody
): Promise<AdminShopOrderListItem> {
  const body = { note: dispatch.note }
  switch (action.kind) {
    case "preparation":
      return markShopOrderPreparationStarted(orderId, body)
    case "production":
      return markShopOrderProductionStarted(orderId, body)
    case "dispatch":
      return markShopOrderDispatched(orderId, dispatch)
    case "delivery":
      return markShopOrderDelivered(orderId, body)
  }
}

function fulfilmentErrorMessage(error: unknown): string {
  if (axios.isAxiosError(error)) {
    const message = (error.response?.data as { message?: string } | undefined)
      ?.message
    if (message) return message
  }
  return "Could not update fulfilment."
}

/**
 * Confirms one fulfilment action for an order. Dispatch collects the carrier
 * or supplier name and reference; a tracking URL is optional. Every action
 * takes an optional note for the audit row. Shared by the
 * Shop orders table and the order drawer so both record the same evidence.
 */
export function AdminShopFulfilmentActionDialog({
  order,
  action,
  onClose,
  onCompleted,
}: AdminShopFulfilmentActionDialogProps) {
  const [carrierName, setCarrierName] = useState("")
  const [carrierReference, setCarrierReference] = useState("")
  const [trackingUrl, setTrackingUrl] = useState("")
  const [note, setNote] = useState("")
  const [busy, setBusy] = useState(false)

  const isDispatch = action?.kind === "dispatch"
  const canSubmit =
    action !== null &&
    canSubmitAdminShopFulfilmentAction(action, {
      carrierName,
      carrierReference,
    })

  const resetFields = () => {
    setCarrierName("")
    setCarrierReference("")
    setTrackingUrl("")
    setNote("")
  }

  const handleOpenChange = (nextOpen: boolean) => {
    if (busy || nextOpen) return
    resetFields()
    onClose()
  }

  const handleConfirm = async () => {
    if (!order || !action || !canSubmit) return
    setBusy(true)
    try {
      const updated = await runFulfilmentAction(order.id, action, {
        carrierName,
        carrierReference,
        trackingUrl,
        note: normaliseAdminShopTransitionNote(note),
      })
      resetFields()
      toast.success(`Order marked as ${action.resultLabel}`)
      onCompleted(updated)
    } catch (error) {
      toast.error(fulfilmentErrorMessage(error))
    } finally {
      setBusy(false)
    }
  }

  return (
    <AlertDialog
      open={order !== null && action !== null}
      onOpenChange={handleOpenChange}
    >
      <AlertDialogContent className="rounded-2xl">
        <AlertDialogHeader>
          <AlertDialogTitle>
            {action ? `${action.label}?` : "Update fulfilment?"}
          </AlertDialogTitle>
          <AlertDialogDescription>
            {order && action
              ? `This updates ${order.orderNumber} to ${action.resultLabel}.`
              : null}
          </AlertDialogDescription>
        </AlertDialogHeader>
        {isDispatch && (
          <div className="flex flex-col gap-4">
            <div className="flex flex-col gap-2">
              <Label htmlFor="admin-shop-dispatch-carrier-name">
                Carrier or supplier name (required)
              </Label>
              <Input
                id="admin-shop-dispatch-carrier-name"
                value={carrierName}
                onChange={(event) => setCarrierName(event.target.value)}
                required
                aria-required="true"
                disabled={busy}
                className="rounded-[2px]"
              />
            </div>
            <div className="flex flex-col gap-2">
              <Label htmlFor="admin-shop-dispatch-carrier-reference">
                Carrier or supplier reference (required)
              </Label>
              <Input
                id="admin-shop-dispatch-carrier-reference"
                value={carrierReference}
                onChange={(event) => setCarrierReference(event.target.value)}
                required
                aria-required="true"
                disabled={busy}
                className="rounded-[2px]"
              />
            </div>
            <div className="flex flex-col gap-2">
              <Label htmlFor="admin-shop-dispatch-tracking-url">
                Tracking URL (optional)
              </Label>
              <Input
                id="admin-shop-dispatch-tracking-url"
                value={trackingUrl}
                onChange={(event) => setTrackingUrl(event.target.value)}
                placeholder="https://…"
                disabled={busy}
                className="rounded-[2px]"
              />
            </div>
          </div>
        )}
        <div className="flex flex-col gap-2">
          <Label htmlFor="admin-shop-transition-note">
            Note for this change (optional)
          </Label>
          <Textarea
            id="admin-shop-transition-note"
            value={note}
            onChange={(event) => setNote(event.target.value)}
            maxLength={ADMIN_SHOP_TRANSITION_NOTE_MAX_LENGTH}
            placeholder="e.g. courier collection reference"
            disabled={busy}
            className="min-h-20 rounded-[2px]"
          />
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
            {busy ? "Saving…" : "Confirm"}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}
