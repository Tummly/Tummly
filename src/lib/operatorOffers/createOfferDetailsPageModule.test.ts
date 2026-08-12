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
    issueCount: 0,
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
      ["0", "0", "0%", "0", "0"]
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
    expect(pageModule.getSnapshot().viewModel?.claims.empty).toEqual({
      title: "No one has claimed this offer yet",
      helper:
        "Once guests claim this offer from a feedback form, campaign or manual link, they'll appear here.",
      primaryCtaLabel: "Share offer in a campaign",
    })
    expect(pageModule.getSnapshot().viewModel?.claims.rows).toEqual([])
    expect(pageModule.getSnapshot().viewModel?.claims.columns.guest).toBe(
      "Guest"
    )

    pageModule.setActiveTab("void-requests")
    expect(pageModule.getSnapshot().viewModel?.activeTabId).toBe(
      "void-requests"
    )
    expect(pageModule.getSnapshot().viewModel?.voidRequests.empty?.title).toBe(
      "No void requests yet"
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
    expect(
      overview?.kpis.every(
        (kpi) => kpi.primaryText === "0" || kpi.primaryText === "0%"
      )
    ).toBe(true)
  })

  it("getOfferMetrics adapter maps live KPIs and refetches on date range change", async () => {
    const getOfferMetrics = vi
      .fn()
      .mockResolvedValueOnce({
        claims: 40,
        redemptions: 10,
        expiredUnused: 3,
        failedAttempts: 2,
      })
      .mockResolvedValueOnce({
        claims: 80,
        redemptions: 20,
        expiredUnused: 6,
        failedAttempts: 4,
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
    expect(
      pageModule.getSnapshot().viewModel?.overview.kpis.map((kpi) => kpi.primaryText)
    ).toEqual(["40", "10", "25%", "3", "2"])
    expect(pageModule.getSnapshot().viewModel?.overview.recommendation.emptyTitle).toBe(
      OFFER_DETAILS_COPY.recommendedEmptyTitle
    )

    await pageModule.setOverviewDateRange({
      kind: "preset",
      presetId: "last30",
    })
    expect(getOfferMetrics).toHaveBeenLastCalledWith(10, {
      kind: "preset",
      presetId: "last30",
    })
    expect(
      pageModule.getSnapshot().viewModel?.overview.kpis.map((kpi) => kpi.primaryText)
    ).toEqual(["80", "20", "25%", "6", "4"])
    expect(
      pageModule
        .getSnapshot()
        .viewModel?.overview.kpis.every(
          (kpi) => !kpi.helperText.includes("% vs previous")
        )
    ).toBe(true)
  })

  it("requestHeaderAction opens confirm chrome; cancel clears without writes", async () => {
    const getOffer = vi.fn().mockResolvedValue(sampleOffer())
    const pauseOffer = vi.fn()
    const pageModule = createOfferDetailsPageModule({ getOffer, pauseOffer })
    await pageModule.syncWorkspace(workspace)

    pageModule.requestHeaderAction("pause-issuance")
    expect(pageModule.getSnapshot().viewModel?.pendingHeaderAction).toEqual({
      actionId: "pause-issuance",
      title: OFFER_DETAILS_COPY.pauseConfirmTitle,
      description: OFFER_DETAILS_COPY.pauseConfirmDescription,
    })

    pageModule.cancelPendingHeaderAction()
    expect(pageModule.getSnapshot().viewModel?.pendingHeaderAction).toBeNull()
    expect(pauseOffer).not.toHaveBeenCalled()
  })

  it("confirmPendingHeaderAction pauses and refreshes offer chrome", async () => {
    const getOffer = vi.fn().mockResolvedValue(sampleOffer())
    const pauseOffer = vi.fn(async () => sampleOffer({ status: "paused" }))
    const pageModule = createOfferDetailsPageModule({ getOffer, pauseOffer })
    await pageModule.syncWorkspace(workspace)

    pageModule.requestHeaderAction("pause-issuance")
    await pageModule.confirmPendingHeaderAction()

    expect(pauseOffer).toHaveBeenCalledWith(10)
    expect(pageModule.getSnapshot().viewModel?.pendingHeaderAction).toBeNull()
    expect(pageModule.getSnapshot().viewModel?.status).toBe("paused")
    expect(pageModule.getSnapshot().viewModel?.headerMenuItems.map((item) => item.id)).toEqual([
      "resume-issuance",
      "archive-offer",
      "duplicate",
    ])
  })

  it("confirmPendingHeaderAction resumes and archives in place", async () => {
    const getOffer = vi.fn().mockResolvedValue(sampleOffer({ status: "paused" }))
    const resumeOffer = vi.fn(async () => sampleOffer({ status: "active" }))
    const archiveOffer = vi.fn(async () => sampleOffer({ status: "archived" }))
    const pageModule = createOfferDetailsPageModule({
      getOffer,
      resumeOffer,
      archiveOffer,
    })
    await pageModule.syncWorkspace(workspace)

    pageModule.requestHeaderAction("resume-issuance")
    await pageModule.confirmPendingHeaderAction()
    expect(resumeOffer).toHaveBeenCalledWith(10)
    expect(pageModule.getSnapshot().viewModel?.status).toBe("active")

    pageModule.requestHeaderAction("archive-offer")
    await pageModule.confirmPendingHeaderAction()
    expect(archiveOffer).toHaveBeenCalledWith(10)
    expect(pageModule.getSnapshot().viewModel?.status).toBe("archived")
    expect(pageModule.getSnapshot().viewModel?.offerId).toBe(10)
    expect(pageModule.getSnapshot().viewModel?.headerMenuItems.map((item) => item.id)).toEqual([
      "duplicate",
    ])
  })

  it("confirmPendingHeaderAction duplicates and calls onDuplicated", async () => {
    const getOffer = vi.fn().mockResolvedValue(sampleOffer())
    const onDuplicated = vi.fn()
    const duplicateOffer = vi.fn(async () =>
      sampleOffer({ id: 99, status: "draft", title: "10% off next visit (copy)" })
    )
    const pageModule = createOfferDetailsPageModule({
      getOffer,
      duplicateOffer,
      onDuplicated,
    })
    await pageModule.syncWorkspace(workspace)

    pageModule.requestHeaderAction("duplicate")
    await pageModule.confirmPendingHeaderAction()

    expect(duplicateOffer).toHaveBeenCalledWith(10)
    expect(onDuplicated).toHaveBeenCalledWith(99)
    expect(pageModule.getSnapshot().viewModel?.offerId).toBe(10)
    expect(pageModule.getSnapshot().viewModel?.status).toBe("active")
  })

  it("lifecycle write failure clears pending and keeps current offer", async () => {
    const getOffer = vi.fn().mockResolvedValue(sampleOffer())
    const archiveOffer = vi.fn(async () => {
      throw new Error("offline")
    })
    const pageModule = createOfferDetailsPageModule({ getOffer, archiveOffer })
    await pageModule.syncWorkspace(workspace)

    pageModule.requestHeaderAction("archive-offer")
    await pageModule.confirmPendingHeaderAction()

    expect(pageModule.getSnapshot().viewModel?.pendingHeaderAction).toBeNull()
    expect(pageModule.getSnapshot().viewModel?.status).toBe("active")
    expect(pageModule.getSnapshot().loadStatus).toBe("loaded")
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

  it("setCampaignsSubTab switches Linked vs Issuance sources chrome", async () => {
    const pageModule = createOfferDetailsPageModule({
      getOffer: vi.fn().mockResolvedValue(sampleOffer()),
    })
    await pageModule.syncWorkspace(workspace)

    expect(pageModule.getSnapshot().viewModel?.campaigns.activeSubTabId).toBe(
      "linked"
    )
    expect(
      pageModule.getSnapshot().viewModel?.campaigns.linked.empty?.title
    ).toBe("No linked campaigns yet")

    pageModule.setCampaignsSubTab("issuance-sources")
    expect(pageModule.getSnapshot().viewModel?.campaigns.activeSubTabId).toBe(
      "issuance-sources"
    )
    expect(
      pageModule.getSnapshot().viewModel?.campaigns.issuanceSources.empty
        ?.title
    ).toBe("No issuance sources yet")
  })

  it("requestClaimsRowAction opens gated confirm and confirm clears without APIs", async () => {
    const pageModule = createOfferDetailsPageModule({
      getOffer: vi.fn().mockResolvedValue(sampleOffer()),
      getClaims: vi.fn().mockResolvedValue([
        {
          id: "claim-1",
          guestName: "Alex Guest",
          guestId: "g-1",
          claimCode: "ABCD1234",
          claimedText: "1 Aug 2026",
          sourceText: "Campaign",
          locationName: "Camden",
          expiryText: "15 Aug 2026",
          statusText: "Open",
          actions: [],
        },
      ]),
    })
    await pageModule.syncWorkspace(workspace)

    expect(pageModule.getSnapshot().viewModel?.claims.empty).toBeNull()
    expect(pageModule.getSnapshot().viewModel?.claims.rows).toHaveLength(1)
    expect(
      pageModule.getSnapshot().viewModel?.claims.rows[0]?.actions.map(
        (action) => action.id
      )
    ).toEqual([
      "view-guest-profile",
      "resend-offer",
      "cancel-claim",
      "copy-code",
    ])

    pageModule.requestClaimsRowAction("claim-1", "resend-offer")
    expect(pageModule.getSnapshot().viewModel?.pendingRowAction).toEqual({
      tabId: "claims",
      actionId: "resend-offer",
      rowId: "claim-1",
      title: OFFER_DETAILS_COPY.claimsResendConfirmTitle,
      description: OFFER_DETAILS_COPY.claimsResendConfirmDescription,
    })

    pageModule.confirmPendingRowAction()
    expect(pageModule.getSnapshot().viewModel?.pendingRowAction).toBeNull()

    pageModule.requestClaimsRowAction("claim-1", "cancel-claim")
    pageModule.cancelPendingRowAction()
    expect(pageModule.getSnapshot().viewModel?.pendingRowAction).toBeNull()
  })

  it("redemptions tab ships columns without Override and omits Export in row actions", async () => {
    const pageModule = createOfferDetailsPageModule({
      getOffer: vi.fn().mockResolvedValue(sampleOffer()),
      getRedemptions: vi.fn().mockResolvedValue([
        {
          id: "red-1",
          dateTimeText: "2 Aug 2026, 12:00",
          guestName: "Alex Guest",
          guestId: "g-1",
          passReferenceText: "PASS-1",
          locationName: "Camden",
          staffMemberText: "Sam",
          outcomeText: "Redeemed",
          reasonText: "—",
          offerVersionText: "v1",
          actions: [],
        },
      ]),
    })
    await pageModule.syncWorkspace(workspace)

    const redemptions = pageModule.getSnapshot().viewModel?.redemptions
    expect(redemptions?.empty).toBeNull()
    expect(redemptions?.columns).not.toHaveProperty("override")
    expect(
      redemptions?.rows[0]?.actions.map((action) => action.id)
    ).toEqual([
      "view-redemption",
      "view-pass",
      "view-guest",
      "view-issued-terms",
      "request-void",
    ])
  })
})
