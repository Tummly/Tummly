export type OperatorNotificationCategory =
  | "product-updates"
  | "account-notices"
  | "weekly-brief-reminders"
  | "tips-and-playbooks"
  | "campaign-and-report-updates"

export type OperatorNotificationPreferences = Record<
  OperatorNotificationCategory,
  boolean
>

export const DEFAULT_NOTIFICATION_PREFERENCES: OperatorNotificationPreferences =
  {
    "product-updates": true,
    "account-notices": true,
    "weekly-brief-reminders": true,
    "tips-and-playbooks": true,
    "campaign-and-report-updates": true,
  }

export type OperatorNotification = {
  id: number
  userId: number
  category: string
  type: string
  title: string
  body: string
  createdAt: string
  readAt?: string | null
  ctaLabel?: string | null
  ctaHref?: string | null
  capability?: string | null
  dedupeKey?: string | null
}

export type OperatorNotificationsTab =
  | "all"
  | "product"
  | "account"
  | "unread"

export type NotificationVisibleFilter = {
  category?: string
  unreadOnly?: boolean
}

export type OperatorNotificationsSnapshot = {
  items: OperatorNotification[]
  filteredItems: OperatorNotification[]
  unreadCount: number
  loadStatus: "idle" | "loading" | "loaded" | "error"
  drawerOpen: boolean
  activeTab: OperatorNotificationsTab
  markReadBusy: boolean
  loadError: string | null
  settingsOpen: boolean
  preferences: OperatorNotificationPreferences
  preferencesStatus: "idle" | "loading" | "loaded" | "error"
  preferencesBusy: boolean
  preferencesError: string | null
}

export type OperatorNotificationsRealtimeHandlers = {
  onNotificationCreated: (notification: OperatorNotification) => void
  onReconnected: () => void
}

export type OperatorNotificationsRealtimeSession = {
  stop: () => Promise<void>
}

export type OperatorNotificationsAdapters = {
  listNotifications: () => Promise<{
    unreadCount: number
    items: OperatorNotification[]
  }>
  markOneRead: (id: number) => Promise<void>
  markInboxRead: () => Promise<void>
  markVisibleRead: (filter: NotificationVisibleFilter) => Promise<void>
  getPreferences: () => Promise<OperatorNotificationPreferences>
  setPreferences: (
    preferences: OperatorNotificationPreferences
  ) => Promise<OperatorNotificationPreferences>
  navigate: (href: string) => void
  connectRealtime: (
    handlers: OperatorNotificationsRealtimeHandlers
  ) => Promise<OperatorNotificationsRealtimeSession>
  ensureSeeds: () => Promise<{ reToast: OperatorNotification[] }>
  showToast: (notification: OperatorNotification) => void
}

export type OperatorNotificationsModule = {
  getSnapshot: () => OperatorNotificationsSnapshot
  subscribe: (listener: () => void) => () => void
  connect: () => Promise<void>
  disconnect: () => Promise<void>
  load: () => Promise<void>
  openDrawer: () => Promise<void>
  closeDrawer: () => void
  setTab: (tab: OperatorNotificationsTab) => void
  markOneRead: (id: number) => void
  markVisibleRead: () => void
  activateCta: (id: number) => void
  openSettings: () => Promise<void>
  closeSettings: () => void
  setPreference: (
    category: OperatorNotificationCategory,
    enabled: boolean
  ) => void
  reset: () => void
}

export type OperatorNotificationsModuleOptions = {
  now?: () => string
}

const LOAD_ERROR = "Could not load Notifications. Please try again."
const PREFERENCES_LOAD_ERROR =
  "Could not load Notification preferences. Please try again."
const PREFERENCES_SAVE_ERROR =
  "Could not save Notification preferences. Please try again."

const PRODUCT_CATEGORY = "product-updates"
const ACCOUNT_CATEGORY = "account-notices"

type NotificationsState = {
  items: OperatorNotification[]
  unreadCount: number
  loadStatus: OperatorNotificationsSnapshot["loadStatus"]
  drawerOpen: boolean
  activeTab: OperatorNotificationsTab
  markReadBusy: boolean
  loadError: string | null
  loadGeneration: number
  settingsOpen: boolean
  preferences: OperatorNotificationPreferences
  preferencesStatus: OperatorNotificationsSnapshot["preferencesStatus"]
  preferencesBusy: boolean
  preferencesError: string | null
  preferencesGeneration: number
}

