import {
  resolveGuestsTableEmptyStateKind,
  formatPageRangeLabel,
} from "@/lib/operatorGuests/buildGuestsViewModel"
import {
  OPERATOR_GUEST_OVERVIEW_KPIS,
  OPERATOR_GUEST_SMART_GROUP_TABS,
  OPERATOR_GUEST_SORT_LABELS,
} from "@/lib/operatorGuests/guestsPresentation"
import type { GuestsResponse } from "@/types/dashboard"
import type {
  GuestFeedbackSentiment,
  GuestMarketingStatusLabel,
  OperatorGuestSmartGroupId,
  OperatorGuestSortId,
  OperatorGuestsViewModel,
} from "@/types/operatorGuests"

export type MapGuestsApiResponseInput = {
  response: GuestsResponse
  activeSmartGroupId: OperatorGuestSmartGroupId
  sortId: OperatorGuestSortId
  /** Prior view model — used when the response omits overview / smart-group counts. */
  previous?: OperatorGuestsViewModel | null
}

export function mapGuestsApiResponseToViewModel(
  input: MapGuestsApiResponseInput
): OperatorGuestsViewModel {
  const { response, activeSmartGroupId, sortId, previous } = input

  const previousOverviewValues = Object.fromEntries(
    (previous?.overviewKpis ?? []).map((kpi) => [kpi.id, kpi.value])
  ) as Partial<
    Record<(typeof OPERATOR_GUEST_OVERVIEW_KPIS)[number]["id"], number>
  >
  const previousSmartGroupCounts = Object.fromEntries(
    (previous?.smartGroupTabs ?? []).map((tab) => [tab.id, tab.count])
  ) as Partial<Record<OperatorGuestSmartGroupId, number>>

  const overview = response.overview
  const smartGroupCounts = response.smartGroupCounts

  const overviewValues: Record<
    (typeof OPERATOR_GUEST_OVERVIEW_KPIS)[number]["id"],
    number
  > = {
    "total-guests":
      overview?.totalGuests ?? previousOverviewValues["total-guests"] ?? 0,
    "new-this-month":
      overview?.newThisMonth ?? previousOverviewValues["new-this-month"] ?? 0,
    "marketing-eligible":
      overview?.marketingEligible ??
      previousOverviewValues["marketing-eligible"] ??
      0,
    "needs-recovery":
      overview?.needsRecovery ?? previousOverviewValues["needs-recovery"] ?? 0,
  }

  const smartGroupCountFor = (id: OperatorGuestSmartGroupId): number =>
    smartGroupCounts?.[id] ?? previousSmartGroupCounts[id] ?? 0

  const tableEmptyState = resolveGuestsTableEmptyStateKind(
    smartGroupCountFor(activeSmartGroupId),
    response.totalFilteredCount
  )

  return {
    overviewKpis: OPERATOR_GUEST_OVERVIEW_KPIS.map((kpi) => ({
      ...kpi,
      value: overviewValues[kpi.id],
    })),
    smartGroupTabs: OPERATOR_GUEST_SMART_GROUP_TABS.map((tab) => ({
      ...tab,
      count: smartGroupCountFor(tab.id),
    })),
    activeSmartGroupId,
    tableRows: response.rows.map((row) => ({
      id: row.id,
      name: row.name,
      email: row.email ?? "",
      mobile: row.mobile,
      marketingStatusLabel: row.marketingStatus as GuestMarketingStatusLabel,
      locationName: row.locationName,
      latestFeedbackSentiment:
        row.latestFeedbackSentiment as GuestFeedbackSentiment,
      feedbackSubmissionCount: row.feedbackSubmissionCount,
      lastInteractionLabel: row.lastInteractionLabel,
      lastInteractionAt: row.lastInteractionAt,
    })),
    tableEmptyState,
    totalFilteredCount: response.totalFilteredCount,
    sortLabel: OPERATOR_GUEST_SORT_LABELS[sortId],
    pageSize: response.pageSize,
    currentPage: response.page,
    pageRangeLabel: formatPageRangeLabel(
      response.page,
      response.pageSize,
      response.totalFilteredCount
    ),
  }
}
