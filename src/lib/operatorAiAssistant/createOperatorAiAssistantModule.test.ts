import { describe, expect, it } from "vitest"

import {
  createInMemoryOperatorNotificationsAdapters,
  createOperatorNotificationsModule,
} from "@/lib/operatorNotifications/createOperatorNotificationsModule"

import {
  buildEmptyComposerPlaceholders,
  createInMemoryOperatorAiAssistantAdapters,
  createOperatorAiAssistantModule,
  EMPTY_SUGGESTION_CHIPS,
  periodPhraseForReportingPeriod,
} from "./createOperatorAiAssistantModule"

describe("createOperatorAiAssistantModule", () => {
  it("openDrawer shows the empty greeting at collapsed 620 width with no server row", () => {
    const adapters = createInMemoryOperatorAiAssistantAdapters()
    const module = createOperatorAiAssistantModule(adapters)

    expect(module.getSnapshot()).toMatchObject({
      drawerOpen: false,
      widthMode: "collapsed",
      view: "empty",
      conversationId: null,
    })

    module.openDrawer({ operatorFirstName: "Mohamed" })

    expect(module.getSnapshot()).toMatchObject({
      drawerOpen: true,
      widthMode: "collapsed",
      view: "empty",
      conversationId: null,
      greeting: {
        hello: "Hello, Mohamed",
        headline: "What would you like help with?",
        body: "Ask about feedback, guests, offers, campaigns or performance for this restaurant.",
      },
    })
    expect(adapters.conversations).toEqual([])
  })

  it("Close and Escape close the Assistant without creating a server row", () => {
    const adapters = createInMemoryOperatorAiAssistantAdapters()
    const module = createOperatorAiAssistantModule(adapters)

    module.openDrawer({ operatorFirstName: "Mohamed" })
    module.closeDrawer()
    expect(module.getSnapshot().drawerOpen).toBe(false)
    expect(module.getSnapshot().conversationId).toBe(null)
    expect(adapters.conversations).toEqual([])

    module.openDrawer({ operatorFirstName: "Mohamed" })
    module.setOpen(false)
    expect(module.getSnapshot().drawerOpen).toBe(false)
    expect(adapters.conversations).toEqual([])
  })

  it("New chat returns the empty greeting and does not create a server row", () => {
    const adapters = createInMemoryOperatorAiAssistantAdapters()
    const module = createOperatorAiAssistantModule(adapters)

    module.openDrawer({ operatorFirstName: "Alex" })
    module.openRecent()
    expect(module.getSnapshot().view).toBe("recent")
    expect(adapters.conversations).toEqual([])

    module.startNewChat()

    expect(module.getSnapshot()).toMatchObject({
      drawerOpen: true,
      view: "empty",
      conversationId: null,
      greeting: {
        hello: "Hello, Alex",
      },
    })
    expect(adapters.conversations).toEqual([])
  })

  it("opening the Assistant closes Notifications so only one right Drawer is open", async () => {
    const notifications = createOperatorNotificationsModule(
      createInMemoryOperatorNotificationsAdapters()
    )
    const assistant = createOperatorAiAssistantModule(
      createInMemoryOperatorAiAssistantAdapters({
        closePeerRightDrawers: () => notifications.closeDrawer(),
      })
    )

    await notifications.openDrawer()
    expect(notifications.getSnapshot().drawerOpen).toBe(true)

    assistant.openDrawer({ operatorFirstName: "Mohamed" })

    expect(notifications.getSnapshot().drawerOpen).toBe(false)
    expect(assistant.getSnapshot().drawerOpen).toBe(true)
  })

  it("opening Notifications closes the Assistant so the two Drawers never stack", async () => {
    const notifications = createOperatorNotificationsModule(
      createInMemoryOperatorNotificationsAdapters()
    )
    const assistant = createOperatorAiAssistantModule(
      createInMemoryOperatorAiAssistantAdapters({
        closePeerRightDrawers: () => notifications.closeDrawer(),
      })
    )

    assistant.openDrawer({ operatorFirstName: "Mohamed" })
    expect(assistant.getSnapshot().drawerOpen).toBe(true)

    assistant.closeDrawer()
    await notifications.openDrawer()

    expect(assistant.getSnapshot().drawerOpen).toBe(false)
    expect(notifications.getSnapshot().drawerOpen).toBe(true)
  })

  it("a new empty conversation copies dashboard Owned location and Last 7 days", () => {
    const dashboard = {
      ownedLocation: { id: 11, name: "Camden" },
      restaurantName: "Mehmet's Grill",
    }
    const adapters = createInMemoryOperatorAiAssistantAdapters({
      getDashboardOwnedLocation: () => dashboard.ownedLocation,
      getRestaurantName: () => dashboard.restaurantName,
    })
    const module = createOperatorAiAssistantModule(adapters)

    module.openDrawer({ operatorFirstName: "Mohamed" })

    expect(module.getSnapshot().analysisScope).toEqual({
      ownedLocationId: 11,
      ownedLocationName: "Camden",
      reportingPeriod: { kind: "preset", presetId: "last7" },
    })
    expect(module.getSnapshot().restaurantName).toBe("Mehmet's Grill")
    expect(module.getSnapshot().headerStatusLine).toBe(
      "Mehmet's Grill · Camden · Last 7 days"
    )
    expect(adapters.conversations).toEqual([])
  })

  it("after that open, changing the dashboard location does not rewrite Analysis scope", () => {
    const dashboard = {
      ownedLocation: { id: 11, name: "Camden" },
      restaurantName: "Mehmet's Grill",
    }
    const adapters = createInMemoryOperatorAiAssistantAdapters({
      getDashboardOwnedLocation: () => dashboard.ownedLocation,
      getRestaurantName: () => dashboard.restaurantName,
    })
    const module = createOperatorAiAssistantModule(adapters)

    module.openDrawer({ operatorFirstName: "Mohamed" })
    dashboard.ownedLocation = { id: 22, name: "Shoreditch" }

    expect(module.getSnapshot().analysisScope).toMatchObject({
      ownedLocationId: 11,
      ownedLocationName: "Camden",
      reportingPeriod: { kind: "preset", presetId: "last7" },
    })
    expect(module.getSnapshot().headerStatusLine).toBe(
      "Mehmet's Grill · Camden · Last 7 days"
    )
  })

  it("New chat copies the current dashboard location and Last 7 days", () => {
    const dashboard = {
      ownedLocation: { id: 11, name: "Camden" },
      restaurantName: "Mehmet's Grill",
    }
    const adapters = createInMemoryOperatorAiAssistantAdapters({
      getDashboardOwnedLocation: () => dashboard.ownedLocation,
      getRestaurantName: () => dashboard.restaurantName,
    })
    const module = createOperatorAiAssistantModule(adapters)

    module.openDrawer({ operatorFirstName: "Mohamed" })
    dashboard.ownedLocation = { id: 22, name: "Shoreditch" }
    module.startNewChat()

    expect(module.getSnapshot().analysisScope).toMatchObject({
      ownedLocationId: 22,
      ownedLocationName: "Shoreditch",
      reportingPeriod: { kind: "preset", presetId: "last7" },
    })
    expect(module.getSnapshot().headerStatusLine).toBe(
      "Mehmet's Grill · Shoreditch · Last 7 days"
    )
    expect(adapters.conversations).toEqual([])
  })

  it("Change Scope in mode multi shows Owned location and Reporting period", () => {
    const adapters = createInMemoryOperatorAiAssistantAdapters({
      getDashboardMode: () => "multi",
      getDashboardOwnedLocation: () => ({ id: 11, name: "Camden" }),
      listOwnedLocations: () => [
        { id: 11, name: "Camden" },
        { id: 22, name: "Shoreditch" },
      ],
    })
    const module = createOperatorAiAssistantModule(adapters)

    module.openDrawer({ operatorFirstName: "Mohamed" })
    module.openChangeScope()

    expect(module.getSnapshot().changeScopeDialog).toEqual({
      open: true,
      title: "Change analysis scope",
      showsOwnedLocationField: true,
      draftOwnedLocationId: 11,
      draftReportingPeriod: { kind: "preset", presetId: "last7" },
      locationOptions: [
        { id: 11, name: "Camden" },
        { id: 22, name: "Shoreditch" },
      ],
    })
  })

  it("Change Scope in mode single shows Reporting period only", () => {
    const adapters = createInMemoryOperatorAiAssistantAdapters({
      getDashboardMode: () => "single",
      getDashboardOwnedLocation: () => ({ id: 11, name: "Camden" }),
      listOwnedLocations: () => [{ id: 11, name: "Camden" }],
    })
    const module = createOperatorAiAssistantModule(adapters)

    module.openDrawer({ operatorFirstName: "Mohamed" })
    module.openChangeScope()

    expect(module.getSnapshot().changeScopeDialog).toMatchObject({
      open: true,
      title: "Change analysis scope",
      showsOwnedLocationField: false,
      draftOwnedLocationId: 11,
      draftReportingPeriod: { kind: "preset", presetId: "last7" },
    })
  })

  it("Cancel leaves saved Analysis scope unchanged", () => {
    const adapters = createInMemoryOperatorAiAssistantAdapters({
      getDashboardMode: () => "multi",
      getDashboardOwnedLocation: () => ({ id: 11, name: "Camden" }),
      listOwnedLocations: () => [
        { id: 11, name: "Camden" },
        { id: 22, name: "Shoreditch" },
      ],
    })
    const module = createOperatorAiAssistantModule(adapters)

    module.openDrawer({ operatorFirstName: "Mohamed" })
    module.openChangeScope()
    module.setChangeScopeDraftLocation(22)
    module.setChangeScopeDraftReportingPeriod({
      kind: "preset",
      presetId: "last30",
    })
    module.cancelChangeScope()

    expect(module.getSnapshot().changeScopeDialog.open).toBe(false)
    expect(module.getSnapshot().analysisScope).toMatchObject({
      ownedLocationId: 11,
      ownedLocationName: "Camden",
      reportingPeriod: { kind: "preset", presetId: "last7" },
    })
    expect(module.getSnapshot().headerStatusLine).toBe(
      "Mehmet's Grill · Camden · Last 7 days"
    )
  })

  it("Apply on an empty conversation updates the header and snapshot with no server row", () => {
    const adapters = createInMemoryOperatorAiAssistantAdapters({
      getDashboardMode: () => "multi",
      getDashboardOwnedLocation: () => ({ id: 11, name: "Camden" }),
      listOwnedLocations: () => [
        { id: 11, name: "Camden" },
        { id: 22, name: "Shoreditch" },
      ],
    })
    const module = createOperatorAiAssistantModule(adapters)

    module.openDrawer({ operatorFirstName: "Mohamed" })
    module.openChangeScope()
    module.setChangeScopeDraftLocation(22)
    module.setChangeScopeDraftReportingPeriod({
      kind: "preset",
      presetId: "thisMonth",
    })
    module.applyChangeScope()

    expect(module.getSnapshot().changeScopeDialog.open).toBe(false)
    expect(module.getSnapshot().conversationId).toBe(null)
    expect(module.getSnapshot().analysisScope).toEqual({
      ownedLocationId: 22,
      ownedLocationName: "Shoreditch",
      reportingPeriod: { kind: "preset", presetId: "thisMonth" },
    })
    expect(module.getSnapshot().restaurantName).toBe("Mehmet's Grill")
    expect(module.getSnapshot().headerStatusLine).toBe(
      "Mehmet's Grill · Shoreditch · This month"
    )
    expect(adapters.conversations).toEqual([])
  })

  it("Expand and collapse change width only while the Drawer stays open", () => {
    const module = createOperatorAiAssistantModule(
      createInMemoryOperatorAiAssistantAdapters()
    )

    module.expandDrawer()
    expect(module.getSnapshot()).toMatchObject({
      drawerOpen: false,
      widthMode: "collapsed",
    })

    module.openDrawer({ operatorFirstName: "Mohamed" })
    module.expandDrawer()
    expect(module.getSnapshot()).toMatchObject({
      drawerOpen: true,
      widthMode: "expanded",
      view: "empty",
    })

    module.startNewChat()
    module.openRecent()
    module.openChangeScope()
    expect(module.getSnapshot()).toMatchObject({
      drawerOpen: true,
      widthMode: "expanded",
      view: "recent",
    })
    expect(module.getSnapshot().changeScopeDialog.open).toBe(true)

    module.leaveExpand()
    expect(module.getSnapshot()).toMatchObject({
      drawerOpen: true,
      widthMode: "collapsed",
      view: "recent",
    })
    expect(module.getSnapshot().changeScopeDialog.open).toBe(true)
  })

  it("a route destination collapses Expand and keeps the Assistant open", () => {
    const module = createOperatorAiAssistantModule(
      createInMemoryOperatorAiAssistantAdapters()
    )

    module.openDrawer({ operatorFirstName: "Mohamed" })
    module.expandDrawer()
    module.leaveExpand()

    expect(module.getSnapshot()).toMatchObject({
      drawerOpen: true,
      widthMode: "collapsed",
    })
  })

  it("crossing below lg leaves Expand, stays open, and does not restore Expand", () => {
    const module = createOperatorAiAssistantModule(
      createInMemoryOperatorAiAssistantAdapters()
    )

    module.openDrawer({ operatorFirstName: "Mohamed" })
    module.expandDrawer()
    module.leaveExpand()

    expect(module.getSnapshot()).toMatchObject({
      drawerOpen: true,
      widthMode: "collapsed",
    })

    module.leaveExpand()
    expect(module.getSnapshot().widthMode).toBe("collapsed")
    expect(module.getSnapshot().drawerOpen).toBe(true)
  })

  it("the next open after Close is collapsed 620 and Expand is not stored", () => {
    const module = createOperatorAiAssistantModule(
      createInMemoryOperatorAiAssistantAdapters()
    )

    module.openDrawer({ operatorFirstName: "Mohamed" })
    module.expandDrawer()
    expect(module.getSnapshot().widthMode).toBe("expanded")

    module.closeDrawer()
    expect(module.getSnapshot()).toMatchObject({
      drawerOpen: false,
      widthMode: "collapsed",
    })

    module.openDrawer({ operatorFirstName: "Mohamed" })
    expect(module.getSnapshot()).toMatchObject({
      drawerOpen: true,
      widthMode: "collapsed",
    })
  })
})

