import type { SignupOnboardingPayload, SignupSavedPlan } from "@/api/signupApi"
import {
  isQuoteReviewCode,
  quoteReviewMessage,
} from "@/lib/marketingCustomPlan/quoteReview"
import { toOnboardingCustomPlanConfigJson } from "@/lib/marketingCustomPlan/customPlanEstimate"
import {
  activeCustomConfig,
  parseSignupPlanIntent,
  type SignupPlanIntent,
} from "@/lib/signupPlanIntent"

export type SignupPaymentFailureView =
  | { kind: "review"; message: string }
  | { kind: "retry" }

/**
 * The provisioning page after a paid checkout did not open. A stale or retired
 * saved quote cannot be retried: it needs a fresh review. Any other failure
 * keeps Retry.
 */
export function signupPaymentFailureView(status: {
  status?: string
  paymentFailureCode?: string | null
  savedPlan?: SignupSavedPlan | null
}): SignupPaymentFailureView {
  const message = quoteReviewMessage(status.paymentFailureCode)
  if (message != null && status.savedPlan != null) {
    return { kind: "review", message }
  }
  return { kind: "retry" }
}

/**
 * The plan intent for the saved plan the server holds. A retired pack drops the
 * config (never converted) and the saved base plan stays selected, so the
 * customer chooses again from the current options.
 */
export function signupPlanIntentFromSavedPlan(
  saved: SignupSavedPlan | null | undefined,
): SignupPlanIntent | null {
  if (saved?.chosenPlan == null) {
    return null
  }
  const params = new URLSearchParams({ plan: saved.chosenPlan })
  if (saved.chosenCadence != null) {
    params.set("cadence", saved.chosenCadence)
  }
  if (saved.customPlanConfigJson != null && saved.customPlanConfigJson !== "") {
    params.set("customIntent", saved.customPlanConfigJson)
  }
  const intent = parseSignupPlanIntent(params)
  if (intent == null || intent.plan === "Pilot") {
    return null
  }
  if (intent.capacityOptionRetired === true && intent.plan === "Custom") {
    const basePlan = savedBasePlan(saved.customPlanConfigJson)
    if (basePlan != null) {
      return { ...intent, plan: basePlan }
    }
  }
  return intent
}

function savedBasePlan(
  configJson: string | null | undefined,
): "Starter" | "Growth" | "Group" | null {
  try {
    const parsed = JSON.parse(configJson ?? "") as { basePlan?: unknown }
    switch (parsed.basePlan) {
      case "starter":
        return "Starter"
      case "growth":
        return "Growth"
      case "group":
        return "Group"
      default:
        return null
    }
  } catch {
    return null
  }
}

export type SignupPlanUpdatePayload = Pick<
  SignupOnboardingPayload,
  "chosenPlan" | "chosenCadence" | "customPlanConfigJson"
>

/** The reviewed plan to send, or null when there is no paid plan to send. */
export function signupPlanUpdatePayload(
  intent: SignupPlanIntent | null | undefined,
): SignupPlanUpdatePayload | null {
  if (
    intent == null ||
    intent.plan === "Pilot" ||
    intent.capacityOptionRetired === true
  ) {
    return null
  }
  const cadence = intent.cadence ?? "monthly"
  const config = activeCustomConfig(intent)
  return {
    chosenPlan: intent.plan,
    chosenCadence: cadence,
    ...(config != null
      ? {
          customPlanConfigJson: toOnboardingCustomPlanConfigJson({
            ...config,
            cadence,
          }),
        }
      : {}),
  }
}

export type SignupReviewSubmitOutcome =
  | "review_again"
  | "choose_again"
  | "show_error"

/** What the review page does after the server rejected the reviewed plan. */
export function signupReviewSubmitOutcome(
  code: string | null | undefined,
): SignupReviewSubmitOutcome {
  if (!isQuoteReviewCode(code)) {
    return "show_error"
  }
  return code === "quote_changed" ? "review_again" : "choose_again"
}
