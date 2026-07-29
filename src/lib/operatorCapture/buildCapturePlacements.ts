import { formatRelativeTime } from "@/lib/operatorHome/relativeTime"
import type {
  CapturePlacementItem,
  CapturePlacementQrType,
  CapturePlacementStatus,
} from "@/types/dashboard"

export type CapturePlacementFact = CapturePlacementItem

export type OperatorCapturePlacementRow = {
  qrCodeId: number
  qrType: CapturePlacementQrType
  placementLabel: string
  status: CapturePlacementStatus
  qrLinkUrl: string
  qrScansText: string
  feedbackSubmittedText: string
  marketingOptInsText: string
  offerClaimsText: string
  /** Relative time via formatRelativeTime, or "—" when never scanned. */
  lastScanText: string
}

export type CapturePlacementsResult = {
  rows: OperatorCapturePlacementRow[]
  isEmpty: boolean
}

const QR_TYPE_LABELS: Record<CapturePlacementQrType, string> = {
  CounterCard: "Counter card",
  PackagingSticker: "Packaging sticker",
  DeliveryInsert: "Delivery insert",
  WindowSticker: "Window sticker",
  SmartGuest: "Smart Guest",
}

/** Build QR placements table rows from Active/Paused list facts. */
export function buildCapturePlacements(
  facts: readonly CapturePlacementFact[],
  nowMs: number = Date.now()
): CapturePlacementsResult {
  const rows = facts.map((fact) => {
    const lastScanText =
      fact.lastScanAt == null || fact.lastScanAt === ""
        ? "—"
        : formatRelativeTime(fact.lastScanAt, nowMs) || "—"

    return {
      qrCodeId: fact.qrCodeId,
      qrType: fact.qrType,
      placementLabel: QR_TYPE_LABELS[fact.qrType],
      status: fact.status,
      qrLinkUrl: fact.qrLinkUrl,
      qrScansText: `${fact.qrScans} scans`,
      feedbackSubmittedText: `${fact.feedbackSubmitted} feedback`,
      marketingOptInsText: `${fact.marketingOptIns} opt-ins`,
      offerClaimsText: `${fact.offerClaims} claims`,
      lastScanText,
    }
  })

  return {
    rows,
    isEmpty: rows.length === 0,
  }
}
