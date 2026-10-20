import type { AdminCommercialCustomUnitRate } from "@/api/adminCommercialCatalogApi"
import type { BillingCadence } from "@/lib/operatorBillingCredits/managePlanPresentation"

export const CUSTOM_PLAN_MAX_LOCATIONS = 500
export const ANNUAL_FALLBACK_FACTOR = 0.85

export type CustomPlanQuantities = {
  locations: number
  teamUsers: number
  qrPlacements: number
  offers: number
  emailCredits: number
  smsCredits: number
  aiCredits: number
}

export type CustomPlanCreditChannel = "email" | "sms" | "ai"

export type CustomPlanCreditPackDef = {
  id: string
  channel: CustomPlanCreditChannel
  quantity: number
  /** Figma pack title, e.g. "+20,000 Email/month". */
  title: string
  /** Fallback monthly pack price (pence) when catalog rate is missing. */
  monthlyNetPenceFallback: number
}

/** Figma full calculator credit packs `6657:37518`. */
export const CUSTOM_PLAN_CREDIT_PACKS: readonly CustomPlanCreditPackDef[] = [
  {
    id: "email_20k",
    channel: "email",
    quantity: 20_000,
    title: "+20,000 Email/month",
    monthlyNetPenceFallback: 2400,
  },
  {
    id: "email_50k",
    channel: "email",
    quantity: 50_000,
    title: "+50,000 Email/month",
    monthlyNetPenceFallback: 5000,
  },
  {
    id: "sms_500",
    channel: "sms",
    quantity: 500,
    title: "+500 SMS/month",
    monthlyNetPenceFallback: 5000,
  },
  {
    id: "sms_1000",
    channel: "sms",
    quantity: 1_000,
    title: "+1,000 SMS/month",
    monthlyNetPenceFallback: 9000,
  },
  {
    id: "sms_5000",
    channel: "sms",
    quantity: 5_000,
    title: "+5,000 SMS/month",
    monthlyNetPenceFallback: 42_000,
  },
  {
    id: "ai_500",
    channel: "ai",
    quantity: 500,
    title: "+500 AI actions/month",
    monthlyNetPenceFallback: 1200,
  },
  {
    id: "ai_2000",
    channel: "ai",
    quantity: 2_000,
    title: "+2,000 AI actions/month",
    monthlyNetPenceFallback: 3400,
  },
] as const

/** Pack stepper counts keyed by `CUSTOM_PLAN_CREDIT_PACKS` id. */
export type CustomPlanCreditPackCounts = Record<string, number>

export const EMPTY_CREDIT_PACK_COUNTS: CustomPlanCreditPackCounts =
  Object.fromEntries(CUSTOM_PLAN_CREDIT_PACKS.map((pack) => [pack.id, 0]))

export function creditsFromPackCounts(
  counts: CustomPlanCreditPackCounts,
): Pick<CustomPlanQuantities, "emailCredits" | "smsCredits" | "aiCredits"> {
  let emailCredits = 0
  let smsCredits = 0
  let aiCredits = 0
  for (const pack of CUSTOM_PLAN_CREDIT_PACKS) {
    const n = Math.max(0, counts[pack.id] ?? 0)
    if (n <= 0) continue
    const total = n * pack.quantity
    if (pack.channel === "email") emailCredits += total
    else if (pack.channel === "sms") smsCredits += total
    else aiCredits += total
  }
  return { emailCredits, smsCredits, aiCredits }
}

export type CustomPlanLine = {
  rateKey: string
  label: string
  quantity: number
  netPence: number
}

const RATE_TO_QTY: Record<
  string,
  keyof CustomPlanQuantities | null
> = {
  location: "locations",
  team_user: "teamUsers",
  qr_placement: "qrPlacements",
  offer: "offers",
  email_credit: "emailCredits",
  sms_credit: "smsCredits",
  ai_credit: "aiCredits",
}

const LABELS: Record<string, string> = {
  location: "Locations",
  team_user: "Team users",
  qr_placement: "QR placements",
  offer: "Active Offers",
  email_credit: "Email credits",
  sms_credit: "SMS credits",
  ai_credit: "AI credits",
}

