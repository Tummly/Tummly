/**
 * Map catalogue by-id suggestion keys onto Campaign wizard step ids (ticket 28).
 * Catalogue seeds may use display-oriented aliases; wizard state needs concrete ids.
 */

import {
  CAMPAIGN_AUDIENCE_OPTIONS,
  type CampaignAudienceId,
} from "@/lib/operatorCampaigns/campaignAudiencePresentation"
import {
  CAMPAIGN_CHANNEL_OPTIONS,
  defaultCampaignChannelId,
  type CampaignChannelId,
} from "@/lib/operatorCampaigns/campaignChannelPresentation"
import {
  CAMPAIGN_OFFER_OPTIONS,
  defaultCampaignOfferStanceId,
  type CampaignOfferStanceId,
} from "@/lib/operatorCampaigns/campaignOfferPresentation"
import {
  CAMPAIGN_GOAL_OPTIONS,
  type CampaignGoalId,
} from "@/lib/operatorCampaigns/campaignWizardPresentation"
import type { CampaignTemplateSuggestionDefaults } from "@/types/operatorCampaigns"

const AUDIENCE_IDS = new Set<string>(
  CAMPAIGN_AUDIENCE_OPTIONS.map((option) => option.id)
)

const GOAL_IDS = new Set<string>(CAMPAIGN_GOAL_OPTIONS.map((goal) => goal.id))

const CHANNEL_IDS = new Set<string>(
  CAMPAIGN_CHANNEL_OPTIONS.map((option) => option.id)
)

const OFFER_STANCE_IDS = new Set<string>(
  CAMPAIGN_OFFER_OPTIONS.map((option) => option.id)
)

/** Catalogue aliases → wizard audience id. */
const AUDIENCE_ALIASES: Record<string, CampaignAudienceId> = {
  "all-eligible-or-saved-group": "all-eligible-guests",
  "eligible-returning-guests": "all-eligible-guests",
}

/** Catalogue aliases → wizard channel id. */
const CHANNEL_ALIASES: Record<string, CampaignChannelId> = {
  "email-or-sms": "email",
  "based-on-permission": "email",
}

/** Catalogue aliases → wizard offer stance id. */
const OFFER_STANCE_ALIASES: Record<string, CampaignOfferStanceId> = {
  optional: "no-offer",
  "optional-controlled-recovery": "create-new-offer",
  recommended: "create-new-offer",
}

export type ResolvedCampaignTemplateSuggestions = {
  goalId: CampaignGoalId
  audienceId: CampaignAudienceId
  channelId: CampaignChannelId
  offerStanceId: CampaignOfferStanceId
}

function resolveGoalId(raw: string): CampaignGoalId {
  if (GOAL_IDS.has(raw)) {
    return raw as CampaignGoalId
  }
  return "custom-campaign"
}

function resolveAudienceId(raw: string): CampaignAudienceId {
  if (raw === "saved-group") {
    // Legacy Draft key — not an Audience card; Continue / Save blocked until reselect.
    return "saved-group"
  }
  if (AUDIENCE_IDS.has(raw)) {
    return raw as CampaignAudienceId
  }
  return AUDIENCE_ALIASES[raw] ?? "all-eligible-guests"
}

function resolveChannelId(raw: string): CampaignChannelId {
  if (CHANNEL_IDS.has(raw)) {
    return raw as CampaignChannelId
  }
  return CHANNEL_ALIASES[raw] ?? defaultCampaignChannelId()
}

function resolveOfferStanceId(raw: string): CampaignOfferStanceId {
  if (OFFER_STANCE_IDS.has(raw)) {
    return raw as CampaignOfferStanceId
  }
  return OFFER_STANCE_ALIASES[raw] ?? defaultCampaignOfferStanceId()
}

export function mapCampaignTemplateSuggestions(
  suggestions: CampaignTemplateSuggestionDefaults
): ResolvedCampaignTemplateSuggestions {
  return {
    goalId: resolveGoalId(suggestions.goalId),
    audienceId: resolveAudienceId(suggestions.audienceKey),
    channelId: resolveChannelId(suggestions.channel),
    offerStanceId: resolveOfferStanceId(suggestions.offerStance),
  }
}
