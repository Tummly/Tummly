import { describe, expect, it } from "vitest"

import {
  isOperatorDashboardPath,
  isStaffDashboardPath,
  isThemedAppShellPath,
  resolveOperatorAppearanceDocumentTheme,
} from "./operatorAppearance"

describe("isOperatorDashboardPath", () => {
  it("is true for Operator dashboard home paths", () => {
    expect(isOperatorDashboardPath("/single-dashboard")).toBe(true)
    expect(isOperatorDashboardPath("/multi-dashboard")).toBe(true)
  })

  it("is true for nested paths under Operator dashboard roots", () => {
    expect(isOperatorDashboardPath("/single-dashboard/guests")).toBe(true)
    expect(isOperatorDashboardPath("/multi-dashboard/guests")).toBe(true)
    expect(isOperatorDashboardPath("/single-dashboard/settings")).toBe(true)
  })

  it("is false for Home, auth, admin, prototype, and other product surfaces", () => {
    expect(isOperatorDashboardPath("/")).toBe(false)
    expect(isOperatorDashboardPath("/login")).toBe(false)
    expect(isOperatorDashboardPath("/admin-dashboard")).toBe(false)
    expect(isOperatorDashboardPath("/help-center")).toBe(false)
    expect(isOperatorDashboardPath("/single-dashboardfoo")).toBe(false)
    expect(isOperatorDashboardPath("/multi-dashboardfoo")).toBe(false)
    expect(isOperatorDashboardPath("/prototype")).toBe(false)
    expect(isOperatorDashboardPath("/prototype/formatted-live-answer")).toBe(
      false
    )
  })
})

describe("isStaffDashboardPath", () => {
  it("is true for the staff shell and nested sections", () => {
    expect(isStaffDashboardPath("/admin-dashboard")).toBe(true)
    expect(isStaffDashboardPath("/admin-dashboard/admin")).toBe(true)
    expect(isStaffDashboardPath("/admin-dashboard/support")).toBe(true)
    expect(isStaffDashboardPath("/admin-dashboard/support/queries/1")).toBe(
      true
    )
  })

  it("is false outside the staff shell", () => {
    expect(isStaffDashboardPath("/admin-dashboardfoo")).toBe(false)
    expect(isStaffDashboardPath("/support-dashboard")).toBe(false)
    expect(isStaffDashboardPath("/single-dashboard")).toBe(false)
  })
})

describe("isThemedAppShellPath", () => {
  it("is true for Operator and staff shells", () => {
    expect(isThemedAppShellPath("/single-dashboard")).toBe(true)
    expect(isThemedAppShellPath("/admin-dashboard/support")).toBe(true)
  })

  it("is false on marketing surfaces", () => {
    expect(isThemedAppShellPath("/")).toBe(false)
    expect(isThemedAppShellPath("/help-center")).toBe(false)
  })
})

describe("resolveOperatorAppearanceDocumentTheme", () => {
  it("is always light when theme is disabled", () => {
    expect(
      resolveOperatorAppearanceDocumentTheme({
        themeEnabled: false,
        preference: "dark",
        systemPrefersDark: true,
      })
    ).toBe("light")
    expect(
      resolveOperatorAppearanceDocumentTheme({
        themeEnabled: false,
        preference: "system",
        systemPrefersDark: true,
      })
    ).toBe("light")
  })

  it("honors Light, Dark, and System when theme is enabled", () => {
    expect(
      resolveOperatorAppearanceDocumentTheme({
        themeEnabled: true,
        preference: "light",
        systemPrefersDark: true,
      })
    ).toBe("light")
    expect(
      resolveOperatorAppearanceDocumentTheme({
        themeEnabled: true,
        preference: "dark",
        systemPrefersDark: false,
      })
    ).toBe("dark")
    expect(
      resolveOperatorAppearanceDocumentTheme({
        themeEnabled: true,
        preference: "system",
        systemPrefersDark: true,
      })
    ).toBe("dark")
    expect(
      resolveOperatorAppearanceDocumentTheme({
        themeEnabled: true,
        preference: "system",
        systemPrefersDark: false,
      })
    ).toBe("light")
  })
})
