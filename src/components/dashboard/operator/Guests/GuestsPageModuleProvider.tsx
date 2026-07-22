import { createElement, useState, type ReactNode } from "react"

import { getGuests } from "@/api/dashboardApi"
import { guestsPageModuleContext } from "@/components/dashboard/operator/Guests/utils/guestsPageModuleContext"
import { createOperatorGuestsPageModule } from "@/lib/operatorGuests/createOperatorGuestsPageModule"

export function GuestsPageModuleProvider({
  children,
}: {
  children: ReactNode
}) {
  const [pageModule] = useState(() =>
    createOperatorGuestsPageModule({
      getGuests: async (params) => getGuests(params),
    })
  )

  return createElement(
    guestsPageModuleContext.Provider,
    { value: pageModule },
    children
  )
}
