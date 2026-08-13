import {
  DEFAULT_HOME_PERFORMANCE_DATE_RANGE,
  labelForHomePerformanceDateRange,
  type HomePerformanceDateRange,
} from "@/lib/operatorHome/homePerformanceDateRange"

import {
  filterConversationsByTitle,
  formatConversationListMeta,
  groupRecentConversations,
  sortNewestFirst,
  type OperatorAiAssistantListItem,
  type OperatorAiAssistantRecentGroup,
} from "./assistantConversationList"
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

export type OperatorAiAssistantWidthMode = "collapsed" | "expanded"

export type OperatorAiAssistantView = "empty" | "recent" | "archive" | "thread"

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

export type OperatorAiAssistantHelpfulFill = "helpful" | "not-helpful"

export type OperatorAiAssistantAction = {
  type: string
  label: string
  tab?: string | null
  sentiment?: string | null
  detectedTag?: string | null
  count?: number | null
  offerId?: number | null
  guestId?: number | null
  smartGroup?: string | null
  marketingEligible?: boolean | null
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
}

export type OperatorAiAssistantOwnedLocationOption = {
  id: number
  name: string
}

export type OperatorAiAssistantAnalysisScope = {
  ownedLocationId: number
  ownedLocationName: string
  reportingPeriod: HomePerformanceDateRange
}

export const CHANGE_ANALYSIS_SCOPE_TITLE = "Change analysis scope"
export const ASSISTANT_WAIT_BODY = "Working…"
export const ASSISTANT_COMPOSER_PLACEHOLDER = "Ask AI Assistant..."
export const ASSISTANT_FAILURE_BODY =
  "The answer could not be completed. Retry this turn."

export type OperatorAiAssistantChangeScopeDialogSnapshot = {
  open: boolean
  title: typeof CHANGE_ANALYSIS_SCOPE_TITLE
  showsOwnedLocationField: boolean
  draftOwnedLocationId: number
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
  deleteConfirm: OperatorAiAssistantDeleteConfirmSnapshot
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
  }) => void
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
}

export type OperatorAiAssistantModule = {
  getSnapshot: () => OperatorAiAssistantSnapshot
  subscribe: (listener: () => void) => () => void
  openDrawer: (input?: { operatorFirstName?: string }) => void
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
  setChangeScopeDraftLocation: (locationId: number) => void
  setChangeScopeDraftReportingPeriod: (
    reportingPeriod: HomePerformanceDateRange
  ) => void
  cancelChangeScope: () => void
  applyChangeScope: () => void
  setComposerDraft: (text: string) => void
  fillComposerFromChip: (label: string) => void
  send: () => void
  retry: () => void
  toggleHelpful: (
    messageId: string,
    fill: OperatorAiAssistantHelpfulFill
  ) => void
  clickAction: (action: OperatorAiAssistantAction) => void
}

const EMPTY_HEADLINE = "What would you like help with?"
const EMPTY_BODY =
  "Ask about feedback, guests, offers, campaigns or performance for this restaurant."

const DEFAULT_OWNED_LOCATION: OperatorAiAssistantOwnedLocationOption = {
  id: 1,
  name: "Camden",
}

