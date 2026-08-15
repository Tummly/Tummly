/**
 * Wait-row chrome — Claude-style shimmer plus the drafting-dialogue spin.
 * The module still stores `ASSISTANT_WAIT_BODY` ("Working…") for a11y.
 */

export const ASSISTANT_WAIT_PHRASES = [
  "Working…",
  "Thinking…",
  "Analyzing…",
  "Looking up…",
] as const

export const ASSISTANT_WAIT_PHRASE_MS = 2200

export function assistantWaitPhraseAt(
  elapsedMs: number,
  reducedMotion: boolean
): string {
  if (reducedMotion || elapsedMs < 0) {
    return ASSISTANT_WAIT_PHRASES[0]
  }
  const index =
    Math.floor(elapsedMs / ASSISTANT_WAIT_PHRASE_MS)
    % ASSISTANT_WAIT_PHRASES.length
  return ASSISTANT_WAIT_PHRASES[index]
}

/** Same spin as the Drafting AI Response dialogue. */
export const ASSISTANT_WAIT_ICON_CLASS =
  "animate-spin motion-reduce:animate-none"

export const ASSISTANT_WAIT_TEXT_CLASS =
  "assistant-wait-shimmer text-sm leading-5"
