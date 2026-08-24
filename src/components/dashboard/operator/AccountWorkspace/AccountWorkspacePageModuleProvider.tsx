import {
  createElement,
  useEffect,
  useRef,
  useState,
  type ReactNode,
} from "react"
import { useOutletContext, useSearchParams, useNavigate } from "react-router-dom"
import { toast } from "sonner"

import {
  exportAccountWorkspaceGuestData,
  getAccountWorkspaceDetails,
  pauseAccountWorkspace,
  resumeAccountWorkspace,
  updateAccountWorkspaceBusinessDetails,
  updateAccountWorkspaceDetails,
  updateAccountWorkspaceKeyContacts,
  updateAccountWorkspaceWorkspaceDefaults,
} from "@/api/accountWorkspaceApi"
import {
  createAccountRequestQuery,
  getOpenAccountRequest,
} from "@/api/helpCentreApi"
import type { DashboardOutletContext } from "@/components/dashboard/operator/Dashboard"
import { accountWorkspacePageModuleContext } from "@/components/dashboard/operator/AccountWorkspace/utils/accountWorkspacePageModuleContext"
import { triggerBrowserDownload } from "@/lib/operatorHome/homeActions"
import {
  createOperatorAccountWorkspacePageModule,
  resolveAccountWorkspaceTabId,
} from "@/lib/operatorAccountWorkspace/createOperatorAccountWorkspacePageModule"

export function AccountWorkspacePageModuleProvider({
  children,
}: {
  children: ReactNode
}) {
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
  const initialTabId = searchParams.get("tab")
  const { applyRestaurantIdentity } =
    useOutletContext<DashboardOutletContext>()
  const applyIdentityRef = useRef(applyRestaurantIdentity)
  applyIdentityRef.current = applyRestaurantIdentity

  const [pageModule] = useState(() =>
    createOperatorAccountWorkspacePageModule(
      {
        getDetails: getAccountWorkspaceDetails,
        updateAccountDetails: updateAccountWorkspaceDetails,
        updateBusinessDetails: updateAccountWorkspaceBusinessDetails,
        updateKeyContacts: updateAccountWorkspaceKeyContacts,
        updateWorkspaceDefaults: updateAccountWorkspaceWorkspaceDefaults,
        pauseWorkspace: pauseAccountWorkspace,
        resumeWorkspace: resumeAccountWorkspace,
        exportGuestData: exportAccountWorkspaceGuestData,
        findOpenAccountRequest: getOpenAccountRequest,
        createAccountRequest: createAccountRequestQuery,
        triggerBrowserDownload,
        onIdentityPersisted: (details) => {
          applyIdentityRef.current({
            restaurantName: details.workspaceName,
            brandLogoPublicUrl: details.brandLogoPublicUrl,
          })
        },
      },
      { initialTabId }
    )
  )

  useEffect(() => {
    void pageModule.load()
  }, [pageModule])

  useEffect(() => {
    return pageModule.subscribe(() => {
      const snap = pageModule.getSnapshot()
      if (snap.toast == null) {
        return
      }
      const toastOptions = snap.toast.action
        ? {
            action: {
              label: snap.toast.action.label,
              onClick: () => {
                navigate(snap.toast!.action!.href)
              },
            },
          }
        : undefined

      if (snap.toast.kind === "success") {
        toast.success(snap.toast.message, toastOptions)
      } else {
        toast.error(snap.toast.message, toastOptions)
      }
      pageModule.clearToast()
    })
  }, [pageModule, navigate])

  useEffect(() => {
    const tab = resolveAccountWorkspaceTabId(searchParams.get("tab"))
    pageModule.setActiveTabFromUrl(tab)
  }, [pageModule, searchParams])

  return createElement(
    accountWorkspacePageModuleContext.Provider,
    { value: pageModule },
    children
  )
}
