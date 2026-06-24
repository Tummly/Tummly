import { describe, expect, it } from "vitest"

import {
  formatPhoneForDisplay,
  tryNormalizePhoneToE164,
} from "@/lib/phoneNumber"

const ukMobileE164 = "+447911123456"

describe("tryNormalizePhoneToE164", () => {
  it("normalizes common UK formats", () => {
    expect(tryNormalizePhoneToE164("07911123456")).toBe(ukMobileE164)
    expect(tryNormalizePhoneToE164("+44 7911 123 456")).toBe(ukMobileE164)
    expect(tryNormalizePhoneToE164("447911123456")).toBe(ukMobileE164)
    expect(tryNormalizePhoneToE164("0044 7911 123456")).toBe(ukMobileE164)
  })

  it("accepts international numbers with a country code", () => {
    expect(tryNormalizePhoneToE164("+923156878896")).toBe("+923156878896")
  })

  it("rejects ambiguous non-UK local numbers", () => {
    expect(tryNormalizePhoneToE164("03156878896")).toBeNull()
    expect(tryNormalizePhoneToE164("07700900123")).toBeNull()
  })
})

describe("formatPhoneForDisplay", () => {
  it("formats UK E.164 numbers nationally", () => {
    expect(formatPhoneForDisplay(ukMobileE164)).toBe("07911 123456")
  })
})
