import { describe, expect, it, vi } from "vitest"

import {
  createFinishSettingUpAcksModule,
  createInMemoryFinishSettingUpAcksAdapters,
  type FinishSettingUpAcksAdapters,
} from "./createFinishSettingUpAcksModule"

describe("createFinishSettingUpAcksModule", () => {
  it("loads raw Finish-setting-up acknowledgements for an Owned location", async () => {
    const adapters = createInMemoryFinishSettingUpAcksAdapters({
      1: {
        guestFormPreviewed: true,
        qrPlacementGuideViewed: false,
        logoUploaded: false,
      },
    })
    const acks = createFinishSettingUpAcksModule(adapters)

    expect(acks.getSnapshot().loadStatus).toBe("idle")

    const loadPromise = acks.load(1)
    expect(acks.getSnapshot().loadStatus).toBe("loading")

    await loadPromise

    expect(acks.getSnapshot()).toMatchObject({
      loadStatus: "loaded",
      guestFormPreviewed: true,
      qrPlacementGuideViewed: false,
      logoUploaded: false,
      acknowledgeBusy: false,
      acknowledgeError: null,
    })
  })

  it("round-trips logoUploaded on load and update adapters", async () => {
    const adapters = createInMemoryFinishSettingUpAcksAdapters({
      1: {
        guestFormPreviewed: false,
        qrPlacementGuideViewed: false,
        logoUploaded: true,
      },
    })
    const acks = createFinishSettingUpAcksModule(adapters)
    await acks.load(1)

    expect(acks.getSnapshot().logoUploaded).toBe(true)

    const setChecklistAcks = vi.spyOn(adapters, "setChecklistAcks")
    await adapters.setChecklistAcks(1, { logoUploaded: true })

    expect(setChecklistAcks).toHaveBeenCalledWith(1, { logoUploaded: true })
  })

  it("acknowledges a field optimistically and persists via the HTTP adapter", async () => {
    const adapters = createInMemoryFinishSettingUpAcksAdapters({
      1: {
        guestFormPreviewed: false,
        qrPlacementGuideViewed: false,
        logoUploaded: false,
      },
    })
    const setChecklistAcks = vi.spyOn(adapters, "setChecklistAcks")
    const acks = createFinishSettingUpAcksModule(adapters)
    await acks.load(1)

    acks.acknowledge("guestFormPreviewed")

    expect(acks.getSnapshot()).toMatchObject({
      guestFormPreviewed: true,
      qrPlacementGuideViewed: false,
      acknowledgeBusy: true,
      acknowledgeError: null,
    })

    await vi.waitFor(() => {
      expect(acks.getSnapshot().acknowledgeBusy).toBe(false)
    })

    expect(setChecklistAcks).toHaveBeenCalledWith(1, {
      guestFormPreviewed: true,
    })
    expect(acks.getSnapshot().guestFormPreviewed).toBe(true)
  })

  it("does not re-POST when the field is already acknowledged", async () => {
    const adapters = createInMemoryFinishSettingUpAcksAdapters({
      1: {
        guestFormPreviewed: true,
        qrPlacementGuideViewed: false,
        logoUploaded: false,
      },
    })
    const setChecklistAcks = vi.spyOn(adapters, "setChecklistAcks")
    const acks = createFinishSettingUpAcksModule(adapters)
    await acks.load(1)

    acks.acknowledge("guestFormPreviewed")

    expect(setChecklistAcks).not.toHaveBeenCalled()
    expect(acks.getSnapshot().acknowledgeBusy).toBe(false)
  })

  it("rolls back an optimistic acknowledgement and surfaces a recoverable error", async () => {
    const adapters: FinishSettingUpAcksAdapters = {
      ...createInMemoryFinishSettingUpAcksAdapters({
        1: {
        guestFormPreviewed: false,
        qrPlacementGuideViewed: false,
        logoUploaded: false,
      },
      }),
      setChecklistAcks: async () => {
        throw new Error("network")
      },
    }
    const acks = createFinishSettingUpAcksModule(adapters)
    await acks.load(1)

    acks.acknowledge("guestFormPreviewed")
    expect(acks.getSnapshot().guestFormPreviewed).toBe(true)

    await vi.waitFor(() => {
      expect(acks.getSnapshot().acknowledgeBusy).toBe(false)
    })

    expect(acks.getSnapshot()).toMatchObject({
      guestFormPreviewed: false,
      acknowledgeError: "Could not save checklist progress. Please try again.",
    })
  })

  it("keeps qrPlacementGuideViewed on the acknowledge surface without enabling the guide CTA", async () => {
    const adapters = createInMemoryFinishSettingUpAcksAdapters({
      1: {
        guestFormPreviewed: false,
        qrPlacementGuideViewed: false,
        logoUploaded: false,
      },
    })
    const setChecklistAcks = vi.spyOn(adapters, "setChecklistAcks")
    const acks = createFinishSettingUpAcksModule(adapters)
    await acks.load(1)

    acks.acknowledge("qrPlacementGuideViewed")

    await vi.waitFor(() => {
      expect(acks.getSnapshot().acknowledgeBusy).toBe(false)
    })

    expect(setChecklistAcks).toHaveBeenCalledWith(1, {
      qrPlacementGuideViewed: true,
    })
    expect(acks.getSnapshot().qrPlacementGuideViewed).toBe(true)
  })

  it("notifies subscribers when the snapshot changes", async () => {
    const acks = createFinishSettingUpAcksModule(
      createInMemoryFinishSettingUpAcksAdapters()
    )
    const listener = vi.fn()
    const unsubscribe = acks.subscribe(listener)

    await acks.load(1)
    expect(listener).toHaveBeenCalled()

    unsubscribe()
    listener.mockClear()
    acks.reset()
    expect(listener).not.toHaveBeenCalled()
  })
})
