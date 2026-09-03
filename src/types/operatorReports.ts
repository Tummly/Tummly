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

export type ReportsFeedbackNeedsAttentionWire = {
  feedbackId: number
  submittedAt: string
  guestName: string
  source: string
  commentPreview: string
  workflowStatus: string
}

export type ReportsFeedbackBySourceWire = {
  qrCodeId: number
  source: string
  feedback: number
  marketingOptIns: number
  followUpNeeded: number
}

export type ReportsFeedbackResponse =
  | {
      success: true
      lifetimeEmpty: true
    }
  | {
      success: true
      lifetimeEmpty: false
      kpis: {
        feedbackReceived: ReportsMetricWire
        marketingOptIns: ReportsMetricWire
        followUpNeeded: ReportsMetricWire
        resolved: ReportsMetricWire
      }
      status: {
        new: ReportsMetricWire
        inProgress: ReportsMetricWire
        followUpNeeded: ReportsMetricWire
        resolved: ReportsMetricWire
      }
      needsAttention: ReportsFeedbackNeedsAttentionWire[]
      bySource: ReportsFeedbackBySourceWire[]
    }
  | {
      success: false
      message?: string
    }

export type ReportsRateMetricWire = {
  value: number | null
  valuePrevious: number | null
}

export type ReportsOffersPerformanceRowWire = {
  offerId: number
  offer: string
  status: string
  claims: number
  redemptions: number
  rate: number | null
  expired: number
  invalid: number
}

export type ReportsOffersRecentRedemptionWire = {
  id: number
  dateTimeUtc: string
  offerTitle: string
  guestName: string
  locationName: string
  outcome: "redeemed"
}

export type ReportsOffersRepeatedInvalidSignalWire = {
  kind: "repeated-invalid"
  count: number
  target: "redemption-log"
}

export type ReportsOffersLowRedemptionSignalWire = {
  kind: "low-redemption"
  offerId: number
  offerTitle: string
  claims: number
  redemptions: number
  rate: number
  target: "offers"
}

export type ReportsOffersControlSignalWire =
  | ReportsOffersRepeatedInvalidSignalWire
  | ReportsOffersLowRedemptionSignalWire

export type ReportsOffersResponse =
  | {
      success: true
      lifetimeEmpty: true
    }
  | {
      success: true
      lifetimeEmpty: false
      kpis: {
        activeOffers: ReportsMetricWire
        offerClaims: ReportsMetricWire
        redemptions: ReportsMetricWire
        redemptionRate: ReportsRateMetricWire
        expiredClaims: ReportsMetricWire
        invalidAttempts: ReportsMetricWire
      }
      performance: ReportsOffersPerformanceRowWire[]
      recentRedemptions: ReportsOffersRecentRedemptionWire[]
      controlSignals: ReportsOffersControlSignalWire[]
    }
  | {
      success: false
      message?: string
    }

export type ReportsCampaignsPerformanceWire = {
  campaignId: number
  name: string
  goal: string | null
  channel: string | null
  sent: number
  status: string
}

export type ReportsCampaignsAttentionWire = {
  campaignId: number
  name: string
  status: string
}

export type ReportsCampaignsResponse =
  | {
      success: true
      lifetimeEmpty: true
    }
  | {
      success: true
      lifetimeEmpty: false
      campaignsSent: ReportsMetricWire
      guestsMessaged: ReportsMetricWire
      failedSends: ReportsMetricWire
      performance: ReportsCampaignsPerformanceWire[]
      needsAttention: ReportsCampaignsAttentionWire[]
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
