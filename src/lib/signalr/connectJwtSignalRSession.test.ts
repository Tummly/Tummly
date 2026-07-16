import {
  HubConnection,
  HubConnectionState,
} from "@microsoft/signalr"
import { describe, expect, it, vi } from "vitest"

import { connectJwtSignalRSession } from "./connectJwtSignalRSession"

function createFakeConnection() {
  const handlers = new Map<string, (...args: unknown[]) => void>()
  let state = HubConnectionState.Disconnected
  let onReconnecting: (() => void) | undefined
  let onReconnected: (() => void) | undefined

  const connection = {
    get state() {
      return state
    },
    on: vi.fn((event: string, handler: (...args: unknown[]) => void) => {
      handlers.set(event, handler)
    }),
    onreconnecting: vi.fn((handler: () => void) => {
      onReconnecting = handler
    }),
    onreconnected: vi.fn((handler: () => void) => {
      onReconnected = handler
    }),
    start: vi.fn(async () => {
      state = HubConnectionState.Connected
    }),
    stop: vi.fn(async () => {
      state = HubConnectionState.Disconnected
    }),
    emit(event: string, payload: unknown) {
      handlers.get(event)?.(payload)
    },
    triggerReconnecting() {
      onReconnecting?.()
    },
    triggerReconnected() {
      onReconnected?.()
    },
  }

  return connection as typeof connection & HubConnection
}

describe("connectJwtSignalRSession", () => {
  it("starts the connection, binds events, and stops cleanly", async () => {
    const connection = createFakeConnection()
    const onEvent = vi.fn()
    const onReconnected = vi.fn()

    const session = await connectJwtSignalRSession({
      hubUrl: "https://api.example.com/hubs/notifications",
      events: {
        NotificationCreated: onEvent,
      },
      onReconnected,
      getAccessToken: () => "token",
      buildConnection: () => connection,
    })

    expect(connection.start).toHaveBeenCalledOnce()
    connection.emit("NotificationCreated", { id: "n1" })
    expect(onEvent).toHaveBeenCalledWith({ id: "n1" })

    connection.triggerReconnected()
    expect(onReconnected).toHaveBeenCalledOnce()

    await session.stop()
    expect(connection.stop).toHaveBeenCalledOnce()
  })

  it("stops reconnecting when the access token is gone", async () => {
    const connection = createFakeConnection()
    let token: string | null = "token"

    await connectJwtSignalRSession({
      hubUrl: "https://api.example.com/hubs/notifications",
      events: {},
      onReconnected: () => {},
      getAccessToken: () => token,
      buildConnection: () => connection,
    })

    token = null
    connection.triggerReconnecting()
    expect(connection.stop).toHaveBeenCalledOnce()
  })
})
