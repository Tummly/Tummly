import { useEffect, useRef, useState, useSyncExternalStore } from "react"

import {
  createInMemoryOperatorAiAssistantAdapters,
  createOperatorAiAssistantModule,
  type OperatorAiAssistantModule,
  type OperatorAiAssistantOwnedLocationOption,
  type OperatorAiAssistantSnapshot,
} from "@/lib/operatorAiAssistant/createOperatorAiAssistantModule"

export type OperatorAiAssistantDashboardContext = {
  mode: "single" | "multi"
  restaurantName: string
  selectedLocation: OperatorAiAssistantOwnedLocationOption | null
  locations: readonly OperatorAiAssistantOwnedLocationOption[]
}

export type OperatorAiAssistantApi = {
  snapshot: OperatorAiAssistantSnapshot
  openDrawer: OperatorAiAssistantModule["openDrawer"]
  closeDrawer: OperatorAiAssistantModule["closeDrawer"]
  setOpen: OperatorAiAssistantModule["setOpen"]
  startNewChat: OperatorAiAssistantModule["startNewChat"]
  openRecent: OperatorAiAssistantModule["openRecent"]
  expandDrawer: OperatorAiAssistantModule["expandDrawer"]
  leaveExpand: OperatorAiAssistantModule["leaveExpand"]
  openChangeScope: OperatorAiAssistantModule["openChangeScope"]
  setChangeScopeDraftLocation: OperatorAiAssistantModule["setChangeScopeDraftLocation"]
  setChangeScopeDraftReportingPeriod: OperatorAiAssistantModule["setChangeScopeDraftReportingPeriod"]
  cancelChangeScope: OperatorAiAssistantModule["cancelChangeScope"]
  applyChangeScope: OperatorAiAssistantModule["applyChangeScope"]
}

export function useAiAssistantModule(
  context: OperatorAiAssistantDashboardContext
): OperatorAiAssistantApi {
  const contextRef = useRef(context)
  useEffect(() => {
    contextRef.current = context
  })

  // Adapters read contextRef on user actions, not during module construction.
  // eslint-disable-next-line react-hooks/refs -- stable module; adapters close over the ref
  const [assistant] = useState(() =>
    createOperatorAiAssistantModule(
      createInMemoryOperatorAiAssistantAdapters({
        getDashboardOwnedLocation: () => {
          const current = contextRef.current
          return (
            current.selectedLocation ?? {
              id: 0,
              name: "",
            }
          )
        },
        getRestaurantName: () => contextRef.current.restaurantName,
        getDashboardMode: () => contextRef.current.mode,
        listOwnedLocations: () => contextRef.current.locations,
      })
    )
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
    expandDrawer: assistant.expandDrawer,
    leaveExpand: assistant.leaveExpand,
    openChangeScope: assistant.openChangeScope,
    setChangeScopeDraftLocation: assistant.setChangeScopeDraftLocation,
    setChangeScopeDraftReportingPeriod: assistant.setChangeScopeDraftReportingPeriod,
    cancelChangeScope: assistant.cancelChangeScope,
    applyChangeScope: assistant.applyChangeScope,
  }
}
