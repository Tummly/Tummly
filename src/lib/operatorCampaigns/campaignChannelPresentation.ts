/**
 * Campaign wizard Channel / estimate panel — Figma 4707:52097 / ticket 23 / lock 09.
 * Email vs SMS. Usage rows use live eligibility + messaging balances.
 */

import type {
  CampaignAudienceEligibilityBreakdown,
} from "@/lib/operatorCampaigns/campaignAudiencePresentation"
import {
  CAMPAIGNS_MESSAGING_BALANCES_FIXTURE,
  CAMPAIGNS_MESSAGING_USAGE_COPY,
  campaignsMessagingSkippedCount,
  formatCampaignsMessagingAfterSend,
  formatCampaignsMessagingSkipped,
  type CampaignsMessagingBalancesFixture,
} from "@/lib/operatorCampaigns/campaignsMessagingCreditChrome"
import { formatCreditCount } from "@/lib/operatorBillingCredits/creditsUsagePresentation"

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

export type CampaignChannelShortfall = {
  title: string
  body: string
  buyCreditsLabel: string
  changePlanLabel: string
  channelId: CampaignChannelId
}

/** @deprecated Prefer CampaignChannelShortfall — kept for existing imports. */
export type CampaignChannelSmsShortfall = CampaignChannelShortfall

export type CampaignChannelEstimateMode = "floor" | "exact"

export const CAMPAIGN_CHANNEL_COPY = {
  stepHeading: "How should guests receive this campaign?",
  stepDescription:
    "Choose one channel. Recipient eligibility and estimated usage will update for that channel.",
  usageTitle: "Estimated message usage",
  estimatedRecipientsSuffix: "estimated recipients",
  unavailableCount: "—",
  eligibleRecipientsLabel: "Eligible recipients",
  skippedLabel: "Skipped",
  estimatedEmailMessagesLabel: "Estimated email messages",
  estimatedSmsPartsLabel: "Estimated SMS parts",
  estimatedSmsPartsFloorValue: "At least 1 per recipient",
  estimatedCreditsLabel: "Estimated credits",
  emailCreditsRemainingLabel: "Email credits remaining",
  smsCreditsRemainingLabel: "SMS credits remaining",
  estimatedRemainingAfterSendLabel: "Estimated remaining after send",
  shortfallTitleEmail: "More Email credits are required.",
  shortfallTitleSms: "More SMS credits are required.",
  buyEmailCredits: CAMPAIGNS_MESSAGING_USAGE_COPY.buyEmailCredits,
  buySmsCredits: CAMPAIGNS_MESSAGING_USAGE_COPY.buySmsCredits,
  changePlan: CAMPAIGNS_MESSAGING_USAGE_COPY.changePlan,
  hardStopBody:
    "There are not enough credits left on this channel. Schedule and send stay blocked.",
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

function formatOptionalCount(value: number | null): string {
  if (value == null) {
    return CAMPAIGN_CHANNEL_COPY.unavailableCount
  }
  return formatCreditCount(value)
}

function channelTitleForId(channelId: CampaignChannelId): string {
  return (
    CAMPAIGN_CHANNEL_OPTIONS.find((option) => option.id === channelId)?.title
    ?? "Channel"
  )
}

function formatEstimatedCreditsFloor(credits: number | null): string {
  if (credits == null) {
    return CAMPAIGN_CHANNEL_COPY.unavailableCount
  }
  return `At least ${formatCreditCount(credits)}`
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
  fixture?: CampaignsMessagingBalancesFixture
  /**
   * Channel/Offer or empty body → floor. Message/Schedule/Review with a body
   * → exact (Reserve estimate when provided; else eligible floor as `{n}`).
   */
  estimateMode?: CampaignChannelEstimateMode
  /** Exact SMS credit estimate from Reserve when live; else omit. */
  smsCreditEstimate?: number | null
}): {
  audienceLine: string
  rows: CampaignChannelUsageRow[]
  estimate: number | null
  remaining: number
  shortfall: number | null
} {
  const fixture = input.fixture ?? CAMPAIGNS_MESSAGING_BALANCES_FIXTURE
  const estimateMode = input.estimateMode ?? "floor"
  const channelEligible =
    input.channelId === "email"
      ? input.eligibility.emailEligible
      : input.eligibility.smsEligible
  const audienceLine = formatCampaignChannelUsageAudienceLine({
    locationName: input.locationName,
    channelId: input.channelId,
    estimatedRecipients: channelEligible,
  })
  const skipped = campaignsMessagingSkippedCount({
    matched: input.eligibility.matched,
    channelEligible,
  })
  const remaining =
    input.channelId === "email"
      ? fixture.email.combinedRemaining
      : fixture.sms.combinedRemaining

  let estimate: number | null
  if (input.channelId === "email") {
    estimate = channelEligible
  } else if (estimateMode === "exact") {
    estimate =
      input.smsCreditEstimate != null
        ? input.smsCreditEstimate
        : channelEligible
  } else {
    estimate = channelEligible
  }

  const afterSend = formatCampaignsMessagingAfterSend({
    remaining,
    estimate,
  })
  const shortfall =
    estimate != null && remaining < estimate ? estimate - remaining : null

  const commonHead: CampaignChannelUsageRow[] = [
    {
      label: CAMPAIGN_CHANNEL_COPY.eligibleRecipientsLabel,
      value: formatOptionalCount(channelEligible),
    },
    {
      label: CAMPAIGN_CHANNEL_COPY.skippedLabel,
      value: formatCampaignsMessagingSkipped(skipped),
    },
  ]

  if (input.channelId === "email") {
    return {
      audienceLine,
      estimate,
      remaining,
      shortfall,
      rows: [
        ...commonHead,
        {
          label: CAMPAIGN_CHANNEL_COPY.estimatedEmailMessagesLabel,
          value: formatOptionalCount(estimate),
        },
        {
          label: CAMPAIGN_CHANNEL_COPY.emailCreditsRemainingLabel,
          value: formatCreditCount(remaining),
        },
        {
          label: CAMPAIGN_CHANNEL_COPY.estimatedRemainingAfterSendLabel,
          value: afterSend,
        },
      ],
    }
  }

  const smsEstimateRows: CampaignChannelUsageRow[] =
    estimateMode === "floor"
      ? [
          {
            label: CAMPAIGN_CHANNEL_COPY.estimatedSmsPartsLabel,
            value: CAMPAIGN_CHANNEL_COPY.estimatedSmsPartsFloorValue,
          },
          {
            label: CAMPAIGN_CHANNEL_COPY.estimatedCreditsLabel,
            value: formatEstimatedCreditsFloor(estimate),
          },
        ]
      : [
          {
            label: CAMPAIGN_CHANNEL_COPY.estimatedSmsPartsLabel,
            value: formatOptionalCount(estimate),
          },
          {
            label: CAMPAIGN_CHANNEL_COPY.estimatedCreditsLabel,
            value: formatOptionalCount(estimate),
          },
        ]

  return {
    audienceLine,
    estimate,
    remaining,
    shortfall,
    rows: [
      ...commonHead,
      ...smsEstimateRows,
      {
        label: CAMPAIGN_CHANNEL_COPY.smsCreditsRemainingLabel,
        value: formatCreditCount(remaining),
      },
      {
        label: CAMPAIGN_CHANNEL_COPY.estimatedRemainingAfterSendLabel,
        value: afterSend,
      },
    ],
  }
}

