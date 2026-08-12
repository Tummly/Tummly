import axios from "axios"

import { API_BASE_URL } from "@/config/api"
import type { GuestSttResult } from "@/lib/guestFeedback/createGuestMicSttModule"
import type { GuestFeedbackFormValues } from "@/schemas/guestFeedback"
import { toGuestFeedbackPayload } from "@/schemas/guestFeedback"

export type ScanLocationMetadata = {
  restaurantName: string
  locationName: string
  address: string
}

export type GuestThankYouOffer = {
  title: string
  description: string
  claimCode: string
  expiryLabel: string
}

type ScanMetadataResponse = {
  success: boolean
  restaurantName?: string
  locationName?: string
  address?: string
  message?: string
}

type ScanFeedbackResponse = {
  success: boolean
  message?: string
  offer?: GuestThankYouOffer | null
}

type ScanSttResponse = {
  success: boolean
  text?: string
  code?: string
  message?: string
}

export async function fetchScanLocationMetadata(
  token: string
): Promise<ScanLocationMetadata> {
  const response = await axios.get<ScanMetadataResponse>(
    `${API_BASE_URL}/scan/${encodeURIComponent(token)}`
  )

  if (!response.data.success) {
    throw new Error(response.data.message ?? "Link not found.")
  }

  return {
    restaurantName: response.data.restaurantName ?? "",
    locationName: response.data.locationName ?? "",
    address: response.data.address ?? "",
  }
}

export async function submitGuestFeedback(
  token: string,
  values: GuestFeedbackFormValues
): Promise<GuestThankYouOffer | null> {
  const payload = toGuestFeedbackPayload(values)

  const response = await axios.post<ScanFeedbackResponse>(
    `${API_BASE_URL}/scan/${encodeURIComponent(token)}/feedback`,
    payload
  )

  if (!response.data.success) {
    throw new Error(response.data.message ?? "Unable to submit feedback.")
  }

  return parseThankYouOffer(response.data.offer)
}

function parseThankYouOffer(raw: unknown): GuestThankYouOffer | null {
  if (raw == null || typeof raw !== "object") {
    return null
  }

  const offer = raw as Record<string, unknown>
  const title = typeof offer.title === "string" ? offer.title.trim() : ""
  const claimCode =
    typeof offer.claimCode === "string" ? offer.claimCode.trim() : ""
  if (title === "" || claimCode === "") {
    return null
  }

  return {
    title,
    description:
      typeof offer.description === "string" ? offer.description.trim() : "",
    claimCode,
    expiryLabel:
      typeof offer.expiryLabel === "string" ? offer.expiryLabel.trim() : "",
  }
}

export async function transcribeGuestAudio(
  token: string,
  audio: Blob
): Promise<GuestSttResult> {
  const formData = new FormData()
  const extension = audio.type.includes("ogg")
    ? "ogg"
    : audio.type.includes("mp4")
      ? "mp4"
      : "webm"
  formData.append("audio", audio, `clip.${extension}`)

  try {
    const response = await axios.post<ScanSttResponse>(
      `${API_BASE_URL}/scan/${encodeURIComponent(token)}/stt`,
      formData
    )

    if (!response.data.success || typeof response.data.text !== "string") {
      return { ok: false, reason: "stt_failure" }
    }

    return { ok: true, text: response.data.text }
  } catch (error) {
    if (axios.isAxiosError(error)) {
      if (error.response?.status === 429) {
        return { ok: false, reason: "rate_limit" }
      }

      const code = error.response?.data?.code
      if (
        error.response?.status === 422 &&
        code === "empty_speech"
      ) {
        return { ok: false, reason: "empty_speech" }
      }
    }

    return { ok: false, reason: "stt_failure" }
  }
}

export function getScanApiErrorMessage(error: unknown, fallback: string) {
  if (axios.isAxiosError(error)) {
    const message = error.response?.data?.message
    if (typeof message === "string" && message.trim()) {
      return message.trim()
    }

    if (error.response?.status === 404) {
      return "This link was not found or is no longer active."
    }

    if (error.response?.status === 429) {
      return "Too many submissions from this link. Please try again later."
    }
  }

  if (error instanceof Error && error.message.trim()) {
    return error.message.trim()
  }

  return fallback
}
