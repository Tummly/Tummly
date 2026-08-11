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

  it("assembles Needs attention overview from expiring list + open void facts", async () => {
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
                lifetimeClaims: 23,
                lifetimeRedeemed: 9,
              }),
              offerListItem({
                id: 2,
                title: "Free dessert",
                status: "active",
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
        offerId: 42,
        offerTitle: "Lunch deal",
        pendingCount: 1,
      },
    ])
    const pageModule = createOperatorOffersPageModule(
      createAdapters({ listCatalogOffers, listOpenVoidAttention })
    )

    await pageModule.syncWorkspace({
      selectedLocationId: 7,
      locations: [{ id: 7, locationName: "Manchester" }],
    })

    const needsAttention = pageModule.getSnapshot().viewModel?.needsAttention
    expect(needsAttention?.isEmpty).toBe(false)
    expect(needsAttention?.showViewAll).toBe(false)
    expect(needsAttention?.rows).toHaveLength(2)
    expect(needsAttention?.rows[0]).toMatchObject({
      kind: "warning",
      title: "2 offers expire this week",
      ctaKind: "review-expiring",
      metaLine: expect.stringContaining("Warning ·"),
    })
    expect(needsAttention?.rows[1]).toMatchObject({
      kind: "warning",
      ctaKind: "review-void-offer",
      offerId: 42,
      ctaLabel: "Review void request",
    })

    await pageModule.selectNeedsAttentionList()
    expect(listCatalogOffers).toHaveBeenCalledWith(
      expect.objectContaining({ view: "needs-attention" })
    )
    expect(pageModule.getSnapshot().viewModel?.list.activeViewId).toBe(
      "needs-attention"
    )
  })
})
