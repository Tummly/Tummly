import type { HubConnection } from "@microsoft/signalr"

import { API_BASE_URL } from "@/config/api"
import type {
  OperatorNotification,
  OperatorNotificationsRealtimeHandlers,
  OperatorNotificationsRealtimeSession,
} from "@/lib/operatorNotifications/createOperatorNotificationsModule"
import { connectJwtSignalRSession } from "@/lib/signalr/connectJwtSignalRSession"
import { operatorHubUrl } from "@/lib/signalr/operatorHubUrl"

/** Backend event name from SignalRNotificationRealtimePublisher. */
export const NOTIFICATION_CREATED_EVENT = "NotificationCreated"

/**
 * Hub lives at /hubs/notifications on the API host (not under /api).
 */
export function notificationsHubUrl(apiBaseUrl: string = API_BASE_URL): string {
  return operatorHubUrl(apiBaseUrl, "/hubs/notifications")
}

export type CreateNotificationsHubConnectionOptions = {
  hubUrl?: string
  getAccessToken?: () => string | null
  buildConnection?: (hubUrl: string) => HubConnection
}

/**
 * Opens a JWT-authenticated Notifications hub for the Operator shell visit.
 * Session policy lives in connectJwtSignalRSession (ADR-0009).
 */
export async function connectNotificationsHub(
  handlers: OperatorNotificationsRealtimeHandlers,
  options: CreateNotificationsHubConnectionOptions = {}
): Promise<OperatorNotificationsRealtimeSession> {
  return connectJwtSignalRSession({
    hubUrl: options.hubUrl ?? notificationsHubUrl(),
    getAccessToken: options.getAccessToken,
    buildConnection: options.buildConnection,
    onReconnected: handlers.onReconnected,
    events: {
      [NOTIFICATION_CREATED_EVENT]: (payload: unknown) => {
        handlers.onNotificationCreated(payload as OperatorNotification)
      },
    },
  })
}
