import {
  DEFAULT_HOME_PERFORMANCE_DATE_RANGE,
  labelForHomePerformanceDateRange,
  type HomePerformanceDateRange,
} from "@/lib/operatorHome/homePerformanceDateRange"
import {
  createGuestMicSttModule,
  createInMemoryGuestMicSttAdapters,
  type GuestMicChrome,
  type GuestMicErrorKind,
  type GuestMicSttSnapshot,
  type GuestSttResult,
} from "@/lib/guestFeedback/createGuestMicSttModule"

import {
  filterConversationsByTitle,
  formatConversationListMeta,
  groupRecentConversations,
  sortNewestFirst,
  type OperatorAiAssistantListItem,
  type OperatorAiAssistantRecentGroup,
} from "./assistantConversationList"
import type { RecoveryDraftActionPayload } from "@/lib/operatorFeedback/recoveryDraftAction"
import type {
  CampaignDraftDetail,
  CatalogOfferDetail,
  CreateCampaignDraftRequest,
} from "@/types/operatorCampaigns"
import type { CampaignScheduleModeId } from "@/lib/operatorCampaigns/campaignSchedulePresentation"
import {
  ASSISTANT_ARCHIVE_EMPTY_TITLE,
  ASSISTANT_ARCHIVE_TITLE,
  ASSISTANT_BODY_ERROR_HEADING,
  ASSISTANT_EMPTY_ARCHIVE_BODY,
  ASSISTANT_EMPTY_ARCHIVE_HEADING,
  ASSISTANT_EMPTY_RECENT_BODY,
  ASSISTANT_EMPTY_RECENT_HEADING,
  ASSISTANT_LIST_ERROR_HEADING,
  ASSISTANT_OFFLINE_BODY,
  ASSISTANT_OFFLINE_HEADING,
  ASSISTANT_RECENT_TITLE,
  ASSISTANT_SEARCH_MISS_HEADING,
  conversationCountLabel,
  type OperatorAiAssistantListChromeKind,
} from "./assistantListPresentation"
import type { BillingCreditsAccessLevel } from "@/lib/operatorBillingCredits/billingCreditsPresentation"
import {
  ASSISTANT_ADD_CREDITS_LABEL,
  ASSISTANT_CREDITS_STUB_ALLOWANCE,
  ASSISTANT_CREDITS_STUB_REMAINING,
  ASSISTANT_VIEW_USAGE_LABEL,
  assistantCreditsAddCreditsHref,
  assistantCreditsDepleted,
  assistantCreditsRemainingLine,
  assistantCreditsRestorationHelper,
  assistantCreditsShowAddCredits,
  assistantCreditsShowViewUsage,
  assistantCreditsViewUsageHref,
  isAssistantAccountLocked,
  resolveAssistantAccountLockCause,
  type AssistantCreditsRestorationHelper,
} from "./assistantCreditsPresentation"
import type { CreateCatalogOfferRequestBody } from "@/lib/operatorOffers/offerCatalogPresentation"
import { ASSISTANT_NEXT_TRY_SCOPE_SENTENCE } from "./assistantNextTryCopy"
import {
  ASSISTANT_WAIT_BODY,
  ASSISTANT_WAIT_GERUND_INTERVAL_MS,
  ASSISTANT_WAIT_PREPARING_BODY,
  ASSISTANT_WAIT_RETRIEVING_BODY,
  assistantWaitGerundAt,
  formatAssistantWaitGerund,
} from "./assistantWaitGerunds"

export type OperatorAiAssistantWidthMode = "collapsed" | "expanded"

export type OperatorAiAssistantView = "empty" | "recent" | "archive" | "thread"
export type OperatorAiAssistantListPanel = "recent" | "archive"

export type {
  OperatorAiAssistantListItem,
  OperatorAiAssistantRecentGroup,
} from "./assistantConversationList"

export type OperatorAiAssistantGreeting = {
  hello: string
  headline: string
  body: string
}

export type OperatorAiAssistantMessageClass =
  | "grounded"
  | "refusal"
  | "failure"
  | "clarify"
  | "gap"

export type OperatorAiAssistantHelpfulFill = "helpful" | "not-helpful"

export type OperatorAiAssistantActionType =
  | "review-campaign"
  | "change-audience"
  | "add-offer"
  | "review-offer"
  | "draft-campaign"
  | "draft-offer"
  | "open-recovery"
  | "view-feedback-set"
  | "prepare-recovery"
  | "view-campaigns"
  | "view-offers"
  | "view-offer"
  | "view-guests"
  | "view-guest"
  | "view-capture"

export type OperatorAiAssistantAction = {
  type: OperatorAiAssistantActionType
  label: string
  tab?: string | null
  sentiment?: string | null
  detectedTag?: string | null
  count?: number | null
  offerId?: number | null
  guestId?: number | null
  smartGroup?: string | null
  marketingEligible?: boolean | null
  feedbackId?: number | null
  intent?: string | null
  campaignId?: number | null
  clickable?: boolean
}

export type OperatorAiAssistantMessage = {
  id: string
  role: "user" | "assistant" | "wait"
  class?: OperatorAiAssistantMessageClass
  title?: string | null
  body: string
  analysisScope?: OperatorAiAssistantAnalysisScope
  actions?: OperatorAiAssistantAction[]
}

export type OperatorAiAssistantConversationRow = {
  id: string
  title: string
  analysisScope: OperatorAiAssistantAnalysisScope
  lastActivityAt: string
  isArchived: boolean
  messages: OperatorAiAssistantMessage[]
  pendingCampaignDraft?: CreateCampaignDraftRequest | null
  pendingOfferDraft?: CreateCatalogOfferRequestBody | null
  pendingRecoveryDraft?: RecoveryDraftActionPayload | null
  draftInterviewActive?: boolean
  sendScheduleRoute?: AssistantSendScheduleRoute | null
}

export type AssistantSendScheduleRoute = {
  kind: "campaign" | "recovery"
  campaignId?: number | null
  step?: "review" | "schedule" | null
  scheduleMode?: CampaignScheduleModeId | null
  dateLocal?: string | null
  timeLocal?: string | null
  feedbackId?: number | null
  intent?: string | null
}

export type OperatorAiAssistantOwnedLocationOption = {
  id: number
  name: string
}

export type OperatorAiAssistantAnalysisScope = {
  scopeKind?: OperatorAiAssistantScopeKind
  ownedLocationId: number | null
  ownedLocationName: string
  reportingPeriod: HomePerformanceDateRange
}

export const CHANGE_ANALYSIS_SCOPE_TITLE = "Change analysis scope"
export const ALL_OWNED_LOCATIONS_SELECT_VALUE = "all"
export const ALL_OWNED_LOCATIONS_PICKER_LABEL = "All owned locations"
export const ALL_LOCATIONS_CHROME_LABEL = "All Locations"

export type OperatorAiAssistantScopeKind = "all" | "single"
export type OperatorAiAssistantDraftLocation =
  | number
  | typeof ALL_OWNED_LOCATIONS_SELECT_VALUE
export { ASSISTANT_WAIT_BODY }
export const ASSISTANT_COMPOSER_PLACEHOLDER = "Ask AI Assistant..."
export const ASSISTANT_FAILURE_BODY =
  `The answer could not be completed. Retry this send. ${ASSISTANT_NEXT_TRY_SCOPE_SENTENCE}`

export const OPERATOR_ASSISTANT_MIC_ERROR_COPY = {
  permission:
    "Microphone access was denied. You can still type your question.",
  empty_speech:
    "We didn't catch any speech. Try again or type your question.",
  stt_failure:
    "We couldn't transcribe that recording. Try again or type your question.",
  rate_limit: "Too many voice attempts. Try typing instead.",
  truncated: "Your question was shortened.",
} as const

export type OperatorAiAssistantMicError = {
  kind: GuestMicErrorKind
  message: string
}

export type OperatorAiAssistantMicAdapters = {
  startRecording: () => Promise<void>
  stopRecording: () => Promise<Blob>
  cancelRecording: () => Promise<void>
  transcribe: (audio: Blob) => Promise<GuestSttResult>
}

export type OperatorAiAssistantChangeScopeDialogSnapshot = {
  open: boolean
  title: typeof CHANGE_ANALYSIS_SCOPE_TITLE
  showsOwnedLocationField: boolean
  includesAllOwnedLocationsOption: boolean
  draftScopeKind: OperatorAiAssistantScopeKind
  draftOwnedLocationId: number | null
  draftReportingPeriod: HomePerformanceDateRange
  locationOptions: readonly OperatorAiAssistantOwnedLocationOption[]
}

export const EMPTY_SUGGESTION_CHIPS = [
  "Summarise recent feedback",
  "Show what needs attention",
  "Draft a campaign",
  "Draft an offer",
  "Explain performance",
  "Help with guest recovery",
] as const

export type OperatorAiAssistantPresentedRow = OperatorAiAssistantListItem & {
  meta: string
  isCurrent: boolean
}

export type OperatorAiAssistantDeleteConfirmSnapshot = {
  open: boolean
  conversationId: string | null
}

export type OperatorAiAssistantSnapshot = {
  drawerOpen: boolean
  widthMode: OperatorAiAssistantWidthMode
  view: OperatorAiAssistantView
  listPanel: OperatorAiAssistantListPanel
  conversationId: string | null
  greeting: OperatorAiAssistantGreeting
  restaurantName: string
  analysisScope: OperatorAiAssistantAnalysisScope | null
  headerStatusLine: string
  changeScopeDialog: OperatorAiAssistantChangeScopeDialogSnapshot
  composerDraft: string
  composerPlaceholders: readonly string[]
  placeholderCycleGeneration: number
  suggestionChips: readonly string[]
  composerPlaceholder: string
  showSuggestionChips: boolean
  messages: OperatorAiAssistantMessage[]
  turnInFlight: boolean
  sendLocked: boolean
  chipsLocked: boolean
  retryVisible: boolean
  helpfulFills: Record<string, OperatorAiAssistantHelpfulFill>
  searchQuery: string
  listStatus: "idle" | "loading" | "loaded" | "offline" | "error"
  listChromeKind: OperatorAiAssistantListChromeKind
  listTitle: string
  listHeading: string | null
  listBody: string | null
  listCountLabel: string | null
  showStartConversation: boolean
  showListRetry: boolean
  showSearch: boolean
  showArchiveFooter: boolean
  recentGroups: readonly {
    id: OperatorAiAssistantRecentGroup["id"]
    label: string
    rows: readonly OperatorAiAssistantPresentedRow[]
  }[]
  archiveRows: readonly OperatorAiAssistantPresentedRow[]
  listRows: readonly OperatorAiAssistantPresentedRow[]
  bodyLoadError: boolean
  sendBlocked: boolean
  micChrome: GuestMicChrome
  micPhase: GuestMicSttSnapshot["phase"]
  micAvailable: boolean
  micLocked: boolean
  composerLocked: boolean
  micError: OperatorAiAssistantMicError | null
  creditsRemainingLine: string
  viewUsageLabel: string
  addCreditsLabel: string
  showViewUsage: boolean
  showAddCredits: boolean
  restorationHelper: AssistantCreditsRestorationHelper | null
  deleteConfirm: OperatorAiAssistantDeleteConfirmSnapshot
}

