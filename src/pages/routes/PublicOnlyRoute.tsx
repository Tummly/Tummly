import { Navigate, Outlet } from "react-router-dom"

import { AuthSessionLoading } from "@/components/auth/AuthSessionLoading"
import { useAuthStore } from "@/stores/authStore"

/** Blocks signed-in admins from marketing / public pages. */
const PublicOnlyRoute = () => {
  const token = useAuthStore((state) => state.token)
  const role = useAuthStore((state) => state.role)
  const hasHydrated = useAuthStore((state) => state._hasHydrated)

  if (!hasHydrated) {
    return <AuthSessionLoading />
  }

  if (token && role === "ADMIN") {
    return <Navigate to="/admin-dashboard" replace />
  }

  return <Outlet />
}

export default PublicOnlyRoute
