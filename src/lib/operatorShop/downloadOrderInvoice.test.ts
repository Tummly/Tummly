import { describe, expect, it, vi, beforeEach } from "vitest"
import { downloadShopOrderInvoicePdf } from "./downloadOrderInvoice"

const fetchBillingCreditsInvoicePdf = vi.fn()
const downloadInvoicePdfBlob = vi.fn()

vi.mock("@/api/billingCreditsApi", () => ({
  fetchBillingCreditsInvoicePdf: (...args: unknown[]) =>
    fetchBillingCreditsInvoicePdf(...args),
  downloadInvoicePdfBlob: (...args: unknown[]) =>
    downloadInvoicePdfBlob(...args),
}))

describe("downloadShopOrderInvoicePdf", () => {
  beforeEach(() => {
    fetchBillingCreditsInvoicePdf.mockReset()
    downloadInvoicePdfBlob.mockReset()
  })

  it("returns false when invoice document number is missing", async () => {
    await expect(downloadShopOrderInvoicePdf(null)).resolves.toBe(false)
    await expect(downloadShopOrderInvoicePdf("  ")).resolves.toBe(false)
    expect(fetchBillingCreditsInvoicePdf).not.toHaveBeenCalled()
  })

  it("fetches and downloads the TM VAT invoice PDF", async () => {
    const blob = new Blob(["pdf"], { type: "application/pdf" })
    fetchBillingCreditsInvoicePdf.mockResolvedValue(blob)

    await expect(
      downloadShopOrderInvoicePdf("TM-2026-000042")
    ).resolves.toBe(true)

    expect(fetchBillingCreditsInvoicePdf).toHaveBeenCalledWith(
      "TM-2026-000042"
    )
    expect(downloadInvoicePdfBlob).toHaveBeenCalledWith(
      blob,
      "TM-2026-000042"
    )
  })
})
