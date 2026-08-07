/**
 * Campaign wizard Schedule step — Figma 4751:67079 / ticket 27.
 * Send now vs Schedule for later chrome only. No schedule reservation API.
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
