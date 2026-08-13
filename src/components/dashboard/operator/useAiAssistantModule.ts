import { useEffect, useRef, useState, useSyncExternalStore } from "react"

import {
  applyAssistantScope,
  archiveAssistantConversation,
  deleteAssistantConversation,
  getAssistantConversation,
  listAssistantConversations,
  retryAssistantTurn,
  sendAssistantTurn,
  unarchiveAssistantConversation,
} from "@/api/assistantApi"
import {
  createOperatorAiAssistantModule,
  type OperatorAiAssistantAction,
  type OperatorAiAssistantAnalysisScope,
  type OperatorAiAssistantModule,
  type OperatorAiAssistantOwnedLocationOption,
  type OperatorAiAssistantSnapshot,
} from "@/lib/operatorAiAssistant/createOperatorAiAssistantModule"

export type OperatorAiAssistantDashboardContext = {
  mode: "single" | "multi"
  restaurantName: string
  selectedLocation: OperatorAiAssistantOwnedLocationOption | null
  locations: readonly OperatorAiAssistantOwnedLocationOption[]
  navigateAction: (input: {
    action: OperatorAiAssistantAction
    analysisScope: OperatorAiAssistantAnalysisScope
  }) => void
}

export type OperatorAiAssistantApi = {
  snapshot: OperatorAiAssistantSnapshot
  openDrawer: OperatorAiAssistantModule["openDrawer"]
  closeDrawer: OperatorAiAssistantModule["closeDrawer"]
  setOpen: OperatorAiAssistantModule["setOpen"]
  startNewChat: OperatorAiAssistantModule["startNewChat"]
  openRecent: OperatorAiAssistantModule["openRecent"]
  openArchive: OperatorAiAssistantModule["openArchive"]
  backToConversation: OperatorAiAssistantModule["backToConversation"]
  setSearchQuery: OperatorAiAssistantModule["setSearchQuery"]
  openConversation: OperatorAiAssistantModule["openConversation"]
  archiveConversation: OperatorAiAssistantModule["archiveConversation"]
  unarchiveConversation: OperatorAiAssistantModule["unarchiveConversation"]
  requestDelete: OperatorAiAssistantModule["requestDelete"]
  cancelDelete: OperatorAiAssistantModule["cancelDelete"]
  confirmDelete: OperatorAiAssistantModule["confirmDelete"]
  retryList: OperatorAiAssistantModule["retryList"]
  retryBody: OperatorAiAssistantModule["retryBody"]
  expandDrawer: OperatorAiAssistantModule["expandDrawer"]
  leaveExpand: OperatorAiAssistantModule["leaveExpand"]
  openChangeScope: OperatorAiAssistantModule["openChangeScope"]
  setChangeScopeDraftLocation: OperatorAiAssistantModule["setChangeScopeDraftLocation"]
  setChangeScopeDraftReportingPeriod: OperatorAiAssistantModule["setChangeScopeDraftReportingPeriod"]
  cancelChangeScope: OperatorAiAssistantModule["cancelChangeScope"]
  applyChangeScope: OperatorAiAssistantModule["applyChangeScope"]
  setComposerDraft: OperatorAiAssistantModule["setComposerDraft"]
  fillComposerFromChip: OperatorAiAssistantModule["fillComposerFromChip"]
  send: OperatorAiAssistantModule["send"]
  retry: OperatorAiAssistantModule["retry"]
  toggleHelpful: OperatorAiAssistantModule["toggleHelpful"]
  clickAction: OperatorAiAssistantModule["clickAction"]
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
    createOperatorAiAssistantModule({
      closePeerRightDrawers: () => {},
      isOnline: () =>
        typeof navigator === "undefined" ? true : navigator.onLine,
      sendTurn: sendAssistantTurn,
      retryTurn: (input) =>
        retryAssistantTurn(input.conversationId, input.signal),
      getConversation: getAssistantConversation,
      applyScope: applyAssistantScope,
      navigateAction: (input) => {
        contextRef.current.navigateAction(input)
      },
      listConversations: listAssistantConversations,
      archiveConversation: archiveAssistantConversation,
      unarchiveConversation: unarchiveAssistantConversation,
      deleteConversation: deleteAssistantConversation,
      nowMs: () => Date.now(),
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
    openArchive: assistant.openArchive,
    backToConversation: assistant.backToConversation,
    setSearchQuery: assistant.setSearchQuery,
    openConversation: assistant.openConversation,
    archiveConversation: assistant.archiveConversation,
    unarchiveConversation: assistant.unarchiveConversation,
    requestDelete: assistant.requestDelete,
    cancelDelete: assistant.cancelDelete,
    confirmDelete: assistant.confirmDelete,
    retryList: assistant.retryList,
    retryBody: assistant.retryBody,
    expandDrawer: assistant.expandDrawer,
    leaveExpand: assistant.leaveExpand,
    openChangeScope: assistant.openChangeScope,
    setChangeScopeDraftLocation: assistant.setChangeScopeDraftLocation,
    setChangeScopeDraftReportingPeriod: assistant.setChangeScopeDraftReportingPeriod,
    cancelChangeScope: assistant.cancelChangeScope,
    applyChangeScope: assistant.applyChangeScope,
    setComposerDraft: assistant.setComposerDraft,
    fillComposerFromChip: assistant.fillComposerFromChip,
    send: assistant.send,
    retry: assistant.retry,
    toggleHelpful: assistant.toggleHelpful,
    clickAction: assistant.clickAction,
  }
}
