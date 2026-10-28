import type { CustomPlanQuoteResult } from "@/api/adminCommercialCatalogApi"
import { CUSTOM_PLAN_QUOTE_COPY } from "@/content/marketing/customPlanQuoteCopy"
import {
  CUSTOM_PLAN_MAX_LOCATIONS,
  CUSTOM_PLAN_MAX_PACK_COUNT,
} from "@/lib/marketingCustomPlan/customPlanEstimate"

export type LocationInputCheck = "empty" | "not_integer" | "over_limit" | "ok"

/**
 * Reads a typed count as typed: digits with an optional decimal point.
 * Anything else is no number, so the caller reports it as empty.
 */
export function parseTypedCount(raw: string): number | null {
  const trimmed = raw.trim()
  if (!/^\d+(\.\d+)?$/.test(trimmed)) {
    return null
  }
  return Number(trimmed)
}

export type PackCountCheck = "empty" | "not_integer" | "over_limit" | "ok"

/** Classifies a typed pack count before any quote request. 0 is a valid count. */
export function checkPackCount(value: number | null | undefined): PackCountCheck {
  if (value == null || Number.isNaN(value)) {
    return "empty"
  }
  if (!Number.isInteger(value) || value < 0) {
    return "not_integer"
  }
  if (value > CUSTOM_PLAN_MAX_PACK_COUNT) {
    return "over_limit"
  }
  return "ok"
}

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

/** The quote to show: the ready quote, or the last one while a new one loads or fails. */
export function lastQuote(state: QuoteState): CustomPlanQuoteResult | null {
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

/** Copy under a pack stepper while its typed count is invalid. */
export function packCountMessage(check: PackCountCheck): string | null {
  switch (check) {
    case "empty":
      return CUSTOM_PLAN_QUOTE_COPY.packEmpty
    case "not_integer":
      return CUSTOM_PLAN_QUOTE_COPY.packNotWhole
    case "over_limit":
      return CUSTOM_PLAN_QUOTE_COPY.packOverLimit
    case "ok":
      return null
  }
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

/**
 * True when the saved config carried a quote hash and the first ready quote
 * has another one: the price moved since the user last saw it.
 */
export function quoteHashChanged(
  savedHash: string | undefined,
  readyHash: string,
): boolean {
  return savedHash != null && savedHash !== "" && savedHash !== readyHash
}
