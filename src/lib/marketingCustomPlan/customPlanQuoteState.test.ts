import { describe, expect, it } from "vitest"

import type { CustomPlanQuoteResult } from "@/api/adminCommercialCatalogApi"
import { CUSTOM_PLAN_QUOTE_COPY } from "@/content/marketing/customPlanQuoteCopy"
import {
  checkLocationInput,
  checkPackCount,
  formatPenceGbp,
  packCountMessage,
  parseTypedCount,
  quoteHashChanged,
  quoteReducer,
  quoteView,
  type QuoteState,
} from "@/lib/marketingCustomPlan/customPlanQuoteState"

function quote(
  partial: Partial<CustomPlanQuoteResult> = {},
): CustomPlanQuoteResult {
  return {
    version: 2,
    catalogPublicId: "catalog-1",
    cadence: "monthly",
    basePlan: "starter",
    locations: 1,
    packs: {},
    quantities: {
      locations: 1,
      teamUsers: 1,
      qrPlacements: 1,
      qrPlacementsAccountExtra: 0,
      offers: 1,
      emailCredits: 0,
      smsCredits: 0,
      aiCredits: 0,
    },
    monthlyNetPence: 3900,
    periodNetPence: 3900,
    lines: [],
    basePlanChanged: false,
    requestedBasePlan: null,
    storedAsPlan: "Custom",
    quoteHash: "a".repeat(32),
    vatRateBps: 0,
    ...partial,
  }
}

const IDLE: QuoteState = { status: "idle" }

describe("checkLocationInput", () => {
  it("classifies the Location count", () => {
    expect(checkLocationInput(null)).toBe("empty")
    expect(checkLocationInput(Number.NaN)).toBe("empty")
    expect(checkLocationInput(0)).toBe("empty")
    expect(checkLocationInput(2.5)).toBe("not_integer")
    expect(checkLocationInput(501)).toBe("over_limit")
    expect(checkLocationInput(500)).toBe("ok")
    expect(checkLocationInput(1)).toBe("ok")
  })
})

describe("parseTypedCount", () => {
  it("reads the typed text as typed, keeping a decimal point", () => {
    expect(parseTypedCount("2.5")).toBe(2.5)
    expect(parseTypedCount(" 12 ")).toBe(12)
    expect(parseTypedCount("0")).toBe(0)
    expect(parseTypedCount("007")).toBe(7)
  })

  it("reads anything that is not a plain number as no number", () => {
    expect(parseTypedCount("")).toBeNull()
    expect(parseTypedCount("   ")).toBeNull()
    expect(parseTypedCount("abc")).toBeNull()
    expect(parseTypedCount("-3")).toBeNull()
    expect(parseTypedCount("0x1F")).toBeNull()
    expect(parseTypedCount("1e3")).toBeNull()
  })
})

describe("checkPackCount", () => {
  it("classifies a pack count without clamping it", () => {
    expect(checkPackCount(null)).toBe("empty")
    expect(checkPackCount(2.5)).toBe("not_integer")
    expect(checkPackCount(-1)).toBe("not_integer")
    expect(checkPackCount(100)).toBe("over_limit")
    expect(checkPackCount(99)).toBe("ok")
    expect(checkPackCount(0)).toBe("ok")
  })

  it("maps each invalid pack count to its copy and no copy when valid", () => {
    expect(packCountMessage("empty")).toBe(
      "Enter a number of packs. Use 0 for none.",
    )
    expect(packCountMessage("not_integer")).toBe(
      "Enter a whole number of packs.",
    )
    expect(packCountMessage("over_limit")).toBe("Maximum 99 packs per option.")
    expect(packCountMessage("ok")).toBeNull()
  })
})

describe("quoteReducer", () => {
  it("ignores a stale response and keeps the newest request", () => {
    const q1 = quote({ locations: 1 })
    const q2 = quote({ locations: 2 })

    let state: QuoteState = quoteReducer(IDLE, { type: "request", requestId: 1 })
    state = quoteReducer(state, { type: "request", requestId: 2 })
    state = quoteReducer(state, {
      type: "success",
      requestId: 1,
      quote: q1,
    })
    expect(state).toEqual({ status: "loading", requestId: 2, last: null })

    state = quoteReducer(state, {
      type: "success",
      requestId: 2,
      quote: q2,
    })
    expect(state).toEqual({ status: "ready", quote: q2 })
  })

  it("ignores a stale failure", () => {
    let state: QuoteState = quoteReducer(IDLE, { type: "request", requestId: 1 })
    state = quoteReducer(state, { type: "request", requestId: 2 })
    state = quoteReducer(state, {
      type: "failure",
      requestId: 1,
      code: null,
    })
    expect(state).toEqual({ status: "loading", requestId: 2, last: null })
  })

  it("keeps the last ready quote while loading", () => {
    const q1 = quote()
    let state: QuoteState = quoteReducer(IDLE, { type: "request", requestId: 1 })
    state = quoteReducer(state, { type: "success", requestId: 1, quote: q1 })
    state = quoteReducer(state, { type: "request", requestId: 2 })
    expect(state).toEqual({ status: "loading", requestId: 2, last: q1 })
  })
})

