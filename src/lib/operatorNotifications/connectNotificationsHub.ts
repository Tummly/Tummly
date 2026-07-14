import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from "@microsoft/signalr"

import { API_BASE_URL } from "@/config/api"
import type {
  OperatorNotification,
  OperatorNotificationsRealtimeHandlers,
  OperatorNotificationsRealtimeSession,
} from "@/lib/operatorNotifications/createOperatorNotificationsModule"
import { getAuthToken } from "@/stores/authStore"

/** Backend event name from SignalRNotificationRealtimePublisher. */
export const NOTIFICATION_CREATED_EVENT = "NotificationCreated"

/**
 * Hub lives at /hubs/notifications on the API host (not under /api).
 */
export function notificationsHubUrl(apiBaseUrl: string = API_BASE_URL): string {
  const root = apiBaseUrl.replace(/\/api\/?$/, "")
  return `${root}/hubs/notifications`
}

export type CreateNotificationsHubConnectionOptions = {
  hubUrl?: string
  getAccessToken?: () => string | null
  buildConnection?: (hubUrl: string) => HubConnection
}

/**
 * Opens a JWT-authenticated Notifications hub for the Operator shell visit.
 * Reconnect triggers REST catch-up via handlers.onReconnected.
 * Server CloseOnAuthenticationExpiration ends the connection when the JWT expires.
 */
export async function connectNotificationsHub(
  handlers: OperatorNotificationsRealtimeHandlers,
  options: CreateNotificationsHubConnectionOptions = {}
): Promise<OperatorNotificationsRealtimeSession> {
  const hubUrl = options.hubUrl ?? notificationsHubUrl()
  const getAccessToken = options.getAccessToken ?? getAuthToken
  const connection =
    options.buildConnection?.(hubUrl)
    ?? new HubConnectionBuilder()
      .withUrl(hubUrl, {
        accessTokenFactory: () => getAccessToken() ?? "",
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build()

  // Prefer not to leave a zombie after auth expiry: skip reconnect when token gone.
  connection.onreconnecting(() => {
    if (!getAccessToken()?.trim()) {
      void connection.stop()
    }
  })

  connection.onreconnected(() => {
    handlers.onReconnected()
  })

  connection.on(NOTIFICATION_CREATED_EVENT, (payload: OperatorNotification) => {
    handlers.onNotificationCreated(payload)
  })

  await connection.start()

  return {
    stop: async () => {
      if (connection.state !== HubConnectionState.Disconnected) {
        await connection.stop()
      }
    },
  }
}
