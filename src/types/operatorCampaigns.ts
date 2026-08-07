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

/** Campaign Draft detail — create / get / PATCH response body (ticket 29). */
export type CampaignDraftDetail = {
  id: number
  locationId: number
  status: "draft"
  name: string
  goalId: string | null
  templateId: string | null
  templateVersion: number | null
  audienceKey: string | null
  channel: string | null
  offerStance: string | null
  messageSubject: string | null
  messageBody: string | null
  rowVersion: number
  createdAt: string
  updatedAt: string
}

export type CreateCampaignDraftRequest = {
  locationId: number
  name?: string
  goalId?: string | null
  templateId?: string | null
  templateVersion?: number | null
  audienceKey?: string | null
  channel?: string | null
  offerStance?: string | null
  messageSubject?: string | null
  messageBody?: string | null
}

export type PatchCampaignDraftRequest = {
  rowVersion: number
  name?: string
  goalId?: string | null
  templateId?: string | null
  templateVersion?: number | null
  audienceKey?: string | null
  channel?: string | null
  offerStance?: string | null
  messageSubject?: string | null
  messageBody?: string | null
}

export type CampaignDraftResponse = {
  success: boolean
  campaign: CampaignDraftDetail
}
