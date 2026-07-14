import { describe, expect, it } from "vitest"

import {
  OPERATOR_SIDEBAR_NAV,
  getOperatorSidebarNav,
} from "./sidebarNav"

describe("OPERATOR_SIDEBAR_NAV", () => {
  it("lists Figma Operator Dashboard nav order and labels", () => {
    expect(OPERATOR_SIDEBAR_NAV.map((item) => item.id)).toEqual([
      "home",
      "guests",
      "capture",
      "feedback",
      "campaigns",
      "offers",
      "reports",
      "settings",
    ])
    expect(OPERATOR_SIDEBAR_NAV.map((item) => item.label)).toEqual([
      "Home",
      "Guests",
      "Capture",
      "Feedback",
      "Campaigns",
      "Offers",
      "Reports",
      "Settings",
    ])
  })
})

describe("getOperatorSidebarNav", () => {
  it("makes only Home navigable and marks it active on Home", () => {
    const nav = getOperatorSidebarNav("home")

    expect(nav.find((item) => item.id === "home")).toMatchObject({
      label: "Home",
      navigable: true,
      active: true,
    })

    for (const item of nav.filter((entry) => entry.id !== "home")) {
      expect(item.navigable).toBe(false)
      expect(item.active).toBe(false)
    }
  })
})
