import { describe, expect, it } from "vitest"

import {
  createInMemoryOperatorNotificationsAdapters,
  createOperatorNotificationsModule,
} from "@/lib/operatorNotifications/createOperatorNotificationsModule"

import {
  createInMemoryOperatorAiAssistantAdapters,
  createOperatorAiAssistantModule,
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
})
