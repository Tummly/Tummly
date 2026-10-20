import type { BillingCadence } from "@/lib/operatorBillingCredits/managePlanPresentation"

/** Marketing entry to self-serve Signup. */
export const SIGNUP_PATH = "/signup"

export type SignupPlanIntent = {
  plan: "Pilot" | "Starter" | "Growth" | "Group" | "Custom"
  cadence?: BillingCadence
  /** Base64 or JSON from `/custom-plan` continue CTA (`customIntent` query). */
  customConfigJson?: string
}

const SIGNUP_PLAN_INTENT_KEY = "tummly.signupPlanIntent"

const PAID_PLANS = new Set(["Starter", "Growth", "Group", "Custom"])

export type BuildSignupPathOptions = {
  /** Prefills Work email on `/signup` so the visitor does not type it again. */
  email?: string | null
}

/** Build `/signup?plan=&cadence=&email=` for Pricing / marketing CTAs. */
export function buildSignupPath(
  intent?: SignupPlanIntent | null,
  options?: BuildSignupPathOptions,
): string {
  const params = new URLSearchParams()
  if (intent != null) {
    params.set("plan", intent.plan)
    if (intent.plan !== "Pilot" && intent.cadence != null) {
      params.set("cadence", intent.cadence)
    }
  }
  const email = options?.email?.trim()
  if (email != null && email !== "") {
    params.set("email", email)
  }
  const query = params.toString()
  return query === "" ? SIGNUP_PATH : `${SIGNUP_PATH}?${query}`
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
  const customIntent = searchParams.get("customIntent")?.trim()
  return {
    plan: plan as "Starter" | "Growth" | "Group" | "Custom",
    cadence,
    ...(plan === "Custom" && customIntent != null && customIntent !== ""
      ? { customConfigJson: customIntent }
      : {}),
  }
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
      const customConfigJson =
        typeof parsed.customConfigJson === "string" &&
        parsed.customConfigJson.trim() !== ""
          ? parsed.customConfigJson
          : undefined
      return {
        plan: parsed.plan,
        cadence: parsed.cadence === "annual" ? "annual" : "monthly",
        ...(parsed.plan === "Custom" && customConfigJson != null
          ? { customConfigJson }
          : {}),
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