function monthlyLineNetPence(
  rate: AdminCommercialCustomUnitRate,
  quantity: number
): number {
  if (quantity <= 0) {
    return 0
  }
  const unitQty = Math.max(1, rate.unitQuantity)
  return Math.ceil((quantity * rate.monthlyNetPence) / unitQty)
}

function annualLineNetPence(
  rate: AdminCommercialCustomUnitRate,
  quantity: number
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
  cadence: BillingCadence = "monthly"
): number {
  return cadence === "annual"
    ? annualLineNetPence(rate, quantity)
    : monthlyLineNetPence(rate, quantity)
}

/** Monthly net pence for one pack — catalog unit rate when present, else Figma fallback. */
export function creditPackMonthlyNetPence(
  pack: CustomPlanCreditPackDef,
  rates: AdminCommercialCustomUnitRate[],
): number {
  const rateKey =
    pack.channel === "email"
      ? "email_credit"
      : pack.channel === "sms"
        ? "sms_credit"
        : "ai_credit"
  const rate = rates.find((row) => row.rateKey === rateKey)
  if (rate == null) {
    return pack.monthlyNetPenceFallback
  }
  return lineNetPence(rate, pack.quantity, "monthly")
}

export function buildCustomPlanLines(
  rates: AdminCommercialCustomUnitRate[],
  quantities: CustomPlanQuantities,
  cadence: BillingCadence = "monthly"
): CustomPlanLine[] {
  const lines: CustomPlanLine[] = []
  for (const rate of rates) {
    const qtyKey = RATE_TO_QTY[rate.rateKey]
    if (qtyKey == null) {
      continue
    }
    const quantity = quantities[qtyKey]
    const net = lineNetPence(rate, quantity, cadence)
    if (quantity <= 0 && rate.rateKey !== "location") {
      continue
    }
    lines.push({
      rateKey: rate.rateKey,
      label: LABELS[rate.rateKey] ?? rate.rateKey,
      quantity,
      netPence: net,
    })
  }
  return lines
}

export function estimateCustomPlanTotalPence(
  rates: AdminCommercialCustomUnitRate[],
  quantities: CustomPlanQuantities,
  cadence: BillingCadence
): { monthlyNetPence: number; displayNetPence: number; lines: CustomPlanLine[] } {
  const monthlyLines = buildCustomPlanLines(rates, quantities, "monthly")
  const monthlyNetPence = monthlyLines.reduce((sum, line) => sum + line.netPence, 0)
  if (cadence === "monthly") {
    return { monthlyNetPence, displayNetPence: monthlyNetPence, lines: monthlyLines }
  }
  const lines = buildCustomPlanLines(rates, quantities, "annual")
  const displayNetPence = lines.reduce((sum, line) => sum + line.netPence, 0)
  return { monthlyNetPence, displayNetPence, lines }
}

export function formatPenceGbp(pence: number): string {
  if (pence % 100 === 0) {
    return `£${pence / 100}`
  }
  return `£${(pence / 100).toFixed(2)}`
}

export const EMPTY_CUSTOM_PLAN_QUANTITIES: CustomPlanQuantities = {
  locations: 0,
  teamUsers: 0,
  qrPlacements: 0,
  offers: 0,
  emailCredits: 0,
  smsCredits: 0,
  aiCredits: 0,
}

export function encodeCustomPlanIntent(
  quantities: CustomPlanQuantities,
  cadence: BillingCadence,
  catalogPublicId: string,
  rates: AdminCommercialCustomUnitRate[]
): string {
  const { monthlyNetPence, displayNetPence, lines } = estimateCustomPlanTotalPence(
    rates,
    quantities,
    cadence
  )
  const payload = {
    catalogPublicId,
    cadence,
    quantities,
    monthlyNetPence,
    periodNetPence: displayNetPence,
    rateSnapshot: rates,
    lines,
  }
  return btoa(unescape(encodeURIComponent(JSON.stringify(payload))))
}
