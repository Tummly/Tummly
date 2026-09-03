import { useEffect, useState } from "react"
import axios from "axios"
import { toast } from "sonner"

import { postAdminPaymentRefund } from "@/api/adminApi"
import {
  patchAdminShopOrderFulfilment,
  type AdminShopOrderListItem,
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
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import {
  Drawer,
  DrawerContent,
  DrawerDescription,
  DrawerHeader,
  DrawerTitle,
} from "@/components/ui/drawer"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Separator } from "@/components/ui/separator"
import { Textarea } from "@/components/ui/textarea"
import {
  adminShopFulfilmentLabel,
  canEditAdminShopOpsNotes,
  canEditAdminShopTrackingUrl,
  formatAdminShopGbpFromPence,
  nextAdminShopFulfilmentAction,
} from "@/lib/adminShopOrderFulfilment"

type AdminShopOrderDetailDrawerProps = {
  order: AdminShopOrderListItem | null
  open: boolean
  onOpenChange: (open: boolean) => void
  onOrderUpdated: (order: AdminShopOrderListItem) => void
}

function refundErrorMessage(code: string | undefined): string {
  switch (code) {
    case "payment_not_found":
      return "No matching Revolut payment was found for this restaurant."
    case "restaurant_not_found":
      return "Restaurant was not found."
    case "partial_refund_while_bindable":
      return "Partial refund is blocked while bindable top-up credit remains."
    case "refund_in_progress":
      return "A refund with this key is already in progress."
    case "idempotency_key_required":
      return "Refund request was missing an idempotency key."
    default:
      return code
        ? `Could not start refund (${code}).`
        : "Could not start refund."
  }
}

