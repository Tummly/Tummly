import type { OperatorSidebarPrimaryNavId } from "@/lib/operatorHome/sidebarNav"

export type OperatorDashboardMode = "single" | "multi"

const NAVIGABLE_PRIMARY_NAV_IDS = new Set<OperatorSidebarPrimaryNavId>([
  "home",
  "guests",
])

export function operatorDashboardRootPath(
  mode: OperatorDashboardMode
): string {
  return mode === "single" ? "/single-dashboard" : "/multi-dashboard"
}

export function operatorDashboardNavPath(
  mode: OperatorDashboardMode,
  navId: Extract<OperatorSidebarPrimaryNavId, "home" | "guests">,
  locationId: number
): string {
  const root = operatorDashboardRootPath(mode)
  const path = navId === "guests" ? `${root}/guests` : root
  return `${path}?location=${locationId}`
}

export function operatorDashboardGuestProfilePath(
  mode: OperatorDashboardMode,
  guestId: number | string,
  locationId: number
): string {
  const root = operatorDashboardRootPath(mode)
  return `${root}/guests/${guestId}?location=${locationId}`
}

export function operatorDashboardGuestEditPath(
  mode: OperatorDashboardMode,
  guestId: number | string,
  locationId: number,
  hash?: "tags" | "data-privacy"
): string {
  const root = operatorDashboardRootPath(mode)
  const base = `${root}/guests/${guestId}/edit?location=${locationId}`
  return hash == null ? base : `${base}#${hash}`
}

/** Guest Profile header destinations: Edit + ⋮ Manage tags / Delete. */
export function guestProfileHeaderActionPaths(
  mode: OperatorDashboardMode,
  guestId: number | string,
  locationId: number
): {
  editGuestDetails: string
  manageTags: string
  deleteGuestData: string
} {
  return {
    editGuestDetails: operatorDashboardGuestEditPath(mode, guestId, locationId),
    manageTags: operatorDashboardGuestEditPath(mode, guestId, locationId, "tags"),
    deleteGuestData: operatorDashboardGuestEditPath(
      mode,
      guestId,
      locationId,
      "data-privacy"
    ),
  }
}

export function resolveOperatorSidebarActiveId(
  pathname: string
): Extract<OperatorSidebarPrimaryNavId, "home" | "guests"> {
  const segments = pathname.split("/").filter(Boolean)
  if (segments.includes("guests")) {
    return "guests"
  }

  return "home"
}

export function isNavigableOperatorSidebarPrimaryNavId(
  id: OperatorSidebarPrimaryNavId
): id is Extract<OperatorSidebarPrimaryNavId, "home" | "guests"> {
  return NAVIGABLE_PRIMARY_NAV_IDS.has(id)
}
