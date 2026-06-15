export type SetupAccountType = "Single" | "Multi"

export interface SetupTokenPrefill {
  email: string
  fullName: string
  businessName: string
  accountType: SetupAccountType
}

function readStringField(
  source: Record<string, unknown>,
  keys: string[]
): string {
  for (const key of keys) {
    const value = source[key]
    if (typeof value === "string" && value.trim()) {
      return value.trim()
    }
  }

  return ""
}

function normalizeAccountType(value: string): SetupAccountType | null {
  const normalized = value.trim().toLowerCase()

  if (normalized === "single") {
    return "Single"
  }

  if (normalized === "multi") {
    return "Multi"
  }

  return null
}

export function parseValidateSetupTokenResponse(
  payload: unknown
): SetupTokenPrefill | null {
  if (!payload || typeof payload !== "object") {
    return null
  }

  const envelope = payload as Record<string, unknown>
  const nested =
    envelope.data && typeof envelope.data === "object"
      ? (envelope.data as Record<string, unknown>)
      : envelope.Data && typeof envelope.Data === "object"
        ? (envelope.Data as Record<string, unknown>)
        : null

  if (!nested) {
    return null
  }

  const accountTypeRaw = readStringField(nested, ["accountType", "AccountType"])
  const accountType = normalizeAccountType(accountTypeRaw)

  if (!accountType) {
    return null
  }

  const email = readStringField(nested, ["email", "Email"])
  const fullName = readStringField(nested, ["fullName", "FullName"])
  const businessName = readStringField(nested, [
    "businessName",
    "BusinessName",
  ])

  if (!email) {
    return null
  }

  return {
    email,
    fullName,
    businessName,
    accountType,
  }
}

export function getSetupAccountPath(
  accountType: SetupAccountType,
  token: string
): string {
  const encodedToken = encodeURIComponent(token)
  const basePath =
    accountType === "Single"
      ? "/setup-account-single"
      : "/setup-account-multi"

  return `${basePath}?token=${encodedToken}`
}

export function getSetupTokenErrorMessage(error: unknown): string {
  if (
    error &&
    typeof error === "object" &&
    "response" in error &&
    error.response &&
    typeof error.response === "object" &&
    "data" in error.response
  ) {
    const data = error.response.data

    if (data && typeof data === "object" && "message" in data) {
      const message = data.message
      if (typeof message === "string" && message.trim()) {
        return message.trim()
      }
    }
  }

  if (error instanceof Error && error.message.trim()) {
    if (error.message.toLowerCase().includes("network error")) {
      return "Unable to reach the server. Check your connection and try again."
    }

    return error.message.trim()
  }

  return "This setup link is invalid or has expired."
}
