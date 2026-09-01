import {
  createElement,
  useEffect,
  useRef,
  useState,
  type ReactNode,
} from "react"
import { useLocation, useNavigate, useOutletContext, useSearchParams } from "react-router-dom"
import { toast } from "sonner"

import {
  addExtraGroupLocation,
  cancelBillingPlan,
  confirmBillingCreditTopUp,
  createPaymentMethodUpdateSession,
  downloadInvoicePdfBlob,
  fetchBillingCreditsInvoicePdf,
  getBillingCreditsPage,
  getBillingCreditsUsage,
  getBillingCreditsActivity,
  openInvoicePdfBlob,
  payBillingCreditTopUp,
  removeExtraGroupLocation,
  submitBillingPlanChange,
  updateBillingContacts,
} from "@/api/billingCreditsApi"
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
  const navigate = useNavigate()
  const { mode, selectedLocationId } =
    useOutletContext<DashboardOutletContext>()
  const initialTabId = searchParams.get("tab")
  const initialSection = searchParams.get("section")
  const handledTopUpReturnRef = useRef<string | null>(null)
  const [pageModule] = useState(() =>
    createOperatorBillingCreditsPageModule(
      {
        getPage: getBillingCreditsPage,
        getUsage: getBillingCreditsUsage,
        getBillingActivity: getBillingCreditsActivity,
        submitPlanChange: submitBillingPlanChange,
        addExtraGroupLocation,
        removeExtraGroupLocation,
        cancelPlan: cancelBillingPlan,
        createPaymentMethodUpdateSession,
        fetchInvoicePdf: fetchBillingCreditsInvoicePdf,
        openInvoicePdf: openInvoicePdfBlob,
        downloadInvoicePdf: downloadInvoicePdfBlob,
        updateBillingContacts,
        confirmCreditTopUp: confirmBillingCreditTopUp,
        payCreditTopUp: payBillingCreditTopUp,
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

  useEffect(() => {
    pageModule.setSurface(surface)
  }, [pageModule, surface])

  useEffect(() => {
    pageModule.setManagePlanSectionFromUrl(searchParams.get("section"))
  }, [pageModule, searchParams])

  useEffect(() => {
    pageModule.setFocusedTopUpChannelFromUrl(searchParams.get("channel"))
  }, [pageModule, searchParams])

  useEffect(() => {
    const outcome = searchParams.get("topUpOutcome")
    if (outcome == null) {
      handledTopUpReturnRef.current = null
      return
    }
    if (
      outcome !== "success"
      && outcome !== "cancel"
      && outcome !== "fail"
    ) {
      return
    }

    const returnKey = `${location.pathname}?${searchParams.toString()}`
    if (handledTopUpReturnRef.current === returnKey) {
      return
    }
    handledTopUpReturnRef.current = returnKey

    pageModule.handleTopUpPayReturn(outcome)
    void pageModule.load()

    const next = new URLSearchParams(searchParams)
    next.delete("topUpOutcome")
    navigate(
      {
        pathname: location.pathname,
        search: next.toString() === "" ? "" : `?${next.toString()}`,
      },
      { replace: true }
    )
  }, [pageModule, searchParams, location.pathname, navigate])

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
