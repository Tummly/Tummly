import { describe, expect, it, vi } from "vitest"

import { DEFAULT_HOME_PERFORMANCE_DATE_RANGE } from "@/lib/operatorHome/homePerformanceDateRange"
import {
  createOperatorReportsPageModule,
  type OperatorReportsPageAdapters,
  type OperatorReportsWorkspaceInput,
} from "@/lib/operatorReports/createOperatorReportsPageModule"
import type {
  WeeklyBriefBody,
  WeeklyBriefGenerateResponse,
  WeeklyBriefGetResponse,
  WeeklyBriefMetrics,
} from "@/types/operatorHome"
import type {
  ReportsCampaignsResponse,
  ReportsCaptureResponse,
  ReportsFeedbackResponse,
  ReportsOffersResponse,
  ReportsOverviewResponse,
} from "@/types/operatorReports"

const emptySection = {
  hasData: false,
  summary: "",
  echoedCounts: null,
}

const weeklyBriefBodyFixture: WeeklyBriefBody = {
  headline: "Loop health held steady this week.",
  capture: {
    hasData: true,
    summary: "Counter cards drove most scans.",
    echoedCounts: null,
  },
  feedback: emptySection,
  offers: emptySection,
  campaigns: emptySection,
  watchNext: ["Follow up on delivery notes"],
}

const weeklyBriefMetricsFixture: WeeklyBriefMetrics = {
  guestsJoined: 28,
  qrScanEvents: 72,
  feedbackCount: 42,
  positiveFeedbackCount: 30,
  neutralFeedbackCount: 8,
  negativeFeedbackCount: 4,
  needsAttentionCount: 2,
  detectedTagCounts: {},
  activeOffers: 3,
  claimsInWeek: 10,
  redemptionsInWeek: 4,
  campaignsSentInWeek: 1,
  campaignRecipientsReached: 40,
}

function readyWeeklyBriefResponse(
  locationId: number
): Extract<WeeklyBriefGetResponse, { ready: true }> {
  return {
    success: true,
    ready: true,
    locationId,
    week: "2026-W33",
    status: "succeeded",
    generatedAtUtc: "2026-08-17T08:00:00Z",
    body: weeklyBriefBodyFixture,
    metrics: weeklyBriefMetricsFixture,
    meta: {
      period: "Week 33, 2026",
      dataSources: ["Capture"],
      confidence: "Based on enough activity to show useful patterns.",
      confidenceLevel: "high",
    },
    executiveSummary:
      "Loop health held steady this week. Counter cards drove most scans.",
  }
}

function notReadyWeeklyBriefResponse(
  locationId: number
): Extract<WeeklyBriefGetResponse, { ready: false }> {
  return {
    success: true,
    ready: false,
    locationId,
    week: "2026-W33",
  }
}

function readyOverview(): Extract<
  ReportsOverviewResponse,
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
      qrScans: metric(10, 5),
      feedbackReceived: metric(4, 2),
      marketingOptIns: metric(3, 1),
      offerRedemptions: metric(1, 0),
      campaignsSent: metric(2, 2),
    },
    privateFeedback: {
      feedbackMessages: metric(4, 2),
      marketingOptIns: metric(3, 1),
      followUpNeeded: metric(1, 0),
      followedUp: metric(2, 1),
    },
    offersAndCampaigns: {
      activeOffers: metric(2, 1),
      offerClaims: metric(5, 3),
      offerRedemptions: metric(1, 0),
      campaignsSent: metric(2, 2),
      unsubscribes: metric(0, 1),
    },
    topCaptureSources: [
      {
        qrCodeId: 9,
        source: "Counter card",
        scans: 10,
        feedback: 4,
        marketingOptIns: 3,
      },
    ],
  }
}

function workspace(
  overrides: Partial<OperatorReportsWorkspaceInput> = {}
): OperatorReportsWorkspaceInput {
  return {
    selectedLocationId: 1,
    locations: [
      { id: 1, locationName: "Main", address: "1 High St" },
      { id: 2, locationName: "Second", address: "2 High St" },
    ],
    billingStatus: "Active",
    chargebackRestricted: false,
    ...overrides,
  }
}

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
      feedbackSubmitted: metric(4, 2),
      contactableGuests: metric(3, 1),
      offerClaimed: metric(1, 0),
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
    ],
  }
}

