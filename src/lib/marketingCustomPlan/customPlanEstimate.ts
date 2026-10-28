import type { CustomPlanQuoteResult } from "@/api/adminCommercialCatalogApi"
import type { BillingCadence } from "@/lib/operatorBillingCredits/managePlanPresentation"

export const CUSTOM_PLAN_MAX_LOCATIONS = 500
export const CUSTOM_PLAN_MAX_PACK_COUNT = 99
/** Most single capacity units (team users, Offers, QR placements) of one kind. */
export const CUSTOM_PLAN_MAX_CAPACITY_UNITS = 500

export type CustomPlanBasePlanKey = "starter" | "growth" | "group"

export type CustomPlanPackKey =
  | "team_user"
  | "offer"
  | "qr_placement"
  | "email_20k"
  | "email_50k"
  | "sms_500"
  | "sms_1000"
  | "sms_5000"
  | "ai_500"
  | "ai_2000"

export type CustomPlanPackDef = {
  packKey: CustomPlanPackKey
  rateKey: string
  /** Units one count buys: 1 for capacity (single units), a pack size for credits. */
  packSize: number
  /** Largest count one quote accepts. */
  maxCount: number
  /** Short title for pack steppers. */
  title: string
  channel?: "capacity" | "email" | "sms" | "ai"
}

/** Fixed pack catalogue — contract §1 (both sides). */
export const CUSTOM_PLAN_PACKS: readonly CustomPlanPackDef[] = [
  {
    packKey: "team_user",
    rateKey: "team_user",
    packSize: 1,
    maxCount: CUSTOM_PLAN_MAX_CAPACITY_UNITS,
    title: "Additional team users",
    channel: "capacity",
  },
  {
    packKey: "offer",
    rateKey: "offer",
    packSize: 1,
    maxCount: CUSTOM_PLAN_MAX_CAPACITY_UNITS,
    title: "Additional active Offers",
    channel: "capacity",
  },
  {
    packKey: "qr_placement",
    rateKey: "qr_placement",
    packSize: 1,
    maxCount: CUSTOM_PLAN_MAX_CAPACITY_UNITS,
    title: "Extra QR placements",
    channel: "capacity",
  },
  {
    packKey: "email_20k",
    rateKey: "email_credit",
    packSize: 20_000,
    maxCount: CUSTOM_PLAN_MAX_PACK_COUNT,
    title: "+20,000 Email/month",
    channel: "email",
  },
  {
    packKey: "email_50k",
    rateKey: "email_credit",
    packSize: 50_000,
    maxCount: CUSTOM_PLAN_MAX_PACK_COUNT,
    title: "+50,000 Email/month",
    channel: "email",
  },
  {
    packKey: "sms_500",
    rateKey: "sms_credit",
    packSize: 500,
    maxCount: CUSTOM_PLAN_MAX_PACK_COUNT,
    title: "+500 SMS/month",
    channel: "sms",
  },
  {
    packKey: "sms_1000",
    rateKey: "sms_credit",
    packSize: 1_000,
    maxCount: CUSTOM_PLAN_MAX_PACK_COUNT,
    title: "+1,000 SMS/month",
    channel: "sms",
  },
  {
    packKey: "sms_5000",
    rateKey: "sms_credit",
    packSize: 5_000,
    maxCount: CUSTOM_PLAN_MAX_PACK_COUNT,
    title: "+5,000 SMS/month",
    channel: "sms",
  },
  {
    packKey: "ai_500",
    rateKey: "ai_credit",
    packSize: 500,
    maxCount: CUSTOM_PLAN_MAX_PACK_COUNT,
    title: "+500 AI actions/month",
    channel: "ai",
  },
  {
    packKey: "ai_2000",
    rateKey: "ai_credit",
    packSize: 2_000,
    maxCount: CUSTOM_PLAN_MAX_PACK_COUNT,
    title: "+2,000 AI actions/month",
    channel: "ai",
  },
] as const

export type CustomPlanPackCounts = Record<CustomPlanPackKey, number>

export const EMPTY_PACK_COUNTS: CustomPlanPackCounts = {
  team_user: 0,
  offer: 0,
  qr_placement: 0,
  email_20k: 0,
  email_50k: 0,
  sms_500: 0,
  sms_1000: 0,
  sms_5000: 0,
  ai_500: 0,
  ai_2000: 0,
}

