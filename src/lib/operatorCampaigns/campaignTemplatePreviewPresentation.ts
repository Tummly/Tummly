/** Figma Campaign template Preview / Detail drawer — node 5116:19403. */

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

/** Above template picker Dialog (z-[140]) so Preview stacks on top. */
export const CAMPAIGN_TEMPLATE_PREVIEW_OVERLAY_CLASS = "z-[150]"

export const CAMPAIGN_TEMPLATE_PREVIEW_CONTENT_CLASS =
  "z-[155] flex h-full max-h-dvh flex-col overflow-hidden rounded-l-[2px] bg-op-surface-secondary dark:bg-[var(--op-color-gray-1000)] data-[vaul-drawer-direction=right]:w-[min(620px,100vw)] data-[vaul-drawer-direction=right]:sm:max-w-[620px]"

export const CAMPAIGN_TEMPLATE_PREVIEW_SECTION_CLASS =
  "flex w-full flex-col gap-6 border-t border-op-card-border p-[22px]"

export const CAMPAIGN_TEMPLATE_PREVIEW_SECTION_TITLE_CLASS =
  "m-0 text-lg font-bold leading-normal text-op-text-primary"

export const CAMPAIGN_TEMPLATE_PREVIEW_FIELD_LABEL_CLASS =
  "m-0 text-sm font-medium leading-normal text-op-text-primary"

export const CAMPAIGN_TEMPLATE_PREVIEW_FIELD_VALUE_CLASS =
  "m-0 text-sm font-medium leading-normal text-[var(--op-color-gray-550)]"
