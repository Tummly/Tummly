import {
  DEFAULT_HOME_PERFORMANCE_DATE_RANGE,
  labelForHomePerformanceDateRange,
  type HomePerformanceDateRange,
} from "@/lib/operatorHome/homePerformanceDateRange"

export type OperatorAiAssistantWidthMode = "collapsed" | "expanded"

export type OperatorAiAssistantView = "empty" | "recent"

export type OperatorAiAssistantGreeting = {
  hello: string
  headline: string
  body: string
}

export type OperatorAiAssistantConversationRow = {
  id: string
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
}

export type OperatorAiAssistantAdapters = {
  closePeerRightDrawers: () => void
  createConversation: () => Promise<OperatorAiAssistantConversationRow>
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
}

const EMPTY_HEADLINE = "What would you like help with?"
const EMPTY_BODY =
  "Ask about feedback, guests, offers, campaigns or performance for this restaurant."

const DEFAULT_OWNED_LOCATION: OperatorAiAssistantOwnedLocationOption = {
  id: 1,
  name: "Camden",
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

export function createInMemoryOperatorAiAssistantAdapters(
  overrides: Partial<OperatorAiAssistantAdapters> = {}
): OperatorAiAssistantAdapters & {
  conversations: OperatorAiAssistantConversationRow[]
} {
  const conversations: OperatorAiAssistantConversationRow[] = []
  const ownedLocations: OperatorAiAssistantOwnedLocationOption[] = [
    DEFAULT_OWNED_LOCATION,
    { id: 2, name: "Shoreditch" },
  ]

  return {
    conversations,
    closePeerRightDrawers: () => {},
    createConversation: async () => {
      const row = { id: `conv-${conversations.length + 1}` }
      conversations.push(row)
      return row
    },
    getDashboardOwnedLocation: () => DEFAULT_OWNED_LOCATION,
    getRestaurantName: () => "Mehmet's Grill",
    getDashboardMode: () => "multi",
    listOwnedLocations: () => ownedLocations,
    ...overrides,
  }
}

type ChangeScopeDialogState = {
  open: boolean
  showsOwnedLocationField: boolean
  draftOwnedLocationId: number
  draftReportingPeriod: HomePerformanceDateRange
  locationOptions: readonly OperatorAiAssistantOwnedLocationOption[]
}

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

function toSnapshot(state: AssistantState): OperatorAiAssistantSnapshot {
  const emptyConversation = isEmptyAssistantConversation(state)
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
    suggestionChips: emptyConversation ? EMPTY_SUGGESTION_CHIPS : [],
  }
}

export function createOperatorAiAssistantModule(
  adapters: OperatorAiAssistantAdapters
): OperatorAiAssistantModule {
  let state: AssistantState = { ...INITIAL_STATE }
  let snapshot = toSnapshot(state)
  const listeners = new Set<() => void>()

  const publish = () => {
    snapshot = toSnapshot(state)
    for (const listener of listeners) {
      listener()
    }
  }

  const openDrawer = (input?: { operatorFirstName?: string }) => {
    adapters.closePeerRightDrawers()
    state = {
      ...state,
      drawerOpen: true,
      widthMode: "collapsed",
      view: "empty",
      conversationId: null,
      operatorFirstName: input?.operatorFirstName?.trim() || state.operatorFirstName,
      restaurantName: adapters.getRestaurantName(),
      analysisScope: copyDashboardAnalysisScope(adapters),
      changeScopeDialog: CLOSED_CHANGE_SCOPE_DIALOG,
      composerDraft: "",
      placeholderCycleGeneration: state.placeholderCycleGeneration + 1,
    }
    publish()
  }

  const closeDrawer = () => {
    if (!state.drawerOpen) {
      return
    }
    state = {
      ...state,
      drawerOpen: false,
      widthMode: "collapsed",
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
      state = {
        ...state,
        view: "empty",
        conversationId: null,
        restaurantName: adapters.getRestaurantName(),
        analysisScope: copyDashboardAnalysisScope(adapters),
        changeScopeDialog: CLOSED_CHANGE_SCOPE_DIALOG,
        composerDraft: "",
        placeholderCycleGeneration: state.placeholderCycleGeneration + 1,
      }
      publish()
    },
    openRecent: () => {
      state = { ...state, view: "recent" }
      publish()
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
      const composerIsEmpty = state.composerDraft.trim().length === 0
      state = {
        ...state,
        analysisScope: {
          ...state.analysisScope,
          ownedLocationId: nextLocation.id,
          ownedLocationName: nextLocation.name,
          reportingPeriod: dialog.draftReportingPeriod,
        },
        changeScopeDialog: CLOSED_CHANGE_SCOPE_DIALOG,
        placeholderCycleGeneration: composerIsEmpty
          ? state.placeholderCycleGeneration + 1
          : state.placeholderCycleGeneration,
      }
      publish()
    },
    setComposerDraft: (text) => {
      if (state.composerDraft === text) {
        return
      }
      state = { ...state, composerDraft: text }
      publish()
    },
    fillComposerFromChip: (label) => {
      if (!isEmptyAssistantConversation(state)) {
        return
      }
      if (state.composerDraft === label) {
        return
      }
      state = { ...state, composerDraft: label }
      publish()
    },
  }
}
