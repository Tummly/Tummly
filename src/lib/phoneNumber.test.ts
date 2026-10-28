import { afterEach, describe, expect, it, vi } from "vitest"

import {
  formatPhoneForDisplay,
  tryNormalizePhoneToE164,
  tryNormalizeUkMobileToE164,
} from "@/lib/phoneNumber"

const ukMobileE164 = "+447911123456"

afterEach(() => {
  vi.unstubAllEnvs()
})

describe("tryNormalizePhoneToE164", () => {
  it("normalizes common UK formats", () => {
    expect(tryNormalizePhoneToE164("07911123456")).toBe(ukMobileE164)
    expect(tryNormalizePhoneToE164("+44 7911 123 456")).toBe(ukMobileE164)
    expect(tryNormalizePhoneToE164("447911123456")).toBe(ukMobileE164)
    expect(tryNormalizePhoneToE164("0044 7911 123456")).toBe(ukMobileE164)
    expect(tryNormalizePhoneToE164("02079460000")).toBe("+442079460000")
  })

  it("rejects international numbers", () => {
    expect(tryNormalizePhoneToE164("+923156878896")).toBeNull()
    expect(tryNormalizePhoneToE164("+14155552671")).toBeNull()
  })

  it("rejects ambiguous non-UK local numbers", () => {
    expect(tryNormalizePhoneToE164("03156878896")).toBeNull()
    expect(tryNormalizePhoneToE164("07700900123")).toBeNull()
  })
})

describe("tryNormalizeUkMobileToE164", () => {
  it("accepts UK mobile numbers in local and international formats", () => {
    expect(tryNormalizeUkMobileToE164("07911 123456")).toBe(ukMobileE164)
    expect(tryNormalizeUkMobileToE164("+44 7911 123456")).toBe(ukMobileE164)
    expect(tryNormalizeUkMobileToE164("  07911123456  ")).toBe(ukMobileE164)
  })

  it("accepts the Ofcom 07700 900xxx range that the shared check rejects", () => {
    expect(tryNormalizeUkMobileToE164("07700 900123")).toBe("+447700900123")
    expect(tryNormalizeUkMobileToE164("+44 7700 900123")).toBe("+447700900123")
    expect(tryNormalizePhoneToE164("07700 900123")).toBeNull()
  })

  it("rejects the Ofcom range outside DEV and QA builds", () => {
    vi.stubEnv("DEV", false)
    vi.stubEnv("VITE_APP_ENV", "production")

    expect(tryNormalizeUkMobileToE164("07700 900123")).toBeNull()
    expect(tryNormalizeUkMobileToE164("+44 7700 900123")).toBeNull()
  })

  it("accepts the Ofcom range on QA builds", () => {
    vi.stubEnv("DEV", false)
    vi.stubEnv("VITE_APP_ENV", "qa")

    expect(tryNormalizeUkMobileToE164("07700 900123")).toBe("+447700900123")
  })

  it("rejects UK landline numbers that the shared check accepts", () => {
    expect(tryNormalizePhoneToE164("020 7946 0000")).toBe("+442079460000")
    expect(tryNormalizeUkMobileToE164("020 7946 0000")).toBeNull()
    expect(tryNormalizeUkMobileToE164("+44 20 7946 0000")).toBeNull()
  })

  it("rejects international and blank values", () => {
    expect(tryNormalizeUkMobileToE164("+14155552671")).toBeNull()
    expect(tryNormalizeUkMobileToE164("   ")).toBeNull()
  })
})

describe("formatPhoneForDisplay", () => {
  it("formats UK E.164 numbers nationally", () => {
    expect(formatPhoneForDisplay(ukMobileE164)).toBe("07911 123456")
  })
})
