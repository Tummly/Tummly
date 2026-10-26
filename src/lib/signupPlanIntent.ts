import type { BillingCadence } from "@/lib/operatorBillingCredits/managePlanPresentation"
import {
  decodeCustomPlanIntentV2,
  encodeCustomPlanIntentV2,
  type CustomPlanBasePlanKey,
  type CustomPlanPackCounts,
  type CustomPlanV2ConfigInput,
} from "@/lib/marketingCustomPlan/customPlanEstimate"

/** Marketing entry to self-serve Signup. */
export const SIGNUP_PATH = "/signup"

export type SignupCustomPlanConfig = CustomPlanV2ConfigInput

export type SignupPlanIntent = {
  plan: "Pilot" | "Starter" | "Growth" | "Group" | "Custom"
  cadence?: BillingCadence
  /** v2 calculator config — survives Back, reload, and plan changes. */
  config?: SignupCustomPlanConfig
}

const SIGNUP_PLAN_INTENT_KEY = "tummly.signupPlanIntent"

const PAID_PLANS = new Set(["Starter", "Growth", "Group", "Custom"])

export type BuildSignupPathOptions = {
  /** Prefills Work email on `/signup` so the visitor does not type it again. */
  email?: string | null
}

function titleCasePlanFromBase(
  basePlan: CustomPlanBasePlanKey,
): "Starter" | "Growth" | "Group" {
  if (basePlan === "starter") return "Starter"
  if (basePlan === "growth") return "Growth"
  return "Group"
}

/** Build `/signup?plan=&cadence=&email=&customIntent=` for Pricing / marketing CTAs. */
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
    if (intent.config != null) {
      params.set(
        "customIntent",
        encodeCustomPlanIntentV2(intent.config),
      )
    }
  }
  const email = options?.email?.trim()
  if (email != null && email !== "") {
    params.set("email", email)
  }
  const query = params.toString()
  return query === "" ? SIGNUP_PATH : `${SIGNUP_PATH}?${query}`
}

/** A whole Location count of 1 or more. Anything else is not restored. */
function isValidLocationCount(value: unknown): value is number {
  return typeof value === "number" && Number.isInteger(value) && value >= 1
}

function normalizeIntent(parsed: SignupPlanIntent): SignupPlanIntent | null {
  if (parsed.plan === "Pilot") {
    return { plan: "Pilot" }
  }
  if (!PAID_PLANS.has(parsed.plan)) {
    return null
  }
  const cadence: BillingCadence =
    parsed.cadence === "annual" ? "annual" : "monthly"
  // An invalid count drops the whole config: the review shows the restore-failure copy.
  const config =
    parsed.config != null &&
    parsed.config.version === 2 &&
    isValidLocationCount(parsed.config.locations)
      ? {
          version: 2 as const,
          cadence: parsed.config.cadence === "annual" ? "annual" as const : "monthly" as const,
          basePlan: parsed.config.basePlan,
          locations: parsed.config.locations,
          packs: parsed.config.packs ?? {},
          ...(parsed.config.quoteHash ? { quoteHash: parsed.config.quoteHash } : {}),
        }
      : undefined
  return {
    plan: parsed.plan,
    cadence,
    ...(config != null ? { config } : {}),
  }
}

export function parseSignupPlanIntent(
  searchParams: URLSearchParams,
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
  const decoded =
    customIntent != null && customIntent !== ""
      ? decodeCustomPlanIntentV2(customIntent)
      : null

  if (decoded != null) {
    const config: SignupCustomPlanConfig = {
      ...decoded,
      cadence: decoded.cadence === "annual" ? "annual" : cadence,
    }
    // Prefer explicit Custom; otherwise keep the card plan for preselect.
    const resolvedPlan =
      plan === "Custom" ? "Custom" : (plan as SignupPlanIntent["plan"])
    return {
      plan: resolvedPlan,
      cadence: config.cadence,
      config,
    }
  }

  return {
    plan: plan as "Starter" | "Growth" | "Group" | "Custom",
    cadence,
  }
}

