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
  inMemoryOpenableCampaignDraft,
  OPERATOR_ASSISTANT_MIC_ERROR_COPY,
  periodPhraseForReportingPeriod,
  type OperatorAiAssistantConversationRow,
} from "./createOperatorAiAssistantModule"
import { planAssistantActionNavigate } from "./assistantActionNavigate"

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

  it("keeps Expand active for sidebar list and conversation actions", async () => {
    const adapters = createInMemoryOperatorAiAssistantAdapters()
    const module = createOperatorAiAssistantModule(adapters)

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

    module.setComposerDraft("Summarise recent feedback")
    module.send()
    await Promise.resolve()
    await Promise.resolve()

    module.startNewChat()
    expect(module.getSnapshot().widthMode).toBe("expanded")

    module.openRecent()
    expect(module.getSnapshot()).toMatchObject({
      widthMode: "expanded",
      view: "empty",
      listPanel: "recent",
    })

    module.openArchive()
    expect(module.getSnapshot()).toMatchObject({
      widthMode: "expanded",
      view: "empty",
      listPanel: "archive",
    })

    module.openRecent()
    await Promise.resolve()
    await Promise.resolve()
    module.openConversation("conv-1")
    await Promise.resolve()
    await Promise.resolve()
    expect(module.getSnapshot()).toMatchObject({
      widthMode: "expanded",
      view: "thread",
      conversationId: "conv-1",
      listPanel: "recent",
    })

    module.openChangeScope()
    expect(module.getSnapshot()).toMatchObject({
      drawerOpen: true,
      widthMode: "expanded",
      view: "thread",
    })
    expect(module.getSnapshot().changeScopeDialog.open).toBe(true)

    module.leaveExpand()
    expect(module.getSnapshot()).toMatchObject({
      drawerOpen: true,
      widthMode: "collapsed",
      view: "thread",
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
  it("replaces the wait text with live pipeline progress and ignores late events", async () => {
    let release!: (
      row: ReturnType<
        typeof createInMemoryOperatorAiAssistantAdapters
      >["conversations"][number]
    ) => void
    const pending = new Promise<
      ReturnType<
        typeof createInMemoryOperatorAiAssistantAdapters
      >["conversations"][number]
    >((resolve) => {
      release = resolve
    })
    const adapters = createInMemoryOperatorAiAssistantAdapters({
      sendTurn: () => pending,
    })
    const module = createOperatorAiAssistantModule(adapters)

    module.openDrawer({ operatorFirstName: "Mohamed" })
    module.setComposerDraft("Summarise recent feedback")
    module.send()

    const waitBody = () =>
      module
        .getSnapshot()
        .messages.find((message) => message.role === "wait")?.body

    expect(waitBody()).toBe("Checking your question…")

    module.onTurnProgress({ conversationId: "conv-1", step: "checking" })
    module.onTurnProgress({ conversationId: "conv-other", step: "retrieving" })
    expect(waitBody()).toBe("Checking your question…")

    module.onTurnProgress({ conversationId: "conv-1", step: "retrieving" })
    expect(waitBody()).toBe("Retrieving data…")

    module.onTurnProgress({ conversationId: "conv-1", step: "preparing" })
    expect(waitBody()).toBe("Preparing answer…")

    release({
      id: "conv-1",
      title: "Summarise recent feedback",
      analysisScope: module.getSnapshot().analysisScope!,
      lastActivityAt: new Date().toISOString(),
      isArchived: false,
      messages: [],
    })
    await Promise.resolve()
    await Promise.resolve()

    expect(waitBody()).toBeUndefined()
    module.onTurnProgress({ conversationId: "conv-1", step: "retrieving" })
    expect(waitBody()).toBeUndefined()
  })

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
  it("shows Review, Change audience, and Add Offer without retrieve Actions on a completing answer", async () => {
    const adapters = createInMemoryOperatorAiAssistantAdapters({
      sendTurn: async (input) => ({
        id: "conv-mixed-ready",
        title: input.message,
        analysisScope: input.analysisScope,
        lastActivityAt: new Date().toISOString(),
        isArchived: false,
        pendingCampaignDraft: null,
        messages: [
          { id: "u1", role: "user", body: input.message, analysisScope: input.analysisScope },
          {
            id: "a1",
            role: "assistant",
            class: "grounded",
            title: "Campaign Draft saved",
            body: "1 feedback item.\n\nCampaign Draft saved.",
            actions: [
              { type: "view-feedback-set", label: "View 1 feedback item", count: 1 },
              {
                type: "review-campaign",
                label: "Review campaign draft",
                campaignId: 41,
              },
              {
                type: "change-audience",
                label: "Change audience",
                campaignId: 41,
              },
              {
                type: "add-offer",
                label: "Add Offer",
                campaignId: 41,
              },
            ],
          },
        ],
      }),
    })
    const module = createOperatorAiAssistantModule(adapters)
    module.openDrawer()
    module.setComposerDraft("Summarise feedback and draft a Campaign")
    module.send()
    await Promise.resolve()
    await Promise.resolve()

    expect(module.getSnapshot().messages.at(-1)?.actions).toEqual([
      expect.objectContaining({
        type: "review-campaign",
        label: "Review campaign draft",
        campaignId: 41,
        clickable: true,
      }),
      expect.objectContaining({
        type: "change-audience",
        label: "Change audience",
        campaignId: 41,
        clickable: true,
      }),
      expect.objectContaining({
        type: "add-offer",
        label: "Add Offer",
        campaignId: 41,
        clickable: true,
      }),
    ])
  })

  it("omits Add Offer on a completing answer when the Draft already has an Offer", async () => {
    const adapters = createInMemoryOperatorAiAssistantAdapters({
      sendTurn: async (input) => ({
        id: "conv-offer-attached",
        title: input.message,
        analysisScope: input.analysisScope,
        lastActivityAt: new Date().toISOString(),
        isArchived: false,
        pendingCampaignDraft: null,
        messages: [
          { id: "u1", role: "user", body: input.message, analysisScope: input.analysisScope },
          {
            id: "a1",
            role: "assistant",
            class: "grounded",
            title: "Campaign Draft saved",
            body: "Campaign Draft saved with an Offer.",
            actions: [
              {
                type: "review-campaign",
                label: "Review campaign draft",
                campaignId: 41,
              },
              {
                type: "change-audience",
                label: "Change audience",
                campaignId: 41,
              },
            ],
          },
        ],
      }),
    })
    const module = createOperatorAiAssistantModule(adapters)
    module.openDrawer()
    module.setComposerDraft("Draft an Email Campaign with Weekend brunch")
    module.send()
    await Promise.resolve()
    await Promise.resolve()

    const types = (module.getSnapshot().messages.at(-1)?.actions ?? []).map(
      (action) => action.type
    )
    expect(types).toEqual(["review-campaign", "change-audience"])
  })

  it("shows one Offer Draft Action without navigate Actions on a completing answer", async () => {
    const adapters = createInMemoryOperatorAiAssistantAdapters({
      sendTurn: async (input) => ({
        id: "conv-offer-ready",
        title: input.message,
        analysisScope: input.analysisScope,
        lastActivityAt: new Date().toISOString(),
        isArchived: false,
        pendingOfferDraft: {
          locationId: input.analysisScope.ownedLocationId,
          offerType: "percentage_discount",
          discountPercentage: 20,
          validity: "7_days_after_issue",
          title: "20% off",
          description: "Save 20%",
        },
        messages: [
          { id: "u1", role: "user", body: input.message, analysisScope: input.analysisScope },
          {
            id: "a1",
            role: "assistant",
            class: "grounded",
            title: "Feedback and Offer draft",
            body: "1 feedback item.\n\n- **Offer type:** Percentage discount",
            actions: [
              { type: "view-feedback-set", label: "View 1 feedback item", count: 1 },
              { type: "draft-offer", label: "Create offer draft" },
            ],
          },
        ],
      }),
    })
    const module = createOperatorAiAssistantModule(adapters)
    module.openDrawer()
    module.setComposerDraft("Summarise feedback and draft an Offer")
    module.send()
    await Promise.resolve()
    await Promise.resolve()

    expect(module.getSnapshot().messages.at(-1)?.actions).toEqual([
      expect.objectContaining({
        type: "draft-offer",
        label: "Create offer draft",
        clickable: true,
      }),
    ])
  })

  it("shows one open-recovery Action without navigate Actions on a completing answer", async () => {
    const adapters = createInMemoryOperatorAiAssistantAdapters({
      sendTurn: async (input) => ({
        id: "conv-recovery-ready",
        title: input.message,
        analysisScope: input.analysisScope,
        lastActivityAt: new Date().toISOString(),
        isArchived: false,
        pendingRecoveryDraft: {
          feedbackId: 42,
          intent: "respond-to-guest",
          channel: "email",
          message: "Thanks for telling us",
        },
        messages: [
          { id: "u1", role: "user", body: input.message, analysisScope: input.analysisScope },
          {
            id: "a1",
            role: "assistant",
            class: "grounded",
            title: "Feedback and recovery draft",
            body: "1 feedback item.\n\n- **Intent:** Respond to the guest",
            actions: [
              { type: "view-feedback-set", label: "View 1 feedback item", count: 1 },
              {
                type: "open-recovery",
                label: "Review recovery",
                feedbackId: 42,
                intent: "respond-to-guest",
              },
            ],
          },
        ],
      }),
    })
    const module = createOperatorAiAssistantModule(adapters)
    module.openDrawer()
    module.setComposerDraft("Summarise feedback and draft a recovery")
    module.send()
    await Promise.resolve()
    await Promise.resolve()

    expect(module.getSnapshot().messages.at(-1)?.actions).toEqual([
      expect.objectContaining({
        type: "open-recovery",
        label: "Review recovery",
        clickable: true,
      }),
    ])
  })

  it("keeps the Assistant open after a completing Campaign persist and does not POST create", async () => {
    let createCalls = 0
    const adapters = createInMemoryOperatorAiAssistantAdapters({
      sendTurn: async (input) => ({
        id: "conv-review",
        title: input.message,
        analysisScope: input.analysisScope,
        lastActivityAt: new Date().toISOString(),
        isArchived: false,
        pendingCampaignDraft: null,
        messages: [
          { id: "u1", role: "user", body: input.message, analysisScope: input.analysisScope },
          {
            id: "a1",
            role: "assistant",
            class: "grounded",
            title: "Campaign Draft saved",
            body: "Campaign Draft saved.",
            actions: [
              {
                type: "review-campaign",
                label: "Review campaign draft",
                campaignId: 41,
              },
            ],
          },
        ],
      }),
      createCampaignDraft: async () => {
        createCalls += 1
      },
    })
    const module = createOperatorAiAssistantModule(adapters)
    module.openDrawer()
    module.setComposerDraft(
      "Draft an Email Campaign to bring back all currently Email-eligible guests at Camden"
    )
    module.send()
    await Promise.resolve()
    await Promise.resolve()

    expect(module.getSnapshot().drawerOpen).toBe(true)
    expect(createCalls).toBe(0)
    expect(module.getSnapshot().messages.at(-1)?.actions?.[0]).toMatchObject({
      type: "review-campaign",
      label: "Review campaign draft",
      campaignId: 41,
      clickable: true,
    })
  })

  it("closes the Assistant and navigates on Review without POSTing create", async () => {
    let createCalls = 0
    const adapters = createInMemoryOperatorAiAssistantAdapters({
      sendTurn: async (input) => ({
        id: "conv-review",
        title: input.message,
        analysisScope: input.analysisScope,
        lastActivityAt: new Date().toISOString(),
        isArchived: false,
        pendingCampaignDraft: null,
        messages: [
          { id: "u1", role: "user", body: input.message, analysisScope: input.analysisScope },
          {
            id: "a1",
            role: "assistant",
            class: "grounded",
            title: "Campaign Draft saved",
            body: "Campaign Draft saved.",
            actions: [
              {
                type: "review-campaign",
                label: "Review campaign draft",
                campaignId: 41,
              },
            ],
          },
        ],
      }),
      createCampaignDraft: async () => {
        createCalls += 1
      },
    })
    const module = createOperatorAiAssistantModule(adapters)
    module.openDrawer()
    module.setComposerDraft(
      "Draft an Email Campaign to bring back all currently Email-eligible guests at Camden"
    )
    module.send()
    await Promise.resolve()
    await Promise.resolve()

    const action = module.getSnapshot().messages.at(-1)?.actions?.[0]
    expect(action?.type).toBe("review-campaign")
    module.clickAction(action!)
    await Promise.resolve()
    await Promise.resolve()

    expect(createCalls).toBe(0)
    expect(module.getSnapshot().drawerOpen).toBe(false)
    expect(adapters.lastNavigate?.action).toMatchObject({
      type: "review-campaign",
      label: "Review campaign draft",
      campaignId: 41,
    })
    expect(adapters.lastNavigate?.analysisScope.ownedLocationId).toBe(1)
    expect(adapters.lastNavigate?.campaignDraft).toMatchObject({
      id: 41,
      status: "draft",
      locationId: 1,
    })
  })

  it("closes the Assistant and lands Continue editing at Audience without POSTing create", async () => {
    let createCalls = 0
    const adapters = createInMemoryOperatorAiAssistantAdapters({
      sendTurn: async (input) => ({
        id: "conv-review",
        title: input.message,
        analysisScope: input.analysisScope,
        lastActivityAt: new Date().toISOString(),
        isArchived: false,
        pendingCampaignDraft: null,
        messages: [
          { id: "u1", role: "user", body: input.message, analysisScope: input.analysisScope },
          {
            id: "a1",
            role: "assistant",
            class: "grounded",
            title: "Campaign Draft saved",
            body: "Campaign Draft saved.",
            actions: [
              {
                type: "review-campaign",
                label: "Review campaign draft",
                campaignId: 41,
              },
              {
                type: "change-audience",
                label: "Change audience",
                campaignId: 41,
              },
              {
                type: "add-offer",
                label: "Add Offer",
                campaignId: 41,
              },
            ],
          },
        ],
      }),
      createCampaignDraft: async () => {
        createCalls += 1
      },
    })
    const module = createOperatorAiAssistantModule(adapters)
    module.openDrawer()
    module.setComposerDraft(
      "Draft an Email Campaign to bring back all currently Email-eligible guests at Camden"
    )
    module.send()
    await Promise.resolve()
    await Promise.resolve()

    const action = module
      .getSnapshot()
      .messages.at(-1)
      ?.actions?.find((item) => item.type === "change-audience")
    expect(action).toBeDefined()
    module.clickAction(action!)
    await Promise.resolve()
    await Promise.resolve()

    expect(createCalls).toBe(0)
    expect(module.getSnapshot().drawerOpen).toBe(false)
    expect(adapters.lastNavigate?.action).toMatchObject({
      type: "change-audience",
      label: "Change audience",
      campaignId: 41,
    })
    expect(adapters.lastNavigate?.analysisScope.ownedLocationId).toBe(1)
    expect(
      planAssistantActionNavigate({
        action: adapters.lastNavigate!.action,
        analysisScope: adapters.lastNavigate!.analysisScope,
        mode: "multi",
        campaignDraft: adapters.lastNavigate!.campaignDraft,
      }).campaigns
    ).toEqual({
      continueEditingCampaignId: 41,
      continueEditingStep: "audience",
      campaign: expect.objectContaining({ id: 41, status: "draft" }),
    })
  })

  it("closes the Assistant and lands Continue editing at Offer without POSTing create", async () => {
    let createCalls = 0
    const adapters = createInMemoryOperatorAiAssistantAdapters({
      sendTurn: async (input) => ({
        id: "conv-review",
        title: input.message,
        analysisScope: input.analysisScope,
        lastActivityAt: new Date().toISOString(),
        isArchived: false,
        pendingCampaignDraft: null,
        messages: [
          { id: "u1", role: "user", body: input.message, analysisScope: input.analysisScope },
          {
            id: "a1",
            role: "assistant",
            class: "grounded",
            title: "Campaign Draft saved",
            body: "Campaign Draft saved.",
            actions: [
              {
                type: "review-campaign",
                label: "Review campaign draft",
                campaignId: 41,
              },
              {
                type: "change-audience",
                label: "Change audience",
                campaignId: 41,
              },
              {
                type: "add-offer",
                label: "Add Offer",
                campaignId: 41,
              },
            ],
          },
        ],
      }),
      createCampaignDraft: async () => {
        createCalls += 1
      },
    })
    const module = createOperatorAiAssistantModule(adapters)
    module.openDrawer()
    module.setComposerDraft(
      "Draft an Email Campaign to bring back all currently Email-eligible guests at Camden"
    )
    module.send()
    await Promise.resolve()
    await Promise.resolve()

    const action = module
      .getSnapshot()
      .messages.at(-1)
      ?.actions?.find((item) => item.type === "add-offer")
    expect(action).toBeDefined()
    module.clickAction(action!)
    await Promise.resolve()
    await Promise.resolve()

    expect(createCalls).toBe(0)
    expect(module.getSnapshot().drawerOpen).toBe(false)
    expect(adapters.lastNavigate?.action).toMatchObject({
      type: "add-offer",
      label: "Add Offer",
      campaignId: 41,
    })
    expect(
      planAssistantActionNavigate({
        action: adapters.lastNavigate!.action,
        analysisScope: adapters.lastNavigate!.analysisScope,
        mode: "multi",
        campaignDraft: adapters.lastNavigate!.campaignDraft,
      }).campaigns
    ).toEqual({
      continueEditingCampaignId: 41,
      continueEditingStep: "offer",
      campaign: expect.objectContaining({ id: 41, status: "draft" }),
    })
  })

  it("does not make completing Campaign rows clickable while navigate is in flight", async () => {
    let releaseGet!: (campaign: ReturnType<typeof inMemoryOpenableCampaignDraft>) => void
    const adapters = createInMemoryOperatorAiAssistantAdapters({
      sendTurn: async (input) => ({
        id: "conv-review",
        title: input.message,
        analysisScope: input.analysisScope,
        lastActivityAt: new Date().toISOString(),
        isArchived: false,
        pendingCampaignDraft: null,
        messages: [
          { id: "u1", role: "user", body: input.message, analysisScope: input.analysisScope },
          {
            id: "a1",
            role: "assistant",
            class: "grounded",
            title: "Campaign Draft saved",
            body: "Campaign Draft saved.",
            actions: [
              {
                type: "review-campaign",
                label: "Review campaign draft",
                campaignId: 41,
              },
              {
                type: "change-audience",
                label: "Change audience",
                campaignId: 41,
              },
              {
                type: "add-offer",
                label: "Add Offer",
                campaignId: 41,
              },
            ],
          },
        ],
      }),
      getCampaignDraft: () =>
        new Promise((resolve) => {
          releaseGet = resolve
        }),
    })
    const module = createOperatorAiAssistantModule(adapters)
    module.openDrawer()
    module.setComposerDraft(
      "Draft an Email Campaign to bring back all currently Email-eligible guests at Camden"
    )
    module.send()
    await Promise.resolve()
    await Promise.resolve()

    const action = module
      .getSnapshot()
      .messages.at(-1)
      ?.actions?.find((item) => item.type === "change-audience")
    module.clickAction(action!)
    await Promise.resolve()

    const types = (module.getSnapshot().messages.at(-1)?.actions ?? []).map(
      (item) => [item.type, item.clickable] as const
    )
    expect(types).toEqual([
      ["review-campaign", false],
      ["change-audience", false],
      ["add-offer", false],
    ])
    expect(module.getSnapshot().drawerOpen).toBe(true)
    expect(adapters.lastNavigate).toBeNull()

    releaseGet(inMemoryOpenableCampaignDraft(41))
    await Promise.resolve()
    await Promise.resolve()
    expect(module.getSnapshot().drawerOpen).toBe(false)
    expect(adapters.lastNavigate?.action.type).toBe("change-audience")
  })

  it("keeps completing Campaign rows on an earlier answer clickable while a later answer navigate is in flight", async () => {
    let turns = 0
    let releaseGet!: (campaign: ReturnType<typeof inMemoryOpenableCampaignDraft>) => void
    const adapters = createInMemoryOperatorAiAssistantAdapters({
      sendTurn: async (input) => {
        turns += 1
        const first = {
          id: "a1",
          role: "assistant" as const,
          class: "grounded" as const,
          title: "Campaign Draft saved",
          body: "Campaign Draft saved.",
          actions: [
            {
              type: "review-campaign" as const,
              label: "Review campaign draft",
              campaignId: 41,
            },
            {
              type: "change-audience" as const,
              label: "Change audience",
              campaignId: 41,
            },
            {
              type: "add-offer" as const,
              label: "Add Offer",
              campaignId: 41,
            },
          ],
        }
        if (turns === 1) {
          return {
            id: "conv-review",
            title: input.message,
            analysisScope: input.analysisScope,
            lastActivityAt: new Date().toISOString(),
            isArchived: false,
            pendingCampaignDraft: null,
            messages: [
              {
                id: "u1",
                role: "user",
                body: input.message,
                analysisScope: input.analysisScope,
              },
              first,
            ],
          }
        }
        return {
          id: "conv-review",
          title: input.message,
          analysisScope: input.analysisScope,
          lastActivityAt: new Date().toISOString(),
          isArchived: false,
          pendingCampaignDraft: null,
          messages: [
            {
              id: "u1",
              role: "user",
              body: "Draft campaign one",
              analysisScope: input.analysisScope,
            },
            first,
            {
              id: "u2",
              role: "user",
              body: input.message,
              analysisScope: input.analysisScope,
            },
            {
              id: "a2",
              role: "assistant",
              class: "grounded",
              title: "Campaign Draft saved",
              body: "Campaign Draft saved.",
              actions: [
                {
                  type: "review-campaign",
                  label: "Review campaign draft",
                  campaignId: 42,
                },
                {
                  type: "change-audience",
                  label: "Change audience",
                  campaignId: 42,
                },
                {
                  type: "add-offer",
                  label: "Add Offer",
                  campaignId: 42,
                },
              ],
            },
          ],
        }
      },
      getCampaignDraft: (campaignId) => {
        if (campaignId === 42) {
          return new Promise((resolve) => {
            releaseGet = resolve
          })
        }
        return Promise.resolve(inMemoryOpenableCampaignDraft(campaignId))
      },
    })
    const module = createOperatorAiAssistantModule(adapters)
    module.openDrawer()
    module.setComposerDraft("Draft campaign one")
    module.send()
    await Promise.resolve()
    await Promise.resolve()
    module.setComposerDraft("Draft campaign two")
    module.send()
    await Promise.resolve()
    await Promise.resolve()

    const later = module
      .getSnapshot()
      .messages.at(-1)
      ?.actions?.find((item) => item.type === "change-audience")
    module.clickAction(later!)
    await Promise.resolve()

    const earlierClickable = module
      .getSnapshot()
      .messages
      .flatMap((message) => message.actions ?? [])
      .filter((action) => action.campaignId === 41)
      .map((action) => action.clickable)
    const laterClickable = module
      .getSnapshot()
      .messages.at(-1)
      ?.actions?.map((action) => [action.type, action.clickable] as const)

    expect(earlierClickable).toEqual([true, true, true])
    expect(laterClickable).toEqual([
      ["review-campaign", false],
      ["change-audience", false],
      ["add-offer", false],
    ])
    expect(module.getSnapshot().drawerOpen).toBe(true)

    releaseGet(inMemoryOpenableCampaignDraft(42))
    await Promise.resolve()
    await Promise.resolve()
    expect(module.getSnapshot().drawerOpen).toBe(false)
  })

  it("keeps earlier completing Campaign rows clickable after a later send", async () => {
    let turns = 0
    const adapters = createInMemoryOperatorAiAssistantAdapters({
      sendTurn: async (input) => {
        turns += 1
        const completing = {
          id: "a1",
          role: "assistant" as const,
          class: "grounded" as const,
          title: "Campaign Draft saved",
          body: "Campaign Draft saved.",
          actions: [
            {
              type: "review-campaign" as const,
              label: "Review campaign draft",
              campaignId: 41,
            },
            {
              type: "change-audience" as const,
              label: "Change audience",
              campaignId: 41,
            },
            {
              type: "add-offer" as const,
              label: "Add Offer",
              campaignId: 41,
            },
          ],
        }
        if (turns === 1) {
          return {
            id: "conv-review",
            title: input.message,
            analysisScope: input.analysisScope,
            lastActivityAt: new Date().toISOString(),
            isArchived: false,
            pendingCampaignDraft: null,
            messages: [
              { id: "u1", role: "user", body: input.message, analysisScope: input.analysisScope },
              completing,
            ],
          }
        }
        return {
          id: "conv-review",
          title: input.message,
          analysisScope: input.analysisScope,
          lastActivityAt: new Date().toISOString(),
          isArchived: false,
          pendingCampaignDraft: null,
          messages: [
            {
              id: "u1",
              role: "user",
              body: "Draft an Email Campaign",
              analysisScope: input.analysisScope,
            },
            completing,
            { id: "u2", role: "user", body: input.message, analysisScope: input.analysisScope },
            {
              id: "a2",
              role: "assistant",
              class: "grounded",
              title: "Feedback",
              body: "Stub retrieve",
              actions: [
                { type: "view-feedback-set", label: "View 1 feedback item", count: 1 },
              ],
            },
          ],
        }
      },
    })
    const module = createOperatorAiAssistantModule(adapters)
    module.openDrawer()
    module.setComposerDraft("Draft an Email Campaign")
    module.send()
    await Promise.resolve()
    await Promise.resolve()

    module.setComposerDraft("Summarise recent feedback")
    module.send()
    await Promise.resolve()
    await Promise.resolve()

    const earlier = module
      .getSnapshot()
      .messages
      .flatMap((message) => message.actions ?? [])
      .filter((action) =>
        action.type === "review-campaign"
        || action.type === "change-audience"
        || action.type === "add-offer"
      )
    expect(earlier).toHaveLength(3)
    expect(earlier.every((action) => action.clickable === true)).toBe(true)
    expect(earlier.map((action) => action.label)).toEqual([
      "Review campaign draft",
      "Change audience",
      "Add Offer",
    ])
  })

  it("keeps Review clickable after resume from getConversation", async () => {
    const reviewRow: OperatorAiAssistantConversationRow = {
      id: "conv-review",
      title: "Draft an Email Campaign",
      analysisScope: {
        ownedLocationId: 1,
        ownedLocationName: "Camden",
        reportingPeriod: { kind: "preset" as const, presetId: "last7" },
      },
      lastActivityAt: new Date().toISOString(),
      isArchived: false,
      pendingCampaignDraft: null,
      messages: [
        {
          id: "u1",
          role: "user" as const,
          body: "Draft an Email Campaign to bring back all currently Email-eligible guests at Camden",
          analysisScope: {
            ownedLocationId: 1,
            ownedLocationName: "Camden",
            reportingPeriod: { kind: "preset" as const, presetId: "last7" },
          },
        },
        {
          id: "a1",
          role: "assistant" as const,
          class: "grounded" as const,
          title: "Campaign Draft saved",
          body: "Campaign Draft saved.",
          actions: [
            {
              type: "review-campaign" as const,
              label: "Review campaign draft",
              campaignId: 41,
            },
          ],
        },
      ],
    }
    const adapters = createInMemoryOperatorAiAssistantAdapters({
      sendTurn: async () => reviewRow,
      getConversation: async (conversationId) =>
        conversationId === "conv-review" ? reviewRow : null,
      createCampaignDraft: async () => {
        throw new Error("createCampaignDraft must not run")
      },
    })
    const module = createOperatorAiAssistantModule(adapters)
    module.openDrawer()
    module.setComposerDraft("Draft an Email Campaign")
    module.send()
    await Promise.resolve()
    await Promise.resolve()
    module.closeDrawer()

    module.openDrawer()
    module.openConversation("conv-review")
    await Promise.resolve()
    await Promise.resolve()

    expect(module.getSnapshot().conversationId).toBe("conv-review")
    expect(module.getSnapshot().messages.at(-1)?.actions?.[0]).toMatchObject({
      type: "review-campaign",
      label: "Review campaign draft",
      campaignId: 41,
      clickable: true,
    })
  })

  it("does not make Review clickable while a turn is in flight", async () => {
    let turns = 0
    let release!: (
      row: ReturnType<typeof createInMemoryOperatorAiAssistantAdapters>["conversations"][number]
    ) => void
    const hung = new Promise<
      ReturnType<typeof createInMemoryOperatorAiAssistantAdapters>["conversations"][number]
    >((resolve) => {
      release = resolve
    })
    const adapters = createInMemoryOperatorAiAssistantAdapters({
      sendTurn: async (input) => {
        turns += 1
        if (turns === 1) {
          return {
            id: "conv-review",
            title: input.message,
            analysisScope: input.analysisScope,
            lastActivityAt: new Date().toISOString(),
            isArchived: false,
            pendingCampaignDraft: null,
            messages: [
              { id: "u1", role: "user", body: input.message, analysisScope: input.analysisScope },
              {
                id: "a1",
                role: "assistant",
                class: "grounded",
                title: "Campaign Draft saved",
                body: "Campaign Draft saved.",
                actions: [
                  {
                    type: "review-campaign",
                    label: "Review campaign draft",
                    campaignId: 41,
                  },
                ],
              },
            ],
          }
        }
        return hung
      },
    })
    const module = createOperatorAiAssistantModule(adapters)
    module.openDrawer()
    module.setComposerDraft(
      "Draft an Email Campaign to bring back all currently Email-eligible guests at Camden"
    )
    module.send()
    await Promise.resolve()
    await Promise.resolve()

    expect(
      module
        .getSnapshot()
        .messages.flatMap((message) => message.actions ?? [])
        .find((action) => action.type === "review-campaign")?.clickable
    ).toBe(true)

    module.setComposerDraft("Summarise recent feedback")
    module.send()

    expect(module.getSnapshot().turnInFlight).toBe(true)
    expect(
      module
        .getSnapshot()
        .messages.flatMap((message) => message.actions ?? [])
        .find((action) => action.type === "review-campaign")?.clickable
    ).toBe(false)

    release({
      id: "conv-review",
      title: "Summarise recent feedback",
      analysisScope: module.getSnapshot().analysisScope!,
      lastActivityAt: new Date().toISOString(),
      isArchived: false,
      messages: [
        {
          id: "u2",
          role: "user",
          body: "Summarise recent feedback",
          analysisScope: module.getSnapshot().analysisScope!,
        },
        {
          id: "a2",
          role: "assistant",
          class: "grounded",
          title: "Feedback",
          body: "Stub",
        },
      ],
    })
    await Promise.resolve()
    await Promise.resolve()
  })

  it("keeps the Assistant open when Review navigate fails", async () => {
    const adapters = createInMemoryOperatorAiAssistantAdapters({
      sendTurn: async (input) => ({
        id: "conv-review",
        title: input.message,
        analysisScope: input.analysisScope,
        lastActivityAt: new Date().toISOString(),
        isArchived: false,
        pendingCampaignDraft: null,
        messages: [
          { id: "u1", role: "user", body: input.message, analysisScope: input.analysisScope },
          {
            id: "a1",
            role: "assistant",
            class: "grounded",
            title: "Campaign Draft saved",
            body: "Campaign Draft saved.",
            actions: [
              {
                type: "review-campaign",
                label: "Review campaign draft",
                campaignId: 41,
              },
            ],
          },
        ],
      }),
      navigateAction: () => {
        throw new Error("navigate failed")
      },
    })
    const module = createOperatorAiAssistantModule(adapters)
    module.openDrawer()
    module.setComposerDraft("Draft an Email Campaign")
    module.send()
    await Promise.resolve()
    await Promise.resolve()

    const action = module.getSnapshot().messages.at(-1)?.actions?.[0]
    module.clickAction(action!)
    await Promise.resolve()
    await Promise.resolve()

    expect(module.getSnapshot().drawerOpen).toBe(true)
    expect(adapters.lastNavigate).toBeNull()
    expect(module.getSnapshot().messages.at(-1)?.actions?.[0]).toMatchObject({
      type: "review-campaign",
      label: "Review campaign draft",
      clickable: true,
    })
  })

  it.each([
    {
      name: "the Campaign Draft is missing",
      getCampaignDraft: async () => null,
    },
    {
      name: "the Campaign Draft load fails",
      getCampaignDraft: async () => {
        throw new Error("load failed")
      },
    },
    {
      name: "the Campaign is not Draft",
      getCampaignDraft: async () =>
        inMemoryOpenableCampaignDraft(41, { status: "scheduled" }),
    },
    {
      name: "the Campaign is at the wrong location",
      getCampaignDraft: async () =>
        inMemoryOpenableCampaignDraft(41, { locationId: 2 }),
    },
  ])(
    "keeps the Assistant open and Change audience clickable when $name",
    async ({ getCampaignDraft }) => {
      const adapters = createInMemoryOperatorAiAssistantAdapters({
        sendTurn: async (input) => ({
          id: "conv-review",
          title: input.message,
          analysisScope: input.analysisScope,
          lastActivityAt: new Date().toISOString(),
          isArchived: false,
          pendingCampaignDraft: null,
          messages: [
            {
              id: "u1",
              role: "user",
              body: input.message,
              analysisScope: input.analysisScope,
            },
            {
              id: "a1",
              role: "assistant",
              class: "grounded",
              title: "Campaign Draft saved",
              body: "Campaign Draft saved.",
              actions: [
                {
                  type: "review-campaign",
                  label: "Review campaign draft",
                  campaignId: 41,
                },
                {
                  type: "change-audience",
                  label: "Change audience",
                  campaignId: 41,
                },
                {
                  type: "add-offer",
                  label: "Add Offer",
                  campaignId: 41,
                },
              ],
            },
          ],
        }),
        getCampaignDraft,
      })
      const module = createOperatorAiAssistantModule(adapters)
      module.openDrawer()
      module.setComposerDraft(
        "Draft an Email Campaign to bring back all currently Email-eligible guests at Camden"
      )
      module.send()
      await Promise.resolve()
      await Promise.resolve()

      const action = module
        .getSnapshot()
        .messages.at(-1)
        ?.actions?.find((item) => item.type === "change-audience")
      module.clickAction(action!)
      await Promise.resolve()
      await Promise.resolve()

      expect(module.getSnapshot().drawerOpen).toBe(true)
      expect(adapters.lastNavigate).toBeNull()
      expect(
        module
          .getSnapshot()
          .messages.at(-1)
          ?.actions?.find((item) => item.type === "change-audience")
      ).toMatchObject({
        type: "change-audience",
        clickable: true,
      })
    }
  )

  it("opens Review recovery with prepare + navigate and spends the row", async () => {
    const calls: string[] = []
    const recoveryDraft = {
      feedbackId: 42,
      intent: "respond-to-guest" as const,
      channel: "email" as const,
      purpose: "acknowledge_feedback" as const,
      tone: "warm_and_apologetic" as const,
      includeNotes: "",
      subject: "Following up",
      message: "Thanks",
    }
    const adapters = createInMemoryOperatorAiAssistantAdapters({
      sendTurn: async (input) => ({
        id: "conv-recovery",
        title: input.message,
        analysisScope: input.analysisScope,
        lastActivityAt: new Date().toISOString(),
        isArchived: false,
        pendingRecoveryDraft: recoveryDraft,
        messages: [
          {
            id: "u1",
            role: "user",
            body: input.message,
            analysisScope: input.analysisScope,
          },
          {
            id: "a1",
            role: "assistant",
            class: "grounded",
            title: "Recovery draft ready",
            body: "- **Feedback:** Pat Guest",
            actions: [
              {
                type: "open-recovery",
                label: "Review recovery",
                feedbackId: 42,
                intent: "respond-to-guest",
              },
            ],
          },
        ],
      }),
      prepareOpenRecovery: async () => {
        calls.push("prepare")
      },
      openRecoveryFromDraftAction: async () => {
        calls.push("open")
      },
      clearDraftInterview: async () => {
        calls.push("clear")
      },
      navigateAction: () => {
        calls.push("land")
      },
    })
    const module = createOperatorAiAssistantModule(adapters)
    module.openDrawer()
    module.setComposerDraft("Draft a recovery response")
    module.send()
    await Promise.resolve()
    await Promise.resolve()

    module.clickAction({
      type: "open-recovery",
      label: "Review recovery",
      feedbackId: 42,
      intent: "respond-to-guest",
    })
    await new Promise((resolve) => setTimeout(resolve, 0))

    expect(calls).toEqual(["prepare", "open", "clear", "land"])
    expect(module.getSnapshot().drawerOpen).toBe(false)
    expect(module.getSnapshot().messages.at(-1)?.actions?.[0]).toMatchObject({
      label: "Review recovery",
      clickable: false,
    })
  })

  it("keeps Review recovery clickable after hydrate failure", async () => {
    const toasts: string[] = []
    const calls: string[] = []
    const adapters = createInMemoryOperatorAiAssistantAdapters({
      sendTurn: async (input) => ({
        id: "conv-recovery-hydrate-fail",
        title: input.message,
        analysisScope: input.analysisScope,
        lastActivityAt: new Date().toISOString(),
        isArchived: false,
        pendingRecoveryDraft: {
          feedbackId: 42,
          intent: "respond-to-guest",
          channel: "email",
          purpose: "acknowledge_feedback",
          tone: "warm_and_apologetic",
          includeNotes: "",
          subject: "Hi",
          message: "Thanks",
        },
        messages: [
          {
            id: "u1",
            role: "user",
            body: input.message,
            analysisScope: input.analysisScope,
          },
          {
            id: "a1",
            role: "assistant",
            class: "grounded",
            title: "Recovery draft ready",
            body: "- **Feedback:** Pat Guest",
            actions: [
              {
                type: "open-recovery",
                label: "Review recovery",
                feedbackId: 42,
                intent: "respond-to-guest",
              },
            ],
          },
        ],
      }),
      prepareOpenRecovery: async () => {
        calls.push("prepare")
      },
      openRecoveryFromDraftAction: async () => {
        calls.push("open")
        throw new Error("Could not open recovery. Please try again.")
      },
      clearDraftInterview: async () => {
        calls.push("clear")
      },
      navigateAction: () => {
        calls.push("land")
      },
      notifyRecoveryDraftError: (message) => {
        toasts.push(message)
      },
    })
    const module = createOperatorAiAssistantModule(adapters)
    module.openDrawer()
    module.setComposerDraft("Draft a recovery response")
    module.send()
    await Promise.resolve()
    await Promise.resolve()

    module.clickAction({
      type: "open-recovery",
      label: "Review recovery",
      feedbackId: 42,
      intent: "respond-to-guest",
    })
    await new Promise((resolve) => setTimeout(resolve, 0))

    expect(calls).toEqual(["prepare", "open"])
    expect(module.getSnapshot().drawerOpen).toBe(true)
    expect(module.getSnapshot().messages.at(-1)?.actions?.[0]?.clickable).toBe(
      true
    )
    expect(toasts).toEqual(["Could not open recovery. Please try again."])
  })

  it("keeps Review recovery clickable after prepare failure and surfaces the toast", async () => {
    const toasts: string[] = []
    const adapters = createInMemoryOperatorAiAssistantAdapters({
      sendTurn: async (input) => ({
        id: "conv-recovery-fail",
        title: input.message,
        analysisScope: input.analysisScope,
        lastActivityAt: new Date().toISOString(),
        isArchived: false,
        pendingRecoveryDraft: {
          feedbackId: 42,
          intent: "respond-to-guest",
          channel: "email",
          purpose: "acknowledge_feedback",
          tone: "warm_and_apologetic",
          includeNotes: "",
          subject: "Hi",
          message: "Thanks",
        },
        messages: [
          {
            id: "u1",
            role: "user",
            body: input.message,
            analysisScope: input.analysisScope,
          },
          {
            id: "a1",
            role: "assistant",
            class: "grounded",
            title: "Recovery draft ready",
            body: "- **Feedback:** Pat Guest",
            actions: [
              {
                type: "open-recovery",
                label: "Review recovery",
                feedbackId: 42,
                intent: "respond-to-guest",
              },
            ],
          },
        ],
      }),
      prepareOpenRecovery: async () => {
        throw new Error(
          "This feedback is resolved. Reopen it before starting recovery."
        )
      },
      notifyRecoveryDraftError: (message) => {
        toasts.push(message)
      },
    })
    const module = createOperatorAiAssistantModule(adapters)
    module.openDrawer()
    module.setComposerDraft("Draft a recovery response")
    module.send()
    await Promise.resolve()
    await Promise.resolve()

    module.clickAction({
      type: "open-recovery",
      label: "Review recovery",
      feedbackId: 42,
      intent: "respond-to-guest",
    })
    await new Promise((resolve) => setTimeout(resolve, 0))

    expect(module.getSnapshot().drawerOpen).toBe(true)
    expect(module.getSnapshot().messages.at(-1)?.actions?.[0]?.clickable).toBe(
      true
    )
    expect(toasts).toEqual([
      "This feedback is resolved. Reopen it before starting recovery.",
    ])
  })

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

  it("keeps named-row list bodies and summarise bodies from sendTurn", async () => {
    const adapters = createInMemoryOperatorAiAssistantAdapters({
      sendTurn: async (input) => {
        const listAsk = /show|list/i.test(input.message)
        const row = {
          id: "conv-named",
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
              title: listAsk
                ? "Feedback at Camden over the last 7 days"
                : "WaitTime is the main theme over the last 7 days",
              body: listAsk
                ? "Camden has 2 feedback items over the last 7 days. Ava Guest — negative. Ben Guest — negative."
                : "Camden received 2 feedback items over the last 7 days. Top themes: WaitTime (2). Excerpts: \"Slow service\".",
              actions: listAsk
                ? [
                    {
                      type: "view-feedback-set",
                      label: "View 2 feedback items",
                      count: 2,
                    },
                  ]
                : [
                    {
                      type: "view-feedback-set",
                      label: "View 2 feedback items",
                      count: 2,
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

    const summariseBody = module.getSnapshot().messages.at(-1)?.body ?? ""
    expect(summariseBody).toContain("received 2 feedback items")
    expect(summariseBody).not.toContain("Ava Guest")
    expect(summariseBody).not.toContain("Ben Guest")

    module.startNewChat()
    module.setComposerDraft("List feedback")
    module.send()
    await Promise.resolve()
    await Promise.resolve()

    const listBody = module.getSnapshot().messages.at(-1)?.body ?? ""
    expect(listBody).toContain("Ava Guest")
    expect(listBody).toContain("Ben Guest")
  })

  it("guest Action click navigates with Analysis scope location and does not set a date range", async () => {
    const adapters = createInMemoryOperatorAiAssistantAdapters({
      sendTurn: async (input) => {
        const row = {
          id: "conv-guests",
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
              title: "Location Guests at Camden",
              body: "Location Guests at Camden over the last 7 days: Ava Guest — Eligible — Email; Ben Guest — Eligible — Email.",
              actions: [
                {
                  type: "view-guests",
                  label: "View guests",
                  marketingEligible: true,
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
    module.setComposerDraft("Show guests who gave poor feedback but opted in")
    module.send()
    await Promise.resolve()
    await Promise.resolve()

    module.clickAction({
      type: "view-guests",
      label: "View guests",
      marketingEligible: true,
    })

    expect(adapters.lastNavigate?.action.type).toBe("view-guests")
    expect(adapters.lastNavigate?.action.marketingEligible).toBe(true)
    expect(adapters.lastNavigate?.analysisScope.ownedLocationId).toBe(1)
    expect(adapters.lastNavigate).not.toHaveProperty("guestsOverviewDateRange")
  })

  it("Action click for offers, Campaigns, and Capture still collapses Expand", async () => {
    const actions = [
      { type: "view-capture", label: "View Capture" },
      { type: "view-offer", label: "View offer", offerId: 22 },
      { type: "view-campaigns", label: "Open Campaigns" },
      { type: "view-offers", label: "Open Offers" },
    ]
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
              title: "Offers at Camden",
              body: "Camden has catalog offers.",
              actions,
            },
          ],
        }
        adapters.conversations.push(row)
        return row
      },
    })
    const module = createOperatorAiAssistantModule(adapters)

    module.openDrawer({ operatorFirstName: "Mohamed" })
    module.setComposerDraft("Summarise Offers Performance")
    module.send()
    await Promise.resolve()
    await Promise.resolve()

    for (const action of actions) {
      module.expandDrawer()
      expect(module.getSnapshot().widthMode).toBe("expanded")
      module.clickAction(action)
      expect(module.getSnapshot().widthMode).toBe("collapsed")
      expect(module.getSnapshot().drawerOpen).toBe(true)
      expect(adapters.lastNavigate?.action.type).toBe(action.type)
      expect(adapters.lastNavigate?.analysisScope.ownedLocationId).toBe(1)
    }
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
    expect(module.getSnapshot().messages.at(-1)).toMatchObject({
      role: "wait",
      body: "Checking your question…",
    })
    module.onTurnProgress({ conversationId: "conv-1", step: "retrieving" })
    expect(module.getSnapshot().messages.at(-1)).toMatchObject({
      role: "wait",
      body: "Retrieving data…",
    })
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

  it("shows the generated Assistant conversation title on Recent after the first complete without a page reload", async () => {
    const inner = createInMemoryOperatorAiAssistantAdapters({ nowMs: () => now })
    const sendTurn = inner.sendTurn.bind(inner)
    inner.sendTurn = async (input) => {
      const row = await sendTurn(input)
      row.title = "Bring back Email-eligible guests"
      return row
    }
    const module = createOperatorAiAssistantModule(inner)
    const ask =
      "Draft an Email Campaign to bring back all currently Email-eligible guests at Camden"

    module.openDrawer({ operatorFirstName: "Mohamed" })
    module.expandDrawer()
    await Promise.resolve()
    await Promise.resolve()
    expect(module.getSnapshot().listChromeKind).toBe("empty-recent")

    module.setComposerDraft(ask)
    module.send()
    await Promise.resolve()
    await Promise.resolve()

    const snapshot = module.getSnapshot()
    expect(snapshot.listChromeKind).toBe("rows")
    expect(snapshot.listRows.map((row) => row.title)).toEqual([
      "Bring back Email-eligible guests",
    ])
    expect(snapshot.recentGroups.flatMap((group) => group.rows).map((row) => row.title)).toEqual(
      ["Bring back Email-eligible guests"]
    )
    expect(snapshot.listRows[0]?.title).not.toBe(ask)

    module.archiveConversation(snapshot.conversationId!)
    await Promise.resolve()
    await Promise.resolve()
    module.openArchive()
    await Promise.resolve()
    await Promise.resolve()
    expect(module.getSnapshot().archiveRows.map((row) => row.title)).toEqual([
      "Bring back Email-eligible guests",
    ])
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

  it("keeps loaded Recent rows visible when the list goes offline and blocks Send", async () => {
    const adapters = createInMemoryOperatorAiAssistantAdapters({ nowMs: () => now })
    seed(adapters, [
      {
        id: "conv-1",
        title: "Today ask",
        lastActivityAt: new Date(2026, 7, 13, 14, 0, 0).toISOString(),
      },
    ])
    const module = createOperatorAiAssistantModule(adapters)
    module.openDrawer({ operatorFirstName: "Mohamed" })
    module.openRecent()
    await Promise.resolve()
    await Promise.resolve()

    expect(module.getSnapshot().listChromeKind).toBe("rows")
    expect(module.getSnapshot().recentGroups.flatMap((group) => group.rows).map((row) => row.id)).toEqual(
      ["conv-1"]
    )

    adapters.online = false
    module.openRecent()
    await Promise.resolve()
    await Promise.resolve()

    const snapshot = module.getSnapshot()
    expect(snapshot.listChromeKind).toBe("offline")
    expect(snapshot.listHeading).toBe("You’re offline")
    expect(snapshot.listBody).toBe(
      "Previously loaded conversations may remain visible, but new messages cannot be sent until the connection returns."
    )
    expect(snapshot.listRows.map((row) => row.id)).toEqual(["conv-1"])
    expect(snapshot.recentGroups.flatMap((group) => group.rows).map((row) => row.id)).toEqual(
      ["conv-1"]
    )
    expect(snapshot.sendBlocked).toBe(true)
  })

  it("keeps centre offline chrome when the list is offline before any rows load", async () => {
    const adapters = createInMemoryOperatorAiAssistantAdapters({
      nowMs: () => now,
      isOnline: () => false,
    })
    const module = createOperatorAiAssistantModule(adapters)
    module.openDrawer({ operatorFirstName: "Mohamed" })
    module.openRecent()
    await Promise.resolve()
    await Promise.resolve()

    const snapshot = module.getSnapshot()
    expect(snapshot.listChromeKind).toBe("offline")
    expect(snapshot.listHeading).toBe("You’re offline")
    expect(snapshot.listBody).toBe(
      "Previously loaded conversations may remain visible, but new messages cannot be sent until the connection returns."
    )
    expect(snapshot.listRows).toEqual([])
    expect(snapshot.recentGroups).toEqual([])
    expect(snapshot.sendBlocked).toBe(true)
  })
})

describe("Expand Escape and exclusive-open", () => {
  it("closes Delete this conversation? on Expand Escape and keeps the Assistant expanded", async () => {
    const adapters = createInMemoryOperatorAiAssistantAdapters()
    const module = createOperatorAiAssistantModule(adapters)
    module.openDrawer({ operatorFirstName: "Mohamed" })
    module.setComposerDraft("Summarise recent feedback")
    module.send()
    await Promise.resolve()
    await Promise.resolve()

    module.expandDrawer()
    module.requestDelete("conv-1")
    expect(module.getSnapshot()).toMatchObject({
      drawerOpen: true,
      widthMode: "expanded",
      deleteConfirm: { open: true, conversationId: "conv-1" },
    })

    module.dismissFromEscape()

    expect(module.getSnapshot()).toMatchObject({
      drawerOpen: true,
      widthMode: "expanded",
      deleteConfirm: { open: false, conversationId: null },
    })
    expect(adapters.conversations).toHaveLength(1)
  })

  it("closes Change analysis scope on Expand Escape and keeps the Assistant expanded", () => {
    const module = createOperatorAiAssistantModule(
      createInMemoryOperatorAiAssistantAdapters()
    )
    module.openDrawer({ operatorFirstName: "Mohamed" })
    module.expandDrawer()
    module.openChangeScope()
    expect(module.getSnapshot().changeScopeDialog.open).toBe(true)

    module.dismissFromEscape()

    expect(module.getSnapshot()).toMatchObject({
      drawerOpen: true,
      widthMode: "expanded",
    })
    expect(module.getSnapshot().changeScopeDialog.open).toBe(false)
  })

  it("closes the Assistant on Expand Escape when no portaled dialog is open", () => {
    const module = createOperatorAiAssistantModule(
      createInMemoryOperatorAiAssistantAdapters()
    )
    module.openDrawer({ operatorFirstName: "Mohamed" })
    module.expandDrawer()

    module.dismissFromEscape()

    expect(module.getSnapshot()).toMatchObject({
      drawerOpen: false,
      widthMode: "collapsed",
    })
  })

  it("calls closePeerRightDrawers when the Assistant opens so exclusive-open cannot be skipped", () => {
    let closePeerCalls = 0
    const adapters = createInMemoryOperatorAiAssistantAdapters({
      closePeerRightDrawers: () => {
        closePeerCalls += 1
      },
    })
    const module = createOperatorAiAssistantModule(adapters)

    module.openDrawer({ operatorFirstName: "Mohamed" })

    expect(closePeerCalls).toBe(1)
    expect(module.getSnapshot().drawerOpen).toBe(true)
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

  it("gap snapshot has body, no retry, no actions", async () => {
    const adapters = createInMemoryOperatorAiAssistantAdapters({
      sendTurn: async (input) => {
        const row = {
          id: "conv-gap",
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
              class: "gap" as const,
              body: "Which Owned location should this Campaign Draft use? Name one.",
            },
          ],
        }
        adapters.conversations.push(row)
        return row
      },
    })
    const module = createOperatorAiAssistantModule(adapters)
    module.openDrawer({ operatorFirstName: "Mohamed" })
    module.setComposerDraft("Draft an Email Campaign for all locations")
    module.send()
    await Promise.resolve()
    await Promise.resolve()

    const snapshot = module.getSnapshot()
    const assistant = snapshot.messages.find((message) => message.role === "assistant")
    expect(assistant?.class).toBe("gap")
    expect(assistant?.body).toContain("Name one")
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

  it("start then confirm fills the composer and does not Send", async () => {
    const adapters = createInMemoryOperatorAiAssistantAdapters({
      mic: {
        startRecording: async () => undefined,
        stopRecording: async () => new Blob(["audio"], { type: "audio/webm" }),
        cancelRecording: async () => undefined,
        transcribe: async () => ({
          ok: true,
          text: "Summarise Camden this week",
        }),
      },
    })
    const module = createOperatorAiAssistantModule(adapters)
    module.openDrawer({ operatorFirstName: "Mohamed" })
    module.setComposerDraft("draft that transcript replaces")

    await module.startMic()
    expect(module.getSnapshot()).toMatchObject({
      micPhase: "recording",
      micChrome: "tick_cancel",
      composerLocked: true,
      sendLocked: true,
      chipsLocked: true,
    })

    await module.confirmMic()

    expect(module.getSnapshot().composerDraft).toBe("Summarise Camden this week")
    expect(module.getSnapshot().messages).toEqual([])
    expect(module.getSnapshot().conversationId).toBe(null)
    expect(adapters.conversations).toEqual([])
    expect(module.getSnapshot()).toMatchObject({
      micPhase: "idle",
      micChrome: "mic",
      composerLocked: false,
      sendLocked: false,
    })
  })

  it("cancel leaves the composer draft unchanged and does not Send", async () => {
    const adapters = createInMemoryOperatorAiAssistantAdapters()
    const module = createOperatorAiAssistantModule(adapters)
    module.openDrawer({ operatorFirstName: "Mohamed" })
    module.setComposerDraft("keep this draft")

    await module.startMic()
    await module.cancelMic()

    expect(module.getSnapshot().composerDraft).toBe("keep this draft")
    expect(module.getSnapshot().messages).toEqual([])
    expect(adapters.conversations).toEqual([])
    expect(module.getSnapshot().micPhase).toBe("idle")
  })

  it("does not start recording while a turn is in flight", async () => {
    const adapters = createInMemoryOperatorAiAssistantAdapters({
      sendTurn: () => new Promise(() => {}),
    })
    const module = createOperatorAiAssistantModule(adapters)
    module.openDrawer({ operatorFirstName: "Mohamed" })
    module.setComposerDraft("Summarise recent feedback")
    module.send()

    expect(module.getSnapshot().turnInFlight).toBe(true)
    expect(module.getSnapshot().micLocked).toBe(true)

    await module.startMic()

    expect(module.getSnapshot().micPhase).toBe("idle")
    expect(module.getSnapshot().composerDraft).toBe("")
  })

  it("does not start recording while offline", async () => {
    const adapters = createInMemoryOperatorAiAssistantAdapters()
    adapters.online = false
    const module = createOperatorAiAssistantModule(adapters)
    module.openDrawer({ operatorFirstName: "Mohamed" })
    module.setComposerDraft("keep this draft")

    expect(module.getSnapshot().micLocked).toBe(true)

    await module.startMic()

    expect(module.getSnapshot().micPhase).toBe("idle")
    expect(module.getSnapshot().composerDraft).toBe("keep this draft")
    expect(adapters.conversations).toEqual([])
  })

  it("permission denied uses operator copy and keeps typing available", async () => {
    const adapters = createInMemoryOperatorAiAssistantAdapters({
      mic: {
        startRecording: async () => {
          throw new DOMException("Permission denied", "NotAllowedError")
        },
        stopRecording: async () => new Blob(["audio"], { type: "audio/webm" }),
        cancelRecording: async () => undefined,
        transcribe: async () => ({ ok: true, text: "unused" }),
      },
    })
    const module = createOperatorAiAssistantModule(adapters)
    module.openDrawer({ operatorFirstName: "Mohamed" })
    module.setComposerDraft("typed question")

    await module.startMic()

    expect(module.getSnapshot()).toMatchObject({
      micPhase: "idle",
      composerLocked: false,
      micAvailable: false,
      micLocked: true,
      micError: {
        kind: "permission",
        message: OPERATOR_ASSISTANT_MIC_ERROR_COPY.permission,
      },
    })
    expect(module.getSnapshot().composerDraft).toBe("typed question")

    module.dismissMicError()
    expect(module.getSnapshot().micError).toBe(null)
    module.setComposerDraft("typed question still editable")
    expect(module.getSnapshot().composerDraft).toBe("typed question still editable")
  })

  it("empty speech uses operator copy and does not Send", async () => {
    const adapters = createInMemoryOperatorAiAssistantAdapters({
      mic: {
        startRecording: async () => undefined,
        stopRecording: async () => new Blob(["audio"], { type: "audio/webm" }),
        cancelRecording: async () => undefined,
        transcribe: async () => ({ ok: false, reason: "empty_speech" }),
      },
    })
    const module = createOperatorAiAssistantModule(adapters)
    module.openDrawer({ operatorFirstName: "Mohamed" })
    module.setComposerDraft("keep typing")

    await module.startMic()
    await module.confirmMic()

    expect(module.getSnapshot()).toMatchObject({
      composerDraft: "keep typing",
      composerLocked: false,
      micError: {
        kind: "empty_speech",
        message: OPERATOR_ASSISTANT_MIC_ERROR_COPY.empty_speech,
      },
    })
    expect(adapters.conversations).toEqual([])
  })

  it("transcribe failure uses operator copy and does not Send", async () => {
    const adapters = createInMemoryOperatorAiAssistantAdapters({
      mic: {
        startRecording: async () => undefined,
        stopRecording: async () => new Blob(["audio"], { type: "audio/webm" }),
        cancelRecording: async () => undefined,
        transcribe: async () => ({ ok: false, reason: "stt_failure" }),
      },
    })
    const module = createOperatorAiAssistantModule(adapters)
    module.openDrawer({ operatorFirstName: "Mohamed" })
    module.setComposerDraft("keep typing")

    await module.startMic()
    await module.confirmMic()

    expect(module.getSnapshot()).toMatchObject({
      composerDraft: "keep typing",
      composerLocked: false,
      micError: {
        kind: "stt_failure",
        message: OPERATOR_ASSISTANT_MIC_ERROR_COPY.stt_failure,
      },
    })
    expect(adapters.conversations).toEqual([])
  })

  it("a later chip click still replaces the whole draft after a transcript", async () => {
    const adapters = createInMemoryOperatorAiAssistantAdapters({
      mic: {
        startRecording: async () => undefined,
        stopRecording: async () => new Blob(["audio"], { type: "audio/webm" }),
        cancelRecording: async () => undefined,
        transcribe: async () => ({
          ok: true,
          text: "Summarise Camden this week",
        }),
      },
    })
    const module = createOperatorAiAssistantModule(adapters)
    module.openDrawer({ operatorFirstName: "Mohamed" })

    await module.startMic()
    await module.confirmMic()
    expect(module.getSnapshot().composerDraft).toBe("Summarise Camden this week")

    module.fillComposerFromChip("Draft an offer")
    expect(module.getSnapshot().composerDraft).toBe("Draft an offer")
    expect(adapters.conversations).toEqual([])
  })

  it("exposes stub AI credit chrome and stub actions do not send or change the snapshot", () => {
    const adapters = createInMemoryOperatorAiAssistantAdapters()
    const module = createOperatorAiAssistantModule(adapters)
    module.openDrawer({ operatorFirstName: "Mohamed" })

    expect(module.getSnapshot()).toMatchObject({
      creditsRemainingLine: "20 of 20 monthly AI actions remaining",
      viewUsageLabel: "View usage",
      addCreditsLabel: "Add credits",
    })

    const before = module.getSnapshot()
    module.viewUsage()
    module.addCredits()
    expect(module.getSnapshot()).toEqual(before)
    expect(adapters.conversations).toEqual([])
  })
})
