/** Offers Filter sheet — Status + Offer type (attach source). No Location / Date MVP. */

import type { FilterSheetSchema, SchemaOption } from "@/lib/operatorFilterSheet"
import type { CatalogOfferStatus } from "@/types/operatorCampaigns"

export type OffersFilterStatusId = CatalogOfferStatus

export type OffersFilterAttachSourceId =
  | "campaign"
  | "recovery"
  | "guest-form-thank-you"
  | "manual"

export const OFFERS_STATUS_LABELS: Record<OffersFilterStatusId, string> = {
  draft: "Draft",
  active: "Active",
  paused: "Paused",
  expired: "Expired",
  archived: "Archived",
}

/** Catalog Offers that may receive a new live attach (Draft or Active). */
export const ATTACHABLE_OFFER_STATUS_IDS: ReadonlyArray<
  Extract<OffersFilterStatusId, "draft" | "active">
> = ["draft", "active"]

export function isAttachableCatalogOfferStatus(
  status: string
): status is Extract<OffersFilterStatusId, "draft" | "active"> {
  return status === "draft" || status === "active"
}

export const OFFERS_ATTACH_SOURCE_LABELS: Record<
  OffersFilterAttachSourceId,
  string
> = {
  campaign: "Campaign",
  recovery: "Recovery",
  "guest-form-thank-you": "Guest form thank-you",
  manual: "Manual",
}

function toOptions<TId extends string>(
  labels: Record<TId, string>
): SchemaOption[] {
  return Object.entries(labels).map(([id, label]) => ({
    id,
    label: label as string,
  }))
}

export function offersFilterSheetSchema(): FilterSheetSchema {
  return {
    fields: [
      {
        id: "status",
        kind: "multi-select",
        label: "Status",
        chipKind: "status",
        options: toOptions(OFFERS_STATUS_LABELS),
      },
      {
        id: "attachSource",
        kind: "multi-select",
        label: "Offer type",
        chipKind: "offerStance",
        options: toOptions(OFFERS_ATTACH_SOURCE_LABELS),
      },
    ],
  }
}
