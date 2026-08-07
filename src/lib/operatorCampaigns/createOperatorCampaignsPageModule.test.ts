import { describe, expect, it, vi, type Mock } from "vitest"

import {
  CAMPAIGNS_LOAD_ERROR_MESSAGE,
  CAMPAIGNS_PAGE_COPY,
  createOperatorCampaignsPageModule,
  type OperatorCampaignsPageAdapters,
} from "@/lib/operatorCampaigns/createOperatorCampaignsPageModule"
import {
  DEFAULT_CAMPAIGNS_OVERVIEW_DATE_RANGE,
  type CampaignsOverviewDateRange,
} from "@/lib/operatorCampaigns/campaignsOverviewDateRange"

function createAdapters(
  overrides: Partial<OperatorCampaignsPageAdapters> & {
    loadOverview?: Mock<OperatorCampaignsPageAdapters["loadOverview"]>
    loadMarketingEligible?: Mock<
      OperatorCampaignsPageAdapters["loadMarketingEligible"]
    >
  } = {}
): OperatorCampaignsPageAdapters {
  return {
    loadOverview:
      overrides.loadOverview
      ?? vi.fn(async () => ({ totalCount: 0 })),
    loadMarketingEligible:
      overrides.loadMarketingEligible
      ?? vi.fn(async () => 42),
    getCampaignsOverviewDateRange:
      overrides.getCampaignsOverviewDateRange
      ?? (() => DEFAULT_CAMPAIGNS_OVERVIEW_DATE_RANGE),
  }
}

