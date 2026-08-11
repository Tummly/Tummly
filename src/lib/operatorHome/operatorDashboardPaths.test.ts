import { describe, expect, it } from "vitest"

import {
  guestProfileHeaderActionPaths,
  operatorDashboardCaptureLocationPath,
  operatorDashboardGuestEditPath,
  operatorDashboardGuestProfilePath,
  operatorDashboardNavPath,
  operatorDashboardOffersRedemptionLogPath,
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

  it("builds Capture paths with location query preserved", () => {
    expect(operatorDashboardNavPath("single", "capture", 42)).toBe(
      "/single-dashboard/capture?location=42"
    )
    expect(operatorDashboardNavPath("multi", "capture", 7)).toBe(
      "/multi-dashboard/capture?location=7"
    )
  })

  it("builds Feedback paths with location query preserved", () => {
    expect(operatorDashboardNavPath("single", "feedback", 42)).toBe(
      "/single-dashboard/feedback?location=42"
    )
    expect(operatorDashboardNavPath("multi", "feedback", 7)).toBe(
      "/multi-dashboard/feedback?location=7"
    )
  })

  it("builds Campaigns paths with location query preserved", () => {
    expect(operatorDashboardNavPath("single", "campaigns", 42)).toBe(
      "/single-dashboard/campaigns?location=42"
    )
    expect(operatorDashboardNavPath("multi", "campaigns", 7)).toBe(
      "/multi-dashboard/campaigns?location=7"
    )
  })

  it("builds Offers paths with location query preserved", () => {
    expect(operatorDashboardNavPath("single", "offers", 42)).toBe(
      "/single-dashboard/offers?location=42"
    )
    expect(operatorDashboardNavPath("multi", "offers", 7)).toBe(
      "/multi-dashboard/offers?location=7"
    )
  })
})

describe("operatorDashboardOffersRedemptionLogPath", () => {
  it("builds location-wide redemption log paths with location query", () => {
    expect(operatorDashboardOffersRedemptionLogPath("single", 42)).toBe(
      "/single-dashboard/offers/redemption-log?location=42"
    )
    expect(operatorDashboardOffersRedemptionLogPath("multi", 7)).toBe(
      "/multi-dashboard/offers/redemption-log?location=7"
    )
  })
})

describe("operatorDashboardGuestProfilePath", () => {
  it("builds Guest Profile paths with location query", () => {
    expect(operatorDashboardGuestProfilePath("single", 1842, 42)).toBe(
      "/single-dashboard/guests/1842?location=42"
    )
    expect(operatorDashboardGuestProfilePath("multi", "7", 3)).toBe(
      "/multi-dashboard/guests/7?location=3"
    )
  })
})

describe("operatorDashboardGuestEditPath", () => {
  it("builds Edit guest details paths with location query", () => {
    expect(operatorDashboardGuestEditPath("single", 1842, 42)).toBe(
      "/single-dashboard/guests/1842/edit?location=42"
    )
    expect(operatorDashboardGuestEditPath("multi", "7", 3)).toBe(
      "/multi-dashboard/guests/7/edit?location=3"
    )
  })

  it("appends hash targets for tags and data privacy entry", () => {
    expect(operatorDashboardGuestEditPath("single", 1842, 42, "tags")).toBe(
      "/single-dashboard/guests/1842/edit?location=42#tags"
    )
    expect(
      operatorDashboardGuestEditPath("multi", 7, 3, "data-privacy")
    ).toBe("/multi-dashboard/guests/7/edit?location=3#data-privacy")
  })
})

describe("guestProfileHeaderActionPaths", () => {
  it("wires header Edit / Manage tags / Delete to edit path + hashes", () => {
    expect(guestProfileHeaderActionPaths("single", 1842, 42)).toEqual({
      editGuestDetails: "/single-dashboard/guests/1842/edit?location=42",
      manageTags: "/single-dashboard/guests/1842/edit?location=42#tags",
      deleteGuestData:
        "/single-dashboard/guests/1842/edit?location=42#data-privacy",
    })
    expect(guestProfileHeaderActionPaths("multi", "7", 3)).toEqual({
      editGuestDetails: "/multi-dashboard/guests/7/edit?location=3",
      manageTags: "/multi-dashboard/guests/7/edit?location=3#tags",
      deleteGuestData:
        "/multi-dashboard/guests/7/edit?location=3#data-privacy",
    })
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

  it("marks Guests active on Guest Profile routes", () => {
    expect(
      resolveOperatorSidebarActiveId("/single-dashboard/guests/1842")
    ).toBe("guests")
    expect(resolveOperatorSidebarActiveId("/multi-dashboard/guests/7")).toBe(
      "guests"
    )
  })

  it("marks Guests active on Edit guest details routes", () => {
    expect(
      resolveOperatorSidebarActiveId("/single-dashboard/guests/1842/edit")
    ).toBe("guests")
    expect(
      resolveOperatorSidebarActiveId("/multi-dashboard/guests/7/edit")
    ).toBe("guests")
  })

  it("marks Home active on dashboard root routes", () => {
    expect(resolveOperatorSidebarActiveId("/single-dashboard")).toBe("home")
    expect(resolveOperatorSidebarActiveId("/multi-dashboard")).toBe("home")
  })

  it("marks Capture active on Capture routes", () => {
    expect(resolveOperatorSidebarActiveId("/single-dashboard/capture")).toBe(
      "capture"
    )
    expect(resolveOperatorSidebarActiveId("/multi-dashboard/capture")).toBe(
      "capture"
    )
  })

  it("marks Capture active on multi nested Capture location routes", () => {
    expect(
      resolveOperatorSidebarActiveId(
        "/multi-dashboard/capture/locations/42"
      )
    ).toBe("capture")
  })

  it("marks Feedback active on Feedback routes", () => {
    expect(resolveOperatorSidebarActiveId("/single-dashboard/feedback")).toBe(
      "feedback"
    )
    expect(resolveOperatorSidebarActiveId("/multi-dashboard/feedback")).toBe(
      "feedback"
    )
  })

  it("marks Campaigns active on Campaigns routes", () => {
    expect(resolveOperatorSidebarActiveId("/single-dashboard/campaigns")).toBe(
      "campaigns"
    )
    expect(resolveOperatorSidebarActiveId("/multi-dashboard/campaigns")).toBe(
      "campaigns"
    )
  })

  it("marks Offers active on Offers routes", () => {
    expect(resolveOperatorSidebarActiveId("/single-dashboard/offers")).toBe(
      "offers"
    )
    expect(resolveOperatorSidebarActiveId("/multi-dashboard/offers")).toBe(
      "offers"
    )
  })
})

describe("operatorDashboardCaptureLocationPath", () => {
  it("builds multi nested Capture path with location query", () => {
    expect(operatorDashboardCaptureLocationPath(42)).toBe(
      "/multi-dashboard/capture/locations/42?location=42"
    )
    expect(operatorDashboardCaptureLocationPath(7)).toBe(
      "/multi-dashboard/capture/locations/7?location=7"
    )
  })
})
