import { beforeEach, describe, expect, it, vi } from "vitest"

import { downloadSelectedLocationQr } from "./homeActions"

describe("downloadSelectedLocationQr", () => {
  const downloadQrCode = vi.fn()
  const triggerBrowserDownload = vi.fn()

  beforeEach(() => {
    downloadQrCode.mockReset()
    triggerBrowserDownload.mockReset()
  })

  it("downloads the QR asset for the selected Owned location", async () => {
    const blob = new Blob(["png"], { type: "image/png" })
    downloadQrCode.mockResolvedValue(blob)

    await expect(
      downloadSelectedLocationQr({
        locationId: 42,
        locationName: "Mehmet's Grill",
        downloadQrCode,
        triggerBrowserDownload,
      })
    ).resolves.toEqual({ ok: true })

    expect(downloadQrCode).toHaveBeenCalledWith(42)
    expect(triggerBrowserDownload).toHaveBeenCalledWith(
      blob,
      "QR_Mehmet's Grill.png"
    )
  })

  it("returns a recoverable error when QR download fails", async () => {
    downloadQrCode.mockRejectedValue(new Error("network"))

    await expect(
      downloadSelectedLocationQr({
        locationId: 42,
        locationName: "Mehmet's Grill",
        downloadQrCode,
        triggerBrowserDownload,
      })
    ).resolves.toEqual({
      ok: false,
      error: "Could not download QR code. Please try again.",
    })

    expect(triggerBrowserDownload).not.toHaveBeenCalled()
  })
})
