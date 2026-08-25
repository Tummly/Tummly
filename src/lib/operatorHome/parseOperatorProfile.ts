import {
  readOptionalNullableString,
  readString,
  unwrapDataObject,
} from "@/lib/apiEnvelope"

export type TeamPermissionsAccess = "none" | "view" | "manage"

export function parseTeamPermissionsAccess(
  raw: string | null | undefined
): TeamPermissionsAccess | null {
  if (raw === "none" || raw === "view" || raw === "manage") {
    return raw
  }
  return null
}

export interface OperatorProfile {
  fullName: string
  /** Nominated account email from `/auth/me` — used to prefill Send test. */
  email: string | null
  activationExpiresAt: string | null
  /** Self role from linked Trial Request; distinct from permission role. */
  selfRole: string | null
  teamPermissionsAccess: TeamPermissionsAccess
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

  return {
    fullName,
    email,
    activationExpiresAt,
    selfRole,
    teamPermissionsAccess,
  }
}
