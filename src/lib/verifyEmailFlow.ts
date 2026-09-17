export function buildVerifyEmailPath(email: string) {
  const trimmed = email.trim()
  return `/verify-email?email=${encodeURIComponent(trimmed)}`
}

export function readVerifyEmail(
  searchParams: URLSearchParams,
  stateEmail?: string | null
): string | null {
  const fromQuery = searchParams.get("email")?.trim()
  if (fromQuery) return fromQuery.toLowerCase()
  const fromState = stateEmail?.trim()
  if (fromState) return fromState.toLowerCase()
  return null
}