export type OperatorAiAssistantCreditsChrome = {
  remaining: number
  allowance: number
  accessLevel: BillingCreditsAccessLevel
  permissionRole: string
  billingStatus: string
  isPilot: boolean
  mode: "single" | "multi"
  locationId: number
}

export type OperatorAiAssistantSendTurnInput = {
  conversationId: string | null
  message: string
  analysisScope: OperatorAiAssistantAnalysisScope
  signal?: AbortSignal
}

export type OperatorAiAssistantRetryTurnInput = {
  conversationId: string
  signal?: AbortSignal
}

export type AssistantTurnProgressStep =
  | "checking"
  | "retrieving"
  | "preparing"

export type AssistantTurnProgressSignal = {
  conversationId: string
  step: AssistantTurnProgressStep
}

export type OperatorAiAssistantAdapters = {
  closePeerRightDrawers: () => void
  sendTurn: (
    input: OperatorAiAssistantSendTurnInput
  ) => Promise<OperatorAiAssistantConversationRow>
  retryTurn: (
    input: OperatorAiAssistantRetryTurnInput
  ) => Promise<OperatorAiAssistantConversationRow>
  getConversation: (
    conversationId: string
  ) => Promise<OperatorAiAssistantConversationRow | null>
  applyScope: (
    conversationId: string,
    analysisScope: OperatorAiAssistantAnalysisScope
  ) => Promise<OperatorAiAssistantConversationRow>
  navigateAction: (input: {
    action: OperatorAiAssistantAction
    analysisScope: OperatorAiAssistantAnalysisScope
    recoveryDraft?: RecoveryDraftActionPayload | null
    campaignDraft?: CampaignDraftDetail | null
    catalogOffer?: CatalogOfferDetail | null
    sendScheduleRoute?: AssistantSendScheduleRoute | null
  }) => void
  /**
   * Load the Campaign Draft to gate completing-turn open. Return null when
   * the Campaign is missing. Throw on load fail.
   */
  getCampaignDraft: (campaignId: number) => Promise<CampaignDraftDetail | null>
  /**
   * Load the Offers catalog row to gate completing-turn open. Return null when
   * the Offer is missing. Throw on load fail. Draft and Active may open;
   * paused, archived, and other statuses stay in the Assistant.
   */
  getCatalogOffer: (offerId: number) => Promise<CatalogOfferDetail | null>
  createCampaignDraft?: (body: CreateCampaignDraftRequest) => Promise<void>
  createCatalogOfferDraft?: (body: CreateCatalogOfferRequestBody) => Promise<void>
  /**
   * Recheck eligibility, then New → In progress and offer attach.
   * Throws Error with a shipped toast. Does not prepare copy.
   */
  prepareOpenRecovery: (input: {
    feedbackId: number
    intent: string
    offerId: number | null
  }) => Promise<void>
  /**
   * Hydrate Feedback Review from stored conversation fields.
   * Throws on failure so the row stays re-clickable.
   */
  openRecoveryFromDraftAction: (
    payload: RecoveryDraftActionPayload
  ) => Promise<void>
  clearDraftInterview: (conversationId: string) => Promise<void>
  notifyDraftError: () => void
  notifyRecoveryDraftError: (message: string) => void
  listConversations: (
    archived: boolean
  ) => Promise<OperatorAiAssistantListItem[]>
  archiveConversation: (conversationId: string) => Promise<void>
  unarchiveConversation: (conversationId: string) => Promise<void>
  deleteConversation: (conversationId: string) => Promise<void>
  isOnline: () => boolean
  nowMs: () => number
  getDashboardOwnedLocation: () => OperatorAiAssistantOwnedLocationOption
  getRestaurantName: () => string
  getDashboardMode: () => "single" | "multi"
  listOwnedLocations: () => readonly OperatorAiAssistantOwnedLocationOption[]
  /** Live AI remaining + Billing access for the composer credits bar. */
  getCreditsChrome: () => Promise<OperatorAiAssistantCreditsChrome>
  /** Navigate after the drawer has closed (View usage / Add credits / restoration). */
  navigateBillingHref: (href: string) => void
  mic: OperatorAiAssistantMicAdapters
}

export type OperatorAiAssistantModule = {
  getSnapshot: () => OperatorAiAssistantSnapshot
  subscribe: (listener: () => void) => () => void
  openDrawer: (input?: { operatorFirstName?: string }) => void
  summariseFeedbackForPeriod: (input: {
    operatorFirstName?: string
    reportingPeriod: HomePerformanceDateRange
  }) => void
  closeDrawer: () => void
  setOpen: (open: boolean) => void
  startNewChat: () => void
  openRecent: () => void
  openArchive: () => void
  backToConversation: () => void
  setSearchQuery: (query: string) => void
  openConversation: (conversationId: string) => void
  archiveConversation: (conversationId: string) => void
  unarchiveConversation: (conversationId: string) => void
  requestDelete: (conversationId: string) => void
  cancelDelete: () => void
  confirmDelete: () => void
  retryList: () => void
  retryBody: () => void
  expandDrawer: () => void
  leaveExpand: () => void
  openChangeScope: () => void
  setChangeScopeDraftLocation: (locationId: OperatorAiAssistantDraftLocation) => void
  setChangeScopeDraftReportingPeriod: (
    reportingPeriod: HomePerformanceDateRange
  ) => void
  cancelChangeScope: () => void
  applyChangeScope: () => void
  onOwnedLocationSwitcherChange: () => void
  setComposerDraft: (text: string) => void
  fillComposerFromChip: (label: string) => void
  send: () => void
  startMic: () => Promise<void>
  confirmMic: () => Promise<void>
  cancelMic: () => Promise<void>
  dismissMicError: () => void
  retry: () => void
  onTurnProgress: (signal: AssistantTurnProgressSignal) => void
  toggleHelpful: (
    messageId: string,
    fill: OperatorAiAssistantHelpfulFill
  ) => void
  clickAction: (action: OperatorAiAssistantAction) => void
  dismissFromEscape: () => void
  viewUsage: () => void
  addCredits: () => void
  followRestorationHelper: () => void
}

const EMPTY_HEADLINE = "What would you like help with?"
const EMPTY_BODY =
  "Ask about feedback, guests, offers, campaigns or performance for this restaurant."

const DEFAULT_OWNED_LOCATION: OperatorAiAssistantOwnedLocationOption = {
  id: 1,
  name: "Camden",
}

export function inMemoryOpenableCampaignDraft(
  campaignId: number,
  overrides: Partial<CampaignDraftDetail> = {}
): CampaignDraftDetail {
  return {
    id: campaignId,
    locationId: DEFAULT_OWNED_LOCATION.id,
    status: "draft",
    name: "Campaign Draft",
    goalId: "re-engage-inactive",
    templateId: null,
    templateVersion: null,
    audienceKey: "all-eligible-guests",
    channel: "email",
    offerStance: "no-offer",
    offerId: null,
    messageSubject: null,
    messageBody: null,
    rowVersion: "1",
    createdAt: "2026-08-19T00:00:00Z",
    updatedAt: "2026-08-19T00:00:00Z",
    ...overrides,
  }
}

export function inMemoryOpenableCatalogOffer(
  offerId: number,
  overrides: Partial<CatalogOfferDetail> = {}
): CatalogOfferDetail {
  return {
    id: offerId,
    locationId: DEFAULT_OWNED_LOCATION.id,
    status: "draft",
    offerType: "percentage_discount",
    title: "25% off your next visit",
    description: "Save 25% on your next visit.",
    validity: "30_days_after_issue",
    expiryDate: null,
    discountPercentage: 25,
    discountAmount: null,
    freeItemText: null,
    purchaseRequirement: null,
    minimumSpend: null,
    additionalExclusions: null,
    replacementItemText: null,
    staffInstructions: null,
    issueCount: 0,
    createdAt: "2026-08-19T00:00:00Z",
    updatedAt: "2026-08-19T00:00:00Z",
    ...overrides,
  }
}

export function buildAssistantEmptyGreeting(
  operatorFirstName: string
): OperatorAiAssistantGreeting {
  const firstName = operatorFirstName.trim() || "Operator"
  return {
    hello: `Hello, ${firstName}`,
    headline: EMPTY_HEADLINE,
    body: EMPTY_BODY,
  }
}

export function analysisScopeKind(
  scope: OperatorAiAssistantAnalysisScope
): OperatorAiAssistantScopeKind {
  return scope.scopeKind === "all" ? "all" : "single"
}

export function changeScopeLocationSelectValue(
  dialog: OperatorAiAssistantChangeScopeDialogSnapshot
): string {
  if (dialog.draftScopeKind === "all") {
    return ALL_OWNED_LOCATIONS_SELECT_VALUE
  }
  return String(dialog.draftOwnedLocationId ?? "")
}

export function formatAnalysisScopeStatusLine(
  restaurantName: string,
  scope: OperatorAiAssistantAnalysisScope
): string {
  return `${restaurantName} · ${scope.ownedLocationName} · ${labelForHomePerformanceDateRange(scope.reportingPeriod)}`
}

export function periodPhraseForReportingPeriod(
  range: HomePerformanceDateRange
): string {
  if (range.kind === "custom") {
    return labelForHomePerformanceDateRange(range)
  }

  switch (range.presetId) {
    case "last7":
      return "the last 7 days"
    case "last30":
      return "the last 30 days"
    case "thisMonth":
      return "this month"
  }
}

