import type { AdminCommercialCustomUnitRate } from "@/api/adminCommercialCatalogApi"
import type { BillingCadence } from "@/lib/operatorBillingCredits/managePlanPresentation"

export const CUSTOM_PLAN_MAX_LOCATIONS = 500
export const CUSTOM_PLAN_MAX_PACK_COUNT = 99
export const ANNUAL_FALLBACK_FACTOR = 0.85

export type CustomPlanBasePlanKey = "starter" | "growth" | "group"

export type CustomPlanPackKey =
  | "team_users_5"
  | "offers_5"
  | "qr_10"
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
  packSize: number
  /** Short title for pack steppers. */
  title: string
  channel?: "capacity" | "email" | "sms" | "ai"
}

/** Fixed pack catalogue — contract §1 (both sides). */
export const CUSTOM_PLAN_PACKS: readonly CustomPlanPackDef[] = [
  {
    packKey: "team_users_5",
    rateKey: "team_user",
    packSize: 5,
    title: "Additional team users",
    channel: "capacity",
  },
  {
    packKey: "offers_5",
    rateKey: "offer",
    packSize: 5,
    title: "Additional active Offers",
    channel: "capacity",
  },
  {
    packKey: "qr_10",
    rateKey: "qr_placement",
    packSize: 10,
    title: "Extra QR placements",
    channel: "capacity",
  },
  {
    packKey: "email_20k",
    rateKey: "email_credit",
    packSize: 20_000,
    title: "+20,000 Email/month",
    channel: "email",
  },
  {
    packKey: "email_50k",
    rateKey: "email_credit",
    packSize: 50_000,
    title: "+50,000 Email/month",
    channel: "email",
  },
  {
    packKey: "sms_500",
    rateKey: "sms_credit",
    packSize: 500,
    title: "+500 SMS/month",
    channel: "sms",
  },
  {
    packKey: "sms_1000",
    rateKey: "sms_credit",
    packSize: 1_000,
    title: "+1,000 SMS/month",
    channel: "sms",
  },
  {
    packKey: "sms_5000",
    rateKey: "sms_credit",
    packSize: 5_000,
    title: "+5,000 SMS/month",
    channel: "sms",
  },
  {
    packKey: "ai_500",
    rateKey: "ai_credit",
    packSize: 500,
    title: "+500 AI actions/month",
    channel: "ai",
  },
  {
    packKey: "ai_2000",
    rateKey: "ai_credit",
    packSize: 2_000,
    title: "+2,000 AI actions/month",
    channel: "ai",
  },
] as const

export type CustomPlanPackCounts = Record<CustomPlanPackKey, number>

export const EMPTY_PACK_COUNTS: CustomPlanPackCounts = {
  team_users_5: 0,
  offers_5: 0,
  qr_10: 0,
  email_20k: 0,
  email_50k: 0,
  sms_500: 0,
  sms_1000: 0,
  sms_5000: 0,
  ai_500: 0,
  ai_2000: 0,
}

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

export type PublicExtraLocationSummary = {
  usersAdded: number
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
}

export type CustomPlanResolvedQuantities = {
  locations: number
  teamUsers: number
  qrPlacements: number
  qrPlacementsAccountExtra: number
  offers: number
  emailCredits: number
  smsCredits: number
  aiCredits: number
}

export type CustomPlanLine = {
  key: string
  label: string
  quantity: number
  netPence: number
}

export type CustomPlanV2Estimate = {
  contactSales: boolean
  basePlan: CustomPlanBasePlanKey
  requestedBasePlan: CustomPlanBasePlanKey | null
  basePlanChanged: boolean
  locations: number
  additionalLocations: number
  packs: CustomPlanPackCounts
  quantities: CustomPlanResolvedQuantities
  monthlyNetPence: number
  periodNetPence: number
  lines: CustomPlanLine[]
  storedAsPlan: "Starter" | "Growth" | "Group" | "Custom"
}

function monthlyLineNetPence(
  rate: AdminCommercialCustomUnitRate,
  quantity: number,
): number {
  if (quantity <= 0) {
    return 0
  }
  const unitQty = Math.max(1, rate.unitQuantity)
  return Math.ceil((quantity * rate.monthlyNetPence) / unitQty)
}

function annualLineNetPence(
  rate: AdminCommercialCustomUnitRate,
  quantity: number,
): number {
  if (quantity <= 0) {
    return 0
  }
  const unitQty = Math.max(1, rate.unitQuantity)
  const annual = rate.annualNetPence
  if (annual != null && annual > 0) {
    return Math.ceil((quantity * annual) / unitQty)
  }
  return Math.round(monthlyLineNetPence(rate, quantity) * 12 * ANNUAL_FALLBACK_FACTOR)
}

/** Net pence for `quantity` natural units at the given rate row and cadence. */
export function lineNetPence(
  rate: AdminCommercialCustomUnitRate,
  quantity: number,
  cadence: BillingCadence = "monthly",
): number {
  return cadence === "annual"
    ? annualLineNetPence(rate, quantity)
    : monthlyLineNetPence(rate, quantity)
}

