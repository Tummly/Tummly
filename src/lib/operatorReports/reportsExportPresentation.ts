import type {
  ReportsExportKind,
  ReportsSurface,
} from "@/types/operatorReports"

/**
 * Guest-data ack (RPT-007) — when the pack may include guest/contact rows.
 * Overview PDF includes Feedback comments (+ optional consent page) so it
 * requires ack (REP-03). Capture CSV and Campaigns CSV still download without.
 */
export function reportsExportRequiresGuestDataAck(
  kind: ReportsExportKind,
  format?: "pdf" | "csv" | "xlsx"
): boolean {
  if (kind === "overview" && (format == null || format === "pdf")) {
    return true
  }
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
