/**
 * Figma Operator Reports — Capture report sub-page.
 * Node 3498 / reports-capture-flow.
 */

export type DatePreset = "7d" | "30d" | "90d" | "month" | "ytd"

export const DATE_PRESET_LABELS: Record<DatePreset, string> = {
  "7d": "Last 7 days",
  "30d": "Last 30 days",
  "90d": "Last 90 days",
  month: "This month",
  ytd: "Year to date",
}

export const CAPTURE_REPORT_PAGE_COPY = {
  breadcrumbReports: "Reports",
  breadcrumbCaptureReport: "Capture report",
  title: "Capture report",
  subtitle:
    "See which QR codes and placements turn scans into guest feedback and contactable guests.",
  generateBrief: "Generate brief",
  export: "Export",
  emptyTitle: "No QR activity yet",
  emptySubtitle:
    "Reports will appear once guests start scanning your QR codes or Smart Guest Links.",
  funnelSectionTitle: "Scan-to-guest funnel",
  funnelInsight:
    "Most drop-off happened between opening the form and submitting feedback. Review the form length, offer wording and page load speed.",
  reviewGuestForm: "Review guest form",
  placementSectionTitle: "QR placement performance",
  placementInsightTitle: "Placement insight",
  placementInsightSubtitle:
    "Your quiet-day offer had the most redemptions this period. One campaign caused more opt-outs than usual, so review the audience before sending again.",
  createPlacement: "Create another QR placement",
  actionsMenuLabel: "Actions",
  viewPlacement: "View QR placement",
  editDetails: "Edit details",
  downloadQr: "Download QR code",
} as const

export type CaptureReportKpi = {
  label: string
  value: string | number
  delta: string
  positive?: boolean | null
}

export type CaptureReportFunnelStep = {
  step: string
  count: number | string
  dropOff: number | string
}

export type CaptureReportPlacementRow = {
  id: string
  qrName: string
  placement: string
  status: "Active" | "Paused" | "Archived"
  scans: number
  feedback: number
  contactable: number
  claims: number
  conversion: string
}

export type CaptureReportData = {
  kpis: {
    qrScans: CaptureReportKpi
    formOpened: CaptureReportKpi
    feedbackSubmitted: CaptureReportKpi
    contactProvided: CaptureReportKpi
    contactableGuests: CaptureReportKpi
    offerClaims: CaptureReportKpi
  }
  funnel: CaptureReportFunnelStep[]
  placements: CaptureReportPlacementRow[]
}

export const mockCaptureReportData: CaptureReportData = {
  kpis: {
    qrScans: {
      label: "QR scans",
      value: "0",
      delta: "[X]% vs previous period",
      positive: true,
    },
    formOpened: {
      label: "Form opened",
      value: "0",
      delta: "[X]% vs previous period",
      positive: true,
    },
    feedbackSubmitted: {
      label: "Feedback submitted",
      value: "0",
      delta: "[X]% vs previous period",
      positive: true,
    },
    contactProvided: {
      label: "Contact provided",
      value: "0",
      delta: "[X]% vs previous period",
      positive: true,
    },
    contactableGuests: {
      label: "Contactable guests",
      value: "0",
      delta: "[X]% vs previous period",
      positive: true,
    },
    offerClaims: {
      label: "Offer claims",
      value: "0",
      delta: "[X]% vs previous period",
      positive: true,
    },
  },
  funnel: [
    { step: "QR scans", count: 72, dropOff: "—" },
    { step: "Form opened", count: 45, dropOff: 12 },
    { step: "Feedback submitted", count: 33, dropOff: 15 },
    { step: "Contact provided", count: 88, dropOff: 22 },
    { step: "Contactable guests", count: 88, dropOff: 22 },
    { step: "Offer claimed", count: 88, dropOff: 22 },
  ],
  placements: [
    {
      id: "delivery-insert",
      qrName: "Delivery insert",
      placement: "Delivery",
      status: "Active",
      scans: 72,
      feedback: 18,
      contactable: 11,
      claims: 8,
      conversion: "25%",
    },
    {
      id: "counter-card",
      qrName: "Counter card",
      placement: "Delivery",
      status: "Active",
      scans: 72,
      feedback: 18,
      contactable: 11,
      claims: 8,
      conversion: "25%",
    },
    {
      id: "receipt-qr",
      qrName: "Receipt QR",
      placement: "Delivery",
      status: "Active",
      scans: 72,
      feedback: 18,
      contactable: 11,
      claims: 8,
      conversion: "25%",
    },
    {
      id: "table-card",
      qrName: "Table card",
      placement: "Delivery",
      status: "Active",
      scans: 72,
      feedback: 18,
      contactable: 11,
      claims: 8,
      conversion: "25%",
    },
  ],
}
