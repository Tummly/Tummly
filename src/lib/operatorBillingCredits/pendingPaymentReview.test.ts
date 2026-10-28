import { describe, expect, it } from "vitest"

import type { CustomPlanQuoteResult } from "@/api/adminCommercialCatalogApi"
import {
  pendingPaymentErrorCode,
  pendingPaymentReviewCode,
  pendingPaymentSavedChoices,
  reviewedConfigJsonFromQuote,
} from "@/lib/operatorBillingCredits/pendingPaymentReview"

const pending = (over: Record<string, unknown> = {}) => ({
  plan: "Custom",
  cadence: "monthly",
  failedAtUtc: null,
  failureCode: "quote_changed",
  basePlan: "growth",
  locations: 3,
  packs: { offer: 2 },
  ...over,
})

describe("pendingPaymentErrorCode", () => {
  it("reads the code from the response body", () => {
    expect(
      pendingPaymentErrorCode({ response: { data: { code: "quote_changed" } } }),
    ).toBe("quote_changed")
  })

  it("reads a code set on the error", () => {
    expect(pendingPaymentErrorCode({ code: "legacy_pack_key" })).toBe(
      "legacy_pack_key",
    )
  })

  it("returns null for anything else", () => {
    expect(pendingPaymentErrorCode(new Error("x"))).toBeNull()
    expect(pendingPaymentErrorCode(null)).toBeNull()
    expect(pendingPaymentErrorCode({ response: { data: {} } })).toBeNull()
  })
})

describe("pendingPaymentReviewCode", () => {
  it("reads the failure code from the status payload", () => {
    expect(pendingPaymentReviewCode(pending(), null)).toBe("quote_changed")
    expect(
      pendingPaymentReviewCode(pending({ failureCode: "legacy_pack_key" }), null),
    ).toBe("legacy_pack_key")
  })

  it("lets a code from the last failed attempt win", () => {
    expect(
      pendingPaymentReviewCode(
        pending({ failureCode: "revolut_http_error" }),
        "quote_changed",
      ),
    ).toBe("quote_changed")
  })

  it("is null for other failures and for no pending payment", () => {
    expect(
      pendingPaymentReviewCode(pending({ failureCode: "revolut_http_error" }), null),
    ).toBeNull()
    expect(pendingPaymentReviewCode(null, null)).toBeNull()
  })
})

describe("pendingPaymentSavedChoices", () => {
  it("returns the saved choices for a fresh quote request", () => {
    expect(pendingPaymentSavedChoices(pending())).toEqual({
      cadence: "monthly",
      basePlan: "growth",
      locations: 3,
      packs: { offer: 2 },
    })
  })

  it("returns null when a saved pack was retired, so the owner chooses again", () => {
    expect(
      pendingPaymentSavedChoices(pending({ packs: { qr_10: 1 } })),
    ).toBeNull()
  })

  it("returns null when the choices are missing or unreadable", () => {
    expect(pendingPaymentSavedChoices(pending({ basePlan: null }))).toBeNull()
    expect(pendingPaymentSavedChoices(pending({ locations: 0 }))).toBeNull()
    expect(pendingPaymentSavedChoices(null)).toBeNull()
  })
})

describe("reviewedConfigJsonFromQuote", () => {
  it("sends the quote choices with the hash of the quote on screen", () => {
    const json = reviewedConfigJsonFromQuote({
      cadence: "annual",
      basePlan: "growth",
      locations: 3,
      packs: { offer: 2 },
      quoteHash: "freshhash",
    } as unknown as CustomPlanQuoteResult)
    expect(JSON.parse(json ?? "{}")).toMatchObject({
      version: 2,
      cadence: "annual",
      basePlan: "growth",
      locations: 3,
      packs: { offer: 2 },
      quoteHash: "freshhash",
    })
  })

  it("returns null for a quote with an unknown base plan", () => {
    expect(
      reviewedConfigJsonFromQuote({
        cadence: "monthly",
        basePlan: "pilot",
        locations: 1,
        packs: {},
        quoteHash: "x",
      } as unknown as CustomPlanQuoteResult),
    ).toBeNull()
  })
})
