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
          title: "No feedback at Camden for the last 7 days",
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
      isArchived: false,
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
          title: "No feedback at Camden for the last 7 days",
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

  it("close drawer after first send does not delete stored conversations", async () => {
    const adapters = createInMemoryOperatorAiAssistantAdapters()
    const module = createOperatorAiAssistantModule(adapters)

    module.openDrawer({ operatorFirstName: "Mohamed" })
    module.setComposerDraft("Summarise recent feedback")
    module.send()
    await Promise.resolve()
    await Promise.resolve()
    expect(adapters.conversations).toHaveLength(1)

    module.closeDrawer()
    expect(module.getSnapshot().drawerOpen).toBe(false)
    expect(adapters.conversations).toHaveLength(1)
    expect(adapters.conversations[0]?.title).toBe("Summarise recent feedback")
  })

  it("a new module with the same adapters does not delete stored conversations", async () => {
    const adapters = createInMemoryOperatorAiAssistantAdapters()
    const module = createOperatorAiAssistantModule(adapters)

    module.openDrawer({ operatorFirstName: "Mohamed" })
    module.setComposerDraft("Summarise recent feedback")
    module.send()
    await Promise.resolve()
    await Promise.resolve()
    expect(adapters.conversations).toHaveLength(1)

    const signedOut = createOperatorAiAssistantModule(adapters)
    signedOut.openDrawer({ operatorFirstName: "Mohamed" })

    expect(signedOut.getSnapshot().conversationId).toBe(null)
    expect(adapters.conversations).toHaveLength(1)
    expect(adapters.conversations[0]?.title).toBe("Summarise recent feedback")
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

describe("grounded live answers, helpful fill, and Actions", () => {
  it("fills, switches, and clears Helpful on a grounded answer", async () => {
    const adapters = createInMemoryOperatorAiAssistantAdapters()
    const module = createOperatorAiAssistantModule(adapters)

    module.openDrawer({ operatorFirstName: "Mohamed" })
    module.setComposerDraft("Summarise recent feedback")
    module.send()
    await Promise.resolve()
    await Promise.resolve()

    const assistantId = module
      .getSnapshot()
      .messages.find((message) => message.role === "assistant")?.id
    expect(assistantId).toBeTruthy()

    module.toggleHelpful(assistantId!, "helpful")
    expect(module.getSnapshot().helpfulFills[assistantId!]).toBe("helpful")

    module.toggleHelpful(assistantId!, "not-helpful")
    expect(module.getSnapshot().helpfulFills[assistantId!]).toBe("not-helpful")

    module.toggleHelpful(assistantId!, "not-helpful")
    expect(module.getSnapshot().helpfulFills[assistantId!]).toBeUndefined()
  })

  it("keeps Helpful fill after Close and resume, and clears it on New chat", async () => {
    const adapters = createInMemoryOperatorAiAssistantAdapters()
    const module = createOperatorAiAssistantModule(adapters)

    module.openDrawer({ operatorFirstName: "Mohamed" })
    module.setComposerDraft("Summarise recent feedback")
    module.send()
    await Promise.resolve()
    await Promise.resolve()

    const assistantId = module
      .getSnapshot()
      .messages.find((message) => message.role === "assistant")!.id
    module.toggleHelpful(assistantId, "helpful")
    module.closeDrawer()
    module.openDrawer({ operatorFirstName: "Mohamed" })
    await Promise.resolve()
    await Promise.resolve()

    expect(module.getSnapshot().helpfulFills[assistantId]).toBe("helpful")

    module.startNewChat()
    expect(module.getSnapshot().helpfulFills).toEqual({})

    const refreshed = createOperatorAiAssistantModule(adapters)
    expect(refreshed.getSnapshot().helpfulFills).toEqual({})
  })

  it("Action click leaves Expand and navigates with the send Analysis scope", async () => {
    const adapters = createInMemoryOperatorAiAssistantAdapters({
      sendTurn: async (input) => {
        const row = {
          id: "conv-1",
          title: input.message,
          analysisScope: input.analysisScope,
          lastActivityAt: new Date().toISOString(),
          isArchived: false,
          messages: [
            {
              id: "u1",
              role: "user" as const,
              body: input.message,
              analysisScope: input.analysisScope,
            },
            {
              id: "a1",
              role: "assistant" as const,
              class: "grounded" as const,
              title: "Feedback at Camden",
              body: "Camden received 1 feedback item over the last 7 days.",
              actions: [
                {
                  type: "view-feedback-set",
                  label: "View 1 feedback item",
                  count: 1,
                },
              ],
            },
          ],
        }
        adapters.conversations.push(row)
        return row
      },
    })
    const module = createOperatorAiAssistantModule(adapters)

    module.openDrawer({ operatorFirstName: "Mohamed" })
    module.expandDrawer()
    module.setComposerDraft("Summarise recent feedback")
    module.send()
    await Promise.resolve()
    await Promise.resolve()

    expect(module.getSnapshot().widthMode).toBe("expanded")
    module.clickAction({
      type: "view-feedback-set",
      label: "View 1 feedback item",
      count: 1,
    })

    expect(module.getSnapshot().widthMode).toBe("collapsed")
    expect(module.getSnapshot().drawerOpen).toBe(true)
    expect(adapters.lastNavigate?.action.type).toBe("view-feedback-set")
    expect(adapters.lastNavigate?.analysisScope.ownedLocationId).toBe(1)
  })

  it("Retry replaces the failure without a second user bubble", async () => {
    const adapters = createInMemoryOperatorAiAssistantAdapters({
      sendTurn: async (input) => {
        const row = {
          id: "conv-1",
          title: input.message,
          analysisScope: input.analysisScope,
          lastActivityAt: new Date().toISOString(),
          isArchived: false,
          messages: [
            {
              id: "u1",
              role: "user" as const,
              body: input.message,
              analysisScope: input.analysisScope,
            },
            {
              id: "a1",
              role: "assistant" as const,
              class: "failure" as const,
              body: "The answer could not be completed. Retry this turn.",
            },
          ],
        }
        adapters.conversations.push(row)
        return row
      },
    })
    const module = createOperatorAiAssistantModule(adapters)

    module.openDrawer({ operatorFirstName: "Mohamed" })
    module.setComposerDraft("Summarise recent feedback")
    module.send()
    await Promise.resolve()
    await Promise.resolve()

    expect(module.getSnapshot().retryVisible).toBe(true)
    module.retry()
    await Promise.resolve()
    await Promise.resolve()

    const users = module
      .getSnapshot()
      .messages.filter((message) => message.role === "user")
    expect(users).toHaveLength(1)
    expect(module.getSnapshot().messages.at(-1)?.class).toBe("grounded")
    expect(module.getSnapshot().retryVisible).toBe(false)
  })

  it("hides Retry after Apply when saved Analysis scope no longer matches the send", async () => {
    const adapters = createInMemoryOperatorAiAssistantAdapters({
      getDashboardMode: () => "multi",
      sendTurn: async (input) => {
        const row = {
          id: "conv-1",
          title: input.message,
          analysisScope: input.analysisScope,
          lastActivityAt: new Date().toISOString(),
          isArchived: false,
          messages: [
            {
              id: "u1",
              role: "user" as const,
              body: input.message,
              analysisScope: input.analysisScope,
            },
            {
              id: "a1",
              role: "assistant" as const,
              class: "failure" as const,
              body: "The answer could not be completed. Retry this turn.",
            },
          ],
        }
        adapters.conversations.push(row)
        return row
      },
    })
    const module = createOperatorAiAssistantModule(adapters)

    module.openDrawer({ operatorFirstName: "Mohamed" })
    module.setComposerDraft("Summarise recent feedback")
    module.send()
    await Promise.resolve()
    await Promise.resolve()
    expect(module.getSnapshot().retryVisible).toBe(true)

    module.openChangeScope()
    module.setChangeScopeDraftLocation(2)
    module.setChangeScopeDraftReportingPeriod({
      kind: "preset",
      presetId: "last30",
    })
    module.applyChangeScope()
    await Promise.resolve()
    await Promise.resolve()

    expect(module.getSnapshot().retryVisible).toBe(false)
  })
})

describe("Recent, Archive, search, and delete", () => {
  const now = new Date(2026, 7, 13, 15, 0, 0).getTime()
  const scope = {
    ownedLocationId: 11,
    ownedLocationName: "Camden",
    reportingPeriod: { kind: "preset" as const, presetId: "last7" as const },
  }

  function seed(
    adapters: ReturnType<typeof createInMemoryOperatorAiAssistantAdapters>,
    rows: Array<{
      id: string
      title: string
      lastActivityAt: string
      isArchived?: boolean
      ownedLocationName?: string
    }>
  ) {
    for (const row of rows) {
      adapters.conversations.push({
        id: row.id,
        title: row.title,
        analysisScope: {
          ...scope,
          ownedLocationName: row.ownedLocationName ?? "Camden",
        },
        lastActivityAt: row.lastActivityAt,
        isArchived: row.isArchived ?? false,
        messages: [
          { id: `${row.id}-u`, role: "user", body: row.title, analysisScope: scope },
          {
            id: `${row.id}-a`,
            role: "assistant",
            class: "grounded",
            title: "Stub",
            body: "Stub",
          },
        ],
      })
    }
  }

  it("groups Recent with now() and shows location · relative time in single mode", async () => {
    const adapters = createInMemoryOperatorAiAssistantAdapters({
      getDashboardMode: () => "single",
      nowMs: () => now,
    })
    seed(adapters, [
      {
        id: "today",
        title: "Today ask",
        lastActivityAt: new Date(2026, 7, 13, 14, 0, 0).toISOString(),
      },
      {
        id: "yest",
        title: "Yesterday ask",
        lastActivityAt: new Date(2026, 7, 12, 10, 0, 0).toISOString(),
      },
      {
        id: "old",
        title: "Older ask",
        lastActivityAt: new Date(2026, 7, 1, 10, 0, 0).toISOString(),
      },
    ])
    const module = createOperatorAiAssistantModule(adapters)
    module.openDrawer({ operatorFirstName: "Mohamed" })
    module.openRecent()
    await Promise.resolve()
    await Promise.resolve()

    expect(module.getSnapshot().recentGroups.map((group) => group.id)).toEqual([
      "today",
      "yesterday",
      "older",
    ])
    expect(module.getSnapshot().listRows[0]?.meta).toContain("Camden ·")
    expect(module.getSnapshot().listRows[0]?.meta).not.toMatch(/Draft saved/i)
  })

  it("filters Recent by client title search and uses search-miss copy", async () => {
    const adapters = createInMemoryOperatorAiAssistantAdapters({ nowMs: () => now })
    seed(adapters, [
      {
        id: "1",
        title: "Weekly feedback themes",
        lastActivityAt: new Date(2026, 7, 13, 14, 0, 0).toISOString(),
      },
      {
        id: "2",
        title: "August offer idea",
        lastActivityAt: new Date(2026, 7, 13, 13, 0, 0).toISOString(),
      },
    ])
    const module = createOperatorAiAssistantModule(adapters)
    module.openDrawer({ operatorFirstName: "Mohamed" })
    module.openRecent()
    await Promise.resolve()
    await Promise.resolve()

    module.setSearchQuery("offer")
    expect(module.getSnapshot().listRows.map((row) => row.id)).toEqual(["2"])
    expect(module.getSnapshot().showStartConversation).toBe(false)

    module.setSearchQuery("no such thread")
    expect(module.getSnapshot().listChromeKind).toBe("search-miss")
    expect(module.getSnapshot().listHeading).toBe("No conversations match")
    expect(module.getSnapshot().showStartConversation).toBe(false)
  })

  it("shows empty Recent copy with Start a conversation", async () => {
    const adapters = createInMemoryOperatorAiAssistantAdapters({ nowMs: () => now })
    const module = createOperatorAiAssistantModule(adapters)
    module.openDrawer({ operatorFirstName: "Mohamed" })
    module.openRecent()
    await Promise.resolve()
    await Promise.resolve()

    expect(module.getSnapshot().listChromeKind).toBe("empty-recent")
    expect(module.getSnapshot().listHeading).toBe("No conversations yet")
    expect(module.getSnapshot().showStartConversation).toBe(true)
    expect(module.getSnapshot().showArchiveFooter).toBe(true)
  })

  it("archives the open thread and stays on it; Archive is a flat list", async () => {
    const adapters = createInMemoryOperatorAiAssistantAdapters({ nowMs: () => now })
    const module = createOperatorAiAssistantModule(adapters)
    module.openDrawer({ operatorFirstName: "Mohamed" })
    module.setComposerDraft("Summarise recent feedback")
    module.send()
    await Promise.resolve()
    await Promise.resolve()

    const conversationId = module.getSnapshot().conversationId
    const lastActivity = adapters.conversations[0]?.lastActivityAt
    expect(conversationId).toBe("conv-1")

    module.openRecent()
    await Promise.resolve()
    await Promise.resolve()
    module.archiveConversation("conv-1")
    await Promise.resolve()
    await Promise.resolve()

    expect(module.getSnapshot().view).toBe("thread")
    expect(module.getSnapshot().conversationId).toBe("conv-1")
    expect(adapters.conversations[0]?.isArchived).toBe(true)
    expect(adapters.conversations[0]?.lastActivityAt).toBe(lastActivity)

    module.openArchive()
    await Promise.resolve()
    await Promise.resolve()
    expect(module.getSnapshot().view).toBe("archive")
    expect(module.getSnapshot().showSearch).toBe(false)
    expect(module.getSnapshot().recentGroups).toEqual([])
    expect(module.getSnapshot().archiveRows.map((row) => row.id)).toEqual(["conv-1"])

    module.unarchiveConversation("conv-1")
    await Promise.resolve()
    await Promise.resolve()
    expect(module.getSnapshot().view).toBe("thread")
    expect(adapters.conversations[0]?.isArchived).toBe(false)
    expect(adapters.conversations[0]?.lastActivityAt).toBe(lastActivity)
  })

  it("delete confirm hard-deletes and open-thread delete shows the empty greeting", async () => {
    const adapters = createInMemoryOperatorAiAssistantAdapters({ nowMs: () => now })
    const module = createOperatorAiAssistantModule(adapters)
    module.openDrawer({ operatorFirstName: "Mohamed" })
    module.setComposerDraft("Summarise recent feedback")
    module.send()
    await Promise.resolve()
    await Promise.resolve()

    module.requestDelete("conv-1")
    expect(module.getSnapshot().deleteConfirm).toEqual({
      open: true,
      conversationId: "conv-1",
    })

    module.cancelDelete()
    expect(module.getSnapshot().deleteConfirm.open).toBe(false)
    expect(adapters.conversations).toHaveLength(1)

    module.requestDelete("conv-1")
    module.confirmDelete()
    await Promise.resolve()
    await Promise.resolve()

    expect(adapters.conversations).toHaveLength(0)
    expect(module.getSnapshot().view).toBe("empty")
    expect(module.getSnapshot().conversationId).toBe(null)
    expect(module.getSnapshot().messages).toEqual([])
    expect(module.getSnapshot().deleteConfirm.open).toBe(false)
  })

  it("shows empty Archive copy and Back to conversation returns to the current chat", async () => {
    const adapters = createInMemoryOperatorAiAssistantAdapters({ nowMs: () => now })
    const module = createOperatorAiAssistantModule(adapters)
    module.openDrawer({ operatorFirstName: "Mohamed" })
    module.setComposerDraft("Summarise recent feedback")
    module.send()
    await Promise.resolve()
    await Promise.resolve()

    module.openRecent()
    await Promise.resolve()
    await Promise.resolve()
    module.openArchive()
    await Promise.resolve()
    await Promise.resolve()

    expect(module.getSnapshot().listChromeKind).toBe("empty-archive")
    expect(module.getSnapshot().listHeading).toBe("No archived conversations")
    expect(module.getSnapshot().listBody).toBe(
      "Conversations you archive will appear here."
    )

    module.backToConversation()
    expect(module.getSnapshot().view).toBe("thread")
    expect(module.getSnapshot().conversationId).toBe("conv-1")
  })

  it("opens a row, loads messages, and restores saved Analysis scope", async () => {
    const adapters = createInMemoryOperatorAiAssistantAdapters({ nowMs: () => now })
    seed(adapters, [
      {
        id: "conv-soho",
        title: "Soho ask",
        lastActivityAt: new Date(2026, 7, 13, 14, 0, 0).toISOString(),
        ownedLocationName: "Shoreditch",
      },
    ])
    adapters.conversations[0]!.analysisScope = {
      ownedLocationId: 22,
      ownedLocationName: "Shoreditch",
      reportingPeriod: { kind: "preset", presetId: "thisMonth" },
    }
    const module = createOperatorAiAssistantModule(adapters)
    module.openDrawer({ operatorFirstName: "Mohamed" })
    module.openRecent()
    await Promise.resolve()
    await Promise.resolve()
    module.openConversation("conv-soho")
    await Promise.resolve()
    await Promise.resolve()

    expect(module.getSnapshot().view).toBe("thread")
    expect(module.getSnapshot().conversationId).toBe("conv-soho")
    expect(module.getSnapshot().analysisScope).toEqual({
      ownedLocationId: 22,
      ownedLocationName: "Shoreditch",
      reportingPeriod: { kind: "preset", presetId: "thisMonth" },
    })
    expect(module.getSnapshot().messages.some((message) => message.role === "user")).toBe(
      true
    )
  })

  it("shows offline, list-error, and body-error chrome", async () => {
    const adapters = createInMemoryOperatorAiAssistantAdapters({
      nowMs: () => now,
      isOnline: () => false,
    })
    const module = createOperatorAiAssistantModule(adapters)
    module.openDrawer({ operatorFirstName: "Mohamed" })
    module.openRecent()
    await Promise.resolve()
    await Promise.resolve()

    expect(module.getSnapshot().listChromeKind).toBe("offline")
    expect(module.getSnapshot().listHeading).toBe("You’re offline")
    expect(module.getSnapshot().showListRetry).toBe(false)

    const failingList = createInMemoryOperatorAiAssistantAdapters({
      nowMs: () => now,
      listConversations: async () => {
        throw new Error("list failed")
      },
    })
    const listError = createOperatorAiAssistantModule(failingList)
    listError.openDrawer({ operatorFirstName: "Mohamed" })
    listError.openRecent()
    await Promise.resolve()
    await Promise.resolve()
    expect(listError.getSnapshot().listChromeKind).toBe("list-error")
    expect(listError.getSnapshot().listHeading).toBe("Could not load conversations")
    expect(listError.getSnapshot().showListRetry).toBe(true)

    const failingBody = createInMemoryOperatorAiAssistantAdapters({
      nowMs: () => now,
      getConversation: async () => {
        throw new Error("body failed")
      },
    })
    seed(failingBody, [
      {
        id: "conv-1",
        title: "Today ask",
        lastActivityAt: new Date(2026, 7, 13, 14, 0, 0).toISOString(),
      },
    ])
    const bodyError = createOperatorAiAssistantModule(failingBody)
    bodyError.openDrawer({ operatorFirstName: "Mohamed" })
    bodyError.openRecent()
    await Promise.resolve()
    await Promise.resolve()
    expect(bodyError.getSnapshot().view).toBe("recent")
    bodyError.openConversation("conv-1")
    await Promise.resolve()
    await Promise.resolve()
    expect(bodyError.getSnapshot().view).toBe("recent")
    expect(bodyError.getSnapshot().listChromeKind).toBe("body-error")
    expect(bodyError.getSnapshot().listHeading).toBe(
      "Could not load this conversation"
    )
    expect(bodyError.getSnapshot().sendBlocked).toBe(true)
    expect(bodyError.getSnapshot().showListRetry).toBe(true)
  })
})

describe("clarify vs grounded vs failure chrome", () => {
  const scope = {
    ownedLocationId: 1,
    ownedLocationName: "Camden",
    reportingPeriod: { kind: "preset" as const, presetId: "last7" as const },
  }

  it("clarify snapshot has body, no retry, no actions", async () => {
    const adapters = createInMemoryOperatorAiAssistantAdapters({
      sendTurn: async (input) => {
        const row = {
          id: "conv-clarify",
          title: input.message,
          analysisScope: input.analysisScope,
          lastActivityAt: new Date().toISOString(),
          isArchived: false,
          messages: [
            {
              id: "u1",
              role: "user" as const,
              body: input.message,
              analysisScope: input.analysisScope,
            },
            {
              id: "a1",
              role: "assistant" as const,
              class: "clarify" as const,
              body: "Which Owned locations should I compare? Name up to 3. Your locations: Camden, Soho.",
            },
          ],
        }
        adapters.conversations.push(row)
        return row
      },
    })
    const module = createOperatorAiAssistantModule(adapters)
    module.openDrawer({ operatorFirstName: "Mohamed" })
    module.setComposerDraft("compare all locations")
    module.send()
    await Promise.resolve()
    await Promise.resolve()

    const snapshot = module.getSnapshot()
    const assistant = snapshot.messages.find((message) => message.role === "assistant")
    expect(assistant?.class).toBe("clarify")
    expect(assistant?.body).toContain("Which Owned locations")
    expect(assistant?.actions ?? []).toEqual([])
    expect(snapshot.retryVisible).toBe(false)
  })

  it("grounded snapshot is eligible for helpful chrome", async () => {
    const adapters = createInMemoryOperatorAiAssistantAdapters({
      sendTurn: async (input) => {
        const row = {
          id: "conv-grounded",
          title: input.message,
          analysisScope: input.analysisScope,
          lastActivityAt: new Date().toISOString(),
          isArchived: false,
          messages: [
            {
              id: "u1",
              role: "user" as const,
              body: input.message,
              analysisScope: input.analysisScope,
            },
            {
              id: "a1",
              role: "assistant" as const,
              class: "grounded" as const,
              title: "Feedback at Camden",
              body: "Camden received 1 feedback item over the last 7 days.",
              actions: [
                {
                  type: "view-feedback-set",
                  label: "View 1 feedback item",
                  count: 1,
                },
              ],
            },
          ],
        }
        adapters.conversations.push(row)
        return row
      },
    })
    const module = createOperatorAiAssistantModule(adapters)
    module.openDrawer({ operatorFirstName: "Mohamed" })
    module.setComposerDraft("Summarise recent feedback")
    module.send()
    await Promise.resolve()
    await Promise.resolve()

    const snapshot = module.getSnapshot()
    const assistant = snapshot.messages.find((message) => message.role === "assistant")
    expect(assistant?.class).toBe("grounded")
    expect(assistant?.actions).toHaveLength(1)
    expect(snapshot.retryVisible).toBe(false)
    module.toggleHelpful(assistant!.id, "helpful")
    expect(module.getSnapshot().helpfulFills[assistant!.id]).toBe("helpful")
  })

  it("failure snapshot shows retry when send Analysis scope still matches", async () => {
    const adapters = createInMemoryOperatorAiAssistantAdapters({
      sendTurn: async (input) => {
        const row = {
          id: "conv-failure",
          title: input.message,
          analysisScope: input.analysisScope,
          lastActivityAt: new Date().toISOString(),
          isArchived: false,
          messages: [
            {
              id: "u1",
              role: "user" as const,
              body: input.message,
              analysisScope: input.analysisScope,
            },
            {
              id: "a1",
              role: "assistant" as const,
              class: "failure" as const,
              body: "The answer could not be completed. Retry this turn.",
            },
          ],
        }
        adapters.conversations.push(row)
        return row
      },
    })
    const module = createOperatorAiAssistantModule(adapters)
    module.openDrawer({ operatorFirstName: "Mohamed" })
    module.setComposerDraft("Summarise recent feedback")
    module.send()
    await Promise.resolve()
    await Promise.resolve()

    const snapshot = module.getSnapshot()
    const assistant = snapshot.messages.find((message) => message.role === "assistant")
    expect(assistant?.class).toBe("failure")
    expect(assistant?.actions ?? []).toEqual([])
    expect(snapshot.retryVisible).toBe(true)
    expect(snapshot.analysisScope).toMatchObject(scope)
  })
})
