/**
 * Campaign wizard Schedule step — Figma 4751:67079 / ticket 26.
 * Send now vs Schedule for later + datetime when later.
 */

export type CampaignScheduleModeId = "send-now" | "schedule-later"

export type CampaignScheduleOptionDef = {
  id: CampaignScheduleModeId
  title: string
  description: string
}

export const CAMPAIGN_SCHEDULE_COPY = {
  stepHeading: "When should this campaign send?",
  stepDescription:
    "Choose whether to start processing after final review, or pick a later time.",
  usageTitle: "Estimated message usage",
  sendDateLabel: "Send date",
  sendTimeLabel: "Send time",
  sendDatePlaceholder: "Select date",
  sendTimePlaceholder: "Select time",
  datetimeRequired:
    "Choose a send date and time that is after now.",
} as const

export const CAMPAIGN_SCHEDULE_OPTIONS: readonly CampaignScheduleOptionDef[] = [
  {
    id: "send-now",
    title: "Send now",
    description:
      "Start processing the campaign after final review and confirmation.",
  },
  {
    id: "schedule-later",
    title: "Schedule for later",
    description:
      "Choose when Tummly should begin processing the campaign.",
  },
] as const

/** Half-hour slots for Schedule for later (local wall clock). */
export const CAMPAIGN_SCHEDULE_TIME_OPTIONS: readonly string[] = [
  "00:00",
  "00:30",
  "01:00",
  "01:30",
  "02:00",
  "02:30",
  "03:00",
  "03:30",
  "04:00",
  "04:30",
  "05:00",
  "05:30",
  "06:00",
  "06:30",
  "07:00",
  "07:30",
  "08:00",
  "08:30",
  "09:00",
  "09:30",
  "10:00",
  "10:30",
  "11:00",
  "11:30",
  "12:00",
  "12:30",
  "13:00",
  "13:30",
  "14:00",
  "14:30",
  "15:00",
  "15:30",
  "16:00",
  "16:30",
  "17:00",
  "17:30",
  "18:00",
  "18:30",
  "19:00",
  "19:30",
  "20:00",
  "20:30",
  "21:00",
  "21:30",
  "22:00",
  "22:30",
  "23:00",
  "23:30",
]

const DEFAULT_SCHEDULE_MODE_ID: CampaignScheduleModeId = "send-now"

export function defaultCampaignScheduleModeId(): CampaignScheduleModeId {
  return DEFAULT_SCHEDULE_MODE_ID
}

export function labelForCampaignScheduleModeId(
  modeId: CampaignScheduleModeId
): string {
  return (
    CAMPAIGN_SCHEDULE_OPTIONS.find((option) => option.id === modeId)?.title
    ?? modeId
  )
}

export function defaultCampaignScheduleTimeZone(): string {
  try {
    const zone = Intl.DateTimeFormat().resolvedOptions().timeZone
    if (typeof zone === "string" && zone.trim().length > 0) {
      return zone.trim()
    }
  } catch {
    // Fall through.
  }
  return "Europe/London"
}

/**
 * Combine local YYYY-MM-DD + HH:mm into a UTC ISO instant.
 * Returns null when the parts are incomplete or invalid.
 */
export function campaignScheduledAtUtcIso(input: {
  dateLocal: string
  timeLocal: string
}): string | null {
  const date = input.dateLocal.trim()
  const time = input.timeLocal.trim()
  if (!/^\d{4}-\d{2}-\d{2}$/.test(date) || !/^\d{2}:\d{2}$/.test(time)) {
    return null
  }
  const at = new Date(`${date}T${time}:00`)
  if (Number.isNaN(at.getTime())) {
    return null
  }
  return at.toISOString()
}

export function canContinueCampaignSchedule(input: {
  modeId: CampaignScheduleModeId
  dateLocal: string
  timeLocal: string
  now: Date
}): boolean {
  if (input.modeId === "send-now") {
    return true
  }
  const iso = campaignScheduledAtUtcIso({
    dateLocal: input.dateLocal,
    timeLocal: input.timeLocal,
  })
  if (iso == null) {
    return false
  }
  return new Date(iso).getTime() > input.now.getTime()
}