describe("createOperatorCampaignsPageModule", () => {
  it("loads true-empty overview chrome for a location with no campaigns", async () => {
    const loadOverview = vi.fn(async () => ({ totalCount: 0 }))
    const loadMarketingEligible = vi.fn(async () => 12)
    const pageModule = createOperatorCampaignsPageModule(
      createAdapters({ loadOverview, loadMarketingEligible })
    )

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    expect(loadOverview).toHaveBeenCalledWith({ locationId: 42 })
    expect(loadMarketingEligible).toHaveBeenCalledWith({
      locationId: 42,
      overviewDateRange: DEFAULT_CAMPAIGNS_OVERVIEW_DATE_RANGE,
    })
    const snapshot = pageModule.getSnapshot()
    expect(snapshot.loadStatus).toBe("loaded")
    expect(snapshot.viewModel).toMatchObject({
      locationId: 42,
      locationName: "Camden",
      isTrueEmpty: true,
      dateRangeLabel: "Last 30 days",
      selectedDateRange: DEFAULT_CAMPAIGNS_OVERVIEW_DATE_RANGE,
      header: {
        createCampaignLabel: CAMPAIGNS_PAGE_COPY.createCampaign,
        useTemplateLabel: CAMPAIGNS_PAGE_COPY.useTemplate,
      },
      listEmpty: {
        title: CAMPAIGNS_PAGE_COPY.trueEmptyTitle,
        helper: CAMPAIGNS_PAGE_COPY.trueEmptyHelper,
        createCampaignLabel: CAMPAIGNS_PAGE_COPY.createCampaign,
        useTemplateLabel: CAMPAIGNS_PAGE_COPY.useTemplate,
      },
    })
    expect(snapshot.viewModel?.summary.kpis).toEqual([
      {
        id: "marketing-eligible",
        label: CAMPAIGNS_PAGE_COPY.marketingEligibleLabel,
        description: CAMPAIGNS_PAGE_COPY.marketingEligibleDescription,
        value: 12,
      },
      {
        id: "campaigns-in-flight",
        label: CAMPAIGNS_PAGE_COPY.campaignsInFlightLabel,
        description: CAMPAIGNS_PAGE_COPY.campaignsInFlightDescription,
        value: 3,
      },
      {
        id: "messages-sent",
        label: CAMPAIGNS_PAGE_COPY.messagesSentLabel,
        description: CAMPAIGNS_PAGE_COPY.messagesSentDescription,
        value: 1842,
      },
      {
        id: "campaign-attributed-redemptions",
        label: CAMPAIGNS_PAGE_COPY.campaignAttributedRedemptionsLabel,
        description: CAMPAIGNS_PAGE_COPY.campaignAttributedRedemptionsDescription,
        value: 0,
      },
    ])
  })

  it("surfaces load error and recovers on retry", async () => {
    const loadOverview = vi
      .fn()
      .mockRejectedValueOnce(new Error("network"))
      .mockResolvedValueOnce({ totalCount: 0 })
    const pageModule = createOperatorCampaignsPageModule(
      createAdapters({ loadOverview })
    )

    await pageModule.syncWorkspace({
      selectedLocationId: 7,
      locations: [{ id: 7, locationName: "Soho" }],
    })

    expect(pageModule.getSnapshot()).toMatchObject({
      loadStatus: "error",
      viewModel: null,
      loadError: CAMPAIGNS_LOAD_ERROR_MESSAGE,
    })

    await pageModule.retryLoad()

    expect(pageModule.getSnapshot()).toMatchObject({
      loadStatus: "loaded",
      loadError: null,
      viewModel: {
        locationId: 7,
        isTrueEmpty: true,
      },
    })
  })

  it("reloads when the selected Owned location changes", async () => {
    const loadOverview = vi.fn(async () => ({ totalCount: 0 }))
    const loadMarketingEligible = vi.fn(async () => 5)
    const pageModule = createOperatorCampaignsPageModule(
      createAdapters({ loadOverview, loadMarketingEligible })
    )

    await pageModule.syncWorkspace({
      selectedLocationId: 1,
      locations: [
        { id: 1, locationName: "Camden" },
        { id: 2, locationName: "Soho" },
      ],
    })
    await pageModule.syncWorkspace({
      selectedLocationId: 2,
      locations: [
        { id: 1, locationName: "Camden" },
        { id: 2, locationName: "Soho" },
      ],
    })

    expect(loadOverview).toHaveBeenNthCalledWith(1, { locationId: 1 })
    expect(loadOverview).toHaveBeenNthCalledWith(2, { locationId: 2 })
    expect(loadMarketingEligible).toHaveBeenNthCalledWith(1, {
      locationId: 1,
      overviewDateRange: DEFAULT_CAMPAIGNS_OVERVIEW_DATE_RANGE,
    })
    expect(loadMarketingEligible).toHaveBeenNthCalledWith(2, {
      locationId: 2,
      overviewDateRange: DEFAULT_CAMPAIGNS_OVERVIEW_DATE_RANGE,
    })
    expect(pageModule.getSnapshot().viewModel?.locationName).toBe("Soho")
  })

  it("refetches Marketing eligible on date-window change while sibling mock KPIs stay fixed", async () => {
    let range: CampaignsOverviewDateRange = DEFAULT_CAMPAIGNS_OVERVIEW_DATE_RANGE
    const loadOverview = vi.fn(async () => ({ totalCount: 0 }))
    const loadMarketingEligible = vi.fn(
      async (input: {
        locationId: number
        overviewDateRange: CampaignsOverviewDateRange
      }) => (input.overviewDateRange.kind === "all-time" ? 99 : 12)
    )
    const pageModule = createOperatorCampaignsPageModule(
      createAdapters({
        loadOverview,
        loadMarketingEligible,
        getCampaignsOverviewDateRange: () => range,
      })
    )

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    expect(loadOverview).toHaveBeenCalledTimes(1)
    expect(loadMarketingEligible).toHaveBeenCalledTimes(1)
    const firstKpis = pageModule.getSnapshot().viewModel?.summary.kpis
    expect(firstKpis?.find((kpi) => kpi.id === "marketing-eligible")?.value).toBe(
      12
    )
    const mockValuesBefore = firstKpis
      ?.filter((kpi) => kpi.id !== "marketing-eligible")
      .map((kpi) => ({ id: kpi.id, value: kpi.value }))

    range = { kind: "all-time" }
    await pageModule.reloadForOverviewDateRange()

    expect(loadOverview).toHaveBeenCalledTimes(1)
    expect(loadMarketingEligible).toHaveBeenCalledTimes(2)
    expect(loadMarketingEligible).toHaveBeenLastCalledWith({
      locationId: 42,
      overviewDateRange: { kind: "all-time" },
    })

    const snapshot = pageModule.getSnapshot()
    expect(snapshot.viewModel?.dateRangeLabel).toBe("All time")
    expect(snapshot.viewModel?.selectedDateRange).toEqual({ kind: "all-time" })
    expect(
      snapshot.viewModel?.summary.kpis.find(
        (kpi) => kpi.id === "marketing-eligible"
      )?.value
    ).toBe(99)
    expect(
      snapshot.viewModel?.summary.kpis
        .filter((kpi) => kpi.id !== "marketing-eligible")
        .map((kpi) => ({ id: kpi.id, value: kpi.value }))
    ).toEqual(mockValuesBefore)
  })
})
