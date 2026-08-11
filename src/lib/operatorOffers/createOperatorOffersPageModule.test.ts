import { describe, expect, it } from "vitest"

import {
  OFFERS_LOAD_ERROR_MESSAGE,
  createOperatorOffersPageModule,
} from "@/lib/operatorOffers/createOperatorOffersPageModule"
import { OFFERS_PAGE_COPY } from "@/lib/operatorOffers/offersPresentation"
import { NEEDS_ATTENTION_EMPTY_COPY } from "@/lib/operatorHome/operatorHomeSectionPresentation"
import { DEFAULT_HOME_PERFORMANCE_DATE_RANGE } from "@/lib/operatorHome/homePerformanceDateRange"

describe("createOperatorOffersPageModule", () => {
  it("starts idle with an empty snapshot", () => {
    const pageModule = createOperatorOffersPageModule()

    expect(pageModule.getSnapshot()).toEqual({
      loadStatus: "idle",
      viewModel: null,
      loadError: null,
    })
  })

  it("notifies subscribers when syncWorkspace loads location chrome", async () => {
    const pageModule = createOperatorOffersPageModule()
    const statuses: string[] = []
    const unsubscribe = pageModule.subscribe(() => {
      statuses.push(pageModule.getSnapshot().loadStatus)
    })

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    expect(statuses).toEqual(["loading", "loaded"])
    expect(pageModule.getSnapshot()).toEqual({
      loadStatus: "loaded",
      loadError: null,
      viewModel: {
        locationId: 42,
        locationName: "Camden",
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
      },
    })

    unsubscribe()
  })

  it("clears the view model when selected location is null", async () => {
    const pageModule = createOperatorOffersPageModule()

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
    const pageModule = createOperatorOffersPageModule()

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

  it("setPerformanceDateRange updates range and label and republishes", async () => {
    const pageModule = createOperatorOffersPageModule()
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
    const pageModule = createOperatorOffersPageModule()
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
})
