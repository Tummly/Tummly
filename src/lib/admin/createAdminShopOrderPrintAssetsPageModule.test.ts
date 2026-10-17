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
    productionStartedAtUtc: null,
    grossPence: 1000,
    lines: [],
    printAssets: [
      {
        qrType: "TableTent",
        quantity: 25,
        status: "Ready",
        fileName: "table-tent.csv",
        lastError: null,
        approved: false,
        printRoute: "PatchCsv60",
      },
      {
        qrType: "OfferCard",
        quantity: 50,
        status: "Failed",
        fileName: null,
        lastError: "storage unavailable",
        approved: false,
        printRoute: "GuestCardCsv",
      },
    ],
  }
}

describe("createAdminShopOrderPrintAssetsPageModule", () => {
  it("returns the same snapshot object until publish", () => {
    const module = createAdminShopOrderPrintAssetsPageModule({
      download: vi.fn(),
      retry: vi.fn(),
      approve: vi.fn(),
    })

    expect(module.getSnapshot()).toBe(module.getSnapshot())
    module.setOrder(order())
    expect(module.getSnapshot()).toBe(module.getSnapshot())
  })

  it("shows Admin Shop order readiness and one action per ordered type", () => {
    const module = createAdminShopOrderPrintAssetsPageModule({
      download: vi.fn(),
      retry: vi.fn(),
      approve: vi.fn(),
    })

    module.setOrder(order())

    expect(module.getSnapshot().rows).toEqual([
      expect.objectContaining({
        qrType: "TableTent",
        quantity: 25,
        canDownload: false,
        canApprove: true,
        canRetry: false,
      }),
      expect.objectContaining({
        qrType: "OfferCard",
        quantity: 50,
        canDownload: false,
        canApprove: false,
        canRetry: true,
      }),
    ])
  })

  it("keeps canDownload false until approved", () => {
    const module = createAdminShopOrderPrintAssetsPageModule({
      download: vi.fn(),
      retry: vi.fn(),
      approve: vi.fn(),
    })
    module.setOrder(order())

    expect(module.getSnapshot().rows[0]).toMatchObject({
      canDownload: false,
      canApprove: true,
    })
  })

  it("approves then enables download with CSV filename", async () => {
    const approve = vi.fn().mockResolvedValue({
      qrType: "TableTent",
      quantity: 25,
      status: "Ready",
      fileName: "table-tent.csv",
      lastError: null,
      approved: true,
      printRoute: "PatchCsv60",
    })
    const download = vi.fn().mockResolvedValue(
      new Blob(["JobId,OrderId"], { type: "text/csv" })
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
      approve,
    })
    module.setOrder(order())

    await expect(module.download("TableTent")).resolves.toBe(false)
    await expect(module.approve("TableTent")).resolves.toBe(true)
    expect(module.getSnapshot().rows[0]).toMatchObject({
      approved: true,
      canDownload: true,
      canApprove: false,
    })
    await expect(module.download("TableTent")).resolves.toBe(true)
    expect(download).toHaveBeenCalledWith("order-1", "TableTent")
    expect(link.download).toBe("table-tent.csv")
    expect(link.click).toHaveBeenCalled()
    vi.unstubAllGlobals()
  })

  it("retries a Failed Admin Shop order asset and enables approve", async () => {
    const retry = vi.fn().mockResolvedValue({
      qrType: "OfferCard",
      quantity: 50,
      status: "Ready",
      fileName: "offer-card.csv",
      lastError: null,
      approved: false,
      printRoute: "GuestCardCsv",
    })
    const module = createAdminShopOrderPrintAssetsPageModule({
      download: vi.fn(),
      retry,
      approve: vi.fn(),
    })
    module.setOrder(order())

    await expect(module.retry("OfferCard")).resolves.toBe(true)

    expect(module.getSnapshot().rows[1]).toMatchObject({
      status: "Ready",
      canDownload: false,
      canApprove: true,
      canRetry: false,
    })
  })

  it("keeps retry available when generation remains Failed", async () => {
    const retry = vi.fn().mockResolvedValue({
      qrType: "OfferCard",
      quantity: 50,
      status: "Failed",
      fileName: null,
      lastError: "storage still unavailable",
      approved: false,
      printRoute: "GuestCardCsv",
    })
    const module = createAdminShopOrderPrintAssetsPageModule({
      download: vi.fn(),
      retry,
      approve: vi.fn(),
    })
    module.setOrder(order())

    await expect(module.retry("OfferCard")).resolves.toBe(false)

    expect(module.getSnapshot().rows[1]).toMatchObject({
      status: "Failed",
      lastError: "storage still unavailable",
      canDownload: false,
      canApprove: false,
      canRetry: true,
    })
  })
})