describe("quoteView", () => {
  it("shows the loading copy and blocks Continue while loading", () => {
    const view = quoteView({
      check: "ok",
      state: { status: "loading", requestId: 1, last: null },
      changed: false,
    })
    expect(view.message).toBe("Updating your quote…")
    expect(view.continueEnabled).toBe(false)
    expect(view.showRetry).toBe(false)
  })

  it("shows the failed copy with retry and blocks Continue", () => {
    const view = quoteView({
      check: "ok",
      state: { status: "failed", last: null, code: null },
      changed: false,
    })
    expect(view.message).toBe(
      "We could not load a current quote. Your selections are saved. Try again.",
    )
    expect(view.showRetry).toBe(true)
    expect(view.continueEnabled).toBe(false)
  })

  it("enables Continue when the quote is ready", () => {
    const view = quoteView({
      check: "ok",
      state: { status: "ready", quote: quote() },
      changed: false,
    })
    expect(view.continueEnabled).toBe(true)
    expect(view.message).toBeNull()
  })

  it("shows the changed copy and blocks Continue until the new price is confirmed", () => {
    const view = quoteView({
      check: "ok",
      state: { status: "ready", quote: quote() },
      changed: true,
    })
    expect(view.message).toBe(
      "Your quote has changed. Review the updated total before continuing.",
    )
    expect(view.continueEnabled).toBe(false)
  })

  it("enables Continue on a changed quote once the customer confirms it", () => {
    const view = quoteView({
      check: "ok",
      state: { status: "ready", quote: quote() },
      changed: true,
      priceConfirmed: true,
    })
    expect(view.message).toBe(
      "Your quote has changed. Review the updated total before continuing.",
    )
    expect(view.continueEnabled).toBe(true)
  })

  it("does not block Continue on confirmation when the price did not change", () => {
    const view = quoteView({
      check: "ok",
      state: { status: "ready", quote: quote() },
      changed: false,
      priceConfirmed: false,
    })
    expect(view.continueEnabled).toBe(true)
  })

  it("shows the whole-number copy for not_integer", () => {
    const view = quoteView({
      check: "not_integer",
      state: IDLE,
      changed: false,
    })
    expect(view.message).toBe("Enter a whole number of Locations.")
    expect(view.continueEnabled).toBe(false)
  })

  it("shows the over-limit copy and the contact option for over_limit", () => {
    const view = quoteView({
      check: "over_limit",
      state: IDLE,
      changed: false,
    })
    expect(view.message).toBe("Maximum online plan size is 500 Locations")
    expect(view.showContact).toBe(true)
    expect(view.continueEnabled).toBe(false)
  })

  it("shows the over-limit copy for a contact-sales failure code", () => {
    const view = quoteView({
      check: "ok",
      state: { status: "failed", last: null, code: "custom_plan_contact_sales" },
      changed: false,
    })
    expect(view.message).toBe("Maximum online plan size is 500 Locations")
    expect(view.showContact).toBe(true)
    expect(view.showRetry).toBe(false)
  })

  it("shows the whole-number copy for an invalid_location_count failure code", () => {
    const view = quoteView({
      check: "ok",
      state: { status: "failed", last: null, code: "invalid_location_count" },
      changed: false,
    })
    expect(view.message).toBe("Enter a whole number of Locations.")
    expect(view.showRetry).toBe(false)
  })

  it("shows the add-a-Location copy when the count is empty", () => {
    const view = quoteView({
      check: "empty",
      state: IDLE,
      changed: false,
    })
    expect(view.message).toBe(
      "Add at least 1 Location to customise your estimate.",
    )
    expect(view.continueEnabled).toBe(false)
  })

  it("returns the tax text when the quote carries VAT", () => {
    const view = quoteView({
      check: "ok",
      state: { status: "ready", quote: quote({ vatRateBps: 2000 }) },
      changed: false,
    })
    expect(view.taxText).toBe("Prices exclude VAT. VAT is added at checkout.")
  })

  it("returns no tax text when the VAT rate is zero", () => {
    const view = quoteView({
      check: "ok",
      state: { status: "ready", quote: quote({ vatRateBps: 0 }) },
      changed: false,
    })
    expect(view.taxText).toBeNull()
  })

  it("exposes the doc 08 copy in one object", () => {
    expect(CUSTOM_PLAN_QUOTE_COPY.contactUsHref).toBe(
      "/help-center/contact?topic=plans-and-pricing",
    )
    expect(CUSTOM_PLAN_QUOTE_COPY.contactUs).toBe("Contact us")
    expect(CUSTOM_PLAN_QUOTE_COPY.retry).toBe("Try again")
  })
})

describe("formatPenceGbp", () => {
  it("groups thousands and keeps pence only when needed", () => {
    expect(formatPenceGbp(101_000)).toBe("£1,010")
    expect(formatPenceGbp(3900)).toBe("£39")
    expect(formatPenceGbp(1250)).toBe("£12.50")
  })
})

describe("quoteHashChanged", () => {
  it("is true when a non-empty saved hash differs from the ready hash", () => {
    expect(quoteHashChanged("a".repeat(32), "b".repeat(32))).toBe(true)
  })

  it("is false when the hashes match", () => {
    expect(quoteHashChanged("a".repeat(32), "a".repeat(32))).toBe(false)
  })

  it("is false when no hash was saved", () => {
    expect(quoteHashChanged(undefined, "b".repeat(32))).toBe(false)
    expect(quoteHashChanged("", "b".repeat(32))).toBe(false)
  })
})
