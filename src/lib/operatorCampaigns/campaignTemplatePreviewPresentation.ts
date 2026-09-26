/** Figma Campaign template Preview / Detail drawer — node 5116:19403. */

import { OPERATOR_RIGHT_DRAWER_CONTENT_CLASS } from "@/lib/operatorHome/shellResponsivePresentation"

export const CAMPAIGN_TEMPLATE_PREVIEW_COPY = {
  subtitle:
    "See the suggested audience, channel, offer, message and send logic before using this template.",
  templateSummary: "Template summary",
  goal: "Goal:",
  bestFor: "Best for:",
  suggestedAudience: "Suggested audience:",
  suggestedChannel: "Suggested channel:",
  offer: "Offer:",
  exampleMessage: "Example guest-facing message",
  estimatedUsage: "Estimated usage",
  emailTab: "Email",
  smsTab: "SMS",
  offerLogic: "Offer logic preview",
  audienceEligibility: "Audience eligibility",
  emailEligible: "Email",
  smsEligible: "SMS",
  totalUniqueGuests: "Total unique guests",
  suggestedTiming: "Suggested timing",
  footerDisclaimer:
    "You'll be able to review the audience, offer, message, cost and final recipient count before anything is sent.",
  useThisTemplate: "Use this template",
  close: "Close",
  closeAriaLabel: "Close campaign template preview",
  loadError: "Could not load this campaign template preview. Please try again.",
  retry: "Retry",
} as const

/**
 * Above template picker Dialog (z-[140]). No blur — same as Feedback /
 * Operator right drawers (`supports-backdrop-filter:backdrop-blur-none`).
 */
export const CAMPAIGN_TEMPLATE_PREVIEW_OVERLAY_CLASS =
  "z-[150] supports-backdrop-filter:backdrop-blur-none"

/** Shared Operator right-drawer chrome + stack above picker Dialog. */
export const CAMPAIGN_TEMPLATE_PREVIEW_CONTENT_CLASS = `z-[155] flex flex-col ${OPERATOR_RIGHT_DRAWER_CONTENT_CLASS}`

export const CAMPAIGN_TEMPLATE_PREVIEW_SECTION_CLASS =
  "flex w-full flex-col gap-6 border-t border-op-card-border p-[22px]"

export const CAMPAIGN_TEMPLATE_PREVIEW_SECTION_TITLE_CLASS =
  "m-0 text-lg font-bold leading-normal text-op-text-primary"

export const CAMPAIGN_TEMPLATE_PREVIEW_FIELD_LABEL_CLASS =
  "m-0 text-sm font-medium leading-normal text-op-text-primary"

export const CAMPAIGN_TEMPLATE_PREVIEW_FIELD_VALUE_CLASS =
  "m-0 text-sm font-medium leading-normal text-[var(--op-color-gray-550)]"
