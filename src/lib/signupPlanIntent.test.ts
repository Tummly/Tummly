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

  it("clears stale intent when signup URL has no plan", () => {
    saveSignupPlanIntent({ plan: "Pilot" })
    expect(
      captureSignupPlanIntentFromSearch(new URLSearchParams())
    ).toBeNull()
    expect(readSignupPlanIntent()).toBeNull()
  })
})