/** Included allowances per paid plan, for the "Included in …" lines. Prices come from the server quote. */
export type PublicCatalogPlanSummary = {
  planKey: CustomPlanBasePlanKey
  displayName: string
  monthlyNetPence: number
  annualNetPence: number
  includedLocations: number
  includedTeamMembers: number
  activeOffersAccount: number
  activeQrPlacementsPerLocation: number
  creditsMonthlyAi: number
  creditsMonthlyEmail: number
  creditsMonthlySms: number
}

export type CustomPlanV2ConfigInput = {
  version: 2
  cadence: BillingCadence
  basePlan: CustomPlanBasePlanKey
  locations: number
  packs: Partial<CustomPlanPackCounts>
  /** Hash of the server quote the customer saw. Sent back at signup. */
  quoteHash?: string
}

export function normalizePackCounts(
  packs: Partial<CustomPlanPackCounts> | Record<string, number> | null | undefined,
): CustomPlanPackCounts {
  const next = { ...EMPTY_PACK_COUNTS }
  if (packs == null) {
    return next
  }
  for (const def of CUSTOM_PLAN_PACKS) {
    const raw = packs[def.packKey]
    if (typeof raw !== "number" || !Number.isFinite(raw)) {
      continue
    }
    // No clamp or rounding: the calculator shows an invalid saved count instead.
    next[def.packKey] = raw
  }
  return next
}

/** Capacity packs Commercial V4 replaced by single units. They are no longer sold. */
const LEGACY_CAPACITY_PACK_KEYS = ["team_users_5", "offers_5", "qr_10"] as const

/** True when saved packs name a replaced capacity pack. It is never converted. */
export function hasLegacyCapacityPack(
  packs: Record<string, unknown> | null | undefined,
): boolean {
  return (
    packs != null &&
    LEGACY_CAPACITY_PACK_KEYS.some((key) => Object.hasOwn(packs, key))
  )
}

/** Largest count for one pack key. */
export function packMaxCount(packKey: CustomPlanPackKey): number {
  return (
    CUSTOM_PLAN_PACKS.find((def) => def.packKey === packKey)?.maxCount ??
    CUSTOM_PLAN_MAX_PACK_COUNT
  )
}

export function packsHaveAny(packs: CustomPlanPackCounts): boolean {
  return CUSTOM_PLAN_PACKS.some((def) => packs[def.packKey] > 0)
}

/** Starter 1 < Growth 2 < Group 3. */
export function basePlanRank(planKey: CustomPlanBasePlanKey): number {
  if (planKey === "starter") return 1
  if (planKey === "growth") return 2
  return 3
}

export function parseBasePlanKey(
  value: string | null | undefined,
): CustomPlanBasePlanKey | null {
  const key = value?.trim().toLowerCase()
  if (key === "starter" || key === "growth" || key === "group") {
    return key
  }
  return null
}

export function basePlanToChosenPlan(
  basePlan: CustomPlanBasePlanKey,
): "Starter" | "Growth" | "Group" {
  if (basePlan === "starter") return "Starter"
  if (basePlan === "growth") return "Growth"
  return "Group"
}

export function chosenPlanToBasePlan(
  plan: string | null | undefined,
): CustomPlanBasePlanKey | null {
  return parseBasePlanKey(plan)
}

/** Period suffix for a quote total: "/ per month" or "/ per year". */
export function quoteCadenceSuffix(cadence: string): string {
  return cadence === "annual" ? "/ per year" : "/ per month"
}

/**
 * The server line for one pack: its amount and the suffix for the quote's
 * cadence. Null without a quote or without that line. The browser adds no price.
 */
export function quotePackLineAmount(
  quote: CustomPlanQuoteResult | null,
  packKey: CustomPlanPackKey,
): { netPence: number; suffix: string } | null {
  if (quote == null) {
    return null
  }
  const line = quote.lines.find((entry) => entry.key === `pack:${packKey}`)
  if (line == null) {
    return null
  }
  return {
    netPence: line.netPence,
    suffix: quote.cadence === "annual" ? "/ per year" : "/mo",
  }
}

/** "starter" or "Starter" from the server → "Starter". */
export function quotePlanName(quote: CustomPlanQuoteResult): string {
  const key = quote.basePlan.trim().toLowerCase()
  return key.charAt(0).toUpperCase() + key.slice(1)
}

