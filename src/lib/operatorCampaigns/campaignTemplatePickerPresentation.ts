/** Figma Choose a campaign template — node 4756:74801. */

export const CAMPAIGN_TEMPLATE_PICKER_COPY = {
  title: "Choose a campaign template",
  subtitle:
    "Start with a practical campaign structure, then review the audience, channel, offer, message, timing and usage.",
  searchPlaceholder: "Search campaign templates",
  useTemplate: "Use template",
  preview: "Preview",
  loadError: "Could not load campaign templates. Please try again.",
  emptyError: "Campaign template catalogue is empty.",
  retry: "Retry",
  searchMiss: "No templates match your search.",
  goalMeta: "Goal",
  audienceMeta: "Suggested audience",
  channelMeta: "Suggested channel",
  offerMeta: "Offer",
  closeAriaLabel: "Close template picker",
} as const

/**
 * Content and overlay share z-[140] so the picker sits above OperatorWizardShell
 * (z-[130]) with content ≥ overlay ≥ parent.
 */
export const CAMPAIGN_TEMPLATE_PICKER_OVERLAY_CLASS = "z-[140]"

/**
 * Shell keeps `p-8` + `overflow-hidden` so bottom padding stays visible.
 * Scroll lives on the body — padding on a scrolling shell clips the last cards.
 */
export const CAMPAIGN_TEMPLATE_PICKER_CONTENT_CLASS =
  "z-[140] flex max-h-[min(90vh,920px)] min-h-[min(90vh,720px)] w-full max-w-[calc(100%-2rem)] flex-col gap-10 overflow-hidden rounded-op-md border-0 bg-op-surface-secondary p-8 text-op-text-primary shadow-lg sm:max-w-[1408px] dark:bg-[var(--op-color-gray-1000)]"

/** Shared scroll body — loading / error / loaded share height inside the padded shell. */
export const CAMPAIGN_TEMPLATE_PICKER_BODY_CLASS =
  "flex min-h-0 flex-1 flex-col overflow-y-auto"

export const CAMPAIGN_TEMPLATE_PICKER_TITLE_CLASS =
  "pr-0 text-2xl font-bold leading-normal tracking-normal text-op-text-primary"

export const CAMPAIGN_TEMPLATE_PICKER_SUBTITLE_CLASS =
  "max-w-[395px] text-sm font-medium leading-[18px] tracking-normal text-[var(--op-color-gray-550)]"

export const CAMPAIGN_TEMPLATE_PICKER_SEARCH_WRAP_CLASS =
  "relative w-full"

export const CAMPAIGN_TEMPLATE_PICKER_SEARCH_FIELD_CLASS =
  "h-10 w-full rounded-[2px] border-0 bg-op-header-search-background pl-10 text-sm font-medium text-op-text-primary placeholder:text-op-header-search-text focus-visible:ring-1 focus-visible:ring-op-border-default"

export const CAMPAIGN_TEMPLATE_PICKER_GRID_CLASS =
  "grid grid-cols-1 gap-[30px] md:grid-cols-2 xl:grid-cols-3"

export const CAMPAIGN_TEMPLATE_CARD_CLASS =
  "flex flex-col gap-[46px] overflow-clip rounded-op-md border border-op-card-border p-6"

export const CAMPAIGN_TEMPLATE_CARD_TITLE_CLASS =
  "m-0 text-base font-semibold leading-6 tracking-[-0.4px] text-op-card-title-color"

/** Figma Main Bg / Subtitle (#7c7c7c) — same as picker dialog subtitle. */
export const CAMPAIGN_TEMPLATE_CARD_DESCRIPTION_CLASS =
  "m-0 text-sm font-medium leading-[19px] text-[var(--op-color-gray-550)]"

export const CAMPAIGN_TEMPLATE_CARD_META_ROW_CLASS =
  "flex items-start justify-between gap-3 text-xs leading-normal text-[var(--op-color-gray-550)]"

export const CAMPAIGN_TEMPLATE_CARD_META_LABEL_CLASS =
  "shrink-0 font-medium"

export const CAMPAIGN_TEMPLATE_CARD_META_VALUE_CLASS =
  "text-right font-normal"

export const CAMPAIGN_TEMPLATE_CARD_ACTIONS_CLASS =
  "flex flex-wrap items-center gap-3"
