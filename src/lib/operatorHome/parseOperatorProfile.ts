import {
  readOptionalNullableString,
  readString,
  unwrapDataObject,
} from "@/lib/apiEnvelope"

export interface OperatorProfile {
  fullName: string
  activationExpiresAt: string | null
  /** Self role from linked Trial Request; distinct from permission role. */
  selfRole: string | null
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

  const activationExpiresAt =
    readOptionalNullableString(data, "activationExpiresAt") ?? null

  const selfRole = readOptionalNullableString(data, "selfRole") ?? null

  return {
    fullName,
    activationExpiresAt,
    selfRole,
  }
}
