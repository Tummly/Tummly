export type GuestPreferencesPageMode =
  | { kind: "link"; token: string }
  | { kind: "verify-email"; v: string }
  | { kind: "link-contact"; v: string }
  | { kind: "session" }
  | { kind: "entry" }

function nonEmptyParam(params: URLSearchParams, name: string): string | null {
  const value = params.get(name)
  return value !== null && value.length > 0 ? value : null
}

/**
 * Maps the URL to the page mode. Entry links in the URL win over a stored
 * session, so a fresh link always takes effect in this tab.
 */
export function resolveGuestPreferencesPageMode(
  params: URLSearchParams,
  hasSession: boolean
): GuestPreferencesPageMode {
  const token = nonEmptyParam(params, "t")
  if (token !== null) {
    return { kind: "link", token }
  }

  const verifyEmail = nonEmptyParam(params, "v")
  if (verifyEmail !== null) {
    return { kind: "verify-email", v: verifyEmail }
  }

  const linkContact = nonEmptyParam(params, "link")
  if (linkContact !== null) {
    return { kind: "link-contact", v: linkContact }
  }

  return hasSession ? { kind: "session" } : { kind: "entry" }
}
