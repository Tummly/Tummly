import { isAxiosError } from "axios"

import axiosInstance from "./axiosInstance"

export type AdminCommercialPlan = {
  id: string
  planKey: string
  displayName: string
  monthlyNetPence: number
  annualNetPence: number
  includedLocations: number
  includedTeamMembers: number
  activeOffersAccount: number
  activeQrPlacementsPerLocation: number
  publishedGuestFormsPerLocation: number
  draftGuestFormsPerLocation: number
  creditsMonthlyAi: number | null
  creditsMonthlyEmail: number | null
  creditsMonthlySms: number | null
  creditsOneTimeAi: number | null
  creditsOneTimeEmail: number | null
  creditsOneTimeSms: number | null
  lookupKeyMonthly: string | null
  lookupKeyAnnual: string | null
  revolutPlanVariationIdMonthly: string | null
  revolutPlanVariationIdAnnual: string | null
  selfServeMaxLocations: number | null
}

export type AdminCommercialTopUpPack = {
  id: string
  channel: string
  quantity: number
  netPence: number
  lookupKey: string
  approvalRequired: boolean
}

export type AdminCommercialExtraLocation = {
  id: string
  monthlyNetPence: number
  annualNetPence: number
  usersAdded: number
  creditsMonthlyAi: number
  creditsMonthlyEmail: number
  creditsMonthlySms: number
  lookupKeyMonthly: string | null
  lookupKeyAnnual: string | null
  revolutPlanVariationIdMonthly: string | null
  revolutPlanVariationIdAnnual: string | null
}

export type AdminCommercialCustomUnitRate = {
  id: string
  rateKey: string
  unitQuantity: number
  monthlyNetPence: number
  annualNetPence: number | null
}

export type AdminCommercialCatalogVersion = {
  id: string
  publicId: string
  status: string
  publishedAtUtc: string | null
  notes: string | null
  vatRateBps: number
  plans: AdminCommercialPlan[]
  topUpPacks: AdminCommercialTopUpPack[]
  extraLocation: AdminCommercialExtraLocation | null
  customUnitRates: AdminCommercialCustomUnitRate[]
}

type Envelope<T> = { success: boolean; item: T | null; message?: string }

function mapVersion(raw: Record<string, unknown>): AdminCommercialCatalogVersion {
  return {
    id: String(raw.id ?? ""),
    publicId: String(raw.publicId ?? ""),
    status: String(raw.status ?? ""),
    publishedAtUtc:
      raw.publishedAtUtc == null ? null : String(raw.publishedAtUtc),
    notes: raw.notes == null ? null : String(raw.notes),
    vatRateBps: Number(raw.vatRateBps ?? 0),
    plans: Array.isArray(raw.plans)
      ? (raw.plans as Record<string, unknown>[]).map((p) => ({
          id: String(p.id ?? ""),
          planKey: String(p.planKey ?? ""),
          displayName: String(p.displayName ?? ""),
          monthlyNetPence: Number(p.monthlyNetPence ?? 0),
          annualNetPence: Number(p.annualNetPence ?? 0),
          includedLocations: Number(p.includedLocations ?? 0),
          includedTeamMembers: Number(p.includedTeamMembers ?? 0),
          activeOffersAccount: Number(p.activeOffersAccount ?? 0),
          activeQrPlacementsPerLocation: Number(
            p.activeQrPlacementsPerLocation ?? 0
          ),
          publishedGuestFormsPerLocation: Number(
            p.publishedGuestFormsPerLocation ?? 0
          ),
          draftGuestFormsPerLocation: Number(
            p.draftGuestFormsPerLocation ?? 0
          ),
          creditsMonthlyAi:
            p.creditsMonthlyAi == null ? null : Number(p.creditsMonthlyAi),
          creditsMonthlyEmail:
            p.creditsMonthlyEmail == null
              ? null
              : Number(p.creditsMonthlyEmail),
          creditsMonthlySms:
            p.creditsMonthlySms == null ? null : Number(p.creditsMonthlySms),
          creditsOneTimeAi:
            p.creditsOneTimeAi == null ? null : Number(p.creditsOneTimeAi),
          creditsOneTimeEmail:
            p.creditsOneTimeEmail == null
              ? null
              : Number(p.creditsOneTimeEmail),
          creditsOneTimeSms:
            p.creditsOneTimeSms == null ? null : Number(p.creditsOneTimeSms),
          lookupKeyMonthly:
            p.lookupKeyMonthly == null ? null : String(p.lookupKeyMonthly),
          lookupKeyAnnual:
            p.lookupKeyAnnual == null ? null : String(p.lookupKeyAnnual),
          revolutPlanVariationIdMonthly:
            p.revolutPlanVariationIdMonthly == null
              ? null
              : String(p.revolutPlanVariationIdMonthly),
          revolutPlanVariationIdAnnual:
            p.revolutPlanVariationIdAnnual == null
              ? null
              : String(p.revolutPlanVariationIdAnnual),
          selfServeMaxLocations:
            p.selfServeMaxLocations == null
              ? null
              : Number(p.selfServeMaxLocations),
        }))
      : [],
    topUpPacks: Array.isArray(raw.topUpPacks)
      ? (raw.topUpPacks as Record<string, unknown>[]).map((p) => ({
          id: String(p.id ?? ""),
          channel: String(p.channel ?? ""),
          quantity: Number(p.quantity ?? 0),
          netPence: Number(p.netPence ?? 0),
          lookupKey: String(p.lookupKey ?? ""),
          approvalRequired: Boolean(p.approvalRequired),
        }))
      : [],
    extraLocation:
      raw.extraLocation == null
        ? null
        : (() => {
            const e = raw.extraLocation as Record<string, unknown>
            return {
              id: String(e.id ?? ""),
              monthlyNetPence: Number(e.monthlyNetPence ?? 0),
              annualNetPence: Number(e.annualNetPence ?? 0),
              usersAdded: Number(e.usersAdded ?? 0),
              creditsMonthlyAi: Number(e.creditsMonthlyAi ?? 0),
              creditsMonthlyEmail: Number(e.creditsMonthlyEmail ?? 0),
              creditsMonthlySms: Number(e.creditsMonthlySms ?? 0),
              lookupKeyMonthly:
                e.lookupKeyMonthly == null
                  ? null
                  : String(e.lookupKeyMonthly),
              lookupKeyAnnual:
                e.lookupKeyAnnual == null ? null : String(e.lookupKeyAnnual),
              revolutPlanVariationIdMonthly:
                e.revolutPlanVariationIdMonthly == null
                  ? null
                  : String(e.revolutPlanVariationIdMonthly),
              revolutPlanVariationIdAnnual:
                e.revolutPlanVariationIdAnnual == null
                  ? null
                  : String(e.revolutPlanVariationIdAnnual),
            }
          })(),
    customUnitRates: Array.isArray(raw.customUnitRates)
      ? (raw.customUnitRates as Record<string, unknown>[]).map((r) => ({
          id: String(r.id ?? ""),
          rateKey: String(r.rateKey ?? ""),
          unitQuantity: Number(r.unitQuantity ?? 1),
          monthlyNetPence: Number(r.monthlyNetPence ?? 0),
          annualNetPence:
            r.annualNetPence == null ? null : Number(r.annualNetPence),
        }))
      : [],
  }
}

