import { formatRelativeTime } from "@/lib/operatorHome/relativeTime"
import type {
  LocationDetailOfferCard,
  LocationDetailOverviewMetricId,
  LocationDetailQrRow,
} from "@/lib/operatorLocations/locationDetailPresentation"
import type { LocationLifecycleStatus } from "@/lib/operatorLocations/locationsPresentation"
import type { LocationSetupStatus } from "@/lib/operatorLocations/locationsPresentation"
import type {
  LocationGuestActivityChecklistItemId,
  LocationGuestActivityChecklistStatusId,
  LocationDetailLatestFeedbackRow,
  LocationSetupChecklistItemId,
  LocationSetupChecklistStatusId,
} from "@/lib/operatorLocations/locationDetailPresentation"
import type { OperatorDashboardMode } from "@/lib/operatorHome/operatorDashboardPaths"
import {
  operatorDashboardCampaignDetailsPath,
  operatorDashboardCampaignPreviewPath,
  operatorDashboardOfferDetailsPath,
} from "@/lib/operatorHome/operatorDashboardPaths"

export type LocationDetailApiHeader = {
  id: number
  name: string
  city: string | null
  lifecycleStatus: LocationLifecycleStatus
  setupStatus: LocationSetupStatus
  managerName: string | null
  managerUserId: number | null
  address: string
  postcode: string | null
  locationPhone: string | null
  localContact: string | null
  liveQrCount: number
  guestsCapturedThisMonth: number
}

export type LocationDetailApiOverviewMetrics = {
  qrScans: number
  formStarts: number
  feedback: number
  guestsCaptured: number
  optIns: number
  offersClaimed: number
  offersRedeemed: number
}

export type LocationDetailApiQrRow = {
  qrCodeId: number
  name: string
  placement: string
  statusLabel: string
  scans: number
  starts: number
  submissions: number
  optIns: number
  claims: number
  lastScanAtUtc: string | null
}

export type LocationDetailApiOfferCard = {
  entityId: number
  kind: "offer" | "campaign"
  statusLabel: string
  title: string
  meta: string
  primaryCta: string
  secondaryCta: string
}

export type LocationDetailApiLatestFeedbackRow = {
  feedbackId: number
  comment: string
  guestName: string
  sentiment: "positive" | "neutral" | "negative" | null
  timeLabel: string
  canStartRecovery: boolean
  locationGuestId: number | null
}

export type LocationDetailApiResponse = {
  success: boolean
  header: LocationDetailApiHeader
  setupChecklist: Record<string, LocationSetupChecklistStatusId>
  overviewMetrics: LocationDetailApiOverviewMetrics
  qrRows: LocationDetailApiQrRow[]
  offerCards: LocationDetailApiOfferCard[]
  guestActivityChecklist: Record<string, LocationGuestActivityChecklistStatusId>
  latestFeedbackRows: LocationDetailApiLatestFeedbackRow[]
}

export function mapLocationDetailSetupChecklist(
  wire: Record<string, LocationSetupChecklistStatusId>
): Record<LocationSetupChecklistItemId, LocationSetupChecklistStatusId> {
  return {
    locationDetailsAdded: wire.locationDetailsAdded ?? "not-started",
    qrCodePublishedLive: wire.qrCodePublishedLive ?? "not-started",
    guestFormConnected: wire.guestFormConnected ?? "not-started",
    teamAccessAssigned: wire.teamAccessAssigned ?? "optional",
    guestPrivacyNotice: wire.guestPrivacyNotice ?? "not-started",
    firstOfferCreated: wire.firstOfferCreated ?? "optional",
    atLeastOneQrCreated: wire.atLeastOneQrCreated ?? "not-started",
  }
}

export function mapLocationDetailOverviewMetrics(
  wire: LocationDetailApiOverviewMetrics
): Record<LocationDetailOverviewMetricId, number> {
  return {
    qrScans: wire.qrScans,
    formStarts: wire.formStarts,
    feedback: wire.feedback,
    guestsCaptured: wire.guestsCaptured,
    optIns: wire.optIns,
    offersClaimed: wire.offersClaimed,
    offersRedeemed: wire.offersRedeemed,
  }
}