/** The plan name the server stored for a quote. Anything but a paid base plan is Custom. */
export function storedPlanFromQuote(
  storedAsPlan: string,
): "Starter" | "Growth" | "Group" | "Custom" {
  if (
    storedAsPlan === "Starter" ||
    storedAsPlan === "Growth" ||
    storedAsPlan === "Group"
  ) {
    return storedAsPlan
  }
  return "Custom"
}

/**
 * Smallest of starter/growth/group whose IncludedLocations >= n, else group.
 * Contract §2. Returns null until the catalogue has paid plans.
 */
export function minBasePlanForLocations(
  locations: number,
  plans: PublicCatalogPlanSummary[],
): CustomPlanBasePlanKey | null {
  const n = Math.max(1, Math.floor(locations))
  const paid = plans
    .filter(
      (plan) =>
        plan.planKey === "starter" ||
        plan.planKey === "growth" ||
        plan.planKey === "group",
    )
    .slice()
    .sort((a, b) => a.includedLocations - b.includedLocations)

  if (paid.length === 0) {
    return null
  }
  for (const plan of paid) {
    if (plan.includedLocations >= n) {
      return plan.planKey
    }
  }
  return "group"
}

/** Compact v2 intent for query / session / onboarding save. */
export function encodeCustomPlanIntentV2(config: CustomPlanV2ConfigInput): string {
  const payload = {
    version: 2,
    cadence: config.cadence,
    basePlan: config.basePlan,
    locations: config.locations,
    packs: packCountsForWire(config.packs),
    ...(config.quoteHash ? { quoteHash: config.quoteHash } : {}),
  }
  return btoa(unescape(encodeURIComponent(JSON.stringify(payload))))
}

/** Only non-zero packs go on the wire. */
function packCountsForWire(
  packs: Partial<CustomPlanPackCounts>,
): Record<string, number> {
  const normalized = normalizePackCounts(packs)
  const wire: Record<string, number> = {}
  for (const def of CUSTOM_PLAN_PACKS) {
    const count = normalized[def.packKey]
    if (count > 0) {
      wire[def.packKey] = count
    }
  }
  return wire
}

export function decodeCustomPlanIntentV2(
  raw: string | null | undefined,
): CustomPlanV2ConfigInput | null {
  if (raw == null || raw.trim() === "") {
    return null
  }
  try {
    let json = raw.trim()
    if (!json.startsWith("{")) {
      json = decodeURIComponent(escape(atob(json)))
    }
    const parsed = JSON.parse(json) as Record<string, unknown>
    if (Number(parsed.version) !== 2) {
      return null
    }
    const basePlan = parseBasePlanKey(
      typeof parsed.basePlan === "string" ? parsed.basePlan : null,
    )
    if (basePlan == null) {
      return null
    }
    const cadenceRaw =
      typeof parsed.cadence === "string" ? parsed.cadence.toLowerCase() : ""
    const cadence: BillingCadence =
      cadenceRaw === "annual" ? "annual" : "monthly"
    const packsRaw =
      parsed.packs != null && typeof parsed.packs === "object"
        ? (parsed.packs as Record<string, number>)
        : {}
    // A saved selection with a replaced pack is dropped, not converted: the
    // customer chooses single units again.
    if (hasLegacyCapacityPack(packsRaw)) {
      return null
    }
    const quoteHash =
      typeof parsed.quoteHash === "string" && parsed.quoteHash !== ""
        ? parsed.quoteHash
        : undefined
    return {
      version: 2,
      cadence,
      basePlan,
      // Not clamped and no default: a non-number count is rejected where the config is saved.
      locations:
        typeof parsed.locations === "number" ? parsed.locations : Number.NaN,
      packs: normalizePackCounts(packsRaw),
      ...(quoteHash != null ? { quoteHash } : {}),
    }
  } catch {
    return null
  }
}

/** JSON body for onboarding save. The server re-quotes and checks quoteHash. */
export function toOnboardingCustomPlanConfigJson(
  config: CustomPlanV2ConfigInput,
): string {
  return JSON.stringify({
    version: 2,
    cadence: config.cadence,
    basePlan: config.basePlan,
    locations: config.locations,
    packs: packCountsForWire(config.packs),
    ...(config.quoteHash ? { quoteHash: config.quoteHash } : {}),
  })
}
