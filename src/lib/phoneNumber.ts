import {
  parsePhoneNumberFromString,
  type CountryCode,
} from "libphonenumber-js"

export const DEFAULT_PHONE_REGION: CountryCode = "GB"

export function tryNormalizePhoneToE164(
  value: string,
  defaultCountry: CountryCode = DEFAULT_PHONE_REGION
): string | null {
  const trimmed = value.trim()
  if (!trimmed) {
    return null
  }

  const parsed = parsePhoneNumberFromString(trimmed, defaultCountry)
  if (!parsed?.isValid()) {
    return null
  }

  return parsed.format("E.164")
}

export function normalizePhoneToE164(
  value: string,
  defaultCountry: CountryCode = DEFAULT_PHONE_REGION
): string {
  const normalized = tryNormalizePhoneToE164(value, defaultCountry)
  if (!normalized) {
    throw new Error("Please enter a valid phone number.")
  }

  return normalized
}

export function formatPhoneForDisplay(
  e164Value: string,
  defaultCountry: CountryCode = DEFAULT_PHONE_REGION
): string {
  const trimmed = e164Value.trim()
  const parsed = trimmed.startsWith("+")
    ? parsePhoneNumberFromString(trimmed)
    : parsePhoneNumberFromString(trimmed, defaultCountry)

  if (!parsed?.isValid()) {
    return trimmed
  }

  if (parsed.countryCallingCode === "44") {
    return parsed.formatNational()
  }

  return parsed.formatInternational()
}
