import { useEffect, useState, useSyncExternalStore } from "react"
import axios from "axios"
import { DownloadIcon, TriangleAlertIcon, XIcon } from "lucide-react"
import { toast } from "sonner"

import { postAdminPaymentRefund } from "@/api/adminApi"
import {
  forceCancelShopOrder,
  patchAdminShopOrderFulfilment,
  type AdminShopOrderListItem,
  type AdminShopPrintAssetPreview,
} from "@/api/adminShopOrdersApi"
import { AdminShopDispatchEvidenceDialog } from "@/components/dashboard/admin/AdminShopDispatchEvidenceDialog"
import { AdminShopFulfilmentActionDialog } from "@/components/dashboard/admin/AdminShopFulfilmentActionDialog"
import { AdminShopStatusCorrectionDialog } from "@/components/dashboard/admin/AdminShopStatusCorrectionDialog"
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
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { CheckboxLabel } from "@/components/ui/checkbox-label"
import {
  Drawer,
  DrawerClose,
  DrawerContent,
  DrawerDescription,
  DrawerTitle,
} from "@/components/ui/drawer"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Separator } from "@/components/ui/separator"
import { Textarea } from "@/components/ui/textarea"
import {
  ADMIN_SHOP_CORRECT_STATUS_LABEL,
  ADMIN_SHOP_DISPATCH_EVIDENCE_MISSING_LABEL,
  ADMIN_SHOP_RECORD_DISPATCH_EVIDENCE_LABEL,
  adminShopOrderFulfilmentLabel,
  adminShopOrderReviewNotice,
  adminShopPaymentSourceLabel,
  type AdminShopFulfilmentAction,
  canEditAdminShopOpsNotes,
  canEditAdminShopTrackingUrl,
  formatAdminShopGbpFromPence,
  formatAdminShopGbpOrDash,
  isAdminShopDispatchEvidenceMissing,
  nextAdminShopFulfilmentAction,
} from "@/lib/adminShopOrderFulfilment"
import {
  createAdminShopOrderPrintAssetsPageModule,
  httpAdminShopOrderPrintAssetsAdapters,
} from "@/lib/admin/createAdminShopOrderPrintAssetsPageModule"
import { adminShopOrderPaymentLabel } from "@/lib/admin/adminShopOrdersTablePresentation"
import {
  OPERATOR_RIGHT_DRAWER_BODY_CLASS,
  OPERATOR_RIGHT_DRAWER_CONTENT_CLASS,
} from "@/lib/operatorHome/shellResponsivePresentation"
import { cn } from "@/lib/utils"

