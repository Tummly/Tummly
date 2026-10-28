import { describe, expect, it } from "vitest"

import { formatLondonDateTime, londonDayBoundaryUtcIso } from "./londonTime"

describe("formatLondonDateTime", () => {
  it("shows summer time (BST) in Europe/London", () => {
    expect(formatLondonDateTime("2026-07-01T11:30:00Z")).toBe("01 Jul 2026, 12:30")
  })

  it("shows winter time (GMT) in Europe/London", () => {
    expect(formatLondonDateTime("2026-12-01T11:30:00Z")).toBe("01 Dec 2026, 11:30")
  })

  it("returns the input unchanged when it is not a date", () => {
    expect(formatLondonDateTime("not-a-date")).toBe("not-a-date")
  })
})

describe("londonDayBoundaryUtcIso", () => {
  it("starts a summer London day one hour before UTC midnight", () => {
    expect(londonDayBoundaryUtcIso("2026-10-01", "start")).toBe("2026-09-30T23:00:00.000Z")
  })

  it("starts a winter London day at UTC midnight", () => {
    expect(londonDayBoundaryUtcIso("2026-12-01", "start")).toBe("2026-12-01T00:00:00.000Z")
  })

  it("ends a London day at 23:59:59.999 local time", () => {
    expect(londonDayBoundaryUtcIso("2026-10-01", "end")).toBe("2026-10-01T22:59:59.999Z")
    expect(londonDayBoundaryUtcIso("2026-12-01", "end")).toBe("2026-12-01T23:59:59.999Z")
  })

  it("rejects malformed and impossible dates", () => {
    expect(londonDayBoundaryUtcIso("2026-1-1", "start")).toBeNull()
    expect(londonDayBoundaryUtcIso("2026-02-31", "start")).toBeNull()
    expect(londonDayBoundaryUtcIso("", "end")).toBeNull()
  })
})
