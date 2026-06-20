import axios from "axios"

import { API_BASE_URL } from "@/config/api"
import type { GuestFeedbackFormValues } from "@/schemas/guestFeedback"
import { toGuestFeedbackPayload } from "@/schemas/guestFeedback"

export type ScanLocationMetadata = {
  restaurantName: string
  locationName: string
}

type ScanMetadataResponse = {
  success: boolean
  restaurantName?: string
  locationName?: string
  message?: string
}

type ScanFeedbackResponse = {
  success: boolean
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
  }
}

export async function submitGuestFeedback(
  token: string,
  values: GuestFeedbackFormValues
): Promise<void> {
  const payload = toGuestFeedbackPayload(values)

  const response = await axios.post<ScanFeedbackResponse>(
    `${API_BASE_URL}/scan/${encodeURIComponent(token)}/feedback`,
    payload
  )

  if (!response.data.success) {
    throw new Error(response.data.message ?? "Unable to submit feedback.")
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
