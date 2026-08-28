import {
  createElement,
  useEffect,
  useState,
  type ReactNode,
} from "react"
import { useLocation, useOutletContext, useSearchParams } from "react-router-dom"

import { getBillingCreditsPage, getBillingCreditsUsage } from "@/api/billingCreditsApi"
import { billingCreditsPageModuleContext } from "@/components/dashboard/operator/BillingCredits/utils/billingCreditsPageModuleContext"
import type { DashboardOutletContext } from "@/components/dashboard/operator/Dashboard"
import {
  createOperatorBillingCreditsPageModule,
  resolveBillingCreditsTabId,
} from "@/lib/operatorBillingCredits/createOperatorBillingCreditsPageModule"
import { operatorDashboardRootPath } from "@/lib/operatorHome/operatorDashboardPaths"

export function BillingCreditsPageModuleProvider({
  children,
  surface = "tabs",
}: {
  children: ReactNode
  surface?: "tabs" | "manage-plan"
}) {
  const [searchParams] = useSearchParams()
  const location = useLocation()
  const { mode, selectedLocationId } =
    useOutletContext<DashboardOutletContext>()
  const initialTabId = searchParams.get("tab")
  const initialSection = searchParams.get("section")
  const [pageModule] = useState(() =>
    createOperatorBillingCreditsPageModule(
      {
        getPage: getBillingCreditsPage,
        getUsage: getBillingCreditsUsage,
      },
      {
        initialTabId,
        initialSurface: surface,
        initialManagePlanSection: initialSection,
      }
    )
  )

  useEffect(() => {
    pageModule.setNavigationTargets({
      mode,
      locationId: selectedLocationId,
    })
  }, [pageModule, mode, selectedLocationId])

  useEffect(() => {
    void pageModule.load()
  }, [pageModule])

  useEffect(() => {
    pageModule.setActiveTabFromUrl(searchParams.get("tab"))
  }, [pageModule, searchParams])

  useEffect(() => {
    pageModule.setSurface(surface)
  }, [pageModule, surface])

  useEffect(() => {
    pageModule.setManagePlanSectionFromUrl(searchParams.get("section"))
  }, [pageModule, searchParams])

  useEffect(() => {
    const snap = pageModule.getSnapshot()
    if (snap.loadStatus === "forbidden") {
      const root = operatorDashboardRootPath(mode)
      window.location.replace(`${root}?location=${selectedLocationId}`)
      return
    }
    if (
      surface === "manage-plan"
      && snap.loadStatus === "loaded"
      && snap.accessLevel === "view"
    ) {
      const root = operatorDashboardRootPath(mode)
      window.location.replace(
        `${root}/settings/billing-credits?location=${selectedLocationId}&tab=plan-subscription`
      )
    }
  }, [pageModule, mode, selectedLocationId, location.pathname, surface])

  return createElement(
    billingCreditsPageModuleContext.Provider,
    { value: pageModule },
    children
  )
}

export { resolveBillingCreditsTabId }
