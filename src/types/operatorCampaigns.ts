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

/** Product-global campaign-template catalogue list item (ticket 21). */
export type CampaignTemplateListItem = {
  id: string
  version: number
  title: string
  description: string
  goalLabel: string
  audienceLabel: string
  channelLabel: string
  offerLabel: string
  suggestsGoal: boolean
  suggestsAudience: boolean
  suggestsChannel: boolean
  suggestsOffer: boolean
}

export type CampaignTemplateSuggestionDefaults = {
  goalId: string
  audienceKey: string
  channel: string
  offerStance: string
}

export type CampaignTemplateDetail = CampaignTemplateListItem & {
  suggestions: CampaignTemplateSuggestionDefaults
}

export type CampaignTemplatesListResponse = {
  success: boolean
  items: CampaignTemplateListItem[]
}

export type CampaignTemplateDetailResponse = {
  success: boolean
  template: CampaignTemplateDetail
}
