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

export type OperatorCampaignsSortId =
  | "recent-activity"
  | "send-date"
  | "name-az"

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
  createdByUserId?: number | null
  createdByDisplayName?: string | null
  updatedAt: string
  /** Base64 SQL rowversion for list lifecycle actions (ticket 30). */
  rowVersion: string
  /** Null for Draft — no schedule/send yet. */
  sendDate: string | null
  /** Null for Draft — no delivery metrics. */
  delivery: string | null
  /** Null for Draft — no engagement metrics. */
  engagement: string | null
  /** Null for Draft — no redemptions. */
  redemptions: string | null
}

export type CampaignsCreatedByOption = {
  id: number
  label: string
}

export type CampaignsListFilterCatalog = {
  createdBy: CampaignsCreatedByOption[]
}

export type CampaignsListResponse = {
  success: boolean
  items: CampaignsListItem[]
  totalCount: number
  page: number
  pageSize: number
  tabCounts: CampaignsListTabCounts
  filterCatalog?: CampaignsListFilterCatalog
}

/** Overview sibling summary KPIs (ticket 29) — in-flight + messages accepted. */
export type CampaignsSummaryDetail = {
  campaignsInFlightScheduled: number
  campaignsInFlightSending: number
  messagesSentAccepted: number
  messagesSentAcceptedEmail: number
}

export type CampaignsSummaryResponse = {
  success: boolean
  summary: CampaignsSummaryDetail
}

export type CampaignsSummaryQueryParams = {
  locationId: number
  /** UTC inclusive start — omit with overviewDateTo for all-time. */
  overviewDateFrom?: string
  /** UTC exclusive end. */
  overviewDateTo?: string
}

