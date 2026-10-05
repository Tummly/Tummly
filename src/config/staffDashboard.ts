/** Combined Admin + Support staff shell. */
export const STAFF_DASHBOARD_URL = "/admin-dashboard"

export const STAFF_DASHBOARD_ADMIN_URL = `${STAFF_DASHBOARD_URL}/admin`

export const STAFF_DASHBOARD_SUPPORT_URL = `${STAFF_DASHBOARD_URL}/support`

/** React Router path segments for the staff shell (no leading slash). */
export const STAFF_DASHBOARD_ROUTES = {
  root: "admin-dashboard",
  admin: "admin",
  support: "support",
  supportQuery: "support/queries/:id",
} as const

export function staffDashboardDefaultUrl(role: "ADMIN" | "SUPPORT") {
  return role === "SUPPORT"
    ? STAFF_DASHBOARD_SUPPORT_URL
    : STAFF_DASHBOARD_ADMIN_URL
}

export function staffDashboardQueryUrl(id: number | string) {
  return `${STAFF_DASHBOARD_SUPPORT_URL}/queries/${id}`
}
