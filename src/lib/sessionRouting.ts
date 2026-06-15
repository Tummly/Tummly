import { AUTH_API_BASE_URL } from "@/config/api"
import {
  getMultiDashboardPath,
  getPostLoginDestination,
  getSelectedLocationId,
  WORKSPACE_SETUP_PATH,
} from "@/pages/utils/authHelpers"
import { getAuthRole, getAuthToken } from "@/stores/authStore"
import type { AuthSessionRole } from "@/types/auth"

export interface CurrentUserRouting {
  role: AuthSessionRole
  accountType: string
  selectedLocationId: number | null
  workspaceSetupRequired: boolean
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

function readBooleanField(
  source: Record<string, unknown>,
  keys: string[]
): boolean {
  for (const key of keys) {
    const value = source[key]
    if (typeof value === "boolean") {
      return value
    }
  }

  return false
}

export function parseCurrentUserRouting(
  result: unknown,
  sessionRole: AuthSessionRole | null = getAuthRole()
): CurrentUserRouting | null {
  if (!result || typeof result !== "object") {
    return null
  }

  const envelope = result as Record<string, unknown>
  const data =
    envelope.data && typeof envelope.data === "object"
      ? (envelope.data as Record<string, unknown>)
      : envelope

  const accountType = readStringField(data, ["accountType", "AccountType"])

  if (!accountType) {
    return null
  }

  // `/auth/me` returns operator role (e.g. Owner), not auth session role.
  const role: AuthSessionRole = sessionRole === "ADMIN" ? "ADMIN" : "USER"

  return {
    role,
    accountType,
    selectedLocationId: readNumberField(data, [
      "selectedLocationId",
      "SelectedLocationId",
    ]),
    workspaceSetupRequired: readBooleanField(data, [
      "workspaceSetupRequired",
      "WorkspaceSetupRequired",
    ]),
  }
}

export async function fetchCurrentUserRouting(): Promise<CurrentUserRouting | null> {
  const token = getAuthToken()

  if (!token) {
    return null
  }

  const response = await fetch(`${AUTH_API_BASE_URL}/me`, {
    method: "GET",
    headers: {
      Authorization: `Bearer ${token}`,
    },
  })

  if (!response.ok) {
    return null
  }

  const result = await response.json()
  return parseCurrentUserRouting(result)
}

/** Where an already-authenticated operator should land when opening `/login`. */
export function getAuthenticatedLoginDestination(
  routing: CurrentUserRouting
): string {
  if (routing.role === "ADMIN") {
    return "/admin-dashboard"
  }

  if (routing.selectedLocationId != null) {
    persistSelectedLocation(routing.selectedLocationId)
  }

  return getPostLoginDestination(
    routing.accountType,
    routing.workspaceSetupRequired,
    routing.selectedLocationId ?? getSelectedLocationId()
  )
}

export function isAuthenticatedWorkspaceSetupDestination(path: string) {
  return path === WORKSPACE_SETUP_PATH
}

export function getResolvedMultiDashboardPath(
  routing: CurrentUserRouting
): string {
  return getMultiDashboardPath(routing.selectedLocationId)
}
