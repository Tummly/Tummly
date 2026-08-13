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
})
