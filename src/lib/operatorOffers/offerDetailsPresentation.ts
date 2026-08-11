/**
 * Offer Details presentation — Overview copy, KPI labels, header ⋮ matrix (ticket 10/16/23).
 */

import { format } from "date-fns"

import {
  formatOfferValidityLabel,
} from "@/lib/operatorOffers/offerListPresentation"
import { OFFERS_STATUS_LABELS } from "@/lib/operatorOffers/offersFilterSheetSchema"
import type { CatalogOfferDetail, CatalogOfferStatus } from "@/types/operatorCampaigns"

export const OFFER_DETAILS_COPY = {
  breadcrumbOffers: "Offers",
  editOffer: "Edit offer",
  openStaffRedeem: "Open staff redeem",
  moreActionsAriaLabel: "More offer actions",
  loadError: "Could not load this offer.",
  retry: "Retry",
  confirmAction: "Confirm",
  cancelAction: "Cancel",
  definitionTitle: "Claims and redemptions over time",
  dateRangeControlTitle: "Overview date range",
  recommendedTitle: "Recommended next step",
  recommendedSubtitle:
    "AI-assisted guidance based on your recent guest activity.",
  recommendedEmptyTitle: "No recommendation yet",
  recommendedEmptyHelper:
    "Recommendations will appear here when there is enough recent guest activity for this offer.",
  claimsEmptyPlaceholder: "No claims to show yet.",
  redemptionsEmptyPlaceholder: "No redemptions to show yet.",
  campaignsEmptyPlaceholder: "No linked campaigns or issuance sources yet.",
  voidRequestsEmptyPlaceholder: "No void requests yet.",
  metricUnavailable: "—",
  redemptionMethod: "Unique code",
  usageSingleUse: "Single-use",
  rename: "Rename",
  duplicate: "Duplicate",
  duplicateAsNewDraft: "Duplicate as new Draft",
  pauseIssuance: "Pause issuance",
  resumeIssuance: "Resume issuance",
  archiveOffer: "Archive offer",
  renameConfirmTitle: "Rename this offer?",
  renameConfirmDescription: "You will be able to edit the offer title.",
  pauseConfirmTitle: "Pause this offer?",
  pauseConfirmDescription:
    "Guests will not be able to claim this offer until you resume it.",
  resumeConfirmTitle: "Resume this offer?",
  resumeConfirmDescription:
    "This offer will be available to claim again where it is attached.",
  archiveConfirmTitle: "Archive this offer?",
  archiveConfirmDescription:
    "Archived offers leave the active catalog. You can still view them later.",
  duplicateConfirmTitle: "Duplicate this offer?",
  duplicateConfirmDescription: "Creates a new Draft copy of this offer.",
  metaSource: "Source",
  metaLocations: "Locations",
  metaCreatedBy: "Created by",
  metaCreated: "Created",
  fieldOfferValue: "Offer value",
  fieldValidLocations: "Valid locations",
  fieldRedemptionMethod: "Redemption method",
  fieldUsage: "Usage",
  fieldExpiry: "Expiry",
  fieldStaffVerification: "Staff verification",
  fieldManagerOverride: "Manager override",
  fieldAbuseMonitoring: "Abuse monitoring",
  kpiClaimsHelper: "[X]% vs previous period",
  kpiRedemptionsHelper: "Total successful redemptions",
  kpiRedemptionRateHelper: "Redeemed ÷ claimed",
  kpiExpiredUnusedHelper: "Claimed but expired",
  kpiFailedAttemptsHelper: "Invalid, expired or already-used attempts",
} as const

export const OFFER_DETAILS_TAB_IDS = [
  "overview",
  "claims",
  "redemptions",
  "campaigns",
  "void-requests",
] as const

export type OfferDetailsTabId = (typeof OFFER_DETAILS_TAB_IDS)[number]

export const OFFER_DETAILS_TAB_LABELS: Record<OfferDetailsTabId, string> = {
  overview: "Overview",
  claims: "Claims",
  redemptions: "Redemptions",
  campaigns: "Campaigns",
  "void-requests": "Void requests",
}

export const OFFER_DETAILS_DATE_PRESET_IDS = [
  "last7",
  "last30",
  "last90",
] as const

export type OfferDetailsDatePresetId =
  (typeof OFFER_DETAILS_DATE_PRESET_IDS)[number]

export type OfferDetailsDateRange =
  | { kind: "preset"; presetId: OfferDetailsDatePresetId }
  | {
      kind: "custom"
      startDate: string
      endDate: string
    }

export const DEFAULT_OFFER_DETAILS_DATE_RANGE: OfferDetailsDateRange = {
  kind: "preset",
  presetId: "last7",
}

const DATE_PRESET_LABELS: Record<OfferDetailsDatePresetId, string> = {
  last7: "Last 7 days",
  last30: "Last 30 days",
  last90: "Last 90 days",
}