describe("empty-state chips and composer placeholders", () => {
  it("interpolates periodPhrase and Owned location name", () => {
    expect(
      periodPhraseForReportingPeriod({ kind: "preset", presetId: "last7" })
    ).toBe("the last 7 days")
    expect(
      periodPhraseForReportingPeriod({ kind: "preset", presetId: "last30" })
    ).toBe("the last 30 days")
    expect(
      periodPhraseForReportingPeriod({ kind: "preset", presetId: "thisMonth" })
    ).toBe("this month")
    expect(
      periodPhraseForReportingPeriod({
        kind: "custom",
        startDate: "2026-03-02",
        endDate: "2026-03-02",
      })
    ).toBe("2 Mar 2026")
    expect(
      periodPhraseForReportingPeriod({
        kind: "custom",
        startDate: "2026-03-02",
        endDate: "2026-03-09",
      })
    ).toBe("2–9 Mar 2026")

    const placeholders = buildEmptyComposerPlaceholders({
      ownedLocationId: 22,
      ownedLocationName: "Shoreditch",
      reportingPeriod: { kind: "preset", presetId: "last7" },
    })
    expect(placeholders).toEqual([
      "Summarise feedback from the last 7 days\u2026",
      "What needs attention at Shoreditch?",
      "Draft a quiet-day offer for lunch guests\u2026",
      "Show guests who gave poor feedback but opted in\u2026",
      "Suggest next week\u2019s campaign\u2026",
    ])
    expect(placeholders.join(" ")).not.toContain("Custom")
    expect(placeholders.join(" ")).not.toContain("this week")
    expect(placeholders.join(" ")).not.toContain("Camden")
  })

  it("uses Family A Custom labels in placeholder 1 and never the word Custom", () => {
    const singleDay = buildEmptyComposerPlaceholders({
      ownedLocationId: 11,
      ownedLocationName: "Camden",
      reportingPeriod: {
        kind: "custom",
        startDate: "2026-01-05",
        endDate: "2026-01-05",
      },
    })
    expect(singleDay[0]).toBe("Summarise feedback from 5 Jan 2026\u2026")
    expect(singleDay.join(" ")).not.toMatch(/Custom/i)

    const span = buildEmptyComposerPlaceholders({
      ownedLocationId: 11,
      ownedLocationName: "Camden",
      reportingPeriod: {
        kind: "custom",
        startDate: "2026-01-05",
        endDate: "2026-02-10",
      },
    })
    expect(span[0]).toBe("Summarise feedback from 5–10 Feb 2026\u2026")
    expect(span.join(" ")).not.toMatch(/Custom/i)
  })

  it("fills then replaces composer draft from a chip without sending", () => {
    const adapters = createInMemoryOperatorAiAssistantAdapters()
    const module = createOperatorAiAssistantModule(adapters)
    module.openDrawer({ operatorFirstName: "Mohamed" })

    module.fillComposerFromChip("Summarise recent feedback")
    expect(module.getSnapshot().composerDraft).toBe("Summarise recent feedback")
    expect(module.getSnapshot().suggestionChips).toEqual([
      ...EMPTY_SUGGESTION_CHIPS,
    ])

    module.fillComposerFromChip("Draft an offer")
    expect(module.getSnapshot().composerDraft).toBe("Draft an offer")
    expect(module.getSnapshot().suggestionChips).toHaveLength(6)
    expect(adapters.conversations).toEqual([])
  })

  it("shows chips only on an empty conversation", () => {
    const adapters = createInMemoryOperatorAiAssistantAdapters()
    const module = createOperatorAiAssistantModule(adapters)
    module.openDrawer({ operatorFirstName: "Mohamed" })

    expect(module.getSnapshot().suggestionChips).toEqual([
      ...EMPTY_SUGGESTION_CHIPS,
    ])

    module.fillComposerFromChip("Explain performance")
    expect(module.getSnapshot().suggestionChips).toHaveLength(6)

    module.openRecent()
    expect(module.getSnapshot().suggestionChips).toEqual([])
    expect(module.getSnapshot().composerPlaceholders).toEqual([])

    module.startNewChat()
    expect(module.getSnapshot().suggestionChips).toEqual([
      ...EMPTY_SUGGESTION_CHIPS,
    ])
    expect(module.getSnapshot().composerDraft).toBe("")
  })

  it("Apply with empty composer restarts the cycle; Apply with draft leaves both alone", () => {
    const adapters = createInMemoryOperatorAiAssistantAdapters({
      getDashboardMode: () => "multi",
      getDashboardOwnedLocation: () => ({ id: 11, name: "Camden" }),
      listOwnedLocations: () => [
        { id: 11, name: "Camden" },
        { id: 22, name: "Shoreditch" },
      ],
    })
    const module = createOperatorAiAssistantModule(adapters)
    module.openDrawer({ operatorFirstName: "Mohamed" })

    const generationAfterOpen =
      module.getSnapshot().placeholderCycleGeneration
    expect(module.getSnapshot().composerPlaceholders[0]).toBe(
      "Summarise feedback from the last 7 days\u2026"
    )
    expect(module.getSnapshot().composerPlaceholders[1]).toBe(
      "What needs attention at Camden?"
    )

    module.openChangeScope()
    module.setChangeScopeDraftLocation(22)
    module.setChangeScopeDraftReportingPeriod({
      kind: "preset",
      presetId: "thisMonth",
    })
    module.applyChangeScope()

    expect(module.getSnapshot().placeholderCycleGeneration).toBe(
      generationAfterOpen + 1
    )
    expect(module.getSnapshot().composerDraft).toBe("")
    expect(module.getSnapshot().composerPlaceholders[0]).toBe(
      "Summarise feedback from this month\u2026"
    )
    expect(module.getSnapshot().composerPlaceholders[1]).toBe(
      "What needs attention at Shoreditch?"
    )

    module.setComposerDraft("Keep this draft")
    const generationWithDraft =
      module.getSnapshot().placeholderCycleGeneration
    module.openChangeScope()
    module.setChangeScopeDraftReportingPeriod({
      kind: "preset",
      presetId: "last30",
    })
    module.applyChangeScope()

    expect(module.getSnapshot().composerDraft).toBe("Keep this draft")
    expect(module.getSnapshot().placeholderCycleGeneration).toBe(
      generationWithDraft
    )
  })

  it("uses the same chip and placeholder copy in mode single and multi", () => {
    const locations = [{ id: 11, name: "Camden" }]
    const single = createOperatorAiAssistantModule(
      createInMemoryOperatorAiAssistantAdapters({
        getDashboardMode: () => "single",
        getDashboardOwnedLocation: () => locations[0],
        listOwnedLocations: () => locations,
      })
    )
    const multi = createOperatorAiAssistantModule(
      createInMemoryOperatorAiAssistantAdapters({
        getDashboardMode: () => "multi",
        getDashboardOwnedLocation: () => locations[0],
        listOwnedLocations: () => locations,
      })
    )

    single.openDrawer({ operatorFirstName: "Mohamed" })
    multi.openDrawer({ operatorFirstName: "Mohamed" })

    expect(single.getSnapshot().suggestionChips).toEqual([
      ...EMPTY_SUGGESTION_CHIPS,
    ])
    expect(multi.getSnapshot().suggestionChips).toEqual(
      single.getSnapshot().suggestionChips
    )
    expect(single.getSnapshot().suggestionChips).not.toEqual(
      expect.arrayContaining([expect.stringMatching(/compare/i)])
    )
    expect(single.getSnapshot().composerPlaceholders).toEqual(
      multi.getSnapshot().composerPlaceholders
    )
  })
})