type NotificationsAction =
  | { type: "reset" }
  | { type: "load_started"; generation: number }
  | {
      type: "load_succeeded"
      generation: number
      items: OperatorNotification[]
      unreadCount: number
    }
  | { type: "load_failed"; generation: number; error: string }
  | { type: "drawer_opened" }
  | { type: "drawer_closed" }
  | { type: "tab_set"; tab: OperatorNotificationsTab }
  | {
      type: "items_replaced"
      items: OperatorNotification[]
      unreadCount: number
    }
  | { type: "mark_read_busy"; busy: boolean }
  | { type: "settings_opened" }
  | { type: "settings_closed" }
  | { type: "preferences_load_started"; generation: number }
  | {
      type: "preferences_load_succeeded"
      generation: number
      preferences: OperatorNotificationPreferences
    }
  | { type: "preferences_load_failed"; generation: number; error: string }
  | {
      type: "preferences_set"
      preferences: OperatorNotificationPreferences
    }
  | { type: "preferences_busy"; busy: boolean }
  | { type: "preferences_error"; error: string | null }

function sortNewestFirst(
  items: OperatorNotification[]
): OperatorNotification[] {
  return [...items].sort((a, b) => {
    const aMs = new Date(a.createdAt).getTime()
    const bMs = new Date(b.createdAt).getTime()
    if (bMs !== aMs) {
      return bMs - aMs
    }
    return b.id - a.id
  })
}

function countUnread(items: OperatorNotification[]): number {
  return items.reduce((count, item) => (item.readAt == null ? count + 1 : count), 0)
}

function isUnread(item: OperatorNotification): boolean {
  return item.readAt == null
}

export function filterNotificationsForTab(
  items: OperatorNotification[],
  tab: OperatorNotificationsTab
): OperatorNotification[] {
  switch (tab) {
    case "product":
      return items.filter((item) => item.category === PRODUCT_CATEGORY)
    case "account":
      return items.filter((item) => item.category === ACCOUNT_CATEGORY)
    case "unread":
      return items.filter(isUnread)
    case "all":
    default:
      return items
  }
}

export function visibleFilterForTab(
  tab: OperatorNotificationsTab
): NotificationVisibleFilter {
  switch (tab) {
    case "product":
      return { category: PRODUCT_CATEGORY }
    case "account":
      return { category: ACCOUNT_CATEGORY }
    case "unread":
      return { unreadOnly: true }
    case "all":
    default:
      return {}
  }
}

function markItemsRead(
  items: OperatorNotification[],
  predicate: (item: OperatorNotification) => boolean,
  readAt: string
): OperatorNotification[] {
  return items.map((item) => {
    if (!predicate(item) || item.readAt != null) {
      return item
    }
    return { ...item, readAt }
  })
}

