import { isAxiosError } from "axios"

import {
  fetchWorkspaces as fetchWorkspacesApi,
  selectWorkspace as selectWorkspaceApi,
} from "@/api/loginContextClient"
import {
  getFetchErrorMessage,
  readNumber,
  readString,
  unwrapDataArray,
} from "@/lib/apiEnvelope"
import { clearAuthSession } from "@/pages/utils/authHelpers"
import { getAuthToken } from "@/stores/authStore"

export interface WorkspaceLocation {
  locationId: number
  locationName: string
  restaurantName: string
  address: string
}

function assertSignedIn() {
  if (!getAuthToken()?.trim()) {
    throw new Error("You must be signed in to continue.")
  }
}

function rethrowAuthApiError(
  error: unknown,
  fallback: string
): never {
  if (isAxiosError(error) && error.response?.status === 401) {
    clearAuthSession()
  }

  if (
    isAxiosError(error)
    && error.response?.data
    && typeof error.response.data === "object"
  ) {
    throw new Error(
      getFetchErrorMessage(
        error.response.data as { message?: string },
        fallback
      )
    )
  }

  if (error instanceof Error) {
    throw error
  }

  throw new Error(fallback)
}

export function parseWorkspaceLocation(
  source: unknown
): WorkspaceLocation | null {
  if (!source || typeof source !== "object") {
    return null
  }

  const restaurantId = readNumber(source, "restaurantId")
  const locationId = restaurantId ?? readNumber(source, "locationId")

  if (locationId == null) {
    return null
  }

  const locationName =
    readString(source, "locationName")
    ?? readString(source, "restaurantName")
    ?? ""
  const restaurantName = readString(source, "restaurantName") ?? ""
  const address = readString(source, "address") ?? ""

  return {
    locationId,
    locationName,
    restaurantName,
    address,
  }
}

export function parseWorkspaceLocationsResponse(
  result: unknown
): WorkspaceLocation[] {
  return unwrapDataArray(result)
    .map(parseWorkspaceLocation)
    .filter((item): item is WorkspaceLocation => item !== null)
}

export async function fetchWorkspaceLocations() {
  assertSignedIn()

  try {
    const result = await fetchWorkspacesApi()
    return parseWorkspaceLocationsResponse(result)
  } catch (error) {
    rethrowAuthApiError(error, "Unable to load workspaces.")
  }
}

export async function submitWorkspaceSelection(locationId: number) {
  assertSignedIn()

  try {
    const result = await selectWorkspaceApi(locationId)
    return readNumber(result, "locationId") ?? locationId
  } catch (error) {
    rethrowAuthApiError(error, "Unable to save workspace selection.")
  }
}
