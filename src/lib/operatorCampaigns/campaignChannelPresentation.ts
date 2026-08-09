/**
 * Campaign wizard Channel step — Figma 4707:52097 / ticket 24.
 * Email vs SMS only (slice 1). Usage rows use live eligibility + messaging
 * balances (fixtures until Billing cutover).
 */

import type {
  CampaignAudienceEligibilityBreakdown,
} from "@/lib/operatorCampaigns/campaignAudiencePresentation"
import {
  MESSAGING_USAGE_COPY,
  MESSAGING_USAGE_FIXTURE,
  type MessagingUsageFixture,
} from "@/lib/operatorCampaigns/messagingUsageFixtures"

export type CampaignChannelId = "email" | "sms"

export type CampaignChannelOptionDef = {
  id: CampaignChannelId
  title: string
  description: string
}

export type CampaignChannelUsageRow = {
  label: string
  value: string
}

export type CampaignChannelSmsShortfall = {
  title: string
  body: string
  buyCreditsLabel: string
}

export const CAMPAIGN_CHANNEL_COPY = {
  stepHeading: "How should guests receive this campaign?",
  stepDescription:
    "Choose one channel. Recipient eligibility and estimated usage will update for that channel.",
  usageTitle: "Estimated message usage",
  estimatedRecipientsSuffix: "estimated recipients",
  unavailableCount: "—",
  eligibleRecipientsLabel: "Eligible recipients",
  estimatedEmailMessagesLabel: "Estimated email messages",
  allowanceRemainingLabel: "Allowance remaining",
  estimatedRemainingAfterSendLabel: "Estimated remaining after send",
  estimatedSmsPartsLabel: "Estimated SMS parts",
  estimatedSmsPartsValue: "At least 1 per recipient",
  estimatedCreditsLabel: "Estimated credits",
  availableCreditsLabel: "Available credits",
  reservedCreditsLabel: "Reserved credits",
  estimatedBalanceAfterSendLabel: "Estimated balance after send",
  smsShortfallTitle: "More SMS credits are required",
  buySmsCredits: MESSAGING_USAGE_COPY.buySmsCredits,
} as const

export const CAMPAIGN_CHANNEL_OPTIONS: readonly CampaignChannelOptionDef[] = [
  {
    id: "email",
    title: "Email",
    description:
      "Best for longer messages, menu updates and campaigns with more detail.",
  },
  {
    id: "sms",
    title: "SMS",
    description:
      "Best for short, time-sensitive messages and simple offer reminders.",
  },
] as const

const DEFAULT_CHANNEL_ID: CampaignChannelId = "email"

function formatCount(value: number): string {
  return value.toLocaleString("en-GB")
}

function formatOptionalCount(value: number | null): string {
  if (value == null) {
    return CAMPAIGN_CHANNEL_COPY.unavailableCount
  }
  return formatCount(value)
}

function channelTitleForId(channelId: CampaignChannelId): string {
  return (
    CAMPAIGN_CHANNEL_OPTIONS.find((option) => option.id === channelId)?.title
    ?? "Channel"
  )
}

function formatEstimatedCreditsLabel(credits: number | null): string {
  if (credits == null) {
    return CAMPAIGN_CHANNEL_COPY.unavailableCount
  }
  return `At least ${formatCount(credits)}`
}

function formatRemainingAfterSend(
  remaining: number,
  eligible: number | null
): string {
  if (eligible == null) {
    return CAMPAIGN_CHANNEL_COPY.unavailableCount
  }
  return formatCount(remaining - eligible)
}

export function defaultCampaignChannelId(): CampaignChannelId {
  return DEFAULT_CHANNEL_ID
}

/** Figma 4752:67492 — location · channel · N estimated recipients. */
export function formatCampaignChannelUsageAudienceLine(input: {
  locationName: string
  channelId: CampaignChannelId
  estimatedRecipients: number | null
}): string {
  const location =
    input.locationName.trim().length > 0 ? input.locationName.trim() : "—"
  const recipients = formatOptionalCount(input.estimatedRecipients)
  return `${location} · ${channelTitleForId(input.channelId)} · ${recipients} ${CAMPAIGN_CHANNEL_COPY.estimatedRecipientsSuffix}`
}

export function buildCampaignChannelUsageSummary(input: {
  channelId: CampaignChannelId
  locationName: string
  eligibility: CampaignAudienceEligibilityBreakdown
  fixture?: MessagingUsageFixture
}): {
  audienceLine: string
  rows: CampaignChannelUsageRow[]
} {
  const fixture = input.fixture ?? MESSAGING_USAGE_FIXTURE
  const estimatedRecipients =
    input.channelId === "email"
      ? input.eligibility.emailEligible
      : input.eligibility.smsEligible
  const audienceLine = formatCampaignChannelUsageAudienceLine({
    locationName: input.locationName,
    channelId: input.channelId,
    estimatedRecipients,
  })

  if (input.channelId === "email") {
    const eligible = input.eligibility.emailEligible
    return {
      audienceLine,
      rows: [
        {
          label: CAMPAIGN_CHANNEL_COPY.eligibleRecipientsLabel,
          value: formatOptionalCount(eligible),
        },
        {
          label: CAMPAIGN_CHANNEL_COPY.estimatedEmailMessagesLabel,
          value: formatOptionalCount(eligible),
        },
        {
          label: CAMPAIGN_CHANNEL_COPY.allowanceRemainingLabel,
          value: formatCount(fixture.email.remaining),
        },
        {
          label: CAMPAIGN_CHANNEL_COPY.estimatedRemainingAfterSendLabel,
          value: formatRemainingAfterSend(fixture.email.remaining, eligible),
        },
      ],
    }
  }

  const eligible = input.eligibility.smsEligible
  return {
    audienceLine,
    rows: [
      {
        label: CAMPAIGN_CHANNEL_COPY.eligibleRecipientsLabel,
        value: formatOptionalCount(eligible),
      },
      {
        label: CAMPAIGN_CHANNEL_COPY.estimatedSmsPartsLabel,
        value: CAMPAIGN_CHANNEL_COPY.estimatedSmsPartsValue,
      },
      {
        label: CAMPAIGN_CHANNEL_COPY.estimatedCreditsLabel,
        value: formatEstimatedCreditsLabel(eligible),
      },
      {
        label: CAMPAIGN_CHANNEL_COPY.availableCreditsLabel,
        value: formatCount(fixture.sms.available),
      },
      {
        label: CAMPAIGN_CHANNEL_COPY.reservedCreditsLabel,
        value: formatCount(fixture.sms.reserved),
      },
      {
        label: CAMPAIGN_CHANNEL_COPY.estimatedBalanceAfterSendLabel,
        value: formatRemainingAfterSend(fixture.sms.available, eligible),
      },
    ],
  }
}

export function resolveCampaignChannelSmsShortfall(input: {
  channelId: CampaignChannelId
  smsEligible: number | null
  fixture?: MessagingUsageFixture
}): CampaignChannelSmsShortfall | null {
  if (input.channelId !== "sms") {
    return null
  }
  const required = input.smsEligible
  if (required == null) {
    return null
  }
  const fixture = input.fixture ?? MESSAGING_USAGE_FIXTURE
  const available = fixture.sms.available
  if (available >= required) {
    return null
  }
  return {
    title: CAMPAIGN_CHANNEL_COPY.smsShortfallTitle,
    body: `This campaign requires at least ${formatCount(required)} SMS credits. Your account currently has ${formatCount(available)} available.`,
    buyCreditsLabel: CAMPAIGN_CHANNEL_COPY.buySmsCredits,
  }
}
