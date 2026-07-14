import { describe, expect, it, vi } from "vitest"

import {
  createInMemoryOperatorNotificationsAdapters,
  createOperatorNotificationsModule,
  filterNotificationsForTab,
  normalizeNotificationPreferences,
  type OperatorNotification,
  type OperatorNotificationsAdapters,
  type OperatorNotificationsRealtimeHandlers,
} from "./createOperatorNotificationsModule"

const NOW = "2026-07-15T12:00:00.000Z"

function makeNotification(
  overrides: Partial<OperatorNotification> & Pick<OperatorNotification, "id">
): OperatorNotification {
  return {
    userId: 1,
    category: "product-updates",
    type: "product-operator-home-live",
    title: "Your Operator Home is live",
    body: "Home is ready.",
    createdAt: NOW,
    readAt: null,
    ...overrides,
  }
}

describe("filterNotificationsForTab", () => {
  const items: OperatorNotification[] = [
    makeNotification({
      id: 1,
      category: "product-updates",
      createdAt: "2026-07-15T10:00:00.000Z",
    }),
    makeNotification({
      id: 2,
      category: "account-notices",
      type: "password-changed",
      title: "Password changed",
      body: "Your password was changed.",
      createdAt: "2026-07-15T11:00:00.000Z",
      readAt: NOW,
    }),
    makeNotification({
      id: 3,
      category: "tips-and-playbooks",
      type: "tip-place-qr-materials",
      title: "Place your QR",
      body: "Where guests pause.",
      createdAt: "2026-07-15T09:00:00.000Z",
    }),
    makeNotification({
      id: 4,
      category: "weekly-brief-reminders",
      type: "weekly-brief-ready",
      title: "Weekly brief ready",
      body: "Summary ready.",
      createdAt: "2026-07-14T12:00:00.000Z",
      readAt: null,
    }),
    makeNotification({
      id: 5,
      category: "campaign-and-report-updates",
      type: "campaign-update",
      title: "Campaign update",
      body: "Campaign progressed.",
      createdAt: "2026-07-13T12:00:00.000Z",
      readAt: NOW,
    }),
  ]

  it("keeps Tips / Weekly / Campaign under All, and only unread under Unread", () => {
    expect(filterNotificationsForTab(items, "all").map((n) => n.id)).toEqual([
      1, 2, 3, 4, 5,
    ])
    expect(filterNotificationsForTab(items, "unread").map((n) => n.id)).toEqual(
      [1, 3, 4]
    )
  })

  it("filters Product and Account by category id", () => {
    expect(
      filterNotificationsForTab(items, "product").map((n) => n.id)
    ).toEqual([1])
    expect(
      filterNotificationsForTab(items, "account").map((n) => n.id)
    ).toEqual([2])
  })
})

