import { Navigate, Outlet } from "react-router-dom"

import { AuthSessionLoading } from "@/components/auth/AuthSessionLoading"
import { useAuthStore } from "@/stores/authStore"
import { getPostLoginDestination } from "@/pages/utils/authHelpers"
import { getSelectedLocationId } from "@/pages/utils/authHelpers"

/** Blocks signed-in users from marketing / public pages. */
const PublicOnlyRoute = () => {
  const token = useAuthStore((state) => state.token)
  const role = useAuthStore((state) => state.role)
  const accountType = useAuthStore((state) => state.accountType)
  const hasHydrated = useAuthStore((state) => state._hasHydrated)

  if (!hasHydrated) {
    return <AuthSessionLoading />
  }

  if (token && role === "ADMIN") {
    return <Navigate to="/admin-dashboard" replace />
  }

  if (token && role === "USER" && accountType) {
    const destination = getPostLoginDestination(
      accountType,
      false,
      getSelectedLocationId()
    )
    return <Navigate to={destination} replace />
  }

  return <Outlet />
}

export default PublicOnlyRoute
