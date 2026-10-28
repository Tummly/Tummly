import { useEffect, useReducer, useRef, useState } from "react"

import {
  addContact,
  fetchOverview,
  GuestPreferencesSessionExpiredError,
  startSession,
  verifyContactCode,
  verifySessionCode,
} from "@/api/publicGuestPreferencesApi"
import { startContactPolling } from "@/lib/guestPreferences/contactPolling"
import {
  contactFlowReducer,
  initialContactFlowState,
  resendCode,
  resendSecondsRemaining,
  submitContact,
  verifyCode,
  type ContactFlowApi,
  type ContactFlowEnv,
  type ContactFlowState,
} from "@/lib/guestPreferences/contactVerifyFlow"

const ENTRY_API: ContactFlowApi = {
  start: startSession,
  verifyCode: verifySessionCode,
}

const ADD_CONTACT_API: ContactFlowApi = {
  start: addContact,
  verifyCode: verifyContactCode,
}

export type GuestPreferencesContactFlowOptions = {
  /** Entry starts a session. Add-contact works inside the open session. */
  variant: "entry" | "add-contact"
  /** Contacts on the overview when add-contact opened. A larger count means the new contact is confirmed. */
  baselineContactCount?: number
  onVerified: () => void
  onSessionExpired?: () => void
}

export type GuestPreferencesContactFlow = {
  state: ContactFlowState
  resendSeconds: number
  setInputValue: (value: string) => void
  submit: () => void
  verifyCode: (code: string) => void
  resend: () => void
  changeContact: () => void
}

/** Re-renders once a second, only while the resend cooldown is running. */
function useResendSecondsRemaining(sentAt: number | null): number {
  const [now, setNow] = useState(() => Date.now())
  const remaining = resendSecondsRemaining(sentAt, now)
  const cooling = remaining > 0

  useEffect(() => {
    if (!cooling) {
      return undefined
    }
    const tick = () => setNow(Date.now())
    tick()
    const timer = window.setInterval(tick, 1000)
    return () => window.clearInterval(timer)
  }, [sentAt, cooling])

  return remaining
}

/**
 * Contact entry and verification state for the entry form and the add-contact
 * dialog. Add-contact email confirmation is picked up by polling the overview.
 */
export function useGuestPreferencesContactFlow({
  variant,
  baselineContactCount = 0,
  onVerified,
  onSessionExpired,
}: GuestPreferencesContactFlowOptions): GuestPreferencesContactFlow {
  const [state, dispatch] = useReducer(
    contactFlowReducer,
    initialContactFlowState
  )
  const resendSeconds = useResendSecondsRemaining(state.sentAt)

  const latestRef = useRef({ onVerified, onSessionExpired })
  const stateRef = useRef(state)
  useEffect(() => {
    latestRef.current = { onVerified, onSessionExpired }
    stateRef.current = state
  })

  useEffect(() => {
    if (variant !== "add-contact" || state.step !== "email-sent") {
      return undefined
    }
    return startContactPolling({
      check: async () =>
        (await fetchOverview()).contacts.length > baselineContactCount,
      onFound: () => {
        dispatch({ type: "verified" })
        latestRef.current.onVerified()
      },
      onError: (error) => {
        if (error instanceof GuestPreferencesSessionExpiredError) {
          latestRef.current.onSessionExpired?.()
        }
      },
    })
  }, [variant, state.step, baselineContactCount])

  function env(): ContactFlowEnv {
    return {
      api: variant === "entry" ? ENTRY_API : ADD_CONTACT_API,
      getState: () => stateRef.current,
      dispatch,
      now: () => Date.now(),
      onVerified: () => latestRef.current.onVerified(),
      onSessionExpired: () => latestRef.current.onSessionExpired?.(),
    }
  }

  return {
    state,
    resendSeconds,
    setInputValue: (value) => dispatch({ type: "input-changed", value }),
    submit: () => void submitContact(env()),
    verifyCode: (code) => void verifyCode(env(), code),
    resend: () => void resendCode(env()),
    changeContact: () => dispatch({ type: "change-contact" }),
  }
}