function reduce(
  state: NotificationsState,
  action: NotificationsAction
): NotificationsState {
  switch (action.type) {
    case "reset":
      return {
        ...state,
        items: [],
        unreadCount: 0,
        loadStatus: "idle",
        drawerOpen: false,
        activeTab: "all",
        markReadBusy: false,
        loadError: null,
        loadGeneration: state.loadGeneration + 1,
        settingsOpen: false,
        preferences: { ...DEFAULT_NOTIFICATION_PREFERENCES },
        preferencesStatus: "idle",
        preferencesBusy: false,
        preferencesError: null,
        preferencesGeneration: state.preferencesGeneration + 1,
      }
    case "load_started":
      return {
        ...state,
        loadStatus: "loading",
        loadGeneration: action.generation,
        loadError: null,
      }
    case "load_succeeded":
      if (action.generation !== state.loadGeneration) {
        return state
      }
      return {
        ...state,
        loadStatus: "loaded",
        items: sortNewestFirst(action.items),
        unreadCount: action.unreadCount,
        loadError: null,
      }
    case "load_failed":
      if (action.generation !== state.loadGeneration) {
        return state
      }
      return {
        ...state,
        loadStatus: "error",
        loadError: action.error,
      }
    case "drawer_opened":
      return { ...state, drawerOpen: true }
    case "drawer_closed":
      return { ...state, drawerOpen: false, settingsOpen: false }
    case "tab_set":
      return { ...state, activeTab: action.tab }
    case "items_replaced":
      return {
        ...state,
        items: sortNewestFirst(action.items),
        unreadCount: action.unreadCount,
      }
    case "mark_read_busy":
      return { ...state, markReadBusy: action.busy }
    case "settings_opened":
      return { ...state, settingsOpen: true }
    case "settings_closed":
      return { ...state, settingsOpen: false }
    case "preferences_load_started":
      return {
        ...state,
        preferencesStatus: "loading",
        preferencesGeneration: action.generation,
        preferencesError: null,
      }
    case "preferences_load_succeeded":
      if (action.generation !== state.preferencesGeneration) {
        return state
      }
      return {
        ...state,
        preferencesStatus: "loaded",
        preferences: action.preferences,
        preferencesError: null,
      }
    case "preferences_load_failed":
      if (action.generation !== state.preferencesGeneration) {
        return state
      }
      return {
        ...state,
        preferencesStatus: "error",
        preferencesError: action.error,
      }
    case "preferences_set":
      return {
        ...state,
        preferences: action.preferences,
        preferencesStatus: "loaded",
        preferencesError: null,
      }
    case "preferences_busy":
      return { ...state, preferencesBusy: action.busy }
    case "preferences_error":
      return { ...state, preferencesError: action.error }
    default:
      return state
  }
}

function toSnapshot(state: NotificationsState): OperatorNotificationsSnapshot {
  return {
    items: state.items,
    filteredItems: filterNotificationsForTab(state.items, state.activeTab),
    unreadCount: state.unreadCount,
    loadStatus: state.loadStatus,
    drawerOpen: state.drawerOpen,
    activeTab: state.activeTab,
    markReadBusy: state.markReadBusy,
    loadError: state.loadError,
    settingsOpen: state.settingsOpen,
    preferences: state.preferences,
    preferencesStatus: state.preferencesStatus,
    preferencesBusy: state.preferencesBusy,
    preferencesError: state.preferencesError,
  }
}

export function normalizeNotificationPreferences(
  value: Partial<OperatorNotificationPreferences> | null | undefined
): OperatorNotificationPreferences {
  return {
    "product-updates":
      value?.["product-updates"] ??
      DEFAULT_NOTIFICATION_PREFERENCES["product-updates"],
    "account-notices":
      value?.["account-notices"] ??
      DEFAULT_NOTIFICATION_PREFERENCES["account-notices"],
    "weekly-brief-reminders":
      value?.["weekly-brief-reminders"] ??
      DEFAULT_NOTIFICATION_PREFERENCES["weekly-brief-reminders"],
    "tips-and-playbooks":
      value?.["tips-and-playbooks"] ??
      DEFAULT_NOTIFICATION_PREFERENCES["tips-and-playbooks"],
    "campaign-and-report-updates":
      value?.["campaign-and-report-updates"] ??
      DEFAULT_NOTIFICATION_PREFERENCES["campaign-and-report-updates"],
  }
}

export function createInMemoryOperatorNotificationsAdapters(
  initial: OperatorNotification[] = []
): OperatorNotificationsAdapters {
  let items = initial.map((item) => ({ ...item }))
  let preferences: OperatorNotificationPreferences = {
    ...DEFAULT_NOTIFICATION_PREFERENCES,
  }

  const syncUnread = () => countUnread(items)

  return {
    listNotifications: async () => ({
      unreadCount: syncUnread(),
      items: sortNewestFirst(items.map((item) => ({ ...item }))),
    }),
    markOneRead: async (id) => {
      const readAt = new Date().toISOString()
      items = items.map((item) =>
        item.id === id && item.readAt == null ? { ...item, readAt } : item
      )
    },
    markInboxRead: async () => {
      const readAt = new Date().toISOString()
      items = items.map((item) =>
        item.readAt == null ? { ...item, readAt } : item
      )
    },
    markVisibleRead: async (filter) => {
      const readAt = new Date().toISOString()
      items = items.map((item) => {
        if (item.readAt != null) {
          return item
        }
        if (filter.category != null && item.category !== filter.category) {
          return item
        }
        if (filter.unreadOnly === true && !isUnread(item)) {
          return item
        }
        return { ...item, readAt }
      })
    },
    getPreferences: async () => ({ ...preferences }),
    setPreferences: async (next) => {
      preferences = normalizeNotificationPreferences(next)
      return { ...preferences }
    },
    navigate: () => {},
    connectRealtime: async () => ({
      stop: async () => {},
    }),
    ensureSeeds: async () => ({ reToast: [] }),
    showToast: () => {},
  }
}

