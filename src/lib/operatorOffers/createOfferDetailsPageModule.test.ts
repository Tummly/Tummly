import { describe, expect, it, vi } from "vitest"

import {
  createOfferDetailsPageModule,
  OFFER_DETAILS_LOAD_ERROR_MESSAGE,
} from "@/lib/operatorOffers/createOfferDetailsPageModule"
import { OFFER_DETAILS_COPY } from "@/lib/operatorOffers/offerDetailsPresentation"
import type { CatalogOfferDetail } from "@/types/operatorCampaigns"

function sampleOffer(
  overrides: Partial<CatalogOfferDetail> = {}
): CatalogOfferDetail {
  return {
    id: 10,
    locationId: 42,
    status: "active",
    offerType: "percentage_discount",
    title: "10% off next visit",
    description: "Camden thank-you offer · August",
    validity: "14_days_after_issue",
    expiryDate: null,
    discountPercentage: 10,
    discountAmount: null,
    freeItemText: null,
    purchaseRequirement: null,
    minimumSpend: null,
    additionalExclusions: null,
    replacementItemText: null,
    staffInstructions: null,
    createdAt: "2026-07-01T12:00:00.000Z",
    updatedAt: "2026-07-01T12:00:00.000Z",
    ...overrides,
  }
}

const workspace = {
  selectedLocationId: 42,
  locations: [{ id: 42, locationName: "Camden" }],
  offerId: 10,
} as const

