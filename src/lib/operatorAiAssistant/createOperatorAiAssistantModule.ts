export type OperatorAiAssistantWidthMode = "collapsed"

export type OperatorAiAssistantView = "empty" | "recent"

export type OperatorAiAssistantGreeting = {
  hello: string
  headline: string
  body: string
}

export type OperatorAiAssistantConversationRow = {
  id: string
}

export type OperatorAiAssistantSnapshot = {
  drawerOpen: boolean
  widthMode: OperatorAiAssistantWidthMode
  view: OperatorAiAssistantView
  conversationId: string | null
  greeting: OperatorAiAssistantGreeting
}

export type OperatorAiAssistantAdapters = {
  closePeerRightDrawers: () => void
  createConversation: () => Promise<OperatorAiAssistantConversationRow>
}

export type OperatorAiAssistantModule = {
  getSnapshot: () => OperatorAiAssistantSnapshot
  subscribe: (listener: () => void) => () => void
  openDrawer: (input?: { operatorFirstName?: string }) => void
  closeDrawer: () => void
  setOpen: (open: boolean) => void
  startNewChat: () => void
  openRecent: () => void
}

const EMPTY_HEADLINE = "What would you like help with?"
const EMPTY_BODY =
  "Ask about feedback, guests, offers, campaigns or performance for this restaurant."

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

export function createInMemoryOperatorAiAssistantAdapters(
  overrides: Partial<OperatorAiAssistantAdapters> = {}
): OperatorAiAssistantAdapters & {
  conversations: OperatorAiAssistantConversationRow[]
} {
  const conversations: OperatorAiAssistantConversationRow[] = []

  return {
    conversations,
    closePeerRightDrawers: () => {},
    createConversation: async () => {
      const row = { id: `conv-${conversations.length + 1}` }
      conversations.push(row)
      return row
    },
    ...overrides,
  }
}

type AssistantState = {
  drawerOpen: boolean
  widthMode: OperatorAiAssistantWidthMode
  view: OperatorAiAssistantView
  conversationId: string | null
  operatorFirstName: string
}

const INITIAL_STATE: AssistantState = {
  drawerOpen: false,
  widthMode: "collapsed",
  view: "empty",
  conversationId: null,
  operatorFirstName: "Operator",
}

function toSnapshot(state: AssistantState): OperatorAiAssistantSnapshot {
  return {
    drawerOpen: state.drawerOpen,
    widthMode: state.widthMode,
    view: state.view,
    conversationId: state.conversationId,
    greeting: buildAssistantEmptyGreeting(state.operatorFirstName),
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
    }
    publish()
  }

  const closeDrawer = () => {
    if (!state.drawerOpen) {
      return
    }
    state = { ...state, drawerOpen: false }
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
      }
      publish()
    },
    openRecent: () => {
      state = { ...state, view: "recent" }
      publish()
    },
  }
}
