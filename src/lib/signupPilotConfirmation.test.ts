import { describe, expect, it } from "vitest"

import { buildPilotSetupConfirmation } from "@/lib/signupPilotConfirmation"

describe("buildPilotSetupConfirmation", () => {
  it("states the start and end dates for a Pilot", () => {
    expect(
      buildPilotSetupConfirmation({
        subscriptionPlan: "Pilot",
        pilotStartedAtUtc: "2026-10-26T09:30:00Z",
        pilotEndsAtUtc: "2026-11-25T09:30:00Z",
      })
    ).toBe("Your 30-day Pilot started on 26 October 2026 and ends on 25 November 2026.")
  })

  it("returns null for a paid plan", () => {
    expect(
      buildPilotSetupConfirmation({
        subscriptionPlan: "Growth",
        pilotStartedAtUtc: "2026-10-26T09:30:00Z",
        pilotEndsAtUtc: "2026-11-25T09:30:00Z",
      })
    ).toBeNull()
  })

  it("returns null when a date is missing or invalid", () => {
    expect(
      buildPilotSetupConfirmation({
        subscriptionPlan: "Pilot",
        pilotStartedAtUtc: null,
        pilotEndsAtUtc: "2026-11-25T09:30:00Z",
      })
    ).toBeNull()
    expect(
      buildPilotSetupConfirmation({
        subscriptionPlan: "Pilot",
        pilotStartedAtUtc: "not a date",
        pilotEndsAtUtc: "2026-11-25T09:30:00Z",
      })
    ).toBeNull()
    expect(buildPilotSetupConfirmation(null)).toBeNull()
  })
})