describe("first send creates a durable Assistant conversation", () => {
  it("first send creates an Assistant conversation and replaces wait with a stub live answer", async () => {
    const adapters = createInMemoryOperatorAiAssistantAdapters()
    const module = createOperatorAiAssistantModule(adapters)

    module.openDrawer({ operatorFirstName: "Mohamed" })
    expect(module.getSnapshot().conversationId).toBe(null)
    expect(adapters.conversations).toEqual([])

    module.setComposerDraft("Summarise recent feedback")
    module.send()

    expect(module.getSnapshot().turnInFlight).toBe(true)
    expect(module.getSnapshot().sendLocked).toBe(true)
    expect(module.getSnapshot().chipsLocked).toBe(true)
    expect(module.getSnapshot().messages.some((message) => message.role === "wait")).toBe(
      true
    )

    await Promise.resolve()
    await Promise.resolve()

    const snapshot = module.getSnapshot()
    expect(snapshot.conversationId).toBe("conv-1")
    expect(snapshot.turnInFlight).toBe(false)
    expect(snapshot.sendLocked).toBe(false)
    expect(snapshot.showSuggestionChips).toBe(false)
    expect(snapshot.suggestionChips).toEqual([])
    expect(snapshot.composerPlaceholder).toBe("Ask AI Assistant...")
    expect(snapshot.messages.some((message) => message.role === "wait")).toBe(false)
    expect(snapshot.messages).toEqual(
      expect.arrayContaining([
        expect.objectContaining({
          role: "user",
          body: "Summarise recent feedback",
        }),
        expect.objectContaining({
          role: "assistant",
          class: "grounded",
          title: "A stub summary for Camden",
        }),
      ])
    )
    expect(adapters.conversations).toHaveLength(1)
    expect(adapters.conversations[0]?.title).toBe("Summarise recent feedback")
  })

  it("a chip fill then send creates the Assistant conversation", async () => {
    const adapters = createInMemoryOperatorAiAssistantAdapters()
    const module = createOperatorAiAssistantModule(adapters)

    module.openDrawer({ operatorFirstName: "Mohamed" })
    module.fillComposerFromChip("Summarise recent feedback")
    module.send()
    await Promise.resolve()
    await Promise.resolve()

    expect(module.getSnapshot().conversationId).toBe("conv-1")
    expect(adapters.conversations[0]?.title).toBe("Summarise recent feedback")
  })

  it("opening the empty greeting still has no row", () => {
    const adapters = createInMemoryOperatorAiAssistantAdapters()
    const module = createOperatorAiAssistantModule(adapters)

    module.openDrawer({ operatorFirstName: "Mohamed" })

    expect(module.getSnapshot().conversationId).toBe(null)
    expect(module.getSnapshot().messages).toEqual([])
    expect(adapters.conversations).toEqual([])
  })

  it("reopen in this session resumes the last thread; a new module is an empty greeting", async () => {
    const adapters = createInMemoryOperatorAiAssistantAdapters()
    const module = createOperatorAiAssistantModule(adapters)

    module.openDrawer({ operatorFirstName: "Mohamed" })
    module.setComposerDraft("Summarise recent feedback")
    module.send()
    await Promise.resolve()
    await Promise.resolve()
    module.closeDrawer()

    module.openDrawer({ operatorFirstName: "Mohamed" })
    await Promise.resolve()
    await Promise.resolve()

    expect(module.getSnapshot().drawerOpen).toBe(true)
    expect(module.getSnapshot().conversationId).toBe("conv-1")
    expect(module.getSnapshot().messages.some((message) => message.role === "user")).toBe(
      true
    )

    const refreshed = createOperatorAiAssistantModule(adapters)
    refreshed.openDrawer({ operatorFirstName: "Mohamed" })

    expect(refreshed.getSnapshot().conversationId).toBe(null)
    expect(refreshed.getSnapshot().messages).toEqual([])
    expect(adapters.conversations).toHaveLength(1)
  })

  it("composer Send is locked during a turn and a second Send is ignored", async () => {
    let release!: (row: ReturnType<typeof createInMemoryOperatorAiAssistantAdapters>["conversations"][number]) => void
    const hung = new Promise<
      ReturnType<typeof createInMemoryOperatorAiAssistantAdapters>["conversations"][number]
    >((resolve) => {
      release = resolve
    })
    const adapters = createInMemoryOperatorAiAssistantAdapters({
      sendTurn: () => hung,
    })
    const module = createOperatorAiAssistantModule(adapters)

    module.openDrawer({ operatorFirstName: "Mohamed" })
    module.setComposerDraft("Summarise recent feedback")
    module.send()
    module.setComposerDraft("Second")
    module.send()

    expect(module.getSnapshot().sendLocked).toBe(true)
    expect(module.getSnapshot().composerDraft).toBe("Second")
    expect(module.getSnapshot().messages.filter((message) => message.role === "user")).toHaveLength(
      1
    )

    release({
      id: "conv-1",
      title: "Summarise recent feedback",
      analysisScope: module.getSnapshot().analysisScope!,
      lastActivityAt: new Date().toISOString(),
      messages: [
        {
          id: "u1",
          role: "user",
          body: "Summarise recent feedback",
          analysisScope: module.getSnapshot().analysisScope!,
        },
        {
          id: "a1",
          role: "assistant",
          class: "grounded",
          title: "A stub summary for Camden",
          body: "Stub",
        },
      ],
    })
    await Promise.resolve()
    await Promise.resolve()

    expect(module.getSnapshot().sendLocked).toBe(false)
    expect(
      module.getSnapshot().messages.filter((message) => message.role === "user")
    ).toHaveLength(1)
  })

  it("Apply on Change Scope continues the same thread and does not change last activity", async () => {
    const adapters = createInMemoryOperatorAiAssistantAdapters({
      getDashboardMode: () => "multi",
      getDashboardOwnedLocation: () => ({ id: 11, name: "Camden" }),
      listOwnedLocations: () => [
        { id: 11, name: "Camden" },
        { id: 22, name: "Shoreditch" },
      ],
    })
    const module = createOperatorAiAssistantModule(adapters)

    module.openDrawer({ operatorFirstName: "Mohamed" })
    module.setComposerDraft("Summarise recent feedback")
    module.send()
    await Promise.resolve()
    await Promise.resolve()

    const conversationId = module.getSnapshot().conversationId
    const lastActivity = adapters.conversations[0]?.lastActivityAt

    module.openChangeScope()
    module.setChangeScopeDraftLocation(22)
    module.setChangeScopeDraftReportingPeriod({
      kind: "preset",
      presetId: "thisMonth",
    })
    module.applyChangeScope()
    await Promise.resolve()
    await Promise.resolve()

    expect(module.getSnapshot().conversationId).toBe(conversationId)
    expect(module.getSnapshot().analysisScope).toMatchObject({
      ownedLocationId: 22,
      ownedLocationName: "Shoreditch",
      reportingPeriod: { kind: "preset", presetId: "thisMonth" },
    })
    expect(adapters.conversations[0]?.lastActivityAt).toBe(lastActivity)
  })

  it("New chat discards unsent composer text and keeps the previous saved thread", async () => {
    const adapters = createInMemoryOperatorAiAssistantAdapters()
    const module = createOperatorAiAssistantModule(adapters)

    module.openDrawer({ operatorFirstName: "Mohamed" })
    module.setComposerDraft("Summarise recent feedback")
    module.send()
    await Promise.resolve()
    await Promise.resolve()

    module.setComposerDraft("draft that must go")
    module.startNewChat()

    expect(module.getSnapshot().composerDraft).toBe("")
    expect(module.getSnapshot().conversationId).toBe(null)
    expect(module.getSnapshot().messages).toEqual([])
    expect(module.getSnapshot().showSuggestionChips).toBe(true)
    expect(adapters.conversations).toHaveLength(1)
  })

  it("offline at Send creates no user message and no turn", () => {
    const adapters = createInMemoryOperatorAiAssistantAdapters()
    adapters.online = false
    const module = createOperatorAiAssistantModule(adapters)

    module.openDrawer({ operatorFirstName: "Mohamed" })
    module.setComposerDraft("Summarise recent feedback")
    module.send()

    expect(module.getSnapshot().messages).toEqual([])
    expect(module.getSnapshot().conversationId).toBe(null)
    expect(adapters.conversations).toEqual([])
  })

  it("Apply is ignored while a turn runs", async () => {
    const hung = new Promise<never>(() => {})
    const adapters = createInMemoryOperatorAiAssistantAdapters({
      getDashboardMode: () => "multi",
      getDashboardOwnedLocation: () => ({ id: 11, name: "Camden" }),
      listOwnedLocations: () => [
        { id: 11, name: "Camden" },
        { id: 22, name: "Shoreditch" },
      ],
      sendTurn: () => hung,
    })
    const module = createOperatorAiAssistantModule(adapters)

    module.openDrawer({ operatorFirstName: "Mohamed" })
    module.setComposerDraft("Summarise recent feedback")
    module.send()
    module.openChangeScope()
    module.setChangeScopeDraftLocation(22)
    module.applyChangeScope()

    expect(module.getSnapshot().changeScopeDialog.open).toBe(true)
    expect(module.getSnapshot().analysisScope?.ownedLocationId).toBe(11)
  })
})
