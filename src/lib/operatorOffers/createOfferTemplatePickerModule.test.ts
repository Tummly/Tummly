import { describe, expect, it, vi } from "vitest"

import { OFFER_TEMPLATE_PICKER_COPY } from "@/lib/operatorOffers/offerTemplatePickerPresentation"
import { createOfferTemplatePickerModule } from "@/lib/operatorOffers/createOfferTemplatePickerModule"
import type { OfferTemplateSeedItem } from "@/lib/operatorOffers/offerTemplateSeed"
import { OFFER_TEMPLATE_SEED } from "@/lib/operatorOffers/offerTemplateSeed"

function sampleTemplates(): OfferTemplateSeedItem[] {
  return [
    OFFER_TEMPLATE_SEED[0]!,
    OFFER_TEMPLATE_SEED[1]!,
  ]
}

describe("createOfferTemplatePickerModule", () => {
  it("caches getSnapshot for useSyncExternalStore identity", () => {
    const picker = createOfferTemplatePickerModule({
      loadTemplates: async () => sampleTemplates(),
    })

    const first = picker.getSnapshot()
    const second = picker.getSnapshot()
    expect(second).toBe(first)
  })

  it("loads catalogue cards when opened", async () => {
    const loadTemplates = vi.fn(async () => sampleTemplates())
    const picker = createOfferTemplatePickerModule({ loadTemplates })

    await picker.open()

    expect(loadTemplates).toHaveBeenCalledTimes(1)
    const snapshot = picker.getSnapshot()
    expect(snapshot.open).toBe(true)
    expect(snapshot.loadStatus).toBe("loaded")
    expect(snapshot.viewModel?.cards).toHaveLength(2)
    expect(snapshot.viewModel?.cards[0]).toMatchObject({
      id: "welcome-new-guests",
      title: "Welcome new guests",
      previewDisabled: true,
      useTemplateEnabled: true,
    })
    expect(snapshot.viewModel?.title).toBe(OFFER_TEMPLATE_PICKER_COPY.title)
  })

  it("treats an empty catalogue as an error with no fallback cards", async () => {
    const loadTemplates = vi.fn(async () => [])
    const picker = createOfferTemplatePickerModule({ loadTemplates })

    await picker.open()

    const snapshot = picker.getSnapshot()
    expect(snapshot.loadStatus).toBe("error")
    expect(snapshot.loadError).toBe(OFFER_TEMPLATE_PICKER_COPY.emptyError)
    expect(snapshot.viewModel).toBeNull()
  })

  it("retries a failed load after open", async () => {
    const loadTemplates = vi
      .fn()
      .mockRejectedValueOnce(new Error("network"))
      .mockResolvedValueOnce(sampleTemplates())

    const picker = createOfferTemplatePickerModule({ loadTemplates })
    await picker.open()

    expect(picker.getSnapshot().loadStatus).toBe("error")
    expect(picker.getSnapshot().loadError).toBe(
      OFFER_TEMPLATE_PICKER_COPY.loadError
    )

    await picker.retryLoad()

    expect(loadTemplates).toHaveBeenCalledTimes(2)
    expect(picker.getSnapshot().loadStatus).toBe("loaded")
    expect(picker.getSnapshot().viewModel?.cards).toHaveLength(2)
  })

  it("filters cards by Offers-specific search fields", async () => {
    const picker = createOfferTemplatePickerModule({
      loadTemplates: async () => [...OFFER_TEMPLATE_SEED],
    })
    await picker.open()

    picker.setSearchQuery("Guest form signup")
    expect(picker.getSnapshot().viewModel?.cards).toHaveLength(1)
    expect(picker.getSnapshot().viewModel?.cards[0]?.id).toBe(
      "welcome-new-guests"
    )
    expect(picker.getSnapshot().viewModel?.showSearchMiss).toBe(false)

    picker.setSearchQuery("zzzz")
    expect(picker.getSnapshot().viewModel?.cards).toHaveLength(0)
    expect(picker.getSnapshot().viewModel?.showSearchMiss).toBe(true)
  })

  it("keeps loaded catalogue while closing so the exit animation does not flash idle", async () => {
    const picker = createOfferTemplatePickerModule({
      loadTemplates: async () => sampleTemplates(),
    })
    await picker.open()
    picker.setSearchQuery("welcome")

    picker.close()

    const snapshot = picker.getSnapshot()
    expect(snapshot.open).toBe(false)
    expect(snapshot.loadStatus).toBe("loaded")
    expect(snapshot.viewModel?.cards).toHaveLength(2)
    expect(snapshot.viewModel?.searchQuery).toBe("")
  })

  it("cancels an in-flight load when closed", async () => {
    let resolveLoad: ((items: OfferTemplateSeedItem[]) => void) | undefined
    const loadTemplates = vi.fn(
      () =>
        new Promise<OfferTemplateSeedItem[]>((resolve) => {
          resolveLoad = resolve
        })
    )
    const picker = createOfferTemplatePickerModule({ loadTemplates })

    const openPromise = picker.open()
    expect(picker.getSnapshot().loadStatus).toBe("loading")

    picker.close()
    expect(picker.getSnapshot().open).toBe(false)
    expect(picker.getSnapshot().loadStatus).toBe("idle")

    resolveLoad!(sampleTemplates())
    await openPromise

    expect(picker.getSnapshot().loadStatus).toBe("idle")
    expect(picker.getSnapshot().viewModel).toBeNull()
  })
})
