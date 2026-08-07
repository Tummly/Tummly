/** Campaigns list view ids — Figma tabs (no Awaiting approval). */
export type OperatorCampaignsListViewId =
  | "all"
  | "needs-attention"
  | "drafts"
  | "in-flight"
  | "sent"

export type OperatorCampaignsListTab = {
  id: OperatorCampaignsListViewId
  label: string
  count: number
  /** All tab omits the count badge in Figma. */
  showCount: boolean
}

export type OperatorCampaignsListEmptyStateKind =
  | "true-empty"
  | "view-scoped"
  | "filter-search"

export type CampaignsListTabCounts = {
  all: number
  needsAttention: number
  drafts: number
  inFlight: number
  sent: number
}

export type CampaignsListItem = {
  id: number
  name: string
  status: string
}

export type CampaignsListResponse = {
  success: boolean
  items: CampaignsListItem[]
  totalCount: number
  page: number
  pageSize: number
  tabCounts: CampaignsListTabCounts
}

export type CampaignsListQueryParams = {
  locationId: number
  view: OperatorCampaignsListViewId
  q?: string
  page?: number
  pageSize?: number
}
