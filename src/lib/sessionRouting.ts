import { isAxiosError } from "axios"

import {
  readBoolean,
  readNumber,
  readString,
  unwrapDataObject,
} from "@/lib/apiEnvelope"
import {
  getMultiDashboardPath,
  getPostLoginDestination,
  getSelectedLocationId,
  persistAuthSession,
  persistSelectedLocation,
  WORKSPACE_SETUP_PATH,
  clearAuthSession,
} from "@/pages/utils/authHelpers"
import { getAuthAccountType, getAuthRole, getAuthToken } from "@/stores/authStore"
import type { AuthSessionRole } from "@/types/auth"
import { fetchCurrentUser } from "@/api/loginContextClient"

export interface CurrentUserRouting {
  role: AuthSessionRole
  accountType: string
  selectedLocationId: number | null
  workspaceSetupRequired: boolean
}

export function parseCurrentUserRouting(
  result: unknown,
  sessionRole: AuthSessionRole | null = getAuthRole()
): CurrentUserRouting | null {
  const data = unwrapDataObject(result)

  if (!data) {
    return null
  }

  const accountType = readString(data, "accountType")

  if (!accountType) {
    return null
  }

  // `/auth/me` returns operator role (e.g. Owner), not auth session role.
  const role: AuthSessionRole = sessionRole === "ADMIN" ? "ADMIN" : "USER"

  return {
    role,
    accountType,
    selectedLocationId: readNumber(data, "selectedLocationId"),
    workspaceSetupRequired:
      readBoolean(data, "workspaceSetupRequired") ?? false,
  }
}

export async function fetchCurrentUserRouting(): Promise<CurrentUserRouting | null> {
  if (!getAuthToken()) {
    return null
  }

  try {
    const result = await fetchCurrentUser()
    return parseCurrentUserRouting(result)
  } catch (error) {
    if (isAxiosError(error) && error.response?.status === 401) {
      clearAuthSession()
    }

    return null
  }
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

  const currentToken = getAuthToken()
  if (currentToken) {
    persistAuthSession(currentToken, "USER", routing.accountType)
  }

  return getPostLoginDestination(
    routing.accountType,
    routing.workspaceSetupRequired,
    routing.selectedLocationId ?? getSelectedLocationId()
  )
}

/**
 * Fallback destination when `/me` fails but the user has a valid token
 * and a previously stored accountType. Returns null if no fallback is possible.
 */
export function getFallbackLoginDestination(): string | null {
  const accountType = getAuthAccountType()
  const token = getAuthToken()

  if (!token || !accountType) {
    return null
  }

  return getPostLoginDestination(accountType, false, getSelectedLocationId())
}

export function isAuthenticatedWorkspaceSetupDestination(path: string) {
  return path === WORKSPACE_SETUP_PATH
}

export function getResolvedMultiDashboardPath(
  routing: CurrentUserRouting
): string {
  return getMultiDashboardPath(routing.selectedLocationId)
}
