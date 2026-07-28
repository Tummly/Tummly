/**
 * Capture is a coming-soon stub — the same shell renders for single- and
 * multi-location dashboards. No lists, filters, KPIs, or mode-specific
 * layout until the feature ships.
 */

export const CAPTURE_PAGE_STACK_CLASS = "flex flex-col gap-5"

export const CAPTURE_PAGE_TITLE_CLASS =
  "m-0 text-2xl font-bold leading-10 text-foreground sm:text-[32px]"

export const CAPTURE_EMPTY_SHELL_CLASS =
  "flex min-h-[291px] flex-1 flex-col items-center justify-center rounded-op-lg border border-op-card-border bg-op-card-background p-6"

export const CAPTURE_EMPTY_TITLE_CLASS =
  "m-0 text-base font-medium leading-normal text-foreground"

export const CAPTURE_EMPTY_HELPER_CLASS =
  "m-0 max-w-[450px] text-center text-sm font-medium leading-[18px] text-op-text-muted"

export const OPERATOR_CAPTURE_COMING_SOON_COPY = {
  title: "Capture is coming soon",
  helper:
    "You'll soon be able to generate and manage QR codes here to capture guest feedback for this location.",
} as const
