import {
  GUEST_PROFILE_FEEDBACK_RECOVERY_PLACEHOLDER,
  GUEST_PROFILE_FEEDBACK_SOURCE_LABEL,
  OPERATOR_GUEST_FEEDBACKS_SORT_LABELS,
} from "@/lib/operatorGuestProfile/guestProfilePresentation"
import { formatGuestProfileFeedbackDateTime } from "@/lib/operatorGuestProfile/mapGuestProfileApiResponseToViewModel"
import { labelForDetectedTag } from "@/lib/operatorHome/detectedTags"
import { GUEST_FEEDBACKS_PAGE_SIZE } from "@/lib/operatorGuestProfile/guestFeedbacksListQueryParams"
import type { GuestFeedbacksListResponse } from "@/types/dashboard"
import type {
  OperatorGuestFeedbacksSortId,
  OperatorGuestFeedbacksTableEmptyStateKind,
  OperatorGuestFeedbacksViewModel,
  OperatorGuestProfileFeedbacksRow,
} from "@/types/operatorGuestProfile"

const FEEDBACK_COMMENT_PREVIEW_MAX_CHARS = 80

function truncateFeedbackComment(comment: string): string {
  const trimmed = comment.trim()
  if (trimmed.length <= FEEDBACK_COMMENT_PREVIEW_MAX_CHARS) {
    return trimmed
  }
  return `${trimmed.slice(0, FEEDBACK_COMMENT_PREVIEW_MAX_CHARS - 1)}…`
}

export function resolveGuestFeedbacksTableEmptyStateKind(input: {
  hasActiveQuery: boolean
  totalCount: number
}): OperatorGuestFeedbacksTableEmptyStateKind | null {
  if (input.totalCount > 0) {
    return null
  }
  return input.hasActiveQuery ? "filtered-empty" : "virgin-empty"
}

export function formatGuestFeedbacksPageRangeLabel(
  page: number,
  pageSize: number,
  totalCount: number
): string {
  if (totalCount === 0) {
    return "Showing 0 of 0 feedback"
  }

  const start = (page - 1) * pageSize + 1
  const end = Math.min(page * pageSize, totalCount)
  return `Showing ${start}–${end} of ${totalCount} feedback`
}

function mapRow(
  item: GuestFeedbacksListResponse["items"][number]
): OperatorGuestProfileFeedbacksRow {
  const succeeded = item.classificationStatus === "Succeeded"
  const fullComment = item.comment.trim()

  return {
    id: item.id,
    dateDisplay: formatGuestProfileFeedbackDateTime(item.createdAt),
    feedbackDisplay: truncateFeedbackComment(item.comment),
    feedbackFullDisplay: fullComment,
    classificationDisplay: succeeded ? item.sentiment : null,
    issueTagLabels: succeeded
      ? (item.detectedTags ?? []).map(labelForDetectedTag)
      : null,
    recoveryDisplay: GUEST_PROFILE_FEEDBACK_RECOVERY_PLACEHOLDER,
    locationDisplay: `${GUEST_PROFILE_FEEDBACK_SOURCE_LABEL} · ${item.locationName}`,
  }
}

export function mapGuestFeedbacksApiResponseToViewModel(input: {
  response: GuestFeedbacksListResponse
  sortId: OperatorGuestFeedbacksSortId
  hasActiveQuery: boolean
}): OperatorGuestFeedbacksViewModel {
  const { response, sortId, hasActiveQuery } = input
  const tableEmptyState = resolveGuestFeedbacksTableEmptyStateKind({
    hasActiveQuery,
    totalCount: response.totalCount,
  })

  return {
    tableRows: response.items.map(mapRow),
    tableEmptyState,
    totalCount: response.totalCount,
    sortLabel: OPERATOR_GUEST_FEEDBACKS_SORT_LABELS[sortId],
    pageSize: response.pageSize || GUEST_FEEDBACKS_PAGE_SIZE,
    currentPage: response.page,
    pageRangeLabel: formatGuestFeedbacksPageRangeLabel(
      response.page,
      response.pageSize || GUEST_FEEDBACKS_PAGE_SIZE,
      response.totalCount
    ),
    toolbarEnabled: !(tableEmptyState === "virgin-empty"),
  }
}
