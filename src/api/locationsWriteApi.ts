import { isAxiosError } from "axios"

import axiosInstance from "@/api/axiosInstance"
import { getUserFacingApiErrorMessage } from "@/lib/apiErrorMessage"

export type CreateOwnedLocationInput = {
  locationName: string
  address: string
  city: string
  postcode: string
  locationPhone?: string
  localContact?: string
  /** Optional FSA establishment id. Sent as typed; the server never looks it up. */
  fsaEstablishmentId?: string
}

/**
 * Generic premises warning. Every match looks the same: no scope, id or name.
 * `matchedRowIndex` is set only for an earlier row of the caller's own file.
 */
export type PossibleDuplicatePremisesWarning = {
  code: string
  message?: string
  matchedRowIndex?: number | null
}

function readApiError(error: unknown, fallback: string): string {
  if (isAxiosError(error)) {
    const payload = error.response?.data as
      | {
          code?: unknown
          cap?: unknown
          current?: unknown
          scheduledChangeMessage?: unknown
        }
      | undefined
    if (payload?.code === "location_cap_reached") {
      if (typeof payload.scheduledChangeMessage === "string") {
        return payload.scheduledChangeMessage
      }
      const cap =
        typeof payload.cap === "number" ? payload.cap : undefined
      const current =
        typeof payload.current === "number" ? payload.current : undefined
      if (cap != null && current != null) {
        return `Location cap reached (${current} of ${cap}).`
      }
      return "Location cap reached."
    }
  }
  return getUserFacingApiErrorMessage(error, fallback)
}

export async function createOwnedLocation(
  input: CreateOwnedLocationInput
): Promise<{
  locationId: number
  warning?: PossibleDuplicatePremisesWarning | null
}> {
  try {
    const response = await axiosInstance.post<{
      success?: boolean
      locationId?: number
      warning?: PossibleDuplicatePremisesWarning | null
    }>("/locations", {
      locationName: input.locationName,
      address: input.address,
      city: input.city,
      postcode: input.postcode,
      locationPhone: input.locationPhone,
      localContact: input.localContact,
      fsaEstablishmentId: input.fsaEstablishmentId,
    })
    const locationId = response.data.locationId
    if (typeof locationId !== "number") {
      throw new Error("Create location response missing locationId.")
    }
    return { locationId, warning: response.data.warning ?? null }
  } catch (error) {
    throw new Error(readApiError(error, "Could not create location."))
  }
}

export async function activateOwnedLocation(
  locationId: number
): Promise<{ accountType?: string }> {
  try {
    const response = await axiosInstance.post<{
      success?: boolean
      accountType?: string
    }>(`/locations/${locationId}/activate`, {})
    const accountType =
      typeof response.data.accountType === "string"
        ? response.data.accountType
        : undefined
    return { accountType }
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

export type ImportOwnedLocationRowInput = {
  locationName: string
  address: string
  city: string
  postcode: string
  locationPhone?: string
  localContact?: string
  fsaEstablishmentId?: string
}

export type ImportOwnedLocationWarning = PossibleDuplicatePremisesWarning

export type ImportOwnedLocationsResult = {
  created: Array<{
    rowIndex: number
    locationId: number
    warning?: ImportOwnedLocationWarning | null
  }>
  errors: Array<{
    rowIndex: number
    message: string
    code?: string | null
    cap?: number | null
    current?: number | null
  }>
}

export async function importOwnedLocations(
  rows: ImportOwnedLocationRowInput[]
): Promise<ImportOwnedLocationsResult> {
  try {
    const response = await axiosInstance.post<{
      success?: boolean
      created?: ImportOwnedLocationsResult["created"]
      errors?: ImportOwnedLocationsResult["errors"]
      message?: string
    }>("/locations/import", { rows })
    return {
      created: Array.isArray(response.data.created)
        ? response.data.created
        : [],
      errors: Array.isArray(response.data.errors) ? response.data.errors : [],
    }
  } catch (error) {
    throw new Error(readApiError(error, "Could not import locations."))
  }
}
