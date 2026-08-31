import { parseApiInstantMs } from "@/lib/operatorHome/relativeTime"
import type { CapturePlacementItem } from "@/types/dashboard"
import {
  CAPTURE_CONNECTED_OFFERS_NONE,
  formatCaptureConnectedOffersText,
  type CaptureThankYouOfferFact,
} from "@/lib/operatorCapture/captureThankYouOfferPresentation"

/** @deprecated Prefer CAPTURE_CONNECTED_OFFERS_NONE — kept for archive/multi stubs. */
export const CAPTURE_CONNECTED_OFFERS_STUB = CAPTURE_CONNECTED_OFFERS_NONE

/** Fallback preview placement label when no single preview target is selected. */
export const CAPTURE_PREVIEW_PLACEMENT_LABEL = "Smart Guest" as const

export type CaptureLastJourneyUpdateFact = {
  createdAt: string
  guestName: string
} | null

export type CaptureGuestExperienceFacts = {
  /** Null means placements facts are unavailable (e.g. load failure) — distinct from a true empty list. */
  placements: readonly Pick<
    CapturePlacementItem,
    "qrCodeId" | "qrType" | "status"
  >[] | null
  /**
   * All-time latest Feedback on Active/Paused codes.
   * `undefined` = unavailable (load failure); `null` = none.
   */
  lastJourneyUpdate?: CaptureLastJourneyUpdateFact
  locationName: string
  locationAddress: string
  /** Guest form thank-you catalog attach from Capture snapshot. */
  thankYouOffer?: CaptureThankYouOfferFact | null
  /** Active QR plan cap from snapshot entitlements. */
  activeQrCap?: number | null
}

export type CaptureGuestExperiencePreviewEntry =
  | { kind: "disabled" }
  | { kind: "open-preview"; qrCodeId: number; placementLabel: string }
  | { kind: "open-picker" }

export type OperatorCaptureGuestExperienceView = {
  /**
   * Stub Guest forms line, or "—" when placements facts are unavailable.
   * Live Active count fills both "Used by N of N" slots.
   */
  guestFormsText: string
  /** Live `{N} of {M} placements active`, or "—" when unavailable. */
  qrPlacementsText: string
  /** Plan limit line for active QR placements when entitlements are available. */
  activeQrPlanUsageText: string
  connectedOffersText: string
  /** Live Needs attention copy from Paused non-archived count, or "—" when unavailable. */
  needsAttentionText: string
  /** Live `{date} by {guest}` or "—" when none/unavailable. */
  lastJourneyUpdateText: string
  /** Preview CTA entry: disabled / single-code preview / picker for 2+. */
  previewEntry: CaptureGuestExperiencePreviewEntry
  /** Placement label shown in the preview overlay chrome. */
  previewPlacementLabel: string
  locationName: string
  locationAddress: string
  thankYouOffer: CaptureThankYouOfferFact
}

const QR_TYPE_LABELS: Record<CapturePlacementItem["qrType"], string> = {
  CounterCard: "Counter card",
  PackagingSticker: "Packaging sticker",
  DeliveryInsert: "Delivery insert",
  WindowSticker: "Window sticker",
  SmartGuest: "Smart Guest",
  DigitalGuestLink: "Digital guest link",
}

function formatLastJourneyDate(iso: string): string {
  const ms = parseApiInstantMs(iso)
  if (Number.isNaN(ms)) {
    return ""
  }

  return new Date(ms).toLocaleDateString("en-GB", {
    day: "numeric",
    month: "short",
    year: "numeric",
  })
}

function formatLastJourneyUpdateText(
  fact: CaptureLastJourneyUpdateFact | undefined
): string {
  if (fact == null) {
    return "—"
  }

  const datePart = formatLastJourneyDate(fact.createdAt)
  const guestName = fact.guestName.trim()
  if (datePart === "" || guestName === "") {
    return "—"
  }

  return `${datePart} by ${guestName}`
}

/** Build Guest experience summary fields from Active/Paused placement list facts. */
export function buildCaptureGuestExperience(
  facts: CaptureGuestExperienceFacts
): OperatorCaptureGuestExperienceView {
  const thankYouOffer: CaptureThankYouOfferFact = facts.thankYouOffer ?? {
    offerId: null,
    title: null,
    live: false,
  }
  const connectedOffersText = formatCaptureConnectedOffersText(thankYouOffer)

  if (facts.placements == null) {
    return {
      guestFormsText: "—",
      qrPlacementsText: "—",
      activeQrPlanUsageText: "",
      connectedOffersText,
      needsAttentionText: "—",
      lastJourneyUpdateText: formatLastJourneyUpdateText(facts.lastJourneyUpdate),
      previewEntry: { kind: "disabled" },
      previewPlacementLabel: CAPTURE_PREVIEW_PLACEMENT_LABEL,
      locationName: facts.locationName,
      locationAddress: facts.locationAddress,
      thankYouOffer,
    }
  }

  const activeCount = facts.placements.reduce(
    (count, placement) =>
      placement.status === "Active" ? count + 1 : count,
    0
  )
  const pausedCount = facts.placements.reduce(
    (count, placement) =>
      placement.status === "Paused" ? count + 1 : count,
    0
  )
  const totalCount = facts.placements.length

  const previewable = facts.placements.filter(
    (placement) =>
      placement.status === "Active" || placement.status === "Paused"
  )

  let previewEntry: CaptureGuestExperiencePreviewEntry
  if (previewable.length === 0) {
    previewEntry = { kind: "disabled" }
  } else if (previewable.length === 1) {
    const only = previewable[0]!
    previewEntry = {
      kind: "open-preview",
      qrCodeId: only.qrCodeId,
      placementLabel: QR_TYPE_LABELS[only.qrType],
    }
  } else {
    previewEntry = { kind: "open-picker" }
  }

  return {
    guestFormsText: `1 published form · Used by ${activeCount} of ${activeCount} active placements`,
    qrPlacementsText: `${activeCount} of ${totalCount} placements active`,
    activeQrPlanUsageText:
      facts.activeQrCap != null && facts.activeQrCap > 0
        ? `${activeCount} of ${facts.activeQrCap} active QR placements (plan limit)`
        : "",
    connectedOffersText,
    needsAttentionText:
      pausedCount === 0
        ? "All active placements are ready"
        : `${pausedCount} placements require action`,
    lastJourneyUpdateText: formatLastJourneyUpdateText(facts.lastJourneyUpdate),
    previewEntry,
    previewPlacementLabel:
      previewEntry.kind === "open-preview"
        ? previewEntry.placementLabel
        : CAPTURE_PREVIEW_PLACEMENT_LABEL,
    locationName: facts.locationName,
    locationAddress: facts.locationAddress,
    thankYouOffer,
  }
}
