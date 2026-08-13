import { useEffect, useRef, useState, useSyncExternalStore } from "react"

import {
  applyAssistantScope,
  archiveAssistantConversation,
  deleteAssistantConversation,
  getAssistantConversation,
  listAssistantConversations,
  retryAssistantTurn,
  sendAssistantTurn,
  transcribeOperatorAudio,
  unarchiveAssistantConversation,
} from "@/api/assistantApi"
import { createBrowserGuestMicAdapters } from "@/lib/guestFeedback/createBrowserGuestMicAdapters"
import type { GuestMicAudioLevelSource } from "@/lib/guestFeedback/guestMicAudioLevel"
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
  closePeerRightDrawers: () => void
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
  startMic: OperatorAiAssistantModule["startMic"]
  confirmMic: OperatorAiAssistantModule["confirmMic"]
  cancelMic: OperatorAiAssistantModule["cancelMic"]
  dismissMicError: OperatorAiAssistantModule["dismissMicError"]
  micAudioLevelSource: GuestMicAudioLevelSource
  retry: OperatorAiAssistantModule["retry"]
  toggleHelpful: OperatorAiAssistantModule["toggleHelpful"]
  clickAction: OperatorAiAssistantModule["clickAction"]
  dismissFromEscape: OperatorAiAssistantModule["dismissFromEscape"]
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
  const [assistantBundle] = useState(() => {
    const browserMic = createBrowserGuestMicAdapters({
      transcribe: transcribeOperatorAudio,
      replaceComment: () => {},
    })
    return {
      assistant: createOperatorAiAssistantModule({
        closePeerRightDrawers: () => {
          contextRef.current.closePeerRightDrawers()
        },
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
        mic: {
          startRecording: browserMic.adapters.startRecording,
          stopRecording: browserMic.adapters.stopRecording,
          cancelRecording: browserMic.adapters.cancelRecording,
          transcribe: browserMic.adapters.transcribe,
        },
      }),
      audioLevelSource: browserMic.audioLevelSource,
    }
  })
  const assistant = assistantBundle.assistant

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
    startMic: assistant.startMic,
    confirmMic: assistant.confirmMic,
    cancelMic: assistant.cancelMic,
    dismissMicError: assistant.dismissMicError,
    micAudioLevelSource: assistantBundle.audioLevelSource,
    retry: assistant.retry,
    toggleHelpful: assistant.toggleHelpful,
    clickAction: assistant.clickAction,
    dismissFromEscape: assistant.dismissFromEscape,
  }
}
