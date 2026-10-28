import type {
  AdminShopFulfilmentStatus,
  AdminShopOrderListItem,
} from "@/api/adminShopOrdersApi"

export const ADMIN_SHOP_FULFILMENT_STATUS_LABELS: Record<
  AdminShopFulfilmentStatus,
  string
> = {
  processing: "Confirmed",
  in_transit: "Dispatched",
  delivered: "Delivered",
  cancelled: "Cancelled",
}

export const ADMIN_SHOP_FULFILMENT_FILTER_OPTIONS: Array<{
  id: AdminShopFulfilmentStatus | "all"
  label: string
}> = [
  { id: "processing", label: "Confirmed" },
  { id: "in_transit", label: "Dispatched" },
  { id: "delivered", label: "Delivered" },
  { id: "cancelled", label: "Cancelled" },
  { id: "all", label: "All" },
]

export const ALL_ADMIN_SHOP_FULFILMENT_STATUSES: AdminShopFulfilmentStatus[] = [
  "processing",
  "in_transit",
  "delivered",
  "cancelled",
]

/** Stored status label for filters. Row and drawer labels use the evidence. */
export function adminShopFulfilmentLabel(status: string): string {
  if (status in ADMIN_SHOP_FULFILMENT_STATUS_LABELS) {
    return ADMIN_SHOP_FULFILMENT_STATUS_LABELS[
      status as AdminShopFulfilmentStatus
    ]
  }
  return status
}

/** Admin-only marker for a Dispatched or Delivered order without carrier evidence. */
export const ADMIN_SHOP_DISPATCH_EVIDENCE_MISSING_LABEL =
  "Dispatch evidence missing"

export const ADMIN_SHOP_RECORD_DISPATCH_EVIDENCE_LABEL =
  "Record dispatch evidence"

export const ADMIN_SHOP_CORRECT_STATUS_LABEL = "Correct status"

/** Fields that decide the customer fulfilment label and the next action. */
export type AdminShopFulfilmentEvidence = Pick<
  AdminShopOrderListItem,
  | "fulfilmentStatus"
  | "preparationStartedAtUtc"
  | "productionStartedAtUtc"
  | "dispatchedAtUtc"
  | "dispatchCarrierName"
  | "dispatchCarrierReference"
  | "dispatchSource"
  | "dispatchedByAdminUserId"
  | "deliveredAtUtc"
>

export type AdminShopFulfilmentActionKind =
  | "preparation"
  | "production"
  | "dispatch"
  | "delivery"

export type AdminShopFulfilmentAction = {
  kind: AdminShopFulfilmentActionKind
  /** Button and menu text. */
  label: string
  /** Customer label the action records once it succeeds. */
  resultLabel: string
}

const FULFILMENT_ACTIONS: Record<
  AdminShopFulfilmentActionKind,
  Omit<AdminShopFulfilmentAction, "kind">
> = {
  preparation: { label: "Start preparing", resultLabel: "Preparing" },
  production: { label: "Start production", resultLabel: "In production" },
  dispatch: { label: "Mark as Dispatched", resultLabel: "Dispatched" },
  delivery: { label: "Mark as Delivered", resultLabel: "Delivered" },
}

function hasEvidence(value: string | null | undefined): boolean {
  return value != null && value.trim().length > 0
}

/**
 * Carrier evidence for a dispatch: the dispatch time, the carrier or supplier
 * name and reference, and the source and Admin actor that recorded them.
 * Mirrors `ShopOrderDispatchEvidence.IsComplete` on the backend.
 */
export function hasAdminShopDispatchEvidence(
  order: AdminShopFulfilmentEvidence
): boolean {
  return (
    hasEvidence(order.dispatchedAtUtc) &&
    hasEvidence(order.dispatchCarrierName) &&
    hasEvidence(order.dispatchCarrierReference) &&
    hasEvidence(order.dispatchSource) &&
    order.dispatchedByAdminUserId != null
  )
}

/** Stored as Dispatched or Delivered, but the carrier evidence is missing. */
export function isAdminShopDispatchEvidenceMissing(
  order: AdminShopFulfilmentEvidence
): boolean {
  return (
    (order.fulfilmentStatus === "in_transit" ||
      order.fulfilmentStatus === "delivered") &&
    !hasAdminShopDispatchEvidence(order)
  )
}

/**
 * Customer label for a paid order. Each stage needs its own evidence, so the
 * label mirrors the backend: a tracking URL or dispatch time alone never reads
 * "Dispatched", and a delivery time alone never reads "Delivered".
 */
