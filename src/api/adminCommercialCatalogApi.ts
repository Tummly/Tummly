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

export type PublicCustomUnitRates = {
  catalogPublicId: string
  vatRateBps: number
  maxLocations: number
  unitRates: AdminCommercialCustomUnitRate[]
}

export async function fetchPublicCustomUnitRates(): Promise<PublicCustomUnitRates> {
  const { data } = await axiosInstance.get<
    Envelope<Record<string, unknown>>
  >("/public/commercial-catalog/custom-unit-rates")
  if (!data.success || !data.item) {
    throw new Error(data.message ?? "Catalog not ready.")
  }
  const item = data.item
  return {
    catalogPublicId: String(item.catalogPublicId ?? ""),
    vatRateBps: Number(item.vatRateBps ?? 0),
    maxLocations: Number(item.maxLocations ?? 500),
    unitRates: Array.isArray(item.unitRates)
      ? (item.unitRates as Record<string, unknown>[]).map((r) => ({
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
