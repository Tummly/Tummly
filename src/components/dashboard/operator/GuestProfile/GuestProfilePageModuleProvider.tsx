import { createElement, useState, type ReactNode } from "react"

import {
  correctFeedbackClassification,
  createGuestNote,
  exportGuestsCsv,
  getFeedbackDetails,
  getGuestActivity,
  getGuestFeedbacks,
  getGuestProfile,
  listGuestNotes,
  triggerBrowserDownload,
} from "@/api/dashboardApi"
import { GuestActivityTabModuleContextProvider } from "@/components/dashboard/operator/GuestProfile/utils/guestActivityTabModuleContext"
import { GuestFeedbacksTabModuleContextProvider } from "@/components/dashboard/operator/GuestProfile/utils/guestFeedbacksTabModuleContext"
import { guestProfilePageModuleContext } from "@/components/dashboard/operator/GuestProfile/utils/guestProfilePageModuleContext"
import { createGuestActivityTabModule } from "@/lib/operatorGuestProfile/createGuestActivityTabModule"
import { createGuestFeedbacksTabModule } from "@/lib/operatorGuestProfile/createGuestFeedbacksTabModule"
import { createOperatorGuestProfilePageModule } from "@/lib/operatorGuestProfile/createOperatorGuestProfilePageModule"

export function GuestProfilePageModuleProvider({
  children,
}: {
  children: ReactNode
}) {
  const [pageModule] = useState(() =>
    createOperatorGuestProfilePageModule({
      getGuestProfile: async (params) => getGuestProfile(params),
      listGuestNotes: async (params) => listGuestNotes(params),
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
    })
  )

  const [feedbacksModule] = useState(() =>
    createGuestFeedbacksTabModule({
      getGuestFeedbacks,
    })
  )

  const [activityModule] = useState(() =>
    createGuestActivityTabModule({
      getGuestActivity,
    })
  )

  return createElement(
    guestProfilePageModuleContext.Provider,
    { value: pageModule },
    createElement(
      GuestFeedbacksTabModuleContextProvider,
      { value: feedbacksModule },
      createElement(
        GuestActivityTabModuleContextProvider,
        { value: activityModule },
        children
      )
    )
  )
}
