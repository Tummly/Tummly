import type { HubConnection } from "@microsoft/signalr"

import { API_BASE_URL } from "@/config/api"
import type {
  ClassificationTerminalSignal,
  FeedbackHomeRealtimeHandlers,
  FeedbackHomeRealtimeSession,
} from "@/lib/operatorHome/createOperatorHomePageModule"
import { connectJwtSignalRSession } from "@/lib/signalr/connectJwtSignalRSession"
import { operatorHubUrl } from "@/lib/signalr/operatorHubUrl"

/** Backend event name from SignalRFeedbackHomeRealtimePublisher. */
export const CLASSIFICATION_TERMINAL_EVENT = "ClassificationTerminal"

/**
 * Hub lives at /hubs/feedback-home on the API host (not under /api).
 */
export function feedbackHomeHubUrl(apiBaseUrl: string = API_BASE_URL): string {
  return operatorHubUrl(apiBaseUrl, "/hubs/feedback-home")
}

export type CreateFeedbackHomeHubConnectionOptions = {
  hubUrl?: string
  getAccessToken?: () => string | null
  buildConnection?: (hubUrl: string) => HubConnection
}

/**
 * Opens a JWT-authenticated Feedback/Home hub while Operator Home is active.
 * Session policy lives in connectJwtSignalRSession (ADR-0009).
 */
export async function connectFeedbackHomeHub(
  handlers: FeedbackHomeRealtimeHandlers,
  options: CreateFeedbackHomeHubConnectionOptions = {}
): Promise<FeedbackHomeRealtimeSession> {
  return connectJwtSignalRSession({
    hubUrl: options.hubUrl ?? feedbackHomeHubUrl(),
    getAccessToken: options.getAccessToken,
    buildConnection: options.buildConnection,
    onReconnected: handlers.onReconnected,
    events: {
      [CLASSIFICATION_TERMINAL_EVENT]: (payload: unknown) => {
        handlers.onClassificationTerminal(
          payload as ClassificationTerminalSignal
        )
      },
    },
  })
}
