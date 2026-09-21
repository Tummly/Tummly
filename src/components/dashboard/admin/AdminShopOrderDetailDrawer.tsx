import { useEffect, useState, useSyncExternalStore } from "react"
import axios from "axios"
import { DownloadIcon, FactoryIcon, RefreshCcwIcon } from "lucide-react"
import { toast } from "sonner"

import { postAdminPaymentRefund } from "@/api/adminApi"
import {
  forceCancelShopOrder,
  markShopOrderProductionStarted,
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
import { CheckboxLabel } from "@/components/ui/checkbox-label"
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
import {
  createAdminShopOrderPrintAssetsPageModule,
  httpAdminShopOrderPrintAssetsAdapters,
} from "@/lib/admin/createAdminShopOrderPrintAssetsPageModule"

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

function forceCancelErrorMessage(code: string | undefined): string {
  switch (code) {
    case "in_transit":
      return "This order is already dispatched and cannot be force-cancelled."
    case "delivered":
      return "This order is already delivered and cannot be force-cancelled."
    case "shop_order_not_cancellable":
      return "This order cannot be force-cancelled in its current state."
    case "invalid_cancel_reason":
      return "Enter a non-empty cancel reason."
    case "order_not_found":
      return "Shop order was not found."
    default:
      return code
        ? `Could not force-cancel order (${code}).`
        : "Could not force-cancel order."
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
  const [markStartedConfirmOpen, setMarkStartedConfirmOpen] = useState(false)
  const [markingStarted, setMarkingStarted] = useState(false)
  const [forceCancelConfirmOpen, setForceCancelConfirmOpen] = useState(false)
  const [forceCancelling, setForceCancelling] = useState(false)
  const [forceCancelReason, setForceCancelReason] = useState("")
  const [skipRefund, setSkipRefund] = useState(false)
  const [printAssetsModule] = useState(() =>
    createAdminShopOrderPrintAssetsPageModule(
      httpAdminShopOrderPrintAssetsAdapters
    )
  )
  const printAssets = useSyncExternalStore(
    printAssetsModule.subscribe,
    printAssetsModule.getSnapshot,
    printAssetsModule.getSnapshot
  )

  /* eslint-disable react-hooks/set-state-in-effect */
  useEffect(() => {
    if (!order) {
      return
    }
    setTrackingUrl(order.trackingUrl ?? "")
    setOpsNotes(order.opsNotes ?? "")
    setPartialAmountPounds("")
    setRefundConfirmOpen(false)
    setRefundIdempotencyKey(crypto.randomUUID())
    setMarkStartedConfirmOpen(false)
    setForceCancelConfirmOpen(false)
    setForceCancelReason("")
    setSkipRefund(false)
  }, [order])
  /* eslint-enable react-hooks/set-state-in-effect */

  useEffect(() => {
    printAssetsModule.setOrder(order)
  }, [order, printAssetsModule])

  if (!order) {
    return null
  }

  const nextAction = nextAdminShopFulfilmentAction(order.fulfilmentStatus)
  const trackingEditable = canEditAdminShopTrackingUrl(order.fulfilmentStatus)
  const notesEditable = canEditAdminShopOpsNotes(order.fulfilmentStatus)
  const canRefund =
    order.paymentStatus === "paid" &&
    Boolean(order.revolutOrderId?.trim())
  const isProcessing = order.fulfilmentStatus === "processing"
  const productionStarted =
    order.productionStartedAtUtc != null &&
    order.productionStartedAtUtc.trim().length > 0
  const canMarkProductionStarted = isProcessing && !productionStarted
  const canForceCancel = isProcessing
  const anyBusy = saving || refunding || markingStarted || forceCancelling

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

  const handleConfirmMarkStarted = async () => {
    setMarkingStarted(true)
    try {
      const updated = await markShopOrderProductionStarted(order.id)
      onOrderUpdated(updated)
      setMarkStartedConfirmOpen(false)
      toast.success(
        "Production marked as started. Operator cancel is now blocked."
      )
    } catch (error) {
      if (axios.isAxiosError(error)) {
        const message =
          (error.response?.data as { message?: string } | undefined)?.message ??
          "Could not mark production started."
        toast.error(message)
        return
      }
      toast.error("Could not mark production started.")
    } finally {
      setMarkingStarted(false)
    }
  }

  const handleConfirmForceCancel = async () => {
    const reason = forceCancelReason.trim()
    if (reason.length === 0) {
      toast.error("Enter a cancel reason.")
      return
    }

    setForceCancelling(true)
    try {
      const updated = await forceCancelShopOrder(order.id, {
        reason,
        skipRefund,
      })
      onOrderUpdated(updated)
      setForceCancelConfirmOpen(false)
      setForceCancelReason("")
      setSkipRefund(false)
      toast.success(
        skipRefund
          ? "Order force-cancelled without refund."
          : "Order force-cancelled. Refund runs when a Revolut payment exists."
      )
    } catch (error) {
      if (axios.isAxiosError(error)) {
        const data = error.response?.data as
          | { code?: string; message?: string }
          | undefined
        toast.error(data?.message ?? forceCancelErrorMessage(data?.code))
        return
      }
      toast.error("Could not force-cancel order.")
    } finally {
      setForceCancelling(false)
    }
  }

  const handlePrintDownload = async (
    qrType: "TableTent" | "WindowSticker" | "OfferCard"
  ) => {
    const downloaded = await printAssetsModule.download(qrType)
    if (!downloaded) {
      toast.error("Could not download the print-ready PDF.")
    }
  }

  const handlePrintRetry = async (
    qrType: "TableTent" | "WindowSticker" | "OfferCard"
  ) => {
    const retried = await printAssetsModule.retry(qrType)
    if (!retried) {
      toast.error("Could not retry the print-ready PDF.")
      return
    }

    const updatedPrintAssets = printAssetsModule.getSnapshot().rows.map(
      ({ qrType, quantity, status, fileName, lastError }) => ({
        qrType,
        quantity,
        status,
        fileName,
        lastError,
      })
    )
    onOrderUpdated({ ...order, printAssets: updatedPrintAssets })
    toast.success("Print-ready PDF is ready.")
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
              <Badge variant="outline">
                {order.isComplimentary ? "Free" : order.paymentStatus}
              </Badge>
              {productionStarted ? (
                <Badge variant="outline">Production started</Badge>
              ) : null}
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
              <div>
                <p className="text-xs font-medium tracking-wide text-muted-foreground uppercase">
                  Production started
                </p>
                <p>
                  {productionStarted
                    ? new Date(order.productionStartedAtUtc!).toLocaleString()
                    : "—"}
                </p>
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
              <div>
                <h3 className="text-sm font-semibold">Print-ready PDFs</h3>
                <p className="text-xs text-muted-foreground">
                  One master PDF per ordered QR type. Quantity is the print-run
                  instruction.
                </p>
              </div>
              <ul className="flex flex-col gap-2">
                {printAssets.rows.map((asset) => {
                  const busy = printAssets.busyQrType === asset.qrType
                  return (
                    <li
                      key={asset.qrType}
                      className="flex flex-col gap-3 rounded-xl border px-3 py-3 sm:flex-row sm:items-center sm:justify-between"
                    >
                      <div>
                        <p className="font-medium">
                          {asset.qrType === "TableTent"
                            ? "Table Tent QR"
                            : asset.qrType === "WindowSticker"
                              ? "Window Sticker QR"
                              : "Offer Card"}
                        </p>
                        <p className="text-xs text-muted-foreground">
                          Print quantity: {asset.quantity}
                        </p>
                        {asset.lastError && (
                          <p className="mt-1 text-xs text-destructive">
                            {asset.lastError}
                          </p>
                        )}
                      </div>
                      <div className="flex items-center gap-2">
                        <Badge
                          variant={
                            asset.status === "Ready" ? "secondary" : "outline"
                          }
                        >
                          {asset.status}
                        </Badge>
                        {asset.canDownload && (
                          <Button
                            type="button"
                            size="sm"
                            variant="outline"
                            disabled={busy}
                            onClick={() =>
                              void handlePrintDownload(asset.qrType)
                            }
                          >
                            <DownloadIcon />
                            Download PDF
                          </Button>
                        )}
                        {asset.canRetry && (
                          <Button
                            type="button"
                            size="sm"
                            variant="outline"
                            disabled={busy}
                            onClick={() => void handlePrintRetry(asset.qrType)}
                          >
                            <RefreshCcwIcon />
                            Retry
                          </Button>
                        )}
                      </div>
                    </li>
                  )
                })}
                {printAssets.rows.length === 0 && (
                  <li className="text-sm text-muted-foreground">
                    No physical QR print assets for this order.
                  </li>
                )}
              </ul>
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
                    disabled={anyBusy}
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
                      disabled={anyBusy}
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
                    disabled={anyBusy}
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

                {canMarkProductionStarted && (
                  <Button
                    type="button"
                    variant="outline"
                    disabled={anyBusy}
                    onClick={() => setMarkStartedConfirmOpen(true)}
                  >
                    <FactoryIcon />
                    Mark production started
                  </Button>
                )}

                {canForceCancel && (
                  <Button
                    type="button"
                    variant="destructive"
                    disabled={anyBusy}
                    onClick={() => setForceCancelConfirmOpen(true)}
                  >
                    Force cancel
                  </Button>
                )}

                {isProcessing && (
                  <p className="w-full text-xs text-muted-foreground">
                    Notes and tracking are saved when you mark the order as
                    Dispatched.
                    {productionStarted
                      ? " Production has started — operator cancel is blocked."
                      : " Mark production started to block operator cancel."}
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

      <AlertDialog
        open={markStartedConfirmOpen}
        onOpenChange={(nextOpen) => {
          if (markingStarted) {
            return
          }
          setMarkStartedConfirmOpen(nextOpen)
        }}
      >
        <AlertDialogContent className="rounded-2xl">
          <AlertDialogHeader>
            <AlertDialogTitle>Mark production started?</AlertDialogTitle>
            <AlertDialogDescription>
              This stamps {order.orderNumber} so the operator cannot cancel it.
              You can still force-cancel from Admin if needed.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel disabled={markingStarted}>
              Cancel
            </AlertDialogCancel>
            <AlertDialogAction
              disabled={markingStarted}
              onClick={(event) => {
                event.preventDefault()
                void handleConfirmMarkStarted()
              }}
            >
              {markingStarted ? "Marking…" : "Mark started"}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>

      <AlertDialog
        open={forceCancelConfirmOpen}
        onOpenChange={(nextOpen) => {
          if (forceCancelling) {
            return
          }
          setForceCancelConfirmOpen(nextOpen)
        }}
      >
        <AlertDialogContent className="rounded-2xl">
          <AlertDialogHeader>
            <AlertDialogTitle>Force-cancel this order?</AlertDialogTitle>
            <AlertDialogDescription>
              Cancels {order.orderNumber} even if production has started.
              Unless you skip refund, a full Revolut refund is attempted when a
              payment id exists.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <div className="flex flex-col gap-3 px-6 pb-2">
            <div className="flex flex-col gap-2">
              <Label htmlFor="admin-shop-force-cancel-reason">
                Cancel reason
              </Label>
              <Textarea
                id="admin-shop-force-cancel-reason"
                value={forceCancelReason}
                onChange={(event) => setForceCancelReason(event.target.value)}
                placeholder="e.g. warehouse damage / customer request"
                disabled={forceCancelling}
                className="min-h-20 rounded-xl"
              />
            </div>
            <CheckboxLabel
              id="admin-shop-force-cancel-skip-refund"
              checked={skipRefund}
              disabled={forceCancelling}
              onCheckedChange={(checked) => setSkipRefund(checked)}
            >
              Skip refund (cancel fulfilment only)
            </CheckboxLabel>
          </div>
          <AlertDialogFooter>
            <AlertDialogCancel disabled={forceCancelling}>
              Keep order
            </AlertDialogCancel>
            <AlertDialogAction
              variant="destructive-solid"
              disabled={forceCancelling}
              onClick={(event) => {
                event.preventDefault()
                void handleConfirmForceCancel()
              }}
            >
              {forceCancelling ? "Cancelling…" : "Force cancel"}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </>
  )
}
