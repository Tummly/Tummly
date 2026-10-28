import { describe, expect, it } from "vitest"

import { CUSTOM_PLAN_QUOTE_COPY } from "@/content/marketing/customPlanQuoteCopy"
import {
  isQuoteReviewCode,
  quoteReviewMessage,
} from "@/lib/marketingCustomPlan/quoteReview"

describe("quote review codes", () => {
  it("names the two codes that need a fresh review", () => {
    expect(isQuoteReviewCode("quote_changed")).toBe(true)
    expect(isQuoteReviewCode("legacy_pack_key")).toBe(true)
    expect(isQuoteReviewCode("revolut_http_error")).toBe(false)
    expect(isQuoteReviewCode(null)).toBe(false)
    expect(isQuoteReviewCode(undefined)).toBe(false)
  })

  it("uses the approved copy for each code", () => {
    expect(quoteReviewMessage("quote_changed")).toBe(CUSTOM_PLAN_QUOTE_COPY.changed)
    expect(quoteReviewMessage("legacy_pack_key")).toBe(
      "This option is no longer available. Review the current capacity options and confirm your updated total before continuing.",
    )
    expect(quoteReviewMessage("revolut_http_error")).toBeNull()
  })
})