async function getVersion(
  path: string
): Promise<AdminCommercialCatalogVersion | null> {
  const { data } = await axiosInstance.get<Envelope<Record<string, unknown>>>(
    path
  )
  if (!data.item) {
    return null
  }
  return mapVersion(data.item)
}

export async function fetchPublishedCommercialCatalog() {
  return getVersion("/admin/commercial-catalog/published")
}

export async function fetchDraftCommercialCatalog() {
  return getVersion("/admin/commercial-catalog/draft")
}

export async function ensureDraftCommercialCatalog() {
  const { data } = await axiosInstance.post<
    Envelope<Record<string, unknown>>
  >("/admin/commercial-catalog/draft/ensure")
  if (!data.success || !data.item) {
    throw new Error(data.message ?? "Could not create draft.")
  }
  return mapVersion(data.item)
}

export async function saveDraftCommercialCatalog(
  body: Partial<AdminCommercialCatalogVersion> & {
    plans?: AdminCommercialPlan[]
    topUpPacks?: AdminCommercialTopUpPack[]
    extraLocation?: AdminCommercialExtraLocation | null
    customUnitRates?: AdminCommercialCustomUnitRate[]
    notes?: string | null
    vatRateBps?: number
  }
) {
  const { data } = await axiosInstance.put<Envelope<Record<string, unknown>>>(
    "/admin/commercial-catalog/draft",
    body
  )
  if (!data.success || !data.item) {
    throw new Error(data.message ?? "Could not save draft.")
  }
  return mapVersion(data.item)
}

export async function publishCommercialCatalog(options?: {
  notes?: string
  publicId?: string
}) {
  const { data } = await axiosInstance.post<Envelope<Record<string, unknown>>>(
    "/admin/commercial-catalog/publish",
    options ?? {}
  )
  if (!data.success || !data.item) {
    throw new Error(data.message ?? "Could not publish.")
  }
  return mapVersion(data.item)
}

