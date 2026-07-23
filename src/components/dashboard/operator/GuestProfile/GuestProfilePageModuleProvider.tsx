import { createElement, useState, type ReactNode } from "react"
import { Outlet, useOutletContext } from "react-router-dom"

import {
  correctFeedbackClassification,
  createGuestNote,
  deleteLocationGuest,
  exportGuestsCsv,
  getFeedbackDetails,
  getGuestActivity,
  getGuestFeedbacks,
  getGuestProfile,
  listGuestNotes,
  listGuestTags,
  patchGuestIdentity,
  syncGuestTags,
  triggerBrowserDownload,
} from "@/api/dashboardApi"
import type { DashboardOutletContext } from "@/components/dashboard/operator/Dashboard"
import { GuestActivityTabModuleContextProvider } from "@/components/dashboard/operator/GuestProfile/utils/guestActivityTabModuleContext"
import { GuestFeedbacksTabModuleContextProvider } from "@/components/dashboard/operator/GuestProfile/utils/guestFeedbacksTabModuleContext"
import { guestProfilePageModuleContext } from "@/components/dashboard/operator/GuestProfile/utils/guestProfilePageModuleContext"
import { createOperatorGuestProfilePageModule } from "@/lib/operatorGuestProfile/createOperatorGuestProfilePageModule"

/**
 * Guest-scoped layout provider for Profile + Edit routes.
 * One Operator Guest Profile page module lives for the Location Guest visit
 * and is destroyed when leaving guest routes back to the Guests list.
 *
 * Forwards Dashboard outlet context so nested Profile/Edit routes can read
 * `selectedLocationId` / `mode` (React Router does not inherit outlet context
 * across nested layout Outlets).
 */
export function GuestProfilePageModuleProvider({
  children,
}: {
  children?: ReactNode
}) {
  const dashboardContext = useOutletContext<DashboardOutletContext>()
  const [pageModule] = useState(() =>
    createOperatorGuestProfilePageModule({
      getGuestProfile: async (params) => getGuestProfile(params),
      listGuestNotes: async (params) => listGuestNotes(params),
      createGuestNote: async (params) => createGuestNote(params),
      patchGuestIdentity: async (params) => patchGuestIdentity(params),
      listGuestTags: async (params) => listGuestTags(params),
      syncGuestTags: async (params) => syncGuestTags(params),
      getGuestActivity,
      getGuestFeedbacks,
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
    guestProfilePageModuleContext.Provider,
    { value: pageModule },
    createElement(
      GuestFeedbacksTabModuleContextProvider,
      { value: pageModule.feedbacksTab },
      createElement(
        GuestActivityTabModuleContextProvider,
        { value: pageModule.activityTab },
        children ?? createElement(Outlet, { context: dashboardContext })
      )
    )
  )
}