export function adminShopOrderFulfilmentLabel(
  order: AdminShopFulfilmentEvidence
): string {
  if (order.fulfilmentStatus === "cancelled") {
    return "Cancelled"
  }
  const dispatchEvidence = hasAdminShopDispatchEvidence(order)
  if (
    order.fulfilmentStatus === "delivered" &&
    hasEvidence(order.deliveredAtUtc) &&
    dispatchEvidence
  ) {
    return "Delivered"
  }
  if (
    (order.fulfilmentStatus === "in_transit" ||
      order.fulfilmentStatus === "delivered") &&
    dispatchEvidence
  ) {
    return "Dispatched"
  }
  if (hasEvidence(order.productionStartedAtUtc)) {
    return "In production"
  }
  if (hasEvidence(order.preparationStartedAtUtc)) {
    return "Preparing"
  }
  return "Confirmed"
}

/** The one fulfilment action an order can take next, in customer-status order. */
export function nextAdminShopFulfilmentAction(
  order: AdminShopFulfilmentEvidence
): AdminShopFulfilmentAction | null {
  const action = (kind: AdminShopFulfilmentActionKind) => ({
    kind,
    ...FULFILMENT_ACTIONS[kind],
  })

  if (order.fulfilmentStatus === "in_transit") {
    return hasAdminShopDispatchEvidence(order) ? action("delivery") : null
  }
  if (order.fulfilmentStatus !== "processing") {
    return null
  }
  if (hasEvidence(order.productionStartedAtUtc)) {
    return action("dispatch")
  }
  if (!hasEvidence(order.preparationStartedAtUtc)) {
    return action("preparation")
  }
  return action("production")
}

export type AdminShopDispatchFields = {
  carrierName: string
  carrierReference: string
}

/** Dispatch needs a carrier or supplier name and reference. Other actions need no fields. */
export function canSubmitAdminShopFulfilmentAction(
  action: Pick<AdminShopFulfilmentAction, "kind">,
  dispatch: AdminShopDispatchFields
): boolean {
  if (action.kind !== "dispatch") {
    return true
  }
  return (
    dispatch.carrierName.trim().length > 0 &&
    dispatch.carrierReference.trim().length > 0
  )
}

export type AdminShopDispatchEvidenceFields = AdminShopDispatchFields & {
  /** Value of a `datetime-local` input: the actual dispatch time in local time. */
  dispatchedAt: string
}

/**
 * Converts a `datetime-local` value to an ISO UTC string. Returns null when the
 * value is empty or not a date.
 */
export function adminShopDispatchedAtToIso(localValue: string): string | null {
  const trimmed = localValue.trim()
  if (trimmed.length === 0) {
    return null
  }
  const date = new Date(trimmed)
  return Number.isNaN(date.getTime()) ? null : date.toISOString()
}

/** Record evidence needs a carrier name, a reference and the actual dispatch time. */
export function canSubmitAdminShopDispatchEvidence(
  fields: AdminShopDispatchEvidenceFields
): boolean {
  return (
    fields.carrierName.trim().length > 0 &&
    fields.carrierReference.trim().length > 0 &&
    adminShopDispatchedAtToIso(fields.dispatchedAt) !== null
  )
}

/** Correction needs a short reason, up to the backend limit of 500 characters. */
export function canSubmitAdminShopStatusCorrection(reason: string): boolean {
  const trimmed = reason.trim()
  return trimmed.length > 0 && trimmed.length <= 500
}

export function canEditAdminShopTrackingUrl(status: string): boolean {
  return status === "in_transit"
}

export function canEditAdminShopOpsNotes(status: string): boolean {
  return (
    status === "processing" ||
    status === "in_transit" ||
    status === "delivered"
  )
}

export function formatAdminShopGbpFromPence(pence: number): string {
  return new Intl.NumberFormat("en-GB", {
    style: "currency",
    currency: "GBP",
  }).format(pence / 100)
}


export type AdminShopOrderReviewNotice = {
  title: string
  lines: string[]
}

const ADMIN_SHOP_REVIEW_REASON_LABELS: Record<string, string> = {
  qr_capacity_unavailable_after_payment:
    "paid, but the plan had no room for a new QR code.",
}

function formatReviewSince(iso: string): string {
  const date = new Date(iso)
  if (Number.isNaN(date.getTime())) {
    return iso
  }
  return `${date.toISOString().slice(0, 16).replace("T", " ")} UTC`
}

/**
 * Warning for Admin when a paid Shop order needs a manual look: the review
 * reason, or QR slot reservations held in `review`. Null when none.
 */
export function adminShopOrderReviewNotice(
  order: Pick<AdminShopOrderListItem, "reviewReason" | "reviewQrReservations">
): AdminShopOrderReviewNotice | null {
  const reservations = order.reviewQrReservations ?? []
  const reason = order.reviewReason?.trim() || null

  if (!reason && reservations.length === 0) {
    return null
  }

  const title = reason
    ? `Needs review: ${ADMIN_SHOP_REVIEW_REASON_LABELS[reason] ?? reason}`
    : "Needs review: a QR slot has been held for over 24 hours."

  return {
    title,
    lines: reservations.map(
      (row) =>
        `${row.qrType} held since ${formatReviewSince(row.createdAtUtc)} (${
          row.lastReconcileError?.trim() || "no error recorded"
        })`
    ),
  }
}