function formatQrMetricCount(value: number, suffix: string): string {
  return `${value.toLocaleString("en-GB")} ${suffix}`
}

export function mapLocationDetailQrRows(
  wire: readonly LocationDetailApiQrRow[],
  nowMs: number = Date.now()
): LocationDetailQrRow[] {
  return wire.map((row) => ({
    id: String(row.qrCodeId),
    name: row.name,
    placement: row.placement,
    statusLabel: row.statusLabel,
    scans: formatQrMetricCount(row.scans, "opens"),
    starts: formatQrMetricCount(row.starts, "starts"),
    submissions: formatQrMetricCount(row.submissions, "submissions"),
    optIns: formatQrMetricCount(row.optIns, "opt-ins"),
    claims: formatQrMetricCount(row.claims, "claims"),
    lastScannedLabel:
      row.lastScanAtUtc == null || row.lastScanAtUtc === ""
        ? "—"
        : formatRelativeTime(row.lastScanAtUtc, nowMs) || "—",
  }))
}

export function mapLocationDetailOfferCards(
  wire: readonly LocationDetailApiOfferCard[],
  options: {
    mode: OperatorDashboardMode
    locationId: number
  }
): LocationDetailOfferCard[] {
  return wire.map((card) => {
    if (card.kind === "campaign") {
      return {
        id: `campaign-${card.entityId}`,
        kind: "campaign",
        statusLabel: card.statusLabel,
        title: card.title,
        meta: card.meta,
        primaryCta: card.primaryCta,
        secondaryCta: card.secondaryCta,
        hrefPrimary: operatorDashboardCampaignDetailsPath(
          options.mode,
          card.entityId,
          options.locationId
        ),
        hrefSecondary: operatorDashboardCampaignPreviewPath(
          options.mode,
          card.entityId,
          options.locationId
        ),
      }
    }

    return {
      id: `offer-${card.entityId}`,
      kind: "offer",
      statusLabel: card.statusLabel,
      title: card.title,
      meta: card.meta,
      primaryCta: card.primaryCta,
      secondaryCta: card.secondaryCta,
      hrefPrimary: operatorDashboardOfferDetailsPath(
        options.mode,
        card.entityId,
        options.locationId
      ),
      hrefSecondary: operatorDashboardOfferDetailsPath(
        options.mode,
        card.entityId,
        options.locationId,
        { tab: "redemptions" }
      ),
    }
  })
}

export function mapLocationDetailGuestActivityChecklist(
  wire: Record<string, LocationGuestActivityChecklistStatusId>
): Record<
  LocationGuestActivityChecklistItemId,
  LocationGuestActivityChecklistStatusId
> {
  return {
    guestProfilesCreated: wire.guestProfilesCreated ?? "optional",
    offerClaims: wire.offerClaims ?? "optional",
    consentOptIns: wire.consentOptIns ?? "optional",
    offerRedemptions: wire.offerRedemptions ?? "optional",
    feedbackSubmitted: wire.feedbackSubmitted ?? "optional",
    unsubscribes: wire.unsubscribes ?? "optional",
    needsRecovery: wire.needsRecovery ?? "complete",
  }
}

export function mapLocationDetailLatestFeedbackRows(
  rows: LocationDetailApiLatestFeedbackRow[]
): LocationDetailLatestFeedbackRow[] {
  return rows.map((row) => ({
    id: String(row.feedbackId),
    feedbackId: row.feedbackId,
    comment: row.comment,
    guestName: row.guestName,
    sentiment: row.sentiment,
    timeLabel: row.timeLabel,
    canStartRecovery: row.canStartRecovery,
    locationGuestId: row.locationGuestId,
  }))
}

/** Feedback inbox deep link for Start recovery (PRD location detail). */
export function locationDetailRecoveryFeedbackPath(
  feedbackPath: string,
  feedbackId: number
): string {
  const separator = feedbackPath.includes("?") ? "&" : "?"
  return `${feedbackPath}${separator}feedbackId=${feedbackId}`
}