export function AdminShopOrderDetailDrawer({
  order,
  open,
  onOpenChange,
  onOrderUpdated,
}: AdminShopOrderDetailDrawerProps) {
  const [trackingUrl, setTrackingUrl] = useState("")
  const [opsNotes, setOpsNotes] = useState("")
  const [saving, setSaving] = useState(false)
  const [refundConfirmOpen, setRefundConfirmOpen] = useState(false)
  const [refunding, setRefunding] = useState(false)
  const [partialAmountPounds, setPartialAmountPounds] = useState("")
  const [refundIdempotencyKey, setRefundIdempotencyKey] = useState(() =>
    crypto.randomUUID()
  )

  useEffect(() => {
    if (!order) {
      return
    }
    setTrackingUrl(order.trackingUrl ?? "")
    setOpsNotes(order.opsNotes ?? "")
    setPartialAmountPounds("")
    setRefundConfirmOpen(false)
    setRefundIdempotencyKey(crypto.randomUUID())
  }, [order])

  if (!order) {
    return null
  }

  const nextAction = nextAdminShopFulfilmentAction(order.fulfilmentStatus)
  const trackingEditable = canEditAdminShopTrackingUrl(order.fulfilmentStatus)
  const notesEditable = canEditAdminShopOpsNotes(order.fulfilmentStatus)
  const canRefund =
    order.paymentStatus === "paid" &&
    Boolean(order.revolutOrderId?.trim())

  const savePatch = async (input: {
    fulfilmentStatus?: "in_transit" | "delivered"
    includeTracking?: boolean
    includeNotes?: boolean
  }) => {
    setSaving(true)
    try {
      const patch: Parameters<typeof patchAdminShopOrderFulfilment>[1] = {}
      if (input.fulfilmentStatus) {
        patch.fulfilmentStatus = input.fulfilmentStatus
      }
      if (input.includeTracking) {
        const trimmed = trackingUrl.trim()
        patch.trackingUrl = trimmed.length === 0 ? null : trimmed
      }
      if (input.includeNotes) {
        const trimmed = opsNotes.trim()
        patch.opsNotes = trimmed.length === 0 ? null : trimmed
      }

      if (
        input.fulfilmentStatus === "in_transit" &&
        trackingEditable &&
        trackingUrl.trim().length > 0
      ) {
        patch.trackingUrl = trackingUrl.trim()
      }

      const updated = await patchAdminShopOrderFulfilment(order.id, patch)
      onOrderUpdated(updated)
      toast.success(
        input.fulfilmentStatus
          ? `Order marked as ${adminShopFulfilmentLabel(input.fulfilmentStatus)}`
          : "Order fulfilment updated"
      )
    } catch (error) {
      if (axios.isAxiosError(error) && error.response?.status === 409) {
        const message =
          (error.response.data as { message?: string } | undefined)?.message ??
          "This fulfilment change is not allowed."
        toast.error(message)
        return
      }
      if (axios.isAxiosError(error) && error.response?.status === 400) {
        const message =
          (error.response.data as { message?: string } | undefined)?.message ??
          "Could not update fulfilment."
        toast.error(message)
        return
      }
      toast.error("Could not update fulfilment.")
    } finally {
      setSaving(false)
    }
  }

  const handleConfirmRefund = async () => {
    const revolutOrderId = order.revolutOrderId?.trim()
    if (!revolutOrderId) {
      toast.error("This order has no Revolut payment id.")
      return
    }

    let amountMinor: number | undefined
    const amountText = partialAmountPounds.trim()
    if (amountText.length > 0) {
      const pounds = Number(amountText)
      if (!Number.isFinite(pounds) || pounds <= 0) {
        toast.error("Enter a valid partial refund amount in pounds.")
        return
      }
      amountMinor = Math.round(pounds * 100)
      if (amountMinor > order.grossPence) {
        toast.error("Partial refund cannot exceed the order total.")
        return
      }
    }

    setRefunding(true)
    try {
      const result = await postAdminPaymentRefund(
        {
          restaurantId: order.restaurantId,
          orderId: revolutOrderId,
          amountMinor,
        },
        refundIdempotencyKey
      )
      setRefundConfirmOpen(false)
      setRefundIdempotencyKey(crypto.randomUUID())
      toast.success(
        result.refundOrderId
          ? `Refund started (${result.refundOrderId}). Payment becomes Refunded after Revolut confirms.`
          : "Refund started. Payment becomes Refunded after Revolut confirms."
      )
    } catch (error) {
      if (axios.isAxiosError(error)) {
        const code = (error.response?.data as { code?: string } | undefined)
          ?.code
        toast.error(refundErrorMessage(code))
        return
      }
      toast.error("Could not start refund.")
    } finally {
      setRefunding(false)
    }
  }

  return (
    <>
      <Drawer open={open} onOpenChange={onOpenChange}>
        <DrawerContent className="mx-auto flex max-h-[92vh] w-full max-w-xl flex-col rounded-t-2xl">
          <DrawerHeader className="border-b text-left">
            <DrawerTitle className="font-heading text-xl">
              {order.orderNumber}
            </DrawerTitle>
            <DrawerDescription>
              {order.locationNameSnapshot} · Restaurant #{order.restaurantId}
            </DrawerDescription>
          </DrawerHeader>

          <div className="flex flex-1 flex-col gap-6 overflow-y-auto px-4 py-5">
            <section className="flex flex-wrap items-center gap-2">
              <Badge variant="secondary">
                {adminShopFulfilmentLabel(order.fulfilmentStatus)}
              </Badge>
              <Badge variant="outline">{order.paymentStatus}</Badge>
              <span className="text-sm text-muted-foreground">
                {formatAdminShopGbpFromPence(order.grossPence)}
              </span>
            </section>

            <section className="grid gap-3 text-sm sm:grid-cols-2">
              <div>
                <p className="text-xs font-medium tracking-wide text-muted-foreground uppercase">
                  Paid at
                </p>
                <p>
                  {order.paidAtUtc
                    ? new Date(order.paidAtUtc).toLocaleString()
                    : "—"}
                </p>
              </div>
              <div>
                <p className="text-xs font-medium tracking-wide text-muted-foreground uppercase">
                  Location ID
                </p>
                <p>{order.locationId}</p>
              </div>
              <div className="sm:col-span-2">
                <p className="text-xs font-medium tracking-wide text-muted-foreground uppercase">
                  Revolut payment id
                </p>
                <p className="break-all font-mono text-xs">
                  {order.revolutOrderId?.trim() || "—"}
                </p>
              </div>
            </section>

            <Separator />

            <section className="flex flex-col gap-3">
              <h3 className="text-sm font-semibold">Line items</h3>
              <ul className="flex flex-col gap-2">
                {order.lines.map((line) => (
                  <li
                    key={`${line.catalogSkuId}-${line.titleSnapshot}`}
                    className="flex items-start justify-between gap-3 rounded-xl border px-3 py-2 text-sm"
                  >
                    <div>
                      <p className="font-medium">{line.titleSnapshot}</p>
                      <p className="text-muted-foreground">
                        {line.catalogSkuId}
                      </p>
                    </div>
                    <div className="text-right">
                      <p>×{line.quantity}</p>
                      <p className="text-muted-foreground">
                        {formatAdminShopGbpFromPence(line.lineNetPence)} net
                      </p>
                    </div>
                  </li>
                ))}
                {order.lines.length === 0 && (
                  <li className="text-sm text-muted-foreground">
                    No line items.
                  </li>
                )}
              </ul>
            </section>

            <Separator />

            <section className="flex flex-col gap-3">
              <h3 className="text-sm font-semibold">Payment refund</h3>
              {order.paymentStatus === "refunded" ? (
                <p className="text-sm text-muted-foreground">
                  This payment is already marked refunded.
                </p>
              ) : canRefund ? (
                <>
                  <p className="text-sm text-muted-foreground">
                    Starts a Revolut refund for this Shop payment. Payment
                    status flips to Refunded after the refund webhook, and a
                    credit note is minted then. Leave amount empty for a full
                    refund.
                  </p>
                  <div className="flex flex-col gap-2">
                    <Label htmlFor="admin-shop-partial-refund">
                      Partial amount (GBP, optional)
                    </Label>
                    <Input
                      id="admin-shop-partial-refund"
                      inputMode="decimal"
                      value={partialAmountPounds}
                      onChange={(event) =>
                        setPartialAmountPounds(event.target.value)
                      }
                      placeholder="e.g. 12.50"
                      disabled={refunding}
                      className="rounded-xl"
                    />
                  </div>
                  <Button
                    type="button"
                    variant="destructive"
                    disabled={refunding || saving}
                    onClick={() => setRefundConfirmOpen(true)}
                  >
                    Refund payment
                  </Button>
                </>
              ) : (
                <p className="text-sm text-muted-foreground">
                  Refund needs a paid order with a Revolut payment id.
                </p>
              )}
            </section>

            <Separator />

            <section className="flex flex-col gap-3">
              <div className="flex flex-col gap-2">
                <Label htmlFor="admin-shop-tracking-url">Tracking URL</Label>
                <Input
                  id="admin-shop-tracking-url"
                  value={trackingUrl}
                  onChange={(event) => setTrackingUrl(event.target.value)}
                  placeholder="https://…"
                  disabled={!trackingEditable || saving}
                  className="rounded-xl"
                />
                {!trackingEditable && (
                  <p className="text-xs text-muted-foreground">
                    Tracking is read-only for this status.
                  </p>
                )}
              </div>

              <div className="flex flex-col gap-2">
                <Label htmlFor="admin-shop-ops-notes">Ops notes</Label>
                <Textarea
                  id="admin-shop-ops-notes"
                  value={opsNotes}
                  onChange={(event) => setOpsNotes(event.target.value)}
                  placeholder="Internal notes for warehouse / support"
                  disabled={!notesEditable || saving}
                  className="min-h-24 rounded-xl"
                />
              </div>

              <div className="flex flex-wrap gap-2">
                {(order.fulfilmentStatus === "in_transit" ||
                  order.fulfilmentStatus === "delivered") &&
                  notesEditable && (
                    <Button
                      type="button"
                      variant="outline"
                      disabled={saving}
                      onClick={() =>
                        void savePatch({
                          includeNotes: true,
                          includeTracking:
                            order.fulfilmentStatus === "in_transit" &&
                            trackingEditable,
                        })
                      }
                    >
                      Save notes
                      {order.fulfilmentStatus === "in_transit" &&
                      trackingEditable
                        ? " & tracking"
                        : ""}
                    </Button>
                  )}

                {nextAction && (
                  <Button
                    type="button"
                    disabled={saving}
                    onClick={() =>
                      void savePatch({
                        fulfilmentStatus: nextAction.status,
                        includeNotes: notesEditable,
                      })
                    }
                  >
                    {nextAction.label}
                  </Button>
                )}

                {order.fulfilmentStatus === "processing" && (
                  <p className="w-full text-xs text-muted-foreground">
                    Notes and tracking are saved when you mark the order as
                    Dispatched.
                  </p>
                )}
              </div>
            </section>
          </div>
        </DrawerContent>
      </Drawer>

      <AlertDialog
        open={refundConfirmOpen}
        onOpenChange={(nextOpen) => {
          if (refunding) {
            return
          }
          setRefundConfirmOpen(nextOpen)
        }}
      >
        <AlertDialogContent className="rounded-2xl">
          <AlertDialogHeader>
            <AlertDialogTitle>Refund this Shop payment?</AlertDialogTitle>
            <AlertDialogDescription>
              This calls Revolut for {order.orderNumber}
              {partialAmountPounds.trim()
                ? ` (${partialAmountPounds.trim()} GBP partial)`
                : " (full amount)"}
              . Payment stays Paid until Revolut confirms the refund webhook.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel disabled={refunding}>Cancel</AlertDialogCancel>
            <AlertDialogAction
              variant="destructive-solid"
              disabled={refunding}
              onClick={(event) => {
                event.preventDefault()
                void handleConfirmRefund()
              }}
            >
              {refunding ? "Starting refund…" : "Start refund"}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </>
  )
}
