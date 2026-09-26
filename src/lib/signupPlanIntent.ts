import type { BillingCadence } from "@/lib/operatorBillingCredits/managePlanPresentation"

/** Marketing entry to self-serve Signup. */
export const SIGNUP_PATH = "/signup"

export type SignupPlanIntent = {
  plan: "Pilot" | "Starter" | "Growth" | "Group"
  cadence?: BillingCadence
}

const SIGNUP_PLAN_INTENT_KEY = "tummly.signupPlanIntent"

const PAID_PLANS = new Set(["Starter", "Growth", "Group"])

/** Build `/signup?plan=&cadence=` for Pricing / marketing CTAs. */
export function buildSignupPath(intent?: SignupPlanIntent | null): string {
  if (intent == null) {
    return SIGNUP_PATH
  }
  const params = new URLSearchParams()
  params.set("plan", intent.plan)
  if (intent.plan !== "Pilot" && intent.cadence != null) {
    params.set("cadence", intent.cadence)
  }
  return `${SIGNUP_PATH}?${params.toString()}`
}

export function parseSignupPlanIntent(
  searchParams: URLSearchParams
): SignupPlanIntent | null {
  const planRaw = searchParams.get("plan")?.trim()
  if (planRaw == null || planRaw === "") {
    return null
  }
  const plan =
    planRaw === "Pilot" || PAID_PLANS.has(planRaw) ? planRaw : null
  if (plan == null) {
    return null
  }
  if (plan === "Pilot") {
    return { plan: "Pilot" }
  }
  const cadenceRaw = searchParams.get("cadence")?.trim().toLowerCase()
  const cadence: BillingCadence =
    cadenceRaw === "annual" ? "annual" : "monthly"
  return { plan: plan as "Starter" | "Growth" | "Group", cadence }
}

export function saveSignupPlanIntent(intent: SignupPlanIntent): void {
  sessionStorage.setItem(SIGNUP_PLAN_INTENT_KEY, JSON.stringify(intent))
}

export function readSignupPlanIntent(): SignupPlanIntent | null {
  const raw = sessionStorage.getItem(SIGNUP_PLAN_INTENT_KEY)
  if (raw == null || raw.trim() === "") {
    return null
  }
  try {
    const parsed = JSON.parse(raw) as SignupPlanIntent
    if (parsed.plan === "Pilot") {
      return { plan: "Pilot" }
    }
    if (PAID_PLANS.has(parsed.plan)) {
      return {
        plan: parsed.plan,
        cadence: parsed.cadence === "annual" ? "annual" : "monthly",
      }
    }
  } catch {
    return null
  }
  return null
}

export function clearSignupPlanIntent(): void {
  sessionStorage.removeItem(SIGNUP_PLAN_INTENT_KEY)
}

/** Capture `?plan=` / `?cadence=` from the current signup URL into sessionStorage.
 * Plain `/signup` clears a stale intent so Free is not overridden.
 */
export function captureSignupPlanIntentFromSearch(
  searchParams: URLSearchParams
): SignupPlanIntent | null {
  const intent = parseSignupPlanIntent(searchParams)
  if (intent != null) {
    saveSignupPlanIntent(intent)
    return intent
  }
  clearSignupPlanIntent()
  return null
}
