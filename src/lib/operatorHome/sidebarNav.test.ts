import { describe, expect, it } from "vitest"

import {
  OPERATOR_SIDEBAR_PRIMARY_NAV,
  OPERATOR_SIDEBAR_SETTINGS_CHILDREN,
  OPERATOR_SIDEBAR_SHOP,
  getOperatorSidebarNav,
  isSettingsChildId,
  resolveSettingsDisclosureOpen,
} from "./sidebarNav"

describe("OPERATOR_SIDEBAR_PRIMARY_NAV", () => {
  it("lists Figma Operator Dashboard primary nav order and labels", () => {
    expect(OPERATOR_SIDEBAR_PRIMARY_NAV.map((item) => item.id)).toEqual([
      "home",
      "guests",
      "capture",
      "feedback",
      "campaigns",
      "offers",
      "reports",
    ])
    expect(OPERATOR_SIDEBAR_PRIMARY_NAV.map((item) => item.label)).toEqual([
      "Home",
      "Guests",
      "Capture",
      "Feedback",
      "Campaigns",
      "Offers",
      "Reports",
    ])
  })
})

describe("OPERATOR_SIDEBAR_SETTINGS_CHILDREN", () => {
  it("lists Settings nav group children in Figma order", () => {
    expect(OPERATOR_SIDEBAR_SETTINGS_CHILDREN.map((item) => item.id)).toEqual([
      "account-workspace",
      "locations",
      "team-permissions",
      "billing-credits",
      "privacy-consent",
      "brand-guest-form",
    ])
    expect(OPERATOR_SIDEBAR_SETTINGS_CHILDREN.map((item) => item.label)).toEqual(
      [
        "Account & workspace",
        "Locations",
        "Team & permissions",
        "Billing & credits",
        "Privacy & consent",
        "Brand & guest form",
      ]
    )
  })
})

describe("OPERATOR_SIDEBAR_SHOP", () => {
  it("exposes presentational Tummly Shop footer chrome", () => {
    expect(OPERATOR_SIDEBAR_SHOP).toEqual({
      id: "tummly-shop",
      label: "Tummly Shop",
    })
  })
})

describe("getOperatorSidebarNav", () => {
  it("makes Home, Guests, Capture, and Feedback navigable and marks Home active on Home", () => {
    const nav = getOperatorSidebarNav("home", { mode: "single", locationId: 10 })

    expect(nav.primary.find((item) => item.id === "home")).toMatchObject({
      label: "Home",
      navigable: true,
      active: true,
      to: "/single-dashboard?location=10",
    })
    expect(nav.primary.find((item) => item.id === "guests")).toMatchObject({
      label: "Guests",
      navigable: true,
      active: false,
      to: "/single-dashboard/guests?location=10",
    })
    expect(nav.primary.find((item) => item.id === "capture")).toMatchObject({
      label: "Capture",
      navigable: true,
      active: false,
      to: "/single-dashboard/capture?location=10",
    })
    expect(nav.primary.find((item) => item.id === "feedback")).toMatchObject({
      label: "Feedback",
      navigable: true,
      active: false,
      to: "/single-dashboard/feedback?location=10",
    })

    for (const item of nav.primary.filter(
      (entry) =>
        entry.id !== "home"
        && entry.id !== "guests"
        && entry.id !== "capture"
        && entry.id !== "feedback"
    )) {
      expect(item.navigable).toBe(false)
      expect(item.active).toBe(false)
      expect(item.to).toBeUndefined()
    }
  })

  it("marks Guests active on Guests routes", () => {
    const nav = getOperatorSidebarNav("guests", { mode: "multi", locationId: 3 })

    expect(nav.primary.find((item) => item.id === "guests")).toMatchObject({
      active: true,
      to: "/multi-dashboard/guests?location=3",
    })
    expect(nav.primary.find((item) => item.id === "home")).toMatchObject({
      active: false,
      to: "/multi-dashboard?location=3",
    })
  })

  it("marks Capture active on Capture routes", () => {
    const nav = getOperatorSidebarNav("capture", {
      mode: "multi",
      locationId: 3,
    })

    expect(nav.primary.find((item) => item.id === "capture")).toMatchObject({
      active: true,
      to: "/multi-dashboard/capture?location=3",
    })
    expect(nav.primary.find((item) => item.id === "home")).toMatchObject({
      active: false,
      to: "/multi-dashboard?location=3",
    })
    expect(nav.primary.find((item) => item.id === "guests")).toMatchObject({
      active: false,
      to: "/multi-dashboard/guests?location=3",
    })
  })

  it("marks Feedback active on Feedback routes", () => {
    const nav = getOperatorSidebarNav("feedback", {
      mode: "single",
      locationId: 10,
    })

    expect(nav.primary.find((item) => item.id === "feedback")).toMatchObject({
      active: true,
      navigable: true,
      to: "/single-dashboard/feedback?location=10",
    })
    expect(nav.primary.find((item) => item.id === "home")).toMatchObject({
      active: false,
    })
  })

  it("models Settings as a non-navigable disclosure group, never active", () => {
    const nav = getOperatorSidebarNav("home")

    expect(nav.settings).toMatchObject({
      id: "settings",
      label: "Settings",
      navigable: false,
      active: false,
      forceExpanded: false,
    })
    expect(nav.settings.children).toHaveLength(6)
    for (const child of nav.settings.children) {
      expect(child.navigable).toBe(false)
      expect(child.active).toBe(false)
    }
  })

  it("models Tummly Shop as non-navigable footer chrome", () => {
    const nav = getOperatorSidebarNav("home")

    expect(nav.footer).toEqual([
      {
        id: "tummly-shop",
        label: "Tummly Shop",
        navigable: false,
        active: false,
      },
    ])
  })

  it("forces Settings disclosure open when a Settings child is active", () => {
    const nav = getOperatorSidebarNav("locations")

    expect(nav.settings.forceExpanded).toBe(true)
    expect(nav.settings.children.find((c) => c.id === "locations")).toMatchObject(
      {
        active: true,
        navigable: false,
      }
    )
    expect(nav.primary.every((item) => item.active === false)).toBe(true)
  })
})

describe("isSettingsChildId", () => {
  it("recognizes Settings child ids only", () => {
    expect(isSettingsChildId("locations")).toBe(true)
    expect(isSettingsChildId("home")).toBe(false)
    expect(isSettingsChildId("tummly-shop")).toBe(false)
  })
})

describe("resolveSettingsDisclosureOpen", () => {
  it("defaults open from persistence and forces open when a child is active", () => {
    expect(resolveSettingsDisclosureOpen(true, false)).toBe(true)
    expect(resolveSettingsDisclosureOpen(false, false)).toBe(false)
    expect(resolveSettingsDisclosureOpen(false, true)).toBe(true)
    expect(resolveSettingsDisclosureOpen(true, true)).toBe(true)
  })
})
