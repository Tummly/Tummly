import { isAxiosError } from "axios"

import axiosInstance from "@/api/axiosInstance"

export type CreateOwnedLocationInput = {
  locationName: string
  address: string
  city: string
  postcode: string
}

function readApiError(error: unknown, fallback: string): string {
  if (isAxiosError(error)) {
    const payload = error.response?.data as
      | {
          message?: unknown
          code?: unknown
          cap?: unknown
          current?: unknown
        }
      | undefined
    if (payload?.code === "location_cap_reached") {
      const cap =
        typeof payload.cap === "number" ? payload.cap : undefined
      const current =
        typeof payload.current === "number" ? payload.current : undefined
      if (cap != null && current != null) {
        return `Location cap reached (${current} of ${cap}).`
      }
      return "Location cap reached."
    }
    const message = payload?.message
    if (typeof message === "string" && message.trim() !== "") {
      return message
    }
  }
  return fallback
}

export async function createOwnedLocation(
  input: CreateOwnedLocationInput
): Promise<{ locationId: number }> {
  try {
    const response = await axiosInstance.post<{
      success?: boolean
      locationId?: number
    }>("/locations", {
      locationName: input.locationName,
      address: input.address,
      city: input.city,
      postcode: input.postcode,
    })
    const locationId = response.data.locationId
    if (typeof locationId !== "number") {
      throw new Error("Create location response missing locationId.")
    }
    return { locationId }
  } catch (error) {
    throw new Error(readApiError(error, "Could not create location."))
  }
}

export async function activateOwnedLocation(locationId: number): Promise<void> {
  try {
    await axiosInstance.post(`/locations/${locationId}/activate`, {})
  } catch (error) {
    throw new Error(readApiError(error, "Could not activate location."))
  }
}

export async function deleteOwnedLocationDraft(
  locationId: number
): Promise<void> {
  try {
    await axiosInstance.delete(`/locations/${locationId}`)
  } catch (error) {
    throw new Error(readApiError(error, "Could not delete draft."))
  }
}

export async function setOwnedLocationManager(
  locationId: number,
  managerUserId: number | null
): Promise<void> {
  try {
    await axiosInstance.put(`/locations/${locationId}/manager`, {
      managerUserId,
    })
  } catch (error) {
    throw new Error(readApiError(error, "Could not update manager."))
  }
}
