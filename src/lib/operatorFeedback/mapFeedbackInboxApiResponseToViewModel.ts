import { FEEDBACK_INBOX_PAGE_SIZE } from "@/lib/operatorFeedback/feedbackInboxListQueryParams"
import {
  feedbackInboxPageRangeLabel,
  OPERATOR_FEEDBACK_INBOX_SORT_LABELS,
  OPERATOR_FEEDBACK_INBOX_TAB_LABELS,
} from "@/lib/operatorFeedback/feedbackPresentation"
import { formatGuestProfileFeedbackDateTime } from "@/lib/operatorGuestProfile/mapGuestProfileApiResponseToViewModel"
import { labelForDetectedTag } from "@/lib/operatorHome/detectedTags"
import type { FeedbackInboxListResponse } from "@/types/dashboard"
import type {
  OperatorFeedbackInboxEmptyStateKind,
  OperatorFeedbackInboxSortId,
  OperatorFeedbackInboxTab,
  OperatorFeedbackInboxTabId,
  OperatorFeedbackInboxTableRow,
  OperatorFeedbackInboxViewModel,
} from "@/types/operatorFeedback"
import type { FilterChip } from "@/lib/operatorFilterSheet"

const FEEDBACK_COMMENT_PREVIEW_MAX_CHARS = 80

function truncateFeedbackComment(comment: string): string {
  const trimmed = comment.trim()
  if (trimmed.length <= FEEDBACK_COMMENT_PREVIEW_MAX_CHARS) {
    return trimmed
  }
  return `${trimmed.slice(0, FEEDBACK_COMMENT_PREVIEW_MAX_CHARS - 1)}…`
}

export function resolveFeedbackInboxEmptyStateKind(input: {
  hasActiveQuery: boolean
  totalCount: number
}): OperatorFeedbackInboxEmptyStateKind | null {
  if (input.totalCount > 0) {
    return null
  }
  return input.hasActiveQuery ? "no-match" : "true-empty"
}

function mapTabCounts(
  tabCounts: FeedbackInboxListResponse["tabCounts"]
): OperatorFeedbackInboxTab[] {
  const counts: Record<OperatorFeedbackInboxTabId, number> = {
    all: tabCounts.all,
    "needs-attention": tabCounts.needsAttention,
    new: tabCounts.new,
    "in-progress": tabCounts.inProgress,
    resolved: tabCounts.resolved,
  }

  return (
    Object.entries(OPERATOR_FEEDBACK_INBOX_TAB_LABELS) as Array<
      [OperatorFeedbackInboxTabId, string]
    >
  ).map(([id, label]) => ({
    id,
    label,
    count: counts[id],
  }))
}

function mapRow(
  item: FeedbackInboxListResponse["items"][number]
): OperatorFeedbackInboxTableRow {
  const succeeded = item.classificationStatus === "Succeeded"
  const fullComment = item.comment.trim()
  const qrSource = item.qrSource?.trim() ?? ""
  const locationSourceDisplay =
    qrSource.length > 0
      ? `${item.locationName} · ${qrSource}`
      : item.locationName

  return {
    id: item.id,
    feedbackPreview: truncateFeedbackComment(item.comment),
    feedbackFull: fullComment,
    sentiment: succeeded ? item.sentiment : null,
    issueTagLabels: succeeded
      ? (item.detectedTags ?? []).map(labelForDetectedTag)
      : null,
    guestName: item.guestName,
    locationSourceDisplay,
    submittedDisplay: formatGuestProfileFeedbackDateTime(item.createdAt),
    workflowStatus: item.workflowStatus,
    canReopen: item.workflowStatus === "resolved",
    canMarkNoActionNeeded: item.workflowStatus !== "resolved",
  }
}

export function mapFeedbackInboxApiResponseToViewModel(input: {
  response: FeedbackInboxListResponse
  sortId: OperatorFeedbackInboxSortId
  searchQuery: string
  filterChips: readonly FilterChip[]
  filterChipCount: number
  hasActiveQuery: boolean
}): OperatorFeedbackInboxViewModel {
  const { response, sortId, searchQuery, filterChips, filterChipCount } =
    input
  const pageSize = response.pageSize || FEEDBACK_INBOX_PAGE_SIZE
  const totalCount = response.totalCount
  const currentPage = response.page
  const from =
    totalCount === 0 ? 0 : (currentPage - 1) * pageSize + 1
  const to =
    totalCount === 0 ? 0 : Math.min(currentPage * pageSize, totalCount)

  return {
    tabs: mapTabCounts(response.tabCounts),
    tableRows: response.items.map(mapRow),
    tableEmptyState: resolveFeedbackInboxEmptyStateKind({
      hasActiveQuery: input.hasActiveQuery,
      totalCount,
    }),
    searchQuery,
    sortId,
    sortLabel: OPERATOR_FEEDBACK_INBOX_SORT_LABELS[sortId],
    filterChips,
    filterChipCount,
    pageRangeLabel: feedbackInboxPageRangeLabel(from, to, totalCount),
    canGoPrevious: currentPage > 1,
    canGoNext: currentPage * pageSize < totalCount,
    digitalGuestLinks: response.digitalGuestLinks.map((link) => ({
      id: link.id,
      linkName: link.linkName,
    })),
  }
}
