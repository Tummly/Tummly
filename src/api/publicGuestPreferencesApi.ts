import axios, {
  AxiosHeaders,
  isAxiosError,
  type AxiosResponse,
  type RawAxiosHeaders,
} from "axios"

import { API_BASE_URL } from "@/config/api"
import {
  clearGuestPreferencesSession,
  readGuestPreferencesSession,
  writeGuestPreferencesSession,
} from "@/lib/guestPreferences/guestPreferencesSession"

const PREFERENCES_URL = `${API_BASE_URL}/public/preferences`
const SESSION_HEADER = "X-Guest-Preferences-Session"

export class GuestPreferencesSessionExpiredError extends Error {
  constructor() {
    super("Guest preferences session has expired")
    this.name = "GuestPreferencesSessionExpiredError"
  }
}

export type GuestPreferenceContact = {
  id: number
  type: string
  masked: string
}

export type GuestPreferencesOverview = {
  firstName: string | null
  contacts: GuestPreferenceContact[]
  emailOn: boolean
  smsOn: boolean
  restaurantCount: number
}

export type GuestPreferenceLocation = {
  locationId: number
  name: string
  muted: boolean
}

export type GuestPreferenceRestaurant = {
  restaurantId: number
  name: string
  logoUrl: string | null
  locations: GuestPreferenceLocation[]
}

type SessionBody = { sessionToken?: unknown }

function sessionTokenFrom(body: SessionBody | undefined): string | null {
  const token = body?.sessionToken
  return typeof token === "string" && token.length > 0 ? token : null
}

/** Stores the token from the body, if the endpoint returned one. */
function storeBodySession(body: SessionBody | undefined): void {
  const token = sessionTokenFrom(body)
  if (token !== null) {
    writeGuestPreferencesSession(token)
  }
}

/** Stores the renewed (sliding) session from the response header, if present. */
function storeRenewedSession(response: AxiosResponse): void {
  const value = AxiosHeaders.from(response.headers as RawAxiosHeaders).get(
    SESSION_HEADER
  )
  if (typeof value === "string" && value.length > 0) {
    writeGuestPreferencesSession(value)
  }
}

/**
 * Session-gated call. Sends the stored session, keeps the renewed session, and
 * turns a 401 into a cleared session plus GuestPreferencesSessionExpiredError.
 */
async function sessionRequest<T = unknown>(
  method: "get" | "put" | "post",
  path: string,
  options: { data?: unknown; params?: Record<string, string> } = {}
): Promise<AxiosResponse<T>> {
  const token = readGuestPreferencesSession()
  try {
    const response = await axios.request<T>({
      method,
      url: `${PREFERENCES_URL}${path}`,
      data: options.data,
      params: options.params,
      headers: token !== null ? { [SESSION_HEADER]: token } : undefined,
    })
    storeRenewedSession(response)
    return response
  } catch (error) {
    if (isAxiosError(error) && error.response) {
      if (error.response.status === 401) {
        clearGuestPreferencesSession()
        throw new GuestPreferencesSessionExpiredError()
      }
      storeRenewedSession(error.response)
    }
    throw error
  }
}

/** Open the session from a signed `?t=` link. */
export async function openSessionFromLink(
  token: string
): Promise<{ valid: boolean }> {
  const { data } = await axios.post<SessionBody & { valid?: boolean }>(
    `${PREFERENCES_URL}/session/link`,
    { token }
  )
  const sessionToken = sessionTokenFrom(data)
  if (sessionToken === null) {
    return { valid: false }
  }
  writeGuestPreferencesSession(sessionToken)
  return { valid: true }
}

export async function startSession(contact: string): Promise<void> {
  await axios.post(`${PREFERENCES_URL}/session/start`, { contact })
}

export async function verifySessionCode(
  contact: string,
  code: string
): Promise<void> {
  const { data } = await axios.post<SessionBody>(
    `${PREFERENCES_URL}/session/verify-code`,
    { contact, code }
  )
  storeBodySession(data)
}

export async function verifySessionEmail(v: string): Promise<void> {
  const { data } = await axios.post<SessionBody>(
    `${PREFERENCES_URL}/session/verify-email`,
    { v }
  )
  storeBodySession(data)
}

export async function fetchOverview(): Promise<GuestPreferencesOverview> {
  const { data } = await sessionRequest<GuestPreferencesOverview>("get", "")
  return data
}

export async function updateFirstName(firstName: string): Promise<void> {
  await sessionRequest("put", "/name", { data: { firstName } })
}

export async function updateChannels(changes: {
  email?: boolean
  sms?: boolean
}): Promise<void> {
  await sessionRequest("put", "/channels", { data: changes })
}

export async function stopAllMarketing(): Promise<void> {
  await sessionRequest("post", "/stop-all")
}

export async function fetchRestaurants(
  q?: string
): Promise<GuestPreferenceRestaurant[]> {
  const { data } = await sessionRequest<GuestPreferenceRestaurant[]>(
    "get",
    "/restaurants",
    { params: q === undefined ? undefined : { q } }
  )
  return data
}

export async function setLocationMuted(
  locationId: number,
  muted: boolean
): Promise<void> {
  await sessionRequest("put", `/locations/${locationId}/mute`, {
    data: { muted },
  })
}

export async function setRestaurantMuted(
  restaurantId: number,
  muted: boolean
): Promise<void> {
  await sessionRequest("put", `/restaurants/${restaurantId}/mute`, {
    data: { muted },
  })
}

export async function addContact(contact: string): Promise<void> {
  await sessionRequest("post", "/contacts", { data: { contact } })
}

export async function verifyContactCode(
  contact: string,
  code: string
): Promise<void> {
  await sessionRequest("post", "/contacts/verify-code", {
    data: { contact, code },
  })
}

/**
 * Add-contact email link. Needs no session and must not write one: the link
 * may be opened in another browser or by another person.
 */
export async function verifyContactEmail(v: string): Promise<void> {
  await axios.post(`${PREFERENCES_URL}/contacts/verify-email`, { v })
}
