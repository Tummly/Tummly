import { describe, expect, it } from "vitest"

import {
  guestProfileHeaderActionPaths,
  operatorDashboardCampaignsPathWithOffer,
  operatorDashboardCaptureLocationPath,
  operatorDashboardCaptureReportPath,
  operatorDashboardFeedbackReportPath,
  operatorDashboardGuestEditPath,
  operatorDashboardGuestProfilePath,
  operatorDashboardModeForAccountType,
  operatorDashboardNavPath,
  operatorDashboardCampaignDetailsPath,
  operatorDashboardCampaignPreviewPath,
  operatorDashboardOfferDetailsPath,
  operatorDashboardOfferPreviewPath,
  operatorDashboardOffersRedemptionLogPath,
  operatorDashboardRootPath,
  resolveMismatchedOperatorDashboardRedirect,
  resolveOperatorSidebarActiveId,
} from "./operatorDashboardPaths"

describe("operatorDashboardRootPath", () => {
  it("maps dashboard mode to the root path", () => {
    expect(operatorDashboardRootPath("single")).toBe("/single-dashboard")
    expect(operatorDashboardRootPath("multi")).toBe("/multi-dashboard")
  })
})

describe("operatorDashboardModeForAccountType", () => {
  it("maps Single and Multi account types to dashboard modes", () => {
    expect(operatorDashboardModeForAccountType("Single")).toBe("single")
    expect(operatorDashboardModeForAccountType("Multi")).toBe("multi")
  })

  it("returns null for unknown or missing account types", () => {
    expect(operatorDashboardModeForAccountType(null)).toBeNull()
    expect(operatorDashboardModeForAccountType(undefined)).toBeNull()
    expect(operatorDashboardModeForAccountType("Owner")).toBeNull()
  })
})

