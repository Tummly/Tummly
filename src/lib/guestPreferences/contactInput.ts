export type ContactKind = "email" | "uk-mobile"

/** Same shape as the backend email check (GuestPreferenceContact.TryParse). */
const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
/** UK only: +44 then ten digits, or 0 then ten digits. Spaces are ignored. */
const UK_MOBILE_PATTERN = /^(?:\+44\d{10}|0\d{10})$/
const MASK_SUFFIX = "•••••"

/**
 * Client mirror of the backend contact rule. `null` means the input is
 * rejected before any request is sent.
 */
export function classifyContact(raw: string): ContactKind | null {
  const trimmed = raw.trim()
  if (trimmed === "") {
    return null
  }
  if (EMAIL_PATTERN.test(trimmed.toLowerCase())) {
    return "email"
  }
  if (UK_MOBILE_PATTERN.test(trimmed.replace(/\s+/g, ""))) {
    return "uk-mobile"
  }
  return null
}

/**
 * Client mirror of GuestPreferenceContact.MaskEmail: the backend masks the
 * lowercased address, keeps min(2, length - 1) local-part characters, then
 * adds five dots and the domain.
 */
export function maskEmailAddress(raw: string): string {
  const email = raw.trim().toLowerCase()
  const at = email.indexOf("@")
  if (at < 0) {
    return MASK_SUFFIX
  }
  const local = email.slice(0, at)
  const visibleCount = Math.max(0, Math.min(2, local.length - 1))
  return local.slice(0, visibleCount) + MASK_SUFFIX + email.slice(at)
}
