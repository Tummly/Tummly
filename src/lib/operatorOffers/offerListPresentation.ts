/**
 * Offers list table — Figma Main Offers projection (ticket 20 / 3498:1587).
 */

import {
  OFFERS_ATTACH_SOURCE_LABELS,
  OFFERS_STATUS_LABELS,
  type OffersFilterAttachSourceId,
} from "@/lib/operatorOffers/offersFilterSheetSchema"
import type {
  CatalogOfferStatus,
  CatalogOffersListItem,
} from "@/types/operatorCampaigns"

export const OFFERS_LIST_TABLE_COPY = {
  offerColumn: "Offer",
  statusColumn: "Status",
  validityColumn: "Validity",
  claimsColumn: "Claims",
  redeemedColumn: "Redeemed",
  redemptionRateColumn: "Redemption rate",
  controlsColumn: "Controls",
  actionsColumn: "Actions",
  view: "View",
  edit: "Edit",
  pause: "Pause",
  resume: "Resume",
  duplicate: "Duplicate",
  archive: "Archive",
  metricDash: "—",
  uniqueCode: "Unique code",
} as const

export type OfferRowActionId =
  | "view"
  | "edit"
  | "pause"
  | "resume"
  | "duplicate"
  | "archive"

export type OfferRowAction = {
  id: OfferRowActionId
  label: string
}

/** Slim ⋮ actions by Offer status (ticket 14 / 20). */
export function buildOfferRowActions(status: CatalogOfferStatus): OfferRowAction[] {
  const view: OfferRowAction = {
    id: "view",
    label: OFFERS_LIST_TABLE_COPY.view,
  }
  const edit: OfferRowAction = {
    id: "edit",
    label: OFFERS_LIST_TABLE_COPY.edit,
  }
  const pause: OfferRowAction = {
    id: "pause",
    label: OFFERS_LIST_TABLE_COPY.pause,
  }
  const resume: OfferRowAction = {
    id: "resume",
    label: OFFERS_LIST_TABLE_COPY.resume,
  }
  const duplicate: OfferRowAction = {
    id: "duplicate",
    label: OFFERS_LIST_TABLE_COPY.duplicate,
  }
  const archive: OfferRowAction = {
    id: "archive",
    label: OFFERS_LIST_TABLE_COPY.archive,
  }

  switch (status) {
    case "draft":
      return [view, edit, duplicate, archive]
    case "active":
      return [view, edit, pause, duplicate, archive]
    case "paused":
      return [view, edit, resume, duplicate, archive]
    case "expired":
      return [view, duplicate, archive]
    case "archived":
      return [view, duplicate]
    default:
      return [view]
  }
}

export type OperatorOffersListTableRow = {
  id: number
  title: string
  /** Live attach kinds subline — comma-joined human labels. */
  attachSubline: string | null
  status: CatalogOfferStatus
  statusLabel: string
  validityLabel: string
  claimsLabel: string
  redeemedLabel: string
  redemptionRateLabel: string
  /** Claim-code model · validity summary — use-rule omitted until list DTO has it. */
  controlsLabel: string
}

const VALIDITY_COLUMN_LABELS: Record<string, string> = {
  "7_days_after_issue": "7 days after issue",
  "14_days_after_issue": "14 days after issue",
  "30_days_after_issue": "30 days after issue",
  choose_expiry_date: "Fixed end date",
}

const VALIDITY_CONTROLS_LABELS: Record<string, string> = {
  "7_days_after_issue": "7-day expiry",
  "14_days_after_issue": "14-day expiry",
  "30_days_after_issue": "30-day expiry",
}

function labelForAttachKind(kind: string): string {
  if (kind in OFFERS_ATTACH_SOURCE_LABELS) {
    return OFFERS_ATTACH_SOURCE_LABELS[kind as OffersFilterAttachSourceId]
  }
  return kind
}

export function formatOfferAttachSubline(
  attachKinds: readonly string[]
): string | null {
  if (attachKinds.length === 0) {
    return null
  }
  return attachKinds.map(labelForAttachKind).join(", ")
}

export function formatOfferValidityLabel(
  validity: string,
  expiryDate: string | null
): string {
  if (validity === "choose_expiry_date") {
    if (expiryDate != null && expiryDate.trim().length > 0) {
      return expiryDate
    }
    return VALIDITY_COLUMN_LABELS.choose_expiry_date
  }
  return VALIDITY_COLUMN_LABELS[validity] ?? validity
}

export function formatOfferControlsLabel(
  validity: string,
  expiryDate: string | null
): string {
  const parts: string[] = [OFFERS_LIST_TABLE_COPY.uniqueCode]
  if (validity === "choose_expiry_date") {
    if (expiryDate != null && expiryDate.trim().length > 0) {
      parts.push(`Ends ${expiryDate}`)
    } else {
      parts.push("Fixed end")
    }
  } else {
    parts.push(
      VALIDITY_CONTROLS_LABELS[validity]
        ?? formatOfferValidityLabel(validity, expiryDate)
    )
  }
  return parts.join(" · ")
}

export function formatOfferRedemptionRate(
  lifetimeClaims: number,
  lifetimeRedeemed: number
): string {
  if (lifetimeClaims <= 0) {
    return OFFERS_LIST_TABLE_COPY.metricDash
  }
  const rate = Math.round((lifetimeRedeemed / lifetimeClaims) * 100)
  return `${rate}%`
}

export function mapCatalogOfferListItemToTableRow(
  item: CatalogOffersListItem
): OperatorOffersListTableRow {
  const claims = item.lifetimeClaims ?? 0
  const redeemed = item.lifetimeRedeemed ?? 0

  return {
    id: item.id,
    title: item.title,
    attachSubline: formatOfferAttachSubline(item.attachKinds),
    status: item.status,
    statusLabel: OFFERS_STATUS_LABELS[item.status] ?? item.status,
    validityLabel: formatOfferValidityLabel(item.validity, item.expiryDate),
    claimsLabel: String(claims),
    redeemedLabel: String(redeemed),
    redemptionRateLabel: formatOfferRedemptionRate(claims, redeemed),
    controlsLabel: formatOfferControlsLabel(item.validity, item.expiryDate),
  }
}
