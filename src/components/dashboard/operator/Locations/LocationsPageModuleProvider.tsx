import {
  createElement,
  useEffect,
  useState,
  type ReactNode,
} from "react"
import { useSearchParams } from "react-router-dom"

import { getLocationsList } from "@/api/dashboardApi"
import {
  activateOwnedLocation,
  createOwnedLocation,
  deleteOwnedLocationDraft,
  setOwnedLocationManager,
} from "@/api/locationsWriteApi"
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
        createDraft: async (input) => {
          await createOwnedLocation(input)
        },
        activateDraft: async (locationId) => {
          await activateOwnedLocation(Number.parseInt(locationId, 10))
        },
        deleteDraft: async (locationId) => {
          await deleteOwnedLocationDraft(Number.parseInt(locationId, 10))
        },
        setManager: async (locationId, managerUserId) => {
          await setOwnedLocationManager(
            Number.parseInt(locationId, 10),
            managerUserId
          )
        },
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
