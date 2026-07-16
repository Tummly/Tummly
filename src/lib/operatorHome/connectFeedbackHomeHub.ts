import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from "@microsoft/signalr"

import { API_BASE_URL } from "@/config/api"
import type {
  ClassificationTerminalSignal,
  FeedbackHomeRealtimeHandlers,
  FeedbackHomeRealtimeSession,
} from "@/lib/operatorHome/createOperatorHomePageModule"
import { getAuthToken } from "@/stores/authStore"

/** Backend event name from SignalRFeedbackHomeRealtimePublisher. */
export const CLASSIFICATION_TERMINAL_EVENT = "ClassificationTerminal"

/**
 * Hub lives at /hubs/feedback-home on the API host (not under /api).
 */
export function feedbackHomeHubUrl(apiBaseUrl: string = API_BASE_URL): string {
  const root = apiBaseUrl.replace(/\/api\/?$/, "")
  return `${root}/hubs/feedback-home`
}

export type CreateFeedbackHomeHubConnectionOptions = {
  hubUrl?: string
  getAccessToken?: () => string | null
  buildConnection?: (hubUrl: string) => HubConnection
}

/**
 * Opens a JWT-authenticated Feedback/Home hub while Operator Home is active.
 * Reconnect triggers REST catch-up via handlers.onReconnected.
 * Server CloseOnAuthenticationExpiration ends the connection when the JWT expires.
 */
export async function connectFeedbackHomeHub(
  handlers: FeedbackHomeRealtimeHandlers,
  options: CreateFeedbackHomeHubConnectionOptions = {}
): Promise<FeedbackHomeRealtimeSession> {
  const hubUrl = options.hubUrl ?? feedbackHomeHubUrl()
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

  connection.onreconnecting(() => {
    if (!getAccessToken()?.trim()) {
      void connection.stop()
    }
  })

  connection.onreconnected(() => {
    handlers.onReconnected()
  })

  connection.on(
    CLASSIFICATION_TERMINAL_EVENT,
    (payload: ClassificationTerminalSignal) => {
      handlers.onClassificationTerminal(payload)
    }
  )

  await connection.start()

  return {
    stop: async () => {
      if (connection.state !== HubConnectionState.Disconnected) {
        await connection.stop()
      }
    },
  }
}
