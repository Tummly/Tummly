import { CUSTOM_PLAN_QUOTE_COPY } from "@/content/marketing/customPlanQuoteCopy"

/**
 * Server codes for a saved Custom quote that cannot be charged: the price moved
 * (`quote_changed`) or a capacity option was retired (`legacy_pack_key`). The
 * customer reviews a fresh server quote; no charge starts without it.
 */
export type QuoteReviewCode = "quote_changed" | "legacy_pack_key"

export function isQuoteReviewCode(
  code: string | null | undefined,
): code is QuoteReviewCode {
  return code === "quote_changed" || code === "legacy_pack_key"
}

/** Approved copy for a quote review code; null for any other code. */
export function quoteReviewMessage(
  code: string | null | undefined,
): string | null {
  switch (code) {
    case "quote_changed":
      return CUSTOM_PLAN_QUOTE_COPY.changed
    case "legacy_pack_key":
      return CUSTOM_PLAN_QUOTE_COPY.optionUnavailable
    default:
      return null
  }
}
