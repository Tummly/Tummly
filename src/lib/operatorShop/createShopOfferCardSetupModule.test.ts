import { describe, expect, it, vi } from "vitest"

import type { ShopOfferCardOfferFact } from "@/api/shopOfferCardOfferApi"
import { createShopOfferCardSetupModule } from "@/lib/operatorShop/createShopOfferCardSetupModule"

function createModule(options?: {
  attached?: ShopOfferCardOfferFact
  failCreate?: boolean
  failAttach?: boolean
  enabled?: boolean
  guestCardAvailable?: boolean
}) {
  let attached = options?.attached ?? {
    offerId: null,
    title: null,
    live: false,
  }
  let locationId: number | null = 9
  const getAttached = vi.fn(async () => attached)
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
    locationId: () => locationId,
    isEnabled: () => options?.enabled !== false,
    isGuestCardAvailable: () => options?.guestCardAvailable !== false,
    getAttached,
    createCatalogOffer,
    putOfferCardOffer,
  })

  /** Switches the selected location and the attach state that the next load reads. */
  const moveToLocation = (nextLocationId: number, nextAttached: ShopOfferCardOfferFact) => {
    locationId = nextLocationId
    attached = nextAttached
  }

  return { module, createCatalogOffer, putOfferCardOffer, getAttached, moveToLocation }
}

describe("createShopOfferCardSetupModule", () => {
  it("loadForLocation never opens when attach is missing", async () => {
    const { module } = createModule()

    await module.loadForLocation()

    const snap = module.getSnapshot()
    expect(snap.loadStatus).toBe("ready")
    expect(snap.hasAttach).toBe(false)
    expect(snap.isOpen).toBe(false)
  })

  it("loadForLocation never opens when attach exists", async () => {
    const { module } = createModule({
      attached: { offerId: 7, title: "Existing", live: true },
    })

    await module.loadForLocation()

    const snap = module.getSnapshot()
    expect(snap.hasAttach).toBe(true)
    expect(snap.isOpen).toBe(false)
  })

  it("open returns noop when Guest Cards unavailable", async () => {
    const { module } = createModule({ guestCardAvailable: false })
    await module.loadForLocation()

    expect(module.open()).toBe("noop")
    expect(module.getSnapshot().isOpen).toBe(false)
  })

  it("location change closes an open dialog and reloads attach state", async () => {
    const { module, getAttached, moveToLocation } = createModule()
    await module.loadForLocation()
    expect(module.open()).toBe("opened")
    expect(module.getSnapshot().isOpen).toBe(true)

    moveToLocation(10, { offerId: 7, title: "Other location offer", live: true })
    await module.loadForLocation()

    expect(module.getSnapshot()).toMatchObject({ isOpen: false, hasAttach: true })
    expect(getAttached).toHaveBeenCalledTimes(2)
  })

  it("stays closed on Free without calling attach", async () => {
    const { module, getAttached } = createModule({ enabled: false })

    await module.loadForLocation()

    const snap = module.getSnapshot()
    expect(snap.loadStatus).toBe("ready")
    expect(snap.isOpen).toBe(false)
    expect(snap.hasAttach).toBe(false)
    expect(getAttached).not.toHaveBeenCalled()
    expect(module.open()).toBe("noop")
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
    expect(module.open()).toBe("opened")
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

  it("title follows the percentage value until the operator types", async () => {
    const { module } = createModule()
    await module.loadForLocation()
    module.open()

    module.setOfferType("percentage_discount")
    expect(module.getSnapshot().draft.title).toBe("Get 20% off your next visit")

    module.patchDraft({ discountPercentage: "30" })
    expect(module.getSnapshot().draft.title).toBe("Get 30% off your next visit")

    module.patchDraft({ discountPercentage: "40" })
    expect(module.getSnapshot().draft.title).toBe("Get 40% off your next visit")
  })

  it("keeps a manually typed title when the value changes", async () => {
    const { module } = createModule()
    await module.loadForLocation()
    module.open()
    module.setOfferType("percentage_discount")

    module.patchDraft({ title: "Welcome back offer" })
    module.patchDraft({ discountPercentage: "50" })

    expect(module.getSnapshot().draft.title).toBe("Welcome back offer")
    expect(module.getSnapshot().draft.titleTouched).toBe(true)
  })

  it("keeps a manually typed title when the offer type changes", async () => {
    const { module } = createModule()
    await module.loadForLocation()
    module.open()
    module.setOfferType("percentage_discount")
    module.patchDraft({ title: "Welcome back offer" })

    module.setOfferType("fixed_discount")

    expect(module.getSnapshot().draft.title).toBe("Welcome back offer")
  })

  it("sends default description, validity and staff instructions on confirm", async () => {
    const { module, createCatalogOffer } = createModule()
    await module.loadForLocation()
    module.open()
    module.setOfferType("percentage_discount")

    const result = await module.confirm()

    expect(result).toBe("created")
    expect(createCatalogOffer).toHaveBeenCalledWith({
      locationId: 9,
      offerType: "percentage_discount",
      title: "Get 20% off your next visit",
      description:
        "Show this offer to a member of staff on your next visit to redeem it.",
      validity: "30_days_after_issue",
      staffInstructions:
        "Ask the guest for their unique offer code. Check the code in Tummly and select Confirm redemption before applying the offer.",
      discountPercentage: 20,
    })
  })

  it("does not confirm with a blank percentage", async () => {
    const { module, createCatalogOffer } = createModule()
    await module.loadForLocation()
    module.open()
    module.setOfferType("percentage_discount")
    module.patchDraft({ discountPercentage: "" })

    expect(module.getSnapshot().canConfirm).toBe(false)
    expect(await module.confirm()).toBe("noop")
    expect(createCatalogOffer).not.toHaveBeenCalled()
  })
})
