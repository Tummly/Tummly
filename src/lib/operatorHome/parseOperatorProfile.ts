import {
  readOptionalNullableString,
  readString,
  unwrapDataObject,
} from "@/lib/apiEnvelope"

export type TeamPermissionsAccess = "none" | "view" | "manage"

export type BillingCreditsAccess = TeamPermissionsAccess

export type OffersAccess = TeamPermissionsAccess

export function parseTeamPermissionsAccess(
  raw: string | null | undefined
): TeamPermissionsAccess | null {
  if (raw === "none" || raw === "view" || raw === "manage") {
    return raw
  }
  return null
}

export const parseBillingCreditsAccess = parseTeamPermissionsAccess

export const parseOffersAccess = parseTeamPermissionsAccess

export interface OperatorProfile {
  fullName: string
  /** Nominated account email from `/auth/me` — used to prefill Send test. */
  email: string | null
  activationExpiresAt: string | null
  /** Self role from linked Trial Request; distinct from permission role. */
  selfRole: string | null
  teamPermissionsAccess: TeamPermissionsAccess
  billingCreditsAccess: BillingCreditsAccess
}

/** Profile fields from `/auth/me` for Operator Dashboard shell chrome. */
export function parseOperatorProfile(result: unknown): OperatorProfile | null {
  const data = unwrapDataObject(result)

  if (!data) {
    return null
  }

  const fullName = readString(data, "fullName")

  if (!fullName) {
    return null
  }

  const emailRaw = readOptionalNullableString(data, "email")
  const email =
    emailRaw != null && emailRaw.trim().length > 0 ? emailRaw.trim() : null

  const activationExpiresAt =
    readOptionalNullableString(data, "activationExpiresAt") ?? null

  const selfRole = readOptionalNullableString(data, "selfRole") ?? null
  // Omit means the session predates this field. Existing Account owners must
  // still see Team & permissions. Explicit "none" still hides the SideNav row.
  const accessRaw = readOptionalNullableString(data, "teamPermissionsAccess")
  const teamPermissionsAccess: TeamPermissionsAccess =
    parseTeamPermissionsAccess(accessRaw) ?? "manage"

  const billingAccessRaw = readOptionalNullableString(
    data,
    "billingCreditsAccess"
  )
  const billingCreditsAccess: BillingCreditsAccess =
    parseBillingCreditsAccess(billingAccessRaw) ?? "manage"

  return {
    fullName,
    email,
    activationExpiresAt,
    selfRole,
    teamPermissionsAccess,
    billingCreditsAccess,
  }
}
