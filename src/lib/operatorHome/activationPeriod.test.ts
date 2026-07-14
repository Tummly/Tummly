import { describe, expect, it } from "vitest"

import {
  computeActivationDaysRemaining,
  formatActivationPeriodBadge,
} from "./activationPeriod"

describe("computeActivationDaysRemaining", () => {
  it("returns whole days left until Activation expiry (ceil)", () => {
    const now = new Date("2026-07-12T12:00:00.000Z")
    const expiresAt = "2026-07-26T12:00:00.000Z"

    expect(computeActivationDaysRemaining(expiresAt, now)).toBe(14)
  })

  it("returns null when Activation expiry is missing", () => {
    expect(
      computeActivationDaysRemaining(null, new Date("2026-07-12T12:00:00.000Z"))
    ).toBeNull()
  })

  it("returns null when Activation period has already ended", () => {
    const now = new Date("2026-07-12T12:00:00.000Z")
    const expiresAt = "2026-07-10T12:00:00.000Z"

    expect(computeActivationDaysRemaining(expiresAt, now)).toBeNull()
  })
})

describe("formatActivationPeriodBadge", () => {
  it("splits Advanced trial title from remaining days", () => {
    expect(formatActivationPeriodBadge(14)).toEqual({
      title: "Advanced trial",
      remaining: "14 days left",
    })
  })

  it("uses singular day when one day remains", () => {
    expect(formatActivationPeriodBadge(1)).toEqual({
      title: "Advanced trial",
      remaining: "1 day left",
    })
  })

  it("returns null when days remaining are unknown (hide badge)", () => {
    expect(formatActivationPeriodBadge(null)).toBeNull()
  })
})
