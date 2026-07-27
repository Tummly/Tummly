import {
  GUEST_PROFILE_FEEDBACK_SOURCE_LABEL,
  OPERATOR_GUEST_ACTIVITY_SORT_LABELS,
} from "@/lib/operatorGuestProfile/guestProfilePresentation"
import { formatGuestProfileAbsoluteDateTime } from "@/lib/operatorGuestProfile/mapGuestProfileApiResponseToViewModel"
import { GUEST_ACTIVITY_PAGE_SIZE } from "@/lib/operatorGuestProfile/guestActivityListQueryParams"
import type { GuestActivityListItem, GuestActivityListResponse } from "@/types/dashboard"
import type {
  OperatorGuestActivitySortId,
  OperatorGuestActivityTimelineEmptyStateKind,
  OperatorGuestActivityViewModel,
  OperatorGuestProfileActivityRow,
} from "@/types/operatorGuestProfile"

const CHANGED_FIELD_LABELS: Record<string, string> = {
  name: "Name",
  email: "Email",
  phone: "Phone",
  mobile: "Phone",
}

function humanizeChangedField(field: string): string {
  const key = field.trim().toLowerCase()
  if (CHANGED_FIELD_LABELS[key] != null) {
    return CHANGED_FIELD_LABELS[key]
  }
  if (field.length === 0) {
    return field
  }
  return field.charAt(0).toUpperCase() + field.slice(1)
}

function humanizeSentiment(sentiment: string): string {
  const lower = sentiment.trim().toLowerCase()
  if (lower === "positive") return "Positive"
  if (lower === "neutral") return "Neutral"
  if (lower === "negative") return "Negative"
  return humanizeChangedField(sentiment)
}

export function resolveGuestActivityTimelineEmptyStateKind(input: {
  hasActiveFilters: boolean
  totalCount: number
}): OperatorGuestActivityTimelineEmptyStateKind | null {
  if (input.totalCount > 0) {
    return null
  }
  return input.hasActiveFilters ? "filtered-empty" : "virgin-empty"
}

export function formatGuestActivityPageRangeLabel(
  page: number,
  pageSize: number,
  totalCount: number
): string {
  if (totalCount === 0) {
    return "Showing 0 of 0 activity"
  }

  const start = (page - 1) * pageSize + 1
  const end = Math.min(page * pageSize, totalCount)
  return `Showing ${start}–${end} of ${totalCount} activity`
}

export function mapGuestActivityItemToRow(
  item: GuestActivityListItem
): OperatorGuestProfileActivityRow {
  const datetime = formatGuestProfileAbsoluteDateTime(item.occurredAt)
  const metaDisplay = `${item.locationName} · ${datetime}`

  switch (item.kind) {
    case "guest-joined":
      return {
        id: item.id,
        headline: "Guest joined",
        body: `Guest joined through the ${GUEST_PROFILE_FEEDBACK_SOURCE_LABEL}.`,
        metaDisplay,
      }
    case "feedback": {
      const sentiment =
        item.sentiment != null && item.sentiment.trim().length > 0
          ? humanizeSentiment(item.sentiment)
          : null
      return {
        id: item.id,
        headline: "Feedback received",
        body:
          sentiment != null
            ? `${sentiment} feedback received through the ${GUEST_PROFILE_FEEDBACK_SOURCE_LABEL}.`
            : `Feedback received through the ${GUEST_PROFILE_FEEDBACK_SOURCE_LABEL}.`,
        metaDisplay,
      }
    }
    case "classification-succeeded": {
      const sentiment =
        item.sentiment != null && item.sentiment.trim().length > 0
          ? humanizeSentiment(item.sentiment)
          : null
      return {
        id: item.id,
        headline: "Classification succeeded",
        body:
          sentiment != null
            ? `Feedback classified as ${sentiment}.`
            : "Feedback classified.",
        metaDisplay,
      }
    }
    case "classification-failed":
      return {
        id: item.id,
        headline: "Classification failed",
        body: "Feedback classification failed.",
        metaDisplay,
      }
    case "note-added": {
      const author = item.authorDisplayName?.trim()
      return {
        id: item.id,
        headline: "Note added",
        body:
          author != null && author.length > 0
            ? `Note added by ${author}.`
            : "Note added.",
        metaDisplay,
      }
    }
    case "note-deleted": {
      const author = item.authorDisplayName?.trim()
      return {
        id: item.id,
        headline: "Note deleted",
        body:
          author != null && author.length > 0
            ? `Note deleted by ${author}.`
            : "Note deleted.",
        metaDisplay,
      }
    }
    case "tag-applied": {
      const tagName = item.tagName?.trim()
      return {
        id: item.id,
        headline: "Tag applied",
        body:
          tagName != null && tagName.length > 0
            ? `Tag “${tagName}” applied.`
            : "Tag applied.",
        metaDisplay,
      }
    }
    case "tag-removed": {
      const tagName = item.tagName?.trim()
      return {
        id: item.id,
        headline: "Tag removed",
        body:
          tagName != null && tagName.length > 0
            ? `Tag “${tagName}” removed.`
            : "Tag removed.",
        metaDisplay,
      }
    }
    case "profile-edited": {
      const fields = (item.changedFields ?? [])
        .map(humanizeChangedField)
        .filter((label) => label.length > 0)
      return {
        id: item.id,
        headline: "Profile updated",
        body:
          fields.length > 0
            ? `Profile details updated (${fields.join(", ")}).`
            : "Profile details updated.",
        metaDisplay,
      }
    }
    default:
      return {
        id: item.id,
        headline: item.kind,
        body: item.kind,
        metaDisplay,
      }
  }
}

export function mapGuestActivityApiResponseToViewModel(input: {
  response: GuestActivityListResponse
  sortId: OperatorGuestActivitySortId
  hasActiveFilters: boolean
}): OperatorGuestActivityViewModel {
  const { response, sortId, hasActiveFilters } = input
  const timelineEmptyState = resolveGuestActivityTimelineEmptyStateKind({
    hasActiveFilters,
    totalCount: response.totalCount,
  })

  return {
    timelineRows: response.items.map(mapGuestActivityItemToRow),
    timelineEmptyState,
    totalCount: response.totalCount,
    sortLabel: OPERATOR_GUEST_ACTIVITY_SORT_LABELS[sortId],
    pageSize: response.pageSize || GUEST_ACTIVITY_PAGE_SIZE,
    currentPage: response.page,
    pageRangeLabel: formatGuestActivityPageRangeLabel(
      response.page,
      response.pageSize || GUEST_ACTIVITY_PAGE_SIZE,
      response.totalCount
    ),
    toolbarEnabled: !(timelineEmptyState === "virgin-empty"),
  }
}
