import type { OperatorSidebarPrimaryNavId } from "@/lib/operatorHome/sidebarNav"

export type OperatorDashboardMode = "single" | "multi"

const NAVIGABLE_PRIMARY_NAV_IDS = new Set<OperatorSidebarPrimaryNavId>([
  "home",
  "guests",
  "capture",
  "feedback",
  "campaigns",
])

export type NavigableOperatorSidebarPrimaryNavId = Extract<
  OperatorSidebarPrimaryNavId,
  "home" | "guests" | "capture" | "feedback" | "campaigns"
>

export function operatorDashboardRootPath(
  mode: OperatorDashboardMode
): string {
  return mode === "single" ? "/single-dashboard" : "/multi-dashboard"
}

export function operatorDashboardNavPath(
  mode: OperatorDashboardMode,
  navId: NavigableOperatorSidebarPrimaryNavId,
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

/** Account-wide Capture Archive. Optional locationId pre-selects Location filter (multi). */
export function operatorDashboardCaptureArchivePath(
  mode: OperatorDashboardMode,
  options?: { locationId?: number; from?: string }
): string {
  const root = operatorDashboardRootPath(mode)
  const params = new URLSearchParams()
  if (options?.locationId != null) {
    params.set("locationId", String(options.locationId))
  }
  if (options?.from != null && options.from !== "") {
    params.set("from", options.from)
  }
  const query = params.toString()
  return query === ""
    ? `${root}/capture/archive`
    : `${root}/capture/archive?${query}`
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
): NavigableOperatorSidebarPrimaryNavId {
  const segments = pathname.split("/").filter(Boolean)
  if (segments.includes("guests")) {
    return "guests"
  }
  if (segments.includes("capture")) {
    return "capture"
  }
  if (segments.includes("feedback")) {
    return "feedback"
  }
  if (segments.includes("campaigns")) {
    return "campaigns"
  }

  return "home"
}

export function isNavigableOperatorSidebarPrimaryNavId(
  id: OperatorSidebarPrimaryNavId
): id is NavigableOperatorSidebarPrimaryNavId {
  return NAVIGABLE_PRIMARY_NAV_IDS.has(id)
}
