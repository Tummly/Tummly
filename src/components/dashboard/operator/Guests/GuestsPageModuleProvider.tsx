import { createElement, useEffect, useState, type ReactNode } from "react"

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
  updateFeedbackDetectedTags,
  updateFeedbackInternalNote,
} from "@/api/dashboardApi"
import { guestsPageModuleContext } from "@/components/dashboard/operator/Guests/utils/guestsPageModuleContext"
import { useDashboardUiStoreApi } from "@/components/dashboard/operator/DashboardUiStoreProvider"
import { emptySelection } from "@/lib/operatorFilterSheet"
import { guestsFilterSheetSchema } from "@/lib/operatorGuests/guestsFilterSheetSchema"
import { createOperatorGuestsPageModule } from "@/lib/operatorGuests/createOperatorGuestsPageModule"
import { createRecoveryWizardApiAdapters } from "@/lib/operatorFeedback/createRecoveryWizardApiAdapters"
import type { OperatorGuestSmartGroupId } from "@/types/operatorGuests"

const GUESTS_SCHEMA = guestsFilterSheetSchema()

const LIVE_SMART_GROUPS = new Set<OperatorGuestSmartGroupId>([
  "all-guests",
  "new-guests",
  "needs-recovery",
  "positive-feedback",
  "dormant-guests",
])

function isLiveSmartGroup(
  value: string | undefined
): value is OperatorGuestSmartGroupId {
  return (
    value != null
    && LIVE_SMART_GROUPS.has(value as OperatorGuestSmartGroupId)
  )
}

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
      correctClassification: async (feedbackId, input) => {
        const trimmedNote = input.noteBody?.trim() ?? ""
        const result = await correctFeedbackClassification(feedbackId, {
          sentiment: input.sentiment,
          reason: input.reason,
          ...(trimmedNote.length > 0 || input.reason === "other"
            ? { note: trimmedNote }
            : {}),
        })
        return {
          classificationStatus: result.classificationStatus,
          sentiment: result.sentiment,
          detectedTags: result.detectedTags,
          activityEvent: result.activityEvent ?? null,
        }
      },
      updateDetectedTags: async (feedbackId, input) => {
        const result = await updateFeedbackDetectedTags(feedbackId, {
          detectedTags: input.detectedTags,
          ...(input.sentiment != null ? { sentiment: input.sentiment } : {}),
        })
        return {
          classificationStatus: result.classificationStatus,
          sentiment: result.sentiment,
          detectedTags: result.detectedTags,
          needsAttention: result.needsAttention,
          classifiedAt: result.classifiedAt ?? null,
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

  useEffect(() => {
    return dashboardUiStore.subscribe((state) => {
      const intent = state.guestsIntent
      if (intent == null) {
        return
      }

      if (isLiveSmartGroup(intent.smartGroup)) {
        pageModule.setActiveSmartGroupId(intent.smartGroup)
      }
      if (intent.marketingEligible === true) {
        pageModule.applyFilters({
          ...emptySelection(GUESTS_SCHEMA),
          marketing: { kind: "multi-select", ids: ["eligible"] },
        })
      }
      state.setGuestsIntent(null)
    })
  }, [dashboardUiStore, pageModule])

  return createElement(
    guestsPageModuleContext.Provider,
    { value: pageModule },
    children
  )
}