const WAIT_MESSAGE: OperatorAiAssistantMessage = {
  id: "wait",
  role: "wait",
  body: ASSISTANT_WAIT_BODY,
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

export function buildEmptyComposerPlaceholders(
  scope: OperatorAiAssistantAnalysisScope
): readonly string[] {
  const periodPhrase = periodPhraseForReportingPeriod(scope.reportingPeriod)
  return [
    `Summarise feedback from ${periodPhrase}\u2026`,
    `What needs attention at ${scope.ownedLocationName}?`,
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
    ownedLocationId: ownedLocation.id,
    ownedLocationName: ownedLocation.name,
    reportingPeriod: DEFAULT_HOME_PERFORMANCE_DATE_RANGE,
  }
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
    body: `There is nothing to summarise or list at ${scope.ownedLocationName} over ${period}.`,
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
  lastNavigate: {
    action: OperatorAiAssistantAction
    analysisScope: OperatorAiAssistantAnalysisScope
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
    lastNavigate: null as {
      action: OperatorAiAssistantAction
      analysisScope: OperatorAiAssistantAnalysisScope
    } | null,
  }

  const defaults: OperatorAiAssistantAdapters = {
    closePeerRightDrawers: () => {},
    isOnline: () => extras.online,
    navigateAction: (input) => {
      extras.lastNavigate = input
    },
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
        .map((row) => ({
          id: row.id,
          title: row.title,
          ownedLocationName: row.analysisScope.ownedLocationName,
          lastActivityAt: row.lastActivityAt,
          isArchived: row.isArchived,
        }))
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
  }

  return {
    ...defaults,
    ...overrides,
    conversations,
    get lastNavigate() {
      return extras.lastNavigate
    },
    get online() {
      return extras.online
    },
    set online(value: boolean) {
      extras.online = value
    },
    isOnline: overrides.isOnline ?? (() => extras.online),
  }
}

type ChangeScopeDialogState = {
  open: boolean
  showsOwnedLocationField: boolean
  draftOwnedLocationId: number
  draftReportingPeriod: HomePerformanceDateRange
  locationOptions: readonly OperatorAiAssistantOwnedLocationOption[]
}

type AssistantListStatus = "idle" | "loading" | "loaded" | "offline" | "error"

type AssistantState = {
  drawerOpen: boolean
  widthMode: OperatorAiAssistantWidthMode
  view: OperatorAiAssistantView
  conversationId: string | null
  operatorFirstName: string
  restaurantName: string
  analysisScope: OperatorAiAssistantAnalysisScope | null
  changeScopeDialog: ChangeScopeDialogState
  composerDraft: string
  placeholderCycleGeneration: number
  messages: OperatorAiAssistantMessage[]
  turnInFlight: boolean
  helpfulFills: Record<string, OperatorAiAssistantHelpfulFill>
  searchQuery: string
  listItems: OperatorAiAssistantListItem[]
  listStatus: AssistantListStatus
  bodyLoadError: boolean
  failedBodyConversationId: string | null
  deleteConfirmConversationId: string | null
}

const CLOSED_CHANGE_SCOPE_DIALOG: ChangeScopeDialogState = {
  open: false,
  showsOwnedLocationField: false,
  draftOwnedLocationId: 0,
  draftReportingPeriod: DEFAULT_HOME_PERFORMANCE_DATE_RANGE,
  locationOptions: [],
}

const INITIAL_STATE: AssistantState = {
  drawerOpen: false,
  widthMode: "collapsed",
  view: "empty",
  conversationId: null,
  operatorFirstName: "Operator",
  restaurantName: "",
  analysisScope: null,
  changeScopeDialog: CLOSED_CHANGE_SCOPE_DIALOG,
  composerDraft: "",
  placeholderCycleGeneration: 0,
  messages: [],
  turnInFlight: false,
  helpfulFills: {},
  searchQuery: "",
  listItems: [],
  listStatus: "idle",
  bodyLoadError: false,
  failedBodyConversationId: null,
  deleteConfirmConversationId: null,
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
    draftOwnedLocationId: dialog.draftOwnedLocationId,
    draftReportingPeriod: dialog.draftReportingPeriod,
    locationOptions: dialog.locationOptions,
  }
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
  const isArchive = state.view === "archive"
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

function toSnapshot(
  state: AssistantState,
  nowMs: number
): OperatorAiAssistantSnapshot {
  const emptyConversation = isEmptyAssistantConversation(state)
  const storedMessages = state.messages.filter((message) => message.role !== "wait")
  const displayMessages = state.turnInFlight
    ? [...storedMessages, WAIT_MESSAGE]
    : storedMessages
  const lastAssistant = [...displayMessages]
    .reverse()
    .find((message) => message.role === "assistant")
  const retryVisible =
    lastAssistant?.class === "failure"
    && analysisScopesEqual(lastUserScope(storedMessages), state.analysisScope)
  const suggestionChips = emptyConversation ? EMPTY_SUGGESTION_CHIPS : []
  const chrome = listChromeFor(state)
  const filtered = filterConversationsByTitle(state.listItems, state.searchQuery)
  const visibleRows =
    state.view === "archive" ? sortNewestFirst(state.listItems) : filtered
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
  const recentGroups =
    state.view === "recent" && chrome.listChromeKind === "rows"
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
    sendLocked: state.turnInFlight,
    chipsLocked: state.turnInFlight,
    retryVisible,
    helpfulFills: state.helpfulFills,
    searchQuery: state.searchQuery,
    listStatus: state.listStatus,
    listChromeKind: chrome.listChromeKind,
    listTitle: chrome.listTitle,
    listHeading: chrome.listHeading,
    listBody: chrome.listBody,
    listCountLabel:
      chrome.listChromeKind === "rows"
        ? conversationCountLabel(presentedRows.length)
        : null,
    showStartConversation: chrome.showStartConversation,
    showListRetry: chrome.showListRetry,
    showSearch: chrome.showSearch,
    showArchiveFooter: chrome.showArchiveFooter,
    recentGroups,
    archiveRows: state.view === "archive" ? presentedRows : [],
    listRows: presentedRows,
    bodyLoadError: state.bodyLoadError,
    sendBlocked: state.turnInFlight || state.bodyLoadError,
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
    turnInFlight: false,
  }
}

