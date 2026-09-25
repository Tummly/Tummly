import axios from "axios"

import { API_BASE_URL } from "@/config/api"
import {
  parseGuestFormConsentFromScanMetadata,
  type GuestFormConsentConfig,
} from "@/lib/guestFeedback/guestFormConsentPresentation"
import type { GuestFeedbackUnlockChannel } from "@/lib/guestFeedback/guestFeedbackUnlockPresentation"
import type { GuestSttResult } from "@/lib/guestFeedback/createGuestMicSttModule"
import type { GuestFeedbackFormValues } from "@/schemas/guestFeedback"
import { toGuestFeedbackPayload } from "@/schemas/guestFeedback"

export type ScanLocationMetadata = {
  restaurantName: string
  locationName: string
  address: string
  brandLogoPublicUrl: string | null
  guestFormConsent: GuestFormConsentConfig | null
}

export type GuestThankYouOffer = {
  title: string
  description: string
  claimCode: string
  expiryLabel: string
}

export type GuestUnlockThankYouOffer = {
  title: string
  channel: GuestFeedbackUnlockChannel
  restaurantName: string
  locationName: string
  unlockToken: string
}

export type GuestFeedbackSubmitResult = {
  offer: GuestThankYouOffer | null
  unlockOffer: GuestUnlockThankYouOffer | null
}

type ScanMetadataResponse = {
  success: boolean
  restaurantName?: string
  locationName?: string
  address?: string
  brandLogoPublicUrl?: string | null
  guestFormConsent?: unknown
  message?: string
}

type ScanFeedbackResponse = {
  success: boolean
  message?: string
  offer?: GuestThankYouOffer | null
  unlockOffer?: GuestUnlockThankYouOffer | null
}

type ScanUnlockResponse = {
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
    brandLogoPublicUrl: response.data.brandLogoPublicUrl ?? null,
    guestFormConsent: parseGuestFormConsentFromScanMetadata(
      response.data.guestFormConsent
    ),
  }
}

export async function submitGuestFeedback(
  token: string,
  values: GuestFeedbackFormValues,
  options?: { recaptchaToken?: string | null }
): Promise<GuestFeedbackSubmitResult> {
  const payload = {
    ...toGuestFeedbackPayload(values),
    ...(options?.recaptchaToken
      ? { recaptchaToken: options.recaptchaToken }
      : {}),
  }

  const response = await axios.post<ScanFeedbackResponse>(
    `${API_BASE_URL}/scan/${encodeURIComponent(token)}/feedback`,
    payload
  )

  if (!response.data.success) {
    throw new Error(response.data.message ?? "Unable to submit feedback.")
  }

  return {
    offer: parseThankYouOffer(response.data.offer),
    unlockOffer: parseUnlockThankYouOffer(response.data.unlockOffer),
  }
}

export async function unlockGuestThankYouOffer(
  token: string,
  unlockToken: string
): Promise<GuestThankYouOffer> {
  const response = await axios.post<ScanUnlockResponse>(
    `${API_BASE_URL}/scan/${encodeURIComponent(token)}/thank-you-offer/unlock`,
    { unlockToken }
  )

  if (!response.data.success) {
    throw new Error(response.data.message ?? "Unable to unlock this offer.")
  }

  const offer = parseThankYouOffer(response.data.offer)
  if (offer == null) {
    throw new Error("Unable to unlock this offer.")
  }

  return offer
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

function parseUnlockThankYouOffer(raw: unknown): GuestUnlockThankYouOffer | null {
  if (raw == null || typeof raw !== "object") {
    return null
  }

  const value = raw as Record<string, unknown>
  const title = typeof value.title === "string" ? value.title.trim() : ""
  const unlockToken =
    typeof value.unlockToken === "string" ? value.unlockToken.trim() : ""
  const channelRaw =
    typeof value.channel === "string" ? value.channel.trim() : ""
  if (title === "" || unlockToken === "") {
    return null
  }
  if (channelRaw !== "email" && channelRaw !== "sms") {
    return null
  }

  return {
    title,
    channel: channelRaw,
    restaurantName:
      typeof value.restaurantName === "string" ? value.restaurantName.trim() : "",
    locationName:
      typeof value.locationName === "string" ? value.locationName.trim() : "",
    unlockToken,
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
        error.response?.status === 422
        && code === "empty_speech"
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
