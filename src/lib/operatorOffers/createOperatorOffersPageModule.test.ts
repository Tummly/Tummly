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

function createAdapters(
  overrides: Partial<OperatorOffersPageAdapters> & {
    listCatalogOffers?: Mock<OperatorOffersPageAdapters["listCatalogOffers"]>
  } = {}
): OperatorOffersPageAdapters {
  return {
    listCatalogOffers:
      overrides.listCatalogOffers
      ?? vi.fn(async () => emptyListResponse()),
    debounceMs: overrides.debounceMs ?? 0,
  }
}

describe("createOperatorOffersPageModule", () => {
  it("starts idle with an empty snapshot", () => {
    const pageModule = createOperatorOffersPageModule(createAdapters())

    expect(pageModule.getSnapshot()).toEqual({
      loadStatus: "idle",
      viewModel: null,
      loadError: null,
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

    pageModule.setPerformanceDateRange({
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

    pageModule.setPerformanceDateRange({
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
      viewModel: null,
      loadError: null,
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
      viewModel: null,
      loadError: OFFERS_LOAD_ERROR_MESSAGE,
    })
  })

  it("surfaces load error and recovers on retry", async () => {
    const listCatalogOffers = vi
      .fn()
      .mockRejectedValueOnce(new Error("network"))
      .mockResolvedValueOnce(emptyListResponse())
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

    expect(listCatalogOffers).toHaveBeenLastCalledWith(
      expect.objectContaining({ view: "needs-attention", page: 1 })
    )
    expect(pageModule.getSnapshot().viewModel?.list.empty?.kind).toBe(
      "view-scoped"
    )
    expect(pageModule.getSnapshot().viewModel?.list.showListChrome).toBe(true)
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
      expect(listCatalogOffers).toHaveBeenLastCalledWith(
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
    expect(listCatalogOffers).toHaveBeenLastCalledWith(
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
      expect(listCatalogOffers).toHaveBeenLastCalledWith(
        expect.objectContaining({
          status: ["draft"],
          attachSource: ["campaign"],
          page: 1,
        })
      )
    })
    expect(
      pageModule.getSnapshot().viewModel?.list.filterChipCount
    ).toBeGreaterThan(0)
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
      expect(listCatalogOffers).toHaveBeenLastCalledWith(
        expect.objectContaining({ sort: "title-az", page: 1 })
      )
    })
  })

  it("opens gated lifecycle confirm chrome without calling writes", async () => {
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
    const pageModule = createOperatorOffersPageModule(
      createAdapters({ listCatalogOffers })
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

    pageModule.confirmPendingLifecycleAction()
    expect(
      pageModule.getSnapshot().viewModel?.pendingLifecycleAction
    ).toBeNull()
    expect(listCatalogOffers.mock.calls.length).toBe(callsBefore)
  })

  it("treats View and Edit as no-ops until Details / Edit land", async () => {
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
    const pageModule = createOperatorOffersPageModule(
      createAdapters({ listCatalogOffers })
    )

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    pageModule.requestRowAction(1, "view")
    pageModule.requestRowAction(1, "edit")
    expect(
      pageModule.getSnapshot().viewModel?.pendingLifecycleAction
    ).toBeNull()
  })
})