export type CampaignsListQueryParams = {
  locationId: number
  view: OperatorCampaignsListViewId
  q?: string
  sort?: OperatorCampaignsSortId
  page?: number
  pageSize?: number
  status?: string[]
  channel?: string[]
  goalId?: string[]
  offerStance?: string[]
  createdBy?: number[]
  deliveryIssue?: string[]
  dateAxis?: string
  datePreset?: string
  dateFrom?: string
  dateTo?: string
  locationScope?: "all"
  locationIds?: number[]
  utcOffsetMinutes?: number
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

/** Campaign detail — create / get / PATCH response body (ticket 29 / 27). */
export type CampaignDraftDetail = {
  id: number
  locationId: number
  /** Lifecycle status — Preview loads any status; PATCH stays Draft-only. */
  status: string
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

/** Campaign eligibility estimate — GET /campaigns/eligibility (ticket 21). */
export type CampaignEligibilityExcludedReason = {
  reason: string
  count: number
}

export type CampaignEligibilityDetail = {
  audienceKey: string
  evaluable: boolean
  matched: number | null
  currentlyEligible: number | null
  excluded: number | null
  emailEligible: number | null
  smsEligible: number | null
  excludedReasons: CampaignEligibilityExcludedReason[]
  checkSetVersion: string
  evaluatedAt: string
}

export type CampaignEligibilityResponse = {
  success: boolean
  eligibility: CampaignEligibilityDetail
}

/** Offers catalog definition status on the wire (effective / badge). */
export type CatalogOfferStatus =
  | "draft"
  | "active"
  | "paused"
  | "expired"
  | "archived"

/** Offers catalog definition — create / get / list (ticket 22). */
export type CatalogOfferDetail = {
  id: number
  locationId: number
  status: CatalogOfferStatus
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
  /** Count of OfferIssue rows — soft-confirm gate for benefit/validity edits. */
  issueCount: number
  createdAt: string
  updatedAt: string
}

export type CatalogOfferResponse = {
  success: boolean
  offer: CatalogOfferDetail
}

export type OperatorOffersListViewId =
  | "all"
  | "needs-attention"
  | "drafts"
  | "in-flight"
  | "sent"

export type OperatorOffersListTab = {
  id: OperatorOffersListViewId
  label: string
  count: number
  /** All tab omits the count badge in Figma. */
  showCount: boolean
}

export type OperatorOffersListEmptyStateKind =
  | "true-empty"
  | "view-scoped"
  | "filter-search"

export type OperatorOffersSortId = "recent-activity" | "title-az"

export type CatalogOffersListTabCounts = {
  all: number
  needsAttention: number
  drafts: number
  inFlight: number
  sent: number
}

export type CatalogOffersListItem = {
  id: number
  locationId: number
  title: string
  status: CatalogOfferStatus
  offerType: string
  validity: string
  expiryDate: string | null
  attachKinds: string[]
  description?: string | null
  lifetimeClaims?: number
  lifetimeRedeemed?: number
  createdAt: string
  updatedAt: string
}

/** GET /api/offers/performance — Main Offers Performance KPIs ([from, to)). */
export type OffersPerformanceResponse = {
  success: boolean
  activeOffers: number
  offersIssued: number
  claims: number
  redemptions: number
  /** Redemptions ÷ Claims (0–1), or null when claims = 0. */
  claimToRedemptionRate: number | null
}

/** GET /api/offers/{id}/metrics — Details Overview KPIs ([from, to)). */
export type OfferMetricsResponse = {
  success: boolean
  claims: number
  redemptions: number
  /** Redemptions ÷ Claims (0–1), or null when claims = 0. */
  redemptionRate: number | null
  expiredUnused: number
  failedAttempts: number
}

/** POST /api/offers/redeem/check — Staff Redeem Check offer (ticket 38). */
export type StaffRedeemCheckFailureReasonApi =
  | "invalid"
  | "expired"
  | "already_used"
  | "voided"
  | "wrong_location"

export type StaffRedeemConfirmPreviewApi = {
  issueId: string
  offerTitle: string
  guestName: string
  validAt: string
  expires: string
  usage: string
  staffInstruction: string
}

export type StaffRedeemCheckApiResponse =
  | { success: true; preview: StaffRedeemConfirmPreviewApi }
  | { success: false; reason: StaffRedeemCheckFailureReasonApi }

/** POST /api/offers/redeem — Staff Redeem Mark as redeemed (ticket 38). */
export type StaffRedeemMarkApiResponse =
  | { success: true }
  | { success: false; reason?: string }

/** Offers void request APIs (ticket 39). */
export type VoidRequestDetailApi = {
  requestId: string
  passId: string
  offerId: number
  locationId: number
  offerTitle: string
  guestName: string
  passCodeMasked: string
  currentStateText: string
  expiresText: string
  locationName: string
  linkedCampaignText: string
  requestedByText: string
  requestedAtText: string
  reasonId: string
  reasonText: string
  explanation: string | null
  correctionId: string
  correctionText: string
}

export type OpenVoidAttentionOfferApi = {
  offerId: number
  offerTitle: string
  pendingCount: number
}

export type CreateVoidRequestApiResponse =
  | { success: true; requestId: string }
  | { success: false; reason?: "pending_exists" | "not_redeemed" | string }

export type VoidRequestDetailApiResponse =
  | { success: true; request: VoidRequestDetailApi }
  | { success: false; message?: string }

export type OpenVoidAttentionApiResponse = {
  success: boolean
  items: OpenVoidAttentionOfferApi[]
}

/** GET /api/offers/{id}/claims — Details Claims tab (ticket 40). */
export type OfferDetailsClaimListItemApi = {
  id: string
  guestName: string
  guestId: number | null
  claimCode: string
  claimedAtUtc: string | null
  issuedAtUtc: string
  source: string
  sourceLabel: string
  campaignName: string | null
  locationName: string
  expiryAtUtc: string
  status: string
  statusLabel: string
  passCodeMasked: string
  offerTitle: string
  linkedCampaignText: string | null
}

export type OfferDetailsClaimsListResponse = {
  success: boolean
  items: OfferDetailsClaimListItemApi[]
}

/** GET /api/offers/{id}/redemptions — Details Redemptions tab (ticket 40).
 *  GET /api/offers/redemptions?locationId= — location-wide log (ticket 42).
 */
export type OfferDetailsRedemptionListItemApi = {
  id: string
  kind: "redeemed" | "failed"
  dateTimeUtc: string
  guestName: string
  guestId: number | null
  passReferenceText: string
  passId: string
  passCodeMasked: string
  locationName: string
  staffMemberText: string | null
  outcome: string
  outcomeLabel: string
  reason: string | null
  reasonLabel: string | null
  offerVersionLabel: string
  expiresAtUtc: string | null
  linkedCampaignText: string | null
  offerTitle: string
}

export type OfferDetailsRedemptionsListResponse = {
  success: boolean
  items: OfferDetailsRedemptionListItemApi[]
}

export type CatalogOffersListResponse = {
  success: boolean
  items: CatalogOffersListItem[]
  totalCount: number
  page: number
  pageSize: number
  tabCounts: CatalogOffersListTabCounts
}

export type CatalogOffersListQueryParams = {
  locationId: number
  view?: OperatorOffersListViewId
  q?: string
  sort?: OperatorOffersSortId
  page?: number
  pageSize?: number
  status?: CatalogOfferStatus[]
  attachSource?: Array<
    "campaign" | "recovery" | "guest-form-thank-you" | "manual"
  >
  utcOffsetMinutes?: number
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

export type CampaignSendTestOfferRequest = {
  title: string
  description: string
  expiryLabel: string
}

export type CampaignSendTestRequest = {
  locationId: number
  toEmail: string
  subject: string
  body: string
  offer?: CampaignSendTestOfferRequest | null
}

export type CampaignSendTestResponse = {
  success: boolean
}

/** POST /campaigns/{id}/commit — schedule / send-now (ticket 26). */
export type CommitCampaignScheduleRequest = {
  rowVersion: string
  scheduleMode: "send-now" | "schedule-later"
  scheduledAtUtc?: string | null
  scheduleTimeZone: string
}

export type CampaignScheduleCommitDetail = {
  id: number
  locationId: number
  status: string
  name: string
  scheduleMode: string | null
  scheduledAtUtc: string | null
  scheduleTimeZone: string | null
  billingReservationRef: string | null
  reservedEstimate: number | null
  frozenRecipientCount: number
  rowVersion: string
  updatedAt: string
}

export type CommitCampaignScheduleResponse = {
  success: boolean
  campaign: CampaignScheduleCommitDetail
}

/** POST /campaigns/{id}/unschedule|pause|cancel|resume|retry-remaining|duplicate */
export type CampaignLifecycleActionRequest = {
  rowVersion: string
}

export type CampaignLifecycleDetail = {
  id: number
  locationId: number
  status: string
  name: string
  scheduleMode: string | null
  scheduledAtUtc: string | null
  scheduleTimeZone: string | null
  billingReservationRef: string | null
  reservedEstimate: number | null
  frozenRecipientCount: number
  rowVersion: string
  updatedAt: string
}

export type CampaignLifecycleActionResponse = {
  success: boolean
  campaign: CampaignLifecycleDetail | CampaignDraftDetail
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
