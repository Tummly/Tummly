import { describe, expect, it, vi } from "vitest"

import type { AdminShopOrderListItem } from "@/api/adminShopOrdersApi"
import { createAdminShopOrderPrintAssetsPageModule } from "./createAdminShopOrderPrintAssetsPageModule"

function order(): AdminShopOrderListItem {
  return {
    id: "order-1",
    orderNumber: "ORD-1",
    restaurantId: 10,
    locationId: 11,
    locationNameSnapshot: "Front Room",
    fulfilmentStatus: "processing",
    paymentStatus: "paid",
    revolutOrderId: "revolut-1",
    trackingUrl: null,
    opsNotes: null,
    paidAtUtc: "2026-09-11T12:00:00Z",
    grossPence: 1000,
    lines: [],
    printAssets: [
      {
        qrType: "TableTent",
        quantity: 25,
        status: "Ready",
        fileName: "table-tent.pdf",
        lastError: null,
      },
      {
        qrType: "OfferCard",
        quantity: 50,
        status: "Failed",
        fileName: null,
        lastError: "storage unavailable",
      },
    ],
  }
}

describe("createAdminShopOrderPrintAssetsPageModule", () => {
  it("returns the same snapshot object until publish", () => {
    const module = createAdminShopOrderPrintAssetsPageModule({
      download: vi.fn(),
      retry: vi.fn(),
    })

    expect(module.getSnapshot()).toBe(module.getSnapshot())
    module.setOrder(order())
    expect(module.getSnapshot()).toBe(module.getSnapshot())
  })

  it("shows Admin Shop order readiness and one action per ordered type", () => {
    const module = createAdminShopOrderPrintAssetsPageModule({
      download: vi.fn(),
      retry: vi.fn(),
    })

    module.setOrder(order())

    expect(module.getSnapshot().rows).toEqual([
      expect.objectContaining({
        qrType: "TableTent",
        quantity: 25,
        canDownload: true,
        canRetry: false,
      }),
      expect.objectContaining({
        qrType: "OfferCard",
        quantity: 50,
        canDownload: false,
        canRetry: true,
      }),
    ])
  })

  it("downloads a Ready Admin Shop order PDF", async () => {
    const download = vi.fn().mockResolvedValue(
      new Blob(["%PDF"], { type: "application/pdf" })
    )
    const link = {
      href: "",
      download: "",
      click: vi.fn(),
    } as unknown as HTMLAnchorElement
    vi.stubGlobal("document", { createElement: vi.fn(() => link) })
    vi.stubGlobal("URL", {
      createObjectURL: vi.fn(() => "blob:print"),
      revokeObjectURL: vi.fn(),
    })
    const module = createAdminShopOrderPrintAssetsPageModule({
      download,
      retry: vi.fn(),
    })
    module.setOrder(order())

    await expect(module.download("TableTent")).resolves.toBe(true)
    await expect(module.download("OfferCard")).resolves.toBe(false)
    expect(download).toHaveBeenCalledTimes(1)
    expect(download).toHaveBeenCalledWith("order-1", "TableTent")
    expect(link.click).toHaveBeenCalled()
    vi.unstubAllGlobals()
  })

  it("retries a Failed Admin Shop order PDF and enables download", async () => {
    const retry = vi.fn().mockResolvedValue({
      qrType: "OfferCard",
      quantity: 50,
      status: "Ready",
      fileName: "offer-card.pdf",
      lastError: null,
    })
    const module = createAdminShopOrderPrintAssetsPageModule({
      download: vi.fn(),
      retry,
    })
    module.setOrder(order())

    await expect(module.retry("OfferCard")).resolves.toBe(true)

    expect(module.getSnapshot().rows[1]).toMatchObject({
      status: "Ready",
      canDownload: true,
      canRetry: false,
    })
  })
})
