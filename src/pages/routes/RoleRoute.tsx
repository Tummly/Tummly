import type { ReactNode } from "react"
import { Navigate, Outlet } from "react-router-dom"

import { AuthSessionLoading } from "@/components/auth/AuthSessionLoading"
import { useAuthStore } from "@/stores/authStore"
import type { UserRole } from "../../types/auth"

interface RoleRouteProps {
  children?: ReactNode
  role?: UserRole
  /** When set, any matching role is allowed (takes precedence over `role`). */
  roles?: UserRole[]
  /** Where to send signed-in users with the wrong role. Default: `/login`. */
  unauthorizedTo?: string
}

const RoleRoute = ({
  children,
  role,
  roles,
  unauthorizedTo = "/login",
}: RoleRouteProps) => {
  const token = useAuthStore((state) => state.token)
  const userRole = useAuthStore((state) => state.role)
  const hasHydrated = useAuthStore((state) => state._hasHydrated)

  if (!hasHydrated) {
    return <AuthSessionLoading />
  }

  if (!token) {
    return <Navigate to="/login" replace />
  }

  if (roles && roles.length > 0) {
    if (!userRole || !roles.includes(userRole)) {
      return <Navigate to={unauthorizedTo} replace />
    }
  } else if (role && userRole !== role) {
    return <Navigate to={unauthorizedTo} replace />
  }

  return children ?? <Outlet />
}

export default RoleRoute
