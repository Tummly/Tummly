import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from "@microsoft/signalr"

import { getAuthToken } from "@/stores/authStore"
import { ensureFreshAccessToken } from "@/api/sessionRefresh"

export type JwtSignalRSession = {
  stop: () => Promise<void>
}

export type ConnectJwtSignalRSessionOptions = {
  hubUrl: string
  /** Event name → handler. Bound before start. */
  events: Record<string, (...args: unknown[]) => void>
  onReconnected: () => void
  getAccessToken?: () => string | null
  buildConnection?: (hubUrl: string) => HubConnection
}

/**
 * Shared JWT SignalR session for operator hubs (ADR-0009).
 * Domain connect*Hub adapters bind event names; this owns token, reconnect, stop.
 */
export async function connectJwtSignalRSession(
  options: ConnectJwtSignalRSessionOptions
): Promise<JwtSignalRSession> {
  const getAccessToken = options.getAccessToken ?? getAuthToken
  const connection =
    options.buildConnection?.(options.hubUrl)
    ?? new HubConnectionBuilder()
      .withUrl(options.hubUrl, {
        accessTokenFactory: async () => {
          if (!options.getAccessToken) {
            await ensureFreshAccessToken()
          }

          return getAccessToken() ?? ""
        },
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build()

  connection.onreconnecting(() => {
    if (!getAccessToken()?.trim()) {
      void connection.stop()
    }
  })

  connection.onreconnected(() => {
    options.onReconnected()
  })

  for (const [eventName, handler] of Object.entries(options.events)) {
    connection.on(eventName, handler)
  }

  await connection.start()

  return {
    stop: async () => {
      if (connection.state !== HubConnectionState.Disconnected) {
        await connection.stop()
      }
    },
  }
}
