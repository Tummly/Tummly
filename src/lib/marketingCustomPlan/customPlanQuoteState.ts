import type { CustomPlanQuoteResult } from "@/api/adminCommercialCatalogApi"
import { CUSTOM_PLAN_QUOTE_COPY } from "@/content/marketing/customPlanQuoteCopy"
import {
  CUSTOM_PLAN_MAX_LOCATIONS,
  CUSTOM_PLAN_MAX_PACK_COUNT,
  packMaxCount,
  type CustomPlanPackKey,
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
export function checkPackCount(
  value: number | null | undefined,
  packKey?: CustomPlanPackKey,
): PackCountCheck {
  if (value == null || Number.isNaN(value)) {
    return "empty"
  }
  if (!Number.isInteger(value) || value < 0) {
    return "not_integer"
  }
  const max = packKey == null ? CUSTOM_PLAN_MAX_PACK_COUNT : packMaxCount(packKey)
  if (value > max) {
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
export function packCountMessage(
  check: PackCountCheck,
  packKey?: CustomPlanPackKey,
): string | null {
  const unit =
    packKey != null &&
    packMaxCount(packKey) !== CUSTOM_PLAN_MAX_PACK_COUNT
  switch (check) {
    case "empty":
      return unit
        ? CUSTOM_PLAN_QUOTE_COPY.unitEmpty
        : CUSTOM_PLAN_QUOTE_COPY.packEmpty
    case "not_integer":
      return unit
        ? CUSTOM_PLAN_QUOTE_COPY.unitNotWhole
        : CUSTOM_PLAN_QUOTE_COPY.packNotWhole
    case "over_limit":
      return unit
        ? CUSTOM_PLAN_QUOTE_COPY.unitOverLimit
        : CUSTOM_PLAN_QUOTE_COPY.packOverLimit
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

/**
 * Copy and control state for the quote panel. Continue needs a ready quote.
 * A changed price also needs the customer's explicit confirmation.
 */
export function quoteView(input: {
  check: LocationInputCheck
  state: QuoteState
  changed: boolean
  /** The customer confirmed the ready quote's price. Ignored unless `changed`. */
  priceConfirmed?: boolean
  /** Replaces the generic failure copy where the quote gates a payment (signup review). */
  failedMessage?: string
}): QuoteView {
  const {
    check,
    state,
    changed,
    priceConfirmed = false,
    failedMessage = CUSTOM_PLAN_QUOTE_COPY.failed,
  } = input
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
      if (state.code === "legacy_pack_key") {
        return {
          ...base,
          message: CUSTOM_PLAN_QUOTE_COPY.optionUnavailable,
          continueEnabled: false,
        }
      }
      if (state.code === "invalid_pack_quantity") {
        return {
          ...base,
          message: CUSTOM_PLAN_QUOTE_COPY.unitNotWhole,
          continueEnabled: false,
        }
      }
      if (state.code === "pack_quantity_over_limit") {
        return {
          ...base,
          message: CUSTOM_PLAN_QUOTE_COPY.unitOverLimit,
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
        message: failedMessage,
        showRetry: true,
        continueEnabled: false,
      }
    case "ready":
      return {
        ...base,
        message: changed ? CUSTOM_PLAN_QUOTE_COPY.changed : null,
        continueEnabled: !changed || priceConfirmed,
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

/**
 * The price confirmation counts only for the ready quote it was ticked on.
 * Any new quote hash, or no ready quote, leaves it unconfirmed.
 */
export function isPriceConfirmed(
  confirmedHash: string | null,
  readyHash: string | null,
): boolean {
  return readyHash != null && confirmedHash === readyHash
}

/**
 * The acknowledgement to keep once a ready quote is known: null when the quote
 * is another one than the one confirmed, so an earlier total never becomes valid again.
 */
export function keepConfirmedHash(
  confirmedHash: string | null,
  readyHash: string | null,
): string | null {
  return confirmedHash != null && readyHash != null && confirmedHash !== readyHash
    ? null
    : confirmedHash
}

/** Notice for a saved selection that named a capacity option that is no longer sold. */
export function capacityRetiredNotice(
  intent: { capacityOptionRetired?: true } | null | undefined,
): string | null {
  return intent?.capacityOptionRetired === true
    ? CUSTOM_PLAN_QUOTE_COPY.optionUnavailable
    : null
}

/**
 * Signup review: whether Continue to payment is open. A changed price needs the
 * customer's tick for the exact ready total; a failed or loading quote never continues.
 */
export function reviewGate(input: {
  selectable: boolean
  waitingForCatalogue: boolean
  check: LocationInputCheck
  state: QuoteState
  /** The server rejected the saved quote hash. */
  quoteChanged: boolean
  /** The first ready quote differs from the hash saved earlier. */
  restoredHashChanged: boolean
  confirmedHash: string | null
}): {
  view: QuoteView | null
  continueEnabled: boolean
  priceChanged: boolean
  priceConfirmed: boolean
  readyHash: string | null
  /** The tick to keep: null once another total is ready. */
  keptHash: string | null
} {
  const readyHash =
    input.state.status === "ready" ? input.state.quote.quoteHash : null
  const priceChanged = input.quoteChanged || input.restoredHashChanged
  const priceConfirmed = isPriceConfirmed(input.confirmedHash, readyHash)
  const keptHash = keepConfirmedHash(input.confirmedHash, readyHash)
  const view = input.waitingForCatalogue
    ? null
    : quoteView({
        check: input.check,
        state: input.state,
        changed: priceChanged,
        priceConfirmed,
        failedMessage: CUSTOM_PLAN_QUOTE_COPY.quoteUnverified,
      })
  return {
    view,
    continueEnabled: input.selectable && view?.continueEnabled === true,
    priceChanged,
    priceConfirmed,
    readyHash,
    keptHash,
  }
}
