import type { OperatorOffersListEmptyStateKind } from "@/types/operatorCampaigns"

/**
 * Select Offers list empty chrome (ticket 03 / 20).
 * True-empty when All = 0. View-scoped when All > 0 but the active view has no rows.
 * Filter/search when All > 0, a query is active, and the page has no rows.
 */
export function resolveOffersListEmptyStateKind(input: {
  allCount: number
  filteredTotalCount: number
  hasActiveQuery: boolean
}): OperatorOffersListEmptyStateKind | null {
  if (input.filteredTotalCount > 0) {
    return null
  }

  if (input.allCount === 0) {
    return "true-empty"
  }

  if (input.hasActiveQuery) {
    return "filter-search"
  }

  return "view-scoped"
}

export function offersListSearchMissLabel(searchQuery: string): string | null {
  const trimmed = searchQuery.trim()
  if (trimmed.length === 0) {
    return null
  }
  return `No offers found for “${trimmed}”`
}
