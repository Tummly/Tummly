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
  claimsEmptyTitle: "No one has claimed this offer yet",
  claimsEmptyHelper:
    "Once guests claim this offer from a feedback form, campaign or manual link, they'll appear here.",
  claimsEmptyCta: "Share offer in a campaign",
  redemptionsEmptyTitle: "No redemptions yet",
  redemptionsEmptyHelper:
    "When guests redeem this offer, staff redemptions will appear here.",
  campaignsLinkedEmptyTitle: "No linked campaigns yet",
  campaignsLinkedEmptyHelper:
    "Campaigns that use this offer will appear here once you attach it.",
  campaignsIssuanceEmptyTitle: "No issuance sources yet",
  campaignsIssuanceEmptyHelper:
    "Attach paths that issued passes for this offer will appear here.",
  voidRequestsEmptyTitle: "No void requests yet",
  voidRequestsEmptyHelper:
    "Void requests for passes on this offer will appear here.",
  claimsRowViewGuest: "View guest profile",
  claimsRowResend: "Resend offer",
  claimsRowCancelClaim: "Cancel claim",
  claimsRowCopyCode: "Copy code",
  claimsResendConfirmTitle: "Resend this offer?",
  claimsResendConfirmDescription:
    "Sends the offer claim again to this guest. Live resend is not available yet.",
  claimsCancelConfirmTitle: "Cancel this claim?",
  claimsCancelConfirmDescription:
    "Cancels the open pass and invalidates the claim code. Live cancel is not available yet.",
  redemptionsRowViewRedemption: "View redemption",
  redemptionsRowViewPass: "View pass",
  redemptionsRowViewGuest: "View guest",
  redemptionsRowViewIssuedTerms: "View issued terms",
  redemptionsRowRequestVoid: "Request void",
  voidRequestsRowReview: "Review",
  campaignsSubTabLinked: "Linked campaigns",
  campaignsSubTabIssuance: "Issuance sources",
  metricUnavailable: "—",
  staffVerificationRequired: "Required",
  rename: "Rename",
  duplicate: "Duplicate",
  duplicateAsNewDraft: "Duplicate as new Draft",
  pauseIssuance: "Pause issuance",
  resumeIssuance: "Resume issuance",
  archiveOffer: "Archive offer",
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
  kpiClaimsHelper: "Claims in selected window",
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
  actionId: Exclude<OfferDetailsHeaderActionId, "rename">
): { title: string; description: string } {
  switch (actionId) {
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
    return "0%"
  }
  return `${Math.round((redemptions / claims) * 100)}%`
}

/** Catalog benefit fact for Overview definition — not the offer title. */
export function formatOfferDetailsOfferValue(offer: CatalogOfferDetail): string {
  const type = offer.offerType
  if (type === "percentage_discount" && offer.discountPercentage != null) {
    return `${offer.discountPercentage}% off`
  }
  if (type === "fixed_discount" && offer.discountAmount != null) {
    return `£${offer.discountAmount} off`
  }
  if (type === "free_item") {
    const text = offer.freeItemText?.trim()
    return text && text.length > 0 ? text : OFFER_DETAILS_COPY.metricUnavailable
  }
  if (type === "replacement_item") {
    const text = offer.replacementItemText?.trim()
    return text && text.length > 0 ? text : OFFER_DETAILS_COPY.metricUnavailable
  }
  return OFFER_DETAILS_COPY.metricUnavailable
}

