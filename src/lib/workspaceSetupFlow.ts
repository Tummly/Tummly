import { AUTH_API_BASE_URL } from "@/config/api"
import { getAuthToken } from "@/stores/authStore"
export interface WorkspaceLocation {
  locationId: number
  locationName: string
  restaurantName: string
  address: string
}

function getAuthHeaders() {
  const token = getAuthToken()

  if (!token?.trim()) {
    throw new Error("You must be signed in to continue.")
  }

  return {
    Authorization: `Bearer ${token}`,
    "Content-Type": "application/json",
  }
}

function getFetchErrorMessage(
  result: { message?: string },
  fallback: string
) {
  return result.message?.trim() || fallback
}

function readStringField(
  source: Record<string, unknown>,
  keys: string[]
): string | null {
  for (const key of keys) {
    const value = source[key]
    if (typeof value === "string" && value.trim()) {
      return value
    }
  }

  return null
}

function readNumberField(
  source: Record<string, unknown>,
  keys: string[]
): number | null {
  for (const key of keys) {
    const value = source[key]
    if (typeof value === "number" && Number.isFinite(value)) {
      return value
    }
  }

  return null
}

export function parseWorkspaceLocation(
  source: unknown
): WorkspaceLocation | null {
  if (!source || typeof source !== "object") {
    return null
  }

  const data = source as Record<string, unknown>
  const locationId = readNumberField(data, ["locationId", "LocationId"])

  if (locationId == null) {
    return null
  }

  const locationName =
    readStringField(data, ["locationName", "LocationName"]) ?? ""

  const restaurantName =
    readStringField(data, ["restaurantName", "RestaurantName"]) ?? ""

  const address = readStringField(data, ["address", "Address"]) ?? ""

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
  if (!result || typeof result !== "object") {
    return []
  }

  const envelope = result as Record<string, unknown>
  const data = Array.isArray(envelope.data) ? envelope.data : []

  return data
    .map(parseWorkspaceLocation)
    .filter((item): item is WorkspaceLocation => item !== null)
}

export async function fetchWorkspaceLocations() {
  const response = await fetch(`${AUTH_API_BASE_URL}/workspaces`, {
    method: "GET",
    headers: getAuthHeaders(),
  })

  const result = await response.json()

  if (!response.ok) {
    throw new Error(
      getFetchErrorMessage(result, "Unable to load workspaces.")
    )
  }

  return parseWorkspaceLocationsResponse(result)
}

export async function submitWorkspaceSelection(locationId: number) {
  const response = await fetch(`${AUTH_API_BASE_URL}/select-workspace`, {
    method: "POST",
    headers: getAuthHeaders(),
    body: JSON.stringify({ locationId }),
  })

  const result = await response.json()

  if (!response.ok) {
    throw new Error(
      getFetchErrorMessage(result, "Unable to save workspace selection.")
    )
  }

  const savedLocationId =
    readNumberField(result as Record<string, unknown>, [
      "locationId",
      "LocationId",
    ]) ?? locationId

  return savedLocationId
}
