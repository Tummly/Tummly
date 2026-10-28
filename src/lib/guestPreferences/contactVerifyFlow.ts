import { isAxiosError } from "axios"

import { GuestPreferencesSessionExpiredError } from "@/api/publicGuestPreferencesApi"
import { GUEST_PREFERENCES_COPY } from "@/content/guestPreferences/guestPreferencesCopy"
import {
  classifyContact,
  type ContactKind,
} from "@/lib/guestPreferences/contactInput"

export const RESEND_COOLDOWN_SECONDS = 60
export const CODE_LENGTH = 6

export type ContactFlowStep = "input" | "email-sent" | "mobile-code" | "verified"

export type ContactFlowState = {
  step: ContactFlowStep
  /** What the contact input holds. */
  inputValue: string
  /** Trimmed contact the verify step is for. */
  contact: string
  /** Time (ms) the last link or code was accepted. */
  sentAt: number | null
  busy: boolean
  /** Message for the current step. */
  error: string | null
}

export const initialContactFlowState: ContactFlowState = {
  step: "input",
  inputValue: "",
  contact: "",
  sentAt: null,
  busy: false,
  error: null,
}

export type ContactFlowAction =
  | { type: "input-changed"; value: string }
  | { type: "request-started" }
  | { type: "sent"; channel: ContactKind; contact: string; at: number }
  | { type: "failed"; message: string }
  | { type: "change-contact" }
  | { type: "verified" }

export function contactFlowReducer(
  state: ContactFlowState,
  action: ContactFlowAction
): ContactFlowState {
  switch (action.type) {
    case "input-changed":
      return { ...state, inputValue: action.value, error: null }
    case "request-started":
      return { ...state, busy: true, error: null }
    case "sent":
      return {
        ...state,
        step: action.channel === "email" ? "email-sent" : "mobile-code",
        contact: action.contact,
        sentAt: action.at,
        busy: false,
        error: null,
      }
    case "failed":
      return { ...state, busy: false, error: action.message }
    case "change-contact":
      return {
        ...state,
        step: "input",
        inputValue: state.contact,
        contact: "",
        sentAt: null,
        busy: false,
        error: null,
      }
    case "verified":
      return { ...state, step: "verified", busy: false, error: null }
  }
}

/** Seconds left before a resend is allowed. A pure function of time. */
export function resendSecondsRemaining(sentAt: number | null, now: number): number {
  if (sentAt === null) {
    return 0
  }
  const elapsedSeconds = Math.floor(Math.max(0, now - sentAt) / 1000)
  return Math.max(0, RESEND_COOLDOWN_SECONDS - elapsedSeconds)
}

export function isCompleteCode(code: string): boolean {
  return new RegExp(`^\\d{${CODE_LENGTH}}$`).test(code)
}

function responseStatus(error: unknown): number | undefined {
  return isAxiosError(error) ? error.response?.status : undefined
}

/** A 400 on send means the contact was rejected. Anything else is a request failure. */
export function sendFailureMessage(error: unknown): string {
  return responseStatus(error) === 400
    ? GUEST_PREFERENCES_COPY.contactInvalid
    : GUEST_PREFERENCES_COPY.requestFailed
}

/** A 400 on verify covers both `invalid` and `expired` codes. */
export function codeFailureMessage(error: unknown): string {
  return responseStatus(error) === 400
    ? GUEST_PREFERENCES_COPY.codeWrong
    : GUEST_PREFERENCES_COPY.codeFailed
}

export type ContactFlowApi = {
  start: (contact: string) => Promise<void>
  verifyCode: (contact: string, code: string) => Promise<void>
}

/** Dependencies of the flow actions. The hook supplies React-backed ones. */
export type ContactFlowEnv = {
  api: ContactFlowApi
  getState: () => ContactFlowState
  dispatch: (action: ContactFlowAction) => void
  now: () => number
  onVerified: () => void
  onSessionExpired: () => void
}

/** Clears busy first, then routes a 401 to the entry view. */
function failRequest(
  env: ContactFlowEnv,
  error: unknown,
  message: string
): void {
  env.dispatch({ type: "failed", message })
  if (error instanceof GuestPreferencesSessionExpiredError) {
    env.onSessionExpired()
  }
}

async function sendContact(
  env: ContactFlowEnv,
  contact: string,
  channel: ContactKind
): Promise<void> {
  env.dispatch({ type: "request-started" })
  try {
    await env.api.start(contact)
    env.dispatch({ type: "sent", channel, contact, at: env.now() })
  } catch (error) {
    failRequest(env, error, sendFailureMessage(error))
  }
}

/** Continue on the contact input. Bad input never reaches the API. */
export async function submitContact(env: ContactFlowEnv): Promise<void> {
  const state = env.getState()
  if (state.busy) {
    return
  }
  const contact = state.inputValue.trim()
  const channel = classifyContact(contact)
  if (channel === null) {
    env.dispatch({
      type: "failed",
      message: GUEST_PREFERENCES_COPY.contactInvalid,
    })
    return
  }
  await sendContact(env, contact, channel)
}

/** Resend the link or code to the contact already on the verify step. */
export async function resendCode(env: ContactFlowEnv): Promise<void> {
  const state = env.getState()
  if (
    state.busy ||
    (state.step !== "email-sent" && state.step !== "mobile-code") ||
    resendSecondsRemaining(state.sentAt, env.now()) > 0
  ) {
    return
  }
  await sendContact(
    env,
    state.contact,
    state.step === "email-sent" ? "email" : "uk-mobile"
  )
}

/** Verify the six-digit code for the mobile step. */
export async function verifyCode(env: ContactFlowEnv, code: string): Promise<void> {
  const state = env.getState()
  if (state.busy || state.step !== "mobile-code" || !isCompleteCode(code)) {
    return
  }
  env.dispatch({ type: "request-started" })
  try {
    await env.api.verifyCode(state.contact, code)
    env.dispatch({ type: "verified" })
    env.onVerified()
  } catch (error) {
    failRequest(env, error, codeFailureMessage(error))
  }
}
