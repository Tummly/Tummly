export type PlanEntitlementLimit = {
  cap: number
  current: number
  atCap: boolean
  available: boolean
}

export type PlanEntitlementsAccountSnapshot = {
  locations: PlanEntitlementLimit
  teamMembers: PlanEntitlementLimit
  activeOffers: PlanEntitlementLimit
}

export type PlanEntitlementsLocationSnapshot = {
  activeQrPlacements: PlanEntitlementLimit
  publishedGuestForms: PlanEntitlementLimit
  draftGuestForms: PlanEntitlementLimit
}

export type PlanEntitlementsSnapshot = {
  account: PlanEntitlementsAccountSnapshot
  location?: PlanEntitlementsLocationSnapshot | null
}

export function normalizePlanEntitlementLimit(
  raw: Partial<PlanEntitlementLimit> | null | undefined
): PlanEntitlementLimit {
  if (raw == null || raw.available === false) {
    return { cap: 0, current: 0, atCap: false, available: false }
  }
  const cap = raw.cap ?? 0
  const current = raw.current ?? 0
  return {
    cap,
    current,
    atCap: raw.atCap ?? current >= cap,
    available: true,
  }
}

export function normalizePlanEntitlementsAccount(
  raw: Record<string, unknown> | null | undefined
): PlanEntitlementsAccountSnapshot {
  const source = raw ?? {}
  return {
    locations: normalizePlanEntitlementLimit(
      readLimit(source, "locations")
    ),
    teamMembers: normalizePlanEntitlementLimit(
      readLimit(source, "teamMembers")
    ),
    activeOffers: normalizePlanEntitlementLimit(
      readLimit(source, "activeOffers")
    ),
  }
}

export function normalizePlanEntitlementsLocation(
  raw: Record<string, unknown> | null | undefined
): PlanEntitlementsLocationSnapshot | null {
  if (raw == null) {
    return null
  }
  return {
    activeQrPlacements: normalizePlanEntitlementLimit(
      readLimit(raw, "activeQrPlacements")
    ),
    publishedGuestForms: normalizePlanEntitlementLimit(
      readLimit(raw, "publishedGuestForms")
    ),
    draftGuestForms: normalizePlanEntitlementLimit(
      readLimit(raw, "draftGuestForms")
    ),
  }
}

function readLimit(
  source: Record<string, unknown>,
  key: string
): Partial<PlanEntitlementLimit> | undefined {
  const camel = source[key]
  if (camel != null && typeof camel === "object") {
    return camel as Partial<PlanEntitlementLimit>
  }
  const pascalKey = key.charAt(0).toUpperCase() + key.slice(1)
  const pascal = source[pascalKey]
  if (pascal != null && typeof pascal === "object") {
    return pascal as Partial<PlanEntitlementLimit>
  }
  return undefined
}

export function formatEntitlementUsage(limit: PlanEntitlementLimit): string {
  if (!limit.available || limit.cap < 1) {
    return ""
  }
  return `${limit.current} of ${limit.cap} used`
}

export function teamMemberCapReachedMessage(
  limit: PlanEntitlementLimit
): string {
  if (!limit.available) {
    return "Plan limits are unavailable right now."
  }
  return `Your plan includes ${limit.cap} team users. Remove a member or cancel a pending invite to add someone new.`
}

export function activeOfferCapReachedMessage(
  limit: PlanEntitlementLimit
): string {
  if (!limit.available) {
    return "Plan limits are unavailable right now."
  }
  return `Your plan includes ${limit.cap} active offers account-wide. Pause or archive an offer to activate another.`
}

export function activeQrCapReachedMessage(
  limit: PlanEntitlementLimit
): string {
  if (!limit.available) {
    return "Plan limits are unavailable right now."
  }
  return `Your plan includes up to ${limit.cap} active QR placements at this location. Pause one to activate another.`
}
