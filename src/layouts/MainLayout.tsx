import { Outlet, useLocation } from "react-router-dom"

import MarketingHeader from "@/components/marketing/MarketingHeader"
import {
  marketingBodyChromePadding,
  marketingChromeBackground,
  marketingShellMaxWidthClass,
} from "@/lib/marketing-layout"
import {
  isOperatorDashboardPath,
  isStaffDashboardPath,
} from "@/lib/operatorAppearance"
import { isProductDemoPath } from "@/lib/productDemo/productDemoPaths"
import { cn } from "@/lib/utils"

function MainLayout() {
  const { pathname } = useLocation()
  const isOperatorDashboard = isOperatorDashboardPath(pathname)
  const isStaffDashboard = isStaffDashboardPath(pathname)
  const isDemoWalkthrough = isProductDemoPath(pathname)

  if (isOperatorDashboard) {
    return (
      <div className="flex h-dvh flex-col overflow-hidden">
        <div className="flex min-h-0 flex-1 flex-col overflow-hidden">
          <Outlet />
        </div>
      </div>
    )
  }

  if (isStaffDashboard) {
    return (
      <div className="flex min-h-dvh flex-col bg-background">
        <div className="flex min-h-0 flex-1 flex-col">
          <Outlet />
        </div>
      </div>
    )
  }

  // Full-bleed product demo — no marketing header chrome.
  if (isDemoWalkthrough) {
    return (
      <div className="flex h-dvh flex-col overflow-hidden bg-[#f3f3f3]">
        <Outlet />
      </div>
    )
  }

  return (
    <div className={cn("flex min-h-dvh flex-col", marketingChromeBackground)}>
      <div
        className={cn(
          "flex min-h-dvh w-full flex-1 flex-col",
          marketingShellMaxWidthClass,
        )}
      >
        <MarketingHeader />
        <div
          className={cn("flex flex-1 flex-col", marketingBodyChromePadding)}
        >
          <Outlet />
        </div>
      </div>
    </div>
  )
}

export default MainLayout
