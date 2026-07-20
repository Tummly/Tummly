import type { OperatorHomeLocationOption } from "@/types/operatorHome"

/**
 * Client-side Owned-location filter for the Operator location switcher panel.
 * Blank/whitespace queries leave the list unchanged.
 */
export function filterOwnedLocations(
  locations: OperatorHomeLocationOption[],
  query: string
): OperatorHomeLocationOption[] {
  const normalized = query.trim().toLowerCase()
  if (normalized.length === 0) {
    return locations
  }

  return locations.filter((location) => {
    const name = location.name.toLowerCase()
    const address = location.address.toLowerCase()
    return name.includes(normalized) || address.includes(normalized)
  })
}
