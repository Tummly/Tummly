import type {
  OperatorCampaignsListEmptyStateKind,
} from "@/types/operatorCampaigns"

/**
 * Select Campaigns list empty chrome.
 * True-empty when All = 0 (no search/filters needed — chrome is hidden).
 * View-scoped when All > 0 but the active view has no rows.
 * Filter/search when All > 0, a query is active, and the page has no rows.
 */
export function resolveCampaignsListEmptyStateKind(input: {
  allCount: number
  filteredTotalCount: number
  hasActiveQuery: boolean
}): OperatorCampaignsListEmptyStateKind | null {
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

export function campaignsListSearchMissLabel(
  searchQuery: string
): string | null {
  const trimmed = searchQuery.trim()
  if (trimmed.length === 0) {
    return null
  }
  return `No campaigns found for “${trimmed}”`
}
