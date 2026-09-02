import { describe, it, expect } from "vitest"
import {
  CAPTURE_REPORT_PAGE_COPY,
  DATE_PRESET_LABELS,
  mockCaptureReportData,
} from "./captureReportPresentation"

describe("captureReportPresentation", () => {
  it("exports complete copy constants for Capture Report page", () => {
    expect(CAPTURE_REPORT_PAGE_COPY.breadcrumbReports).toBe("Reports")
    expect(CAPTURE_REPORT_PAGE_COPY.breadcrumbCaptureReport).toBe(
      "Capture report",
    )
    expect(CAPTURE_REPORT_PAGE_COPY.title).toBe("Capture report")
    expect(CAPTURE_REPORT_PAGE_COPY.subtitle).toContain("QR codes")
    expect(CAPTURE_REPORT_PAGE_COPY.emptyTitle).toBe("No QR activity yet")
    expect(CAPTURE_REPORT_PAGE_COPY.funnelSectionTitle).toBe(
      "Scan-to-guest funnel",
    )
    expect(CAPTURE_REPORT_PAGE_COPY.placementSectionTitle).toBe(
      "QR placement performance",
    )
    expect(CAPTURE_REPORT_PAGE_COPY.reviewGuestForm).toBe("Review guest form")
    expect(CAPTURE_REPORT_PAGE_COPY.createPlacement).toBe(
      "Create another QR placement",
    )
  })

  it("exports date preset labels for all supported date filters", () => {
    expect(DATE_PRESET_LABELS["7d"]).toBe("Last 7 days")
    expect(DATE_PRESET_LABELS["30d"]).toBe("Last 30 days")
    expect(DATE_PRESET_LABELS["90d"]).toBe("Last 90 days")
    expect(DATE_PRESET_LABELS.month).toBe("This month")
    expect(DATE_PRESET_LABELS.ytd).toBe("Year to date")
  })

  it("provides structured mock data with 6 KPIs, funnel steps, and placements", () => {
    expect(Object.keys(mockCaptureReportData.kpis)).toHaveLength(6)
    expect(mockCaptureReportData.kpis.qrScans.label).toBe("QR scans")
    expect(mockCaptureReportData.kpis.formOpened.label).toBe("Form opened")
    expect(mockCaptureReportData.kpis.feedbackSubmitted.label).toBe(
      "Feedback submitted",
    )
    expect(mockCaptureReportData.kpis.contactProvided.label).toBe(
      "Contact provided",
    )
    expect(mockCaptureReportData.kpis.contactableGuests.label).toBe(
      "Contactable guests",
    )
    expect(mockCaptureReportData.kpis.offerClaims.label).toBe("Offer claims")

    expect(mockCaptureReportData.funnel).toHaveLength(6)
    expect(mockCaptureReportData.funnel[0].step).toBe("QR scans")
    expect(mockCaptureReportData.funnel[1].step).toBe("Form opened")

    expect(mockCaptureReportData.placements.length).toBeGreaterThanOrEqual(4)
    expect(mockCaptureReportData.placements[0].qrName).toBe("Delivery insert")
    expect(mockCaptureReportData.placements[0].status).toBe("Active")
  })
})