function readyFeedback(): Extract<
  ReportsFeedbackResponse,
  { lifetimeEmpty: false }
> {
  const metric = (value: number, valuePrevious: number) => ({
    value,
    valuePrevious,
  })
  return {
    success: true,
    lifetimeEmpty: false,
    kpis: {
      feedbackReceived: metric(8, 1),
      marketingOptIns: metric(6, 1),
      followUpNeeded: metric(2, 0),
      resolved: metric(1, 0),
    },
    status: {
      new: metric(1, 0),
      inProgress: metric(2, 0),
      followUpNeeded: metric(2, 0),
      resolved: metric(1, 0),
    },
    needsAttention: [
      {
        feedbackId: 42,
        submittedAt: "2026-07-13T16:00:00.000Z",
        guestName: "Ada",
        source: "Counter card",
        commentPreview: "Bag leaked",
        workflowStatus: "In progress",
      },
    ],
    bySource: [
      {
        qrCodeId: 9,
        source: "Counter card",
        feedback: 8,
        marketingOptIns: 6,
        followUpNeeded: 2,
      },
    ],
  }
}

function readyOffers(): Extract<
  ReportsOffersResponse,
  { lifetimeEmpty: false }
> {
  const metric = (value: number, valuePrevious: number) => ({
    value,
    valuePrevious,
  })
  return {
    success: true,
    lifetimeEmpty: false,
    kpis: {
      activeOffers: metric(2, 1),
      offerClaims: metric(5, 3),
      redemptions: metric(2, 1),
      redemptionRate: { value: 0.4, valuePrevious: 0.33 },
      expiredClaims: metric(1, 0),
      invalidAttempts: metric(2, 0),
    },
    performance: [
      {
        offerId: 11,
        offer: "Free side",
        status: "active",
        claims: 5,
        redemptions: 2,
        rate: 0.4,
        expired: 1,
        invalid: 2,
      },
    ],
    recentRedemptions: [
      {
        id: 91,
        dateTimeUtc: "2026-07-15T12:00:00.000Z",
        offerTitle: "Free side",
        guestName: "Maya",
        locationName: "Main",
        outcome: "redeemed",
      },
    ],
    controlSignals: [
      {
        kind: "repeated-invalid",
        count: 2,
        target: "redemption-log",
      },
    ],
  }
}

function readyCampaigns(): Extract<
  ReportsCampaignsResponse,
  { lifetimeEmpty: false }
> {
  const metric = (value: number, valuePrevious: number) => ({
    value,
    valuePrevious,
  })
  return {
    success: true,
    lifetimeEmpty: false,
    campaignsSent: metric(2, 1),
    guestsMessaged: metric(4, 2),
    failedSends: metric(1, 0),
    performance: [
      {
        campaignId: 9,
        name: "Quiet Tuesday",
        goal: "boost-quieter-time",
        channel: "sms",
        sent: 3,
        status: "sent",
      },
    ],
    needsAttention: [
      {
        campaignId: 11,
        name: "Failed blast",
        status: "failed",
      },
    ],
  }
}

