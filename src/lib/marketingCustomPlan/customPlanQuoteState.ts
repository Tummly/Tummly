import type { CustomPlanQuoteResult } from "@/api/adminCommercialCatalogApi"
import { CUSTOM_PLAN_QUOTE_COPY } from "@/content/marketing/customPlanQuoteCopy"
import { CUSTOM_PLAN_MAX_LOCATIONS } from "@/lib/marketingCustomPlan/customPlanEstimate"

export type LocationInputCheck = "empty" | "not_integer" | "over_limit" | "ok"

/** Classifies the typed Location count before any quote request. */
export function checkLocationInput(value: number | null): LocationInputCheck {
  if (value == null || Number.isNaN(value) || value < 1) {
    return "empty"
  }
  if (!Number.isInteger(value)) {
    return "not_integer"
  }
  if (value > CUSTOM_PLAN_MAX_LOCATIONS) {
    return "over_limit"
  }
  return "ok"
}

export type QuoteState =
  | { status: "idle" }
  | {
      status: "loading"
      requestId: number
      last: CustomPlanQuoteResult | null
    }
  | { status: "ready"; quote: CustomPlanQuoteResult }
  | {
      status: "failed"
      last: CustomPlanQuoteResult | null
      code: string | null
    }

export type QuoteAction =
  | { type: "request"; requestId: number }
  | {
      type: "success"
      requestId: number
      quote: CustomPlanQuoteResult
    }
  | { type: "failure"; requestId: number; code: string | null }

function lastQuote(state: QuoteState): CustomPlanQuoteResult | null {
  switch (state.status) {
    case "ready":
      return state.quote
    case "loading":
    case "failed":
      return state.last
    case "idle":
      return null
  }
}

/** Responses whose requestId is not the current loading id are ignored. */
export function quoteReducer(state: QuoteState, action: QuoteAction): QuoteState {
  switch (action.type) {
    case "request":
      return {
        status: "loading",
        requestId: action.requestId,
        last: lastQuote(state),
      }
    case "success":
      if (state.status !== "loading" || state.requestId !== action.requestId) {
        return state
      }
      return { status: "ready", quote: action.quote }
    case "failure":
      if (state.status !== "loading" || state.requestId !== action.requestId) {
        return state
      }
      return { status: "failed", last: state.last, code: action.code }
  }
}

export type QuoteView = {
  message: string | null
  showRetry: boolean
  showContact: boolean
  continueEnabled: boolean
  taxText: string | null
}

function checkMessage(check: LocationInputCheck): string | null {
  switch (check) {
    case "empty":
      return CUSTOM_PLAN_QUOTE_COPY.locationEmpty
    case "not_integer":
      return CUSTOM_PLAN_QUOTE_COPY.locationNotWhole
    case "over_limit":
      return CUSTOM_PLAN_QUOTE_COPY.locationOverLimit
    case "ok":
      return null
  }
}

/** Copy and control state for the quote panel. Continue needs a ready quote. */
export function quoteView(input: {
  check: LocationInputCheck
  state: QuoteState
  changed: boolean
}): QuoteView {
  const { check, state, changed } = input
  const shown = lastQuote(state)
  const taxText =
    shown != null && shown.vatRateBps > 0
      ? CUSTOM_PLAN_QUOTE_COPY.taxExcluded
      : null
  const base = { showRetry: false, showContact: false, taxText }

  if (check !== "ok") {
    return {
      ...base,
      message: checkMessage(check),
      showContact: check === "over_limit",
      continueEnabled: false,
    }
  }

  switch (state.status) {
    case "idle":
      return { ...base, message: null, continueEnabled: false }
    case "loading":
      return {
        ...base,
        message: CUSTOM_PLAN_QUOTE_COPY.loading,
        continueEnabled: false,
      }
    case "failed":
      if (state.code === "custom_plan_contact_sales") {
        return {
          ...base,
          message: CUSTOM_PLAN_QUOTE_COPY.locationOverLimit,
          showContact: true,
          continueEnabled: false,
        }
      }
      if (state.code === "invalid_location_count") {
        return {
          ...base,
          message: CUSTOM_PLAN_QUOTE_COPY.locationNotWhole,
          continueEnabled: false,
        }
      }
      return {
        ...base,
        message: CUSTOM_PLAN_QUOTE_COPY.failed,
        showRetry: true,
        continueEnabled: false,
      }
    case "ready":
      return {
        ...base,
        message: changed ? CUSTOM_PLAN_QUOTE_COPY.changed : null,
        continueEnabled: true,
      }
  }
}

/** £1,010 / £12.50 — thousands grouped; pence shown only when not whole. */
export function formatPenceGbp(pence: number): string {
  const whole = pence % 100 === 0
  return `£${(pence / 100).toLocaleString("en-GB", {
    minimumFractionDigits: whole ? 0 : 2,
    maximumFractionDigits: whole ? 0 : 2,
  })}`
}
