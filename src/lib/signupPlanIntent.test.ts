import { describe, expect, it, beforeEach } from "vitest"

import {
  buildSignupPath,
  captureSignupPlanIntentFromSearch,
  clearSignupPlanIntent,
  parseSignupPlanIntent,
  readSignupPlanIntent,
  saveSignupPlanIntent,
} from "@/lib/signupPlanIntent"

describe("buildSignupPath", () => {
  it("returns plain signup when no intent", () => {
    expect(buildSignupPath()).toBe("/signup")
    expect(buildSignupPath(null)).toBe("/signup")
  })

  it("builds Pilot path without cadence", () => {
    expect(buildSignupPath({ plan: "Pilot" })).toBe("/signup?plan=Pilot")
  })

  it("builds paid path with cadence", () => {
    expect(
      buildSignupPath({ plan: "Starter", cadence: "annual" })
    ).toBe("/signup?plan=Starter&cadence=annual")
  })

  it("appends email for marketing CTA handoff", () => {
    expect(
      buildSignupPath({ plan: "Pilot" }, { email: " ops@example.com " })
    ).toBe("/signup?plan=Pilot&email=ops%40example.com")
    expect(buildSignupPath(null, { email: "ops@example.com" })).toBe(
      "/signup?email=ops%40example.com"
    )
  })
})

describe("parseSignupPlanIntent", () => {
  it("returns null when plan missing", () => {
    expect(parseSignupPlanIntent(new URLSearchParams())).toBeNull()
  })

  it("parses Pilot", () => {
    expect(
      parseSignupPlanIntent(new URLSearchParams("plan=Pilot"))
    ).toEqual({ plan: "Pilot" })
  })

  it("defaults paid cadence to monthly", () => {
    expect(
      parseSignupPlanIntent(new URLSearchParams("plan=Growth"))
    ).toEqual({ plan: "Growth", cadence: "monthly" })
  })

  it("captures Custom plan customIntent payload", () => {
    expect(
      parseSignupPlanIntent(
        new URLSearchParams("plan=Custom&cadence=monthly&customIntent=abc123")
      )
    ).toEqual({
      plan: "Custom",
      cadence: "monthly",
      customConfigJson: "abc123",
    })
  })
})

describe("signupPlanIntent session", () => {
  beforeEach(() => {
    clearSignupPlanIntent()
  })

  it("round-trips intent in sessionStorage", () => {
    saveSignupPlanIntent({ plan: "Group", cadence: "annual" })
    expect(readSignupPlanIntent()).toEqual({
      plan: "Group",
      cadence: "annual",
    })
  })

  it("round-trips Custom config through sessionStorage", () => {
    saveSignupPlanIntent({
      plan: "Custom",
      cadence: "monthly",
      customConfigJson: "eyJ0ZXN0IjoxfQ==",
    })
    expect(readSignupPlanIntent()).toEqual({
      plan: "Custom",
      cadence: "monthly",
      customConfigJson: "eyJ0ZXN0IjoxfQ==",
    })
  })

  it("clears stale intent when signup URL has no plan", () => {
    saveSignupPlanIntent({ plan: "Pilot" })
    expect(
      captureSignupPlanIntentFromSearch(new URLSearchParams())
    ).toBeNull()
    expect(readSignupPlanIntent()).toBeNull()
  })
})