export function labelForOfferDetailsDateRange(
  range: OfferDetailsDateRange
): string {
  if (range.kind === "preset") {
    return DATE_PRESET_LABELS[range.presetId]
  }
  if (range.startDate === range.endDate) {
    return format(parseOfferDetailsLocalDateKey(range.startDate), "d MMM yyyy")
  }
  const start = parseOfferDetailsLocalDateKey(range.startDate)
  const end = parseOfferDetailsLocalDateKey(range.endDate)
  return `${format(start, "d")}–${format(end, "d MMM yyyy")}`
}

export function offerDetailsDatePresetOptions(): ReadonlyArray<{
  presetId: OfferDetailsDatePresetId
  label: string
}> {
  return OFFER_DETAILS_DATE_PRESET_IDS.map((presetId) => ({
    presetId,
    label: DATE_PRESET_LABELS[presetId],
  }))
}

export function parseOfferDetailsLocalDateKey(dateKey: string): Date {
  const [year, month, day] = dateKey.split("-").map(Number)
  return new Date(year, month - 1, day)
}

export type OfferDetailsHeaderActionId =
  | "rename"
  | "duplicate"
  | "pause-issuance"
  | "resume-issuance"
  | "archive-offer"

export type OfferDetailsHeaderMenuItem = {
  id: OfferDetailsHeaderActionId
  label: string
}

/**
 * Header ⋮ visibility for first build (ticket 10 matrix + 16 gap locks):
 * hide navigate-only routes and Delete draft until APIs/routes exist;
 * Cancel offer → Archive offer.
 */
export function buildOfferDetailsHeaderMenuItems(
  status: CatalogOfferStatus
): OfferDetailsHeaderMenuItem[] {
  const rename: OfferDetailsHeaderMenuItem = {
    id: "rename",
    label: OFFER_DETAILS_COPY.rename,
  }
  const duplicate: OfferDetailsHeaderMenuItem = {
    id: "duplicate",
    label: OFFER_DETAILS_COPY.duplicate,
  }
  const duplicateAsNewDraft: OfferDetailsHeaderMenuItem = {
    id: "duplicate",
    label: OFFER_DETAILS_COPY.duplicateAsNewDraft,
  }
  const pauseIssuance: OfferDetailsHeaderMenuItem = {
    id: "pause-issuance",
    label: OFFER_DETAILS_COPY.pauseIssuance,
  }
  const resumeIssuance: OfferDetailsHeaderMenuItem = {
    id: "resume-issuance",
    label: OFFER_DETAILS_COPY.resumeIssuance,
  }
  const archiveOffer: OfferDetailsHeaderMenuItem = {
    id: "archive-offer",
    label: OFFER_DETAILS_COPY.archiveOffer,
  }

  switch (status) {
    case "draft":
      return [rename, duplicate]
    case "active":
      return [pauseIssuance, duplicate, archiveOffer]
    case "paused":
      return [resumeIssuance, archiveOffer, duplicate]
    case "expired":
      return [duplicateAsNewDraft, archiveOffer]
    case "archived":
      return [duplicateAsNewDraft]
    default:
      return []
  }
}

export function offerDetailsHeaderActionConfirmCopy(
  actionId: OfferDetailsHeaderActionId
): { title: string; description: string } {
  switch (actionId) {
    case "rename":
      return {
        title: OFFER_DETAILS_COPY.renameConfirmTitle,
        description: OFFER_DETAILS_COPY.renameConfirmDescription,
      }
    case "pause-issuance":
      return {
        title: OFFER_DETAILS_COPY.pauseConfirmTitle,
        description: OFFER_DETAILS_COPY.pauseConfirmDescription,
      }
    case "resume-issuance":
      return {
        title: OFFER_DETAILS_COPY.resumeConfirmTitle,
        description: OFFER_DETAILS_COPY.resumeConfirmDescription,
      }
    case "archive-offer":
      return {
        title: OFFER_DETAILS_COPY.archiveConfirmTitle,
        description: OFFER_DETAILS_COPY.archiveConfirmDescription,
      }
    case "duplicate":
      return {
        title: OFFER_DETAILS_COPY.duplicateConfirmTitle,
        description: OFFER_DETAILS_COPY.duplicateConfirmDescription,
      }
  }
}

export type OfferDetailsKpiId =
  | "claims"
  | "redemptions"
  | "redemption-rate"
  | "expired-unused"
  | "failed-attempts"

export type OfferDetailsKpi = {
  id: OfferDetailsKpiId
  label: string
  primaryText: string
  helperText: string
}

export type OfferDetailsOverviewMetrics = {
  claims: number
  redemptions: number
  expiredUnused: number
  failedAttempts: number
}

export function formatOfferDetailsRedemptionRate(
  claims: number,
  redemptions: number
): string {
  if (claims <= 0) {
    return "0"
  }
  return String(Math.round((redemptions / claims) * 100))
}

