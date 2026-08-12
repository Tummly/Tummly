import { describe, expect, it, vi } from "vitest"

import {
  OFFERS_REDEMPTION_LOG_LOAD_ERROR_MESSAGE,
  createOperatorOffersRedemptionLogModule,
} from "@/lib/operatorOffers/createOperatorOffersRedemptionLogModule"
import { OFFERS_REDEMPTION_LOG_COPY } from "@/lib/operatorOffers/offersRedemptionLogPresentation"

describe("createOperatorOffersRedemptionLogModule", () => {
  it("starts idle with an empty snapshot", () => {
    const pageModule = createOperatorOffersRedemptionLogModule()

    expect(pageModule.getSnapshot()).toEqual({
      loadStatus: "idle",
      viewModel: null,
      loadError: null,
    })
  })

  it("loads location chrome with Details Redemptions columns plus Offer and honest empty", async () => {
    const pageModule = createOperatorOffersRedemptionLogModule()
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
        title: OFFERS_REDEMPTION_LOG_COPY.title,
        subtitle: OFFERS_REDEMPTION_LOG_COPY.subtitle,
        backLabel: OFFERS_REDEMPTION_LOG_COPY.backToOffers,
        columns: OFFERS_REDEMPTION_LOG_COPY.columns,
        rows: [],
        empty: {
          title: OFFERS_REDEMPTION_LOG_COPY.emptyTitle,
          helper: OFFERS_REDEMPTION_LOG_COPY.emptyHelper,
          retryLabel: OFFERS_REDEMPTION_LOG_COPY.retry,
        },
      },
    })

    unsubscribe()
  })

  it("retryLoad re-runs sync for the current workspace", async () => {
    const listRedemptions = vi
      .fn()
      .mockRejectedValueOnce(new Error("offline"))
      .mockResolvedValueOnce([])

    const pageModule = createOperatorOffersRedemptionLogModule({
      listRedemptions,
    })

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    expect(pageModule.getSnapshot().loadStatus).toBe("error")
    expect(pageModule.getSnapshot().loadError).toBe(
      OFFERS_REDEMPTION_LOG_LOAD_ERROR_MESSAGE
    )

    await pageModule.retryLoad()

    expect(listRedemptions).toHaveBeenCalledTimes(2)
    expect(pageModule.getSnapshot().loadStatus).toBe("loaded")
    expect(pageModule.getSnapshot().viewModel?.rows).toEqual([])
  })

  it("surfaces live location rows from listRedemptions including Offer title", async () => {
    const listRedemptions = vi.fn().mockResolvedValue([
      {
        id: "redeemed-1",
        dateTimeText: "2 Aug 2026, 14:30",
        guestName: "Alex",
        passReferenceText: "TUM-RED001",
        locationName: "Camden",
        staffMemberText: "—",
        outcomeText: "Redeemed",
        reasonText: "—",
        offerVersionText: "1 Aug 2026",
        offerTitle: "Free coffee",
      },
    ])

    const pageModule = createOperatorOffersRedemptionLogModule({
      listRedemptions,
    })

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    expect(listRedemptions).toHaveBeenCalledWith(42)
    expect(pageModule.getSnapshot().viewModel?.rows).toEqual([
      {
        id: "redeemed-1",
        dateTimeText: "2 Aug 2026, 14:30",
        guestName: "Alex",
        passReferenceText: "TUM-RED001",
        locationName: "Camden",
        staffMemberText: "—",
        outcomeText: "Redeemed",
        reasonText: "—",
        offerVersionText: "1 Aug 2026",
        offerTitle: "Free coffee",
      },
    ])
  })

  it("clears the view model when selected location is null", async () => {
    const pageModule = createOperatorOffersRedemptionLogModule()

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
    const pageModule = createOperatorOffersRedemptionLogModule()

    await pageModule.syncWorkspace({
      selectedLocationId: 99,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    expect(pageModule.getSnapshot()).toEqual({
      loadStatus: "error",
      viewModel: null,
      loadError: OFFERS_REDEMPTION_LOG_LOAD_ERROR_MESSAGE,
    })
  })
})
