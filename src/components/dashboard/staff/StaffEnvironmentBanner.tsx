import { useEffect, useState } from "react"

import { getAdminEnvironment } from "@/api/adminApi"
import { environmentBannerLabel } from "@/lib/admin/environmentBanner"
import { clientEnvironmentName } from "@/lib/env"
import { useAuthStore } from "@/stores/authStore"
import type { AdminEnvironment } from "@/types/adminOps"

/** Shows the environment name on every non-production staff page. */
export function StaffEnvironmentBanner() {
  const role = useAuthStore((state) => state.role)
  const [loaded, setLoaded] = useState<AdminEnvironment | null>(null)

  useEffect(() => {
    if (role !== "ADMIN") {
      return
    }

    let active = true
    getAdminEnvironment()
      .then((environment) => {
        if (active) setLoaded(environment)
      })
      .catch(() => {
        if (active) setLoaded(null)
      })

    return () => {
      active = false
    }
  }, [role])

  // Only Admin may read the environment endpoint. Other staff use the build name.
  const apiEnvironment = role === "ADMIN" ? loaded : null
  const label = environmentBannerLabel(apiEnvironment, clientEnvironmentName())
  if (!label) {
    return null
  }

  return (
    <div
      role="status"
      className="border-b border-op-toast-warning-border bg-op-toast-warning-background px-4 py-2 text-center text-sm font-semibold text-op-toast-warning-text"
    >
      Non-production environment: {label}. Activity here is not live customer activity.
    </div>
  )
}
