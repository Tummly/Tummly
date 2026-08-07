/**
 * Campaign wizard Channel step — Figma 4707:52097 / ticket 24.
 * Email vs SMS only (slice 1). Usage numbers from shared messaging fixtures.
 */

import {
  CAMPAIGN_AUDIENCE_ELIGIBILITY_MOCK,
  CAMPAIGN_AUDIENCE_OPTIONS,
  type CampaignAudienceId,
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
  eligibleThroughLine: "eligible through at least one channel",
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

function audienceTitleForId(audienceId: CampaignAudienceId): string {
  return (
    CAMPAIGN_AUDIENCE_OPTIONS.find((option) => option.id === audienceId)
      ?.title ?? "Selected audience"
  )
}

function formatEstimatedCreditsLabel(credits: number): string {
  return `At least ${formatCount(credits)}`
}

export function defaultCampaignChannelId(): CampaignChannelId {
  return DEFAULT_CHANNEL_ID
}

export function buildCampaignChannelUsageSummary(input: {
  channelId: CampaignChannelId
  audienceId: CampaignAudienceId
  fixture?: MessagingUsageFixture
}): {
  audienceLine: string
  rows: CampaignChannelUsageRow[]
} {
  const fixture = input.fixture ?? MESSAGING_USAGE_FIXTURE
  const eligibility = CAMPAIGN_AUDIENCE_ELIGIBILITY_MOCK
  const audienceLine = `${audienceTitleForId(input.audienceId)} · ${formatCount(eligibility.currentlyEligible)} ${CAMPAIGN_CHANNEL_COPY.eligibleThroughLine}`

  if (input.channelId === "email") {
    const eligible = eligibility.emailEligible
    const remainingAfterSend = fixture.email.remaining - eligible
    return {
      audienceLine,
      rows: [
        {
          label: CAMPAIGN_CHANNEL_COPY.eligibleRecipientsLabel,
          value: formatCount(eligible),
        },
        {
          label: CAMPAIGN_CHANNEL_COPY.estimatedEmailMessagesLabel,
          value: formatCount(eligible),
        },
        {
          label: CAMPAIGN_CHANNEL_COPY.allowanceRemainingLabel,
          value: formatCount(fixture.email.remaining),
        },
        {
          label: CAMPAIGN_CHANNEL_COPY.estimatedRemainingAfterSendLabel,
          value: formatCount(remainingAfterSend),
        },
      ],
    }
  }

  const eligible = eligibility.smsEligible
  const balanceAfterSend = fixture.sms.available - eligible
  return {
    audienceLine,
    rows: [
      {
        label: CAMPAIGN_CHANNEL_COPY.eligibleRecipientsLabel,
        value: formatCount(eligible),
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
        value: formatCount(balanceAfterSend),
      },
    ],
  }
}

export function resolveCampaignChannelSmsShortfall(input: {
  channelId: CampaignChannelId
  fixture?: MessagingUsageFixture
}): CampaignChannelSmsShortfall | null {
  if (input.channelId !== "sms") {
    return null
  }
  const fixture = input.fixture ?? MESSAGING_USAGE_FIXTURE
  const required = CAMPAIGN_AUDIENCE_ELIGIBILITY_MOCK.smsEligible
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