export function summariseFeedbackPromptForPeriod(
  range: HomePerformanceDateRange
): string {
  return `Summarise feedback from ${periodPhraseForReportingPeriod(range)}`
}

export function buildEmptyComposerPlaceholders(
  scope: OperatorAiAssistantAnalysisScope
): readonly string[] {
  const periodPhrase = periodPhraseForReportingPeriod(scope.reportingPeriod)
  return [
    `Summarise feedback from ${periodPhrase}\u2026`,
    `What needs attention at ${scope.ownedLocationName}?`,
    `What should I do today at ${scope.ownedLocationName}?`,
    "Draft a quiet-day offer for lunch guests\u2026",
    "Show guests who gave poor feedback but opted in\u2026",
    "Suggest next week\u2019s campaign\u2026",
  ]
}

export function titleFromFirstUserMessage(message: string): string {
  const firstLine = message.replace(/\r\n/g, "\n").split("\n")[0]?.trim() ?? ""
  return firstLine.slice(0, 200)
}

export function analysisScopesEqual(
  left: OperatorAiAssistantAnalysisScope | null | undefined,
  right: OperatorAiAssistantAnalysisScope | null | undefined
): boolean {
  if (left == null || right == null) {
    return left == null && right == null
  }
  if (analysisScopeKind(left) !== analysisScopeKind(right)) {
    return false
  }
  if (
    left.ownedLocationId !== right.ownedLocationId
    || left.ownedLocationName !== right.ownedLocationName
  ) {
    return false
  }
  if (left.reportingPeriod.kind !== right.reportingPeriod.kind) {
    return false
  }
  if (left.reportingPeriod.kind === "preset" && right.reportingPeriod.kind === "preset") {
    return left.reportingPeriod.presetId === right.reportingPeriod.presetId
  }
  if (left.reportingPeriod.kind === "custom" && right.reportingPeriod.kind === "custom") {
    return (
      left.reportingPeriod.startDate === right.reportingPeriod.startDate
      && left.reportingPeriod.endDate === right.reportingPeriod.endDate
    )
  }
  return false
}

function isEmptyAssistantConversation(state: {
  view: OperatorAiAssistantView
  conversationId: string | null
}): boolean {
  return state.view === "empty" && state.conversationId == null
}

function copyDashboardAnalysisScope(
  adapters: OperatorAiAssistantAdapters
): OperatorAiAssistantAnalysisScope {
  const ownedLocation = adapters.getDashboardOwnedLocation()
  return {
    scopeKind: "single",
    ownedLocationId: ownedLocation.id,
    ownedLocationName: ownedLocation.name,
    reportingPeriod: DEFAULT_HOME_PERFORMANCE_DATE_RANGE,
  }
}

function sortOwnedLocationOptions(
  options: readonly OperatorAiAssistantOwnedLocationOption[]
): OperatorAiAssistantOwnedLocationOption[] {
  return [...options].sort((left, right) =>
    left.name.localeCompare(right.name, undefined, { sensitivity: "base" })
  )
}

function bindPendingDraftLocation<T extends { locationId: number }>(
  draft: T | null,
  scope: OperatorAiAssistantAnalysisScope
): T | null {
  if (draft == null) {
    return null
  }
  if (analysisScopeKind(scope) === "all" || scope.ownedLocationId == null) {
    return draft
  }
  return { ...draft, locationId: scope.ownedLocationId }
}

function groundedAnswerForScope(
  scope: OperatorAiAssistantAnalysisScope
): OperatorAiAssistantMessage {
  const period = periodPhraseForReportingPeriod(scope.reportingPeriod)
  return {
    id: `msg-assistant-${Date.now()}`,
    role: "assistant",
    class: "grounded",
    title: `No feedback at ${scope.ownedLocationName} for ${period}`,
    body: `There is nothing to summarise or list at ${scope.ownedLocationName} over ${period}. ${ASSISTANT_NEXT_TRY_SCOPE_SENTENCE}`,
    actions: [],
  }
}

function failureMessage(): OperatorAiAssistantMessage {
  return {
    id: `msg-failure-${Date.now()}`,
    role: "assistant",
    class: "failure",
    body: ASSISTANT_FAILURE_BODY,
  }
}

function isAbortError(error: unknown): boolean {
  return (
    typeof error === "object"
    && error !== null
    && "name" in error
    && (error as { name: string }).name === "AbortError"
  )
}

export function createInMemoryOperatorAiAssistantAdapters(
  overrides: Partial<OperatorAiAssistantAdapters> = {}
): OperatorAiAssistantAdapters & {
  conversations: OperatorAiAssistantConversationRow[]
  online: boolean
  billingHrefs: string[]
  lastNavigate: {
    action: OperatorAiAssistantAction
    analysisScope: OperatorAiAssistantAnalysisScope
    campaignDraft?: CampaignDraftDetail | null
    catalogOffer?: CatalogOfferDetail | null
    recoveryDraft?: RecoveryDraftActionPayload | null
    sendScheduleRoute?: AssistantSendScheduleRoute | null
  } | null
} {
  const conversations: OperatorAiAssistantConversationRow[] = []
  const ownedLocations: OperatorAiAssistantOwnedLocationOption[] = [
    DEFAULT_OWNED_LOCATION,
    { id: 2, name: "Shoreditch" },
  ]
  const extras = {
    conversations,
    online: true,
    billingHrefs: [] as string[],
    lastNavigate: null as {
      action: OperatorAiAssistantAction
      analysisScope: OperatorAiAssistantAnalysisScope
      campaignDraft?: CampaignDraftDetail | null
      catalogOffer?: CatalogOfferDetail | null
      recoveryDraft?: RecoveryDraftActionPayload | null
      sendScheduleRoute?: AssistantSendScheduleRoute | null
    } | null,
  }

  const defaults: OperatorAiAssistantAdapters = {
    closePeerRightDrawers: () => {},
    isOnline: () => extras.online,
    navigateAction: (input) => {
      extras.lastNavigate = input
    },
    getCreditsChrome: async () => ({
      remaining: ASSISTANT_CREDITS_STUB_REMAINING,
      allowance: ASSISTANT_CREDITS_STUB_ALLOWANCE,
      accessLevel: "manage",
      permissionRole: "Owner",
      billingStatus: "Active",
      isPilot: false,
      mode: "multi",
      locationId: DEFAULT_OWNED_LOCATION.id,
    }),
    navigateBillingHref: (href) => {
      extras.billingHrefs.push(href)
    },
    getCampaignDraft: async (campaignId) =>
      inMemoryOpenableCampaignDraft(campaignId),
    getCatalogOffer: async (offerId) => inMemoryOpenableCatalogOffer(offerId),
    createCampaignDraft: async () => {},
    createCatalogOfferDraft: async () => {},
    prepareOpenRecovery: async () => {},
    openRecoveryFromDraftAction: async () => {},
    clearDraftInterview: async () => {},
    notifyDraftError: () => {},
    notifyRecoveryDraftError: () => {},
    sendTurn: async (input) => {
      if (input.signal?.aborted) {
        throw new DOMException("Aborted", "AbortError")
      }
      const now = new Date().toISOString()
      let row = input.conversationId
        ? conversations.find((item) => item.id === input.conversationId)
        : undefined
      if (row == null) {
        row = {
          id: `conv-${conversations.length + 1}`,
          title: titleFromFirstUserMessage(input.message),
          analysisScope: input.analysisScope,
          lastActivityAt: now,
          isArchived: false,
          messages: [],
        }
        conversations.push(row)
      }
      row.messages.push({
        id: `msg-user-${row.messages.length + 1}`,
        role: "user",
        body: input.message,
        analysisScope: input.analysisScope,
      })
      row.lastActivityAt = now
      if (input.signal?.aborted) {
        row.messages.push(failureMessage())
        row.lastActivityAt = new Date().toISOString()
        throw new DOMException("Aborted", "AbortError")
      }
      row.messages.push(groundedAnswerForScope(input.analysisScope))
      row.lastActivityAt = new Date().toISOString()
      return row
    },
    retryTurn: async (input) => {
      if (input.signal?.aborted) {
        throw new DOMException("Aborted", "AbortError")
      }
      const row = conversations.find((item) => item.id === input.conversationId)
      if (row == null) {
        throw new Error("Conversation not found.")
      }
      row.messages = row.messages.filter(
        (item) => !(item.role === "assistant" && item.class === "failure")
      )
      row.messages.push(groundedAnswerForScope(row.analysisScope))
      row.lastActivityAt = new Date().toISOString()
      return row
    },
    getConversation: async (conversationId) => {
      return conversations.find((item) => item.id === conversationId) ?? null
    },
    applyScope: async (conversationId, analysisScope) => {
      const row = conversations.find((item) => item.id === conversationId)
      if (row == null) {
        throw new Error("Conversation not found.")
      }
      row.analysisScope = analysisScope
      return row
    },
    listConversations: async (archived) => {
      return conversations
        .filter((row) => row.isArchived === archived)
        .map((row) => toListItemFromConversation(row))
    },
    archiveConversation: async (conversationId) => {
      const row = conversations.find((item) => item.id === conversationId)
      if (row == null) {
        throw new Error("Conversation not found.")
      }
      row.isArchived = true
    },
    unarchiveConversation: async (conversationId) => {
      const row = conversations.find((item) => item.id === conversationId)
      if (row == null) {
        throw new Error("Conversation not found.")
      }
      row.isArchived = false
    },
    deleteConversation: async (conversationId) => {
      const index = conversations.findIndex((item) => item.id === conversationId)
      if (index < 0) {
        throw new Error("Conversation not found.")
      }
      conversations.splice(index, 1)
    },
    nowMs: () => Date.now(),
    getDashboardOwnedLocation: () => DEFAULT_OWNED_LOCATION,
    getRestaurantName: () => "Mehmet's Grill",
    getDashboardMode: () => "multi",
    listOwnedLocations: () => ownedLocations,
    mic: (() => {
      const memoryMic = createInMemoryGuestMicSttAdapters()
      return {
        startRecording: memoryMic.startRecording,
        stopRecording: memoryMic.stopRecording,
        cancelRecording: memoryMic.cancelRecording,
        transcribe: memoryMic.transcribe,
      }
    })(),
  }

  return {
    ...defaults,
    ...overrides,
    conversations,
    get lastNavigate() {
      return extras.lastNavigate
    },
    get billingHrefs() {
      return extras.billingHrefs
    },
    get online() {
      return extras.online
    },
    set online(value: boolean) {
      extras.online = value
    },
    isOnline: overrides.isOnline ?? (() => extras.online),
    navigateBillingHref:
      overrides.navigateBillingHref
      ?? ((href: string) => {
        extras.billingHrefs.push(href)
      }),
  }
}

