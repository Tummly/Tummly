export type ReportsMetricWire = {
  value: number
  valuePrevious: number
}

export type ReportsOverviewCaptureSourceWire = {
  qrCodeId: number
  source: string
  scans: number
  feedback: number
  marketingOptIns: number
}

export type ReportsOverviewResponse =
  | {
      success: true
      lifetimeEmpty: true
    }
  | {
      success: true
      lifetimeEmpty: false
      funnel: {
        qrScans: ReportsMetricWire
        feedbackReceived: ReportsMetricWire
        marketingOptIns: ReportsMetricWire
        offerRedemptions: ReportsMetricWire
        campaignsSent: ReportsMetricWire
      }
      privateFeedback: {
        feedbackMessages: ReportsMetricWire
        marketingOptIns: ReportsMetricWire
        followUpNeeded: ReportsMetricWire
        followedUp: ReportsMetricWire
      }
      offersAndCampaigns: {
        activeOffers: ReportsMetricWire
        offerClaims: ReportsMetricWire
        offerRedemptions: ReportsMetricWire
        campaignsSent: ReportsMetricWire
        unsubscribes: ReportsMetricWire
      }
      topCaptureSources: ReportsOverviewCaptureSourceWire[]
    }
  | {
      success: false
      message?: string
    }

export type ReportsCapturePlacementWire = {
  qrCodeId: number
  name: string
  status: "Active" | "Paused"
  scans: number
  feedback: number
  contactable: number
}

export type ReportsCaptureResponse =
  | {
      success: true
      lifetimeEmpty: true
    }
  | {
      success: true
      lifetimeEmpty: false
      funnel: {
        qrScans: ReportsMetricWire
        feedbackSubmitted: ReportsMetricWire
        contactableGuests: ReportsMetricWire
        offerClaimed: ReportsMetricWire
      }
      placements: ReportsCapturePlacementWire[]
    }
  | {
      success: false
      message?: string
    }

export type ReportsSurface =
  | "hub"
  | "capture"
  | "feedback"
  | "offers"
  | "campaigns"
  | "weekly-brief"

export type ReportsKpiLoadStatus =
  | "idle"
  | "loading"
  | "ready"
  | "lifetimeEmpty"
  | "error"
