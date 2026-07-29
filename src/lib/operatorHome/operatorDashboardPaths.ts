import type { OperatorSidebarPrimaryNavId } from "@/lib/operatorHome/sidebarNav"

export type OperatorDashboardMode = "single" | "multi"

const NAVIGABLE_PRIMARY_NAV_IDS = new Set<OperatorSidebarPrimaryNavId>([
  "home",
  "guests",
  "capture",
])

export function operatorDashboardRootPath(
  mode: OperatorDashboardMode
): string {
  return mode === "single" ? "/single-dashboard" : "/multi-dashboard"
}

export function operatorDashboardNavPath(
  mode: OperatorDashboardMode,
  navId: Extract<OperatorSidebarPrimaryNavId, "home" | "guests" | "capture">,
  locationId: number
): string {
  const root = operatorDashboardRootPath(mode)
  const path =
    navId === "home" ? root : `${root}/${navId}`
  return `${path}?location=${locationId}`
}

/** Multi nested per-location Capture — path segment + `?location=` shell sync. */
export function operatorDashboardCaptureLocationPath(
  locationId: number
): string {
  return `/multi-dashboard/capture/locations/${locationId}?location=${locationId}`
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
): Extract<OperatorSidebarPrimaryNavId, "home" | "guests" | "capture"> {
  const segments = pathname.split("/").filter(Boolean)
  if (segments.includes("guests")) {
    return "guests"
  }
  if (segments.includes("capture")) {
    return "capture"
  }

  return "home"
}

export function isNavigableOperatorSidebarPrimaryNavId(
  id: OperatorSidebarPrimaryNavId
): id is Extract<OperatorSidebarPrimaryNavId, "home" | "guests" | "capture"> {
  return NAVIGABLE_PRIMARY_NAV_IDS.has(id)
}
