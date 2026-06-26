import {
  parsePhoneNumberFromString,
  type CountryCode,
  type PhoneNumber,
} from "libphonenumber-js"

export const DEFAULT_PHONE_REGION: CountryCode = "GB"

const UK_CALLING_CODE = "44"

function isUkPhoneNumber(parsed: PhoneNumber): boolean {
  return parsed.countryCallingCode === UK_CALLING_CODE
}

export function tryNormalizePhoneToE164(value: string): string | null {
  const trimmed = value.trim()
  if (!trimmed) {
    return null
  }

  const parsed = parsePhoneNumberFromString(trimmed, DEFAULT_PHONE_REGION)
  if (!parsed?.isValid() || !isUkPhoneNumber(parsed)) {
    return null
  }

  return parsed.format("E.164")
}

export function normalizePhoneToE164(value: string): string {
  const normalized = tryNormalizePhoneToE164(value)
  if (!normalized) {
    throw new Error("Please enter a valid UK phone number.")
  }

  return normalized
}

export function formatPhoneForDisplay(e164Value: string): string {
  const trimmed = e164Value.trim()
  const parsed = trimmed.startsWith("+")
    ? parsePhoneNumberFromString(trimmed)
    : parsePhoneNumberFromString(trimmed, DEFAULT_PHONE_REGION)

  if (!parsed?.isValid() || !isUkPhoneNumber(parsed)) {
    return trimmed
  }

  return parsed.formatNational()
}
