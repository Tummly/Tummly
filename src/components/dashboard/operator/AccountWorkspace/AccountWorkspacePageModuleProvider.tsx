import { createElement, useEffect, useState, type ReactNode } from "react"
import { useSearchParams } from "react-router-dom"
import { toast } from "sonner"

import {
  getAccountWorkspaceDetails,
  updateAccountWorkspaceDetails,
} from "@/api/accountWorkspaceApi"
import { accountWorkspacePageModuleContext } from "@/components/dashboard/operator/AccountWorkspace/utils/accountWorkspacePageModuleContext"
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
  const initialTabId = searchParams.get("tab")
  const [pageModule] = useState(() =>
    createOperatorAccountWorkspacePageModule(
      {
        getDetails: getAccountWorkspaceDetails,
        updateAccountDetails: updateAccountWorkspaceDetails,
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
      if (snap.toast.kind === "success") {
        toast.success(snap.toast.message)
      } else {
        toast.error(snap.toast.message)
      }
      pageModule.clearToast()
    })
  }, [pageModule])

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
