import { describe, expect, it } from "vitest"

import {
  buildOperatorSidebarAreaAccess,
  isDeniedOperatorSidebarActiveId,
  resolveHiddenOperatorSidebarNavIds,
} from "@/lib/operatorHome/operatorAreaChromeAccess"

describe("buildOperatorSidebarAreaAccess", () => {
  it("maps workspace chrome fields onto SideNav ids", () => {
    expect(
      buildOperatorSidebarAreaAccess({
        guestsAccess: "none",
        captureAccess: "none",
        feedbackAccess: "none",
        campaignsAccess: "none",
        offersAccess: "view",
        reportsAccess: "none",
        accountWorkspaceAccess: "view",
        locationsAccess: "view",
        teamPermissionsAccess: "none",
        billingCreditsAccess: "none",
        privacyConsentAccess: "none",
        tummlyShopAccess: "none",
      })
    ).toEqual({
      guests: "none",
      capture: "none",
      feedback: "none",
      campaigns: "none",
      offers: "view",
      reports: "none",
      "account-workspace": "view",
      locations: "view",
      "team-permissions": "none",
      "billing-credits": "none",
      "privacy-consent": "none",
      "tummly-shop": "none",
    })
  })
})

describe("resolveHiddenOperatorSidebarNavIds", () => {
  it("hides nothing when access is omitted (owner chrome during rollout)", () => {
    expect(resolveHiddenOperatorSidebarNavIds({})).toEqual([])
  })

  it("hides only Areas with explicit none — Staff-shaped payload", () => {
    expect(
      resolveHiddenOperatorSidebarNavIds({
        guests: "none",
        capture: "none",
        feedback: "none",
        campaigns: "none",
        offers: "view",
        reports: "none",
        "account-workspace": "view",
        locations: "view",
        "team-permissions": "none",
        "billing-credits": "none",
        "privacy-consent": "none",
        "tummly-shop": "none",
      })
    ).toEqual([
      "guests",
      "capture",
      "feedback",
      "campaigns",
      "reports",
      "team-permissions",
      "billing-credits",
      "privacy-consent",
      "tummly-shop",
    ])
  })
})

describe("isDeniedOperatorSidebarActiveId", () => {
  it("never denies Home", () => {
    expect(
      isDeniedOperatorSidebarActiveId("home", { guests: "none" })
    ).toBe(false)
  })

  it("denies Guests when guests access is none", () => {
    expect(
      isDeniedOperatorSidebarActiveId("guests", { guests: "none" })
    ).toBe(true)
  })

  it("allows Offers when access is view (Staff redeem)", () => {
    expect(
      isDeniedOperatorSidebarActiveId("offers", { offers: "view" })
    ).toBe(false)
  })

  it("allows Guests when access is omitted", () => {
    expect(isDeniedOperatorSidebarActiveId("guests", {})).toBe(false)
  })
})
