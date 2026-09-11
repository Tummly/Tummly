import { describe, expect, it, vi } from "vitest"

import {
  createInMemoryOperatorPrintMaterialsAdapters,
  createOperatorPrintMaterialsPageModule,
} from "./createOperatorPrintMaterialsPageModule"

describe("createOperatorPrintMaterialsPageModule", () => {
  it("keeps the same snapshot reference until state changes", async () => {
    const module = createOperatorPrintMaterialsPageModule(
      createInMemoryOperatorPrintMaterialsAdapters({ locations: [] })
    )

    expect(module.getSnapshot()).toBe(module.getSnapshot())

    await module.load(42)

    expect(module.getSnapshot()).toBe(module.getSnapshot())
  })

  it("maps readiness and download/retry affordances after load", async () => {
    const adapters = createInMemoryOperatorPrintMaterialsAdapters({
      locations: [
        {
          locationId: 11,
          locationName: "Front Room",
          assets: [
            {
              qrType: "TableTent",
              status: "Ready",
              fileName: "tummly-front-room-table-tent.pdf",
              lastError: null,
            },
            {
              qrType: "WindowSticker",
              status: "Preparing",
              fileName: null,
              lastError: null,
            },
            {
              qrType: "OfferCard",
              status: "Failed",
              fileName: null,
              lastError: "storage down",
            },
          ],
        },
      ],
    })
    const module = createOperatorPrintMaterialsPageModule(adapters)

    await module.load(42)

    expect(module.getSnapshot().loadStatus).toBe("loaded")
    expect(module.getSnapshot().rows).toEqual([
      expect.objectContaining({
        qrType: "TableTent",
        status: "Ready",
        canDownload: true,
        canRetry: false,
      }),
      expect.objectContaining({
        qrType: "WindowSticker",
        status: "Preparing",
        canDownload: false,
        canRetry: false,
      }),
      expect.objectContaining({
        qrType: "OfferCard",
        status: "Failed",
        canDownload: false,
        canRetry: true,
      }),
    ])
  })

  it("downloads only when Ready via the download adapter", async () => {
    const adapters = createInMemoryOperatorPrintMaterialsAdapters({
      locations: [
        {
          locationId: 11,
          locationName: "Front Room",
          assets: [
            {
              qrType: "TableTent",
              status: "Ready",
              fileName: "tent.pdf",
              lastError: null,
            },
          ],
        },
      ],
    })
    const download = vi.spyOn(adapters, "download")
    const click = vi.fn()
    const link = { href: "", download: "", click } as unknown as HTMLAnchorElement
    vi.stubGlobal("document", {
      createElement: vi.fn(() => link),
    })
    vi.stubGlobal("URL", {
      createObjectURL: vi.fn(() => "blob:mock"),
      revokeObjectURL: vi.fn(),
    })
    const module = createOperatorPrintMaterialsPageModule(adapters)
    await module.load(7)

    const ok = await module.download(11, "TableTent")

    expect(ok).toBe(true)
    expect(download).toHaveBeenCalledWith({
      userId: 7,
      locationId: 11,
      qrType: "TableTent",
    })
    expect(click).toHaveBeenCalled()
    vi.unstubAllGlobals()
  })

  it("retries Failed assets and unlocks download when Ready", async () => {
    const adapters = createInMemoryOperatorPrintMaterialsAdapters({
      locations: [
        {
          locationId: 11,
          locationName: "Front Room",
          assets: [
            {
              qrType: "OfferCard",
              status: "Failed",
              fileName: null,
              lastError: "boom",
            },
          ],
        },
      ],
    })
    const module = createOperatorPrintMaterialsPageModule(adapters)
    await module.load(7)

    expect(module.getSnapshot().rows[0]?.canRetry).toBe(true)
    expect(module.getSnapshot().rows[0]?.canDownload).toBe(false)

    const ok = await module.retry(11, "OfferCard")

    expect(ok).toBe(true)
    expect(module.getSnapshot().rows[0]).toMatchObject({
      status: "Ready",
      canDownload: true,
      canRetry: false,
    })
  })
})
