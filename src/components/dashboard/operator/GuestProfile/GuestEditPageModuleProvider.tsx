import { createElement, useState, type ReactNode } from "react"

import {
  correctFeedbackClassification,
  createGuestNote,
  deleteLocationGuest,
  exportGuestsCsv,
  getFeedbackDetails,
  getGuestProfile,
  listGuestTags,
  patchGuestIdentity,
  syncGuestTags,
  triggerBrowserDownload,
} from "@/api/dashboardApi"
import { guestEditPageModuleContext } from "@/components/dashboard/operator/GuestProfile/utils/guestEditPageModuleContext"
import { createOperatorGuestEditPageModule } from "@/lib/operatorGuestProfile/createOperatorGuestEditPageModule"

export function GuestEditPageModuleProvider({
  children,
}: {
  children: ReactNode
}) {
  const [pageModule] = useState(() =>
    createOperatorGuestEditPageModule({
      getGuestProfile: async (params) => getGuestProfile(params),
      patchGuestIdentity: async (params) => patchGuestIdentity(params),
      listGuestTags: async (params) => listGuestTags(params),
      syncGuestTags: async (params) => syncGuestTags(params),
      createGuestNote: async (params) => createGuestNote(params),
      getFeedbackDetails,
      correctClassification: async (feedbackId, sentiment) => {
        const result = await correctFeedbackClassification(
          feedbackId,
          sentiment
        )
        return {
          classificationStatus: result.classificationStatus,
          sentiment: result.sentiment,
          detectedTags: result.detectedTags,
        }
      },
      exportGuestsCsv: async (params) => exportGuestsCsv(params),
      triggerBrowserDownload,
      deleteLocationGuest: async (params) => deleteLocationGuest(params),
    })
  )

  return createElement(
    guestEditPageModuleContext.Provider,
    { value: pageModule },
    children
  )
}