function createAdapters(overrides: {
  getOverview?: OperatorReportsPageAdapters["getOverview"]
  getCapture?: OperatorReportsPageAdapters["getCapture"]
  getFeedback?: OperatorReportsPageAdapters["getFeedback"]
  getOffers?: OperatorReportsPageAdapters["getOffers"]
  getCampaigns?: OperatorReportsPageAdapters["getCampaigns"]
  getReportsDateRange?: OperatorReportsPageAdapters["getReportsDateRange"]
  getWeeklyBrief?: OperatorReportsPageAdapters["getWeeklyBrief"]
  generateWeeklyBrief?: OperatorReportsPageAdapters["generateWeeklyBrief"]
} = {}) {
  const getOverview = vi.fn(
    overrides.getOverview
      ?? (async () => readyOverview() as ReportsOverviewResponse)
  )
  const getCapture = vi.fn(
    overrides.getCapture
      ?? (async () => readyCapture() as ReportsCaptureResponse)
  )
  const getFeedback = vi.fn(
    overrides.getFeedback
      ?? (async () => readyFeedback() as ReportsFeedbackResponse)
  )
  const getOffers = vi.fn(
    overrides.getOffers
      ?? (async () => readyOffers() as ReportsOffersResponse)
  )
  const getCampaigns = vi.fn(
    overrides.getCampaigns
      ?? (async () => readyCampaigns() as ReportsCampaignsResponse)
  )
  const getReportsDateRange = vi.fn(
    overrides.getReportsDateRange
      ?? (() => DEFAULT_HOME_PERFORMANCE_DATE_RANGE)
  )
  const getWeeklyBrief = vi.fn(
    overrides.getWeeklyBrief
      ?? (async (locationId: number) =>
        notReadyWeeklyBriefResponse(locationId))
  )
  const generateWeeklyBrief = vi.fn(
    overrides.generateWeeklyBrief
      ?? (async (locationId: number) =>
        readyWeeklyBriefResponse(locationId) as WeeklyBriefGenerateResponse)
  )
  return {
    getOverview,
    getCapture,
    getFeedback,
    getOffers,
    getCampaigns,
    getReportsDateRange,
    getWeeklyBrief,
    generateWeeklyBrief,
  }
}

