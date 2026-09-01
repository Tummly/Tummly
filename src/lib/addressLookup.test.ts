import { describe, expect, it } from "vitest"

import {
  addressPostcodePairsMatch,
  extractTownCityFromAddress,
  isDuplicatePostcodeBlurSnapshot,
  pickBestAddressMatch,
  postcodesMatch,
  resolveTownCity,
  shouldDeferPostcodeBlurLookup,
  shouldReconcileAddress,
  streetLinesOverlap,
} from "@/lib/addressLookup"

describe("addressLookup helpers", () => {
  it("detects overlapping street lines", () => {
    expect(
      streetLinesOverlap("125 High Street, Manchester", "125 High Street")
    ).toBe(true)
  })

  it("matches postcodes regardless of spacing", () => {
    expect(postcodesMatch("M1 4AB", "M14AB")).toBe(true)
  })

  it("picks the closest address candidate for a hint", () => {
    expect(
      pickBestAddressMatch(
        ["10 Downing Street, London", "11 Downing Street, London"],
        "10 Downing Street"
      )
    ).toBe("10 Downing Street, London")
  })

  it("requires reconciliation when street detail does not overlap", () => {
    expect(
      shouldReconcileAddress(
        "Manchester",
        "10 Downing Street, London",
        "SW1A 2AA",
        "SW1A 2AA"
      )
    ).toBe(true)
  })

  it("skips reconciliation when street detail overlaps", () => {
    expect(
      shouldReconcileAddress(
        "125 High Street, Manchester",
        "125 High Street, Manchester",
        "M1 4AB",
        "M1 4AB"
      )
    ).toBe(false)
  })

  it("matches verified address-postcode pairs regardless of spacing", () => {
    expect(
      addressPostcodePairsMatch(
        {
          address: "125 High Street, Manchester",
          postcode: "M1 4AB",
        },
        "125 High Street, Manchester",
        "M14AB"
      )
    ).toBe(true)
  })

  it("defers postcode blur while suggestion or postcode resolve is in flight", () => {
    expect(
      shouldDeferPostcodeBlurLookup({
        isResolvingSuggestion: true,
        isResolvingPostcode: false,
      })
    ).toBe(true)

    expect(
      shouldDeferPostcodeBlurLookup({
        isResolvingSuggestion: false,
        isResolvingPostcode: true,
      })
    ).toBe(true)
  })

  it("detects duplicate postcode blur snapshots", () => {
    expect(
      isDuplicatePostcodeBlurSnapshot(
        {
          address: "125 High Street, Manchester",
          postcode: "M1 4AB",
        },
        "125 High Street, Manchester",
        "M1 4AB"
      )
    ).toBe(true)

    expect(
      isDuplicatePostcodeBlurSnapshot(
        {
          address: "125 High Street, Manchester",
          postcode: "M1 4AB",
        },
        "127 High Street, Manchester",
        "M1 4AB"
      )
    ).toBe(false)
  })

  it("prefers Ideal Postcodes post_town over the last address segment", () => {
    expect(
      resolveTownCity({
        postTown: "Manchester",
        address: "125 High Street, Salford",
      })
    ).toBe("Manchester")
  })

  it("falls back to the last address segment when post_town is empty", () => {
    expect(extractTownCityFromAddress("125 High Street, Manchester")).toBe(
      "Manchester"
    )
    expect(resolveTownCity({ address: "125 High Street, Manchester" })).toBe(
      "Manchester"
    )
  })
})
