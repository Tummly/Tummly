import { describe, expect, it } from "vitest"

import {
  operatorDashboardNavPath,
  operatorDashboardRootPath,
  resolveOperatorSidebarActiveId,
} from "./operatorDashboardPaths"

describe("operatorDashboardRootPath", () => {
  it("maps dashboard mode to the root path", () => {
    expect(operatorDashboardRootPath("single")).toBe("/single-dashboard")
    expect(operatorDashboardRootPath("multi")).toBe("/multi-dashboard")
  })
})

describe("operatorDashboardNavPath", () => {
  it("builds Home and Guests paths with location query preserved", () => {
    expect(operatorDashboardNavPath("single", "home", 42)).toBe(
      "/single-dashboard?location=42"
    )
    expect(operatorDashboardNavPath("single", "guests", 42)).toBe(
      "/single-dashboard/guests?location=42"
    )
    expect(operatorDashboardNavPath("multi", "home", 7)).toBe(
      "/multi-dashboard?location=7"
    )
    expect(operatorDashboardNavPath("multi", "guests", 7)).toBe(
      "/multi-dashboard/guests?location=7"
    )
  })
})

describe("resolveOperatorSidebarActiveId", () => {
  it("marks Guests active on nested Guests routes", () => {
    expect(resolveOperatorSidebarActiveId("/single-dashboard/guests")).toBe(
      "guests"
    )
    expect(resolveOperatorSidebarActiveId("/multi-dashboard/guests")).toBe(
      "guests"
    )
  })

  it("marks Home active on dashboard root routes", () => {
    expect(resolveOperatorSidebarActiveId("/single-dashboard")).toBe("home")
    expect(resolveOperatorSidebarActiveId("/multi-dashboard")).toBe("home")
  })
})