type ChangeScopeDialogState = {
  open: boolean
  showsOwnedLocationField: boolean
  includesAllOwnedLocationsOption: boolean
  draftScopeKind: OperatorAiAssistantScopeKind
  draftOwnedLocationId: number | null
  draftReportingPeriod: HomePerformanceDateRange
  locationOptions: readonly OperatorAiAssistantOwnedLocationOption[]
}

type AssistantListStatus = "idle" | "loading" | "loaded" | "offline" | "error"

type AssistantState = {
  drawerOpen: boolean
  widthMode: OperatorAiAssistantWidthMode
  view: OperatorAiAssistantView
  listPanel: OperatorAiAssistantListPanel
  conversationId: string | null
  operatorFirstName: string
  restaurantName: string
  analysisScope: OperatorAiAssistantAnalysisScope | null
  changeScopeDialog: ChangeScopeDialogState
  composerDraft: string
  placeholderCycleGeneration: number
  messages: OperatorAiAssistantMessage[]
  turnInFlight: boolean
  waitBody: string
  turnConversationId: string | null
  helpfulFills: Record<string, OperatorAiAssistantHelpfulFill>
  searchQuery: string
  listItems: OperatorAiAssistantListItem[]
  listStatus: AssistantListStatus
  bodyLoadError: boolean
  failedBodyConversationId: string | null
  deleteConfirmConversationId: string | null
  pendingCampaignDraft: CreateCampaignDraftRequest | null
  pendingOfferDraft: CreateCatalogOfferRequestBody | null
  pendingRecoveryDraft: RecoveryDraftActionPayload | null
  actionInFlight: boolean
  /** Completing Campaign id whose Action rows are locked while open is in flight. */
  completingCampaignNavigateId: number | null
  /** Completing Offer id whose Action row is locked while open is in flight. */
  completingOfferNavigateId: number | null
  draftActionSpent: boolean
  draftInterviewActive: boolean
}

const CLOSED_CHANGE_SCOPE_DIALOG: ChangeScopeDialogState = {
  open: false,
  showsOwnedLocationField: false,
  includesAllOwnedLocationsOption: false,
  draftScopeKind: "single",
  draftOwnedLocationId: null,
  draftReportingPeriod: DEFAULT_HOME_PERFORMANCE_DATE_RANGE,
  locationOptions: [],
}

const INITIAL_STATE: AssistantState = {
  drawerOpen: false,
  widthMode: "collapsed",
  view: "empty",
  listPanel: "recent",
  conversationId: null,
  operatorFirstName: "Operator",
  restaurantName: "",
  analysisScope: null,
  changeScopeDialog: CLOSED_CHANGE_SCOPE_DIALOG,
  composerDraft: "",
  placeholderCycleGeneration: 0,
  messages: [],
  turnInFlight: false,
  waitBody: ASSISTANT_WAIT_BODY,
  turnConversationId: null,
  helpfulFills: {},
  searchQuery: "",
  listItems: [],
  listStatus: "idle",
  bodyLoadError: false,
  failedBodyConversationId: null,
  deleteConfirmConversationId: null,
  pendingCampaignDraft: null,
  pendingOfferDraft: null,
  pendingRecoveryDraft: null,
  actionInFlight: false,
  completingCampaignNavigateId: null,
  completingOfferNavigateId: null,
  draftActionSpent: false,
  draftInterviewActive: false,
}

function hasUserMessage(messages: readonly OperatorAiAssistantMessage[]): boolean {
  return messages.some((message) => message.role === "user")
}

function lastUserScope(
  messages: readonly OperatorAiAssistantMessage[]
): OperatorAiAssistantAnalysisScope | undefined {
  for (let index = messages.length - 1; index >= 0; index -= 1) {
    const message = messages[index]
    if (message.role === "user") {
      return message.analysisScope
    }
  }
  return undefined
}

function toChangeScopeSnapshot(
  dialog: ChangeScopeDialogState
): OperatorAiAssistantChangeScopeDialogSnapshot {
  return {
    open: dialog.open,
    title: CHANGE_ANALYSIS_SCOPE_TITLE,
    showsOwnedLocationField: dialog.showsOwnedLocationField,
    includesAllOwnedLocationsOption: dialog.includesAllOwnedLocationsOption,
    draftScopeKind: dialog.draftScopeKind,
    draftOwnedLocationId: dialog.draftOwnedLocationId,
    draftReportingPeriod: dialog.draftReportingPeriod,
    locationOptions: dialog.locationOptions,
  }
}

function activeListPanel(state: AssistantState): OperatorAiAssistantListPanel {
  if (state.widthMode === "expanded") {
    return state.listPanel
  }
  return state.view === "archive" ? "archive" : "recent"
}

function listChromeFor(state: AssistantState): {
  listChromeKind: OperatorAiAssistantListChromeKind
  listTitle: string
  listHeading: string | null
  listBody: string | null
  showStartConversation: boolean
  showListRetry: boolean
  showSearch: boolean
  showArchiveFooter: boolean
} {
  const isArchive = activeListPanel(state) === "archive"
  const listTitle = isArchive
    ? state.listStatus === "loaded" && state.listItems.length === 0
      ? ASSISTANT_ARCHIVE_EMPTY_TITLE
      : ASSISTANT_ARCHIVE_TITLE
    : ASSISTANT_RECENT_TITLE
  const showSearch = !isArchive
  const showArchiveFooter = !isArchive

  if (state.listStatus === "offline") {
    return {
      listChromeKind: "offline",
      listTitle,
      listHeading: ASSISTANT_OFFLINE_HEADING,
      listBody: ASSISTANT_OFFLINE_BODY,
      showStartConversation: false,
      showListRetry: false,
      showSearch,
      showArchiveFooter,
    }
  }
  if (state.listStatus === "error") {
    return {
      listChromeKind: "list-error",
      listTitle,
      listHeading: ASSISTANT_LIST_ERROR_HEADING,
      listBody: null,
      showStartConversation: false,
      showListRetry: true,
      showSearch,
      showArchiveFooter,
    }
  }
  if (state.bodyLoadError) {
    return {
      listChromeKind: "body-error",
      listTitle,
      listHeading: ASSISTANT_BODY_ERROR_HEADING,
      listBody: null,
      showStartConversation: false,
      showListRetry: true,
      showSearch,
      showArchiveFooter,
    }
  }
  if (state.listStatus === "loading" || state.listStatus === "idle") {
    return {
      listChromeKind: "loading",
      listTitle,
      listHeading: null,
      listBody: null,
      showStartConversation: false,
      showListRetry: false,
      showSearch,
      showArchiveFooter,
    }
  }
  if (isArchive) {
    if (state.listItems.length === 0) {
      return {
        listChromeKind: "empty-archive",
        listTitle,
        listHeading: ASSISTANT_EMPTY_ARCHIVE_HEADING,
        listBody: ASSISTANT_EMPTY_ARCHIVE_BODY,
        showStartConversation: false,
        showListRetry: false,
        showSearch: false,
        showArchiveFooter: false,
      }
    }
    return {
      listChromeKind: "rows",
      listTitle,
      listHeading: null,
      listBody: null,
      showStartConversation: false,
      showListRetry: false,
      showSearch: false,
      showArchiveFooter: false,
    }
  }

  const filtered = filterConversationsByTitle(state.listItems, state.searchQuery)
  if (state.listItems.length === 0) {
    return {
      listChromeKind: "empty-recent",
      listTitle,
      listHeading: ASSISTANT_EMPTY_RECENT_HEADING,
      listBody: ASSISTANT_EMPTY_RECENT_BODY,
      showStartConversation: true,
      showListRetry: false,
      showSearch: true,
      showArchiveFooter: true,
    }
  }
  if (filtered.length === 0) {
    return {
      listChromeKind: "search-miss",
      listTitle,
      listHeading: ASSISTANT_SEARCH_MISS_HEADING,
      listBody: null,
      showStartConversation: false,
      showListRetry: false,
      showSearch: true,
      showArchiveFooter: true,
    }
  }
  return {
    listChromeKind: "rows",
    listTitle,
    listHeading: null,
    listBody: null,
    showStartConversation: false,
    showListRetry: false,
    showSearch: true,
    showArchiveFooter: true,
  }
}

function showsLoadedListRows(
  state: AssistantState,
  chrome: { listChromeKind: OperatorAiAssistantListChromeKind }
): boolean {
  return (
    chrome.listChromeKind === "rows"
    || (chrome.listChromeKind === "offline" && state.listItems.length > 0)
  )
}

function isCompletingCampaignAction(
  type: OperatorAiAssistantActionType
): boolean {
  return (
    type === "review-campaign"
    || type === "change-audience"
    || type === "add-offer"
  )
}

function isCompletingOfferAction(
  type: OperatorAiAssistantActionType
): boolean {
  return type === "review-offer"
}

function isRetiredDraftActionType(
  type: OperatorAiAssistantActionType
): boolean {
  return type === "draft-campaign" || type === "draft-offer"
}

function canOpenStoredDraftFromAssistant(
  row: { status: string; locationId: number } | null,
  analysisScopeLocationId: number | null
): boolean {
  if (row == null) {
    return false
  }
  if (row.status.toLowerCase() !== "draft") {
    return false
  }
  if (analysisScopeLocationId == null) {
    return true
  }
  return row.locationId === analysisScopeLocationId
}

function canOpenStoredOfferFromAssistant(
  row: { status: string; locationId: number } | null,
  analysisScopeLocationId: number | null
): boolean {
  if (row == null) {
    return false
  }
  const status = row.status.toLowerCase()
  if (status !== "draft" && status !== "active") {
    return false
  }
  if (analysisScopeLocationId == null) {
    return true
  }
  return row.locationId === analysisScopeLocationId
}

