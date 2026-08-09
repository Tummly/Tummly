/**
 * Campaign wizard Audience step — Figma 4695:51830 / ticket 23 + MVP issue 20.
 * Live Smart Group counts where available; unevaluable cards stay honest.
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
  /** Legacy Draft key only — removed from Audience cards; Continue / Save blocked. */
  | "saved-group"
  | "dormant-guests"

/** Count provenance for audience card stats — never claim server Campaign eligibility. */
export type CampaignAudienceCountSource =
  | "live-smart-group"
  | "mock"
  | "unavailable"

export type CampaignAudienceOptionDef = {
  id: Exclude<CampaignAudienceId, "saved-group">
  title: string
  description: string
  /** Smart Group id when this card can show a live membership count. */
  liveSmartGroupId: OperatorGuestSmartGroupId | null
  /**
   * MVP cannot evaluate membership / eligibility honestly — show unavailable
   * counts and block Audience Continue while selected.
   */
  unevaluable: boolean
  recommended: boolean
}

/**
 * Fixed Figma sample eligibility figures (4695:51830).
 * Not Campaign eligibility API output — mock until that service exists.
 * Never used for unevaluable audience cards.
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
  countsUnavailableLabel: "Counts unavailable",
} as const

/**
 * Figma audience order minus Saved group (out of MVP).
 * Live counts only for non-deferred Smart Group twins
 * (All / New guests / Positive feedback / Dormant).
 * Offer not redeemed / Recent redeemers / No recent Tummly activity stay
 * visible but unevaluable.
 */
export const CAMPAIGN_AUDIENCE_OPTIONS: readonly CampaignAudienceOptionDef[] = [
  {
    id: "all-eligible-guests",
    title: "All eligible guests",
    description:
      "Everyone in the selected location scope who is currently eligible for at least one marketing channel.",
    liveSmartGroupId: "all-guests",
    unevaluable: false,
    recommended: true,
  },
  {
    id: "new-guests",
    title: "New guests",
    description: "Guests first captured during the last 30 days.",
    liveSmartGroupId: "new-guests",
    unevaluable: false,
    recommended: false,
  },
  {
    id: "positive-feedback",
    title: "Positive feedback",
    description:
      "Guests who selected Positive in private feedback during the chosen period.",
    liveSmartGroupId: "positive-feedback",
    unevaluable: false,
    recommended: false,
  },
  {
    id: "offer-not-redeemed",
    title: "Offer not redeemed",
    description:
      "Guests who claimed a selected active offer but have not redeemed it.",
    liveSmartGroupId: "offer-not-redeemed",
    unevaluable: true,
    recommended: false,
  },
  {
    id: "recent-redeemers",
    title: "Recent redeemers",
    description:
      "Guests with a validated Tummly offer redemption during the selected period.",
    liveSmartGroupId: "recent-redeemers",
    unevaluable: true,
    recommended: false,
  },
  {
    id: "no-recent-tummly-activity",
    title: "No recent Tummly activity",
    description:
      "Guests with no recorded scan, feedback, campaign click, offer claim or redemption in the last 30 days (UTC).",
    liveSmartGroupId: null,
    unevaluable: true,
    recommended: false,
  },
  {
    id: "completed-recovery-follow-up",
    title: "Completed recovery follow-up",
    description:
      "Guests whose private feedback recovery workflow is complete and who are separately eligible for marketing.",
    liveSmartGroupId: null,
    unevaluable: false,
    recommended: false,
  },
  {
    id: "dormant-guests",
    title: "Dormant guests",
    description:
      "Guests whose latest feedback is older than 90 days.",
    liveSmartGroupId: "dormant-guests",
    unevaluable: false,
    recommended: false,
  },
] as const

export type CampaignAudienceEligibilityBreakdown = {
  matched: number | null
  currentlyEligible: number | null
  excluded: number | null
  emailEligible: number | null
  smsEligible: number | null
  /** Mock until Campaign eligibility API; unavailable when selected audience cannot be evaluated. */
  source: "mock" | "unavailable"
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

export function unavailableCampaignAudienceEligibilityBreakdown(): CampaignAudienceEligibilityBreakdown {
  return {
    matched: null,
    currentlyEligible: null,
    excluded: null,
    emailEligible: null,
    smsEligible: null,
    source: "unavailable",
  }
}

export function formatAudienceMatchedEligibleLabel(
  matched: number,
  currentlyEligible: number
): string {
  const format = (value: number) => value.toLocaleString("en-GB")
  return `${format(matched)} matched · ${format(currentlyEligible)} currently eligible`
}

export function isCampaignAudienceUnevaluable(
  audienceId: CampaignAudienceId
): boolean {
  if (audienceId === "saved-group") {
    return true
  }
  const option = CAMPAIGN_AUDIENCE_OPTIONS.find(
    (entry) => entry.id === audienceId
  )
  return option?.unevaluable === true
}

/**
 * Resolve card matched / currently-eligible display counts.
 * Live Smart Group membership feeds matched only. Currently eligible on the
 * card stays the fixed mock figure until a Campaign eligibility API exists
 * (never scaled from live matched — that would invent eligibility).
 * Unevaluable cards never show mock numbers.
 */
export function resolveAudienceCardCounts(input: {
  option: CampaignAudienceOptionDef
  liveCounts: CampaignAudienceSmartGroupCountsInput | null
}): {
  matched: number | null
  currentlyEligible: number | null
  countSource: CampaignAudienceCountSource
} {
  const { option, liveCounts } = input
  const mock = CAMPAIGN_AUDIENCE_ELIGIBILITY_MOCK

  if (option.unevaluable) {
    return {
      matched: null,
      currentlyEligible: null,
      countSource: "unavailable",
    }
  }

  if (option.liveSmartGroupId == null || liveCounts == null) {
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
