import { describe, expect, it } from "vitest"

import {
  getPasswordStrengthBarColor,
  getPasswordStrengthLabel,
  getPasswordStrengthScore,
  isPasswordAtLeastGood,
  PASSWORD_STRENGTH_BAR_COUNT,
} from "./passwordStrength"

describe("getPasswordStrengthScore", () => {
  it("returns 0 for an empty password", () => {
    expect(getPasswordStrengthScore("")).toBe(0)
  })

  it("returns 1 when fewer than 8 characters", () => {
    expect(getPasswordStrengthScore("pass")).toBe(1)
  })

  it("returns 2 when 8+ chars but missing uppercase", () => {
    expect(getPasswordStrengthScore("password1")).toBe(2)
  })

  it("returns 2 when 8+ chars but missing number or symbol", () => {
    expect(getPasswordStrengthScore("Password")).toBe(2)
  })

  it("returns 3 for Good — 8+ chars, uppercase, and a digit", () => {
    expect(getPasswordStrengthScore("Password1")).toBe(3)
  })

  it("returns 4 for Strong — 10+ chars with Good mix", () => {
    expect(getPasswordStrengthScore("Password1!")).toBe(4)
  })

  it("returns 5 for Excellent — 12+ chars with uppercase, number, and symbol", () => {
    expect(getPasswordStrengthScore("Password123!")).toBe(5)
  })

  it("stays at Strong when 12+ but missing number or symbol", () => {
    expect(getPasswordStrengthScore("PasswordLong!")).toBe(4)
  })
})

describe("isPasswordAtLeastGood", () => {
  it("accepts Good and above", () => {
    expect(isPasswordAtLeastGood("Password1")).toBe(true)
    expect(isPasswordAtLeastGood("Password1!")).toBe(true)
    expect(isPasswordAtLeastGood("Password123!")).toBe(true)
  })

  it("rejects Weak and Very weak passwords", () => {
    expect(isPasswordAtLeastGood("pass")).toBe(false)
    expect(isPasswordAtLeastGood("password1")).toBe(false)
    expect(isPasswordAtLeastGood("Password")).toBe(false)
  })
})

describe("getPasswordStrengthLabel", () => {
  it("maps scores to labels", () => {
    expect(getPasswordStrengthLabel(1)).toBe("Very weak")
    expect(getPasswordStrengthLabel(2)).toBe("Weak")
    expect(getPasswordStrengthLabel(3)).toBe("Good")
    expect(getPasswordStrengthLabel(4)).toBe("Strong")
    expect(getPasswordStrengthLabel(5)).toBe("Excellent")
    expect(getPasswordStrengthLabel(0)).toBeNull()
  })
})

describe("getPasswordStrengthBarColor", () => {
  it("uses inactive grey for unfilled bars", () => {
    expect(getPasswordStrengthBarColor(2, 1)).toBe("#D2D2D2")
  })

  it("uses red for a very weak score", () => {
    expect(getPasswordStrengthBarColor(0, 1)).toBe("#EF4444")
  })

  it("uses orange for a weak score", () => {
    expect(getPasswordStrengthBarColor(1, 2)).toBe("#F59E0B")
  })

  it("uses green for good, strong, and excellent scores", () => {
    expect(getPasswordStrengthBarColor(2, 3)).toBe("#22C55E")
    expect(getPasswordStrengthBarColor(3, 4)).toBe("#22C55E")
    expect(getPasswordStrengthBarColor(4, 5)).toBe("#22C55E")
  })

  it("exposes five bars to match the Guest Loop design", () => {
    expect(PASSWORD_STRENGTH_BAR_COUNT).toBe(5)
  })
})
