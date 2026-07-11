import type { HelpCentreQueryDetail, HelpCentreQueryListItem } from "./helpCentre"

export type SupportQueryListItem = HelpCentreQueryListItem & {
  submitterName: string
  submitterEmail: string
  businessName: string
  queryLocationLabel?: string | null
  linkedOperator: boolean
  linkedOperatorEmail?: string | null
}

export type SupportQueryDetail = HelpCentreQueryDetail

export type SupportQuerySubmitterType = "operator" | "contact"

export type SupportQueriesListResponse = {
  queries: SupportQueryListItem[]
  totalCount: number
}

export type SupportQueriesListFilters = {
  status?: string
  topic?: string
  q?: string
  type?: SupportQuerySubmitterType
  page?: number
  pageSize?: number
}
