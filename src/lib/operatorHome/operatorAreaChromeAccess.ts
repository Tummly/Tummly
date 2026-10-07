import type { OperatorSidebarActiveId, OperatorSidebarNavId } from "@/lib/operatorHome/sidebarNav"
import type { OperatorAreaChromeAccess } from "@/lib/operatorHome/parseOperatorProfile"

/** Chrome access level for one Operator Area (`/auth/me` / locations omit → treat as allowed). */
export type OperatorAreaChromeAccessLevel = OperatorAreaChromeAccess

/**
 * Per–SideNav-row Area chrome from the locations payload.
 * Omit / undefined must not hide (CODING_STANDARDS chrome omit default).
 * Only explicit `"none"` denies the row.
 */
export type OperatorSidebarAreaAccess = Partial<
  Record<Exclude<OperatorSidebarNavId, "home">, OperatorAreaChromeAccessLevel>
>

/** Workspace / locations chrome fields that gate SideNav rows and deep links. */
export type OperatorWorkspaceAreaChromeFields = {
  guestsAccess: OperatorAreaChromeAccess
  captureAccess: OperatorAreaChromeAccess
  feedbackAccess: OperatorAreaChromeAccess
  campaignsAccess: OperatorAreaChromeAccess
  offersAccess: OperatorAreaChromeAccess
  reportsAccess: OperatorAreaChromeAccess
  accountWorkspaceAccess: OperatorAreaChromeAccess
  locationsAccess: OperatorAreaChromeAccess
  teamPermissionsAccess: OperatorAreaChromeAccess
  billingCreditsAccess: OperatorAreaChromeAccess
  privacyConsentAccess: OperatorAreaChromeAccess
  tummlyShopAccess: OperatorAreaChromeAccess
}

export function buildOperatorSidebarAreaAccess(
  fields: OperatorWorkspaceAreaChromeFields
): OperatorSidebarAreaAccess {
  return {
    guests: fields.guestsAccess,
    capture: fields.captureAccess,
    feedback: fields.feedbackAccess,
    campaigns: fields.campaignsAccess,
    offers: fields.offersAccess,
    reports: fields.reportsAccess,
    "account-workspace": fields.accountWorkspaceAccess,
    locations: fields.locationsAccess,
    "team-permissions": fields.teamPermissionsAccess,
    "billing-credits": fields.billingCreditsAccess,
    "privacy-consent": fields.privacyConsentAccess,
    "tummly-shop": fields.tummlyShopAccess,
  }
}

const AREA_NAV_IDS = [
  "guests",
  "capture",
  "feedback",
  "campaigns",
  "offers",
  "reports",
  "account-workspace",
  "locations",
  "team-permissions",
  "billing-credits",
  "privacy-consent",
  "tummly-shop",
] as const satisfies ReadonlyArray<Exclude<OperatorSidebarNavId, "home">>

/**
 * SideNav ids to omit when the actor has explicit No access.
 * Home is never denied here — it is the safe landing path.
 */
export function resolveHiddenOperatorSidebarNavIds(
  access: OperatorSidebarAreaAccess
): ReadonlyArray<OperatorSidebarNavId> {
  return AREA_NAV_IDS.filter((id) => access[id] === "none")
}

/**
 * When the active section is denied, return true so the shell redirects to Home.
 * Home itself is never denied.
 */
export function isDeniedOperatorSidebarActiveId(
  activeId: OperatorSidebarActiveId,
  access: OperatorSidebarAreaAccess
): boolean {
  if (activeId === "home") {
    return false
  }
  return access[activeId] === "none"
}