export function createOperatorNotificationsModule(
  adapters: OperatorNotificationsAdapters,
  options: OperatorNotificationsModuleOptions = {}
): OperatorNotificationsModule {
  const now = options.now ?? (() => new Date().toISOString())

  let state: NotificationsState = {
    items: [],
    unreadCount: 0,
    loadStatus: "idle",
    drawerOpen: false,
    activeTab: "all",
    markReadBusy: false,
    loadError: null,
    loadGeneration: 0,
    settingsOpen: false,
    preferences: { ...DEFAULT_NOTIFICATION_PREFERENCES },
    preferencesStatus: "idle",
    preferencesBusy: false,
    preferencesError: null,
    preferencesGeneration: 0,
  }

  let snapshot = toSnapshot(state)
  const listeners = new Set<() => void>()

  const emit = () => {
    for (const listener of listeners) {
      listener()
    }
  }

  const publish = () => {
    snapshot = toSnapshot(state)
    emit()
  }

  const dispatch = (action: NotificationsAction) => {
    state = reduce(state, action)
    publish()
  }

  const load = async () => {
    const generation = state.loadGeneration + 1
    dispatch({ type: "load_started", generation })

    try {
      const result = await adapters.listNotifications()
      dispatch({
        type: "load_succeeded",
        generation,
        items: result.items,
        unreadCount: result.unreadCount,
      })
    } catch {
      dispatch({
        type: "load_failed",
        generation,
        error: LOAD_ERROR,
      })
    }
  }

  const applyOptimisticRead = (
    predicate: (item: OperatorNotification) => boolean
  ) => {
    const readAt = now()
    const nextItems = markItemsRead(state.items, predicate, readAt)
    dispatch({
      type: "items_replaced",
      items: nextItems,
      unreadCount: countUnread(nextItems),
    })
  }

  let realtimeSession: OperatorNotificationsRealtimeSession | null = null
  let connectingRealtime = false

  const handleNotificationCreated = (notification: OperatorNotification) => {
    if (state.items.some((item) => item.id === notification.id)) {
      return
    }

    if (state.drawerOpen) {
      const readAt = now()
      const arrived = { ...notification, readAt }
      const nextItems = [...state.items, arrived]
      dispatch({
        type: "items_replaced",
        items: nextItems,
        unreadCount: countUnread(nextItems),
      })
      void adapters.markOneRead(notification.id).catch(() => {
        void load()
      })
      return
    }

    const nextItems = [...state.items, { ...notification, readAt: null }]
    dispatch({
      type: "items_replaced",
      items: nextItems,
      unreadCount: countUnread(nextItems),
    })
    adapters.showToast(notification)
  }

  const ensureRealtime = async () => {
    if (realtimeSession != null || connectingRealtime) {
      return
    }

    connectingRealtime = true
    try {
      realtimeSession = await adapters.connectRealtime({
        onNotificationCreated: handleNotificationCreated,
        onReconnected: () => {
          void load()
        },
      })
    } finally {
      connectingRealtime = false
    }
  }

  const disconnect = async () => {
    const session = realtimeSession
    realtimeSession = null
    if (session != null) {
      await session.stop()
    }
  }

  return {
    getSnapshot: () => snapshot,
    subscribe: (listener) => {
      listeners.add(listener)
      return () => {
        listeners.delete(listener)
      }
    },
    connect: async () => {
      await ensureRealtime()
      try {
        const { reToast } = await adapters.ensureSeeds()
        for (const notification of reToast) {
          adapters.showToast(notification)
        }
      } catch {
        // Seed ensure is best-effort; inbox catch-up still runs.
      }
      await load()
    },
    disconnect,
    load,
    openDrawer: async () => {
      if (state.drawerOpen) {
        return
      }

      dispatch({ type: "drawer_opened" })
      applyOptimisticRead(() => true)

      try {
        await adapters.markInboxRead()
      } catch {
        await load()
      }
    },
    closeDrawer: () => {
      dispatch({ type: "drawer_closed" })
    },
    setTab: (tab) => {
      dispatch({ type: "tab_set", tab })
    },
    markOneRead: (id) => {
      const target = state.items.find((item) => item.id === id)
      if (target == null || target.readAt != null || state.markReadBusy) {
        return
      }

      const previousItems = state.items
      const previousUnread = state.unreadCount
      applyOptimisticRead((item) => item.id === id)
      dispatch({ type: "mark_read_busy", busy: true })

      void adapters
        .markOneRead(id)
        .catch(() => {
          dispatch({
            type: "items_replaced",
            items: previousItems,
            unreadCount: previousUnread,
          })
        })
        .finally(() => {
          dispatch({ type: "mark_read_busy", busy: false })
        })
    },
    markVisibleRead: () => {
      if (state.markReadBusy) {
        return
      }

      const filter = visibleFilterForTab(state.activeTab)
      const visibleIds = new Set(
        filterNotificationsForTab(state.items, state.activeTab)
          .filter(isUnread)
          .map((item) => item.id)
      )

      if (visibleIds.size === 0) {
        return
      }

      const previousItems = state.items
      const previousUnread = state.unreadCount
      applyOptimisticRead((item) => visibleIds.has(item.id))
      dispatch({ type: "mark_read_busy", busy: true })

      void adapters
        .markVisibleRead(filter)
        .catch(() => {
          dispatch({
            type: "items_replaced",
            items: previousItems,
            unreadCount: previousUnread,
          })
        })
        .finally(() => {
          dispatch({ type: "mark_read_busy", busy: false })
        })
    },
    activateCta: (id) => {
      const target = state.items.find((item) => item.id === id)
      if (target == null) {
        return
      }
      const href = target.ctaHref
      if (href == null || href === "") {
        return
      }

      if (target.readAt == null) {
        const previousItems = state.items
        const previousUnread = state.unreadCount
        applyOptimisticRead((item) => item.id === id)

        void adapters.markOneRead(id).catch(() => {
          dispatch({
            type: "items_replaced",
            items: previousItems,
            unreadCount: previousUnread,
          })
        })
      }

      adapters.navigate(href)
    },
    openSettings: async () => {
      dispatch({ type: "settings_opened" })

      const generation = state.preferencesGeneration + 1
      dispatch({ type: "preferences_load_started", generation })

      try {
        const preferences = normalizeNotificationPreferences(
          await adapters.getPreferences()
        )
        dispatch({
          type: "preferences_load_succeeded",
          generation,
          preferences,
        })
      } catch {
        dispatch({
          type: "preferences_load_failed",
          generation,
          error: PREFERENCES_LOAD_ERROR,
        })
      }
    },
    closeSettings: () => {
      dispatch({ type: "settings_closed" })
    },
    setPreference: (category, enabled) => {
      if (state.preferencesBusy) {
        return
      }
      if (state.preferences[category] === enabled) {
        return
      }

      const previous = state.preferences
      const next = { ...previous, [category]: enabled }
      dispatch({ type: "preferences_set", preferences: next })
      dispatch({ type: "preferences_busy", busy: true })

      void adapters
        .setPreferences(next)
        .then((saved) => {
          dispatch({
            type: "preferences_set",
            preferences: normalizeNotificationPreferences(saved),
          })
        })
        .catch(() => {
          dispatch({ type: "preferences_set", preferences: previous })
          dispatch({ type: "preferences_error", error: PREFERENCES_SAVE_ERROR })
        })
        .finally(() => {
          dispatch({ type: "preferences_busy", busy: false })
        })
    },
    reset: () => {
      void disconnect()
      dispatch({ type: "reset" })
    },
  }
}