export function formatOfferDetailsStaffVerification(
  offer: CatalogOfferDetail
): string {
  const text = offer.staffInstructions?.trim()
  if (text == null || text.length === 0) {
    return OFFER_DETAILS_COPY.metricUnavailable
  }
  return OFFER_DETAILS_COPY.staffVerificationRequired
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
      value: formatOfferDetailsOfferValue(offer),
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
      value: formatOfferDetailsStaffVerification(offer),
    },
    {
      label: OFFER_DETAILS_COPY.fieldRedemptionMethod,
      value: dash,
    },
    {
      label: OFFER_DETAILS_COPY.fieldManagerOverride,
      value: dash,
    },
    {
      label: OFFER_DETAILS_COPY.fieldUsage,
      value: dash,
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

export type OfferDetailsLifecycleEmptyKind =
  | "claims"
  | "redemptions"
  | "campaigns-linked"
  | "campaigns-issuance"
  | "void-requests"

export type OfferDetailsLifecycleEmptyState = {
  title: string
  helper: string
  primaryCtaLabel: string | null
}

/** Honest-empty chrome per lifecycle tab — Claims SoT Figma 3527:54811. */
export function buildOfferDetailsLifecycleEmptyState(
  kind: OfferDetailsLifecycleEmptyKind
): OfferDetailsLifecycleEmptyState {
  switch (kind) {
    case "claims":
      return {
        title: OFFER_DETAILS_COPY.claimsEmptyTitle,
        helper: OFFER_DETAILS_COPY.claimsEmptyHelper,
        primaryCtaLabel: OFFER_DETAILS_COPY.claimsEmptyCta,
      }
    case "redemptions":
      return {
        title: OFFER_DETAILS_COPY.redemptionsEmptyTitle,
        helper: OFFER_DETAILS_COPY.redemptionsEmptyHelper,
        primaryCtaLabel: null,
      }
    case "campaigns-linked":
      return {
        title: OFFER_DETAILS_COPY.campaignsLinkedEmptyTitle,
        helper: OFFER_DETAILS_COPY.campaignsLinkedEmptyHelper,
        primaryCtaLabel: null,
      }
    case "campaigns-issuance":
      return {
        title: OFFER_DETAILS_COPY.campaignsIssuanceEmptyTitle,
        helper: OFFER_DETAILS_COPY.campaignsIssuanceEmptyHelper,
        primaryCtaLabel: null,
      }
    case "void-requests":
      return {
        title: OFFER_DETAILS_COPY.voidRequestsEmptyTitle,
        helper: OFFER_DETAILS_COPY.voidRequestsEmptyHelper,
        primaryCtaLabel: null,
      }
  }
}

export const OFFER_DETAILS_CLAIMS_COLUMN_LABELS = {
  guest: "Guest",
  claimCode: "Claim code",
  claimed: "Claimed",
  source: "Source",
  location: "Location",
  expiry: "Expiry",
  status: "Status",
  actions: "Actions",
} as const

/** Details Redemptions — no Override (MVP), no Offer (single-offer page). */
export const OFFER_DETAILS_REDEMPTIONS_COLUMN_LABELS = {
  dateTime: "Date/time",
  guest: "Guest",
  passReference: "Pass reference",
  location: "Location",
  staffMember: "Staff member",
  outcome: "Outcome",
  reason: "Reason",
  offerVersion: "Offer version",
  actions: "Actions",
} as const

export const OFFER_DETAILS_CAMPAIGNS_LINKED_COLUMN_LABELS = {
  campaign: "Campaign",
  status: "Status",
  location: "Location",
  channel: "Channel",
  audience: "Audience",
  offerVersion: "Offer version",
  passesIssued: "Passes issued",
  claims: "Claims",
  redemptions: "Redemptions",
  sendDate: "Send date",
  actions: "Actions",
} as const

export const OFFER_DETAILS_ISSUANCE_SOURCES_COLUMN_LABELS = {
  source: "Source",
  path: "Path",
  passesIssued: "Passes issued",
  lastIssued: "Last issued",
  actions: "Actions",
} as const

export const OFFER_DETAILS_VOID_REQUESTS_COLUMN_LABELS = {
  dateTime: "Date/time",
  requestedBy: "Requested by",
  guest: "Guest",
  offerPass: "Offer pass",
  reason: "Reason",
  location: "Location",
  currentState: "Current state",
  requestedCorrection: "Requested correction",
  status: "Status",
  actions: "Actions",
} as const

export const OFFER_DETAILS_CAMPAIGNS_SUB_TAB_IDS = [
  "linked",
  "issuance-sources",
] as const

export type OfferDetailsCampaignsSubTabId =
  (typeof OFFER_DETAILS_CAMPAIGNS_SUB_TAB_IDS)[number]

export const OFFER_DETAILS_CAMPAIGNS_SUB_TAB_LABELS: Record<
  OfferDetailsCampaignsSubTabId,
  string
> = {
  linked: OFFER_DETAILS_COPY.campaignsSubTabLinked,
  "issuance-sources": OFFER_DETAILS_COPY.campaignsSubTabIssuance,
}

export type OfferDetailsClaimsRowActionId =
  | "view-guest-profile"
  | "resend-offer"
  | "cancel-claim"
  | "copy-code"

export type OfferDetailsRedemptionsRowActionId =
  | "view-redemption"
  | "view-pass"
  | "view-guest"
  | "view-issued-terms"
  | "request-void"

export type OfferDetailsVoidRequestsRowActionId = "review"

export type OfferDetailsLifecycleRowAction<TId extends string> = {
  id: TId
  label: string
  gated: boolean
}

export function buildOfferDetailsClaimsRowActions(): OfferDetailsLifecycleRowAction<OfferDetailsClaimsRowActionId>[] {
  return [
    {
      id: "view-guest-profile",
      label: OFFER_DETAILS_COPY.claimsRowViewGuest,
      gated: false,
    },
    {
      id: "resend-offer",
      label: OFFER_DETAILS_COPY.claimsRowResend,
      gated: true,
    },
    {
      id: "cancel-claim",
      label: OFFER_DETAILS_COPY.claimsRowCancelClaim,
      gated: true,
    },
    {
      id: "copy-code",
      label: OFFER_DETAILS_COPY.claimsRowCopyCode,
      gated: false,
    },
  ]
}

/** Redemptions ⋮ — Export omitted (ticket 16). Write/nav gated until routes/APIs. */
export function buildOfferDetailsRedemptionsRowActions(): OfferDetailsLifecycleRowAction<OfferDetailsRedemptionsRowActionId>[] {
  return [
    {
      id: "view-redemption",
      label: OFFER_DETAILS_COPY.redemptionsRowViewRedemption,
      gated: true,
    },
    {
      id: "view-pass",
      label: OFFER_DETAILS_COPY.redemptionsRowViewPass,
      gated: true,
    },
    {
      id: "view-guest",
      label: OFFER_DETAILS_COPY.redemptionsRowViewGuest,
      gated: true,
    },
    {
      id: "view-issued-terms",
      label: OFFER_DETAILS_COPY.redemptionsRowViewIssuedTerms,
      gated: true,
    },
    {
      id: "request-void",
      label: OFFER_DETAILS_COPY.redemptionsRowRequestVoid,
      gated: true,
    },
  ]
}

export function buildOfferDetailsVoidRequestsRowActions(): OfferDetailsLifecycleRowAction<OfferDetailsVoidRequestsRowActionId>[] {
  return [
    {
      id: "review",
      label: OFFER_DETAILS_COPY.voidRequestsRowReview,
      gated: true,
    },
  ]
}

export function offerDetailsClaimsRowActionConfirmCopy(
  actionId: "resend-offer" | "cancel-claim"
): { title: string; description: string } {
  if (actionId === "resend-offer") {
    return {
      title: OFFER_DETAILS_COPY.claimsResendConfirmTitle,
      description: OFFER_DETAILS_COPY.claimsResendConfirmDescription,
    }
  }
  return {
    title: OFFER_DETAILS_COPY.claimsCancelConfirmTitle,
    description: OFFER_DETAILS_COPY.claimsCancelConfirmDescription,
  }
}
