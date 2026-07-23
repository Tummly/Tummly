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
}

export function mapGuestsApiResponseToViewModel(
  input: MapGuestsApiResponseInput
): OperatorGuestsViewModel {
  const { response, activeSmartGroupId, sortId } = input
  const overviewValues: Record<
    (typeof OPERATOR_GUEST_OVERVIEW_KPIS)[number]["id"],
    number
  > = {
    "total-guests": response.overview.totalGuests,
    "new-this-month": response.overview.newThisMonth,
    "marketing-eligible": response.overview.marketingEligible,
    "needs-recovery": response.overview.needsRecovery,
  }

  const tableEmptyState = resolveGuestsTableEmptyStateKind(
    response.smartGroupCounts[activeSmartGroupId] ?? 0,
    response.totalFilteredCount
  )

  return {
    overviewKpis: OPERATOR_GUEST_OVERVIEW_KPIS.map((kpi) => ({
      ...kpi,
      value: overviewValues[kpi.id],
    })),
    smartGroupTabs: OPERATOR_GUEST_SMART_GROUP_TABS.map((tab) => ({
      ...tab,
      count: response.smartGroupCounts[tab.id] ?? 0,
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
