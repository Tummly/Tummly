import { describe, it, expect } from "vitest"

import {
  buildReportsCaptureViewModel,
  CAPTURE_REPORT_PAGE_COPY,
} from "./captureReportPresentation"
import type { ReportsCaptureResponse } from "@/types/operatorReports"

function readyCapture(): Extract<
  ReportsCaptureResponse,
  { lifetimeEmpty: false }
> {
  const metric = (value: number, valuePrevious: number) => ({
    value,
    valuePrevious,
  })
  return {
    success: true,
    lifetimeEmpty: false,
    funnel: {
      qrScans: metric(12, 6),
      feedbackSubmitted: metric(4, 8),
      contactableGuests: metric(3, 1),
      offerClaimed: metric(5, 0),
    },
    placements: [
      {
        qrCodeId: 9,
        name: "Counter card",
        status: "Active",
        scans: 12,
        feedback: 4,
        contactable: 3,
      },
      {
        qrCodeId: 10,
        name: "Window sticker",
        status: "Paused",
        scans: 0,
        feedback: 0,
        contactable: 0,
      },
    ],
  }
}

describe("captureReportPresentation", () => {
  it("exports complete copy constants for Capture Report page", () => {
    expect(CAPTURE_REPORT_PAGE_COPY.breadcrumbReports).toBe("Reports")
    expect(CAPTURE_REPORT_PAGE_COPY.breadcrumbCaptureReport).toBe(
      "Capture report"
    )
    expect(CAPTURE_REPORT_PAGE_COPY.title).toBe("Capture report")
    expect(CAPTURE_REPORT_PAGE_COPY.subtitle).toContain("QR codes")
    expect(CAPTURE_REPORT_PAGE_COPY.emptyTitle).toBe("No QR activity yet")
    expect(CAPTURE_REPORT_PAGE_COPY.funnelSectionTitle).toBe(
      "Scan-to-guest funnel"
    )
    expect(CAPTURE_REPORT_PAGE_COPY.placementSectionTitle).toBe(
      "QR placement performance"
    )
    expect(CAPTURE_REPORT_PAGE_COPY.reviewGuestForm).toBe("Review guest form")
    expect(CAPTURE_REPORT_PAGE_COPY.createPlacement).toBe(
      "Create another QR placement"
    )
  })

  it("builds four funnel KPIs with drop-off and placement conversion", () => {
    const view = buildReportsCaptureViewModel(readyCapture())

    expect(view.funnelKpis).toHaveLength(4)
    expect(view.funnelKpis.map((row) => row.label)).toEqual([
      "QR scans",
      "Feedback submitted",
      "Contactable guests",
      "Offer claimed",
    ])

    expect(view.funnel).toHaveLength(4)
    expect(view.funnel[0]).toEqual({
      step: "QR scans",
      count: 12,
      dropOff: "—",
    })
    expect(view.funnel[1]?.dropOff).toBe(8)
    expect(view.funnel[2]?.dropOff).toBe(1)
    expect(view.funnel[3]?.dropOff).toBe(0)

    expect(view.placements[0]?.conversion).toBe("33%")
    expect(view.placements[1]?.conversion).toBe("—")
    expect(view.placements[0]?.qrName).toBe("Counter card")
  })
})
