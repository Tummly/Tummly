import { describe, expect, it, vi, type Mock } from "vitest"

import {
  OFFERS_LOAD_ERROR_MESSAGE,
  createOperatorOffersPageModule,
  type OperatorOffersPageAdapters,
} from "@/lib/operatorOffers/createOperatorOffersPageModule"
import { emptySelection } from "@/lib/operatorFilterSheet"
import { offersFilterSheetSchema } from "@/lib/operatorOffers/offersFilterSheetSchema"
import { OFFERS_PAGE_COPY } from "@/lib/operatorOffers/offersPresentation"
import { NEEDS_ATTENTION_EMPTY_COPY } from "@/lib/operatorHome/operatorHomeSectionPresentation"
import { DEFAULT_HOME_PERFORMANCE_DATE_RANGE } from "@/lib/operatorHome/homePerformanceDateRange"
import type {
  CatalogOfferDetail,
  CatalogOffersListItem,
  CatalogOffersListResponse,
} from "@/types/operatorCampaigns"

function emptyListResponse(
  overrides: Partial<CatalogOffersListResponse> = {}
): CatalogOffersListResponse {
  return {
    success: true,
    items: [],
    totalCount: 0,
    page: 1,
    pageSize: 25,
    tabCounts: {
      all: 0,
      needsAttention: 0,
      drafts: 0,
      inFlight: 0,
      sent: 0,
    },
    ...overrides,
  }
}

function offerListItem(
  overrides: Partial<CatalogOffersListItem> & { id: number; title: string }
): CatalogOffersListItem {
  return {
    locationId: 42,
    status: "draft",
    offerType: "percentage_discount",
    validity: "14_days_after_issue",
    expiryDate: null,
    attachKinds: [],
    lifetimeClaims: 0,
    lifetimeRedeemed: 0,
    createdAt: "2026-08-01T10:00:00.000Z",
    updatedAt: "2026-08-01T10:00:00.000Z",
    ...overrides,
  }
}

const OVERVIEW_NOW_MS = Date.parse("2026-08-22T12:00:00.000Z")

function createAdapters(
  overrides: Partial<OperatorOffersPageAdapters> & {
    listCatalogOffers?: Mock<OperatorOffersPageAdapters["listCatalogOffers"]>
  } = {}
): OperatorOffersPageAdapters {
  return {
    listCatalogOffers:
      overrides.listCatalogOffers
      ?? vi.fn(async () => emptyListResponse()),
    listOpenVoidAttention: overrides.listOpenVoidAttention,
    getOffersPerformance: overrides.getOffersPerformance,
    nowMs: overrides.nowMs ?? (() => OVERVIEW_NOW_MS),
    utcOffsetMinutes: overrides.utcOffsetMinutes ?? 0,
    debounceMs: overrides.debounceMs ?? 0,
    createOffer: overrides.createOffer,
    updateOffer: overrides.updateOffer,
    getOffer: overrides.getOffer,
    pauseOffer: overrides.pauseOffer,
    resumeOffer: overrides.resumeOffer,
    archiveOffer: overrides.archiveOffer,
    duplicateOffer: overrides.duplicateOffer,
  }
}

function kpiPrimary(
  pageModule: ReturnType<typeof createOperatorOffersPageModule>,
  id: string
): string | undefined {
  return pageModule
    .getSnapshot()
    .viewModel?.performance.kpis.find((kpi) => kpi.id === id)?.primaryText
}

function closedOfferTemplatePickerSnapshot() {
  return {
    open: false,
    loadStatus: "idle" as const,
    loadError: null,
    viewModel: null,
  }
}

function catalogDetail(
  overrides: Partial<CatalogOfferDetail> = {}
): CatalogOfferDetail {
  return {
    id: 88,
    locationId: 42,
    status: "active",
    offerType: "percentage_discount",
    title: "10% off",
    description: "Ten percent off.",
    validity: "30_days_after_issue",
    expiryDate: null,
    discountPercentage: 10,
    discountAmount: null,
    freeItemText: null,
    purchaseRequirement: null,
    minimumSpend: null,
    additionalExclusions: null,
    replacementItemText: null,
    staffInstructions: "Ask for the code.",
    issueCount: 0,
    createdAt: "2026-08-09T00:00:00Z",
    updatedAt: "2026-08-09T00:00:00Z",
    ...overrides,
  }
}

