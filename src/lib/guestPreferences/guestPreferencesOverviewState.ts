import type { GuestPreferencesOverview } from "@/api/publicGuestPreferencesApi"
import {
  diffChannelChanges,
  type GuestPreferenceChannelChanges,
  type GuestPreferenceChannels,
} from "@/lib/guestPreferences/guestPreferencesChannels"

export type GuestPreferencesOverviewState = {
  /** First name as saved on the server. */
  firstName: string
  /** Non-null while the first-name editor is open. */
  nameDraft: string | null
  /** Channel values as last saved (or as loaded). */
  savedChannels: GuestPreferenceChannels
  /** Channel values as currently ticked. */
  channels: GuestPreferenceChannels
  stopAllOpen: boolean
}

export type GuestPreferencesOverviewAction =
  | { type: "name-edit-started" }
  | { type: "name-draft-changed"; value: string }
  | { type: "name-saved"; firstName: string }
  | {
      type: "channel-toggled"
      channel: keyof GuestPreferenceChannels
      checked: boolean
    }
  | { type: "channels-saved"; channels: GuestPreferenceChannels }
  | { type: "stop-all-opened" }
  | { type: "stop-all-closed" }
  | { type: "stop-all-confirmed" }

/** The one request that may be in flight on the overview at a time. */
export type GuestPreferencesPendingAction =
  | "name"
  | "channels"
  | "stop-all"
  | null

export type UpdateFirstName = (firstName: string) => Promise<void>
export type UpdateChannels = (changes: GuestPreferenceChannelChanges) => Promise<void>

/**
 * Remount key for the overview form. The reducer keeps its state across renders, so a
 * refreshed overview (after an add-contact success, say) would show the old checkboxes.
 * A new key remounts the form and seeds it from the refreshed overview.
 */
export function guestPreferencesOverviewSeedKey(
  overview: Pick<GuestPreferencesOverview, "contacts" | "emailOn" | "smsOn">
): string {
  const contactIds = overview.contacts.map((contact) => contact.id).join(",")
  return `contacts:${contactIds}|email:${overview.emailOn}|sms:${overview.smsOn}`
}

export function createGuestPreferencesOverviewState(input: {
  firstName: string | null
  emailOn: boolean
  smsOn: boolean
}): GuestPreferencesOverviewState {
  const saved = { email: input.emailOn, sms: input.smsOn }
  return {
    firstName: input.firstName ?? "",
    nameDraft: null,
    savedChannels: saved,
    channels: { ...saved },
    stopAllOpen: false,
  }
}

export function guestPreferencesOverviewReducer(
  state: GuestPreferencesOverviewState,
  action: GuestPreferencesOverviewAction
): GuestPreferencesOverviewState {
  switch (action.type) {
    case "name-edit-started":
      return { ...state, nameDraft: state.firstName }
    case "name-draft-changed":
      return state.nameDraft === null
        ? state
        : { ...state, nameDraft: action.value }
    case "name-saved":
      return { ...state, firstName: action.firstName, nameDraft: null }
    case "channel-toggled":
      return {
        ...state,
        channels: { ...state.channels, [action.channel]: action.checked },
      }
    case "channels-saved":
      return { ...state, savedChannels: { ...action.channels } }
    case "stop-all-opened":
      return { ...state, stopAllOpen: true }
    case "stop-all-closed":
      return { ...state, stopAllOpen: false }
    case "stop-all-confirmed": {
      const off = { email: false, sms: false }
      return {
        ...state,
        stopAllOpen: false,
        savedChannels: off,
        channels: { ...off },
      }
    }
  }
}

/** True while any overview request is in flight. Drives every request control. */
export function isGuestPreferencesBusy(
  pending: GuestPreferencesPendingAction
): boolean {
  return pending !== null
}

/**
 * Saves the first name only when the trimmed draft is non-empty and differs
 * from the saved name. Resolves to the name now in effect.
 */
export async function commitFirstName(
  update: UpdateFirstName,
  savedName: string,
  draft: string
): Promise<string> {
  const next = draft.trim()
  if (next === "" || next === savedName) {
    return savedName
  }
  await update(next)
  return next
}

/**
 * Sends only the channels that differ from the saved state. Resolves to
 * whether a request was made.
 */
export async function commitChannelChanges(
  update: UpdateChannels,
  saved: GuestPreferenceChannels,
  current: GuestPreferenceChannels
): Promise<boolean> {
  const changes = diffChannelChanges(saved, current)
  if (Object.keys(changes).length === 0) {
    return false
  }
  await update(changes)
  return true
}

/**
 * Calls the stop-all endpoint, then applies the confirmed state. A rejected
 * request dispatches nothing and rethrows.
 */
export async function commitStopAll(
  stopAll: () => Promise<void>,
  dispatch: (action: GuestPreferencesOverviewAction) => void
): Promise<void> {
  await stopAll()
  dispatch({ type: "stop-all-confirmed" })
}
