import { parseApiInstantMs } from "@/lib/operatorHome/relativeTime"
import {
  OPERATOR_GUEST_DEFAULT_SORT_ID,
  OPERATOR_GUEST_OVERVIEW_KPIS,
  OPERATOR_GUEST_PAGE_SIZE,
  OPERATOR_GUEST_SMART_GROUP_TABS,
  OPERATOR_GUEST_SORT_LABELS,
} from "@/lib/operatorGuests/guestsPresentation"
import {
  countGuestsBySmartGroup,
  filterGuestsBySmartGroup,
} from "@/lib/operatorGuests/smartGroupPredicates"
import type {
  OperatorGuestFixture,
  OperatorGuestOverviewKpi,
  OperatorGuestSmartGroupId,
  OperatorGuestSortId,
  OperatorGuestTableRow,
  OperatorGuestsTableEmptyStateKind,
  OperatorGuestsViewModel,
} from "@/types/operatorGuests"

export type BuildOperatorGuestsViewModelInput = {
  guests: readonly OperatorGuestFixture[]
  activeSmartGroupId: OperatorGuestSmartGroupId
  sortId?: OperatorGuestSortId
  searchQuery?: string
  page?: number
  pageSize?: number
  nowMs?: number
}

function buildOverviewKpis(
  guests: readonly OperatorGuestFixture[],
  nowMs: number
): OperatorGuestOverviewKpi[] {
  const values: Record<
    OperatorGuestOverviewKpi["id"],
    number
  > = {
    "total-guests": guests.length,
    "marketing-eligible": guests.filter((guest) => guest.marketingEligible)
      .length,
    "needs-recovery": countGuestsBySmartGroup(
      guests,
      "needs-recovery",
      nowMs
    ),
  }

  return OPERATOR_GUEST_OVERVIEW_KPIS.map((kpi) => ({
    ...kpi,
    value: values[kpi.id],
  }))
}

function guestMatchesSearch(
  guest: OperatorGuestFixture,
  searchQuery: string
): boolean {
  const query = searchQuery.trim().toLowerCase()
  if (!query) {
    return true
  }

  return (
    guest.name.toLowerCase().includes(query) ||
    guest.email.toLowerCase().includes(query) ||
    (guest.mobile?.toLowerCase().includes(query) ?? false)
  )
}

function sortGuests(
  guests: OperatorGuestFixture[],
  sortId: OperatorGuestSortId
): OperatorGuestFixture[] {
  const sorted = [...guests]

  switch (sortId) {
    case "recent-activity":
      sorted.sort((a, b) => {
        const aMs =
          a.lastInteractionAt == null
            ? Number.NEGATIVE_INFINITY
            : parseApiInstantMs(a.lastInteractionAt)
        const bMs =
          b.lastInteractionAt == null
            ? Number.NEGATIVE_INFINITY
            : parseApiInstantMs(b.lastInteractionAt)
        return bMs - aMs
      })
      break
    case "newest-guests":
      sorted.sort(
        (a, b) =>
          parseApiInstantMs(b.capturedAt) - parseApiInstantMs(a.capturedAt)
      )
      break
    case "oldest-guests":
      sorted.sort(
        (a, b) =>
          parseApiInstantMs(a.capturedAt) - parseApiInstantMs(b.capturedAt)
      )
      break
    case "guest-name-az":
      sorted.sort((a, b) => a.name.localeCompare(b.name))
      break
    case "guest-name-za":
      sorted.sort((a, b) => b.name.localeCompare(a.name))
      break
    case "most-feedback-submissions":
      sorted.sort(
        (a, b) => b.feedbackSubmissionCount - a.feedbackSubmissionCount
      )
      break
    case "most-recent-redemption":
      sorted.sort((a, b) => {
        const aMs =
          a.offerRedeemedAt == null
            ? Number.NEGATIVE_INFINITY
            : parseApiInstantMs(a.offerRedeemedAt)
        const bMs =
          b.offerRedeemedAt == null
            ? Number.NEGATIVE_INFINITY
            : parseApiInstantMs(b.offerRedeemedAt)
        return bMs - aMs
      })
      break
    default:
      break
  }

  return sorted
}

function toTableRow(guest: OperatorGuestFixture): OperatorGuestTableRow {
  return {
    id: guest.id,
    name: guest.name,
    email: guest.email,
    mobile: guest.mobile,
    marketingStatusLabel: guest.marketingStatusLabel,
    locationName: guest.locationName,
    latestFeedbackSentiment: guest.latestFeedbackSentiment,
    feedbackSubmissionCount: guest.feedbackSubmissionCount,
    lastInteractionLabel: guest.lastInteractionLabel,
    lastInteractionAt: guest.lastInteractionAt,
  }
}

export function formatPageRangeLabel(
  page: number,
  pageSize: number,
  totalCount: number
): string {
  if (totalCount === 0) {
    return "Showing 0 of 0 guests"
  }

  const start = (page - 1) * pageSize + 1
  const end = Math.min(page * pageSize, totalCount)
  return `Showing ${start}–${end} of ${totalCount} guests`
}

/**
 * `scopeGuestCount` is guests in the active smart group before search/Filters.
 * Use that — not location-wide totals — so an empty tab (e.g. Needs recovery: 0)
 * shows "No guests yet" instead of "No guests found".
 */
export function resolveGuestsTableEmptyStateKind(
  scopeGuestCount: number,
  totalFilteredCount: number
): OperatorGuestsTableEmptyStateKind | null {
  if (scopeGuestCount === 0) {
    return "no-guests-yet"
  }

  if (totalFilteredCount === 0) {
    return "no-guests-found"
  }

  return null
}

export function buildOperatorGuestsViewModel(
  input: BuildOperatorGuestsViewModelInput
): OperatorGuestsViewModel {
  const nowMs = input.nowMs ?? Date.now()
  const sortId = input.sortId ?? OPERATOR_GUEST_DEFAULT_SORT_ID
  const searchQuery = input.searchQuery ?? ""
  const page = input.page ?? 1
  const pageSize = input.pageSize ?? OPERATOR_GUEST_PAGE_SIZE

  const smartGroupFiltered = filterGuestsBySmartGroup(
    input.guests,
    input.activeSmartGroupId,
    nowMs
  )
  const searched = smartGroupFiltered.filter((guest) =>
    guestMatchesSearch(guest, searchQuery)
  )
  const sorted = sortGuests(searched, sortId)
  const totalFilteredCount = sorted.length
  const pageStart = (page - 1) * pageSize
  const pageRows = sorted.slice(pageStart, pageStart + pageSize)
  const tableEmptyState = resolveGuestsTableEmptyStateKind(
    smartGroupFiltered.length,
    totalFilteredCount
  )

  return {
    overviewKpis: buildOverviewKpis(input.guests, nowMs),
    smartGroupTabs: OPERATOR_GUEST_SMART_GROUP_TABS.map((tab) => ({
      ...tab,
      count: countGuestsBySmartGroup(input.guests, tab.id, nowMs),
    })),
    activeSmartGroupId: input.activeSmartGroupId,
    tableRows: pageRows.map(toTableRow),
    tableEmptyState,
    totalFilteredCount,
    sortLabel: OPERATOR_GUEST_SORT_LABELS[sortId],
    pageSize,
    currentPage: page,
    pageRangeLabel: formatPageRangeLabel(page, pageSize, totalFilteredCount),
  }
}
