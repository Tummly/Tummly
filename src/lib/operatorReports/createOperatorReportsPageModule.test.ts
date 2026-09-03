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
  ReportsCaptureResponse,
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

function createAdapters(overrides: {
  getOverview?: OperatorReportsPageAdapters["getOverview"]
  getCapture?: OperatorReportsPageAdapters["getCapture"]
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
})
