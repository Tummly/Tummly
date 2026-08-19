import { Navigate, Outlet, useLocation } from "react-router-dom"

import { AuthSessionLoading } from "@/components/auth/AuthSessionLoading"
import {
  type OperatorDashboardMode,
  resolveMismatchedOperatorDashboardRedirect,
} from "@/lib/operatorHome/operatorDashboardPaths"
import { useAuthStore } from "@/stores/authStore"

type OperatorDashboardRouteProps = {
  mode: OperatorDashboardMode
}

/**
 * Keeps operators on the dashboard that matches their AccountType.
 * Stops Multi users from opening /single-dashboard (and the reverse) by URL edit.
 */
const OperatorDashboardRoute = ({ mode }: OperatorDashboardRouteProps) => {
  const { pathname, search } = useLocation()
  const accountType = useAuthStore((state) => state.accountType)
  const hasHydrated = useAuthStore((state) => state._hasHydrated)

  if (!hasHydrated) {
    return <AuthSessionLoading />
  }

  const redirectTo = resolveMismatchedOperatorDashboardRedirect({
    mode,
    accountType,
    pathname,
    search,
  })

  if (redirectTo != null) {
    return <Navigate to={redirectTo} replace />
  }

  return <Outlet />
}

export default OperatorDashboardRoute
