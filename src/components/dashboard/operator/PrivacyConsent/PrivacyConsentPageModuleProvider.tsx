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
import { useWorkspaceSession } from "@/components/dashboard/operator/useWorkspaceSession"
import { operatorDashboardGuestProfilePath } from "@/lib/operatorHome/operatorDashboardPaths"
import { createOperatorPrivacyConsentPageModule } from "@/lib/operatorPrivacyConsent/createOperatorPrivacyConsentPageModule"

export function PrivacyConsentPageModuleProvider({
  children,
}: {
  children: ReactNode
}) {
  const navigate = useNavigate()
  const { mode } = useOutletContext<DashboardOutletContext>()
  const workspace = useWorkspaceSession(mode)
  const workspaceRef = useRef(workspace)
  workspaceRef.current = workspace
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
        workspaceRef.current.snapshot.locations.map((location) => ({
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
    if (workspace.snapshot.status !== "loaded") {
      return
    }
    pageModule.syncLocationFilterOptions()
  }, [pageModule, workspace.snapshot.status, workspace.snapshot.locations])

  useEffect(() => {
    if (workspace.snapshot.status !== "loaded") {
      return
    }
    void pageModule.load()
  }, [pageModule, workspace.snapshot.status])

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