export function resolveCampaignChannelShortfall(input: {
  channelId: CampaignChannelId
  channelEligible: number | null
  fixture?: CampaignsMessagingBalancesFixture
  estimateMode?: CampaignChannelEstimateMode
  smsCreditEstimate?: number | null
}): CampaignChannelShortfall | null {
  const summary = buildCampaignChannelUsageSummary({
    channelId: input.channelId,
    locationName: "",
    eligibility: {
      matched: null,
      currentlyEligible: input.channelEligible,
      excluded: null,
      emailEligible:
        input.channelId === "email" ? input.channelEligible : null,
      smsEligible: input.channelId === "sms" ? input.channelEligible : null,
      excludedReasons: [],
      source: input.channelEligible == null ? "unavailable" : "live",
    },
    fixture: input.fixture,
    estimateMode: input.estimateMode,
    smsCreditEstimate: input.smsCreditEstimate,
  })

  if (summary.shortfall == null || summary.estimate == null) {
    return null
  }

  return {
    channelId: input.channelId,
    title:
      input.channelId === "email"
        ? CAMPAIGN_CHANNEL_COPY.shortfallTitleEmail
        : CAMPAIGN_CHANNEL_COPY.shortfallTitleSms,
    body: `This campaign requires at least ${formatCreditCount(summary.estimate)} credits. Your account currently has ${formatCreditCount(summary.remaining)} remaining.`,
    buyCreditsLabel:
      input.channelId === "email"
        ? CAMPAIGN_CHANNEL_COPY.buyEmailCredits
        : CAMPAIGN_CHANNEL_COPY.buySmsCredits,
    changePlanLabel: CAMPAIGN_CHANNEL_COPY.changePlan,
  }
}

/** @deprecated Use resolveCampaignChannelShortfall. */
export function resolveCampaignChannelSmsShortfall(input: {
  channelId: CampaignChannelId
  smsEligible: number | null
  fixture?: CampaignsMessagingBalancesFixture
}): CampaignChannelShortfall | null {
  if (input.channelId !== "sms") {
    return null
  }
  return resolveCampaignChannelShortfall({
    channelId: "sms",
    channelEligible: input.smsEligible,
    fixture: input.fixture,
    estimateMode: "floor",
  })
}
