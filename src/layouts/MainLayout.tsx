import { Outlet, useLocation } from "react-router-dom"

import MarketingHeader from "@/components/marketing/MarketingHeader"
import {
  marketingBodyChromePadding,
  marketingChromeBackground,
} from "@/lib/marketing-layout"
import { isOperatorDashboardPath } from "@/lib/operatorAppearance"
import { cn } from "@/lib/utils"

function MainLayout() {
  const { pathname } = useLocation()
  const isOperatorDashboard = isOperatorDashboardPath(pathname)

  return (
    <div
      className={
        isOperatorDashboard
          ? "flex h-dvh flex-col overflow-hidden"
          : cn("flex min-h-dvh flex-col", marketingChromeBackground)
      }
    >
      {isOperatorDashboard ? null : <MarketingHeader />}
      <div
        className={
          isOperatorDashboard
            ? "flex min-h-0 flex-1 flex-col overflow-hidden"
            : cn("flex flex-1 flex-col", marketingBodyChromePadding)
        }
      >
        <Outlet />
      </div>
    </div>
  )
}

export default MainLayout