/**
 * The saved calculator config applies only to Custom or to the card on its
 * base plan. It stays stored while the user compares other cards, so going
 * back to that card restores the customisation.
 */
export function activeCustomConfig(
  intent: SignupPlanIntent | null | undefined,
): SignupCustomPlanConfig | null {
  if (intent == null || intent.plan === "Pilot" || intent.config == null) {
    return null
  }
  if (intent.plan === "Custom") {
    return intent.config
  }
  return intent.config.basePlan === intent.plan.toLowerCase()
    ? intent.config
    : null
}

export function saveSignupPlanIntent(intent: SignupPlanIntent): void {
  const normalized = normalizeIntent(intent)
  if (normalized == null) {
    return
  }
  sessionStorage.setItem(SIGNUP_PLAN_INTENT_KEY, JSON.stringify(normalized))
}

export function readSignupPlanIntent(): SignupPlanIntent | null {
  const raw = sessionStorage.getItem(SIGNUP_PLAN_INTENT_KEY)
  if (raw == null || raw.trim() === "") {
    return null
  }
  try {
    return normalizeIntent(JSON.parse(raw) as SignupPlanIntent)
  } catch {
    return null
  }
}

export function clearSignupPlanIntent(): void {
  sessionStorage.removeItem(SIGNUP_PLAN_INTENT_KEY)
}

/** Capture `?plan=` / `?cadence=` / `?customIntent=` from the signup URL into sessionStorage.
 * Plain `/signup` (OAuth error return, "Use a different email") keeps the saved
 * intent: the plan step always lists every plan, so it only preselects.
 */
export function captureSignupPlanIntentFromSearch(
  searchParams: URLSearchParams,
): SignupPlanIntent | null {
  const intent = parseSignupPlanIntent(searchParams)
  if (intent != null) {
    saveSignupPlanIntent(intent)
    return intent
  }
  return readSignupPlanIntent()
}

export function updateSignupPlanSelection(args: {
  plan: SignupPlanIntent["plan"]
  cadence?: BillingCadence
  /** When false, drop a prior custom config (plain catalog checkout). */
  keepConfig?: boolean
}): SignupPlanIntent {
  const existing = readSignupPlanIntent()
  const cadence =
    args.plan === "Pilot"
      ? undefined
      : (args.cadence ?? existing?.cadence ?? "monthly")
  const keepConfig = args.keepConfig === true
  let config: SignupCustomPlanConfig | undefined
  if (keepConfig && args.plan !== "Pilot" && existing?.config != null) {
    const nextBase: CustomPlanBasePlanKey =
      args.plan === "Custom" || args.plan === "Starter" || args.plan === "Growth" || args.plan === "Group"
        ? args.plan === "Custom"
          ? existing.config.basePlan
          : (args.plan.toLowerCase() as CustomPlanBasePlanKey)
        : existing.config.basePlan
    config = {
      ...existing.config,
      cadence: cadence ?? existing.config.cadence,
      basePlan: nextBase,
    }
  }

  const next: SignupPlanIntent = {
    plan: args.plan,
    ...(cadence != null ? { cadence } : {}),
    ...(config != null ? { config } : {}),
  }
  saveSignupPlanIntent(next)
  return next
}

export function saveSignupCustomPlanConfig(args: {
  basePlan: CustomPlanBasePlanKey
  cadence: BillingCadence
  locations: number
  packs: Partial<CustomPlanPackCounts>
  /** When true, store plan as Custom (has packs / extras). */
  asCustom: boolean
  /** Hash of the server quote the customer confirmed. */
  quoteHash?: string
}): SignupPlanIntent {
  const config: SignupCustomPlanConfig = {
    version: 2,
    cadence: args.cadence,
    basePlan: args.basePlan,
    locations: args.locations,
    packs: args.packs,
    ...(args.quoteHash ? { quoteHash: args.quoteHash } : {}),
  }
  const next: SignupPlanIntent = {
    plan: args.asCustom ? "Custom" : titleCasePlanFromBase(args.basePlan),
    cadence: args.cadence,
    config,
  }
  saveSignupPlanIntent(next)
  return next
}
