import { describe, expect, it } from "vitest"

import {
  isOperatorDashboardPath,
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

  it("is false for Home, auth, admin, and other product surfaces", () => {
    expect(isOperatorDashboardPath("/")).toBe(false)
    expect(isOperatorDashboardPath("/login")).toBe(false)
    expect(isOperatorDashboardPath("/admin-dashboard")).toBe(false)
    expect(isOperatorDashboardPath("/help-center")).toBe(false)
    expect(isOperatorDashboardPath("/single-dashboardfoo")).toBe(false)
    expect(isOperatorDashboardPath("/multi-dashboardfoo")).toBe(false)
  })
})

describe("resolveOperatorAppearanceDocumentTheme", () => {
  it("is always light outside the Operator dashboard", () => {
    expect(
      resolveOperatorAppearanceDocumentTheme({
        isOperatorDashboard: false,
        preference: "dark",
        systemPrefersDark: true,
      })
    ).toBe("light")
    expect(
      resolveOperatorAppearanceDocumentTheme({
        isOperatorDashboard: false,
        preference: "system",
        systemPrefersDark: true,
      })
    ).toBe("light")
  })

  it("honors Light, Dark, and System inside the Operator dashboard", () => {
    expect(
      resolveOperatorAppearanceDocumentTheme({
        isOperatorDashboard: true,
        preference: "light",
        systemPrefersDark: true,
      })
    ).toBe("light")
    expect(
      resolveOperatorAppearanceDocumentTheme({
        isOperatorDashboard: true,
        preference: "dark",
        systemPrefersDark: false,
      })
    ).toBe("dark")
    expect(
      resolveOperatorAppearanceDocumentTheme({
        isOperatorDashboard: true,
        preference: "system",
        systemPrefersDark: true,
      })
    ).toBe("dark")
    expect(
      resolveOperatorAppearanceDocumentTheme({
        isOperatorDashboard: true,
        preference: "system",
        systemPrefersDark: false,
      })
    ).toBe("light")
  })
})
