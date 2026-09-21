import axios from "axios"

import { API_BASE_URL } from "@/config/api"

export type UnsubscribePreview =
  | { valid: false }
  | { valid: true; restaurantName: string }

type PreviewResponse = {
  valid?: boolean
  restaurantName?: string
}

type SuccessResponse = {
  success?: boolean
}

export async function fetchUnsubscribePreview(
  token: string
): Promise<UnsubscribePreview> {
  const response = await axios.get<PreviewResponse>(
    `${API_BASE_URL}/public/unsubscribe/preview`,
    { params: { t: token } }
  )

  if (response.data.valid === true) {
    const restaurantName =
      typeof response.data.restaurantName === "string"
        ? response.data.restaurantName.trim()
        : ""
    if (restaurantName.length > 0) {
      return { valid: true, restaurantName }
    }
  }

  return { valid: false }
}

export async function confirmUnsubscribe(token: string): Promise<void> {
  await axios.post<SuccessResponse>(
    `${API_BASE_URL}/public/unsubscribe/confirm`,
    { token }
  )
}

export async function submitUnsubscribeForm(
  email: string,
  restaurantId: number
): Promise<void> {
  await axios.post<SuccessResponse>(`${API_BASE_URL}/public/unsubscribe/form`, {
    email,
    restaurantId,
  })
}