describe("createOperatorNotificationsModule", () => {
  it("loads inbox items newest-first with unread count", async () => {
    const adapters = createInMemoryOperatorNotificationsAdapters([
      makeNotification({
        id: 2,
        createdAt: "2026-07-14T12:00:00.000Z",
        title: "Older",
      }),
      makeNotification({
        id: 1,
        createdAt: "2026-07-15T12:00:00.000Z",
        title: "Newer",
      }),
    ])
    const module = createOperatorNotificationsModule(adapters)

    expect(module.getSnapshot().loadStatus).toBe("idle")

    const loadPromise = module.load()
    expect(module.getSnapshot().loadStatus).toBe("loading")

    await loadPromise

    expect(module.getSnapshot()).toMatchObject({
      loadStatus: "loaded",
      unreadCount: 2,
      drawerOpen: false,
      activeTab: "all",
    })
    expect(module.getSnapshot().items.map((n) => n.id)).toEqual([1, 2])
    expect(module.getSnapshot().filteredItems.map((n) => n.id)).toEqual([1, 2])
  })

  it("connect starts realtime then REST catch-up", async () => {
    const adapters = createInMemoryOperatorNotificationsAdapters([
      makeNotification({ id: 1 }),
    ])
    const connectRealtime = vi.spyOn(adapters, "connectRealtime")
    const module = createOperatorNotificationsModule(adapters)

    await module.connect()

    expect(connectRealtime).toHaveBeenCalledTimes(1)
    expect(module.getSnapshot().loadStatus).toBe("loaded")
    expect(module.getSnapshot().unreadCount).toBe(1)
  })

  it("connect ensures seeds then catch-up, and re-toasts unread seeds", async () => {
    const unreadSeed = makeNotification({
      id: 10,
      type: "tip-preview-guest-form",
      category: "tips-and-playbooks",
      title: "Preview your guest form before guests do",
      body: "Open Preview guest form on Home.",
      readAt: null,
    })
    const readSeed = makeNotification({
      id: 11,
      type: "product-operator-home-live",
      title: "Your Operator Home is live",
      body: "Home is ready.",
      readAt: NOW,
    })

    const callOrder: string[] = []
    const adapters: OperatorNotificationsAdapters = {
      ...createInMemoryOperatorNotificationsAdapters([unreadSeed, readSeed]),
      connectRealtime: async () => {
        callOrder.push("realtime")
        return { stop: async () => {} }
      },
      ensureSeeds: async () => {
        callOrder.push("ensureSeeds")
        return { reToast: [unreadSeed] }
      },
      listNotifications: async () => {
        callOrder.push("list")
        return {
          unreadCount: 1,
          items: [unreadSeed, readSeed],
        }
      },
      showToast: vi.fn(),
    }

    const module = createOperatorNotificationsModule(adapters)
    await module.connect()

    expect(callOrder).toEqual(["realtime", "ensureSeeds", "list"])
    expect(adapters.showToast).toHaveBeenCalledTimes(1)
    expect(adapters.showToast).toHaveBeenCalledWith(unreadSeed)
    expect(module.getSnapshot().unreadCount).toBe(1)
    expect(module.getSnapshot().items.map((n) => n.id).sort()).toEqual([10, 11])
  })

  it("reconnect catch-up does not re-ensure or re-toast seeds", async () => {
    let serverItems = [makeNotification({ id: 1 })]
    const realtime = {
      handlers: null as OperatorNotificationsRealtimeHandlers | null,
    }

    const adapters: OperatorNotificationsAdapters = {
      ...createInMemoryOperatorNotificationsAdapters(),
      listNotifications: async () => ({
        unreadCount: serverItems.filter((n) => n.readAt == null).length,
        items: [...serverItems],
      }),
      ensureSeeds: vi.fn(async () => ({ reToast: [] })),
      showToast: vi.fn(),
      connectRealtime: async (next) => {
        realtime.handlers = next
        return { stop: async () => {} }
      },
    }

    const module = createOperatorNotificationsModule(adapters)
    await module.connect()
    expect(adapters.ensureSeeds).toHaveBeenCalledTimes(1)
    expect(adapters.showToast).not.toHaveBeenCalled()

    serverItems = [
      ...serverItems,
      makeNotification({
        id: 2,
        createdAt: "2026-07-15T13:00:00.000Z",
        title: "Arrived offline",
      }),
    ]
    realtime.handlers?.onReconnected()

    await vi.waitFor(() => {
      expect(module.getSnapshot().unreadCount).toBe(2)
    })

    expect(adapters.ensureSeeds).toHaveBeenCalledTimes(1)
    expect(adapters.showToast).not.toHaveBeenCalled()
  })

  it("reconnect runs REST catch-up again", async () => {
    let serverItems = [makeNotification({ id: 1 })]
    const realtime = {
      handlers: null as OperatorNotificationsRealtimeHandlers | null,
    }

    const adapters: OperatorNotificationsAdapters = {
      ...createInMemoryOperatorNotificationsAdapters(),
      listNotifications: async () => ({
        unreadCount: serverItems.filter((n) => n.readAt == null).length,
        items: [...serverItems],
      }),
      connectRealtime: async (next) => {
        realtime.handlers = next
        return { stop: async () => {} }
      },
    }

    const listNotifications = vi.spyOn(adapters, "listNotifications")
    const module = createOperatorNotificationsModule(adapters)
    await module.connect()
    expect(listNotifications).toHaveBeenCalledTimes(1)

    serverItems = [
      ...serverItems,
      makeNotification({
        id: 2,
        createdAt: "2026-07-15T13:00:00.000Z",
        title: "Arrived offline",
      }),
    ]
    realtime.handlers?.onReconnected()

    await vi.waitFor(() => {
      expect(listNotifications).toHaveBeenCalledTimes(2)
      expect(module.getSnapshot().unreadCount).toBe(2)
      expect(module.getSnapshot().items.map((n) => n.id)).toEqual([2, 1])
    })
  })

  it("closed drawer: SignalR arrival updates badge and toasts without marking read", async () => {
    const adapters = createInMemoryOperatorNotificationsAdapters([
      makeNotification({ id: 1, readAt: NOW }),
    ])
    const realtime = {
      handlers: null as OperatorNotificationsRealtimeHandlers | null,
    }
    adapters.connectRealtime = async (next) => {
      realtime.handlers = next
      return { stop: async () => {} }
    }
    const showToast = vi.fn()
    adapters.showToast = showToast
    const markOneRead = vi.spyOn(adapters, "markOneRead")

    const module = createOperatorNotificationsModule(adapters, {
      now: () => NOW,
    })
    await module.connect()

    const incoming = makeNotification({
      id: 99,
      title: "Password changed",
      body: "Your password was changed.",
      createdAt: "2026-07-15T13:00:00.000Z",
    })
    realtime.handlers?.onNotificationCreated(incoming)

    expect(module.getSnapshot().unreadCount).toBe(1)
    expect(module.getSnapshot().items.find((n) => n.id === 99)?.readAt).toBeNull()
    expect(showToast).toHaveBeenCalledWith(incoming)
    expect(markOneRead).not.toHaveBeenCalled()
  })

  it("open drawer: SignalR arrival is auto-marked read with no toast", async () => {
    const adapters = createInMemoryOperatorNotificationsAdapters([
      makeNotification({ id: 1 }),
    ])
    const realtime = {
      handlers: null as OperatorNotificationsRealtimeHandlers | null,
    }
    adapters.connectRealtime = async (next) => {
      realtime.handlers = next
      return { stop: async () => {} }
    }
    const showToast = vi.fn()
    adapters.showToast = showToast
    const markOneRead = vi.spyOn(adapters, "markOneRead")

    const module = createOperatorNotificationsModule(adapters, {
      now: () => NOW,
    })
    await module.connect()
    await module.openDrawer()
    expect(module.getSnapshot().unreadCount).toBe(0)
    showToast.mockClear()
    markOneRead.mockClear()

    const incoming = makeNotification({
      id: 50,
      title: "New tip",
      body: "While drawer open.",
      createdAt: "2026-07-15T13:00:00.000Z",
    })
    realtime.handlers?.onNotificationCreated(incoming)

    await vi.waitFor(() => {
      expect(markOneRead).toHaveBeenCalledWith(50)
    })

    expect(module.getSnapshot().unreadCount).toBe(0)
    expect(module.getSnapshot().items.find((n) => n.id === 50)?.readAt).toBe(
      NOW
    )
    expect(showToast).not.toHaveBeenCalled()
  })

  it("disconnect stops the realtime session", async () => {
    const stop = vi.fn(async () => {})
    const adapters = createInMemoryOperatorNotificationsAdapters([
      makeNotification({ id: 1 }),
    ])
    adapters.connectRealtime = async () => ({ stop })
    const module = createOperatorNotificationsModule(adapters)

    await module.connect()
    await module.disconnect()

    expect(stop).toHaveBeenCalledTimes(1)
  })

  it("openDrawer marks the entire inbox read and clears the badge", async () => {
    const adapters = createInMemoryOperatorNotificationsAdapters([
      makeNotification({ id: 1 }),
      makeNotification({ id: 2, category: "tips-and-playbooks" }),
    ])
    const markInboxRead = vi.spyOn(adapters, "markInboxRead")
    const module = createOperatorNotificationsModule(adapters)
    await module.load()

    expect(module.getSnapshot().unreadCount).toBe(2)

    const openPromise = module.openDrawer()
    expect(module.getSnapshot()).toMatchObject({
      drawerOpen: true,
      unreadCount: 0,
    })
    expect(
      module.getSnapshot().items.every((item) => item.readAt != null)
    ).toBe(true)

    await openPromise
    expect(markInboxRead).toHaveBeenCalledTimes(1)
    expect(module.getSnapshot().unreadCount).toBe(0)
  })

  it("closeDrawer closes without changing read state further", async () => {
    const adapters = createInMemoryOperatorNotificationsAdapters([
      makeNotification({ id: 1 }),
    ])
    const module = createOperatorNotificationsModule(adapters)
    await module.load()
    await module.openDrawer()

    module.closeDrawer()

    expect(module.getSnapshot().drawerOpen).toBe(false)
    expect(module.getSnapshot().unreadCount).toBe(0)
  })

  it("setTab filters Product / Account / Unread including Tips under Unread", async () => {
    const adapters = createInMemoryOperatorNotificationsAdapters([
      makeNotification({
        id: 1,
        category: "product-updates",
        createdAt: "2026-07-15T12:00:00.000Z",
      }),
      makeNotification({
        id: 2,
        category: "account-notices",
        type: "new-sign-in",
        title: "New sign-in",
        body: "Someone signed in.",
        createdAt: "2026-07-15T11:00:00.000Z",
      }),
      makeNotification({
        id: 3,
        category: "tips-and-playbooks",
        type: "tip-preview-guest-form",
        title: "Preview guest form",
        body: "Try Preview on Home.",
        createdAt: "2026-07-15T10:00:00.000Z",
      }),
    ])
    const module = createOperatorNotificationsModule(adapters)
    await module.load()

    module.setTab("product")
    expect(module.getSnapshot().activeTab).toBe("product")
    expect(module.getSnapshot().filteredItems.map((n) => n.id)).toEqual([1])

    module.setTab("account")
    expect(module.getSnapshot().filteredItems.map((n) => n.id)).toEqual([2])

    module.setTab("unread")
    expect(module.getSnapshot().filteredItems.map((n) => n.id)).toEqual([
      1, 2, 3,
    ])
  })

  it("markOneRead marks a single item and decrements unread", async () => {
    const adapters = createInMemoryOperatorNotificationsAdapters([
      makeNotification({ id: 1 }),
      makeNotification({ id: 2 }),
    ])
    const markOneRead = vi.spyOn(adapters, "markOneRead")
    const module = createOperatorNotificationsModule(adapters)
    await module.load()

    module.markOneRead(1)

    await vi.waitFor(() => {
      expect(module.getSnapshot().items.find((n) => n.id === 1)?.readAt).toBeTruthy()
    })

    expect(markOneRead).toHaveBeenCalledWith(1)
    expect(module.getSnapshot().unreadCount).toBe(1)
    expect(module.getSnapshot().items.find((n) => n.id === 2)?.readAt).toBeNull()
  })

  it("markVisibleRead marks only the current tab filter via the adapter", async () => {
    const adapters = createInMemoryOperatorNotificationsAdapters([
      makeNotification({
        id: 1,
        category: "product-updates",
        createdAt: "2026-07-15T12:00:00.000Z",
      }),
      makeNotification({
        id: 2,
        category: "account-notices",
        type: "password-changed",
        title: "Password changed",
        body: "Changed.",
        createdAt: "2026-07-15T11:00:00.000Z",
      }),
      makeNotification({
        id: 3,
        category: "tips-and-playbooks",
        type: "tip-place-qr-materials",
        title: "Place QR",
        body: "Pause points.",
        createdAt: "2026-07-15T10:00:00.000Z",
      }),
    ])
    const markVisibleRead = vi.spyOn(adapters, "markVisibleRead")
    const module = createOperatorNotificationsModule(adapters)
    await module.load()

    module.setTab("product")
    module.markVisibleRead()

    await vi.waitFor(() => {
      expect(module.getSnapshot().items.find((n) => n.id === 1)?.readAt).toBeTruthy()
      expect(module.getSnapshot().markReadBusy).toBe(false)
    })

    expect(markVisibleRead).toHaveBeenCalledWith({
      category: "product-updates",
    })
    expect(module.getSnapshot().items.find((n) => n.id === 2)?.readAt).toBeNull()
    expect(module.getSnapshot().items.find((n) => n.id === 3)?.readAt).toBeNull()
    expect(module.getSnapshot().unreadCount).toBe(2)

    module.setTab("unread")
    module.markVisibleRead()

    await vi.waitFor(() => {
      expect(module.getSnapshot().unreadCount).toBe(0)
      expect(module.getSnapshot().markReadBusy).toBe(false)
    })

    expect(markVisibleRead).toHaveBeenLastCalledWith({ unreadOnly: true })
  })

  it("activateCta marks the item read and navigates via the adapter", async () => {
    const adapters = createInMemoryOperatorNotificationsAdapters([
      makeNotification({
        id: 7,
        ctaLabel: "Open guide",
        ctaHref: "/operator/setup-guide",
      }),
    ])
    const navigate = vi.fn()
    const wired: OperatorNotificationsAdapters = {
      ...adapters,
      navigate,
    }
    const markOneRead = vi.spyOn(wired, "markOneRead")
    const module = createOperatorNotificationsModule(wired)
    await module.load()

    module.activateCta(7)

    await vi.waitFor(() => {
      expect(module.getSnapshot().items[0]?.readAt).toBeTruthy()
    })

    expect(markOneRead).toHaveBeenCalledWith(7)
    expect(navigate).toHaveBeenCalledWith("/operator/setup-guide")
    expect(module.getSnapshot().unreadCount).toBe(0)
  })

  it("activateCta no-ops when the Notification has no CTA href", async () => {
    const adapters = createInMemoryOperatorNotificationsAdapters([
      makeNotification({ id: 1, ctaLabel: null, ctaHref: null }),
    ])
    const navigate = vi.fn()
    const module = createOperatorNotificationsModule({
      ...adapters,
      navigate,
    })
    await module.load()

    module.activateCta(1)

    expect(navigate).not.toHaveBeenCalled()
    expect(module.getSnapshot().unreadCount).toBe(1)
  })

  it("reset returns to the idle empty snapshot", async () => {
    const adapters = createInMemoryOperatorNotificationsAdapters([
      makeNotification({ id: 1 }),
    ])
    const module = createOperatorNotificationsModule(adapters)
    await module.load()
    await module.openDrawer()
    module.setTab("unread")

    module.reset()

    expect(module.getSnapshot()).toMatchObject({
      loadStatus: "idle",
      items: [],
      filteredItems: [],
      unreadCount: 0,
      drawerOpen: false,
      activeTab: "all",
      settingsOpen: false,
      preferences: {
        "product-updates": true,
        "account-notices": true,
        "weekly-brief-reminders": true,
        "tips-and-playbooks": true,
        "campaign-and-report-updates": true,
      },
    })
  })

  it("notifies subscribers when the snapshot changes", async () => {
    const module = createOperatorNotificationsModule(
      createInMemoryOperatorNotificationsAdapters([makeNotification({ id: 1 })])
    )
    const listener = vi.fn()
    module.subscribe(listener)

    await module.load()

    expect(listener).toHaveBeenCalled()
  })

  it("defaults all five Notification preferences on when prefs are missing", async () => {
    const adapters = createInMemoryOperatorNotificationsAdapters([
      makeNotification({ id: 1 }),
    ])
    adapters.getPreferences = async () =>
      ({ "account-notices": false }) as Awaited<
        ReturnType<OperatorNotificationsAdapters["getPreferences"]>
      >
    const module = createOperatorNotificationsModule(adapters)

    expect(module.getSnapshot().preferences).toEqual({
      "product-updates": true,
      "account-notices": true,
      "weekly-brief-reminders": true,
      "tips-and-playbooks": true,
      "campaign-and-report-updates": true,
    })
    expect(module.getSnapshot().settingsOpen).toBe(false)

    await module.openSettings()

    expect(module.getSnapshot().settingsOpen).toBe(true)
    expect(module.getSnapshot().preferencesStatus).toBe("loaded")
    expect(module.getSnapshot().preferences).toEqual({
      "product-updates": true,
      "account-notices": false,
      "weekly-brief-reminders": true,
      "tips-and-playbooks": true,
      "campaign-and-report-updates": true,
    })
  })

  it("normalizeNotificationPreferences fills missing category keys as on", () => {
    expect(normalizeNotificationPreferences(null)).toEqual({
      "product-updates": true,
      "account-notices": true,
      "weekly-brief-reminders": true,
      "tips-and-playbooks": true,
      "campaign-and-report-updates": true,
    })
    expect(
      normalizeNotificationPreferences({ "tips-and-playbooks": false })
    ).toEqual({
      "product-updates": true,
      "account-notices": true,
      "weekly-brief-reminders": true,
      "tips-and-playbooks": false,
      "campaign-and-report-updates": true,
    })
  })

  it("openSettings loads preferences via REST and closeSettings returns to the inbox", async () => {
    const adapters = createInMemoryOperatorNotificationsAdapters([
      makeNotification({ id: 1 }),
    ])
    adapters.getPreferences = async () => ({
      "product-updates": true,
      "account-notices": false,
      "weekly-brief-reminders": true,
      "tips-and-playbooks": true,
      "campaign-and-report-updates": false,
    })
    const getPreferences = vi.spyOn(adapters, "getPreferences")
    const module = createOperatorNotificationsModule(adapters)
    await module.load()
    await module.openDrawer()

    await module.openSettings()

    expect(getPreferences).toHaveBeenCalledTimes(1)
    expect(module.getSnapshot()).toMatchObject({
      settingsOpen: true,
      drawerOpen: true,
      preferences: {
        "product-updates": true,
        "account-notices": false,
        "weekly-brief-reminders": true,
        "tips-and-playbooks": true,
        "campaign-and-report-updates": false,
      },
    })

    module.closeSettings()

    expect(module.getSnapshot().settingsOpen).toBe(false)
    expect(module.getSnapshot().drawerOpen).toBe(true)
  })

  it("setPreference persists via prefs REST and updates the snapshot", async () => {
    const adapters = createInMemoryOperatorNotificationsAdapters([
      makeNotification({ id: 1 }),
    ])
    const prefs = {
      current: {
        "product-updates": true,
        "account-notices": true,
        "weekly-brief-reminders": true,
        "tips-and-playbooks": true,
        "campaign-and-report-updates": true,
      },
    }
    adapters.getPreferences = async () => ({ ...prefs.current })
    adapters.setPreferences = async (next) => {
      prefs.current = { ...next }
      return { ...prefs.current }
    }
    const setPreferences = vi.spyOn(adapters, "setPreferences")
    const module = createOperatorNotificationsModule(adapters)
    await module.openSettings()

    module.setPreference("tips-and-playbooks", false)

    await vi.waitFor(() => {
      expect(module.getSnapshot().preferencesBusy).toBe(false)
    })

    expect(setPreferences).toHaveBeenCalledWith({
      "product-updates": true,
      "account-notices": true,
      "weekly-brief-reminders": true,
      "tips-and-playbooks": false,
      "campaign-and-report-updates": true,
    })
    expect(module.getSnapshot().preferences["tips-and-playbooks"]).toBe(false)
    expect(module.getSnapshot().preferences["product-updates"]).toBe(true)
  })

  it("setPreference rolls back the snapshot when prefs REST fails", async () => {
    const adapters = createInMemoryOperatorNotificationsAdapters([
      makeNotification({ id: 1 }),
    ])
    adapters.getPreferences = async () => ({
      "product-updates": true,
      "account-notices": true,
      "weekly-brief-reminders": true,
      "tips-and-playbooks": true,
      "campaign-and-report-updates": true,
    })
    adapters.setPreferences = async () => {
      throw new Error("network")
    }
    const module = createOperatorNotificationsModule(adapters)
    await module.openSettings()

    module.setPreference("account-notices", false)

    await vi.waitFor(() => {
      expect(module.getSnapshot().preferencesBusy).toBe(false)
    })

    expect(module.getSnapshot().preferences["account-notices"]).toBe(true)
  })

  it("closing the drawer also closes settings", async () => {
    const adapters = createInMemoryOperatorNotificationsAdapters([
      makeNotification({ id: 1 }),
    ])
    adapters.getPreferences = async () => ({
      "product-updates": true,
      "account-notices": true,
      "weekly-brief-reminders": true,
      "tips-and-playbooks": true,
      "campaign-and-report-updates": true,
    })
    const module = createOperatorNotificationsModule(adapters)
    await module.openDrawer()
    await module.openSettings()

    module.closeDrawer()

    expect(module.getSnapshot()).toMatchObject({
      drawerOpen: false,
      settingsOpen: false,
    })
  })
})
