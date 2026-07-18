import { describe, expect, it } from "vitest"

import { formatRelativeTime, parseApiInstantMs } from "./relativeTime"

describe("parseApiInstantMs", () => {
  it("treats timezone-less API datetimes as UTC", () => {
    expect(parseApiInstantMs("2026-07-12T12:00:00")).toBe(
      Date.parse("2026-07-12T12:00:00.000Z")
    )
    expect(parseApiInstantMs("2026-07-12T12:00:00.000")).toBe(
      Date.parse("2026-07-12T12:00:00.000Z")
    )
  })

  it("preserves explicit UTC and offset timestamps", () => {
    expect(parseApiInstantMs("2026-07-12T12:00:00.000Z")).toBe(
      Date.parse("2026-07-12T12:00:00.000Z")
    )
    expect(parseApiInstantMs("2026-07-12T17:00:00+05:00")).toBe(
      Date.parse("2026-07-12T12:00:00.000Z")
    )
  })
})

describe("formatRelativeTime", () => {
  const now = Date.parse("2026-07-12T12:00:00.000Z")

  it("formats minutes ago", () => {
    expect(
      formatRelativeTime("2026-07-12T11:48:00.000Z", now)
    ).toBe("12 minutes ago")
  })

  it("formats hours and days", () => {
    expect(formatRelativeTime("2026-07-12T10:00:00.000Z", now)).toBe(
      "2 hours ago"
    )
    expect(formatRelativeTime("2026-07-10T12:00:00.000Z", now)).toBe(
      "2 days ago"
    )
  })

  it("does not skew timezone-less UTC timestamps by local offset", () => {
    expect(formatRelativeTime("2026-07-12T11:55:00", now)).toBe(
      "5 minutes ago"
    )
  })
})
