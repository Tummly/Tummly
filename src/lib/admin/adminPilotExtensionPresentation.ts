import { format } from "date-fns"

import type {
  AdminPilotAccount,
  AdminPilotAllowance,
  AdminPilotExtensionHistoryItem,
} from "@/types/admin"

export const PILOT_EXTENSION_DAY_OPTIONS = [14, 30] as const
export const PILOT_EXTENSION_MAX_COUNT = 2
export const PILOT_EXTENSION_MAX_DAYS_TOTAL = 90
export const PILOT_EXTENSION_REASON_MIN_LENGTH = 5
export const PILOT_EXTENSION_REASON_MAX_LENGTH = 500

const FALLBACK_DISABLED_REASON = "This Pilot cannot be extended."
const DAY_MS = 24 * 60 * 60 * 1000

const ALLOWANCE_CHANNEL_ORDER = ["ai", "email", "sms"] as const
const ALLOWANCE_CHANNEL_LABELS: Record<string, string> = {
  ai: "AI",
  email: "Email",
  sms: "SMS",
}

export type PilotRowPresentation = {
  startLabel: string
  originalEndLabel: string
  endLabel: string
  extensionsLabel: string
  canExtend: boolean
  disabledReason: string | null
}

export type PilotExtensionPreview = {
  /** `current-end` when the Pilot is still running, otherwise `now`. */
  countsFrom: "current-end" | "now"
  newEnd: Date
  newEndLabel: string
}

export type PilotAllowanceRow = {
  channel: string
  label: string
  granted: number
  used: number
  remaining: number
}

export type PilotExtensionHistoryRow = {
  dateLabel: string
  days: number
  actor: string
  reason: string
}

export function formatPilotDate(value: string | null): string {
  if (!value) return "—"
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return "—"
  return format(date, "d MMM yyyy")
}

/** Matches by restaurant, so a second Pilot restaurant on one owner is not touched. */
export function replacePilotAccountRow(
  rows: AdminPilotAccount[],
  updated: AdminPilotAccount
): AdminPilotAccount[] {
  return rows.map((row) =>
    row.restaurantId === updated.restaurantId ? updated : row
  )
}

/** Uses the server's new end date. The selected length is not repeated. */
export function pilotExtendedToastMessage(account: AdminPilotAccount): string {
  const endLabel = formatPilotDate(account.pilotPeriodEnd)
  return endLabel === "—"
    ? "Pilot extended."
    : `Pilot extended. New end: ${endLabel}.`
}

/** Empty string when the reason is valid. The bounds match the server. */
export function pilotExtensionReasonError(reason: string): string {
  const length = reason.trim().length
  if (length < PILOT_EXTENSION_REASON_MIN_LENGTH) {
    return `Enter at least ${PILOT_EXTENSION_REASON_MIN_LENGTH} characters.`
  }
  if (length > PILOT_EXTENSION_REASON_MAX_LENGTH) {
    return `Use ${PILOT_EXTENSION_REASON_MAX_LENGTH} characters or fewer.`
  }
  return ""
}

/**
 * Same rule as the server: count from the current end while it is in the
 * future, otherwise from now. The server result is still the authority.
 */
export function previewPilotExtension(
  account: AdminPilotAccount,
  days: number,
  now: Date
): PilotExtensionPreview {
  const currentEnd = account.pilotPeriodEnd
    ? new Date(account.pilotPeriodEnd)
    : null
  const useCurrentEnd =
    currentEnd !== null
    && !Number.isNaN(currentEnd.getTime())
    && currentEnd.getTime() > now.getTime()
  const countsFromMs =
    useCurrentEnd && currentEnd ? currentEnd.getTime() : now.getTime()
  const newEnd = new Date(countsFromMs + days * DAY_MS)

  return {
    countsFrom: useCurrentEnd ? "current-end" : "now",
    newEnd,
    newEndLabel: formatPilotDate(newEnd.toISOString()),
  }
}

export function pilotAllowanceRows(
  account: AdminPilotAccount
): PilotAllowanceRow[] | null {
  if (account.allowances === null) return null

  return ALLOWANCE_CHANNEL_ORDER.map((channel) => {
    const balance: AdminPilotAllowance | undefined = account.allowances?.find(
      (row) => row.channel === channel
    )
    return {
      channel,
      label: ALLOWANCE_CHANNEL_LABELS[channel],
      granted: balance?.granted ?? 0,
      used: balance?.used ?? 0,
      remaining: balance?.remaining ?? 0,
    }
  })
}

export function pilotExtensionHistoryRows(
  account: AdminPilotAccount
): PilotExtensionHistoryRow[] {
  return account.extensionHistory.map((item: AdminPilotExtensionHistoryItem) => ({
    dateLabel: formatPilotDate(item.occurredAtUtc),
    days: item.days,
    actor: item.actorIdentity,
    reason: item.reason ?? "No reason recorded",
  }))
}

export function buildPilotRowPresentation(
  account: AdminPilotAccount,
  // The server decides canExtend. The clock is kept for the row signature.
  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  _now: Date
): PilotRowPresentation {
  return {
    startLabel: formatPilotDate(account.pilotStartedAtUtc),
    originalEndLabel: formatPilotDate(account.pilotOriginalEndUtc),
    endLabel: formatPilotDate(account.pilotPeriodEnd),
    extensionsLabel: `${account.extensionCount} of ${PILOT_EXTENSION_MAX_COUNT} · ${account.extensionDaysTotal} of ${PILOT_EXTENSION_MAX_DAYS_TOTAL} days`,
    canExtend: account.canExtend,
    disabledReason: account.canExtend
      ? null
      : (account.cannotExtendReason ?? FALLBACK_DISABLED_REASON),
  }
}
