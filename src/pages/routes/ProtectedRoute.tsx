import { Navigate, Outlet } from "react-router-dom"

import { AuthSessionLoading } from "@/components/auth/AuthSessionLoading"
import { useAuthStore } from "@/stores/authStore"

const ProtectedRoute = () => {
  const token = useAuthStore((state) => state.token)
  const hasHydrated = useAuthStore((state) => state._hasHydrated)

  if (!hasHydrated) {
    return <AuthSessionLoading />
  }

  if (!token) {
    return <Navigate to="/login" replace />
  }

  return <Outlet />
}

export default ProtectedRoute
