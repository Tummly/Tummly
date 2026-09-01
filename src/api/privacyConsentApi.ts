import { isAxiosError } from "axios"

import axiosInstance from "@/api/axiosInstance"

export type SavePrivacyConsentInput = {
  smsConsentWording?: string
  emailConsentWording?: string
}

function readApiError(error: unknown, fallback: string): string {
  if (isAxiosError(error)) {
    const payload = error.response?.data as { message?: unknown } | undefined
    const message = payload?.message
    if (typeof message === "string" && message.trim() !== "") {
      return message
    }
  }
  return fallback
}

/** Minimal Privacy consent ready + wording save (Locations Setup / ticket 07). */
export async function savePrivacyConsent(
  input: SavePrivacyConsentInput = {}
): Promise<{ privacyReady: boolean }> {
  try {
    const response = await axiosInstance.put<{
      success?: boolean
      privacyReady?: boolean
    }>("/privacy-consent", {
      smsConsentWording: input.smsConsentWording ?? "",
      emailConsentWording: input.emailConsentWording ?? "",
    })
    return {
      privacyReady: response.data.privacyReady === true,
    }
  } catch (error) {
    throw new Error(
      readApiError(error, "Could not save Privacy consent.")
    )
  }
}
