import axiosInstance from "@/api/axiosInstance"
import { LOCATION_UPLOAD_TEMPLATE_FILENAME } from "@/lib/locationUpload/locationUploadConstants"
import { isAxiosError } from "axios"

function triggerBrowserDownload(blob: Blob, filename: string) {
  const url = URL.createObjectURL(blob)
  const link = document.createElement("a")
  link.href = url
  link.download = filename
  link.rel = "noopener"
  document.body.append(link)
  link.click()
  link.remove()
  URL.revokeObjectURL(url)
}

export async function downloadLocationUploadTemplate(): Promise<void> {
  try {
    const response = await axiosInstance.get<Blob>(
      "/auth/locations-upload-template",
      {
        responseType: "blob",
        skipAuthRedirect: true,
      }
    )

    triggerBrowserDownload(response.data, LOCATION_UPLOAD_TEMPLATE_FILENAME)
  } catch (error: unknown) {
    if (isAxiosError(error) && error.response?.status === 404) {
      throw new Error("Locations upload template is unavailable.")
    }

    throw new Error("Unable to download the locations template. Please try again.")
  }
}