describe("createOfferDetailsPageModule", () => {
  it("starts idle with an empty snapshot", () => {
    const pageModule = createOfferDetailsPageModule({
      getOffer: vi.fn(),
    })

    expect(pageModule.getSnapshot()).toEqual({
      loadStatus: "idle",
      viewModel: null,
      loadError: null,
    })
  })

  it("loads offer chrome via getOffer and defaults Overview to Last 7 days", async () => {
    const getOffer = vi.fn().mockResolvedValue(sampleOffer())
    const pageModule = createOfferDetailsPageModule({ getOffer })
    const statuses: string[] = []
    const unsubscribe = pageModule.subscribe(() => {
      statuses.push(pageModule.getSnapshot().loadStatus)
    })

    await pageModule.syncWorkspace(workspace)

    expect(getOffer).toHaveBeenCalledWith(10)
    expect(statuses).toEqual(["loading", "loaded"])
    const snapshot = pageModule.getSnapshot()
    expect(snapshot.loadStatus).toBe("loaded")
    expect(snapshot.viewModel?.title).toBe("10% off next visit")
    expect(snapshot.viewModel?.subtitle).toBe(
      "Camden thank-you offer · August"
    )
    expect(snapshot.viewModel?.statusLabel).toBe("Active")
    expect(snapshot.viewModel?.editOfferLabel).toBe("Edit offer")
    expect(snapshot.viewModel?.openStaffRedeemLabel).toBe("Open staff redeem")
    expect(snapshot.viewModel?.activeTabId).toBe("overview")
    expect(snapshot.viewModel?.overview.dateRangeLabel).toBe("Last 7 days")
    expect(snapshot.viewModel?.overview.definitionTitle).toBe(
      "Claims and redemptions over time"
    )
    expect(snapshot.viewModel?.overview.kpis.map((kpi) => kpi.primaryText)).toEqual(
      ["0", "0", "0", "0", "0"]
    )
    expect(snapshot.viewModel?.headerMenuItems.map((item) => item.id)).toEqual([
      "pause-issuance",
      "duplicate",
      "archive-offer",
    ])
    expect(snapshot.viewModel?.tabs.map((tab) => tab.label)).toEqual([
      "Overview",
      "Claims",
      "Redemptions",
      "Campaigns",
      "Void requests",
    ])
    expect(snapshot.viewModel?.overview.recommendation.emptyTitle).toBe(
      OFFER_DETAILS_COPY.recommendedEmptyTitle
    )

    unsubscribe()
  })

  it("setActiveTab switches tab chrome and empty placeholders", async () => {
    const pageModule = createOfferDetailsPageModule({
      getOffer: vi.fn().mockResolvedValue(sampleOffer()),
    })
    await pageModule.syncWorkspace(workspace)

    pageModule.setActiveTab("claims")
    expect(pageModule.getSnapshot().viewModel?.activeTabId).toBe("claims")
    expect(pageModule.getSnapshot().viewModel?.activeTabEmptyPlaceholder).toBe(
      OFFER_DETAILS_COPY.claimsEmptyPlaceholder
    )

    pageModule.setActiveTab("void-requests")
    expect(pageModule.getSnapshot().viewModel?.activeTabId).toBe(
      "void-requests"
    )
    expect(pageModule.getSnapshot().viewModel?.activeTabEmptyPlaceholder).toBe(
      OFFER_DETAILS_COPY.voidRequestsEmptyPlaceholder
    )
  })

  it("setOverviewDateRange updates label and keeps zero KPIs without metrics adapter", async () => {
    const pageModule = createOfferDetailsPageModule({
      getOffer: vi.fn().mockResolvedValue(sampleOffer()),
    })
    await pageModule.syncWorkspace(workspace)

    await pageModule.setOverviewDateRange({
      kind: "preset",
      presetId: "last90",
    })

    const overview = pageModule.getSnapshot().viewModel?.overview
    expect(overview?.dateRangeLabel).toBe("Last 90 days")
    expect(overview?.kpis.every((kpi) => kpi.primaryText === "0")).toBe(true)
  })

  it("getOfferMetrics adapter refreshes KPI zeros for the selected range", async () => {
    const getOfferMetrics = vi
      .fn()
      .mockResolvedValueOnce({
        claims: 0,
        redemptions: 0,
        expiredUnused: 0,
        failedAttempts: 0,
      })
      .mockResolvedValueOnce({
        claims: 0,
        redemptions: 0,
        expiredUnused: 0,
        failedAttempts: 0,
      })

    const pageModule = createOfferDetailsPageModule({
      getOffer: vi.fn().mockResolvedValue(sampleOffer()),
      getOfferMetrics,
    })
    await pageModule.syncWorkspace(workspace)
    expect(getOfferMetrics).toHaveBeenCalledWith(10, {
      kind: "preset",
      presetId: "last7",
    })

    await pageModule.setOverviewDateRange({
      kind: "preset",
      presetId: "last30",
    })
    expect(getOfferMetrics).toHaveBeenLastCalledWith(10, {
      kind: "preset",
      presetId: "last30",
    })
  })

  it("requestHeaderAction opens gated confirm and confirm/cancel do not call writes", async () => {
    const getOffer = vi.fn().mockResolvedValue(sampleOffer())
    const pageModule = createOfferDetailsPageModule({ getOffer })
    await pageModule.syncWorkspace(workspace)

    pageModule.requestHeaderAction("archive-offer")
    expect(pageModule.getSnapshot().viewModel?.pendingHeaderAction).toEqual({
      actionId: "archive-offer",
      title: OFFER_DETAILS_COPY.archiveConfirmTitle,
      description: OFFER_DETAILS_COPY.archiveConfirmDescription,
    })

    pageModule.confirmPendingHeaderAction()
    expect(pageModule.getSnapshot().viewModel?.pendingHeaderAction).toBeNull()
    expect(getOffer).toHaveBeenCalledTimes(1)

    pageModule.requestHeaderAction("pause-issuance")
    pageModule.cancelPendingHeaderAction()
    expect(pageModule.getSnapshot().viewModel?.pendingHeaderAction).toBeNull()
  })

  it("ignores header actions that are not in the status menu", async () => {
    const pageModule = createOfferDetailsPageModule({
      getOffer: vi.fn().mockResolvedValue(sampleOffer({ status: "archived" })),
    })
    await pageModule.syncWorkspace(workspace)

    pageModule.requestHeaderAction("pause-issuance")
    expect(pageModule.getSnapshot().viewModel?.pendingHeaderAction).toBeNull()
  })

  it("errors when getOffer fails and retryLoad recovers", async () => {
    const getOffer = vi
      .fn()
      .mockRejectedValueOnce(new Error("offline"))
      .mockResolvedValueOnce(sampleOffer())
    const pageModule = createOfferDetailsPageModule({ getOffer })

    await pageModule.syncWorkspace(workspace)
    expect(pageModule.getSnapshot()).toEqual({
      loadStatus: "error",
      viewModel: null,
      loadError: OFFER_DETAILS_LOAD_ERROR_MESSAGE,
    })

    await pageModule.retryLoad()
    expect(pageModule.getSnapshot().loadStatus).toBe("loaded")
    expect(pageModule.getSnapshot().viewModel?.offerId).toBe(10)
  })

  it("clears when offerId or location is null", async () => {
    const pageModule = createOfferDetailsPageModule({
      getOffer: vi.fn().mockResolvedValue(sampleOffer()),
    })
    await pageModule.syncWorkspace(workspace)
    await pageModule.syncWorkspace({
      ...workspace,
      offerId: null,
    })

    expect(pageModule.getSnapshot()).toEqual({
      loadStatus: "idle",
      viewModel: null,
      loadError: null,
    })
  })
})
