import { describe, expect, it, vi } from "vitest"

import type { ShopOfferCardOfferFact } from "@/api/shopOfferCardOfferApi"
import { createShopOfferCardSetupModule } from "@/lib/operatorShop/createShopOfferCardSetupModule"

function createModule(options?: {
  attached?: ShopOfferCardOfferFact
  failCreate?: boolean
  failAttach?: boolean
}) {
  let attached = options?.attached ?? {
    offerId: null,
    title: null,
    live: false,
  }
  const createCatalogOffer = vi.fn(async () => {
    if (options?.failCreate) {
      throw new Error("create failed")
    }
    return { id: 42, title: "Get 20% off your next visit" }
  })
  const putOfferCardOffer = vi.fn(async () => {
    if (options?.failAttach) {
      throw new Error("attach failed")
    }
    attached = {
      offerId: 42,
      title: "Get 20% off your next visit",
      live: true,
    }
    return attached
  })

  const module = createShopOfferCardSetupModule({
    locationId: () => 9,
    getAttached: async () => attached,
    createCatalogOffer,
    putOfferCardOffer,
  })

  return { module, createCatalogOffer, putOfferCardOffer }
}

describe("createShopOfferCardSetupModule", () => {
  it("opens on load when attach is missing", async () => {
    const { module } = createModule()

    await module.loadForLocation()

    const snap = module.getSnapshot()
    expect(snap.loadStatus).toBe("ready")
    expect(snap.hasAttach).toBe(false)
    expect(snap.isOpen).toBe(true)
  })

  it("stays closed on load when attach exists", async () => {
    const { module } = createModule({
      attached: { offerId: 7, title: "Existing", live: true },
    })

    await module.loadForLocation()

    const snap = module.getSnapshot()
    expect(snap.hasAttach).toBe(true)
    expect(snap.isOpen).toBe(false)
  })

  it("Cancel closes without creating", async () => {
    const { module, createCatalogOffer } = createModule()
    await module.loadForLocation()

    module.close()

    expect(module.getSnapshot().isOpen).toBe(false)
    expect(createCatalogOffer).not.toHaveBeenCalled()
  })

  it("Continue creates and attaches then closes", async () => {
    const { module, createCatalogOffer, putOfferCardOffer } = createModule()
    await module.loadForLocation()
    module.setOfferType("percentage_discount")
    module.patchDraft({
      title: "Get 20% off your next visit",
      titleTouched: true,
      discountPercentage: "20",
      description: "Offer printed on physical Offer Cards.",
    })

    const result = await module.confirm()

    expect(result).toBe("created")
    expect(createCatalogOffer).toHaveBeenCalledOnce()
    expect(putOfferCardOffer).toHaveBeenCalledWith(9, 42)
    expect(module.getSnapshot().isOpen).toBe(false)
    expect(module.getSnapshot().hasAttach).toBe(true)
  })
})