function visibleActionsForMessage(
  message: OperatorAiAssistantMessage,
  state: AssistantState
): OperatorAiAssistantAction[] | undefined {
  if (message.actions == null) {
    return undefined
  }

  const raw = message.actions.filter(
    (action) => !isRetiredDraftActionType(action.type)
  )

  const hasCompletingCampaign = raw.some((action) =>
    isCompletingCampaignAction(action.type)
  )
  const hasCompletingOffer = raw.some((action) =>
    isCompletingOfferAction(action.type)
  )
  const hasOpenRecovery = raw.some((action) => action.type === "open-recovery")
  const combinedCreate =
    hasCompletingCampaign && hasCompletingOffer
  const filtered = combinedCreate
    ? (["review-campaign", "change-audience", "review-offer"] as const)
        .map((type) => raw.find((action) => action.type === type))
        .filter((action): action is OperatorAiAssistantAction => action != null)
        .slice(0, 3)
    : hasCompletingCampaign
      ? raw.filter((action) => isCompletingCampaignAction(action.type)).slice(0, 3)
      : hasCompletingOffer
        ? raw.filter((action) => isCompletingOfferAction(action.type)).slice(0, 1)
        : hasOpenRecovery
          ? raw.filter((action) => action.type === "open-recovery").slice(0, 1)
          : raw.slice(0, 3)

  return filtered.map((action) => ({
    ...action,
    clickable: isCompletingCampaignAction(action.type)
      ? !(
          state.turnInFlight
          || (state.actionInFlight
            && state.completingCampaignNavigateId != null
            && action.campaignId === state.completingCampaignNavigateId)
        )
      : isCompletingOfferAction(action.type)
        ? !(
            state.turnInFlight
            || (state.actionInFlight
              && state.completingOfferNavigateId != null
              && action.offerId === state.completingOfferNavigateId)
          )
          : action.type === "open-recovery"
            ? !(
                state.turnInFlight
                || state.actionInFlight
                || state.pendingRecoveryDraft == null
              )
            : true,
  }))
}

function toSnapshot(
  state: AssistantState,
  nowMs: number,
  mic: GuestMicSttSnapshot,
  isOnline: boolean,
  credits: OperatorAiAssistantCreditsChrome
): OperatorAiAssistantSnapshot {
  const emptyConversation = isEmptyAssistantConversation(state)
  const storedMessages = state.messages.filter((message) => message.role !== "wait")
  const displayMessages = (state.turnInFlight
    ? [
        ...storedMessages,
        {
          id: "wait",
          role: "wait" as const,
          body: state.waitBody,
        },
      ]
    : storedMessages).map((message) => {
      return {
        ...message,
        actions: visibleActionsForMessage(message, state),
      }
    })
  const lastAssistant = [...displayMessages]
    .reverse()
    .find((message) => message.role === "assistant")
  const retryVisible =
    lastAssistant?.class === "failure"
    && analysisScopesEqual(lastUserScope(storedMessages), state.analysisScope)
  const suggestionChips = emptyConversation ? EMPTY_SUGGESTION_CHIPS : []
  const chrome = listChromeFor(state)
  const listPanel = activeListPanel(state)
  const filtered = filterConversationsByTitle(state.listItems, state.searchQuery)
  const visibleRows =
    listPanel === "archive" ? sortNewestFirst(state.listItems) : filtered
  const presentedRows: OperatorAiAssistantPresentedRow[] = visibleRows.map(
    (row) => ({
      ...row,
      meta: formatConversationListMeta(
        row.ownedLocationName,
        row.lastActivityAt,
        nowMs
      ),
      isCurrent: row.id === state.conversationId,
    })
  )
  const showLoadedListRows = showsLoadedListRows(state, chrome)
  const recentGroups =
    listPanel === "recent" && showLoadedListRows
      ? groupRecentConversations(filtered, nowMs).map((group) => ({
          ...group,
          rows: group.rows.map((row) => ({
            ...row,
            meta: formatConversationListMeta(
              row.ownedLocationName,
              row.lastActivityAt,
              nowMs
            ),
            isCurrent: row.id === state.conversationId,
          })),
        }))
      : []

  return {
    drawerOpen: state.drawerOpen,
    widthMode: state.widthMode,
    view: state.view,
    listPanel,
    conversationId: state.conversationId,
    greeting: buildAssistantEmptyGreeting(state.operatorFirstName),
    restaurantName: state.restaurantName,
    analysisScope: state.analysisScope,
    headerStatusLine:
      state.analysisScope == null
        ? ""
        : formatAnalysisScopeStatusLine(state.restaurantName, state.analysisScope),
    changeScopeDialog: toChangeScopeSnapshot(state.changeScopeDialog),
    composerDraft: state.composerDraft,
    composerPlaceholders:
      emptyConversation && state.analysisScope != null
        ? buildEmptyComposerPlaceholders(state.analysisScope)
        : [],
    placeholderCycleGeneration: state.placeholderCycleGeneration,
    suggestionChips,
    composerPlaceholder: hasUserMessage(storedMessages)
      ? ASSISTANT_COMPOSER_PLACEHOLDER
      : "",
    showSuggestionChips: suggestionChips.length > 0,
    messages: displayMessages,
    turnInFlight: state.turnInFlight,
    sendLocked: state.turnInFlight || mic.submitLocked,
    chipsLocked: state.turnInFlight || mic.submitLocked,
    retryVisible,
    helpfulFills: state.helpfulFills,
    searchQuery: state.searchQuery,
    listStatus: state.listStatus,
    listChromeKind: chrome.listChromeKind,
    listTitle: chrome.listTitle,
    listHeading: chrome.listHeading,
    listBody: chrome.listBody,
    listCountLabel: showLoadedListRows
      ? conversationCountLabel(presentedRows.length)
      : null,
    showStartConversation: chrome.showStartConversation,
    showListRetry: chrome.showListRetry,
    showSearch: chrome.showSearch,
    showArchiveFooter: chrome.showArchiveFooter,
    recentGroups,
    archiveRows: listPanel === "archive" ? presentedRows : [],
    listRows: presentedRows,
    bodyLoadError: state.bodyLoadError,
    sendBlocked:
      state.turnInFlight
      || state.bodyLoadError
      || !isOnline
      || assistantCreditsDepleted(credits.remaining)
      || isAssistantAccountLocked(credits.billingStatus),
    micChrome: mic.chrome,
    micPhase: mic.phase,
    micAvailable: mic.micAvailable,
    micLocked:
      state.turnInFlight
      || state.bodyLoadError
      || !isOnline
      || !mic.micAvailable
      || assistantCreditsDepleted(credits.remaining)
      || isAssistantAccountLocked(credits.billingStatus),
    composerLocked: mic.messageLocked,
    micError: mic.error,
    creditsRemainingLine: assistantCreditsRemainingLine(
      credits.remaining,
      credits.allowance
    ),
    viewUsageLabel: ASSISTANT_VIEW_USAGE_LABEL,
    addCreditsLabel: ASSISTANT_ADD_CREDITS_LABEL,
    showViewUsage: assistantCreditsShowViewUsage(credits.accessLevel),
    showAddCredits: assistantCreditsShowAddCredits({
      accessLevel: credits.accessLevel,
      permissionRole: credits.permissionRole,
    }),
    restorationHelper: assistantCreditsRestorationHelper({
      lockCause: resolveAssistantAccountLockCause({
        billingStatus: credits.billingStatus,
        isPilot: credits.isPilot,
      }),
      accessLevel: credits.accessLevel,
      permissionRole: credits.permissionRole,
      mode: credits.mode,
      locationId: credits.locationId,
    }),
    deleteConfirm: {
      open: state.deleteConfirmConversationId != null,
      conversationId: state.deleteConfirmConversationId,
    },
  }
}

function applyConversation(
  state: AssistantState,
  row: OperatorAiAssistantConversationRow
): AssistantState {
  return {
    ...state,
    view: "thread",
    conversationId: row.id,
    analysisScope: row.analysisScope,
    messages: row.messages.filter((message) => message.role !== "wait"),
    pendingCampaignDraft: row.pendingCampaignDraft ?? null,
    pendingOfferDraft: row.pendingOfferDraft ?? null,
    pendingRecoveryDraft: row.pendingRecoveryDraft ?? null,
    draftInterviewActive: row.draftInterviewActive === true,
    // Per-conversation clickability — do not keep spent/in-flight from another thread.
    actionInFlight: false,
    completingCampaignNavigateId: null,
    completingOfferNavigateId: null,
    draftActionSpent: false,
    turnInFlight: false,
    waitBody: ASSISTANT_WAIT_BODY,
    turnConversationId: null,
    listItems: upsertListItem(state.listItems, row, activeListPanel(state)),
  }
}

function toListItemFromConversation(
  row: OperatorAiAssistantConversationRow
): OperatorAiAssistantListItem {
  return {
    id: row.id,
    title: row.title,
    ownedLocationName: row.analysisScope.ownedLocationName,
    lastActivityAt: row.lastActivityAt,
    isArchived: row.isArchived,
  }
}

function upsertListItem(
  items: OperatorAiAssistantListItem[],
  row: OperatorAiAssistantConversationRow,
  listPanel: OperatorAiAssistantListPanel
): OperatorAiAssistantListItem[] {
  const without = items.filter((item) => item.id !== row.id)
  const rowIsOnActivePanel =
    listPanel === "archive" ? row.isArchived : !row.isArchived
  if (!rowIsOnActivePanel) {
    return without
  }
  return [toListItemFromConversation(row), ...without]
}

function analysisOwnedLocationChanged(
  saved: OperatorAiAssistantAnalysisScope,
  next: OperatorAiAssistantAnalysisScope
): boolean {
  return (
    analysisScopeKind(saved) !== analysisScopeKind(next)
    || saved.ownedLocationId !== next.ownedLocationId
  )
}

function emptyGreetingState(
  state: AssistantState,
  adapters: OperatorAiAssistantAdapters,
  operatorFirstName?: string,
  analysisScope?: OperatorAiAssistantAnalysisScope
): AssistantState {
  return {
    ...state,
    view: "empty",
    conversationId: null,
    operatorFirstName: operatorFirstName?.trim() || state.operatorFirstName,
    restaurantName: adapters.getRestaurantName(),
    analysisScope: analysisScope ?? copyDashboardAnalysisScope(adapters),
    changeScopeDialog: CLOSED_CHANGE_SCOPE_DIALOG,
    composerDraft: "",
    placeholderCycleGeneration: state.placeholderCycleGeneration + 1,
    messages: [],
    pendingCampaignDraft: null,
    pendingOfferDraft: null,
    pendingRecoveryDraft: null,
    actionInFlight: false,
    completingCampaignNavigateId: null,
    completingOfferNavigateId: null,
    draftActionSpent: false,
    draftInterviewActive: false,
    turnInFlight: false,
    waitBody: ASSISTANT_WAIT_BODY,
    turnConversationId: null,
    helpfulFills: {},
    bodyLoadError: false,
    failedBodyConversationId: null,
    deleteConfirmConversationId: null,
  }
}

