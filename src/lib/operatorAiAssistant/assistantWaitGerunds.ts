import {
  ASSISTANT_WAIT_BODY,
  ASSISTANT_WAIT_PHRASE_INTERVAL_MS,
  ASSISTANT_WAIT_PREPARING_BODY,
  ASSISTANT_WAIT_RETRIEVING_BODY,
  isAssistantCheckingWaitBody,
} from "./assistantWaitPhrases"

/**
 * @deprecated Prefer `assistantWaitPhrases`. Kept so older imports keep
 * compiling during the wait-copy migration.
 */
export {
  ASSISTANT_WAIT_BODY,
  ASSISTANT_WAIT_PREPARING_BODY,
  ASSISTANT_WAIT_RETRIEVING_BODY,
  isAssistantCheckingWaitBody,
}

export const ASSISTANT_WAIT_GERUND_INTERVAL_MS = ASSISTANT_WAIT_PHRASE_INTERVAL_MS

/** @deprecated Decorative gerunds are no longer used for wait copy. */
export const ASSISTANT_WAIT_GERUNDS = [] as const

export type AssistantWaitGerund = never

export const ASSISTANT_WAIT_GERUND_COUNT = 0

export function formatAssistantWaitGerund(_word: string): string {
  return ASSISTANT_WAIT_BODY
}

export function assistantWaitGerundAt(_index: number): string {
  return "Matching your question to venue data"
}