/** £1,010 / £12.50 — thousands grouped; pence shown only when not whole. */
export function formatPenceGbp(pence: number): string {
  const whole = pence % 100 === 0
  return `£${(pence / 100).toLocaleString("en-GB", {
    minimumFractionDigits: whole ? 0 : 2,
    maximumFractionDigits: whole ? 0 : 2,
  })}`
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
    next[def.packKey] = Math.max(
      0,
      Math.min(CUSTOM_PLAN_MAX_PACK_COUNT, Math.floor(raw)),
    )
  }
  return next
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

/**
 * Smallest of starter/growth/group whose IncludedLocations >= n, else group.
 * Contract §2.
 */
export function minBasePlanForLocations(
  locations: number,
  plans: PublicCatalogPlanSummary[],
): CustomPlanBasePlanKey {
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
    // Catalog not loaded yet: same bands as the seeded catalog.
    if (n <= 1) return "starter"
    if (n <= 3) return "growth"
    return "group"
  }
  for (const plan of paid) {
    if (plan.includedLocations >= n) {
      return plan.planKey
    }
  }
  return "group"
}

function findPlan(
  plans: PublicCatalogPlanSummary[],
  key: CustomPlanBasePlanKey,
): PublicCatalogPlanSummary | null {
  return plans.find((plan) => plan.planKey === key) ?? null
}

function rateByKey(
  rates: AdminCommercialCustomUnitRate[],
  rateKey: string,
): AdminCommercialCustomUnitRate | undefined {
  return rates.find((rate) => rate.rateKey === rateKey)
}

function planPeriodNetPence(
  plan: PublicCatalogPlanSummary,
  cadence: BillingCadence,
): number {
  if (cadence === "annual") {
    if (plan.annualNetPence > 0) {
      return plan.annualNetPence
    }
    return Math.round(plan.monthlyNetPence * 12 * ANNUAL_FALLBACK_FACTOR)
  }
  return plan.monthlyNetPence
}

export type EstimateCustomPlanV2Args = {
  cadence: BillingCadence
  /** Requested base; null = use minimum for locations. */
  requestedBasePlan: CustomPlanBasePlanKey | null
  locations: number
  packs: Partial<CustomPlanPackCounts> | Record<string, number>
  plans: PublicCatalogPlanSummary[]
  rates: AdminCommercialCustomUnitRate[]
  extraLocation?: PublicExtraLocationSummary | null
}

