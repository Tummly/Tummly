import type { OperatorFilterSelection } from "@/lib/operatorFilterSheet"

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

/** Active Search location scope from View all navigation (`searchScope=all`). */
export function readGlobalSearchScopeParam(
  searchParams: URLSearchParams
): "current" | "all" {
  return searchParams.get("searchScope") === "all" ? "all" : "current"
}

/**
 * Merge location-scope "all" into an existing list filter selection.
 * Used when View all carried Search's widened authorised-locations scope.
 */
export function withAllLocationsFilter(
  applied: OperatorFilterSelection
): OperatorFilterSelection {
  return {
    ...applied,
    location: { kind: "location-scope", value: { kind: "all" } },
  }
}

export function stripGlobalSearchListParams(
  searchParams: URLSearchParams
): URLSearchParams {
  const next = new URLSearchParams(searchParams)
  next.delete("q")
  next.delete("searchScope")
  return next
}
