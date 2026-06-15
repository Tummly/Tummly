import type { ReactNode } from "react"
import { Navigate } from "react-router-dom"

import { AuthSessionLoading } from "@/components/auth/AuthSessionLoading"
import { useAuthStore } from "@/stores/authStore"
import type { UserRole } from "../../types/auth"

interface RoleRouteProps {
  children: ReactNode
  role?: UserRole
}

const RoleRoute = ({ children, role }: RoleRouteProps) => {
  const token = useAuthStore((state) => state.token)
  const userRole = useAuthStore((state) => state.role)
  const hasHydrated = useAuthStore((state) => state._hasHydrated)

  if (!hasHydrated) {
    return <AuthSessionLoading />
  }

  if (!token) {
    return <Navigate to="/login" replace />
  }

  if (role && userRole !== role) {
    return <Navigate to="/login" replace />
  }

  return children
}

export default RoleRoute
