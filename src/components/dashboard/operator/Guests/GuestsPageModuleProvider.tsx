import { createElement, useState, type ReactNode } from "react"

import {
  applyGuestTags,
  closeOutFeedback,
  correctFeedbackClassification,
  createFeedbackInternalNote,
  createGuestNote,
  createGuestTag,
  exportGuestsCsv,
  getGuestProfile,
  getGuestTagMemberships,
  getGuests,
  listGuestTags,
  softDeleteFeedbackInternalNote,
  triggerBrowserDownload,
  updateFeedbackInternalNote,
} from "@/api/dashboardApi"
import { guestsPageModuleContext } from "@/components/dashboard/operator/Guests/utils/guestsPageModuleContext"
import { useDashboardUiStoreApi } from "@/components/dashboard/operator/DashboardUiStoreProvider"
import { createOperatorGuestsPageModule } from "@/lib/operatorGuests/createOperatorGuestsPageModule"
import { createRecoveryWizardApiAdapters } from "@/lib/operatorFeedback/createRecoveryWizardApiAdapters"

export function GuestsPageModuleProvider({
  children,
}: {
  children: ReactNode
}) {
  const dashboardUiStore = useDashboardUiStoreApi()
  const [pageModule] = useState(() =>
    createOperatorGuestsPageModule({
      getGuests: async (params) => getGuests(params),
      exportGuestsCsv: async (params) => exportGuestsCsv(params),
      listGuestTags: async (params) => listGuestTags(params),
      createGuestTag: async (params) => createGuestTag(params),
      applyGuestTags: async (params) => applyGuestTags(params),
      getGuestTagMemberships: async (params) => getGuestTagMemberships(params),
      getGuestProfile: async (params) => getGuestProfile(params),
      createGuestNote: async (params) => createGuestNote(params),
      ...createRecoveryWizardApiAdapters(),
      correctClassification: async (feedbackId, sentiment) => {
        const result = await correctFeedbackClassification(
          feedbackId,
          sentiment
        )
        return {
          classificationStatus: result.classificationStatus,
          sentiment: result.sentiment,
          detectedTags: result.detectedTags,
          activityEvent: result.activityEvent ?? null,
        }
      },
      createInternalNote: async (feedbackId, body) =>
        createFeedbackInternalNote({ feedbackId, body }),
      updateInternalNote: async (feedbackId, noteId, body) =>
        updateFeedbackInternalNote({ feedbackId, noteId, body }),
      deleteInternalNote: async (feedbackId, noteId) =>
        softDeleteFeedbackInternalNote({ feedbackId, noteId }),
      closeOutFeedback: async (feedbackId, body) => {
        const result = await closeOutFeedback(feedbackId, body)
        return {
          workflowStatus: result.workflowStatus,
          needsAttention: result.needsAttention,
          activityEvent: result.activityEvent,
          noteActivityEvent: result.noteActivityEvent ?? null,
          note: result.note ?? null,
        }
      },
      getGuestsOverviewDateRange: () =>
        dashboardUiStore.getState().guestsOverviewDateRange,
      triggerBrowserDownload,
    })
  )

  return createElement(
    guestsPageModuleContext.Provider,
    { value: pageModule },
    children
  )
}