/** Overview KPI strip — zeros / honest empty until metrics wiring. */
export function buildOfferDetailsOverviewKpis(
  metrics: OfferDetailsOverviewMetrics = {
    claims: 0,
    redemptions: 0,
    expiredUnused: 0,
    failedAttempts: 0,
  }
): OfferDetailsKpi[] {
  return [
    {
      id: "claims",
      label: "Claims",
      primaryText: String(metrics.claims),
      helperText: OFFER_DETAILS_COPY.kpiClaimsHelper,
    },
    {
      id: "redemptions",
      label: "Redemptions",
      primaryText: String(metrics.redemptions),
      helperText: OFFER_DETAILS_COPY.kpiRedemptionsHelper,
    },
    {
      id: "redemption-rate",
      label: "Redemption rate",
      primaryText: formatOfferDetailsRedemptionRate(
        metrics.claims,
        metrics.redemptions
      ),
      helperText: OFFER_DETAILS_COPY.kpiRedemptionRateHelper,
    },
    {
      id: "expired-unused",
      label: "Expired unused",
      primaryText: String(metrics.expiredUnused),
      helperText: OFFER_DETAILS_COPY.kpiExpiredUnusedHelper,
    },
    {
      id: "failed-attempts",
      label: "Failed attempts",
      primaryText: String(metrics.failedAttempts),
      helperText: OFFER_DETAILS_COPY.kpiFailedAttemptsHelper,
    },
  ]
}

export type OfferDetailsLabeledValue = {
  label: string
  value: string
}

export function formatOfferDetailsCreatedLabel(createdAt: string): string {
  const parsed = new Date(createdAt)
  if (Number.isNaN(parsed.getTime())) {
    return OFFER_DETAILS_COPY.metricUnavailable
  }
  return format(parsed, "d MMM yyyy")
}

export function buildOfferDetailsMetaRows(input: {
  locationName: string
  createdAt: string
  sourceLabel?: string | null
  createdByLabel?: string | null
}): OfferDetailsLabeledValue[] {
  return [
    {
      label: OFFER_DETAILS_COPY.metaSource,
      value:
        input.sourceLabel?.trim()
        || OFFER_DETAILS_COPY.metricUnavailable,
    },
    {
      label: OFFER_DETAILS_COPY.metaLocations,
      value: input.locationName,
    },
    {
      label: OFFER_DETAILS_COPY.metaCreatedBy,
      value:
        input.createdByLabel?.trim()
        || OFFER_DETAILS_COPY.metricUnavailable,
    },
    {
      label: OFFER_DETAILS_COPY.metaCreated,
      value: formatOfferDetailsCreatedLabel(input.createdAt),
    },
  ]
}

/** Definition fields in Figma two-column order (left then right, paired). */
export function buildOfferDetailsDefinitionFields(input: {
  offer: CatalogOfferDetail
  locationName: string
}): OfferDetailsLabeledValue[] {
  const { offer, locationName } = input
  const dash = OFFER_DETAILS_COPY.metricUnavailable
  return [
    {
      label: OFFER_DETAILS_COPY.fieldOfferValue,
      value: offer.title,
    },
    {
      label: OFFER_DETAILS_COPY.fieldExpiry,
      value: formatOfferValidityLabel(offer.validity, offer.expiryDate),
    },
    {
      label: OFFER_DETAILS_COPY.fieldValidLocations,
      value: locationName,
    },
    {
      label: OFFER_DETAILS_COPY.fieldStaffVerification,
      value: dash,
    },
    {
      label: OFFER_DETAILS_COPY.fieldRedemptionMethod,
      value: OFFER_DETAILS_COPY.redemptionMethod,
    },
    {
      label: OFFER_DETAILS_COPY.fieldManagerOverride,
      value: dash,
    },
    {
      label: OFFER_DETAILS_COPY.fieldUsage,
      value: OFFER_DETAILS_COPY.usageSingleUse,
    },
    {
      label: OFFER_DETAILS_COPY.fieldAbuseMonitoring,
      value: dash,
    },
  ]
}

export function offerDetailsStatusLabel(status: CatalogOfferStatus): string {
  return OFFERS_STATUS_LABELS[status] ?? status
}

export function tabEmptyPlaceholderCopy(tabId: OfferDetailsTabId): string {
  switch (tabId) {
    case "claims":
      return OFFER_DETAILS_COPY.claimsEmptyPlaceholder
    case "redemptions":
      return OFFER_DETAILS_COPY.redemptionsEmptyPlaceholder
    case "campaigns":
      return OFFER_DETAILS_COPY.campaignsEmptyPlaceholder
    case "void-requests":
      return OFFER_DETAILS_COPY.voidRequestsEmptyPlaceholder
    case "overview":
      return ""
  }
}
