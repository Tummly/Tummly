import { createElement, useState, type ReactNode } from "react"

import { getGuestProfile } from "@/api/dashboardApi"
import { guestProfilePageModuleContext } from "@/components/dashboard/operator/GuestProfile/utils/guestProfilePageModuleContext"
import { createOperatorGuestProfilePageModule } from "@/lib/operatorGuestProfile/createOperatorGuestProfilePageModule"

export function GuestProfilePageModuleProvider({
  children,
}: {
  children: ReactNode
}) {
  const [pageModule] = useState(() =>
    createOperatorGuestProfilePageModule({
      getGuestProfile: async (params) => getGuestProfile(params),
    })
  )

  return createElement(
    guestProfilePageModuleContext.Provider,
    { value: pageModule },
    children
  )
}
