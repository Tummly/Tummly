import {
  createElement,
  useEffect,
  useRef,
  useState,
  type ReactNode,
} from "react"
import { useNavigate, useOutletContext, useSearchParams } from "react-router-dom"
import { toast } from "sonner"

import {
  getPermissionRecords,
  getPrivacyConsent,
  getPrivacyConsentActivity,
  patchPrivacyConsentToggles,
  savePrivacyConsent,
} from "@/api/privacyConsentApi"
import type { DashboardOutletContext } from "@/components/dashboard/operator/Dashboard"
import { privacyConsentPageModuleContext } from "@/components/dashboard/operator/PrivacyConsent/utils/privacyConsentPageModuleContext"
import { operatorDashboardGuestProfilePath } from "@/lib/operatorHome/operatorDashboardPaths"
import { createOperatorPrivacyConsentPageModule } from "@/lib/operatorPrivacyConsent/createOperatorPrivacyConsentPageModule"

export function PrivacyConsentPageModuleProvider({
  children,
}: {
  children: ReactNode
}) {
  const navigate = useNavigate()
  const { mode, locations } = useOutletContext<DashboardOutletContext>()
  const locationsRef = useRef(locations)
  locationsRef.current = locations
  const [searchParams] = useSearchParams()
  const initialTabId = searchParams.get("tab")
  const [pageModule] = useState(() =>
    createOperatorPrivacyConsentPageModule({
      getPage: getPrivacyConsent,
      patchToggles: patchPrivacyConsentToggles,
      saveWording: savePrivacyConsent,
      getPermissionRecords,
      getActivity: getPrivacyConsentActivity,
      getLocationFilterOptions: () =>
        locationsRef.current.map((location) => ({
          id: String(location.id),
          label: location.locationName,
        })),
      navigateToGuestProfile: (locationGuestId, locationId) => {
        navigate(
          operatorDashboardGuestProfilePath(mode, locationGuestId, locationId)
        )
      },
      debounceMs: 300,
    }, { initialTabId })
  )

  useEffect(() => {
    pageModule.syncLocationFilterOptions()
  }, [pageModule, locations])

  useEffect(() => {
    void pageModule.load()
  }, [pageModule])

  useEffect(() => {
    return pageModule.subscribe(() => {
      const snap = pageModule.getSnapshot()
      if (snap.toast == null) {
        return
      }
      if (snap.toast.kind === "success") {
        toast.success(snap.toast.message)
      } else {
        toast.error(snap.toast.message)
      }
      pageModule.clearToast()
    })
  }, [pageModule])

  useEffect(() => {
    pageModule.setActiveTabFromUrl(searchParams.get("tab"))
  }, [pageModule, searchParams])

  return createElement(
    privacyConsentPageModuleContext.Provider,
    { value: pageModule },
    children
  )
}
