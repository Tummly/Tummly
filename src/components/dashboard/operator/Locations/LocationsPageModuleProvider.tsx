import {
  createElement,
  useEffect,
  useState,
  type ReactNode,
} from "react"
import { useSearchParams } from "react-router-dom"

import {
  getLocationsActivity,
  getLocationsList,
  mutateLocationLifecycle,
} from "@/api/dashboardApi"
import {
  activateOwnedLocation,
  createOwnedLocation,
  deleteOwnedLocationDraft,
  importOwnedLocations,
  setOwnedLocationManager,
} from "@/api/locationsWriteApi"
import { savePrivacyConsent } from "@/api/privacyConsentApi"
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
        getActivity: getLocationsActivity,
        createDraft: async (input) => {
          await createOwnedLocation(input)
        },
        importDrafts: async (rows) => {
          const result = await importOwnedLocations(rows)
          return {
            createdCount: result.created.length,
            errors: result.errors.map((row) => ({
              rowIndex: row.rowIndex,
              message: row.message,
            })),
          }
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
        mutateLifecycle: mutateLocationLifecycle,
        completePrivacyReview: async () => {
          await savePrivacyConsent()
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