describe("createOperatorReportsPageModule", () => {
  it("keeps getSnapshot identity until the next publish", async () => {
    const adapters = createAdapters()
    const module = createOperatorReportsPageModule(adapters)
    const before = module.getSnapshot()
    expect(module.getSnapshot()).toBe(before)

    await module.syncWorkspace(workspace())
    const after = module.getSnapshot()
    expect(after).not.toBe(before)
    expect(module.getSnapshot()).toBe(after)
  })

  it("reloads hub overview when reports date range commits", async () => {
    const adapters = createAdapters()
    const module = createOperatorReportsPageModule(adapters)
    await module.syncWorkspace(workspace())
    expect(adapters.getOverview).toHaveBeenCalledTimes(1)
    const briefCallsAfterSync = adapters.getWeeklyBrief.mock.calls.length

    adapters.getReportsDateRange.mockReturnValue({
      kind: "preset",
      presetId: "last30",
    })
    await module.reloadForReportsDateRange()
    expect(adapters.getOverview).toHaveBeenCalledTimes(2)
    expect(adapters.getWeeklyBrief.mock.calls.length).toBe(briefCallsAfterSync)
    expect(adapters.getCapture).not.toHaveBeenCalled()
    expect(adapters.getFeedback).not.toHaveBeenCalled()
    expect(adapters.getOffers).not.toHaveBeenCalled()
    expect(module.getSnapshot().dateRange).toEqual({
      kind: "preset",
      presetId: "last30",
    })
  })

  it("sets lifetimeEmpty hub status from overview API", async () => {
    const adapters = createAdapters({
      getOverview: async () => ({
        success: true,
        lifetimeEmpty: true,
      }),
    })
    const module = createOperatorReportsPageModule(adapters)
    await module.syncWorkspace(workspace())
    expect(module.getSnapshot().hubLoadStatus).toBe("lifetimeEmpty")
    expect(module.getSnapshot().hubOverview).toBeNull()
  })

  it("retries hub load after error", async () => {
    const getOverview = vi
      .fn<OperatorReportsPageAdapters["getOverview"]>()
      .mockRejectedValueOnce(new Error("network"))
      .mockResolvedValueOnce(readyOverview())
    const adapters = createAdapters({ getOverview })
    const module = createOperatorReportsPageModule(adapters)

    await module.syncWorkspace(workspace())
    expect(module.getSnapshot().hubLoadStatus).toBe("error")

    await module.retryHubLoad()
    expect(module.getSnapshot().hubLoadStatus).toBe("ready")
    expect(module.getSnapshot().hubOverview?.funnelKpis[0]?.value).toBe("10")
  })

  it("loads capture report when capture surface is active", async () => {
    const adapters = createAdapters()
    const module = createOperatorReportsPageModule(adapters)
    module.setActiveSurface("capture")
    await module.syncWorkspace(workspace())

    expect(adapters.getCapture).toHaveBeenCalledTimes(1)
    expect(adapters.getOverview).not.toHaveBeenCalled()
    expect(module.getSnapshot().captureLoadStatus).toBe("ready")
    expect(module.getSnapshot().captureReport?.funnelKpis).toHaveLength(4)
    expect(module.getSnapshot().captureReport?.funnelKpis[0]?.value).toBe("12")
  })

  it("sets lifetimeEmpty capture status from capture API", async () => {
    const adapters = createAdapters({
      getCapture: async () => ({
        success: true,
        lifetimeEmpty: true,
      }),
    })
    const module = createOperatorReportsPageModule(adapters)
    module.setActiveSurface("capture")
    await module.syncWorkspace(workspace())
    expect(module.getSnapshot().captureLoadStatus).toBe("lifetimeEmpty")
    expect(module.getSnapshot().captureReport).toBeNull()
  })

  it("reloads capture only when reports date range commits on capture", async () => {
    const adapters = createAdapters()
    const module = createOperatorReportsPageModule(adapters)
    module.setActiveSurface("capture")
    await module.syncWorkspace(workspace())
    expect(adapters.getCapture).toHaveBeenCalledTimes(1)
    expect(adapters.getOverview).not.toHaveBeenCalled()

    adapters.getReportsDateRange.mockReturnValue({
      kind: "preset",
      presetId: "last30",
    })
    await module.reloadForReportsDateRange()
    expect(adapters.getCapture).toHaveBeenCalledTimes(2)
    expect(adapters.getOverview).not.toHaveBeenCalled()
  })

  it("retries capture load after error", async () => {
    const getCapture = vi
      .fn<OperatorReportsPageAdapters["getCapture"]>()
      .mockRejectedValueOnce(new Error("network"))
      .mockResolvedValueOnce(readyCapture())
    const adapters = createAdapters({ getCapture })
    const module = createOperatorReportsPageModule(adapters)

    module.setActiveSurface("capture")
    await module.syncWorkspace(workspace())
    expect(module.getSnapshot().captureLoadStatus).toBe("error")

    await module.retryCaptureLoad()
    expect(module.getSnapshot().captureLoadStatus).toBe("ready")
    expect(module.getSnapshot().captureReport?.funnelKpis[0]?.value).toBe("12")
  })

  it("disables export under Soft lock", async () => {
    const adapters = createAdapters()
    const module = createOperatorReportsPageModule(adapters)
    await module.syncWorkspace(
      workspace({ billingStatus: "Soft lock" })
    )
    expect(module.getSnapshot().exportAllowed).toBe(false)
    module.openExportDialog()
    expect(module.getSnapshot().exportDialogOpen).toBe(false)
  })

  it("loads hub overview and brief GET in parallel without auto POST", async () => {
    const getWeeklyBrief = vi.fn(async (locationId: number) =>
      readyWeeklyBriefResponse(locationId)
    )
    const generateWeeklyBrief = vi.fn(async () => {
      throw new Error("should not auto generate")
    })
    const adapters = createAdapters({ getWeeklyBrief, generateWeeklyBrief })
    const module = createOperatorReportsPageModule(adapters)

    await module.syncWorkspace(workspace())

    expect(adapters.getOverview).toHaveBeenCalledTimes(1)
    expect(getWeeklyBrief).toHaveBeenCalledTimes(1)
    expect(generateWeeklyBrief).not.toHaveBeenCalled()
    expect(module.getSnapshot().weeklyBrief.status).toBe("ready")
    expect(module.getSnapshot().weeklyBrief.headline).toBe(
      "Loop health held steady this week."
    )
    expect(module.getSnapshot().weeklyBrief.secondary).toBe(
      "Counter cards drove most scans."
    )
    expect(module.getSnapshot().weeklyBrief.meta).toEqual({
      period: "Week 33, 2026",
      dataSources: ["Capture"],
      confidence: "Based on enough activity to show useful patterns.",
      generatedAtUtc: "2026-08-17T08:00:00Z",
    })
    expect(module.getSnapshot().weeklyBrief.executiveSummary).toBe(
      "Loop health held steady this week. Counter cards drove most scans."
    )
  })

  it("clears meta and executive summary on empty and error paths", async () => {
    const adapters = createAdapters({
      getWeeklyBrief: async (locationId) =>
        notReadyWeeklyBriefResponse(locationId),
    })
    const module = createOperatorReportsPageModule(adapters)
    await module.syncWorkspace(workspace())
    expect(module.getSnapshot().weeklyBrief.status).toBe("empty")
    expect(module.getSnapshot().weeklyBrief.meta).toBeNull()
    expect(module.getSnapshot().weeklyBrief.executiveSummary).toBeNull()

    const errorAdapters = createAdapters({
      getWeeklyBrief: async () => {
        throw new Error("network")
      },
    })
    const errorModule = createOperatorReportsPageModule(errorAdapters)
    await errorModule.syncWorkspace(workspace())
    expect(errorModule.getSnapshot().weeklyBrief.status).toBe("error")
    expect(errorModule.getSnapshot().weeklyBrief.meta).toBeNull()
    expect(errorModule.getSnapshot().weeklyBrief.executiveSummary).toBeNull()
  })

  it("sets brief loading on hub GET before the response", async () => {
    let releaseGet: () => void = () => {}
    const getWeeklyBrief = vi.fn(
      () =>
        new Promise<WeeklyBriefGetResponse>((resolve) => {
          releaseGet = () => resolve(notReadyWeeklyBriefResponse(1))
        })
    )
    const adapters = createAdapters({ getWeeklyBrief })
    const module = createOperatorReportsPageModule(adapters)

    const sync = module.syncWorkspace(workspace())
    await vi.waitFor(() => {
      expect(module.getSnapshot().weeklyBrief.status).toBe("loading")
    })
    expect(adapters.generateWeeklyBrief).not.toHaveBeenCalled()

    releaseGet()
    await sync
    expect(module.getSnapshot().weeklyBrief.status).toBe("empty")
  })

  it("sets brief empty when GET returns not ready", async () => {
    const adapters = createAdapters({
      getWeeklyBrief: async (locationId) =>
        notReadyWeeklyBriefResponse(locationId),
    })
    const module = createOperatorReportsPageModule(adapters)
    await module.syncWorkspace(workspace())
    expect(module.getSnapshot().weeklyBrief.status).toBe("empty")
    expect(adapters.generateWeeklyBrief).not.toHaveBeenCalled()
  })

  it("sets brief error when GET fails", async () => {
    const adapters = createAdapters({
      getWeeklyBrief: async () => {
        throw new Error("network")
      },
    })
    const module = createOperatorReportsPageModule(adapters)
    await module.syncWorkspace(workspace())
    expect(module.getSnapshot().weeklyBrief.status).toBe("error")
    expect(module.getSnapshot().weeklyBrief.errorRetryable).toBe(true)
  })

  it("keeps hub and brief statuses independent", async () => {
    const adapters = createAdapters({
      getOverview: async () => {
        throw new Error("overview down")
      },
      getWeeklyBrief: async (locationId) =>
        readyWeeklyBriefResponse(locationId),
    })
    const module = createOperatorReportsPageModule(adapters)
    await module.syncWorkspace(workspace())
    expect(module.getSnapshot().hubLoadStatus).toBe("error")
    expect(module.getSnapshot().weeklyBrief.status).toBe("ready")
  })

  it("navigates only from Generate brief when already ready", async () => {
    const getWeeklyBrief = vi.fn(async (locationId: number) =>
      readyWeeklyBriefResponse(locationId)
    )
    const generateWeeklyBrief = vi.fn(async () => {
      throw new Error("should not generate")
    })
    const adapters = createAdapters({ getWeeklyBrief, generateWeeklyBrief })
    const module = createOperatorReportsPageModule(adapters)
    await module.syncWorkspace(workspace())

    const ok = await module.ensureWeeklyBriefReady()
    expect(ok).toBe(true)
    expect(generateWeeklyBrief).not.toHaveBeenCalled()
  })

  it("POSTs then succeeds from Generate brief when missing", async () => {
    const getWeeklyBrief = vi.fn(async (locationId: number) =>
      notReadyWeeklyBriefResponse(locationId)
    )
    const generateWeeklyBrief = vi.fn(async (locationId: number) =>
      readyWeeklyBriefResponse(locationId)
    )
    const adapters = createAdapters({ getWeeklyBrief, generateWeeklyBrief })
    const module = createOperatorReportsPageModule(adapters)
    await module.syncWorkspace(workspace())
    expect(module.getSnapshot().weeklyBrief.status).toBe("empty")

    const ok = await module.ensureWeeklyBriefReady()
    expect(ok).toBe(true)
    expect(generateWeeklyBrief).toHaveBeenCalledTimes(1)
    expect(module.getSnapshot().weeklyBrief.status).toBe("ready")
  })

  it("generates in place from the weekly-brief page empty CTA", async () => {
    const getWeeklyBrief = vi.fn(async (locationId: number) =>
      notReadyWeeklyBriefResponse(locationId)
    )
    const generateWeeklyBrief = vi.fn(async (locationId: number) =>
      readyWeeklyBriefResponse(locationId)
    )
    const adapters = createAdapters({ getWeeklyBrief, generateWeeklyBrief })
    const module = createOperatorReportsPageModule(adapters)
    await module.syncWorkspace(workspace())
    module.setActiveSurface("weekly-brief")
    await vi.waitFor(() => {
      expect(module.getSnapshot().activeSurface).toBe("weekly-brief")
    })

    await module.generateWeeklyBriefInPlace()
    expect(generateWeeklyBrief).toHaveBeenCalledTimes(1)
    expect(module.getSnapshot().weeklyBrief.status).toBe("ready")
    expect(module.getSnapshot().weeklyBrief.body?.headline).toBe(
      "Loop health held steady this week."
    )
  })

  it("retries weekly brief with GET then generate if still missing", async () => {
    const getWeeklyBrief = vi
      .fn<OperatorReportsPageAdapters["getWeeklyBrief"]>()
      .mockRejectedValueOnce(new Error("network"))
      .mockResolvedValueOnce(notReadyWeeklyBriefResponse(1))
      .mockResolvedValue(readyWeeklyBriefResponse(1))
    const generateWeeklyBrief = vi.fn(async (locationId: number) =>
      readyWeeklyBriefResponse(locationId)
    )
    const adapters = createAdapters({ getWeeklyBrief, generateWeeklyBrief })
    const module = createOperatorReportsPageModule(adapters)

    await module.syncWorkspace(workspace())
    expect(module.getSnapshot().weeklyBrief.status).toBe("error")

    await module.retryWeeklyBrief()
    expect(module.getSnapshot().weeklyBrief.status).toBe("ready")
    expect(generateWeeklyBrief).toHaveBeenCalledTimes(1)
  })

  it("loads Feedback surface and reloads on date-range commit", async () => {
    const adapters = createAdapters()
    const module = createOperatorReportsPageModule(adapters)
    module.setActiveSurface("feedback")
    await module.syncWorkspace(workspace())
    expect(adapters.getFeedback).toHaveBeenCalledTimes(1)
    expect(module.getSnapshot().feedbackLoadStatus).toBe("ready")
    expect(module.getSnapshot().feedbackReport?.topKpis[0]?.value).toBe("8")

    adapters.getReportsDateRange.mockReturnValue({
      kind: "preset",
      presetId: "last30",
    })
    await module.reloadForReportsDateRange()
    expect(adapters.getFeedback).toHaveBeenCalledTimes(2)
    expect(adapters.getOverview).not.toHaveBeenCalled()
  })

  it("sets lifetimeEmpty Feedback status from feedback API", async () => {
    const adapters = createAdapters({
      getFeedback: async () => ({
        success: true,
        lifetimeEmpty: true,
      }),
    })
    const module = createOperatorReportsPageModule(adapters)
    module.setActiveSurface("feedback")
    await module.syncWorkspace(workspace())
    expect(module.getSnapshot().feedbackLoadStatus).toBe("lifetimeEmpty")
    expect(module.getSnapshot().feedbackReport).toBeNull()
  })

  it("retries Feedback load after error", async () => {
    const getFeedback = vi
      .fn<OperatorReportsPageAdapters["getFeedback"]>()
      .mockRejectedValueOnce(new Error("network"))
      .mockResolvedValueOnce(readyFeedback())
    const adapters = createAdapters({ getFeedback })
    const module = createOperatorReportsPageModule(adapters)
    module.setActiveSurface("feedback")
    await module.syncWorkspace(workspace())
    expect(module.getSnapshot().feedbackLoadStatus).toBe("error")

    await module.retryFeedbackLoad()
    expect(module.getSnapshot().feedbackLoadStatus).toBe("ready")
    expect(module.getSnapshot().feedbackReport?.followUpList[0]?.feedbackId).toBe(
      42
    )
  })

  it("loads Offers report when the Offers surface is active", async () => {
    const adapters = createAdapters()
    const module = createOperatorReportsPageModule(adapters)
    module.setActiveSurface("offers")
    await module.syncWorkspace(workspace())
    expect(adapters.getOffers).toHaveBeenCalledTimes(1)
    expect(adapters.getOverview).not.toHaveBeenCalled()
    expect(module.getSnapshot().offersLoadStatus).toBe("ready")
    expect(module.getSnapshot().offersReport?.kpis.offerClaims.value).toBe("5")
  })

  it("reloads Offers report when date range commits on the Offers surface", async () => {
    const adapters = createAdapters()
    const module = createOperatorReportsPageModule(adapters)
    module.setActiveSurface("offers")
    await module.syncWorkspace(workspace())
    expect(adapters.getOffers).toHaveBeenCalledTimes(1)
    expect(adapters.getOverview).not.toHaveBeenCalled()

    adapters.getReportsDateRange.mockReturnValue({
      kind: "preset",
      presetId: "last30",
    })
    await module.reloadForReportsDateRange()
    expect(adapters.getOffers).toHaveBeenCalledTimes(2)
    expect(adapters.getOverview).not.toHaveBeenCalled()
  })

  it("retries Offers load after error", async () => {
    const getOffers = vi
      .fn<OperatorReportsPageAdapters["getOffers"]>()
      .mockRejectedValueOnce(new Error("network"))
      .mockResolvedValueOnce(readyOffers())
    const adapters = createAdapters({ getOffers })
    const module = createOperatorReportsPageModule(adapters)
    module.setActiveSurface("offers")

    await module.syncWorkspace(workspace())
    expect(module.getSnapshot().offersLoadStatus).toBe("error")

    await module.retryOffersLoad()
    expect(module.getSnapshot().offersLoadStatus).toBe("ready")
    expect(module.getSnapshot().offersReport?.kpis.activeOffers.value).toBe("2")
  })

  it("loads Campaigns report when the Campaigns surface is active", async () => {
    const adapters = createAdapters()
    const module = createOperatorReportsPageModule(adapters)
    module.setActiveSurface("campaigns")
    await module.syncWorkspace(workspace())
    expect(adapters.getCampaigns).toHaveBeenCalledTimes(1)
    expect(adapters.getOverview).not.toHaveBeenCalled()
    expect(module.getSnapshot().campaignsLoadStatus).toBe("ready")
    expect(module.getSnapshot().campaignsReport?.kpis[0]?.value).toBe("2")
  })

  it("reloads Campaigns report when date range commits on the Campaigns surface", async () => {
    const adapters = createAdapters()
    const module = createOperatorReportsPageModule(adapters)
    module.setActiveSurface("campaigns")
    await module.syncWorkspace(workspace())
    expect(adapters.getCampaigns).toHaveBeenCalledTimes(1)
    expect(adapters.getOverview).not.toHaveBeenCalled()

    adapters.getReportsDateRange.mockReturnValue({
      kind: "preset",
      presetId: "last30",
    })
    await module.reloadForReportsDateRange()
    expect(adapters.getCampaigns).toHaveBeenCalledTimes(2)
    expect(adapters.getOverview).not.toHaveBeenCalled()
  })

  it("retries Campaigns load after error", async () => {
    const getCampaigns = vi
      .fn<OperatorReportsPageAdapters["getCampaigns"]>()
      .mockRejectedValueOnce(new Error("network"))
      .mockResolvedValueOnce(readyCampaigns())
    const adapters = createAdapters({ getCampaigns })
    const module = createOperatorReportsPageModule(adapters)
    module.setActiveSurface("campaigns")

    await module.syncWorkspace(workspace())
    expect(module.getSnapshot().campaignsLoadStatus).toBe("error")

    await module.retryCampaignsLoad()
    expect(module.getSnapshot().campaignsLoadStatus).toBe("ready")
    expect(module.getSnapshot().campaignsReport?.kpis[0]?.value).toBe("2")
  })
})
