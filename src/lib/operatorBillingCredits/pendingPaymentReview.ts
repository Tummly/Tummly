import type { CustomPlanQuoteResult } from "@/api/adminCommercialCatalogApi"
import {
  chosenPlanToBasePlan,
  hasLegacyCapacityPack,
  normalizePackCounts,
  toOnboardingCustomPlanConfigJson,
  type CustomPlanBasePlanKey,
} from "@/lib/marketingCustomPlan/customPlanEstimate"
import {
  isQuoteReviewCode,
  type QuoteReviewCode,
} from "@/lib/marketingCustomPlan/quoteReview"

/** The unpaid plan on the Billing page, as the API sends it. */
export type PendingPaymentSnapshot = {
  plan: string
  cadence: string
  failedAtUtc: string | null
  /** Why the last attempt failed (for example `quote_changed`). */
  failureCode?: string | null
  /** Saved Custom choices, so the page can ask for a fresh quote. */
  basePlan?: string | null
  locations?: number | null
  packs?: Record<string, number> | null
}

/** Reads a server error code from a rejected request body, or set on the error. */
export function pendingPaymentErrorCode(error: unknown): string | null {
  const fromBody = (error as { response?: { data?: { code?: unknown } } } | null)
    ?.response?.data?.code
  if (typeof fromBody === "string") {
    return fromBody
  }
  const fromError = (error as { code?: unknown } | null)?.code
  return typeof fromError === "string" ? fromError : null
}

/**
 * The code that sends the owner to a fresh quote review: the failure of the last
 * attempt in this session, else the one the status payload carries.
 */
export function pendingPaymentReviewCode(
  pending: PendingPaymentSnapshot | null | undefined,
  attemptCode: string | null,
): QuoteReviewCode | null {
  if (pending == null) {
    return null
  }
  if (isQuoteReviewCode(attemptCode)) {
    return attemptCode
  }
  return isQuoteReviewCode(pending.failureCode) ? pending.failureCode : null
}

export type PendingPaymentSavedChoices = {
  cadence: "monthly" | "annual"
  basePlan: CustomPlanBasePlanKey
  locations: number
  packs: Record<string, number>
}

/**
 * The saved choices to quote again. Null when a saved pack was retired (the owner
 * chooses again, nothing is converted) or the choices cannot be read.
 */
export function pendingPaymentSavedChoices(
  pending: PendingPaymentSnapshot | null | undefined,
): PendingPaymentSavedChoices | null {
  const basePlan = chosenPlanToBasePlan(pending?.basePlan)
  const locations = pending?.locations
  if (
    pending == null ||
    basePlan == null ||
    typeof locations !== "number" ||
    !Number.isInteger(locations) ||
    locations < 1 ||
    hasLegacyCapacityPack(pending.packs)
  ) {
    return null
  }
  return {
    cadence: pending.cadence === "annual" ? "annual" : "monthly",
    basePlan,
    locations,
    packs: { ...(pending.packs ?? {}) },
  }
}

/** The reviewed quote as the retry request sends it: its choices and its hash. */
export function reviewedConfigJsonFromQuote(
  quote: CustomPlanQuoteResult,
): string | null {
  const basePlan = chosenPlanToBasePlan(quote.basePlan)
  if (basePlan == null) {
    return null
  }
  return toOnboardingCustomPlanConfigJson({
    version: 2,
    cadence: quote.cadence === "annual" ? "annual" : "monthly",
    basePlan,
    locations: quote.locations,
    packs: normalizePackCounts(quote.packs),
    quoteHash: quote.quoteHash,
  })
}
