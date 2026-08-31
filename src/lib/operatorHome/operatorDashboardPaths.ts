import type {
  OperatorSidebarPrimaryNavId,
  OperatorSidebarSettingsChildId,
} from "@/lib/operatorHome/sidebarNav"

export type OperatorDashboardMode = "single" | "multi"

const NAVIGABLE_PRIMARY_NAV_IDS = new Set<OperatorSidebarPrimaryNavId>([
  "home",
  "guests",
  "capture",
  "feedback",
  "campaigns",
  "offers",
])

const NAVIGABLE_SETTINGS_CHILD_IDS = new Set<OperatorSidebarSettingsChildId>([
  "account-workspace",
  "locations",
  "team-permissions",
  "billing-credits",
  "privacy-consent",
])

export type NavigableOperatorSidebarPrimaryNavId = Extract<
  OperatorSidebarPrimaryNavId,
  "home" | "guests" | "capture" | "feedback" | "campaigns" | "offers"
>

export type NavigableOperatorSidebarSettingsChildId = Extract<
  OperatorSidebarSettingsChildId,
  | "account-workspace"
  | "locations"
  | "team-permissions"
  | "billing-credits"
  | "privacy-consent"
>

export type NavigableOperatorSidebarNavId =
  | NavigableOperatorSidebarPrimaryNavId
  | NavigableOperatorSidebarSettingsChildId

export function operatorDashboardRootPath(
  mode: OperatorDashboardMode
): string {
  return mode === "single" ? "/single-dashboard" : "/multi-dashboard"
}

/** AccountType from auth session → which dashboard root the operator may use. */
export function operatorDashboardModeForAccountType(
  accountType: string | null | undefined
): OperatorDashboardMode | null {
  if (accountType === "Single") {
    return "single"
  }
  if (accountType === "Multi") {
    return "multi"
  }
  return null
}

/**
 * When the URL mode does not match AccountType, return a same-suffix path on the
 * allowed dashboard (query string preserved). Returns null when access is allowed
 * or AccountType is unknown.
 */
export function resolveMismatchedOperatorDashboardRedirect(options: {
  mode: OperatorDashboardMode
  accountType: string | null | undefined
  pathname: string
  search?: string
}): string | null {
  const allowedMode = operatorDashboardModeForAccountType(options.accountType)
  if (allowedMode == null || allowedMode === options.mode) {
    return null
  }

  const fromRoot = operatorDashboardRootPath(options.mode)
  const toRoot = operatorDashboardRootPath(allowedMode)
  if (
    options.pathname !== fromRoot &&
    !options.pathname.startsWith(`${fromRoot}/`)
  ) {
    return null
  }

  const suffix = options.pathname.slice(fromRoot.length)
  const search = options.search ?? ""
  return `${toRoot}${suffix}${search}`
}

export function operatorDashboardNavPath(
  mode: OperatorDashboardMode,
  navId: NavigableOperatorSidebarNavId,
  locationId: number
): string {
  const root = operatorDashboardRootPath(mode)
  const path =
    navId === "home"
      ? root
      : navId === "account-workspace"
        ? `${root}/settings/account-workspace`
        : navId === "locations"
          ? `${root}/settings/locations`
          : navId === "team-permissions"
            ? `${root}/settings/team-permissions`
            : navId === "billing-credits"
              ? `${root}/settings/billing-credits`
              : navId === "privacy-consent"
                ? `${root}/settings/privacy-consent`
                : `${root}/${navId}`
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

/** Location-wide Offers redemption log (all catalog offers at the location). */
export function operatorDashboardOffersRedemptionLogPath(
  mode: OperatorDashboardMode,
  locationId: number
): string {
  const root = operatorDashboardRootPath(mode)
  return `${root}/offers/redemption-log?location=${locationId}`
}

/** Offer Details for one catalog offer at the selected location. */
export function operatorDashboardOfferDetailsPath(
  mode: OperatorDashboardMode,
  offerId: number | string,
  locationId: number,
  options?: { tab?: string }
): string {
  const root = operatorDashboardRootPath(mode)
  const params = new URLSearchParams({ location: String(locationId) })
  if (options?.tab != null && options.tab !== "") {
    params.set("tab", options.tab)
  }
  return `${root}/offers/${offerId}?${params.toString()}`
}

/** Thin Campaign detail for one campaign at the selected location. */
export function operatorDashboardCampaignDetailsPath(
  mode: OperatorDashboardMode,
  campaignId: number | string,
  locationId: number
): string {
  const root = operatorDashboardRootPath(mode)
  return `${root}/campaigns/${campaignId}?location=${locationId}`
}

/** Full-page Guest Preview for a campaign message. */
export function operatorDashboardCampaignPreviewPath(
  mode: OperatorDashboardMode,
  campaignId: number | string,
  locationId: number
): string {
  const root = operatorDashboardRootPath(mode)
  return `${root}/campaigns/${campaignId}/preview?location=${locationId}`
}

/** Full-page Guest Preview for a catalog offer coupon. */
export function operatorDashboardOfferPreviewPath(
  mode: OperatorDashboardMode,
  offerId: number | string,
  locationId: number
): string {
  const root = operatorDashboardRootPath(mode)
  return `${root}/offers/${offerId}/preview?location=${locationId}`
}

/**
 * Campaigns list with optional catalog offerId query for Share-in-campaign CTA
 * (Offer Details Claims empty — ticket 24). Does not open wizard prefill.
 */
export function operatorDashboardCampaignsPathWithOffer(
  mode: OperatorDashboardMode,
  locationId: number,
  offerId: number | string
): string {
  return `${operatorDashboardNavPath(mode, "campaigns", locationId)}&offerId=${offerId}`
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
): NavigableOperatorSidebarNavId {
  const segments = pathname.split("/").filter(Boolean)
  if (segments.includes("account-workspace")) {
    return "account-workspace"
  }
  if (
    segments.includes("settings") &&
    segments[segments.indexOf("settings") + 1] === "locations"
  ) {
    return "locations"
  }
  if (segments.includes("team-permissions")) {
    return "team-permissions"
  }
  if (segments.includes("billing-credits")) {
    return "billing-credits"
  }
  if (segments.includes("privacy-consent")) {
    return "privacy-consent"
  }
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
  if (segments.includes("offers")) {
    return "offers"
  }

  return "home"
}

export function isNavigableOperatorSidebarPrimaryNavId(
  id: OperatorSidebarPrimaryNavId
): id is NavigableOperatorSidebarPrimaryNavId {
  return NAVIGABLE_PRIMARY_NAV_IDS.has(id)
}

export function isNavigableOperatorSidebarSettingsChildId(
  id: OperatorSidebarSettingsChildId
): id is NavigableOperatorSidebarSettingsChildId {
  return NAVIGABLE_SETTINGS_CHILD_IDS.has(id)
}
