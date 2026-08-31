import {
  createElement,
  useEffect,
  useState,
  type ReactNode,
} from "react"
import { useSearchParams } from "react-router-dom"

import { getLocationsList } from "@/api/dashboardApi"
import { locationsPageModuleContext } from "@/components/dashboard/operator/Locations/utils/locationsPageModuleContext"
import { createOperatorLocationsPageModule } from "@/lib/operatorLocations/createOperatorLocationsPageModule"

export function LocationsPageModuleProvider({
  children,
}: {
  children: ReactNode
}) {
  const [searchParams] = useSearchParams()
  const initialTabId = searchParams.get("tab")
  const [pageModule] = useState(() =>
    createOperatorLocationsPageModule(
      {
        getList: getLocationsList,
      },
      { initialTabId }
    )
  )

  useEffect(() => {
    void pageModule.load()
  }, [pageModule])

  useEffect(() => {
    pageModule.setActiveTabFromUrl(searchParams.get("tab"))
  }, [pageModule, searchParams])

  return createElement(
    locationsPageModuleContext.Provider,
    { value: pageModule },
    children
  )
}
