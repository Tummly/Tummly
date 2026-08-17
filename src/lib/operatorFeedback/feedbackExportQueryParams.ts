import type { FeedbackInboxListQueryParams } from "@/lib/operatorFeedback/feedbackInboxListQueryParams"

export type FeedbackExportScope = "current" | "all-in-period"

export type FeedbackExportFormat = "xlsx" | "csv"

export type FeedbackExportQueryParams = {
  locationId: number
  from: string
  to: string
  scope: FeedbackExportScope
  format: FeedbackExportFormat
  includeGuestContact: boolean
  feedbackId?: number
  tab?: FeedbackInboxListQueryParams["tab"]
  q?: string
  sort?: FeedbackInboxListQueryParams["sort"]
  sentiment?: string[]
  detectedTags?: string[]
  qrSource?: string[]
  contact?: string[]
  datePreset?: string
  dateFrom?: string
  dateTo?: string
  utcOffsetMinutes?: number
}

export function buildFeedbackExportQueryParams(input: {
  inboxParams: FeedbackInboxListQueryParams
  scope: FeedbackExportScope
  format: FeedbackExportFormat
  includeGuestContact: boolean
}): FeedbackExportQueryParams {
  const { inboxParams, scope, format, includeGuestContact } = input
  const base: FeedbackExportQueryParams = {
    locationId: inboxParams.locationId,
    from: inboxParams.from,
    to: inboxParams.to,
    scope,
    format,
    includeGuestContact,
  }

  if (scope === "all-in-period") {
    return base
  }

  return {
    ...base,
    tab: inboxParams.tab,
    q: inboxParams.q,
    sort: inboxParams.sort,
    sentiment: inboxParams.sentiment,
    detectedTags: inboxParams.detectedTags,
    qrSource: inboxParams.qrSource,
    contact: inboxParams.contact,
    datePreset: inboxParams.datePreset,
    dateFrom: inboxParams.dateFrom,
    dateTo: inboxParams.dateTo,
    utcOffsetMinutes: inboxParams.utcOffsetMinutes,
  }
}