describe("resolveMismatchedOperatorDashboardRedirect", () => {
  it("blocks a Multi operator from opening single-dashboard via URL", () => {
    expect(
      resolveMismatchedOperatorDashboardRedirect({
        mode: "single",
        accountType: "Multi",
        pathname: "/single-dashboard",
        search: "?location=7",
      })
    ).toBe("/multi-dashboard?location=7")
  })

  it("preserves nested path when remapping Multi away from single-dashboard", () => {
    expect(
      resolveMismatchedOperatorDashboardRedirect({
        mode: "single",
        accountType: "Multi",
        pathname: "/single-dashboard/guests",
        search: "?location=7",
      })
    ).toBe("/multi-dashboard/guests?location=7")
  })

  it("blocks a Single operator from opening multi-dashboard via URL", () => {
    expect(
      resolveMismatchedOperatorDashboardRedirect({
        mode: "multi",
        accountType: "Single",
        pathname: "/multi-dashboard/capture",
        search: "?location=42",
      })
    ).toBe("/single-dashboard/capture?location=42")
  })

  it("allows matching AccountType and mode", () => {
    expect(
      resolveMismatchedOperatorDashboardRedirect({
        mode: "multi",
        accountType: "Multi",
        pathname: "/multi-dashboard",
      })
    ).toBeNull()
    expect(
      resolveMismatchedOperatorDashboardRedirect({
        mode: "single",
        accountType: "Single",
        pathname: "/single-dashboard/guests",
      })
    ).toBeNull()
  })

  it("does not redirect when AccountType is unknown", () => {
    expect(
      resolveMismatchedOperatorDashboardRedirect({
        mode: "single",
        accountType: null,
        pathname: "/single-dashboard",
      })
    ).toBeNull()
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

  it("builds Account & workspace settings path with location query", () => {
    expect(
      operatorDashboardNavPath("single", "account-workspace", 42)
    ).toBe("/single-dashboard/settings/account-workspace?location=42")
    expect(
      operatorDashboardNavPath("multi", "account-workspace", 7)
    ).toBe("/multi-dashboard/settings/account-workspace?location=7")
  })

  it("builds Team & permissions settings path with location query", () => {
    expect(
      operatorDashboardNavPath("single", "team-permissions", 42)
    ).toBe("/single-dashboard/settings/team-permissions?location=42")
    expect(
      operatorDashboardNavPath("multi", "team-permissions", 7)
    ).toBe("/multi-dashboard/settings/team-permissions?location=7")
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

describe("operatorDashboardOfferDetailsPath", () => {
  it("builds Offer Details paths with location query", () => {
    expect(operatorDashboardOfferDetailsPath("single", 10, 42)).toBe(
      "/single-dashboard/offers/10?location=42"
    )
    expect(operatorDashboardOfferDetailsPath("multi", "7", 3)).toBe(
      "/multi-dashboard/offers/7?location=3"
    )
  })

  it("appends tab query for Void requests deep-link", () => {
    expect(
      operatorDashboardOfferDetailsPath("single", 10, 42, {
        tab: "void-requests",
      })
    ).toBe("/single-dashboard/offers/10?location=42&tab=void-requests")
  })
})

describe("operatorDashboardCampaignsPathWithOffer", () => {
  it("appends offerId for Share offer in a campaign CTA", () => {
    expect(operatorDashboardCampaignsPathWithOffer("single", 42, 10)).toBe(
      "/single-dashboard/campaigns?location=42&offerId=10"
    )
    expect(operatorDashboardCampaignsPathWithOffer("multi", 7, "3")).toBe(
      "/multi-dashboard/campaigns?location=7&offerId=3"
    )
  })
})

describe("operatorDashboardCaptureReportPath", () => {
  it("builds Capture report paths with location query", () => {
    expect(operatorDashboardCaptureReportPath("single", 42)).toBe(
      "/single-dashboard/reports/capture?location=42"
    )
    expect(operatorDashboardCaptureReportPath("multi", 7)).toBe(
      "/multi-dashboard/reports/capture?location=7"
    )
  })
})

describe("operatorDashboardFeedbackReportPath", () => {
  it("builds Feedback report paths with location query", () => {
    expect(operatorDashboardFeedbackReportPath("single", 42)).toBe(
      "/single-dashboard/reports/feedback?location=42"
    )
    expect(operatorDashboardFeedbackReportPath("multi", 7)).toBe(
      "/multi-dashboard/reports/feedback?location=7"
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

  it("marks Reports active on Reports routes", () => {
    expect(resolveOperatorSidebarActiveId("/single-dashboard/reports")).toBe(
      "reports"
    )
    expect(resolveOperatorSidebarActiveId("/multi-dashboard/reports")).toBe(
      "reports"
    )
    expect(
      resolveOperatorSidebarActiveId("/single-dashboard/reports/capture")
    ).toBe("reports")
    expect(
      resolveOperatorSidebarActiveId("/multi-dashboard/reports/capture")
    ).toBe("reports")
    expect(
      resolveOperatorSidebarActiveId("/single-dashboard/reports/feedback")
    ).toBe("reports")
    expect(
      resolveOperatorSidebarActiveId("/multi-dashboard/reports/feedback")
    ).toBe("reports")
  })

  it("marks Offers active on Offers routes", () => {
    expect(resolveOperatorSidebarActiveId("/single-dashboard/offers")).toBe(
      "offers"
    )
    expect(resolveOperatorSidebarActiveId("/multi-dashboard/offers")).toBe(
      "offers"
    )
  })

  it("marks Offers active on Offer Details and redemption log routes", () => {
    expect(
      resolveOperatorSidebarActiveId("/single-dashboard/offers/10")
    ).toBe("offers")
    expect(
      resolveOperatorSidebarActiveId("/multi-dashboard/offers/redemption-log")
    ).toBe("offers")
  })

  it("marks Account & workspace active on settings child routes", () => {
    expect(
      resolveOperatorSidebarActiveId(
        "/single-dashboard/settings/account-workspace"
      )
    ).toBe("account-workspace")
    expect(
      resolveOperatorSidebarActiveId(
        "/multi-dashboard/settings/account-workspace"
      )
    ).toBe("account-workspace")
  })

  it("marks Team & permissions active on settings child routes", () => {
    expect(
      resolveOperatorSidebarActiveId(
        "/single-dashboard/settings/team-permissions"
      )
    ).toBe("team-permissions")
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

describe("operatorDashboardCampaignDetailsPath", () => {
  it("builds thin Campaign detail paths with location query", () => {
    expect(operatorDashboardCampaignDetailsPath("single", 10, 42)).toBe(
      "/single-dashboard/campaigns/10?location=42"
    )
    expect(operatorDashboardCampaignDetailsPath("multi", "7", 3)).toBe(
      "/multi-dashboard/campaigns/7?location=3"
    )
  })
})

describe("operatorDashboardCampaignPreviewPath", () => {
  it("builds campaign Guest Preview paths", () => {
    expect(operatorDashboardCampaignPreviewPath("single", 10, 42)).toBe(
      "/single-dashboard/campaigns/10/preview?location=42"
    )
  })
})

describe("operatorDashboardOfferPreviewPath", () => {
  it("builds offer Guest Preview paths", () => {
    expect(operatorDashboardOfferPreviewPath("single", 10, 42)).toBe(
      "/single-dashboard/offers/10/preview?location=42"
    )
  })
})
