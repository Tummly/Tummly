import {
  readOptionalNullableString,
  readString,
  unwrapDataObject,
} from "@/lib/apiEnvelope"

export type TeamPermissionsAccess = "none" | "view" | "manage"

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
  const accessRaw =
    readOptionalNullableString(data, "teamPermissionsAccess") ?? "none"
  const teamPermissionsAccess: TeamPermissionsAccess =
    accessRaw === "none" || accessRaw === "view" || accessRaw === "manage"
      ? accessRaw
      : "manage"

  return {
    fullName,
    email,
    activationExpiresAt,
    selfRole,
    teamPermissionsAccess,
  }
}
