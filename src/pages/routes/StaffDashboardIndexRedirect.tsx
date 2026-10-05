import { Navigate } from "react-router-dom"

import { AuthSessionLoading } from "@/components/auth/AuthSessionLoading"
import {
  STAFF_DASHBOARD_ADMIN_URL,
  STAFF_DASHBOARD_SUPPORT_URL,
} from "@/config/staffDashboard"
import { useAuthStore } from "@/stores/authStore"

/** Default section after `/admin-dashboard` by staff role. */
export default function StaffDashboardIndexRedirect() {
  const role = useAuthStore((state) => state.role)
  const hasHydrated = useAuthStore((state) => state._hasHydrated)

  if (!hasHydrated) {
    return <AuthSessionLoading />
  }

  if (role === "SUPPORT") {
    return <Navigate to={STAFF_DASHBOARD_SUPPORT_URL} replace />
  }

  return <Navigate to={STAFF_DASHBOARD_ADMIN_URL} replace />
}
