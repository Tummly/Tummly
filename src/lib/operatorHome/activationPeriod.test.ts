import { describe, expect, it } from "vitest"

import {
  activationPeriodBadgeTone,
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

describe("activationPeriodBadgeTone", () => {
  it("uses default when more than 15 days remain", () => {
    expect(activationPeriodBadgeTone(16)).toBe("default")
    expect(activationPeriodBadgeTone(30)).toBe("default")
  })

  it("uses warning when 15 to 6 days remain", () => {
    expect(activationPeriodBadgeTone(15)).toBe("warning")
    expect(activationPeriodBadgeTone(6)).toBe("warning")
  })

  it("uses urgent when 5 or fewer days remain", () => {
    expect(activationPeriodBadgeTone(5)).toBe("urgent")
    expect(activationPeriodBadgeTone(1)).toBe("urgent")
  })
})

describe("formatActivationPeriodBadge", () => {
  it("builds free-trial countdown with UK end date and tone", () => {
    expect(
      formatActivationPeriodBadge(14, "2026-07-26T12:00:00.000Z")
    ).toEqual({
      remaining: "14 days left",
      endsOn: "26 Jul 2026",
      tone: "warning",
    })
  })

  it("uses singular day when one day remains", () => {
    expect(
      formatActivationPeriodBadge(1, "2026-07-13T12:00:00.000Z")
    ).toEqual({
      remaining: "1 day left",
      endsOn: "13 Jul 2026",
      tone: "urgent",
    })
  })

  it("uses default tone when more than 15 days remain", () => {
    expect(
      formatActivationPeriodBadge(30, "2026-08-13T12:00:00.000Z")
    ).toEqual({
      remaining: "30 days left",
      endsOn: "13 Aug 2026",
      tone: "default",
    })
  })

  it("returns null when days remaining are unknown (hide badge)", () => {
    expect(
      formatActivationPeriodBadge(null, "2026-08-13T12:00:00.000Z")
    ).toBeNull()
  })

  it("returns null when Activation expiry cannot be parsed for the end date", () => {
    expect(formatActivationPeriodBadge(14, "not-a-date")).toBeNull()
  })
})
