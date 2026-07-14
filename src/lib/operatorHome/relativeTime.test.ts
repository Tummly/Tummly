import { describe, expect, it } from "vitest"

import { formatRelativeTime } from "./relativeTime"

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
})
