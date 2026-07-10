import axiosInstance from "@/api/axiosInstance"
import type { LegalDocumentKey } from "@/content/legal/types"
import { isAxiosError } from "axios"

const DOWNLOAD_FILENAMES: Record<LegalDocumentKey, string> = {
  privacy: "Tummly_Privacy_Policy.docx",
  terms: "Tummly_Terms_and_Conditions.docx",
  "cookie-policy": "Tummly_Cookie_Policy.docx",
}

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

export async function downloadLegalDocument(
  documentKey: LegalDocumentKey
): Promise<void> {
  try {
    const response = await axiosInstance.get<Blob>(
      `/legal/documents/${documentKey}`,
      {
        responseType: "blob",
        skipAuthRedirect: true,
      }
    )

    triggerBrowserDownload(response.data, DOWNLOAD_FILENAMES[documentKey])
  } catch (error: unknown) {
    if (isAxiosError(error) && error.response?.status === 404) {
      throw new Error("Legal document is unavailable.")
    }

    throw new Error("Unable to download the legal document. Please try again.")
  }
}
