import { beforeEach, describe, expect, it, vi } from "vitest"

const toastWarning = vi.fn()

vi.mock("sonner", () => ({
  toast: {
    warning: (...args: unknown[]) => toastWarning(...args),
  },
}))

import {
  EMAIL_DISPATCH_DEFAULT_WARNING,
  parseEmailDispatchMeta,
  warnIfEmailDispatchFailed,
} from "@/lib/emailDispatch"

describe("parseEmailDispatchMeta", () => {
  it("returns empty meta when emailDispatched is absent", () => {
    expect(parseEmailDispatchMeta({})).toEqual({})
  })

  it("parses failed dispatch with warning", () => {
    expect(
      parseEmailDispatchMeta({
        emailDispatched: false,
        emailWarning: "Saved, but the notification email could not be sent.",
      })
    ).toEqual({
      emailDispatched: false,
      emailWarning: "Saved, but the notification email could not be sent.",
    })
  })
})

describe("warnIfEmailDispatchFailed", () => {
  beforeEach(() => {
    toastWarning.mockClear()
  })

  it("does nothing when email succeeded", () => {
    expect(warnIfEmailDispatchFailed({ emailDispatched: true })).toBe(true)
    expect(toastWarning).not.toHaveBeenCalled()
  })

  it("toasts when email failed", () => {
    expect(
      warnIfEmailDispatchFailed({
        emailDispatched: false,
        emailWarning: "custom warning",
      })
    ).toBe(false)
    expect(toastWarning).toHaveBeenCalledWith("custom warning")
  })

  it("falls back to the default warning", () => {
    expect(warnIfEmailDispatchFailed({ emailDispatched: false })).toBe(false)
    expect(toastWarning).toHaveBeenCalledWith(EMAIL_DISPATCH_DEFAULT_WARNING)
  })
})
