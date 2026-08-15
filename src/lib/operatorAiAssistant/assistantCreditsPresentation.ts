/**
 * Stub AI credit chrome for the composer inbox (Figma 3454:56050).
 * Metering is not live. These numbers do not burn **AI credit**s.
 */
export const ASSISTANT_CREDITS_STUB_REMAINING = 20
export const ASSISTANT_CREDITS_STUB_ALLOWANCE = 20
export const ASSISTANT_VIEW_USAGE_LABEL = "View usage"
export const ASSISTANT_ADD_CREDITS_LABEL = "Add credits"

export function assistantCreditsRemainingLine(
  remaining: number,
  allowance: number
): string {
  return `${remaining} of ${allowance} monthly AI actions remaining`
}

export const ASSISTANT_CREDITS_STUB_REMAINING_LINE =
  assistantCreditsRemainingLine(
    ASSISTANT_CREDITS_STUB_REMAINING,
    ASSISTANT_CREDITS_STUB_ALLOWANCE
  )

/** Mic / Send circle — Figma Main Bg/Colour fill, primary glyph. */
export const ASSISTANT_COMPOSER_CIRCLE_CLASS = [
  "size-10 min-h-11 min-w-11 shrink-0 rounded-full p-2 shadow-none",
  "bg-op-assistant-credits-background text-op-text-primary",
  "hover:bg-op-assistant-credits-background hover:text-op-text-primary",
  "md:min-h-10 md:min-w-10",
].join(" ")

const ASSISTANT_COMPOSER_FIELD_BASE_CLASS = [
  "flex min-h-[144px] flex-col justify-between rounded-[8px] border p-[21px]",
  "border-op-assistant-composer-border",
].join(" ")

/** True while the mic is recording or transcribing. */
export function assistantComposerMicActive(
  chrome: "mic" | "tick_cancel" | "loader"
): boolean {
  return chrome !== "mic"
}

/** Idle uses Side-nav fill. Mic active lifts to Main Bg/Colour. */
export function assistantComposerFieldClass(
  chrome: "mic" | "tick_cancel" | "loader"
): string {
  return [
    ASSISTANT_COMPOSER_FIELD_BASE_CLASS,
    assistantComposerMicActive(chrome)
      ? "bg-op-assistant-composer-recording-background"
      : "bg-op-assistant-composer-background",
  ].join(" ")
}
