import { describe, expect, it, vi } from "vitest"

import { DEFAULT_HOME_PERFORMANCE_DATE_RANGE } from "@/lib/operatorHome/homePerformanceDateRange"
import {
  createOperatorReportsPageModule,
  type OperatorReportsPageAdapters,
  type OperatorReportsWorkspaceInput,
} from "@/lib/operatorReports/createOperatorReportsPageModule"
import type { ReportsOverviewResponse } from "@/types/operatorReports"

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

function createAdapters(overrides: {
  getOverview?: OperatorReportsPageAdapters["getOverview"]
  getReportsDateRange?: OperatorReportsPageAdapters["getReportsDateRange"]
} = {}) {
  const getOverview = vi.fn(
    overrides.getOverview
      ?? (async () => readyOverview() as ReportsOverviewResponse)
  )
  const getReportsDateRange = vi.fn(
    overrides.getReportsDateRange
      ?? (() => DEFAULT_HOME_PERFORMANCE_DATE_RANGE)
  )
  return {
    getOverview,
    getReportsDateRange,
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

    adapters.getReportsDateRange.mockReturnValue({
      kind: "preset",
      presetId: "last30",
    })
    await module.reloadForReportsDateRange()
    expect(adapters.getOverview).toHaveBeenCalledTimes(2)
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
})
