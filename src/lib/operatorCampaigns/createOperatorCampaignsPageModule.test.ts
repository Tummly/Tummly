import { describe, expect, it, vi, type Mock } from "vitest"

import {
  CAMPAIGNS_LOAD_ERROR_MESSAGE,
  CAMPAIGNS_PAGE_COPY,
  createOperatorCampaignsPageModule,
  type OperatorCampaignsPageAdapters,
} from "@/lib/operatorCampaigns/createOperatorCampaignsPageModule"

function createAdapters(
  overrides: Partial<OperatorCampaignsPageAdapters> & {
    loadOverview?: Mock<OperatorCampaignsPageAdapters["loadOverview"]>
  } = {}
): OperatorCampaignsPageAdapters {
  return {
    loadOverview:
      overrides.loadOverview
      ?? vi.fn(async () => ({ totalCount: 0 })),
  }
}

describe("createOperatorCampaignsPageModule", () => {
  it("loads true-empty overview chrome for a location with no campaigns", async () => {
    const loadOverview = vi.fn(async () => ({ totalCount: 0 }))
    const pageModule = createOperatorCampaignsPageModule(
      createAdapters({ loadOverview })
    )

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    expect(loadOverview).toHaveBeenCalledWith({ locationId: 42 })
    const snapshot = pageModule.getSnapshot()
    expect(snapshot.loadStatus).toBe("loaded")
    expect(snapshot.viewModel).toMatchObject({
      locationId: 42,
      locationName: "Camden",
      isTrueEmpty: true,
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
    const pageModule = createOperatorCampaignsPageModule(
      createAdapters({ loadOverview })
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
    expect(pageModule.getSnapshot().viewModel?.locationName).toBe("Soho")
  })
})
