import {
  resolveActivationPeriodBadgePresentation,
} from "@/lib/operatorHome/activationPeriod"
import {
  billingCreditsHeaderActions,
  operatorDashboardBillingCreditsManagePlanPath,
} from "@/lib/operatorBillingCredits/billingCreditsPresentation"
import type { BillingCreditsAccessLevel } from "@/lib/operatorBillingCredits/billingCreditsPresentation"
import type { OperatorDashboardMode } from "@/lib/operatorHome/operatorDashboardPaths"
import { resolveLockAlertPresentation } from "@/lib/operatorHome/lockAlertPresentation"
import {
  getOperatorFirstName,
  getOperatorInitials,
} from "@/lib/operatorHome/operatorProfile"
import { getOperatorSidebarNav } from "@/lib/operatorHome/sidebarNav"
import type {
  OperatorSidebarNavId,
  OperatorSidebarNavTargets,
} from "@/lib/operatorHome/sidebarNav"
import type {
  OperatorHomeLocationOption,
  OperatorShellPresentation,
  OperatorSidebarActiveId,
} from "@/types/operatorHome"

const OMITTED_NAVBAR_CONTROLS = ["search", "help"] as const

/**
 * Account-menu subtitle: Restaurant Permission role (Owner, Admin, …).
 */
export function resolveProfileSubtitle(
  permissionRole: string | null | undefined
): string | null {
  const trimmed = permissionRole?.trim()
  if (!trimmed) {
    return null
  }
  return trimmed
}

/** Shell-facing inputs from the Operator workspace session (+ active page chrome). */
export type BuildOperatorShellPresentationInput = {
  operatorDisplayName: string
  activationExpiresAt: string | null
  subscriptionPlan: string
  /** Soft lock / Dormant drives Lock Alert; omit or Active hides it. */
  billingStatus?: string | null
  /** Restaurant Permission role; account subtitle + Choose a plan gating. */
  permissionRole?: string | null
  /** Omit defaults to manage so Account-owner chrome stays visible during rollout. */
  billingCreditsAccess?: BillingCreditsAccessLevel
  locations: OperatorHomeLocationOption[]
  selectedLocationId: number
  locationSwitcherInteractive: boolean
  brandLogoPublicUrl?: string | null
  activeNavId?: OperatorSidebarActiveId
  navTargets?: OperatorSidebarNavTargets
  hideTeamPermissions?: boolean
  hideBillingCredits?: boolean
  /** Explicit No-access SideNav ids; omit / empty keeps all rows visible. */
  hiddenNavIds?: ReadonlyArray<OperatorSidebarNavId>
}

/**
 * Derive shell chrome from Operator workspace session inputs.
 * Active nav defaults to Home until another page module supplies it.
 */
export function buildOperatorShellPresentation(
  input: BuildOperatorShellPresentationInput,
  now: Date = new Date()
): OperatorShellPresentation {
  const selected =
    input.locations.find(
      (location) => location.id === input.selectedLocationId
    ) ?? input.locations[0]

  const activeNavId = input.activeNavId ?? "home"
  const mode: OperatorDashboardMode =
    input.navTargets?.mode ?? "multi"
  const locationId = input.navTargets?.locationId ?? input.selectedLocationId
  const accessLevel = input.billingCreditsAccess ?? "manage"
  const permissionRole = input.permissionRole ?? ""
  const showChoosePlanCta = billingCreditsHeaderActions({
    accessLevel,
    permissionRole,
  }).showManagePlan
  const choosePlanHref = showChoosePlanCta
    ? operatorDashboardBillingCreditsManagePlanPath(mode, locationId)
    : null

  return {
    activationPeriodBadge: resolveActivationPeriodBadgePresentation({
      subscriptionPlan: input.subscriptionPlan,
      activationExpiresAt: input.activationExpiresAt,
      choosePlanHref,
      now,
    }),
    lockAlert: resolveLockAlertPresentation({
      billingStatus: input.billingStatus ?? "",
      subscriptionPlan: input.subscriptionPlan,
      accessLevel,
      permissionRole,
      mode,
      locationId,
    }),
    profileDisplayName: input.operatorDisplayName,
    profileFirstName: getOperatorFirstName(input.operatorDisplayName),
    profileInitials: getOperatorInitials(input.operatorDisplayName),
    profileSelfRoleSubtitle: resolveProfileSubtitle(
      input.permissionRole ?? null
    ),
    omittedNavbarControls: [...OMITTED_NAVBAR_CONTROLS],
    sidebarNav: getOperatorSidebarNav(activeNavId, input.navTargets, {
      hideTeamPermissions: input.hideTeamPermissions,
      hideBillingCredits: input.hideBillingCredits,
      hiddenNavIds: input.hiddenNavIds,
    }),
    locationSwitcher: {
      interactive: input.locationSwitcherInteractive,
      selectedLocationId: selected?.id ?? input.selectedLocationId,
      selectedLocationName: selected?.name ?? "",
      brandLogoPublicUrl: input.brandLogoPublicUrl ?? null,
      options: input.locations,
    },
  }
}