function emptyGreetingState(
  state: AssistantState,
  adapters: OperatorAiAssistantAdapters,
  operatorFirstName?: string
): AssistantState {
  return {
    ...state,
    view: "empty",
    conversationId: null,
    operatorFirstName: operatorFirstName?.trim() || state.operatorFirstName,
    restaurantName: adapters.getRestaurantName(),
    analysisScope: copyDashboardAnalysisScope(adapters),
    changeScopeDialog: CLOSED_CHANGE_SCOPE_DIALOG,
    composerDraft: "",
    placeholderCycleGeneration: state.placeholderCycleGeneration + 1,
    messages: [],
    turnInFlight: false,
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
  let snapshot = toSnapshot(state, adapters.nowMs())
  const listeners = new Set<() => void>()
  let sendGeneration = 0
  let listGeneration = 0
  let inflight: AbortController | null = null

  const publish = () => {
    snapshot = toSnapshot(state, adapters.nowMs())
    for (const listener of listeners) {
      listener()
    }
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
  }

  const showEmptyGreeting = (operatorFirstName?: string) => {
    state = emptyGreetingState(state, adapters, operatorFirstName)
    publish()
  }

  const openDrawer = (input?: { operatorFirstName?: string }) => {
    adapters.closePeerRightDrawers()
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
    state = { ...state, widthMode: "expanded" }
    publish()
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
    state = {
      ...state,
      changeScopeDialog: {
        open: true,
        showsOwnedLocationField: mode === "multi",
        draftOwnedLocationId: state.analysisScope.ownedLocationId,
        draftReportingPeriod: state.analysisScope.reportingPeriod,
        locationOptions: adapters.listOwnedLocations(),
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

  const runTurn = (message: string, replaceFailure: boolean) => {
    if (state.turnInFlight) {
      return
    }
    if (!adapters.isOnline()) {
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

    state = {
      ...state,
      view: "thread",
      composerDraft: "",
      messages: storedMessages,
      turnInFlight: true,
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
        state = {
          ...applyConversation(state, row),
          composerDraft: "",
        }
        publish()
      })
      .catch((error: unknown) => {
        if (generation !== sendGeneration) {
          return
        }
        inflight = null
        if (isAbortError(error)) {
          state = { ...state, turnInFlight: false }
          publish()
          return
        }
        state = {
          ...state,
          turnInFlight: false,
          messages: [...storedMessages, failureMessage()],
        }
        publish()
      })
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
    closeDrawer,
    setOpen: (open) => {
      if (open) {
        openDrawer()
      } else {
        closeDrawer()
      }
    },
    startNewChat: () => {
      abortInFlight()
      showEmptyGreeting()
    },
    openRecent: () => {
      state = {
        ...state,
        view: "recent",
        searchQuery: "",
        deleteConfirmConversationId: null,
      }
      publish()
      loadList(false)
    },
    openArchive: () => {
      state = {
        ...state,
        view: "archive",
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
      if (state.view !== "recent" || state.searchQuery === query) {
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
      if (state.turnInFlight) {
        return
      }
      const dialog = state.changeScopeDialog
      const nextLocationId = dialog.showsOwnedLocationField
        ? dialog.draftOwnedLocationId
        : state.analysisScope.ownedLocationId
      const nextLocation =
        dialog.locationOptions.find((location) => location.id === nextLocationId)
        ?? {
          id: state.analysisScope.ownedLocationId,
          name: state.analysisScope.ownedLocationName,
        }
      const nextScope: OperatorAiAssistantAnalysisScope = {
        ...state.analysisScope,
        ownedLocationId: nextLocation.id,
        ownedLocationName: nextLocation.name,
        reportingPeriod: dialog.draftReportingPeriod,
      }
      const composerIsEmpty = state.composerDraft.trim().length === 0
      const conversationId = state.conversationId
      state = {
        ...state,
        analysisScope: nextScope,
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
          state = {
            ...state,
            analysisScope: row.analysisScope,
          }
          publish()
        })
      }
    },
    setComposerDraft: (text) => {
      if (state.composerDraft === text) {
        return
      }
      state = { ...state, composerDraft: text }
      publish()
    },
    fillComposerFromChip: (label) => {
      if (!isEmptyAssistantConversation(state) || state.turnInFlight) {
        return
      }
      if (state.composerDraft === label) {
        return
      }
      state = { ...state, composerDraft: label }
      publish()
    },
    send: () => {
      if (state.bodyLoadError) {
        return
      }
      const text = state.composerDraft.trim()
      if (text.length === 0) {
        return
      }
      runTurn(text, false)
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
      if (state.turnInFlight) {
        return
      }
      const analysisScope =
        lastUserScope(state.messages) ?? state.analysisScope
      if (analysisScope == null) {
        return
      }
      leaveExpand()
      adapters.navigateAction({ action, analysisScope })
    },
  }
}