export function createOperatorAiAssistantModule(
  adapters: OperatorAiAssistantAdapters
): OperatorAiAssistantModule {
  let state: AssistantState = { ...INITIAL_STATE }
  const listeners = new Set<() => void>()
  let sendGeneration = 0
  let listGeneration = 0
  let inflight: AbortController | null = null
  let waitTimer: ReturnType<typeof setInterval> | null = null
  let waitGerundIndex = 0
  let waitPipeline: "checking" | "retrieving" | "preparing" = "checking"
  const initialSwitcherOwnedLocationId = adapters.getDashboardOwnedLocation().id
  let lastSwitcherOwnedLocationId: number | null =
    initialSwitcherOwnedLocationId === 0 ? null : initialSwitcherOwnedLocationId
  let creditsChrome: OperatorAiAssistantCreditsChrome = {
    remaining: ASSISTANT_CREDITS_STUB_REMAINING,
    allowance: ASSISTANT_CREDITS_STUB_ALLOWANCE,
    accessLevel: "none",
    permissionRole: "",
    billingStatus: "Active",
    isPilot: false,
    mode: adapters.getDashboardMode(),
    locationId: adapters.getDashboardOwnedLocation().id,
  }
  let creditsLoadGeneration = 0

  const stopCheckingWaitRotation = () => {
    if (waitTimer != null) {
      clearInterval(waitTimer)
      waitTimer = null
    }
  }

  const startCheckingWaitRotation = () => {
    stopCheckingWaitRotation()
    waitPipeline = "checking"
    waitGerundIndex = adapters.nowMs()
    const waitBody = formatAssistantWaitGerund(
      assistantWaitGerundAt(waitGerundIndex)
    )
    waitTimer = setInterval(() => {
      if (!state.turnInFlight || waitPipeline !== "checking") {
        return
      }
      waitGerundIndex += 1
      state = {
        ...state,
        waitBody: formatAssistantWaitGerund(
          assistantWaitGerundAt(waitGerundIndex)
        ),
      }
      publish()
    }, ASSISTANT_WAIT_GERUND_INTERVAL_MS)
    return waitBody
  }

  const mic = createGuestMicSttModule(
    {
      startRecording: adapters.mic.startRecording,
      stopRecording: adapters.mic.stopRecording,
      cancelRecording: adapters.mic.cancelRecording,
      transcribe: adapters.mic.transcribe,
      replaceComment: (text) => {
        state = { ...state, composerDraft: text }
      },
    },
    {
      errorCopy: OPERATOR_ASSISTANT_MIC_ERROR_COPY,
      maxCommentLength: 100_000,
    }
  )

  const publish = () => {
    snapshot = toSnapshot(
      state,
      adapters.nowMs(),
      mic.getSnapshot(),
      adapters.isOnline(),
      creditsChrome
    )
    for (const listener of listeners) {
      listener()
    }
  }

  mic.subscribe(publish)

  let snapshot = toSnapshot(
    state,
    adapters.nowMs(),
    mic.getSnapshot(),
    adapters.isOnline(),
    creditsChrome
  )

  const paidWriteBlocked = () =>
    assistantCreditsDepleted(creditsChrome.remaining)
    || isAssistantAccountLocked(creditsChrome.billingStatus)

  const composerControlsLocked = () =>
    state.turnInFlight
    || state.bodyLoadError
    || !adapters.isOnline()
    || paidWriteBlocked()

  const refreshCreditsChrome = () => {
    const generation = ++creditsLoadGeneration
    void adapters
      .getCreditsChrome()
      .then((next) => {
        if (generation !== creditsLoadGeneration) {
          return
        }
        creditsChrome = next
        publish()
      })
      .catch(() => {
        if (generation !== creditsLoadGeneration) {
          return
        }
        // Keep the last known chrome (stub until first success).
      })
  }

  const loadList = (archived: boolean) => {
    const generation = ++listGeneration
    if (!adapters.isOnline()) {
      state = { ...state, listStatus: "offline", bodyLoadError: false }
      publish()
      return
    }
    state = { ...state, listStatus: "loading", bodyLoadError: false }
    publish()
    void adapters
      .listConversations(archived)
      .then((rows) => {
        if (generation !== listGeneration) {
          return
        }
        state = {
          ...state,
          listItems: rows,
          listStatus: "loaded",
        }
        publish()
      })
      .catch(() => {
        if (generation !== listGeneration) {
          return
        }
        state = { ...state, listStatus: "error" }
        publish()
      })
  }

  const abortInFlight = () => {
    sendGeneration += 1
    inflight?.abort()
    inflight = null
    stopCheckingWaitRotation()
  }

  const beginNewChat = (analysisScope?: OperatorAiAssistantAnalysisScope) => {
    const abandonedConversationId =
      state.draftInterviewActive ? state.conversationId : null
    abortInFlight()
    mic.reset()
    state = emptyGreetingState(state, adapters, undefined, analysisScope)
    publish()
    if (abandonedConversationId != null) {
      void adapters.clearDraftInterview(abandonedConversationId)
    }
  }

  const showEmptyGreeting = (operatorFirstName?: string) => {
    state = emptyGreetingState(state, adapters, operatorFirstName)
    publish()
  }

  const openDrawer = (input?: { operatorFirstName?: string }) => {
    adapters.closePeerRightDrawers()
    refreshCreditsChrome()
    const operatorFirstName =
      input?.operatorFirstName?.trim() || state.operatorFirstName
    const resumeId = state.conversationId

    if (resumeId == null) {
      state = {
        ...emptyGreetingState(state, adapters, operatorFirstName),
        drawerOpen: true,
        widthMode: "collapsed",
      }
      publish()
      return
    }

    state = {
      ...state,
      drawerOpen: true,
      widthMode: "collapsed",
      operatorFirstName,
      restaurantName: adapters.getRestaurantName(),
      changeScopeDialog: CLOSED_CHANGE_SCOPE_DIALOG,
      composerDraft: "",
    }
    publish()

    void adapters.getConversation(resumeId).then((row) => {
      if (!state.drawerOpen) {
        return
      }
      if (state.conversationId !== resumeId) {
        return
      }
      if (row == null) {
        showEmptyGreeting(operatorFirstName)
        state = { ...state, drawerOpen: true, widthMode: "collapsed" }
        publish()
        return
      }
      state = {
        ...applyConversation(state, row),
        drawerOpen: true,
        widthMode: "collapsed",
        operatorFirstName,
        restaurantName: adapters.getRestaurantName(),
        composerDraft: "",
        changeScopeDialog: CLOSED_CHANGE_SCOPE_DIALOG,
      }
      publish()
    })
  }

  const closeDrawer = () => {
    if (!state.drawerOpen) {
      return
    }
    abortInFlight()
    mic.reset()
    state = {
      ...state,
      drawerOpen: false,
      widthMode: "collapsed",
      composerDraft: "",
      turnInFlight: false,
      changeScopeDialog: CLOSED_CHANGE_SCOPE_DIALOG,
    }
    publish()
  }

  const expandDrawer = () => {
    if (!state.drawerOpen || state.widthMode === "expanded") {
      return
    }
    const listPanel =
      state.view === "archive" || state.view === "recent"
        ? state.view
        : state.listPanel
    state = {
      ...state,
      widthMode: "expanded",
      view: state.conversationId == null ? "empty" : "thread",
      listPanel,
    }
    publish()
    loadList(listPanel === "archive")
  }

  const leaveExpand = () => {
    if (!state.drawerOpen || state.widthMode !== "expanded") {
      return
    }
    state = { ...state, widthMode: "collapsed" }
    publish()
  }

  const openChangeScope = () => {
    if (!state.drawerOpen || state.analysisScope == null) {
      return
    }
    const mode = adapters.getDashboardMode()
    const showsOwnedLocationField = mode === "multi"
    const savedIsAll = analysisScopeKind(state.analysisScope) === "all"
    state = {
      ...state,
      changeScopeDialog: {
        open: true,
        showsOwnedLocationField,
        includesAllOwnedLocationsOption: showsOwnedLocationField,
        draftScopeKind:
          showsOwnedLocationField && savedIsAll ? "all" : "single",
        draftOwnedLocationId: savedIsAll
          ? null
          : state.analysisScope.ownedLocationId,
        draftReportingPeriod: state.analysisScope.reportingPeriod,
        locationOptions: sortOwnedLocationOptions(adapters.listOwnedLocations()),
      },
    }
    publish()
  }

  const cancelChangeScope = () => {
    if (!state.changeScopeDialog.open) {
      return
    }
    state = { ...state, changeScopeDialog: CLOSED_CHANGE_SCOPE_DIALOG }
    publish()
  }

  const followSendScheduleRoute = (
    row: OperatorAiAssistantConversationRow
  ) => {
    const route = row.sendScheduleRoute
    const analysisScope = state.analysisScope
    if (route == null || analysisScope == null || state.actionInFlight) {
      return
    }
    if (route.kind === "recovery") {
      const payload = row.pendingRecoveryDraft
      if (payload == null) {
        return
      }
      state = { ...state, actionInFlight: true }
      publish()
      void adapters
        .prepareOpenRecovery({
          feedbackId: payload.feedbackId,
          intent: payload.intent,
          offerId: payload.offerId ?? null,
        })
        .then(() => adapters.openRecoveryFromDraftAction(payload))
        .then(() => {
          state = { ...state, actionInFlight: false }
          publish()
          closeDrawer()
          adapters.navigateAction({
            action: {
              type: "open-recovery",
              label: "Review recovery",
              feedbackId: payload.feedbackId,
              intent: payload.intent,
            },
            analysisScope,
            recoveryDraft: payload,
            sendScheduleRoute: route,
          })
        })
        .catch((error: unknown) => {
          state = { ...state, actionInFlight: false }
          const message =
            error instanceof Error && error.message.trim() !== ""
              ? error.message
              : "Could not open recovery. Please try again."
          adapters.notifyRecoveryDraftError(message)
          publish()
        })
      return
    }

    const campaignId = route.campaignId
    if (campaignId == null) {
      return
    }
    state = {
      ...state,
      actionInFlight: true,
      completingCampaignNavigateId: campaignId,
    }
    publish()
    void adapters
      .getCampaignDraft(campaignId)
      .then((campaign) => {
        if (
          !canOpenStoredDraftFromAssistant(
            campaign,
            analysisScope.ownedLocationId
          )
        ) {
          throw new Error("Campaign draft cannot open.")
        }
        adapters.navigateAction({
          action: {
            type: "review-campaign",
            label: "Review campaign draft",
            campaignId,
          },
          analysisScope,
          campaignDraft: campaign,
          sendScheduleRoute: route,
        })
        closeDrawer()
      })
      .catch(() => {
        state = {
          ...state,
          actionInFlight: false,
          completingCampaignNavigateId: null,
        }
        publish()
      })
  }

  const runTurn = (message: string, replaceFailure: boolean) => {
    if (state.turnInFlight) {
      return
    }
    if (!adapters.isOnline()) {
      return
    }
    if (paidWriteBlocked()) {
      return
    }
    const analysisScope = state.analysisScope
    if (analysisScope == null) {
      return
    }

    const generation = ++sendGeneration
    inflight = new AbortController()
    const storedMessages = replaceFailure
      ? state.messages.filter(
          (item) => !(item.role === "assistant" && item.class === "failure")
        )
      : [
          ...state.messages.filter((item) => item.role !== "wait"),
          {
            id: `msg-user-local-${generation}`,
            role: "user" as const,
            body: message,
            analysisScope,
          },
        ]

    const waitBody = startCheckingWaitRotation()
    state = {
      ...state,
      view: "thread",
      composerDraft: "",
      messages: storedMessages,
      turnInFlight: true,
      waitBody,
      turnConversationId: state.conversationId,
      changeScopeDialog: CLOSED_CHANGE_SCOPE_DIALOG,
    }
    publish()

    const turn =
      replaceFailure && state.conversationId != null
        ? adapters.retryTurn({
            conversationId: state.conversationId,
            signal: inflight.signal,
          })
        : adapters.sendTurn({
            conversationId: state.conversationId,
            message,
            analysisScope,
            signal: inflight.signal,
          })

    void turn
      .then((row) => {
        if (generation !== sendGeneration) {
          return
        }
        inflight = null
        stopCheckingWaitRotation()
        state = {
          ...applyConversation(state, row),
          composerDraft: "",
        }
        publish()
        followSendScheduleRoute(row)
      })
      .catch((error: unknown) => {
        if (generation !== sendGeneration) {
          return
        }
        inflight = null
        stopCheckingWaitRotation()
        if (isAbortError(error)) {
          state = {
            ...state,
            turnInFlight: false,
            waitBody: ASSISTANT_WAIT_BODY,
            turnConversationId: null,
          }
          publish()
          return
        }
        state = {
          ...state,
          turnInFlight: false,
          waitBody: ASSISTANT_WAIT_BODY,
          turnConversationId: null,
          messages: [...storedMessages, failureMessage()],
        }
        publish()
      })
  }

  const summariseFeedbackForPeriod = (input: {
    operatorFirstName?: string
    reportingPeriod: HomePerformanceDateRange
  }) => {
    adapters.closePeerRightDrawers()
    const abandonedConversationId =
      state.draftInterviewActive ? state.conversationId : null
    abortInFlight()
    mic.reset()
    const operatorFirstName =
      input.operatorFirstName?.trim() || state.operatorFirstName
    const analysisScope: OperatorAiAssistantAnalysisScope = {
      ...copyDashboardAnalysisScope(adapters),
      reportingPeriod: input.reportingPeriod,
    }
    const prompt = summariseFeedbackPromptForPeriod(input.reportingPeriod)
    state = {
      ...emptyGreetingState(state, adapters, operatorFirstName, analysisScope),
      drawerOpen: true,
      widthMode: "collapsed",
      composerDraft: prompt,
    }
    publish()
    if (abandonedConversationId != null) {
      void adapters.clearDraftInterview(abandonedConversationId)
    }
    if (state.bodyLoadError || mic.getSnapshot().submitLocked) {
      return
    }
    runTurn(prompt, false)
  }

  return {
    getSnapshot: () => snapshot,
    subscribe: (listener) => {
      listeners.add(listener)
      return () => {
        listeners.delete(listener)
      }
    },
    openDrawer,
    summariseFeedbackForPeriod,
    closeDrawer,
    setOpen: (open) => {
      if (open) {
        openDrawer()
      } else {
        closeDrawer()
      }
    },
    startNewChat: () => {
      beginNewChat()
    },
    onOwnedLocationSwitcherChange: () => {
      if (adapters.getDashboardMode() === "single") {
        return
      }
      const location = adapters.getDashboardOwnedLocation()
      if (location.id === 0) {
        return
      }
      if (lastSwitcherOwnedLocationId == null) {
        lastSwitcherOwnedLocationId = location.id
        return
      }
      if (location.id === lastSwitcherOwnedLocationId) {
        return
      }
      lastSwitcherOwnedLocationId = location.id
      beginNewChat()
    },
    openRecent: () => {
      state = {
        ...state,
        view: state.widthMode === "expanded" ? state.view : "recent",
        listPanel: "recent",
        searchQuery: "",
        deleteConfirmConversationId: null,
      }
      publish()
      loadList(false)
    },
    openArchive: () => {
      state = {
        ...state,
        view: state.widthMode === "expanded" ? state.view : "archive",
        listPanel: "archive",
        searchQuery: "",
        deleteConfirmConversationId: null,
      }
      publish()
      loadList(true)
    },
    backToConversation: () => {
      state = {
        ...state,
        view: state.conversationId == null ? "empty" : "thread",
        bodyLoadError: false,
        failedBodyConversationId: null,
        deleteConfirmConversationId: null,
      }
      publish()
    },
    setSearchQuery: (query) => {
      if (activeListPanel(state) !== "recent" || state.searchQuery === query) {
        return
      }
      state = { ...state, searchQuery: query }
      publish()
    },
    openConversation: (conversationId) => {
      if (!adapters.isOnline()) {
        state = {
          ...state,
          bodyLoadError: true,
          failedBodyConversationId: conversationId,
        }
        publish()
        return
      }
      const generation = ++listGeneration
      state = {
        ...state,
        composerDraft: "",
        bodyLoadError: false,
        failedBodyConversationId: null,
      }
      publish()
      void adapters
        .getConversation(conversationId)
        .then((row) => {
          if (generation !== listGeneration) {
            return
          }
          if (row == null) {
            state = {
              ...state,
              bodyLoadError: true,
              failedBodyConversationId: conversationId,
            }
            publish()
            return
          }
          state = {
            ...applyConversation(state, row),
            composerDraft: "",
            bodyLoadError: false,
            failedBodyConversationId: null,
          }
          publish()
        })
        .catch(() => {
          if (generation !== listGeneration) {
            return
          }
          state = {
            ...state,
            bodyLoadError: true,
            failedBodyConversationId: conversationId,
          }
          publish()
        })
    },
    archiveConversation: (conversationId) => {
      void adapters.archiveConversation(conversationId).then(() => {
        const stayOnOpenThread = state.conversationId === conversationId
        state = {
          ...state,
          listItems: state.listItems.filter((row) => row.id !== conversationId),
          view: stayOnOpenThread ? "thread" : state.view,
        }
        publish()
      })
    },
    unarchiveConversation: (conversationId) => {
      void adapters.unarchiveConversation(conversationId).then(() => {
        const stayOnOpenThread = state.conversationId === conversationId
        state = {
          ...state,
          listItems: state.listItems.filter((row) => row.id !== conversationId),
          view: stayOnOpenThread ? "thread" : state.view,
        }
        publish()
      })
    },
    requestDelete: (conversationId) => {
      state = { ...state, deleteConfirmConversationId: conversationId }
      publish()
    },
    cancelDelete: () => {
      if (state.deleteConfirmConversationId == null) {
        return
      }
      state = { ...state, deleteConfirmConversationId: null }
      publish()
    },
    confirmDelete: () => {
      const conversationId = state.deleteConfirmConversationId
      if (conversationId == null) {
        return
      }
      void adapters.deleteConversation(conversationId).then(() => {
        const deletedOpen = state.conversationId === conversationId
        if (deletedOpen) {
          showEmptyGreeting()
          return
        }
        state = {
          ...state,
          listItems: state.listItems.filter((row) => row.id !== conversationId),
          deleteConfirmConversationId: null,
        }
        publish()
      })
    },
    retryList: () => {
      if (state.view === "archive") {
        loadList(true)
        return
      }
      if (state.view === "recent") {
        loadList(false)
      }
    },
    retryBody: () => {
      const conversationId = state.failedBodyConversationId
      if (conversationId == null) {
        return
      }
      state = { ...state, bodyLoadError: false }
      publish()
      void adapters
        .getConversation(conversationId)
        .then((row) => {
          if (row == null) {
            state = {
              ...state,
              bodyLoadError: true,
              failedBodyConversationId: conversationId,
            }
            publish()
            return
          }
          state = {
            ...applyConversation(state, row),
            composerDraft: "",
            bodyLoadError: false,
            failedBodyConversationId: null,
          }
          publish()
        })
        .catch(() => {
          state = {
            ...state,
            bodyLoadError: true,
            failedBodyConversationId: conversationId,
          }
          publish()
        })
    },
    expandDrawer,
    leaveExpand,
    openChangeScope,
    setChangeScopeDraftLocation: (locationId) => {
      if (!state.changeScopeDialog.open) {
        return
      }
      if (!state.changeScopeDialog.showsOwnedLocationField) {
        return
      }
      if (locationId === ALL_OWNED_LOCATIONS_SELECT_VALUE) {
        if (!state.changeScopeDialog.includesAllOwnedLocationsOption) {
          return
        }
        state = {
          ...state,
          changeScopeDialog: {
            ...state.changeScopeDialog,
            draftScopeKind: "all",
            draftOwnedLocationId: null,
          },
        }
        publish()
        return
      }
      if (
        !state.changeScopeDialog.locationOptions.some(
          (location) => location.id === locationId
        )
      ) {
        return
      }
      state = {
        ...state,
        changeScopeDialog: {
          ...state.changeScopeDialog,
          draftScopeKind: "single",
          draftOwnedLocationId: locationId,
        },
      }
      publish()
    },
    setChangeScopeDraftReportingPeriod: (reportingPeriod) => {
      if (!state.changeScopeDialog.open) {
        return
      }
      state = {
        ...state,
        changeScopeDialog: {
          ...state.changeScopeDialog,
          draftReportingPeriod: reportingPeriod,
        },
      }
      publish()
    },
    cancelChangeScope,
    applyChangeScope: () => {
      if (!state.changeScopeDialog.open || state.analysisScope == null) {
        return
      }
      const dialog = state.changeScopeDialog
      const savedScope = state.analysisScope
      let nextScope: OperatorAiAssistantAnalysisScope
      if (!dialog.showsOwnedLocationField) {
        nextScope = {
          ...savedScope,
          reportingPeriod: dialog.draftReportingPeriod,
        }
      } else if (dialog.draftScopeKind === "all") {
        nextScope = {
          scopeKind: "all",
          ownedLocationId: null,
          ownedLocationName: ALL_LOCATIONS_CHROME_LABEL,
          reportingPeriod: dialog.draftReportingPeriod,
        }
      } else {
        const nextLocationId = dialog.draftOwnedLocationId
        const nextLocation = dialog.locationOptions.find(
          (location) => location.id === nextLocationId
        )
        if (nextLocation == null) {
          nextScope = {
            ...savedScope,
            reportingPeriod: dialog.draftReportingPeriod,
          }
        } else {
          nextScope = {
            scopeKind: "single",
            ownedLocationId: nextLocation.id,
            ownedLocationName: nextLocation.name,
            reportingPeriod: dialog.draftReportingPeriod,
          }
        }
      }
      if (analysisOwnedLocationChanged(savedScope, nextScope)) {
        beginNewChat(nextScope)
        return
      }
      if (state.turnInFlight) {
        return
      }
      const composerIsEmpty = state.composerDraft.trim().length === 0
      const conversationId = state.conversationId
      state = {
        ...state,
        analysisScope: nextScope,
        pendingCampaignDraft: bindPendingDraftLocation(
          state.pendingCampaignDraft,
          nextScope
        ),
        pendingOfferDraft: bindPendingDraftLocation(
          state.pendingOfferDraft,
          nextScope
        ),
        changeScopeDialog: CLOSED_CHANGE_SCOPE_DIALOG,
        placeholderCycleGeneration: composerIsEmpty
          ? state.placeholderCycleGeneration + 1
          : state.placeholderCycleGeneration,
      }
      publish()
      if (conversationId != null) {
        void adapters.applyScope(conversationId, nextScope).then((row) => {
          if (state.conversationId !== conversationId) {
            return
          }
          const scope = row.analysisScope
          state = {
            ...state,
            analysisScope: scope,
            pendingCampaignDraft: bindPendingDraftLocation(
              state.pendingCampaignDraft,
              scope
            ),
            pendingOfferDraft: bindPendingDraftLocation(
              state.pendingOfferDraft,
              scope
            ),
          }
          publish()
        })
      }
    },
    setComposerDraft: (text) => {
      if (mic.getSnapshot().messageLocked) {
        return
      }
      if (state.composerDraft === text) {
        return
      }
      state = { ...state, composerDraft: text }
      publish()
    },
    fillComposerFromChip: (label) => {
      if (
        !isEmptyAssistantConversation(state)
        || state.turnInFlight
        || mic.getSnapshot().submitLocked
      ) {
        return
      }
      if (state.composerDraft === label) {
        return
      }
      state = { ...state, composerDraft: label }
      publish()
    },
    send: () => {
      if (state.bodyLoadError || mic.getSnapshot().submitLocked) {
        return
      }
      if (paidWriteBlocked()) {
        return
      }
      const text = state.composerDraft.trim()
      if (text.length === 0) {
        return
      }
      runTurn(text, false)
    },
    startMic: async () => {
      if (composerControlsLocked()) {
        return
      }
      await mic.start()
    },
    confirmMic: async () => {
      if (paidWriteBlocked()) {
        return
      }
      await mic.confirm()
    },
    cancelMic: async () => {
      await mic.cancel()
    },
    dismissMicError: () => {
      mic.dismissError()
    },
    retry: () => {
      if (!snapshot.retryVisible) {
        return
      }
      const lastUser = [...state.messages]
        .reverse()
        .find((message) => message.role === "user")
      if (lastUser == null) {
        return
      }
      runTurn(lastUser.body, true)
    },
    onTurnProgress: (signal) => {
      if (!state.turnInFlight) {
        return
      }
      const expectedConversationId =
        state.turnConversationId ?? state.conversationId
      if (
        expectedConversationId != null
        && signal.conversationId !== expectedConversationId
      ) {
        return
      }
      if (signal.step === "checking" && waitPipeline !== "checking") {
        return
      }
      const waitBody = {
        checking: formatAssistantWaitGerund(
          assistantWaitGerundAt(waitGerundIndex)
        ),
        retrieving: ASSISTANT_WAIT_RETRIEVING_BODY,
        preparing: ASSISTANT_WAIT_PREPARING_BODY,
      }[signal.step]
      if (signal.step === "retrieving" || signal.step === "preparing") {
        waitPipeline = signal.step
        stopCheckingWaitRotation()
      }
      state = {
        ...state,
        waitBody,
        turnConversationId: signal.conversationId,
      }
      publish()
    },
    toggleHelpful: (messageId, fill) => {
      const message = state.messages.find((item) => item.id === messageId)
      if (message == null || message.class !== "grounded") {
        return
      }
      const current = state.helpfulFills[messageId]
      const nextFills = { ...state.helpfulFills }
      if (current === fill) {
        delete nextFills[messageId]
      } else {
        nextFills[messageId] = fill
      }
      state = { ...state, helpfulFills: nextFills }
      publish()
    },
    clickAction: (action) => {
      if (state.turnInFlight || action.clickable === false) {
        return
      }
      const analysisScope = state.analysisScope
      if (analysisScope == null) {
        return
      }
      if (isCompletingCampaignAction(action.type)) {
        const campaignId = action.campaignId
        if (campaignId == null || state.actionInFlight) {
          return
        }
        state = {
          ...state,
          actionInFlight: true,
          completingCampaignNavigateId: campaignId,
        }
        publish()
        void adapters
          .getCampaignDraft(campaignId)
          .then((campaign) => {
            if (
              !canOpenStoredDraftFromAssistant(
                campaign,
                analysisScope.ownedLocationId
              )
            ) {
              throw new Error("Campaign draft cannot open.")
            }
            adapters.navigateAction({
              action,
              analysisScope,
              campaignDraft: campaign,
            })
            closeDrawer()
          })
          .catch(() => {
            // Open-failure: stay in the Assistant; the row stays clickable.
            state = {
              ...state,
              actionInFlight: false,
              completingCampaignNavigateId: null,
            }
            publish()
          })
        return
      }
      if (isCompletingOfferAction(action.type)) {
        const offerId = action.offerId
        if (offerId == null || state.actionInFlight) {
          return
        }
        state = {
          ...state,
          actionInFlight: true,
          completingOfferNavigateId: offerId,
        }
        publish()
        void adapters
          .getCatalogOffer(offerId)
          .then((offer) => {
            if (
              !canOpenStoredOfferFromAssistant(
                offer,
                analysisScope.ownedLocationId
              )
            ) {
              throw new Error("Offer draft cannot open.")
            }
            adapters.navigateAction({
              action,
              analysisScope,
              catalogOffer: offer,
            })
            closeDrawer()
          })
          .catch(() => {
            // Open-failure: stay in the Assistant; the row stays clickable.
            state = {
              ...state,
              actionInFlight: false,
              completingOfferNavigateId: null,
            }
            publish()
          })
        return
      }
      if (isRetiredDraftActionType(action.type)) {
        return
      }
      if (action.type === "open-recovery") {
        const payload = state.pendingRecoveryDraft
        const latest = state.messages.at(-1)
        if (
          payload == null
          || state.actionInFlight
          || latest?.role !== "assistant"
          || !latest.actions?.some((item) => item.type === "open-recovery")
        ) {
          return
        }
        state = { ...state, actionInFlight: true }
        publish()
        void adapters
          .prepareOpenRecovery({
            feedbackId: payload.feedbackId,
            intent: payload.intent,
            offerId: payload.offerId ?? null,
          })
          .then(() => adapters.openRecoveryFromDraftAction(payload))
          .then(() => {
            state = {
              ...state,
              actionInFlight: false,
            }
            publish()
            closeDrawer()
            adapters.navigateAction({
              action,
              analysisScope,
              recoveryDraft: payload,
            })
          })
          .catch((error: unknown) => {
            state = { ...state, actionInFlight: false }
            const message =
              error instanceof Error && error.message.trim() !== ""
                ? error.message
                : "Could not open recovery. Please try again."
            adapters.notifyRecoveryDraftError(message)
            publish()
          })
        return
      }
      leaveExpand()
      adapters.navigateAction({ action, analysisScope })
    },
    dismissFromEscape: () => {
      if (!state.drawerOpen) {
        return
      }
      if (state.deleteConfirmConversationId != null) {
        state = { ...state, deleteConfirmConversationId: null }
        publish()
        return
      }
      if (state.changeScopeDialog.open) {
        state = { ...state, changeScopeDialog: CLOSED_CHANGE_SCOPE_DIALOG }
        publish()
        return
      }
      closeDrawer()
    },
    viewUsage: () => {
      if (!assistantCreditsShowViewUsage(creditsChrome.accessLevel)) {
        return
      }
      const href = assistantCreditsViewUsageHref(
        adapters.getDashboardMode(),
        adapters.getDashboardOwnedLocation().id
      )
      closeDrawer()
      adapters.navigateBillingHref(href)
    },
    addCredits: () => {
      if (
        !assistantCreditsShowAddCredits({
          accessLevel: creditsChrome.accessLevel,
          permissionRole: creditsChrome.permissionRole,
        })
      ) {
        return
      }
      const href = assistantCreditsAddCreditsHref(
        adapters.getDashboardMode(),
        adapters.getDashboardOwnedLocation().id
      )
      closeDrawer()
      adapters.navigateBillingHref(href)
    },
    followRestorationHelper: () => {
      const helper = snapshot.restorationHelper
      if (helper == null) {
        return
      }
      closeDrawer()
      adapters.navigateBillingHref(helper.href)
    },
  }
}
