import { API_BASE_URL } from "@/config/api"
import { readString, unwrapDataObject } from "@/lib/apiEnvelope"
import {
  getAuthToken,
  getRefreshToken,
  useAuthStore,
} from "@/stores/authStore"

const EXPIRY_SKEW_MS = 30_000

let inFlightRefresh: Promise<string | null> | null = null

type JwtExpiryPayload = {
  exp?: number
}

function decodeJwtPayload(token: string): JwtExpiryPayload | null {
  const parts = token.split(".")
  if (parts.length < 2) {
    return null
  }

  try {
    const normalized = parts[1].replace(/-/g, "+").replace(/_/g, "/")
    const padded = normalized.padEnd(
      normalized.length + ((4 - (normalized.length % 4)) % 4),
      "="
    )
    return JSON.parse(atob(padded)) as JwtExpiryPayload
  } catch {
    return null
  }
}

function accessTokenNeedsRefresh(token: string): boolean {
  const payload = decodeJwtPayload(token)
  if (payload?.exp == null) {
    return false
  }

  return Date.now() + EXPIRY_SKEW_MS >= payload.exp * 1000
}

async function postAuthJson(path: string, body: unknown): Promise<unknown> {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(body),
  })

  if (!response.ok) {
    throw new Error(`Request to ${path} failed.`)
  }

  return response.json()
}

async function rotateRefreshToken(refreshToken: string): Promise<string | null> {
  try {
    const payload = await postAuthJson("/auth/refresh", { refreshToken })
    const data = unwrapDataObject(payload) ?? payload
    const token = readString(data, "token")
    const nextRefreshToken = readString(data, "refreshToken")
    const role = useAuthStore.getState().role

    if (!token || !nextRefreshToken || !role) {
      return null
    }

    useAuthStore.getState().setSession(
      token,
      role,
      useAuthStore.getState().accountType ?? undefined,
      nextRefreshToken
    )

    return token
  } catch {
    return null
  }
}

export async function ensureFreshAccessToken(
  options?: { force?: boolean }
): Promise<string | null> {
  const refreshToken = getRefreshToken()
  const token = getAuthToken()

  if (!refreshToken) {
    return token
  }

  if (!options?.force && token && !accessTokenNeedsRefresh(token)) {
    return token
  }

  if (!inFlightRefresh) {
    inFlightRefresh = rotateRefreshToken(refreshToken).finally(() => {
      inFlightRefresh = null
    })
  }

  return inFlightRefresh
}

export function revokeRefreshToken(refreshToken: string): Promise<void> {
  return postAuthJson("/auth/logout", { refreshToken })
    .then(() => undefined)
    .catch(() => undefined)
}
