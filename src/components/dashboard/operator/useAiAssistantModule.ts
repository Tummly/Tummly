import { useState, useSyncExternalStore } from "react"

import {
  createInMemoryOperatorAiAssistantAdapters,
  createOperatorAiAssistantModule,
  type OperatorAiAssistantModule,
  type OperatorAiAssistantSnapshot,
} from "@/lib/operatorAiAssistant/createOperatorAiAssistantModule"

export type OperatorAiAssistantApi = {
  snapshot: OperatorAiAssistantSnapshot
  openDrawer: OperatorAiAssistantModule["openDrawer"]
  closeDrawer: OperatorAiAssistantModule["closeDrawer"]
  setOpen: OperatorAiAssistantModule["setOpen"]
  startNewChat: OperatorAiAssistantModule["startNewChat"]
  openRecent: OperatorAiAssistantModule["openRecent"]
}

export function useAiAssistantModule(): OperatorAiAssistantApi {
  const [assistant] = useState(() =>
    createOperatorAiAssistantModule(createInMemoryOperatorAiAssistantAdapters())
  )

  const snapshot = useSyncExternalStore(
    assistant.subscribe,
    assistant.getSnapshot,
    assistant.getSnapshot
  )

  return {
    snapshot,
    openDrawer: assistant.openDrawer,
    closeDrawer: assistant.closeDrawer,
    setOpen: assistant.setOpen,
    startNewChat: assistant.startNewChat,
    openRecent: assistant.openRecent,
  }
}