/** Client-side mirror of server quote maths (display only; server re-prices). */
export function estimateCustomPlanV2(
  args: EstimateCustomPlanV2Args,
): CustomPlanV2Estimate {
  const cadence = args.cadence
  const locations = Math.floor(args.locations)
  const packs = normalizePackCounts(args.packs)

  if (locations > CUSTOM_PLAN_MAX_LOCATIONS) {
    const minPlan = minBasePlanForLocations(
      CUSTOM_PLAN_MAX_LOCATIONS,
      args.plans,
    )
    return {
      contactSales: true,
      basePlan: minPlan,
      requestedBasePlan: args.requestedBasePlan,
      basePlanChanged: false,
      locations,
      additionalLocations: 0,
      packs,
      quantities: {
        locations,
        teamUsers: 0,
        qrPlacements: 0,
        qrPlacementsAccountExtra: 0,
        offers: 0,
        emailCredits: 0,
        smsCredits: 0,
        aiCredits: 0,
      },
      monthlyNetPence: 0,
      periodNetPence: 0,
      lines: [],
      storedAsPlan: "Custom",
    }
  }

  const clampedLocations = Math.max(1, locations)
  const minPlan = minBasePlanForLocations(clampedLocations, args.plans)
  // Location count alone sets the base package (mirrors CustomPlanQuoteService).
  const basePlan = minPlan

  const planRow = findPlan(args.plans, basePlan)
  const basePlanChanged =
    args.requestedBasePlan != null && args.requestedBasePlan !== basePlan

  const includedLocations = planRow?.includedLocations ?? 0
  const additionalLocations = Math.max(0, clampedLocations - includedLocations)
  const extra = args.extraLocation ?? {
    usersAdded: 0,
    creditsMonthlyAi: 0,
    creditsMonthlyEmail: 0,
    creditsMonthlySms: 0,
  }

  const teamFromPacks = packs.team_users_5 * 5
  const offersFromPacks = packs.offers_5 * 5
  const qrAccountExtra = packs.qr_10 * 10
  const emailFromPacks =
    packs.email_20k * 20_000 + packs.email_50k * 50_000
  const smsFromPacks =
    packs.sms_500 * 500 + packs.sms_1000 * 1_000 + packs.sms_5000 * 5_000
  const aiFromPacks = packs.ai_500 * 500 + packs.ai_2000 * 2_000

  const quantities: CustomPlanResolvedQuantities = {
    locations: clampedLocations,
    teamUsers:
      (planRow?.includedTeamMembers ?? 0) +
      teamFromPacks +
      additionalLocations * extra.usersAdded,
    qrPlacements: planRow?.activeQrPlacementsPerLocation ?? 0,
    qrPlacementsAccountExtra: qrAccountExtra,
    offers: (planRow?.activeOffersAccount ?? 0) + offersFromPacks,
    emailCredits:
      (planRow?.creditsMonthlyEmail ?? 0) +
      emailFromPacks +
      additionalLocations * extra.creditsMonthlyEmail,
    smsCredits:
      (planRow?.creditsMonthlySms ?? 0) +
      smsFromPacks +
      additionalLocations * extra.creditsMonthlySms,
    aiCredits:
      (planRow?.creditsMonthlyAi ?? 0) +
      aiFromPacks +
      additionalLocations * extra.creditsMonthlyAi,
  }

  const lines: CustomPlanLine[] = []
  let monthlyNetPence = 0
  let periodNetPence = 0

  if (planRow != null) {
    const monthlyBase = planRow.monthlyNetPence
    const periodBase = planPeriodNetPence(planRow, cadence)
    monthlyNetPence += monthlyBase
    periodNetPence += periodBase
    lines.push({
      key: `base:${basePlan}`,
      label: `${planRow.displayName} · ${includedLocations} included Location${includedLocations === 1 ? "" : "s"}`,
      quantity: 1,
      netPence: periodBase,
    })
  }

  const locationRate = rateByKey(args.rates, "location")
  if (additionalLocations > 0 && locationRate != null) {
    const monthlyExtra = lineNetPence(
      locationRate,
      additionalLocations,
      "monthly",
    )
    const periodExtra = lineNetPence(locationRate, additionalLocations, cadence)
    monthlyNetPence += monthlyExtra
    periodNetPence += periodExtra
    lines.push({
      key: "extra:location",
      label: `${additionalLocations} additional Location${additionalLocations === 1 ? "" : "s"}`,
      quantity: additionalLocations,
      netPence: periodExtra,
    })
  }

  for (const def of CUSTOM_PLAN_PACKS) {
    const count = packs[def.packKey]
    if (count <= 0) continue
    const rate = rateByKey(args.rates, def.rateKey)
    if (rate == null) continue
    const naturalQty = count * def.packSize
    const monthlyPack = lineNetPence(rate, naturalQty, "monthly")
    const periodPack = lineNetPence(rate, naturalQty, cadence)
    monthlyNetPence += monthlyPack
    periodNetPence += periodPack
    lines.push({
      key: `pack:${def.packKey}`,
      label: `${def.title} × ${count}`,
      quantity: count,
      netPence: periodPack,
    })
  }

  const plainCatalog =
    !packsHaveAny(packs) && additionalLocations === 0 && planRow != null

  return {
    contactSales: false,
    basePlan,
    requestedBasePlan: args.requestedBasePlan,
    basePlanChanged,
    locations: clampedLocations,
    additionalLocations,
    packs,
    quantities,
    monthlyNetPence,
    periodNetPence,
    lines,
    storedAsPlan: plainCatalog
      ? basePlanToChosenPlan(basePlan)
      : "Custom",
  }
}

export function packMonthlyNetPence(
  pack: CustomPlanPackDef,
  rates: AdminCommercialCustomUnitRate[],
): number {
  const rate = rateByKey(rates, pack.rateKey)
  if (rate == null) {
    return 0
  }
  return lineNetPence(rate, pack.packSize, "monthly")
}

/** Compact v2 intent for query / session / onboarding save. */
export function encodeCustomPlanIntentV2(config: CustomPlanV2ConfigInput): string {
  const packs: Record<string, number> = {}
  const normalized = normalizePackCounts(config.packs)
  for (const def of CUSTOM_PLAN_PACKS) {
    const count = normalized[def.packKey]
    if (count > 0) {
      packs[def.packKey] = count
    }
  }
  const payload: CustomPlanV2ConfigInput = {
    version: 2,
    cadence: config.cadence,
    basePlan: config.basePlan,
    locations: Math.max(1, Math.floor(config.locations)),
    packs,
  }
  return btoa(unescape(encodeURIComponent(JSON.stringify(payload))))
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
    const locations = Math.floor(Number(parsed.locations ?? 1))
    const packsRaw =
      parsed.packs != null && typeof parsed.packs === "object"
        ? (parsed.packs as Record<string, number>)
        : {}
    return {
      version: 2,
      cadence,
      basePlan,
      locations: Number.isFinite(locations) ? Math.max(1, locations) : 1,
      packs: normalizePackCounts(packsRaw),
    }
  } catch {
    return null
  }
}

/** JSON body for onboarding save (server re-quotes). */
export function toOnboardingCustomPlanConfigJson(
  config: CustomPlanV2ConfigInput,
): string {
  const packs: Record<string, number> = {}
  const normalized = normalizePackCounts(config.packs)
  for (const def of CUSTOM_PLAN_PACKS) {
    const count = normalized[def.packKey]
    if (count > 0) {
      packs[def.packKey] = count
    }
  }
  return JSON.stringify({
    version: 2,
    cadence: config.cadence,
    basePlan: config.basePlan,
    locations: Math.max(1, Math.floor(config.locations)),
    packs,
  })
}
