/** Global Search overlay + trigger chrome — Figma 6518:13702. */

export const GLOBAL_SEARCH_PLACEHOLDER =
  "Search guests, Feedback, Campaigns, Offers and more…"

export const GLOBAL_SEARCH_TRIGGER_PLACEHOLDER =
  "Search guests, feedback, offers and campaigns…"

export const GLOBAL_SEARCH_AI_HEADING = "AI suggestions"

export const GLOBAL_SEARCH_ASK_TUMMLY_HEADING = "Ask Tummly"

export const GLOBAL_SEARCH_GUESTS_HEADING = "Guests"

export const GLOBAL_SEARCH_FEEDBACK_HEADING = "Feedback"

export const GLOBAL_SEARCH_CAMPAIGNS_HEADING = "Campaigns"

export const GLOBAL_SEARCH_OFFERS_HEADING = "Offers"

export const GLOBAL_SEARCH_QR_CODES_HEADING = "QR codes"

export const GLOBAL_SEARCH_VIEW_ALL_GUESTS = "View all Guests"

export const GLOBAL_SEARCH_VIEW_ALL_FEEDBACK = "View all Feedback"

export const GLOBAL_SEARCH_VIEW_ALL_CAMPAIGNS = "View all Campaigns"

export const GLOBAL_SEARCH_VIEW_ALL_OFFERS = "View all Offers"

export const GLOBAL_SEARCH_VIEW_ALL_QR_CODES = "View all QR codes"

export const GLOBAL_SEARCH_ALL_LOCATIONS_LABEL = "Search all authorised locations"

export const GLOBAL_SEARCH_NO_RESULTS_MESSAGE = "No results for this location."

export const GLOBAL_SEARCH_WIDEN_FROM_NO_RESULTS_LABEL =
  GLOBAL_SEARCH_ALL_LOCATIONS_LABEL

export const GLOBAL_SEARCH_ERROR_MESSAGE =
  "Search could not finish. Check your connection and try again."

export const GLOBAL_SEARCH_TRY_AGAIN_LABEL = "Try again"

export const GLOBAL_SEARCH_OFFLINE_MESSAGE =
  "You are offline. Connect to the internet to search."

export const GLOBAL_SEARCH_PARTIAL_WARNING =
  "Some result groups could not load. Showing what is available."

export const GLOBAL_SEARCH_DIALOG_TITLE = "Global Search"

/** Mobile full-screen Search Dialog (desktop uses field-anchored Popover). */
export const GLOBAL_SEARCH_OVERLAY_CLASS = [
  "flex flex-col gap-0 overflow-hidden border border-op-card-border bg-op-surface-primary p-0 text-op-text-primary shadow-none",
  "top-0 left-0 h-dvh max-h-dvh w-full max-w-full translate-x-0 -translate-y-0 rounded-none",
].join(" ")

/** Desktop results Popover under the navbar Search field (Figma 6518:13702).
 * Width is set inline to match the Search field (see OperatorShellSearchField). */
export const GLOBAL_SEARCH_POPOVER_CLASS = [
  "z-[120] flex max-h-[min(80vh,640px)] w-auto max-w-none min-w-0",
  "flex-col gap-0 overflow-hidden rounded-b-op-sm border border-op-card-border",
  "bg-op-surface-primary p-0 text-op-text-primary shadow-none ring-0",
].join(" ")

export const GLOBAL_SEARCH_INPUT_ROW_CLASS =
  "flex items-center gap-2 border-b border-op-card-border px-3 py-3 md:gap-3 md:px-5 md:py-4"

export const GLOBAL_SEARCH_INPUT_CLASS =
  "min-w-0 flex-1 border-0 bg-transparent text-sm text-op-text-primary outline-none placeholder:text-op-header-search-text focus-visible:ring-0"

export const GLOBAL_SEARCH_SECTION_LABEL_CLASS =
  "px-5 pt-5 text-sm font-medium text-op-header-search-text md:pt-5"

export const GLOBAL_SEARCH_AI_ROW_CLASS = [
  "flex cursor-pointer items-center gap-2.5 px-5 py-2 text-sm text-op-text-primary outline-none select-none",
  // Header search hover token equals surface-primary (invisible on this panel).
  "hover:bg-op-assistant-list-row-hover",
  "data-[selected=true]:bg-op-assistant-list-row-active data-[selected=true]:text-op-text-primary",
  "aria-[selected=true]:bg-op-assistant-list-row-active aria-[selected=true]:text-op-text-primary",
  "[&>svg:last-child]:hidden",
].join(" ")

/** Same row chrome as AI suggestions; entity rows add Avatar/Badge content. */
export const GLOBAL_SEARCH_ENTITY_ROW_CLASS = GLOBAL_SEARCH_AI_ROW_CLASS

export const GLOBAL_SEARCH_ENTITY_AVATAR_CLASS =
  "size-8 bg-op-color-gray-992 text-xs font-medium text-op-header-search-text dark:bg-[var(--op-color-gray-980)]"

export const GLOBAL_SEARCH_FOOTER_CLASS =
  "mt-auto flex items-center justify-between border-t border-op-card-border px-5 py-3 text-xs text-op-header-search-text"

export const GLOBAL_SEARCH_FOOTER_HINT_CLASS =
  "inline-flex items-center gap-2"

export const GLOBAL_SEARCH_KBD_CLASS =
  "inline-flex h-auto min-w-0 items-center justify-center rounded-full bg-op-color-gray-992 px-1 py-1 text-[12px] font-normal text-op-header-search-text dark:bg-[var(--op-color-gray-980)]"

export const GLOBAL_SEARCH_TRIGGER_CLASS = [
  "flex min-w-0 cursor-pointer items-center gap-2 px-2.5 text-left text-xs text-op-header-search-text lg:gap-3 lg:px-3.5 lg:text-sm",
  "hover:bg-op-header-search-hover focus-visible:ring-2 focus-visible:ring-ring focus-visible:outline-none",
].join(" ")
