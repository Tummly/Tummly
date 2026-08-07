/**
 * Campaign wizard Audience step — Figma 4695:51830 / ticket 23.
 * Live Smart Group counts where available; Campaign eligibility breakdown mocked.
 */

import type { OperatorGuestSmartGroupId } from "@/types/operatorGuests"

export type CampaignAudienceId =
  | "all-eligible-guests"
  | "new-guests"
  | "positive-feedback"
  | "offer-not-redeemed"
  | "recent-redeemers"
  | "no-recent-tummly-activity"
  | "completed-recovery-follow-up"
  | "saved-group"
  | "dormant-guests"

/** Count provenance for audience card stats — never claim server Campaign eligibility. */
export type CampaignAudienceCountSource = "live-smart-group" | "mock"

export type CampaignAudienceOptionDef = {
  id: CampaignAudienceId
  title: string
  description: string
  /** Smart Group id when this card can show a live membership count. */
  liveSmartGroupId: OperatorGuestSmartGroupId | null
  /** Offer-based Smart Groups — shown in chrome but never as live counts. */
  deferredOfferGroup: boolean
  recommended: boolean
}

/**
 * Fixed Figma sample eligibility figures (4695:51830).
 * Not Campaign eligibility API output — mock until that service exists.
 */
export const CAMPAIGN_AUDIENCE_ELIGIBILITY_MOCK = {
  matched: 184,
  currentlyEligible: 162,
  excluded: 22,
  emailEligible: 148,
  smsEligible: 121,
} as const

export const CAMPAIGN_AUDIENCE_COPY = {
  stepHeading: "Who should receive this campaign?",
  stepDescription:
    "Choose one audience. Only guests who are currently marketing eligible will be included.",
  recommendedBadge: "Recommended audience",
  summaryTitle: "Selected audience summary",
  matchedLabel: "Matched",
  currentlyEligibleLabel: "Currently eligible",
  excludedLabel: "Excluded",
  emailEligibleLabel: "Email eligible",
  smsEligibleLabel: "SMS eligible",
  chooseSavedGroupLabel: "Choose saved group",
  chooseSavedGroupPlaceholder: "Select",
  /** Mock-only saved groups until a saved-audience API exists. */
  mockSavedGroupOptions: [
    { value: "weekday-regulars", label: "Weekday regulars" },
    { value: "vip-guests", label: "VIP guests" },
  ] as const,
} as const

/**
 * Figma audience order. Live counts only for non-deferred Smart Group twins
 * (All / New guests / Positive feedback / Dormant). Deferred offer groups stay mock.
 * Needs recovery is live on Guests but not an Audience card in this frame.
 */
export const CAMPAIGN_AUDIENCE_OPTIONS: readonly CampaignAudienceOptionDef[] = [
  {
    id: "all-eligible-guests",
    title: "All eligible guests",
    description:
      "Everyone in the selected location scope who is currently eligible for at least one marketing channel.",
    liveSmartGroupId: "all-guests",
    deferredOfferGroup: false,
    recommended: true,
  },
  {
    id: "new-guests",
    title: "New guests",
    description: "Guests first captured during the last 30 days.",
    liveSmartGroupId: "new-guests",
    deferredOfferGroup: false,
    recommended: false,
  },
  {
    id: "positive-feedback",
    title: "Positive feedback",
    description:
      "Guests who selected Positive in private feedback during the chosen period.",
    liveSmartGroupId: "positive-feedback",
    deferredOfferGroup: false,
    recommended: false,
  },
  {
    id: "offer-not-redeemed",
    title: "Offer not redeemed",
    description:
      "Guests who claimed a selected active offer but have not redeemed it.",
    liveSmartGroupId: "offer-not-redeemed",
    deferredOfferGroup: true,
    recommended: false,
  },
  {
    id: "recent-redeemers",
    title: "Recent redeemers",
    description:
      "Guests with a validated Tummly offer redemption during the selected period.",
    liveSmartGroupId: "recent-redeemers",
    deferredOfferGroup: true,
    recommended: false,
  },
  {
    id: "no-recent-tummly-activity",
    title: "No recent Tummly activity",
    description:
      "Guests with no recorded scan, feedback, campaign click, offer claim or redemption during the selected period.",
    liveSmartGroupId: null,
    deferredOfferGroup: false,
    recommended: false,
  },
  {
    id: "completed-recovery-follow-up",
    title: "Completed recovery follow-up",
    description:
      "Guests whose private feedback recovery workflow is complete and who are separately eligible for marketing.",
    liveSmartGroupId: null,
    deferredOfferGroup: false,
    recommended: false,
  },
  {
    id: "saved-group",
    title: "Saved group",
    description: "Choose a saved guest group or tag-based audience.",
    liveSmartGroupId: null,
    deferredOfferGroup: false,
    recommended: false,
  },
  {
    id: "dormant-guests",
    title: "Dormant guests",
    description:
      "Guests whose last recorded Guest Loop interaction is older than 90 days.",
    liveSmartGroupId: "dormant-guests",
    deferredOfferGroup: false,
    recommended: false,
  },
] as const

export type CampaignAudienceEligibilityBreakdown = {
  matched: number
  currentlyEligible: number
  excluded: number
  emailEligible: number
  smsEligible: number
  /** Always mock in slice 1 — not a Campaign eligibility API response. */
  source: "mock"
}

export type CampaignAudienceSmartGroupCountsInput = {
  smartGroupCounts: Partial<Record<OperatorGuestSmartGroupId, number>>
}

export function mockCampaignAudienceEligibilityBreakdown(): CampaignAudienceEligibilityBreakdown {
  return {
    ...CAMPAIGN_AUDIENCE_ELIGIBILITY_MOCK,
    source: "mock",
  }
}

export function formatAudienceMatchedEligibleLabel(
  matched: number,
  currentlyEligible: number
): string {
  const format = (value: number) => value.toLocaleString("en-GB")
  return `${format(matched)} matched · ${format(currentlyEligible)} currently eligible`
}

/**
 * Resolve card matched / currently-eligible display counts.
 * Live Smart Group membership feeds matched only. Currently eligible on the
 * card stays the fixed mock figure until a Campaign eligibility API exists
 * (never scaled from live matched — that would invent eligibility).
 */
export function resolveAudienceCardCounts(input: {
  option: CampaignAudienceOptionDef
  liveCounts: CampaignAudienceSmartGroupCountsInput | null
}): {
  matched: number
  currentlyEligible: number
  countSource: CampaignAudienceCountSource
} {
  const { option, liveCounts } = input
  const mock = CAMPAIGN_AUDIENCE_ELIGIBILITY_MOCK

  if (
    option.deferredOfferGroup
    || option.liveSmartGroupId == null
    || liveCounts == null
  ) {
    return {
      matched: mock.matched,
      currentlyEligible: mock.currentlyEligible,
      countSource: "mock",
    }
  }

  const liveMatched =
    liveCounts.smartGroupCounts[option.liveSmartGroupId] ?? null

  if (liveMatched == null) {
    return {
      matched: mock.matched,
      currentlyEligible: mock.currentlyEligible,
      countSource: "mock",
    }
  }

  return {
    matched: liveMatched,
    currentlyEligible: mock.currentlyEligible,
    countSource: "live-smart-group",
  }
}
