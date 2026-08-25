import { describe, expect, it } from "vitest"

import {
  formatAccessActivityCopy,
  formatAccessActivityOccurredAt,
} from "@/lib/operatorTeamPermissions/teamPermissionsPresentation"

describe("formatAccessActivityCopy", () => {
  it("uses possessive without gendered pronouns", () => {
    expect(
      formatAccessActivityCopy({
        kind: "role-changed",
        actorDisplayName: "James Cole",
        targetDisplayName: "Amira Khan",
        fromValue: "Staff",
        toValue: "Marketing",
      })
    ).toBe("James Cole changed Amira Khan's role from Staff to Marketing.")
  })

  it("maps permission-cell area ids to SideNav labels", () => {
    expect(
      formatAccessActivityCopy({
        kind: "permission-cell-changed",
        actorDisplayName: "Alex Owner",
        targetDisplayName: null,
        fromValue: "billing-credits:View",
        toValue: "billing-credits:Manage",
      })
    ).toBe(
      "Alex Owner changed Admin permission for Billing & credits from View to Manage."
    )
  })

  it("formats invitation-sent with role and location scope", () => {
    expect(
      formatAccessActivityCopy({
        kind: "invitation-sent",
        actorDisplayName: "Alex Owner",
        targetDisplayName: "Sam Staff",
        fromValue: "Staff",
        toValue: "Camden only",
      })
    ).toBe("Alex Owner invited Sam Staff as Staff (Camden only).")
  })
})

describe("formatAccessActivityOccurredAt", () => {
  it("uses Today, Yesterday, and calendar day in Europe/London", () => {
    const now = new Date("2026-08-26T12:00:00.000Z")
    expect(
      formatAccessActivityOccurredAt("2026-08-26T09:42:00.000Z", now)
    ).toBe("Today, 10:42")
    expect(
      formatAccessActivityOccurredAt("2026-08-25T15:05:00.000Z", now)
    ).toBe("Yesterday, 16:05")
    expect(
      formatAccessActivityOccurredAt("2026-08-23T15:05:00.000Z", now)
    ).toBe("23 Aug 2026, 16:05")
  })
})
