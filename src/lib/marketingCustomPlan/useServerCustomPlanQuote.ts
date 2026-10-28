import { useCallback, useEffect, useReducer, useRef, useState } from "react"

import {
  quotePublicCustomPlan,
  type CustomPlanQuoteRequest,
} from "@/api/adminCommercialCatalogApi"
import type {
  CustomPlanBasePlanKey,
  CustomPlanPackKey,
} from "@/lib/marketingCustomPlan/customPlanEstimate"
import {
  checkLocationInput,
  checkPackCount,
  quoteReducer,
  type LocationInputCheck,
  type QuoteState,
} from "@/lib/marketingCustomPlan/customPlanQuoteState"
import type { BillingCadence } from "@/lib/operatorBillingCredits/managePlanPresentation"

const DEFAULT_DEBOUNCE_MS = 250

export type ServerCustomPlanQuoteInput = {
  cadence: BillingCadence
  basePlan: CustomPlanBasePlanKey | null
  locations: number | null
  /** A null count is typed text that is not a number yet. */
  packs: Partial<Record<CustomPlanPackKey, number | null>>
}

/** Zero packs are left out; the server treats an absent pack as none. */
function toRequestBody(input: ServerCustomPlanQuoteInput): CustomPlanQuoteRequest {
  const packs: Record<string, number> = {}
  for (const [key, count] of Object.entries(input.packs)) {
    if (typeof count === "number" && count > 0) {
      packs[key] = count
    }
  }
  return {
    cadence: input.cadence,
    basePlan: input.basePlan,
    locations: input.locations ?? 0,
    packs,
  }
}

function errorCode(error: unknown): string | null {
  const code = (error as { code?: unknown } | null)?.code
  return typeof code === "string" ? code : null
}

/**
 * Server-priced quote for the custom plan calculator. Each input change marks
 * the quote loading at once, then sends one request after the debounce. Only a
 * response for the newest request id can change the state. No request is sent
 * while the Location count or any pack count is invalid.
 */
export function useServerCustomPlanQuote(
  input: ServerCustomPlanQuoteInput,
  options?: { debounceMs?: number },
): {
  state: QuoteState
  check: LocationInputCheck
  packsValid: boolean
  retry: () => void
} {
  const debounceMs = options?.debounceMs ?? DEFAULT_DEBOUNCE_MS
  const check = checkLocationInput(input.locations)
  const packsValid = Object.values(input.packs).every(
    (count) => checkPackCount(count) === "ok",
  )
  const inputValid = check === "ok" && packsValid
  // A string key keeps an inline packs object from restarting the debounce on every render.
  const requestKey = JSON.stringify(toRequestBody(input))
  const [state, dispatch] = useReducer(quoteReducer, { status: "idle" })
  const [attempt, setAttempt] = useState(0)
  const requestIdRef = useRef(0)

  useEffect(() => {
    if (!inputValid) {
      return
    }
    requestIdRef.current += 1
    const requestId = requestIdRef.current
    dispatch({ type: "request", requestId })

    const body = JSON.parse(requestKey) as CustomPlanQuoteRequest
    const timer = window.setTimeout(() => {
      quotePublicCustomPlan(body)
        .then((quote) => dispatch({ type: "success", requestId, quote }))
        .catch((error: unknown) =>
          dispatch({ type: "failure", requestId, code: errorCode(error) }),
        )
    }, debounceMs)

    return () => window.clearTimeout(timer)
  }, [inputValid, requestKey, debounceMs, attempt])

  const retry = useCallback(() => setAttempt((n) => n + 1), [])

  return { state, check, packsValid, retry }
}
