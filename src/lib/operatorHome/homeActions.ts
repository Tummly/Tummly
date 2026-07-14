import { downloadQrCode as defaultDownloadQrCode } from "@/api/dashboardApi"

export type HomeActionResult =
  | { ok: true }
  | { ok: false; error: string }

export const QR_DOWNLOAD_ERROR =
  "Could not download QR code. Please try again."

export function buildQrDownloadFilename(locationName: string): string {
  const trimmed = locationName.trim() || "location"
  return `QR_${trimmed}.png`
}

export function triggerBrowserDownload(blob: Blob, filename: string): void {
  const objectUrl = URL.createObjectURL(blob)
  const link = document.createElement("a")
  link.href = objectUrl
  link.download = filename
  document.body.appendChild(link)
  link.click()
  document.body.removeChild(link)
  URL.revokeObjectURL(objectUrl)
}

type DownloadSelectedLocationQrInput = {
  locationId: number
  locationName: string
  downloadQrCode?: (locationId: number) => Promise<Blob>
  triggerBrowserDownload?: (blob: Blob, filename: string) => void
}

/** Download the QR PNG for the selected Owned location via the existing QR API. */
export async function downloadSelectedLocationQr(
  input: DownloadSelectedLocationQrInput
): Promise<HomeActionResult> {
  const download = input.downloadQrCode ?? defaultDownloadQrCode
  const save = input.triggerBrowserDownload ?? triggerBrowserDownload

  try {
    const blob = await download(input.locationId)
    save(blob, buildQrDownloadFilename(input.locationName))
    return { ok: true }
  } catch {
    return { ok: false, error: QR_DOWNLOAD_ERROR }
  }
}
