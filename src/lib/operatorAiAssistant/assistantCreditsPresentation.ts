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

/** Shared Mic / Send circle fill — Figma Main Bg/Colour, primary glyph. */
const ASSISTANT_COMPOSER_CIRCLE_CHROME_CLASS = [
  "shrink-0 rounded-full shadow-none",
  "bg-op-assistant-credits-background text-op-text-primary",
  "hover:bg-op-assistant-credits-background hover:text-op-text-primary",
].join(" ")

/** Mic circle — 40px, 44px hit below md. */
export const ASSISTANT_COMPOSER_CIRCLE_CLASS = [
  ASSISTANT_COMPOSER_CIRCLE_CHROME_CLASS,
  "size-10 min-h-11 min-w-11 p-2 md:min-h-10 md:min-w-10",
].join(" ")

/** Send circle — smaller than mic. */
export const ASSISTANT_COMPOSER_SEND_CIRCLE_CLASS = [
  ASSISTANT_COMPOSER_CIRCLE_CHROME_CLASS,
  "size-8 min-h-8 min-w-8 p-2",
].join(" ")

export const ASSISTANT_COMPOSER_SEND_ICON_CLASS = "size-4"

const ASSISTANT_COMPOSER_FIELD_BASE_CLASS = [
  "flex min-h-[112px] flex-col justify-between border-0 p-4",
  "md:min-h-[144px] md:p-[21px]",
].join(" ")

/**
 * Outer credits + field shell — rest vs focus.
 * Focus keeps the rest border. Do not paint a ring.
 */
export function assistantComposerBorderClass(_focused: boolean): string {
  return "border-op-assistant-composer-border"
}

export function assistantComposerShellClass(focused: boolean): string {
  return [
    "rounded-[8px] border transition-colors",
    assistantComposerBorderClass(focused),
  ].join(" ")
}

export function assistantComposerTextareaClass(): string {
  return [
    "min-h-0 flex-1 resize-none rounded-none border-0 bg-transparent p-0",
    "text-base text-op-text-primary shadow-none",
    "placeholder:text-[var(--op-color-gray-550)]",
    "focus-visible:border-0 focus-visible:ring-0",
    "disabled:bg-transparent disabled:opacity-100 dark:bg-transparent dark:disabled:bg-transparent",
  ].join(" ")
}

/** True while the mic is recording or transcribing. */
export function assistantComposerMicActive(
  chrome: "mic" | "tick_cancel" | "loader"
): boolean {
  return chrome !== "mic"
}

/** Idle uses Side-nav fill. Mic active lifts to Main Bg/Colour. No field border — shell owns it. */
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
