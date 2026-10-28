import { AxiosError, AxiosHeaders } from "axios"
import { describe, expect, it } from "vitest"

import { GuestPreferencesSessionExpiredError } from "@/api/publicGuestPreferencesApi"
import { GUEST_PREFERENCES_COPY } from "@/content/guestPreferences/guestPreferencesCopy"
import {
  codeFailureMessage,
  contactFlowReducer,
  initialContactFlowState,
  resendCode,
  resendSecondsRemaining,
  sendFailureMessage,
  submitContact,
  verifyCode,
  type ContactFlowApi,
  type ContactFlowEnv,
  type ContactFlowState,
} from "./contactVerifyFlow"

function axiosError(status: number): AxiosError {
  return new AxiosError(
    "request failed",
    "ERR_BAD_REQUEST",
    undefined,
    undefined,
    {
      status,
      statusText: "",
      headers: {},
      config: { headers: new AxiosHeaders() },
      data: { error: "invalid" },
    }
  )
}

type Harness = {
  env: ContactFlowEnv
  state: () => ContactFlowState
  starts: string[]
  verifies: Array<{ contact: string; code: string }>
  verifiedCount: () => number
  expiredCount: () => number
  setNow: (at: number) => void
}

/** Builds an env with a real reducer behind dispatch and scripted API errors. */
function harness(
  input: { initial?: Partial<ContactFlowState>; startError?: unknown; verifyError?: unknown } = {}
): Harness {
  let state: ContactFlowState = { ...initialContactFlowState, ...input.initial }
  let now = 1_000_000
  const starts: string[] = []
  const verifies: Array<{ contact: string; code: string }> = []
  let verified = 0
  let expired = 0
  const api: ContactFlowApi = {
    start: async (contact) => {
      starts.push(contact)
      if (input.startError !== undefined) {
        throw input.startError
      }
    },
    verifyCode: async (contact, code) => {
      verifies.push({ contact, code })
      if (input.verifyError !== undefined) {
        throw input.verifyError
      }
    },
  }
  const env: ContactFlowEnv = {
    api,
    getState: () => state,
    dispatch: (action) => {
      state = contactFlowReducer(state, action)
    },
    now: () => now,
    onVerified: () => {
      verified += 1
    },
    onSessionExpired: () => {
      expired += 1
    },
  }
  return {
    env,
    state: () => state,
    starts,
    verifies,
    verifiedCount: () => verified,
    expiredCount: () => expired,
    setNow: (at) => {
      now = at
    },
  }
}

describe("resendSecondsRemaining", () => {
  it("is 60 when the code was just sent", () => {
    expect(resendSecondsRemaining(0, 0)).toBe(60)
  })

  it("counts down one second at a time", () => {
    expect(resendSecondsRemaining(0, 999)).toBe(60)
    expect(resendSecondsRemaining(0, 1000)).toBe(59)
    expect(resendSecondsRemaining(0, 59_999)).toBe(1)
  })

  it("is 0 at 60 seconds and stays at 0", () => {
    expect(resendSecondsRemaining(0, 60_000)).toBe(0)
    expect(resendSecondsRemaining(0, 120_000)).toBe(0)
  })

  it("is 0 before anything was sent", () => {
    expect(resendSecondsRemaining(null, 5_000)).toBe(0)
  })
})

describe("sendFailureMessage and codeFailureMessage", () => {
  it("maps a 400 on send to the invalid contact message", () => {
    expect(sendFailureMessage(axiosError(400))).toBe(GUEST_PREFERENCES_COPY.contactInvalid)
  })

  it("maps other send failures to the request message", () => {
    expect(sendFailureMessage(axiosError(500))).toBe(GUEST_PREFERENCES_COPY.requestFailed)
  })

  it("maps a 400 on verify to the wrong code message", () => {
    expect(codeFailureMessage(axiosError(400))).toBe(GUEST_PREFERENCES_COPY.codeWrong)
  })

  it("maps other verify failures to the check message", () => {
    expect(codeFailureMessage(new Error("network"))).toBe(GUEST_PREFERENCES_COPY.codeFailed)
  })
})

