import { formatRelativeTime } from "@/lib/operatorHome/relativeTime"
import type {
  CapturePlacementItem,
  CapturePlacementStatus,
} from "@/types/dashboard"

export type CaptureDigitalGuestLinkFact = CapturePlacementItem

export type OperatorCaptureDigitalGuestLinkRow = {
  qrCodeId: number
  guestLinkLabel: string
  status: CapturePlacementStatus
  qrLinkUrl: string
  qrScansText: string
  feedbackSubmittedText: string
  marketingOptInsText: string
  offerClaimsText: string
  /** Relative time via formatRelativeTime, or "—" when never scanned. */
  lastScanText: string
}

export type CaptureDigitalGuestLinksResult = {
  rows: OperatorCaptureDigitalGuestLinkRow[]
  isEmpty: boolean
}

/** Build Digital guest links table rows (operator-created only; omits Smart Guest). */
export function buildCaptureDigitalGuestLinks(
  facts: readonly CaptureDigitalGuestLinkFact[],
  nowMs: number = Date.now()
): CaptureDigitalGuestLinksResult {
  const rows = facts
    .filter((fact) => fact.qrType === "DigitalGuestLink")
    .map((fact) => {
      const lastScanText =
        fact.lastScanAt == null || fact.lastScanAt === ""
          ? "—"
          : formatRelativeTime(fact.lastScanAt, nowMs) || "—"

      const guestLinkLabel =
        fact.linkName != null && fact.linkName.trim() !== ""
          ? fact.linkName.trim()
          : "Digital guest link"

      return {
        qrCodeId: fact.qrCodeId,
        guestLinkLabel,
        status: fact.status,
        qrLinkUrl: fact.qrLinkUrl,
        qrScansText: `${fact.qrScans} opens`,
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
