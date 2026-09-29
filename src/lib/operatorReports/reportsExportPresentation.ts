import type {
  ReportsExportKind,
  ReportsSurface,
} from "@/types/operatorReports"

/**
 * Guest-data ack (RPT-007) — only when the pack may include guest/contact rows.
 * Aggregate overview PDF, Capture CSV, and Campaigns CSV download without ack.
 */
export function reportsExportRequiresGuestDataAck(
  kind: ReportsExportKind
): boolean {
  return (
    kind === "feedback"
    || kind === "offers-redemptions"
    || kind === "guest-consent"
  )
}

/**
 * Maps a child Reports surface to its export kind (RPT-006).
 * Hub / weekly-brief have no single kind — caller keeps the picker.
 */
export function reportsExportKindForSurface(
  surface: ReportsSurface
): ReportsExportKind | null {
  switch (surface) {
    case "capture":
      return "capture"
    case "feedback":
      return "feedback"
    case "campaigns":
      return "campaigns"
    case "offers":
      return "offers-redemptions"
    default:
      return null
  }
}