describe("submitContact", () => {
  it("rejects bad input without a request", async () => {
    const h = harness({ initial: { inputValue: "+12025550123" } })

    await submitContact(h.env)

    expect(h.starts).toEqual([])
    expect(h.state().step).toBe("input")
    expect(h.state().error).toBe(GUEST_PREFERENCES_COPY.contactInvalid)
  })

  it("email entry sends the trimmed address and moves to the email step", async () => {
    const h = harness({ initial: { inputValue: "  sophie@example.com " } })

    await submitContact(h.env)

    expect(h.starts).toEqual(["sophie@example.com"])
    expect(h.state()).toMatchObject({
      step: "email-sent",
      contact: "sophie@example.com",
      sentAt: 1_000_000,
      busy: false,
      error: null,
    })
  })

  it("mobile entry moves to the code step", async () => {
    const h = harness({ initial: { inputValue: "07700 900123" } })

    await submitContact(h.env)

    expect(h.starts).toEqual(["07700 900123"])
    expect(h.state().step).toBe("mobile-code")
  })

  it("keeps the input step and shows the request message when send fails", async () => {
    const h = harness({
      initial: { inputValue: "sophie@example.com" },
      startError: axiosError(500),
    })

    await submitContact(h.env)

    expect(h.state()).toMatchObject({
      step: "input",
      busy: false,
      error: GUEST_PREFERENCES_COPY.requestFailed,
    })
  })
})

describe("verifyCode", () => {
  const codeStep = { step: "mobile-code" as const, contact: "07700 900123" }

  it("does not call the API until six digits are entered", async () => {
    const h = harness({ initial: codeStep })

    await verifyCode(h.env, "12345")

    expect(h.verifies).toEqual([])
  })

  it("six digits call verifyCode with the contact and code, then mark verified", async () => {
    const h = harness({ initial: codeStep })

    await verifyCode(h.env, "123456")

    expect(h.verifies).toEqual([{ contact: "07700 900123", code: "123456" }])
    expect(h.verifiedCount()).toBe(1)
    expect(h.state().step).toBe("verified")
  })

  it("a 400 shows the wrong code message and stays on the code step", async () => {
    const h = harness({ initial: codeStep, verifyError: axiosError(400) })

    await verifyCode(h.env, "123456")

    expect(h.verifiedCount()).toBe(0)
    expect(h.state()).toMatchObject({
      step: "mobile-code",
      busy: false,
      error: GUEST_PREFERENCES_COPY.codeWrong,
    })
  })

  it("a session expiry on the add-contact path reports the session as expired", async () => {
    const h = harness({
      initial: codeStep,
      verifyError: new GuestPreferencesSessionExpiredError(),
    })

    await verifyCode(h.env, "123456")

    expect(h.expiredCount()).toBe(1)
    expect(h.verifiedCount()).toBe(0)
  })
})

describe("resendCode", () => {
  it("is blocked for 60 seconds after the last send", async () => {
    const h = harness({
      initial: { step: "email-sent", contact: "sophie@example.com", sentAt: 1_000_000 },
    })
    h.setNow(1_030_000)

    await resendCode(h.env)

    expect(h.starts).toEqual([])
  })

  it("after 60 seconds sends again to the same contact and restarts the cooldown", async () => {
    const h = harness({
      initial: { step: "mobile-code", contact: "07700 900123", sentAt: 1_000_000 },
    })
    h.setNow(1_060_000)

    await resendCode(h.env)

    expect(h.starts).toEqual(["07700 900123"])
    expect(h.state()).toMatchObject({ step: "mobile-code", sentAt: 1_060_000 })
  })
})

describe("contactFlowReducer", () => {
  it("typing clears the error", () => {
    const next = contactFlowReducer(
      { ...initialContactFlowState, error: "old" },
      { type: "input-changed", value: "a" }
    )
    expect(next).toMatchObject({ inputValue: "a", error: null })
  })

  it("change-contact returns to input with the contact restored", () => {
    const next = contactFlowReducer(
      { ...initialContactFlowState, step: "email-sent", contact: "sophie@example.com", sentAt: 5 },
      { type: "change-contact" }
    )
    expect(next).toMatchObject({
      step: "input",
      inputValue: "sophie@example.com",
      contact: "",
      sentAt: null,
    })
  })
})