/** Evidence timestamp for the drawer; a dash until the event exists. */
function formatEvidenceTime(value: string | null | undefined): string {
  return value ? new Date(value).toLocaleString() : "—"
}

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
  const [pendingAction, setPendingAction] =
    useState<AdminShopFulfilmentAction | null>(null)
  const [evidenceDialogOpen, setEvidenceDialogOpen] = useState(false)
  const [correctionDialogOpen, setCorrectionDialogOpen] = useState(false)
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
  const [printPreviews, setPrintPreviews] = useState<
    Partial<
      Record<
        AdminShopOrderListItem["printAssets"][number]["qrType"],
        AdminShopPrintAssetPreview
      >
    >
  >({})

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
    setPendingAction(null)
    setEvidenceDialogOpen(false)
    setCorrectionDialogOpen(false)
    setForceCancelConfirmOpen(false)
    setForceCancelReason("")
    setSkipRefund(false)
  }, [order])
  /* eslint-enable react-hooks/set-state-in-effect */

  useEffect(() => {
    printAssetsModule.setOrder(order)
  }, [order, printAssetsModule])

  useEffect(() => {
    if (!order) {
      setPrintPreviews({})
      return
    }
    let cancelled = false
    void (async () => {
      const next: Partial<
        Record<
          AdminShopOrderListItem["printAssets"][number]["qrType"],
          AdminShopPrintAssetPreview
        >
      > = {}
      for (const row of printAssets.rows) {
        if (row.status !== "Ready") {
          continue
        }
        const preview = await printAssetsModule.preview(row.qrType)
        if (preview) {
          next[row.qrType] = preview
        }
      }
      if (!cancelled) {
        setPrintPreviews(next)
      }
    })()
    return () => {
      cancelled = true
    }
  }, [order, printAssets.rows, printAssetsModule])

  if (!order) {
    return null
  }

  const reviewNotice = adminShopOrderReviewNotice(order)
  const nextAction = nextAdminShopFulfilmentAction(order)
  const trackingEditable = canEditAdminShopTrackingUrl(order.fulfilmentStatus)
  const notesEditable = canEditAdminShopOpsNotes(order.fulfilmentStatus)
  const canRefund =
    order.paymentStatus === "paid" &&
    Boolean(order.revolutOrderId?.trim())
  const isProcessing = order.fulfilmentStatus === "processing"
  const productionStarted =
    order.productionStartedAtUtc != null &&
    order.productionStartedAtUtc.trim().length > 0
  const canForceCancel = isProcessing
  const anyBusy = saving || refunding || forceCancelling
  const evidenceMissing = isAdminShopDispatchEvidenceMissing(order)

  const savePatch = async (input: {
    includeTracking?: boolean
    includeNotes?: boolean
  }) => {
    setSaving(true)
    try {
      const patch: Parameters<typeof patchAdminShopOrderFulfilment>[1] = {}
      if (input.includeTracking) {
        const trimmed = trackingUrl.trim()
        patch.trackingUrl = trimmed.length === 0 ? null : trimmed
      }
      if (input.includeNotes) {
        const trimmed = opsNotes.trim()
        patch.opsNotes = trimmed.length === 0 ? null : trimmed
      }

      const updated = await patchAdminShopOrderFulfilment(order.id, patch)
      onOrderUpdated(updated)
      toast.success("Order updated")
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

  const syncPrintAssetsToOrder = () => {
    const updatedPrintAssets = printAssetsModule.getSnapshot().rows.map(
      ({
        qrType,
        productTitle,
        quantity,
        status,
        fileName,
        lastError,
        approved,
        printRoute,
      }) => ({
        qrType,
        productTitle,
        quantity,
        status,
        fileName,
        lastError,
        approved,
        printRoute,
      })
    )
    onOrderUpdated({ ...order, printAssets: updatedPrintAssets })
  }

  const handlePrintDownload = async (
    qrType: AdminShopOrderListItem["printAssets"][number]["qrType"]
  ) => {
    const downloaded = await printAssetsModule.download(qrType)
    if (!downloaded) {
      toast.error("Could not download the print asset.")
    }
  }

  const handlePrintApprove = async (
    qrType: AdminShopOrderListItem["printAssets"][number]["qrType"]
  ) => {
    const approved = await printAssetsModule.approve(qrType)
    if (!approved) {
      toast.error("Could not approve the print asset.")
      return
    }
    syncPrintAssetsToOrder()
    toast.success("Print asset approved.")
  }

  const handlePrintRetry = async (
    qrType: AdminShopOrderListItem["printAssets"][number]["qrType"]
  ) => {
    const retried = await printAssetsModule.retry(qrType)
    if (!retried) {
      toast.error("Could not retry the print asset.")
      return
    }

    syncPrintAssetsToOrder()
    toast.success("Print asset is ready.")
  }

  const downloadLabel = (
    fileName: string | null,
    printRoute?: string | null
  ) => {
    if (
      fileName?.toLowerCase().endsWith(".csv")
      || printRoute === "PatchCsv60"
      || printRoute === "GuestCardCsv"
    ) {
      return "Download CSV"
    }
    return "Download PDF"
  }

  return (
    <>
      <Drawer open={open} onOpenChange={onOpenChange} direction="right">
        <DrawerContent className={OPERATOR_RIGHT_DRAWER_CONTENT_CLASS}>
          <div className="flex min-h-0 flex-1 flex-col">
            <div className="flex shrink-0 items-start justify-between gap-[22px] px-6 pt-8 pb-6">
              <div className="flex min-w-0 flex-1 flex-col gap-3">
                <div className="flex flex-col gap-1.5">
                  <DrawerTitle className="text-2xl font-bold text-foreground">
                    {order.orderNumber}
                  </DrawerTitle>
                  <DrawerDescription className="text-sm font-medium text-op-text-primary">
                    {order.locationNameSnapshot} ·{" "}
                    {order.restaurantName?.trim() ||
                      `Restaurant #${order.restaurantId}`}
                  </DrawerDescription>
                </div>
                <div className="flex flex-wrap items-center gap-2">
                  <Badge variant="soft">
                    {adminShopOrderFulfilmentLabel(order)}
                  </Badge>
                  {evidenceMissing ? (
                    <Badge variant="soft">
                      {ADMIN_SHOP_DISPATCH_EVIDENCE_MISSING_LABEL}
                    </Badge>
                  ) : null}
                  <Badge variant="soft">
                    {adminShopOrderPaymentLabel(order)}
                  </Badge>
                  {productionStarted ? (
                    <Badge variant="soft">Production started</Badge>
                  ) : null}
                  <span className="text-sm text-op-text-primary">
                    {formatAdminShopGbpFromPence(order.grossPence)}
                  </span>
                </div>
              </div>
              <DrawerClose asChild>
                <Button
                  type="button"
                  variant="ghost"
                  size="icon"
                  className="size-[42px] shrink-0 rounded-[2px] bg-muted hover:bg-muted/80"
                  aria-label="Close order details"
                >
                  <XIcon className="size-[18px]" aria-hidden />
                </Button>
              </DrawerClose>
            </div>

          <div
            className={cn(
              OPERATOR_RIGHT_DRAWER_BODY_CLASS,
              "flex flex-col px-6 pb-6"
            )}
          >
            {reviewNotice ? (
              <Alert variant="destructive" className="mt-6">
                <TriangleAlertIcon aria-hidden />
                <AlertTitle>{reviewNotice.title}</AlertTitle>
                {reviewNotice.lines.length > 0 ? (
                  <AlertDescription>
                    {reviewNotice.lines.map((line) => (
                      <p key={line}>{line}</p>
                    ))}
                  </AlertDescription>
                ) : null}
              </Alert>
            ) : null}

            <section className="grid gap-3 py-6 text-sm text-op-text-primary sm:grid-cols-2">
              <div>
                <p className="text-xs font-medium tracking-wide text-op-text-muted uppercase">
                  Paid at
                </p>
                <p>
                  {order.paidAtUtc
                    ? new Date(order.paidAtUtc).toLocaleString()
                    : "—"}
                </p>
              </div>
              <div>
                <p className="text-xs font-medium tracking-wide text-op-text-muted uppercase">
                  Location ID
                </p>
                <p>{order.locationId}</p>
              </div>
              <div>
                <p className="text-xs font-medium tracking-wide text-op-text-muted uppercase">
                  Preparation started
                </p>
                <p>{formatEvidenceTime(order.preparationStartedAtUtc)}</p>
              </div>
              <div>
                <p className="text-xs font-medium tracking-wide text-op-text-muted uppercase">
                  Production started
                </p>
                <p>{formatEvidenceTime(order.productionStartedAtUtc)}</p>
              </div>
              <div>
                <p className="text-xs font-medium tracking-wide text-op-text-muted uppercase">
                  Dispatched
                </p>
                <p>{formatEvidenceTime(order.dispatchedAtUtc)}</p>
                {evidenceMissing ? (
                  <p className="text-xs text-op-text-muted">
                    {ADMIN_SHOP_DISPATCH_EVIDENCE_MISSING_LABEL}
                  </p>
                ) : null}
                {order.dispatchCarrierName || order.dispatchCarrierReference ? (
                  <p className="text-xs break-all">
                    {[order.dispatchCarrierName, order.dispatchCarrierReference]
                      .filter(Boolean)
                      .join(" · ")}
                  </p>
                ) : null}
              </div>
              <div>
                <p className="text-xs font-medium tracking-wide text-op-text-muted uppercase">
                  Delivered
                </p>
                <p>{formatEvidenceTime(order.deliveredAtUtc)}</p>
              </div>
              <div>
                <p className="text-xs font-medium tracking-wide text-op-text-muted uppercase">
                  Payment source
                </p>
                <p>{adminShopPaymentSourceLabel(order.paymentSource)}</p>
              </div>
              <div>
                <p className="text-xs font-medium tracking-wide text-op-text-muted uppercase">
                  Revolut order ref
                </p>
                <p className="font-mono text-xs">
                  {order.revolutOrderRefRedacted ?? "—"}
                </p>
              </div>
            </section>

            <Separator />

            <section className="grid gap-3 py-6 text-sm text-op-text-primary sm:grid-cols-2">
              <div>
                <p className="text-xs font-medium tracking-wide text-op-text-muted uppercase">
                  Materials (net)
                </p>
                <p>{formatAdminShopGbpOrDash(order.materialsNetPence)}</p>
              </div>
              <div>
                <p className="text-xs font-medium tracking-wide text-op-text-muted uppercase">
                  VAT
                </p>
                <p>{formatAdminShopGbpOrDash(order.vatPence)}</p>
              </div>
              <div>
                <p className="text-xs font-medium tracking-wide text-op-text-muted uppercase">
                  Delivery (net)
                </p>
                <p>{formatAdminShopGbpOrDash(order.deliveryNetPence)}</p>
              </div>
              <div>
                <p className="text-xs font-medium tracking-wide text-op-text-muted uppercase">
                  Total (gross)
                </p>
                <p>{formatAdminShopGbpOrDash(order.grossPence)}</p>
              </div>
            </section>

            <Separator />

            <section className="flex flex-col gap-2 py-6 text-sm text-op-text-primary">
              <h3 className="text-sm font-semibold text-op-text-primary">
                Ship to
              </h3>
              <div className="flex flex-col gap-0.5">
                <p>{order.shipToContactName || "—"}</p>
                <p>{order.shipToAddressLine1 || "—"}</p>
                {order.shipToAddressLine2 ? (
                  <p>{order.shipToAddressLine2}</p>
                ) : null}
                <p>
                  {[order.shipToPostcode, order.shipToCountry]
                    .filter(Boolean)
                    .join(", ") || "—"}
                </p>
              </div>
            </section>

            <Separator />

            <section className="flex flex-col gap-3 py-6">
              <div>
                <h3 className="text-sm font-semibold text-op-text-primary">
                  Print assets
                </h3>
                <p className="text-xs text-op-text-muted">
                  Preview production details, approve, then download PDF or CSV
                  per product route. Quantity is the print-run instruction.
                </p>
              </div>
              <ul className="flex flex-col gap-2">
                {printAssets.rows.map((asset) => {
                  const busy = printAssets.busyQrType === asset.qrType
                  const preview = printPreviews[asset.qrType]
                  return (
                    <li
                      key={asset.qrType}
                      className="flex flex-col gap-3 rounded-xl border p-3"
                    >
                      <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
                        <div>
                          <p className="font-medium text-op-text-primary">
                            {asset.productTitle}
                          </p>
                          <p className="text-xs text-op-text-muted">
                            Print quantity: {asset.quantity}
                          </p>
                          {asset.lastError && (
                            <p className="mt-1 text-xs text-destructive">
                              {asset.lastError}
                            </p>
                          )}
                        </div>
                        <div className="flex flex-wrap items-center gap-2">
                          <Badge
                            variant={
                              asset.status === "Ready" ? "secondary" : "outline"
                            }
                          >
                            {asset.status}
                          </Badge>
                          {asset.approved && (
                            <Badge variant="secondary">Approved</Badge>
                          )}
                          {asset.canApprove && (
                            <Button
                              type="button"
                              size="sm"
                              variant="outline"
                              disabled={busy}
                              onClick={() =>
                                void handlePrintApprove(asset.qrType)
                              }
                            >
                              Approve
                            </Button>
                          )}
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
                              {downloadLabel(asset.fileName, asset.printRoute)}
                            </Button>
                          )}
                          {asset.canRetry && (
                            <Button
                              type="button"
                              size="sm"
                              variant="outline"
                              disabled={busy}
                              onClick={() =>
                                void handlePrintRetry(asset.qrType)
                              }
                            >
                              Retry
                            </Button>
                          )}
                        </div>
                      </div>
                      {preview && (
                        <dl className="grid gap-1 text-xs text-op-text-muted sm:grid-cols-2">
                          <div>
                            <dt className="font-medium text-op-text-primary">
                              Location
                            </dt>
                            <dd>
                              {preview.locationName} (#{preview.locationId})
                            </dd>
                          </div>
                          <div>
                            <dt className="font-medium text-op-text-primary">
                              Placement
                            </dt>
                            <dd>{preview.placement}</dd>
                          </div>
                          <div>
                            <dt className="font-medium text-op-text-primary">Qty</dt>
                            <dd>{preview.quantity}</dd>
                          </div>
                          <div className="sm:col-span-2">
                            <dt className="font-medium text-op-text-primary">
                              QR destination
                            </dt>
                            <dd className="break-all">
                              {preview.qrDestinationUrl}
                            </dd>
                          </div>
                          <div className="sm:col-span-2">
                            <dt className="font-medium text-op-text-primary">
                              Job ID
                            </dt>
                            <dd className="break-all font-mono">
                              {preview.jobId}
                            </dd>
                          </div>
                          {preview.offerHeadline && (
                            <div className="sm:col-span-2">
                              <dt className="font-medium text-op-text-primary">
                                Offer
                              </dt>
                              <dd>{preview.offerHeadline}</dd>
                            </div>
                          )}
                        </dl>
                      )}
                    </li>
                  )
                })}
                {printAssets.rows.length === 0 && (
                  <li className="text-sm text-op-text-primary">
                    No physical QR print assets for this order.
                  </li>
                )}
              </ul>
            </section>

            <Separator />

            <section className="flex flex-col gap-3 py-6">
              <h3 className="text-sm font-semibold text-op-text-primary">
                Line items
              </h3>
              <ul className="flex flex-col gap-2">
                {order.lines.map((line) => (
                  <li
                    key={`${line.catalogSkuId}-${line.titleSnapshot}`}
                    className="flex items-start justify-between gap-3 rounded-xl border p-3 text-sm text-op-text-primary"
                  >
                    <div>
                      <p className="font-medium">{line.titleSnapshot}</p>
                      <p className="text-op-text-muted">{line.catalogSkuId}</p>
                      <p className="text-op-text-muted">
                        Pack of {line.quantity}
                      </p>
                    </div>
                    <div className="text-right">
                      <p>
                        Unit {formatAdminShopGbpFromPence(line.unitNetPence)}
                      </p>
                      <p className="font-medium">
                        Line total{" "}
                        {formatAdminShopGbpFromPence(line.lineNetPence)}
                      </p>
                    </div>
                  </li>
                ))}
                {order.lines.length === 0 && (
                  <li className="text-sm text-op-text-primary">
                    No line items.
                  </li>
                )}
              </ul>
            </section>

            <Separator />

            <section className="flex flex-col gap-3 py-6">
              <h3 className="text-sm font-semibold text-op-text-primary">
                Payment refund
              </h3>
              {order.paymentStatus === "refunded" ? (
                <p className="text-sm leading-body text-op-text-primary">
                  This payment is already marked refunded.
                </p>
              ) : canRefund ? (
                <>
                  <p className="text-sm leading-body text-op-text-primary">
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
                <p className="text-sm leading-body text-op-text-primary">
                  Refund needs a paid order with a Revolut payment id.
                </p>
              )}
            </section>

            <Separator />

            <section className="flex flex-col gap-3 py-6">
              <div className="flex flex-col gap-2">
                <Label htmlFor="admin-shop-tracking-url">Tracking URL</Label>
                <Input
                  id="admin-shop-tracking-url"
                  value={trackingUrl}
                  onChange={(event) => setTrackingUrl(event.target.value)}
                  placeholder="https://…"
                  disabled={!trackingEditable || saving}
                  className="rounded-[2px]"
                />
                {!trackingEditable && (
                  <p className="text-xs text-op-text-muted">
                    Tracking can be added once the order is dispatched.
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
                  className="min-h-24 rounded-[2px]"
                />
              </div>

              <div className="flex flex-wrap gap-2">
                {notesEditable && (
                  <Button
                    type="button"
                    variant="op-tertiary"
                    className="rounded-[2px]"
                    disabled={anyBusy}
                    onClick={() =>
                      void savePatch({
                        includeNotes: true,
                        includeTracking: trackingEditable,
                      })
                    }
                  >
                    {trackingEditable ? "Save notes & tracking" : "Save notes"}
                  </Button>
                )}

                {nextAction && (
                  <Button
                    type="button"
                    variant="op-primary"
                    className="rounded-[2px]"
                    disabled={anyBusy}
                    onClick={() => setPendingAction(nextAction)}
                  >
                    {nextAction.label}
                  </Button>
                )}

                {evidenceMissing && (
                  <>
                    <Button
                      type="button"
                      variant="op-primary"
                      className="rounded-[2px]"
                      disabled={anyBusy}
                      onClick={() => setEvidenceDialogOpen(true)}
                    >
                      {ADMIN_SHOP_RECORD_DISPATCH_EVIDENCE_LABEL}
                    </Button>
                    <Button
                      type="button"
                      variant="op-tertiary"
                      className="rounded-[2px]"
                      disabled={anyBusy}
                      onClick={() => setCorrectionDialogOpen(true)}
                    >
                      {ADMIN_SHOP_CORRECT_STATUS_LABEL}
                    </Button>
                  </>
                )}

                {canForceCancel && (
                  <Button
                    type="button"
                    variant="destructive"
                    className="rounded-[2px]"
                    disabled={anyBusy}
                    onClick={() => setForceCancelConfirmOpen(true)}
                  >
                    Force cancel
                  </Button>
                )}
              </div>
            </section>
          </div>
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

      <AdminShopFulfilmentActionDialog
        order={pendingAction ? order : null}
        action={pendingAction}
        onClose={() => setPendingAction(null)}
        onCompleted={(updated) => {
          setPendingAction(null)
          onOrderUpdated(updated)
        }}
      />

      <AdminShopDispatchEvidenceDialog
        order={evidenceDialogOpen ? order : null}
        onClose={() => setEvidenceDialogOpen(false)}
        onCompleted={(updated) => {
          setEvidenceDialogOpen(false)
          onOrderUpdated(updated)
        }}
      />

      <AdminShopStatusCorrectionDialog
        order={correctionDialogOpen ? order : null}
        onClose={() => setCorrectionDialogOpen(false)}
        onCompleted={(updated) => {
          setCorrectionDialogOpen(false)
          onOrderUpdated(updated)
        }}
      />

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