export type PublicCatalogPlanSummaryDto = {
  planKey: string
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

export type PublicExtraLocationSummaryDto = {
  usersAdded: number
  creditsMonthlyAi: number
  creditsMonthlyEmail: number
  creditsMonthlySms: number
}

export type PublicCatalogPackMetaDto = {
  packKey: string
  rateKey: string
  packSize: number
  /** Per-pack net pence from the catalogue; null when the rate is not set. */
  monthlyNetPence: number | null
  annualNetPence: number | null
}

export type PublicCustomUnitRates = {
  catalogPublicId: string
  vatRateBps: number
  maxLocations: number
  unitRates: AdminCommercialCustomUnitRate[]
  /** Paid plans from published catalog (contract §6.1). */
  plans: PublicCatalogPlanSummaryDto[]
  extraLocation: PublicExtraLocationSummaryDto | null
  packs: PublicCatalogPackMetaDto[]
}

function mapPublicPlan(
  raw: Record<string, unknown>,
): PublicCatalogPlanSummaryDto {
  return {
    planKey: String(raw.planKey ?? "").toLowerCase(),
    displayName: String(raw.displayName ?? ""),
    monthlyNetPence: Number(raw.monthlyNetPence ?? 0),
    annualNetPence: Number(raw.annualNetPence ?? 0),
    includedLocations: Number(raw.includedLocations ?? 0),
    includedTeamMembers: Number(raw.includedTeamMembers ?? 0),
    activeOffersAccount: Number(raw.activeOffersAccount ?? 0),
    activeQrPlacementsPerLocation: Number(
      raw.activeQrPlacementsPerLocation ?? 0,
    ),
    creditsMonthlyAi: Number(raw.creditsMonthlyAi ?? 0),
    creditsMonthlyEmail: Number(raw.creditsMonthlyEmail ?? 0),
    creditsMonthlySms: Number(raw.creditsMonthlySms ?? 0),
  }
}

function mapUnitRate(
  raw: Record<string, unknown>,
): AdminCommercialCustomUnitRate {
  return {
    id: String(raw.id ?? ""),
    rateKey: String(raw.rateKey ?? ""),
    unitQuantity: Number(raw.unitQuantity ?? 1),
    monthlyNetPence: Number(raw.monthlyNetPence ?? 0),
    annualNetPence:
      raw.annualNetPence == null ? null : Number(raw.annualNetPence),
  }
}

export async function fetchPublicCustomUnitRates(): Promise<PublicCustomUnitRates> {
  const { data } = await axiosInstance.get<
    Envelope<Record<string, unknown>>
  >("/public/commercial-catalog/custom-unit-rates")
  if (!data.success || !data.item) {
    throw new Error(data.message ?? "Catalog not ready.")
  }
  const item = data.item
  const extraRaw =
    item.extraLocation != null && typeof item.extraLocation === "object"
      ? (item.extraLocation as Record<string, unknown>)
      : null
  return {
    catalogPublicId: String(item.catalogPublicId ?? ""),
    vatRateBps: Number(item.vatRateBps ?? 0),
    maxLocations: Number(item.maxLocations ?? 500),
    unitRates: Array.isArray(item.unitRates)
      ? (item.unitRates as Record<string, unknown>[]).map(mapUnitRate)
      : [],
    plans: Array.isArray(item.plans)
      ? (item.plans as Record<string, unknown>[]).map(mapPublicPlan)
      : [],
    extraLocation:
      extraRaw == null
        ? null
        : {
            usersAdded: Number(extraRaw.usersAdded ?? 0),
            creditsMonthlyAi: Number(extraRaw.creditsMonthlyAi ?? 0),
            creditsMonthlyEmail: Number(extraRaw.creditsMonthlyEmail ?? 0),
            creditsMonthlySms: Number(extraRaw.creditsMonthlySms ?? 0),
          },
    packs: Array.isArray(item.packs)
      ? (item.packs as Record<string, unknown>[]).map((p) => ({
          packKey: String(p.packKey ?? ""),
          rateKey: String(p.rateKey ?? ""),
          packSize: Number(p.packSize ?? 0),
          monthlyNetPence:
            p.monthlyNetPence == null ? null : Number(p.monthlyNetPence),
          annualNetPence:
            p.annualNetPence == null ? null : Number(p.annualNetPence),
        }))
      : [],
  }
}

export type CustomPlanQuoteRequest = {
  cadence: "monthly" | "annual"
  basePlan: "starter" | "growth" | "group" | null
  locations: number
  packs: Record<string, number>
}

export type CustomPlanQuoteLine = {
  key: string
  label: string
  /** Line name without the quantity. */
  name: string
  quantity: number
  /** Net price of one unit (or one pack) at the quote cadence, from the server. */
  unitNetPence: number
  netPence: number
}

export type CustomPlanQuoteResult = {
  version: number
  catalogPublicId: string
  cadence: string
  basePlan: string
  locations: number
  packs: Record<string, number>
  quantities: {
    locations: number
    teamUsers: number
    qrPlacements: number
    qrPlacementsAccountExtra: number
    offers: number
    emailCredits: number
    smsCredits: number
    aiCredits: number
  }
  /**
   * Workspace-wide active-QR pool from the server: base allowance + 10 per
   * additional paid Group Location + purchased QR units. Not a per-Location limit.
   */
  workspaceQrPool: number
  monthlyNetPence: number
  periodNetPence: number
  lines: CustomPlanQuoteLine[]
  basePlanChanged: boolean
  requestedBasePlan: string | null
  storedAsPlan: string
  /** 32 lower-hex characters. Sent back at signup to detect a changed quote. */
  quoteHash: string
  vatRateBps: number
  /** VAT on the period net total, from the server. */
  vatAmountPence: number
  grossTotalPence: number
  /** Gross amount charged at each later renewal. */
  recurringGrossPence: number
  /** Gross amount due at checkout (signup: the first period in full). */
  dueNowGrossPence: number
  /** When the plan starts; signup: "on_payment_confirmed". */
  effectiveTiming: string
}

function mapQuoteResult(raw: Record<string, unknown>): CustomPlanQuoteResult {
  const quantitiesRaw =
    raw.quantities != null && typeof raw.quantities === "object"
      ? (raw.quantities as Record<string, unknown>)
      : {}
  const packsRaw =
    raw.packs != null && typeof raw.packs === "object"
      ? (raw.packs as Record<string, number>)
      : {}
  return {
    version: Number(raw.version ?? 2),
    catalogPublicId: String(raw.catalogPublicId ?? ""),
    cadence: String(raw.cadence ?? "monthly"),
    basePlan: String(raw.basePlan ?? ""),
    locations: Number(raw.locations ?? 0),
    packs: packsRaw,
    quantities: {
      locations: Number(quantitiesRaw.locations ?? 0),
      teamUsers: Number(quantitiesRaw.teamUsers ?? 0),
      qrPlacements: Number(quantitiesRaw.qrPlacements ?? 0),
      qrPlacementsAccountExtra: Number(
        quantitiesRaw.qrPlacementsAccountExtra ?? 0,
      ),
      offers: Number(quantitiesRaw.offers ?? 0),
      emailCredits: Number(quantitiesRaw.emailCredits ?? 0),
      smsCredits: Number(quantitiesRaw.smsCredits ?? 0),
      aiCredits: Number(quantitiesRaw.aiCredits ?? 0),
    },
    workspaceQrPool: Number(raw.workspaceQrPool ?? 0),
    monthlyNetPence: Number(raw.monthlyNetPence ?? 0),
    periodNetPence: Number(raw.periodNetPence ?? 0),
    lines: Array.isArray(raw.lines)
      ? (raw.lines as Record<string, unknown>[]).map((line) => ({
          key: String(line.key ?? ""),
          label: String(line.label ?? ""),
          name: String(line.name ?? line.label ?? ""),
          quantity: Number(line.quantity ?? 0),
          unitNetPence: Number(line.unitNetPence ?? 0),
          netPence: Number(line.netPence ?? 0),
        }))
      : [],
    basePlanChanged: Boolean(raw.basePlanChanged),
    requestedBasePlan:
      raw.requestedBasePlan == null ? null : String(raw.requestedBasePlan),
    storedAsPlan: String(raw.storedAsPlan ?? "Custom"),
    quoteHash: String(raw.quoteHash ?? ""),
    vatRateBps: Number(raw.vatRateBps ?? 0),
    vatAmountPence: Number(raw.vatAmountPence ?? 0),
    grossTotalPence: Number(raw.grossTotalPence ?? 0),
    recurringGrossPence: Number(raw.recurringGrossPence ?? 0),
    dueNowGrossPence: Number(raw.dueNowGrossPence ?? 0),
    effectiveTiming: String(raw.effectiveTiming ?? "on_payment_confirmed"),
  }
}

/** Anonymous server quote — contract §6.2. */
export async function quotePublicCustomPlan(
  body: CustomPlanQuoteRequest,
): Promise<CustomPlanQuoteResult> {
  type QuoteEnvelope = Envelope<Record<string, unknown>> & { code?: string }
  let data: QuoteEnvelope
  try {
    const response = await axiosInstance.post<QuoteEnvelope>(
      "/public/commercial-catalog/custom-plan/quote",
      body,
      { skipAuthRedirect: true },
    )
    data = response.data
  } catch (error) {
    // A 400 { success: false, code } rejects in axios; keep its body for the code.
    const responseBody = isAxiosError(error) ? error.response?.data : undefined
    if (responseBody == null || typeof responseBody !== "object") {
      throw error
    }
    data = responseBody as QuoteEnvelope
  }
  if (!data.success || !data.item) {
    const err = new Error(data.message ?? "Could not quote plan.") as Error & {
      code?: string
    }
    err.code = data.code
    throw err
  }
  return mapQuoteResult(data.item)
}