describe("createOperatorOffersPageModule", () => {
  it("starts idle with an empty snapshot", () => {
    const pageModule = createOperatorOffersPageModule(createAdapters())

    expect(pageModule.getSnapshot()).toEqual({
      loadStatus: "idle",
      tabContentStatus: "ready",
      viewModel: null,
      loadError: null,
      createOfferDrawer: null,
      offerTemplatePicker: closedOfferTemplatePickerSnapshot(),
      pendingEditOfferSave: null,
      pendingNavigation: null,
    })
  })

  it("loads true-empty list chrome for a location with no offers", async () => {
    const listCatalogOffers = vi.fn(async () => emptyListResponse())
    const pageModule = createOperatorOffersPageModule(
      createAdapters({ listCatalogOffers })
    )

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    expect(listCatalogOffers).toHaveBeenCalledWith(
      expect.objectContaining({
        locationId: 42,
        view: "all",
        sort: "recent-activity",
        page: 1,
        pageSize: 25,
      })
    )

    const snapshot = pageModule.getSnapshot()
    expect(snapshot.loadStatus).toBe("loaded")
    expect(snapshot.viewModel).toMatchObject({
      locationId: 42,
      locationName: "Camden",
      isTrueEmpty: true,
      header: {
        createOfferLabel: OFFERS_PAGE_COPY.createOffer,
        openStaffRedeemLabel: OFFERS_PAGE_COPY.openStaffRedeem,
        viewRedemptionLogLabel: OFFERS_PAGE_COPY.viewRedemptionLog,
      },
      performance: {
        selectedRange: DEFAULT_HOME_PERFORMANCE_DATE_RANGE,
        dateRangeLabel: "Last 7 days",
        kpis: [
          {
            id: "active-offers",
            label: "Active offers",
            primaryText: "0",
            helperText:
              "Offers currently available for valid issuance or redemption.",
          },
          {
            id: "offers-issued",
            label: "Offers issued",
            primaryText: "0",
            helperText:
              "Guest-specific passes issued during the selected period.",
          },
          {
            id: "claims",
            label: "Claims",
            primaryText: "0",
            helperText:
              "Issued offers activated or opened by guests during this period.",
          },
          {
            id: "redemptions",
            label: "Redemptions",
            primaryText: "0",
            helperText:
              "Successful staff-confirmed redemptions during this period.",
          },
          {
            id: "claim-to-redemption-rate",
            label: "Claim-to-redemption rate",
            primaryText: "—",
            helperText:
              "Share of claims in this period that staff redeemed.",
          },
        ],
      },
      needsAttention: {
        title: OFFERS_PAGE_COPY.needsAttentionTitle,
        subtitle: OFFERS_PAGE_COPY.needsAttentionSubtitle,
        emptyCopy: NEEDS_ATTENTION_EMPTY_COPY,
        isEmpty: true,
        rows: [],
        showViewAll: false,
        viewAllLabel: OFFERS_PAGE_COPY.viewAllInNeedsAttention,
      },
      list: {
        showListChrome: false,
        empty: {
          kind: "true-empty",
          title: OFFERS_PAGE_COPY.trueEmptyTitle,
        },
        tabs: expect.arrayContaining([
          expect.objectContaining({ id: "all", showCount: false }),
          expect.objectContaining({ id: "drafts", showCount: true }),
        ]),
      },
    })
  })

  it("setPerformanceDateRange updates range and label and republishes", async () => {
    const pageModule = createOperatorOffersPageModule(createAdapters())
    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    const labels: string[] = []
    const unsubscribe = pageModule.subscribe(() => {
      const label =
        pageModule.getSnapshot().viewModel?.performance.dateRangeLabel
      if (label != null) {
        labels.push(label)
      }
    })

    await pageModule.setPerformanceDateRange({
      kind: "preset",
      presetId: "last30",
    })

    const performance = pageModule.getSnapshot().viewModel?.performance
    expect(performance?.selectedRange).toEqual({
      kind: "preset",
      presetId: "last30",
    })
    expect(performance?.dateRangeLabel).toBe("Last 30 days")
    expect(labels).toEqual(["Last 30 days"])

    unsubscribe()
  })

  it("keeps Active offers unchanged when only the date range changes", async () => {
    const pageModule = createOperatorOffersPageModule(createAdapters())
    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    const before = pageModule
      .getSnapshot()
      .viewModel?.performance.kpis.find((kpi) => kpi.id === "active-offers")

    await pageModule.setPerformanceDateRange({
      kind: "preset",
      presetId: "thisMonth",
    })

    const after = pageModule
      .getSnapshot()
      .viewModel?.performance.kpis.find((kpi) => kpi.id === "active-offers")

    expect(pageModule.getSnapshot().viewModel?.performance.dateRangeLabel).toBe(
      "This month"
    )
    expect(after).toEqual(before)
    expect(after?.primaryText).toBe("0")
  })

  it("loads Active offers and window KPIs from getOffersPerformance on sync", async () => {
    const getOffersPerformance = vi.fn(
      async () => ({
        activeOffers: 4,
        offersIssued: 12,
        claims: 8,
        redemptions: 2,
      })
    )
    const pageModule = createOperatorOffersPageModule(
      createAdapters({ getOffersPerformance })
    )

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    expect(getOffersPerformance).toHaveBeenCalledTimes(1)
    expect(getOffersPerformance).toHaveBeenCalledWith(
      42,
      expect.any(String),
      expect.any(String)
    )
    const firstCall = getOffersPerformance.mock.calls[0]!
    const from = firstCall[1]
    const to = firstCall[2]
    const spanMs = new Date(to).getTime() - new Date(from).getTime()
    expect(spanMs).toBeGreaterThanOrEqual(6 * 24 * 60 * 60 * 1000)
    expect(spanMs).toBeLessThanOrEqual(7 * 24 * 60 * 60 * 1000)

    expect(kpiPrimary(pageModule, "active-offers")).toBe("4")
    expect(kpiPrimary(pageModule, "offers-issued")).toBe("12")
    expect(kpiPrimary(pageModule, "claims")).toBe("8")
    expect(kpiPrimary(pageModule, "redemptions")).toBe("2")
    expect(kpiPrimary(pageModule, "claim-to-redemption-rate")).toBe("25%")
  })

  it("refetches window KPIs when the Performance date range changes", async () => {
    const getOffersPerformance = vi
      .fn(async (locationId: number, from: string, to: string) => {
        void locationId
        void from
        void to
        return {
          activeOffers: 3,
          offersIssued: 5,
          claims: 0,
          redemptions: 0,
        }
      })
      .mockResolvedValueOnce({
        activeOffers: 3,
        offersIssued: 5,
        claims: 0,
        redemptions: 0,
      })
      .mockResolvedValueOnce({
        activeOffers: 3,
        offersIssued: 20,
        claims: 10,
        redemptions: 4,
      })

    const pageModule = createOperatorOffersPageModule(
      createAdapters({ getOffersPerformance })
    )
    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })
    expect(kpiPrimary(pageModule, "offers-issued")).toBe("5")
    expect(kpiPrimary(pageModule, "claim-to-redemption-rate")).toBe("—")

    await pageModule.setPerformanceDateRange({
      kind: "preset",
      presetId: "last30",
    })

    expect(getOffersPerformance).toHaveBeenCalledTimes(2)
    const secondCall = getOffersPerformance.mock.calls[1]!
    const from = secondCall[1]
    const to = secondCall[2]
    const spanMs = new Date(to).getTime() - new Date(from).getTime()
    expect(spanMs).toBeGreaterThanOrEqual(29 * 24 * 60 * 60 * 1000)
    expect(spanMs).toBeLessThanOrEqual(30 * 24 * 60 * 60 * 1000)

    expect(pageModule.getSnapshot().viewModel?.performance.dateRangeLabel).toBe(
      "Last 30 days"
    )
    expect(kpiPrimary(pageModule, "active-offers")).toBe("3")
    expect(kpiPrimary(pageModule, "offers-issued")).toBe("20")
    expect(kpiPrimary(pageModule, "claims")).toBe("10")
    expect(kpiPrimary(pageModule, "redemptions")).toBe("4")
    expect(kpiPrimary(pageModule, "claim-to-redemption-rate")).toBe("40%")
  })

  it("refetches Performance for This month and Custom without remounting the list", async () => {
    const listCatalogOffers = vi.fn(async () => emptyListResponse())
    const getOffersPerformance = vi.fn(
      async (locationId: number, from: string, to: string) => {
        void locationId
        void from
        void to
        return {
          activeOffers: 1,
          offersIssued: 1,
          claims: 1,
          redemptions: 0,
        }
      }
    )
    const pageModule = createOperatorOffersPageModule(
      createAdapters({ listCatalogOffers, getOffersPerformance })
    )
    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })
    const listCallsAfterSync = listCatalogOffers.mock.calls.length
    expect(listCallsAfterSync).toBeGreaterThanOrEqual(1)

    await pageModule.setPerformanceDateRange({
      kind: "preset",
      presetId: "thisMonth",
    })
    expect(listCatalogOffers).toHaveBeenCalledTimes(listCallsAfterSync)
    expect(getOffersPerformance).toHaveBeenCalledTimes(2)
    const monthFrom = getOffersPerformance.mock.calls[1]![1]
    expect(new Date(monthFrom).getDate()).toBe(1)

    await pageModule.setPerformanceDateRange({
      kind: "custom",
      startDate: "2026-07-12",
      endDate: "2026-07-18",
    })
    expect(listCatalogOffers).toHaveBeenCalledTimes(listCallsAfterSync)
    expect(getOffersPerformance).toHaveBeenCalledTimes(3)
    const customCall = getOffersPerformance.mock.calls[2]!
    const customFrom = customCall[1]
    const customTo = customCall[2]
    expect(new Date(customFrom).getFullYear()).toBe(2026)
    expect(new Date(customFrom).getMonth()).toBe(6)
    expect(new Date(customFrom).getDate()).toBe(12)
    expect(new Date(customTo).getDate()).toBe(19)
  })

  it("clears the view model when selected location is null", async () => {
    const pageModule = createOperatorOffersPageModule(createAdapters())

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })
    await pageModule.syncWorkspace({
      selectedLocationId: null,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    expect(pageModule.getSnapshot()).toEqual({
      loadStatus: "idle",
      tabContentStatus: "ready",
      viewModel: null,
      loadError: null,
      createOfferDrawer: null,
      offerTemplatePicker: closedOfferTemplatePickerSnapshot(),
      pendingEditOfferSave: null,
      pendingNavigation: null,
    })
  })

  it("errors when the selected location is missing from the workspace", async () => {
    const pageModule = createOperatorOffersPageModule(createAdapters())

    await pageModule.syncWorkspace({
      selectedLocationId: 99,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    expect(pageModule.getSnapshot()).toEqual({
      loadStatus: "error",
      tabContentStatus: "ready",
      viewModel: null,
      loadError: OFFERS_LOAD_ERROR_MESSAGE,
      createOfferDrawer: null,
      offerTemplatePicker: closedOfferTemplatePickerSnapshot(),
      pendingEditOfferSave: null,
      pendingNavigation: null,
    })
  })

  it("surfaces load error and recovers on retry", async () => {
    const listCatalogOffers = vi
      .fn()
      .mockRejectedValueOnce(new Error("network"))
      .mockResolvedValue(emptyListResponse())
    const pageModule = createOperatorOffersPageModule(
      createAdapters({ listCatalogOffers })
    )

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })
    expect(pageModule.getSnapshot().loadStatus).toBe("error")

    await pageModule.retryLoad()
    expect(pageModule.getSnapshot().loadStatus).toBe("loaded")
    expect(pageModule.getSnapshot().viewModel?.isTrueEmpty).toBe(true)
  })

  it("selects view-scoped empty when All has drafts but Needs attention is empty", async () => {
    const listCatalogOffers = vi.fn(
      async (params: { view?: string }) => {
        if (params.view === "needs-attention") {
          return emptyListResponse({
            totalCount: 0,
            tabCounts: {
              all: 2,
              drafts: 2,
              needsAttention: 0,
              inFlight: 0,
              sent: 0,
            },
          })
        }
        return emptyListResponse({
          totalCount: 2,
          items: [
            offerListItem({ id: 1, title: "A" }),
            offerListItem({ id: 2, title: "B" }),
          ],
          tabCounts: {
            all: 2,
            drafts: 2,
            needsAttention: 0,
            inFlight: 0,
            sent: 0,
          },
        })
      }
    )
    const pageModule = createOperatorOffersPageModule(
      createAdapters({ listCatalogOffers })
    )

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })
    await pageModule.setListView("needs-attention")

    expect(listCatalogOffers).toHaveBeenCalledWith(
      expect.objectContaining({ view: "needs-attention", page: 1 })
    )
    expect(pageModule.getSnapshot().viewModel?.list.empty?.kind).toBe(
      "view-scoped"
    )
    expect(pageModule.getSnapshot().viewModel?.list.showListChrome).toBe(true)
  })

  it("hides old rows and empty content while a cold list tab loads", async () => {
    let resolveDrafts!: (response: CatalogOffersListResponse) => void
    const draftsResponse = new Promise<CatalogOffersListResponse>((resolve) => {
      resolveDrafts = resolve
    })
    const listCatalogOffers = vi.fn(
      async (params: { view?: string }) => {
        if (params.view === "drafts") {
          return draftsResponse
        }
        return emptyListResponse({
          totalCount: 1,
          items: [offerListItem({ id: 1, title: "All offer" })],
          tabCounts: {
            all: 1,
            drafts: 1,
            needsAttention: 0,
            inFlight: 0,
            sent: 0,
          },
        })
      }
    )
    const pageModule = createOperatorOffersPageModule(
      createAdapters({ listCatalogOffers })
    )
    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    const switchPromise = pageModule.setListView("drafts")
    expect(pageModule.getSnapshot()).toMatchObject({
      tabContentStatus: "loading",
      viewModel: {
        list: {
          activeViewId: "drafts",
          rows: [],
          empty: null,
          tabs: expect.arrayContaining([
            expect.objectContaining({ id: "all", count: 1 }),
          ]),
        },
      },
    })

    resolveDrafts(
      emptyListResponse({
        totalCount: 1,
        items: [offerListItem({ id: 2, title: "Draft offer" })],
        tabCounts: {
          all: 1,
          drafts: 1,
          needsAttention: 0,
          inFlight: 0,
          sent: 0,
        },
      })
    )
    await switchPromise
    expect(pageModule.getSnapshot().tabContentStatus).toBe("ready")
    expect(pageModule.getSnapshot().viewModel?.list.rows[0]?.title).toBe(
      "Draft offer"
    )
  })

  it("shows cached tab content while a warm tab refreshes", async () => {
    let allCallCount = 0
    let resolveWarmRefresh!: (response: CatalogOffersListResponse) => void
    const warmRefresh = new Promise<CatalogOffersListResponse>((resolve) => {
      resolveWarmRefresh = resolve
    })
    const listCatalogOffers = vi.fn(
      async (params: { view?: string }) => {
        if (params.view === "all") {
          allCallCount += 1
          if (allCallCount === 1) {
            return emptyListResponse({
              totalCount: 1,
              items: [offerListItem({ id: 1, title: "Cached all offer" })],
            })
          }
          return warmRefresh
        }
        return emptyListResponse({
          totalCount: 1,
          items: [offerListItem({ id: 2, title: "Draft offer" })],
        })
      }
    )
    const pageModule = createOperatorOffersPageModule(
      createAdapters({ listCatalogOffers })
    )
    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })
    await pageModule.setListView("drafts")

    const returnPromise = pageModule.setListView("all")
    expect(pageModule.getSnapshot().tabContentStatus).toBe("refreshing")
    expect(pageModule.getSnapshot().viewModel?.list.rows[0]?.title).toBe(
      "Cached all offer"
    )

    resolveWarmRefresh(
      emptyListResponse({
        totalCount: 1,
        items: [offerListItem({ id: 3, title: "Fresh all offer" })],
      })
    )
    await returnPromise
    expect(pageModule.getSnapshot().tabContentStatus).toBe("ready")
    expect(pageModule.getSnapshot().viewModel?.list.rows[0]?.title).toBe(
      "Fresh all offer"
    )
  })

  it("caches rapid tab responses without replacing the active tab", async () => {
    let resolveDrafts!: (response: CatalogOffersListResponse) => void
    let resolveSent!: (response: CatalogOffersListResponse) => void
    const drafts = new Promise<CatalogOffersListResponse>((resolve) => {
      resolveDrafts = resolve
    })
    const sent = new Promise<CatalogOffersListResponse>((resolve) => {
      resolveSent = resolve
    })
    let draftsCalls = 0
    const listCatalogOffers = vi.fn(
      async (params: { view?: string }) => {
        if (params.view === "drafts") {
          draftsCalls += 1
          if (draftsCalls === 1) {
            return drafts
          }
          return new Promise<CatalogOffersListResponse>(() => undefined)
        }
        if (params.view === "sent") {
          return sent
        }
        return emptyListResponse()
      }
    )
    const pageModule = createOperatorOffersPageModule(
      createAdapters({ listCatalogOffers })
    )
    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    const draftsPromise = pageModule.setListView("drafts")
    const sentPromise = pageModule.setListView("sent")
    resolveDrafts(
      emptyListResponse({
        totalCount: 1,
        items: [offerListItem({ id: 2, title: "Draft response" })],
      })
    )
    await draftsPromise
    expect(pageModule.getSnapshot().viewModel?.list.activeViewId).toBe("sent")
    expect(pageModule.getSnapshot().viewModel?.list.rows).toEqual([])

    resolveSent(
      emptyListResponse({
        totalCount: 1,
        items: [offerListItem({ id: 3, title: "Sent response" })],
      })
    )
    await sentPromise
    expect(pageModule.getSnapshot().viewModel?.list.rows[0]?.title).toBe(
      "Sent response"
    )

    void pageModule.setListView("drafts")
    expect(pageModule.getSnapshot().tabContentStatus).toBe("refreshing")
    expect(pageModule.getSnapshot().viewModel?.list.rows[0]?.title).toBe(
      "Draft response"
    )
  })

  it("ignores an older same-view response that lands after a newer one", async () => {
    const draftsResolvers: Array<
      (response: CatalogOffersListResponse) => void
    > = []
    const listCatalogOffers = vi.fn(
      async (params: { view?: string }) => {
        if (params.view === "drafts") {
          return new Promise<CatalogOffersListResponse>((resolve) => {
            draftsResolvers.push(resolve)
          })
        }
        if (params.view === "all") {
          return emptyListResponse({
            totalCount: 1,
            items: [offerListItem({ id: 1, title: "All offer" })],
          })
        }
        return emptyListResponse()
      }
    )
    const pageModule = createOperatorOffersPageModule(
      createAdapters({ listCatalogOffers })
    )
    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    const olderDrafts = pageModule.setListView("drafts")
    void pageModule.setListView("all")
    const newerDrafts = pageModule.setListView("drafts")
    await vi.waitFor(() => {
      expect(draftsResolvers).toHaveLength(2)
    })

    draftsResolvers[1]!(
      emptyListResponse({
        totalCount: 1,
        items: [offerListItem({ id: 3, title: "Newer drafts" })],
      })
    )
    await newerDrafts
    expect(pageModule.getSnapshot().viewModel?.list.rows[0]?.title).toBe(
      "Newer drafts"
    )

    draftsResolvers[0]!(
      emptyListResponse({
        totalCount: 1,
        items: [offerListItem({ id: 2, title: "Older drafts" })],
      })
    )
    await olderDrafts
    expect(pageModule.getSnapshot().viewModel?.list.rows[0]?.title).toBe(
      "Newer drafts"
    )

    await pageModule.setListView("all")
    void pageModule.setListView("drafts")
    expect(pageModule.getSnapshot().tabContentStatus).toBe("refreshing")
    expect(pageModule.getSnapshot().viewModel?.list.rows[0]?.title).toBe(
      "Newer drafts"
    )
  })

  it("keeps cached content and returns to ready when a warm refresh fails", async () => {
    let allCalls = 0
    const listCatalogOffers = vi.fn(
      async (params: { view?: string }) => {
        if (params.view === "all") {
          allCalls += 1
          if (allCalls > 1) {
            throw new Error("network")
          }
          return emptyListResponse({
            totalCount: 1,
            items: [offerListItem({ id: 1, title: "Cached all offer" })],
          })
        }
        return emptyListResponse({
          totalCount: 1,
          items: [offerListItem({ id: 2, title: "Draft offer" })],
        })
      }
    )
    const pageModule = createOperatorOffersPageModule(
      createAdapters({ listCatalogOffers })
    )
    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })
    await pageModule.setListView("drafts")

    await pageModule.setListView("all")

    expect(pageModule.getSnapshot().tabContentStatus).toBe("ready")
    expect(pageModule.getSnapshot().loadStatus).toBe("loaded")
    expect(pageModule.getSnapshot().loadError).toBeNull()
    expect(pageModule.getSnapshot().viewModel?.list.rows[0]?.title).toBe(
      "Cached all offer"
    )
  })

  it("treats a cleared tab cache as a cold miss", async () => {
    let allCalls = 0
    const listCatalogOffers = vi.fn(
      async (params: { view?: string }) => {
        if (params.view === "all") {
          allCalls += 1
          if (allCalls > 1) {
            return new Promise<CatalogOffersListResponse>(() => undefined)
          }
          return emptyListResponse({
            totalCount: 1,
            items: [offerListItem({ id: 1, title: "Cached all offer" })],
          })
        }
        return emptyListResponse({
          totalCount: 1,
          items: [offerListItem({ id: 2, title: "Draft offer" })],
        })
      }
    )
    const pageModule = createOperatorOffersPageModule(
      createAdapters({ listCatalogOffers })
    )
    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })
    await pageModule.setListView("drafts")

    pageModule.clearTabCache()
    void pageModule.setListView("all")

    expect(pageModule.getSnapshot().tabContentStatus).toBe("loading")
    expect(pageModule.getSnapshot().viewModel?.list.activeViewId).toBe("all")
    expect(pageModule.getSnapshot().viewModel?.list.rows).toEqual([])
    expect(pageModule.getSnapshot().viewModel?.list.empty).toBeNull()
  })

  it("selects filter-search empty when search returns no rows", async () => {
    const listCatalogOffers = vi.fn(
      async (params: { q?: string }) => {
        if (params.q === "xyz") {
          return emptyListResponse({
            totalCount: 0,
            tabCounts: {
              all: 1,
              drafts: 1,
              needsAttention: 0,
              inFlight: 0,
              sent: 0,
            },
          })
        }
        return emptyListResponse({
          totalCount: 1,
          items: [offerListItem({ id: 1, title: "Brunch" })],
          tabCounts: {
            all: 1,
            drafts: 1,
            needsAttention: 0,
            inFlight: 0,
            sent: 0,
          },
        })
      }
    )
    const pageModule = createOperatorOffersPageModule(
      createAdapters({ listCatalogOffers, debounceMs: 0 })
    )

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    pageModule.setSearchQuery("xyz")
    await vi.waitFor(() => {
      expect(listCatalogOffers).toHaveBeenCalledWith(
        expect.objectContaining({ q: "xyz", page: 1 })
      )
    })
    expect(pageModule.getSnapshot().viewModel?.list.empty?.kind).toBe(
      "filter-search"
    )
  })

  it("viewAllOffers resets to All and clears search", async () => {
    const listCatalogOffers = vi.fn(async () =>
      emptyListResponse({
        totalCount: 1,
        items: [offerListItem({ id: 1, title: "Brunch" })],
        tabCounts: {
          all: 1,
          drafts: 1,
          needsAttention: 0,
          inFlight: 0,
          sent: 0,
        },
      })
    )
    const pageModule = createOperatorOffersPageModule(
      createAdapters({ listCatalogOffers, debounceMs: 0 })
    )

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })
    await pageModule.setListView("drafts")
    pageModule.setSearchQuery("miss")
    await vi.waitFor(() => {
      expect(listCatalogOffers).toHaveBeenCalledWith(
        expect.objectContaining({ view: "drafts", q: "miss" })
      )
    })

    await pageModule.viewAllOffers()
    expect(listCatalogOffers).toHaveBeenCalledWith(
      expect.objectContaining({
        view: "all",
        q: undefined,
        page: 1,
      })
    )
  })

  it("maps list items into table rows", async () => {
    const listCatalogOffers = vi.fn(async () =>
      emptyListResponse({
        totalCount: 1,
        items: [
          offerListItem({
            id: 9,
            title: "10% off",
            status: "active",
            attachKinds: ["campaign", "recovery"],
            lifetimeClaims: 8,
            lifetimeRedeemed: 2,
          }),
        ],
        tabCounts: {
          all: 1,
          drafts: 0,
          needsAttention: 0,
          inFlight: 1,
          sent: 0,
        },
      })
    )
    const pageModule = createOperatorOffersPageModule(
      createAdapters({ listCatalogOffers })
    )

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    expect(pageModule.getSnapshot().viewModel?.list.rows).toEqual([
      expect.objectContaining({
        id: 9,
        title: "10% off",
        attachSubline: "Campaign, Recovery",
        statusLabel: "Active",
        claimsLabel: "8",
        redeemedLabel: "2",
        redemptionRateLabel: "25%",
        controlsLabel: "Unique code · 14-day expiry",
      }),
    ])
  })

  it("applyFilters sends Status + attachSource and resets page", async () => {
    const listCatalogOffers = vi.fn(async () =>
      emptyListResponse({
        totalCount: 1,
        items: [offerListItem({ id: 1, title: "Draft" })],
        tabCounts: {
          all: 1,
          drafts: 1,
          needsAttention: 0,
          inFlight: 0,
          sent: 0,
        },
      })
    )
    const pageModule = createOperatorOffersPageModule(
      createAdapters({ listCatalogOffers })
    )

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    const schema = offersFilterSheetSchema()
    pageModule.applyFilters({
      ...emptySelection(schema),
      status: { kind: "multi-select", ids: ["draft"] },
      attachSource: { kind: "multi-select", ids: ["campaign"] },
    })

    await vi.waitFor(() => {
      expect(listCatalogOffers).toHaveBeenCalledWith(
        expect.objectContaining({
          status: ["draft"],
          attachSource: ["campaign"],
          page: 1,
        })
      )
      expect(
        pageModule.getSnapshot().viewModel?.list.filterChipCount
      ).toBeGreaterThan(0)
    })
  })

  it("setSortId resets page and requests sort key", async () => {
    const listCatalogOffers = vi.fn(async () =>
      emptyListResponse({
        totalCount: 1,
        items: [offerListItem({ id: 1, title: "A" })],
        tabCounts: {
          all: 1,
          drafts: 1,
          needsAttention: 0,
          inFlight: 0,
          sent: 0,
        },
      })
    )
    const pageModule = createOperatorOffersPageModule(
      createAdapters({ listCatalogOffers })
    )

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    pageModule.setSortId("title-az")
    await vi.waitFor(() => {
      expect(listCatalogOffers).toHaveBeenCalledWith(
        expect.objectContaining({ sort: "title-az", page: 1 })
      )
    })
  })

  it("confirmPendingLifecycleAction calls pauseOffer and refreshes list", async () => {
    const listCatalogOffers = vi.fn(async () =>
      emptyListResponse({
        totalCount: 1,
        items: [
          offerListItem({
            id: 3,
            title: "Pause me",
            status: "active",
          }),
        ],
        tabCounts: {
          all: 1,
          drafts: 0,
          needsAttention: 0,
          inFlight: 1,
          sent: 0,
        },
      })
    )
    const pauseOffer = vi.fn(async () =>
      catalogDetail({ id: 3, title: "Pause me", status: "paused" })
    )
    const pageModule = createOperatorOffersPageModule(
      createAdapters({ listCatalogOffers, pauseOffer })
    )

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    const callsBefore = listCatalogOffers.mock.calls.length
    pageModule.requestRowAction(3, "pause")
    expect(pageModule.getSnapshot().viewModel?.pendingLifecycleAction).toEqual(
      expect.objectContaining({
        offerId: 3,
        actionId: "pause",
        title: OFFERS_PAGE_COPY.pauseConfirmTitle,
      })
    )

    await pageModule.confirmPendingLifecycleAction()
    expect(pauseOffer).toHaveBeenCalledTimes(1)
    expect(pauseOffer).toHaveBeenCalledWith(3)
    expect(
      pageModule.getSnapshot().viewModel?.pendingLifecycleAction
    ).toBeNull()
    expect(listCatalogOffers.mock.calls.length).toBeGreaterThan(callsBefore)
  })

  it("confirmPendingLifecycleAction runs resume, archive, and duplicate writes", async () => {
    const listCatalogOffers = vi.fn(async () =>
      emptyListResponse({
        totalCount: 1,
        items: [
          offerListItem({
            id: 5,
            title: "Lifecycle me",
            status: "paused",
          }),
        ],
        tabCounts: {
          all: 1,
          drafts: 0,
          needsAttention: 0,
          inFlight: 1,
          sent: 0,
        },
      })
    )
    const resumeOffer = vi.fn(async () =>
      catalogDetail({ id: 5, status: "active" })
    )
    const archiveOffer = vi.fn(async () =>
      catalogDetail({ id: 5, status: "archived" })
    )
    const duplicateOffer = vi.fn(async () =>
      catalogDetail({ id: 99, status: "draft", title: "Lifecycle me (copy)" })
    )
    const pageModule = createOperatorOffersPageModule(
      createAdapters({
        listCatalogOffers,
        resumeOffer,
        archiveOffer,
        duplicateOffer,
      })
    )

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    pageModule.requestRowAction(5, "resume")
    await pageModule.confirmPendingLifecycleAction()
    expect(resumeOffer).toHaveBeenCalledWith(5)

    pageModule.requestRowAction(5, "archive")
    await pageModule.confirmPendingLifecycleAction()
    expect(archiveOffer).toHaveBeenCalledWith(5)

    pageModule.requestRowAction(5, "duplicate")
    await pageModule.confirmPendingLifecycleAction()
    expect(duplicateOffer).toHaveBeenCalledWith(5)
    expect(pageModule.getSnapshot().createOfferDrawer).toBeNull()
    expect(pageModule.getSnapshot().pendingNavigation).toBeNull()
  })

  it("lifecycle write failure clears pending and does not throw", async () => {
    const listCatalogOffers = vi.fn(async () =>
      emptyListResponse({
        totalCount: 1,
        items: [
          offerListItem({
            id: 3,
            title: "Pause me",
            status: "active",
          }),
        ],
        tabCounts: {
          all: 1,
          drafts: 0,
          needsAttention: 0,
          inFlight: 1,
          sent: 0,
        },
      })
    )
    const pauseOffer = vi.fn(async () => {
      throw new Error("offline")
    })
    const pageModule = createOperatorOffersPageModule(
      createAdapters({ listCatalogOffers, pauseOffer })
    )

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    const callsBefore = listCatalogOffers.mock.calls.length
    pageModule.requestRowAction(3, "pause")
    await pageModule.confirmPendingLifecycleAction()
    expect(
      pageModule.getSnapshot().viewModel?.pendingLifecycleAction
    ).toBeNull()
    expect(listCatalogOffers.mock.calls.length).toBe(callsBefore)
    expect(pageModule.getSnapshot().loadStatus).toBe("loaded")
  })

  it("treats View as a no-op and Edit opens the shared drawer", async () => {
    const listCatalogOffers = vi.fn(async () =>
      emptyListResponse({
        totalCount: 1,
        items: [offerListItem({ id: 1, title: "Draft", status: "draft" })],
        tabCounts: {
          all: 1,
          drafts: 1,
          needsAttention: 0,
          inFlight: 0,
          sent: 0,
        },
      })
    )
    const getOffer = vi.fn(async () => catalogDetail({ id: 1, title: "Draft" }))
    const pageModule = createOperatorOffersPageModule(
      createAdapters({ listCatalogOffers, getOffer })
    )

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    pageModule.requestRowAction(1, "view")
    expect(
      pageModule.getSnapshot().viewModel?.pendingLifecycleAction
    ).toBeNull()
    expect(pageModule.getSnapshot().createOfferDrawer).toBeNull()

    pageModule.requestRowAction(1, "edit")
    await vi.waitFor(() => {
      expect(getOffer).toHaveBeenCalledWith(1)
    })
    expect(pageModule.getSnapshot().createOfferDrawer?.mode).toBe("edit")
    expect(pageModule.getSnapshot().createOfferDrawer?.draft.title).toBe(
      "Draft"
    )
    expect(
      pageModule.getSnapshot().viewModel?.pendingLifecycleAction
    ).toBeNull()
  })

  it("create offer success closes drawer and does not navigate", async () => {
    const createOffer = vi.fn(async () => ({
      id: 88,
      locationId: 42,
      status: "active" as const,
      offerType: "percentage_discount",
      title: "10% off",
      description: "Ten percent off your next visit.",
      validity: "30_days_after_issue",
      expiryDate: null,
      discountPercentage: 10,
      discountAmount: null,
      freeItemText: null,
      purchaseRequirement: null,
      minimumSpend: null,
      additionalExclusions: null,
      replacementItemText: null,
      staffInstructions: null,
      issueCount: 0,
      createdAt: "2026-08-09T00:00:00Z",
      updatedAt: "2026-08-09T00:00:00Z",
    }))

    const pageModule = createOperatorOffersPageModule(
      createAdapters({ createOffer })
    )

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    pageModule.openCreateOfferDrawer()
    expect(pageModule.getSnapshot().createOfferDrawer).toMatchObject({
      open: true,
      mode: "create",
      locationSubtitle: "Camden",
      canConfirm: false,
      saveGated: false,
    })

    pageModule.patchCreateOfferDraft({
      offerType: "percentage_discount",
      discountPercentage: "10",
      title: "10% off",
      description: "Ten percent off your next visit.",
    })
    expect(pageModule.getSnapshot().createOfferDrawer!.canConfirm).toBe(true)

    await pageModule.confirmCreateOffer()

    expect(createOffer).toHaveBeenCalledTimes(1)
    expect(pageModule.getSnapshot().createOfferDrawer).toBeNull()
    expect(pageModule.getSnapshot().pendingNavigation).toBeNull()
  })

  it("edit save calls updateOffer and unlocks confirm", async () => {
    const createOffer = vi.fn(async () => {
      throw new Error("create should not run")
    })
    const updateOffer = vi.fn(async () => catalogDetail({ title: "12% off" }))
    const getOffer = vi.fn(async () => catalogDetail())
    const pageModule = createOperatorOffersPageModule(
      createAdapters({ createOffer, updateOffer, getOffer })
    )

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    await pageModule.openEditOfferDrawer(88)

    expect(getOffer).toHaveBeenCalledWith(88)
    const drawer = pageModule.getSnapshot().createOfferDrawer!
    expect(drawer.mode).toBe("edit")
    expect(drawer.saveGated).toBe(false)
    expect(drawer.canConfirm).toBe(true)
    expect(drawer.draft.offerType).toBe("percentage_discount")
    expect(drawer.draft.title).toBe("10% off")

    pageModule.patchCreateOfferDraft({ title: "12% off" })
    await pageModule.confirmCreateOffer()
    expect(createOffer).not.toHaveBeenCalled()
    expect(updateOffer).toHaveBeenCalledTimes(1)
    expect(updateOffer).toHaveBeenCalledWith(
      88,
      expect.objectContaining({
        title: "12% off",
        discountPercentage: 10,
      })
    )
    expect(pageModule.getSnapshot().createOfferDrawer).toBeNull()
    expect(pageModule.getSnapshot().pendingEditOfferSave).toBeNull()
  })

  it("edit benefit change with issues opens soft confirm before update", async () => {
    const updateOffer = vi.fn(async () =>
      catalogDetail({ discountPercentage: 15 })
    )
    const getOffer = vi.fn(async () => catalogDetail({ issueCount: 2 }))
    const pageModule = createOperatorOffersPageModule(
      createAdapters({ updateOffer, getOffer })
    )

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    await pageModule.openEditOfferDrawer(88)
    pageModule.patchCreateOfferDraft({ discountPercentage: "15" })
    await pageModule.confirmCreateOffer()

    expect(updateOffer).not.toHaveBeenCalled()
    expect(pageModule.getSnapshot().pendingEditOfferSave).toEqual({
      title: "Save changes",
      description:
        "Changes apply to new issues only. Existing passes stay as they are.",
    })

    await pageModule.confirmPendingEditOfferSave()
    expect(updateOffer).toHaveBeenCalledTimes(1)
    expect(pageModule.getSnapshot().pendingEditOfferSave).toBeNull()
    expect(pageModule.getSnapshot().createOfferDrawer).toBeNull()
  })

  it("edit opens before hydrate without offer type and then fills from getOffer", async () => {
    let resolveOffer!: (value: CatalogOfferDetail) => void
    const getOffer = vi.fn(
      () =>
        new Promise<CatalogOfferDetail>((resolve) => {
          resolveOffer = resolve
        })
    )
    const pageModule = createOperatorOffersPageModule(
      createAdapters({ getOffer })
    )

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    const openPromise = pageModule.openEditOfferDrawer(88)
    expect(pageModule.getSnapshot().createOfferDrawer).toMatchObject({
      mode: "edit",
      saveGated: false,
      draft: { offerType: null },
    })

    resolveOffer(
      catalogDetail({
        offerType: "fixed_discount",
        title: "£5 off",
        description: "Five pounds off.",
        discountPercentage: null,
        discountAmount: 5,
      })
    )
    await openPromise

    expect(pageModule.getSnapshot().createOfferDrawer!.draft.offerType).toBe(
      "fixed_discount"
    )
    expect(pageModule.getSnapshot().createOfferDrawer!.draft.title).toBe(
      "£5 off"
    )
  })

  it("openCreateOffer opens the templates picker instead of the Create drawer", async () => {
    const createOffer = vi.fn(async () => {
      throw new Error("create should not run")
    })
    const pageModule = createOperatorOffersPageModule(
      createAdapters({ createOffer })
    )

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden Kitchen" }],
    })

    await pageModule.openCreateOffer()

    const snapshot = pageModule.getSnapshot()
    expect(snapshot.createOfferDrawer).toBeNull()
    expect(snapshot.offerTemplatePicker.open).toBe(true)
    expect(snapshot.offerTemplatePicker.loadStatus).toBe("loaded")
    expect(snapshot.offerTemplatePicker.viewModel?.cards).toHaveLength(7)
    expect(createOffer).not.toHaveBeenCalled()
  })

  it("useOfferTemplate closes the picker and soft-fills Create Offer without posting", async () => {
    const createOffer = vi.fn(async () => {
      throw new Error("create should not run")
    })
    const pageModule = createOperatorOffersPageModule(
      createAdapters({ createOffer })
    )

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden Kitchen" }],
    })
    await pageModule.openCreateOffer()

    pageModule.useOfferTemplate("welcome-new-guests")

    const snapshot = pageModule.getSnapshot()
    expect(snapshot.offerTemplatePicker.open).toBe(false)
    expect(snapshot.createOfferDrawer).toMatchObject({
      open: true,
      mode: "create",
      locationSubtitle: "Camden Kitchen",
    })
    expect(snapshot.createOfferDrawer!.draft).toMatchObject({
      offerType: "percentage_discount",
      title: "Welcome to Camden Kitchen",
      validity: "30_days_after_issue",
    })
    expect(createOffer).not.toHaveBeenCalled()
  })

  it("custom offer path soft-fills blank-ish create", async () => {
    const createOffer = vi.fn(async () => {
      throw new Error("create should not run")
    })
    const pageModule = createOperatorOffersPageModule(
      createAdapters({ createOffer })
    )

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })
    await pageModule.openCreateOffer()
    pageModule.useOfferTemplate("custom-offer")

    expect(pageModule.getSnapshot().createOfferDrawer!.draft).toMatchObject({
      offerType: null,
      title: "",
      validity: "30_days_after_issue",
      description: "Enjoy this offer on your next eligible visit to Camden.",
    })
    expect(createOffer).not.toHaveBeenCalled()
  })

  it("does not count Void-only Offers in the expiry overview row", async () => {
    const listCatalogOffers = vi.fn(
      async (params: { view?: string }) => {
        if (params.view === "needs-attention") {
          return emptyListResponse({
            totalCount: 2,
            items: [
              offerListItem({
                id: 1,
                title: "10% off next order",
                status: "active",
                validity: "choose_expiry_date",
                expiryDate: "2026-08-25",
                lifetimeClaims: 23,
                lifetimeRedeemed: 9,
              }),
              offerListItem({
                id: 2,
                title: "Free dessert",
                status: "active",
                validity: "14_days_after_issue",
                expiryDate: null,
                lifetimeClaims: 4,
                lifetimeRedeemed: 1,
              }),
            ],
            tabCounts: {
              all: 5,
              needsAttention: 2,
              drafts: 1,
              inFlight: 2,
              sent: 0,
            },
          })
        }
        return emptyListResponse({
          totalCount: 5,
          items: [offerListItem({ id: 9, title: "Other", status: "active" })],
          tabCounts: {
            all: 5,
            needsAttention: 2,
            drafts: 1,
            inFlight: 2,
            sent: 0,
          },
        })
      }
    )
    const listOpenVoidAttention = vi.fn(async () => [
      {
        offerId: 2,
        offerTitle: "Free dessert",
        pendingCount: 1,
        newestPendingRequestedAtUtc: "2026-08-22T11:00:00.000Z",
      },
    ])
    const pageModule = createOperatorOffersPageModule(
      createAdapters({ listCatalogOffers, listOpenVoidAttention })
    )

    await pageModule.syncWorkspace({
      selectedLocationId: 7,
      locations: [{ id: 7, locationName: "Manchester" }],
    })

    expect(listCatalogOffers).toHaveBeenCalledWith(
      expect.objectContaining({
        view: "needs-attention",
        utcOffsetMinutes: 0,
      })
    )

    const needsAttention = pageModule.getSnapshot().viewModel?.needsAttention
    expect(needsAttention?.isEmpty).toBe(false)
    expect(needsAttention?.showViewAll).toBe(false)
    expect(needsAttention?.rows).toHaveLength(2)
    expect(needsAttention?.rows[0]).toEqual({
      id: "warning-expiring",
      kind: "warning",
      title: "1 offer expires this week",
      body: "“10% off next order” has 23 claims and 9 redemptions before expiry.",
      metaLine: "Warning · 4 days ago · Manchester",
      ctaKind: "review-expiring",
      ctaLabel: "Review expiring offers",
    })
    expect(needsAttention?.rows[1]).toMatchObject({
      kind: "warning",
      ctaKind: "review-void-offer",
      offerId: 2,
      ctaLabel: "Review void request",
      metaLine: "Warning · 1 hour ago · Manchester",
    })

    await pageModule.selectNeedsAttentionList()
    expect(listCatalogOffers).toHaveBeenCalledWith(
      expect.objectContaining({ view: "needs-attention" })
    )
    expect(pageModule.getSnapshot().viewModel?.list.activeViewId).toBe(
      "needs-attention"
    )
  })

  it("still counts a dual-rule Offer in the expiry aggregate", async () => {
    const listCatalogOffers = vi.fn(async (params: { view?: string }) => {
      if (params.view === "needs-attention") {
        return emptyListResponse({
          totalCount: 1,
          items: [
            offerListItem({
              id: 22,
              title: "Dual rule dessert",
              status: "active",
              validity: "choose_expiry_date",
              expiryDate: "2026-08-25",
              lifetimeClaims: 4,
              lifetimeRedeemed: 1,
            }),
          ],
          tabCounts: {
            all: 3,
            needsAttention: 1,
            drafts: 0,
            inFlight: 1,
            sent: 0,
          },
        })
      }
      return emptyListResponse()
    })
    const pageModule = createOperatorOffersPageModule(
      createAdapters({
        listCatalogOffers,
        listOpenVoidAttention: vi.fn(async () => [
          {
            offerId: 22,
            offerTitle: "Dual rule dessert",
            pendingCount: 2,
            newestPendingRequestedAtUtc: "2026-08-22T10:00:00.000Z",
          },
        ]),
      })
    )

    await pageModule.syncWorkspace({
      selectedLocationId: 7,
      locations: [{ id: 7, locationName: "Camden" }],
    })

    const rows = pageModule.getSnapshot().viewModel?.needsAttention.rows
    expect(rows).toHaveLength(2)
    expect(rows?.[0]).toMatchObject({
      title: "1 offer expires this week",
      body: "“Dual rule dessert” has 4 claims and 1 redemption before expiry.",
      ctaKind: "review-expiring",
    })
    expect(rows?.[1]).toMatchObject({
      ctaKind: "review-void-offer",
      offerId: 22,
    })
  })

  it("quotes the soonest end date then lower catalog Offer id as the expiry lead", async () => {
    const listCatalogOffers = vi.fn(async (params: { view?: string }) => {
      if (params.view === "needs-attention") {
        return emptyListResponse({
          totalCount: 3,
          items: [
            offerListItem({
              id: 40,
              title: "Later end",
              status: "active",
              validity: "choose_expiry_date",
              expiryDate: "2026-08-28",
            }),
            offerListItem({
              id: 31,
              title: "Same-day higher id",
              status: "active",
              validity: "choose_expiry_date",
              expiryDate: "2026-08-24",
            }),
            offerListItem({
              id: 30,
              title: "Same-day lead",
              status: "active",
              validity: "choose_expiry_date",
              expiryDate: "2026-08-24",
              lifetimeClaims: 8,
              lifetimeRedeemed: 3,
            }),
          ],
          tabCounts: {
            all: 3,
            needsAttention: 3,
            drafts: 0,
            inFlight: 3,
            sent: 0,
          },
        })
      }
      return emptyListResponse()
    })
    const pageModule = createOperatorOffersPageModule(
      createAdapters({ listCatalogOffers })
    )

    await pageModule.syncWorkspace({
      selectedLocationId: 7,
      locations: [{ id: 7, locationName: "Camden" }],
    })

    expect(pageModule.getSnapshot().viewModel?.needsAttention.rows[0]).toEqual({
      id: "warning-expiring",
      kind: "warning",
      title: "3 offers expire this week",
      body: "“Same-day lead” has 8 claims and 3 redemptions before expiry.",
      metaLine: "Warning · 5 days ago · Camden",
      ctaKind: "review-expiring",
      ctaLabel: "Review expiring offers",
    })
  })

  it("keeps a Void-only queue without an expiry overview row", async () => {
    const listCatalogOffers = vi.fn(async (params: { view?: string }) => {
      if (params.view === "needs-attention") {
        return emptyListResponse({
          totalCount: 1,
          items: [
            offerListItem({
              id: 9,
              title: "Lunch deal",
              status: "active",
              validity: "7_days_after_issue",
              expiryDate: null,
            }),
          ],
          tabCounts: {
            all: 2,
            needsAttention: 1,
            drafts: 0,
            inFlight: 1,
            sent: 0,
          },
        })
      }
      return emptyListResponse()
    })
    const pageModule = createOperatorOffersPageModule(
      createAdapters({
        listCatalogOffers,
        listOpenVoidAttention: vi.fn(async () => [
          {
            offerId: 9,
            offerTitle: "Lunch deal",
            pendingCount: 1,
            newestPendingRequestedAtUtc: "2026-08-22T11:30:00.000Z",
          },
        ]),
      })
    )

    await pageModule.syncWorkspace({
      selectedLocationId: 7,
      locations: [{ id: 7, locationName: "Camden" }],
    })

    const needsAttention = pageModule.getSnapshot().viewModel?.needsAttention
    expect(needsAttention?.isEmpty).toBe(false)
    expect(needsAttention?.rows).toHaveLength(1)
    expect(needsAttention?.rows[0]).toMatchObject({
      ctaKind: "review-void-offer",
      offerId: 9,
      metaLine: "Warning · 30 minutes ago · Camden",
    })
    expect(
      needsAttention?.rows.some((row) => row.ctaKind === "review-expiring")
    ).toBe(false)
  })

  describe("Needs attention warning-type list scope (ticket 03)", () => {
    const fullQueueTabCounts = {
      all: 4,
      needsAttention: 3,
      drafts: 0,
      inFlight: 3,
      sent: 0,
    }

    function scopedListResponse(
      items: CatalogOffersListItem[],
      totalCount: number
    ): CatalogOffersListResponse {
      return emptyListResponse({
        totalCount,
        items,
        tabCounts: fullQueueTabCounts,
      })
    }

    function findListCall(
      listCatalogOffers: Mock<OperatorOffersPageAdapters["listCatalogOffers"]>,
      match: (params: Record<string, unknown>) => boolean
    ) {
      return listCatalogOffers.mock.calls.find((call) => match(call[0] as Record<string, unknown>))
    }

    it("Review expiring CTA requests expiry scope and clears search and filters", async () => {
      const listCatalogOffers = vi.fn(async (params) => {
        if (
          params.view === "needs-attention"
          && params.warningType === "expiry"
        ) {
          return scopedListResponse(
            [
              offerListItem({
                id: 1,
                title: "Expiring",
                status: "active",
                validity: "choose_expiry_date",
                expiryDate: "2026-08-25",
              }),
            ],
            1
          )
        }
        if (params.view === "needs-attention") {
          return scopedListResponse([], 3)
        }
        return emptyListResponse()
      })
      const pageModule = createOperatorOffersPageModule(
        createAdapters({ listCatalogOffers })
      )

      await pageModule.syncWorkspace({
        selectedLocationId: 7,
        locations: [{ id: 7, locationName: "Camden" }],
      })
      pageModule.setSearchQuery("lunch")
      pageModule.applyFilters({
        ...emptySelection(offersFilterSheetSchema()),
        status: { kind: "multi-select", ids: ["active"] },
      })
      pageModule.setSortId("title-az")

      await pageModule.selectNeedsAttentionWarningScope("expiry")

      expect(findListCall(listCatalogOffers, (params) => params.warningType === "expiry")).toEqual(
        expect.arrayContaining([
          expect.objectContaining({
            view: "needs-attention",
            warningType: "expiry",
            q: undefined,
            page: 1,
            sort: "title-az",
          }),
        ])
      )
      const list = pageModule.getSnapshot().viewModel?.list
      expect(list?.activeViewId).toBe("needs-attention")
      expect(list?.searchQuery).toBe("")
      expect(list?.filterChipCount).toBe(0)
      expect(list?.totalCount).toBe(1)
      expect(list?.tabs.find((tab) => tab.id === "needs-attention")?.count).toBe(
        3
      )
      expect(list?.pageRangeLabel).toBe("Showing 1–1 of 1 offers")
    })

    it("Review void aggregate CTA requests void scope", async () => {
      const listCatalogOffers = vi.fn(async (params) => {
        if (
          params.view === "needs-attention"
          && params.warningType === "void"
        ) {
          return scopedListResponse(
            [
              offerListItem({
                id: 8,
                title: "Void only",
                status: "active",
              }),
              offerListItem({
                id: 22,
                title: "Dual rule",
                status: "active",
                validity: "choose_expiry_date",
                expiryDate: "2026-08-25",
              }),
            ],
            2
          )
        }
        if (params.view === "needs-attention") {
          return scopedListResponse([], 3)
        }
        return emptyListResponse()
      })
      const pageModule = createOperatorOffersPageModule(
        createAdapters({ listCatalogOffers })
      )

      await pageModule.syncWorkspace({
        selectedLocationId: 7,
        locations: [{ id: 7, locationName: "Camden" }],
      })

      await pageModule.selectNeedsAttentionWarningScope("void")

      expect(findListCall(listCatalogOffers, (params) => params.warningType === "void")).toEqual(
        expect.arrayContaining([
          expect.objectContaining({
            view: "needs-attention",
            warningType: "void",
          }),
        ])
      )
      expect(pageModule.getSnapshot().viewModel?.list.totalCount).toBe(2)
      expect(
        pageModule.getSnapshot().viewModel?.list.tabs.find(
          (tab) => tab.id === "needs-attention"
        )?.count
      ).toBe(3)
    })

    it("Needs attention tab click and View all omit warning-type", async () => {
      const listCatalogOffers = vi.fn(async (params) => {
        if (params.view === "needs-attention" && params.warningType == null) {
          return scopedListResponse(
            [offerListItem({ id: 1, title: "One", status: "active" })],
            3
          )
        }
        if (params.view === "needs-attention") {
          return scopedListResponse([], 1)
        }
        return emptyListResponse()
      })
      const pageModule = createOperatorOffersPageModule(
        createAdapters({ listCatalogOffers })
      )

      await pageModule.syncWorkspace({
        selectedLocationId: 7,
        locations: [{ id: 7, locationName: "Camden" }],
      })
      await pageModule.selectNeedsAttentionWarningScope("expiry")
      await pageModule.setListView("needs-attention")

      expect(
        findListCall(
          listCatalogOffers,
          (params) =>
            params.view === "needs-attention" && params.warningType == null
        )
      ).toBeDefined()
      expect(pageModule.getSnapshot().viewModel?.list.totalCount).toBe(3)

      await pageModule.selectNeedsAttentionWarningScope("void")
      await pageModule.selectNeedsAttentionList()

      expect(
        findListCall(
          listCatalogOffers,
          (params) =>
            params.view === "needs-attention"
            && params.warningType == null
            && params.page === 1
        )
      ).toBeDefined()
    })

    it("Other tabs clear warning-type scope", async () => {
      const listCatalogOffers = vi.fn(async () => emptyListResponse())
      const pageModule = createOperatorOffersPageModule(
        createAdapters({ listCatalogOffers })
      )

      await pageModule.syncWorkspace({
        selectedLocationId: 7,
        locations: [{ id: 7, locationName: "Camden" }],
      })
      await pageModule.selectNeedsAttentionWarningScope("expiry")
      await pageModule.setListView("drafts")

      expect(
        findListCall(
          listCatalogOffers,
          (params) => params.view === "drafts" && params.warningType == null
        )
      ).toBeDefined()
    })

    it("Location change clears warning-type scope", async () => {
      const listCatalogOffers = vi.fn(async () => emptyListResponse())
      const pageModule = createOperatorOffersPageModule(
        createAdapters({ listCatalogOffers })
      )

      await pageModule.syncWorkspace({
        selectedLocationId: 7,
        locations: [
          { id: 7, locationName: "Camden" },
          { id: 8, locationName: "Manchester" },
        ],
      })
      await pageModule.selectNeedsAttentionWarningScope("expiry")
      await pageModule.syncWorkspace({
        selectedLocationId: 8,
        locations: [
          { id: 7, locationName: "Camden" },
          { id: 8, locationName: "Manchester" },
        ],
      })

      expect(
        findListCall(
          listCatalogOffers,
          (params) => params.view === "all" && params.warningType == null
        )
      ).toBeDefined()
    })
  })
})
