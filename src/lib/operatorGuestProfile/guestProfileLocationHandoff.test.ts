import { describe, expect, it } from "vitest"

import {
  guestProfileHandoffHasIntent,
  readGuestProfileLocationHandoff,
} from "./guestProfileLocationHandoff"

describe("readGuestProfileLocationHandoff", () => {
  it("returns empty handoff for nullish or non-object state", () => {
    expect(readGuestProfileLocationHandoff(null)).toEqual({})
    expect(readGuestProfileLocationHandoff(undefined)).toEqual({})
    expect(readGuestProfileLocationHandoff("activity")).toEqual({})
  })

  it("reads a valid Activity tab and openFeedbackId", () => {
    expect(
      readGuestProfileLocationHandoff({
        tab: "activity",
        openFeedbackId: 99,
      })
    ).toEqual({
      tab: "activity",
      openFeedbackId: 99,
    })
  })

  it("ignores invalid tab and non-positive feedback ids", () => {
    expect(
      readGuestProfileLocationHandoff({
        tab: "not-a-tab",
        openFeedbackId: 0,
      })
    ).toEqual({})
  })

  it("reports whether handoff carries intent", () => {
    expect(guestProfileHandoffHasIntent({})).toBe(false)
    expect(guestProfileHandoffHasIntent({ tab: "activity" })).toBe(true)
    expect(guestProfileHandoffHasIntent({ openFeedbackId: 1 })).toBe(true)
  })
})
