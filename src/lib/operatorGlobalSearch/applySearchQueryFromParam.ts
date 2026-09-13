/**
 * Hitchhiker helper: read a non-empty Global Search `q` query param once.
 * Callers apply it via the list page module `setSearchQuery`, then replace the URL.
 */
export function readGlobalSearchQueryParam(
  searchParams: URLSearchParams
): string | null {
  const raw = searchParams.get("q")
  if (raw == null) {
    return null
  }
  const trimmed = raw.trim()
  return trimmed === "" ? null : raw
}

export function stripGlobalSearchListParams(
  searchParams: URLSearchParams
): URLSearchParams {
  const next = new URLSearchParams(searchParams)
  next.delete("q")
  next.delete("searchScope")
  return next
}
