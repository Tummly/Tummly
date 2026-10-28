import { describe, expect, it } from "vitest"

import {
  normalisePoundsInput,
  penceToPoundsInput,
  poundsInputToPence,
} from "./adminShopCatalogPricing"

describe("penceToPoundsInput", () => {
  it("shows pence as pounds with two decimals", () => {
    expect(penceToPoundsInput(999)).toBe("9.99")
    expect(penceToPoundsInput(299)).toBe("2.99")
    expect(penceToPoundsInput(1500)).toBe("15.00")
    expect(penceToPoundsInput(5)).toBe("0.05")
    expect(penceToPoundsInput(0)).toBe("0.00")
  })
})

describe("poundsInputToPence", () => {
  it("converts pound text to whole pence without float drift", () => {
    expect(poundsInputToPence("9.99")).toBe(999)
    expect(poundsInputToPence("0.1")).toBe(10)
    expect(poundsInputToPence("19.90")).toBe(1990)
    expect(poundsInputToPence("12")).toBe(1200)
    expect(poundsInputToPence(".5")).toBe(50)
    expect(poundsInputToPence("0")).toBe(0)
  })

  it("accepts a leading pound sign and surrounding spaces", () => {
    expect(poundsInputToPence(" £9.99 ")).toBe(999)
  })

  it("rejects empty, negative, and over-precise input", () => {
    expect(poundsInputToPence("")).toBeNull()
    expect(poundsInputToPence("   ")).toBeNull()
    expect(poundsInputToPence("£")).toBeNull()
    expect(poundsInputToPence("-1")).toBeNull()
    expect(poundsInputToPence("1.234")).toBeNull()
    expect(poundsInputToPence("abc")).toBeNull()
    expect(poundsInputToPence("1.2.3")).toBeNull()
  })

  it("round-trips with penceToPoundsInput", () => {
    for (const pence of [0, 1, 99, 100, 299, 999, 123456]) {
      expect(poundsInputToPence(penceToPoundsInput(pence))).toBe(pence)
    }
  })
})

describe("normalisePoundsInput", () => {
  it("pads a valid amount to two decimals on blur", () => {
    expect(normalisePoundsInput("9.9")).toBe("9.90")
    expect(normalisePoundsInput("9")).toBe("9.00")
    expect(normalisePoundsInput(" £2.5 ")).toBe("2.50")
    expect(normalisePoundsInput(".5")).toBe("0.50")
    expect(normalisePoundsInput("9.99")).toBe("9.99")
  })

  it("leaves text that cannot be parsed unchanged", () => {
    expect(normalisePoundsInput("")).toBe("")
    expect(normalisePoundsInput("abc")).toBe("abc")
    expect(normalisePoundsInput("1.234")).toBe("1.234")
    expect(normalisePoundsInput("-1")).toBe("-1")
  })
})
