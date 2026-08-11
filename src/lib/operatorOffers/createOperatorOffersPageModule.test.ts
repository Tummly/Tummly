import { describe, expect, it } from "vitest"

import {
  OFFERS_LOAD_ERROR_MESSAGE,
  createOperatorOffersPageModule,
} from "@/lib/operatorOffers/createOperatorOffersPageModule"
import { OFFERS_PAGE_COPY } from "@/lib/operatorOffers/offersPresentation"

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
})
