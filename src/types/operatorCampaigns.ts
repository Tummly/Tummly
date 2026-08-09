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

/** Figma Campaign table row — Draft projection (ticket 30). */
export type CampaignsListItem = {
  id: number
  name: string
  status: string
  goalId: string | null
  locationId: number
  locationName: string
  channel: string | null
  audienceKey: string | null
  offerStance: string | null
  updatedAt: string
  /** Null for Draft — no schedule/send yet. */
  sendDate: string | null
  /** Null for Draft — no delivery metrics. */
  delivery: string | null
  /** Null for Draft — no engagement metrics. */
  engagement: string | null
  /** Null for Draft — no redemptions. */
  redemptions: string | null
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

/** Static by-id Preview seed — not live eligibility (S6). */
export type CampaignTemplatePreviewChannelId = "email" | "sms"

export type CampaignTemplatePreviewOfferBlock = {
  title: string
  description: string
  redemptionCode: string
  expiryLabel: string
}

export type CampaignTemplatePreviewMessage = {
  channel: CampaignTemplatePreviewChannelId
  estimatedUsageLabel: string
  body: string
  subject: string | null
  offerBlock: CampaignTemplatePreviewOfferBlock | null
}

export type CampaignTemplatePreviewOfferLogicRow = {
  label: string
  value: string
}

export type CampaignTemplatePreviewPayload = {
  summary: {
    goal: string
    bestFor: string
    suggestedAudience: string
    suggestedChannel: string
    offer: string
  }
  suggestedChannels: CampaignTemplatePreviewChannelId[]
  messages: CampaignTemplatePreviewMessage[]
  offerLogic: CampaignTemplatePreviewOfferLogicRow[] | null
  eligibility: {
    emailCount: number
    smsCount: number
    totalUniqueGuests: number
  }
  suggestedTiming: string
  footerDisclaimer: string
}

export type CampaignTemplateDetail = CampaignTemplateListItem & {
  suggestions: CampaignTemplateSuggestionDefaults
  preview: CampaignTemplatePreviewPayload
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
  /** Attached Offers catalog id; null when No offer. */
  offerId: number | null
  messageSubject: string | null
  messageBody: string | null
  rowVersion: string
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
  offerId?: number | null
  messageSubject?: string | null
  messageBody?: string | null
}

export type PatchCampaignDraftRequest = {
  rowVersion: string
  name?: string
  goalId?: string | null
  templateId?: string | null
  templateVersion?: number | null
  audienceKey?: string | null
  channel?: string | null
  offerStance?: string | null
  offerId?: number | null
  messageSubject?: string | null
  messageBody?: string | null
}

export type CampaignDraftResponse = {
  success: boolean
  campaign: CampaignDraftDetail
}

/** Offers catalog definition — create / get (ticket 22). */
export type CatalogOfferDetail = {
  id: number
  locationId: number
  status: "active"
  offerType: string
  title: string
  description: string
  validity: string
  expiryDate: string | null
  discountPercentage: number | null
  discountAmount: number | null
  freeItemText: string | null
  purchaseRequirement: string | null
  minimumSpend: number | null
  additionalExclusions: string | null
  replacementItemText: string | null
  staffInstructions: string | null
  createdAt: string
  updatedAt: string
}

export type CatalogOfferResponse = {
  success: boolean
  offer: CatalogOfferDetail
}

/** Campaign recommendation allow-list (ticket 31 / ticket 11). */
export type CampaignRecommendationType =
  | "thank-recent-guests"
  | "re-engage"
  | "recovery-follow-up"
  | "none"

export type CampaignRecommendationEchoedCounts = {
  marketingEligible: number
  allGuests: number
  newGuests: number
  needsRecovery: number
  positiveFeedback: number
  dormantGuests: number
}

export type CampaignRecommendationDraftPrefill = {
  goalId: string
  audienceKey: string
  channel: string
  offerStance: string
  campaignName: string
  messageSubject: string | null
  messageBody: string
}

export type CampaignRecommendation = {
  type: CampaignRecommendationType
  title?: string | null
  opportunity?: string | null
  eligibleAudience?: string | null
  whyBullets?: string[] | null
  suggestedChannel?: "email" | "sms" | null
  estimatedUsage?: string | null
  echoedCounts?: CampaignRecommendationEchoedCounts | null
  draftPrefill?: CampaignRecommendationDraftPrefill | null
  locationName?: string | null
}

export type CampaignRecommendationRequest = {
  locationId: number
  overviewDatePreset: string
  from: string | null
  to: string | null
  refresh?: boolean
}

export type CampaignRecommendationResponse = {
  success: boolean
  recommendation?: CampaignRecommendation
  message?: string
  retryable?: boolean
}

export type PrepareCampaignMessageDraftApiRequest = {
  locationId: number
  channel: "email" | "sms"
  goalId: string
  audienceKey: string
  offerStance: string
  campaignName?: string | null
  tone: string
  includeNotes?: string | null
  mode: "prepare" | "rewrite_subject" | "rewrite_message"
  currentBody?: string | null
  currentSubject?: string | null
}

export type PrepareCampaignMessageDraftApiResponse = {
  success: boolean
  body?: string
  subject?: string | null
  channel?: "email" | "sms"
  message?: string
  retryable?: boolean
}
