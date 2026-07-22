import { createElement, useState, type ReactNode } from "react"

import {
  applyGuestTags,
  createGuestTag,
  exportGuestsCsv,
  getGuestTagMemberships,
  getGuests,
  listGuestTags,
  triggerBrowserDownload,
} from "@/api/dashboardApi"
import { guestsPageModuleContext } from "@/components/dashboard/operator/Guests/utils/guestsPageModuleContext"
import { useDashboardUiStoreApi } from "@/components/dashboard/operator/DashboardUiStoreProvider"
import { createOperatorGuestsPageModule } from "@/lib/operatorGuests/createOperatorGuestsPageModule"

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
