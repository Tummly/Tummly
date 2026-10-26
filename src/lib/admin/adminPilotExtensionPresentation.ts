import { format } from "date-fns"

import type { AdminPilotAccount } from "@/types/admin"

export const PILOT_EXTENSION_DAY_OPTIONS = [14, 30] as const
export const PILOT_EXTENSION_MAX_COUNT = 2
export const PILOT_EXTENSION_MAX_DAYS_TOTAL = 90

const FALLBACK_DISABLED_REASON = "This Pilot cannot be extended."

export type PilotRowPresentation = {
  startLabel: string
  endLabel: string
  extensionsLabel: string
  canExtend: boolean
  disabledReason: string | null
}

function formatPilotDate(value: string | null): string {
  if (!value) return "—"
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return "—"
  return format(date, "d MMM yyyy")
}

export function buildPilotRowPresentation(
  account: AdminPilotAccount,
  // The server decides canExtend. The clock is kept for the row signature.
  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  _now: Date
): PilotRowPresentation {
  return {
    startLabel: formatPilotDate(account.pilotStartedAtUtc),
    endLabel: formatPilotDate(account.pilotPeriodEnd),
    extensionsLabel: `${account.extensionCount} of ${PILOT_EXTENSION_MAX_COUNT} · ${account.extensionDaysTotal} of ${PILOT_EXTENSION_MAX_DAYS_TOTAL} days`,
    canExtend: account.canExtend,
    disabledReason: account.canExtend
      ? null
      : (account.cannotExtendReason ?? FALLBACK_DISABLED_REASON),
  }
}
