import type { HubConnection } from "@microsoft/signalr"

import { API_BASE_URL } from "@/config/api"
import type { AssistantTurnProgressSignal } from "@/lib/operatorAiAssistant/createOperatorAiAssistantModule"
import {
  connectJwtSignalRSession,
  type JwtSignalRSession,
} from "@/lib/signalr/connectJwtSignalRSession"
import { operatorHubUrl } from "@/lib/signalr/operatorHubUrl"

export const TURN_PROGRESS_EVENT = "TurnProgress"

export function assistantHubUrl(apiBaseUrl: string = API_BASE_URL): string {
  return operatorHubUrl(apiBaseUrl, "/hubs/assistant")
}

export type AssistantRealtimeHandlers = {
  onTurnProgress: (signal: AssistantTurnProgressSignal) => void
}

export type ConnectAssistantHubOptions = {
  hubUrl?: string
  getAccessToken?: () => string | null
  buildConnection?: (hubUrl: string) => HubConnection
}

export async function connectAssistantHub(
  handlers: AssistantRealtimeHandlers,
  options: ConnectAssistantHubOptions = {}
): Promise<JwtSignalRSession> {
  return connectJwtSignalRSession({
    hubUrl: options.hubUrl ?? assistantHubUrl(),
    getAccessToken: options.getAccessToken,
    buildConnection: options.buildConnection,
    onReconnected: () => {},
    events: {
      [TURN_PROGRESS_EVENT]: (payload: unknown) => {
        const signal = payload as Omit<
          AssistantTurnProgressSignal,
          "conversationId"
        > & { conversationId: string | number }
        handlers.onTurnProgress({
          conversationId: String(signal.conversationId),
          step: signal.step,
        })
      },
    },
  })
}
