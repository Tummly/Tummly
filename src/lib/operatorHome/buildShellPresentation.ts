import {
  resolveActivationPeriodBadgePresentation,
} from "@/lib/operatorHome/activationPeriod"
import {
  billingCreditsHeaderActions,
  operatorDashboardBillingCreditsManagePlanPath,
} from "@/lib/operatorBillingCredits/billingCreditsPresentation"
import type { BillingCreditsAccessLevel } from "@/lib/operatorBillingCredits/billingCreditsPresentation"
import type { OperatorDashboardMode } from "@/lib/operatorHome/operatorDashboardPaths"
import { formatSelfRoleSubtitle } from "@/lib/operatorHome/formatSelfRoleSubtitle"
import {
  getOperatorFirstName,
  getOperatorInitials,
} from "@/lib/operatorHome/operatorProfile"
import { getOperatorSidebarNav } from "@/lib/operatorHome/sidebarNav"
import type { OperatorSidebarNavTargets } from "@/lib/operatorHome/sidebarNav"
import type {
  OperatorHomeLocationOption,
  OperatorShellPresentation,
  OperatorSidebarActiveId,
} from "@/types/operatorHome"

const OMITTED_NAVBAR_CONTROLS = ["search", "help"] as const

/** Shell-facing inputs from the Operator workspace session (+ active page chrome). */
export type BuildOperatorShellPresentationInput = {
  operatorDisplayName: string
  activationExpiresAt: string | null
  subscriptionPlan: string
  selfRole?: string | null
  /** Restaurant Permission role; gates Choose a plan with Billing & credits access. */
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
  const showChoosePlanCta = billingCreditsHeaderActions({
    accessLevel: input.billingCreditsAccess ?? "manage",
    permissionRole: input.permissionRole ?? "",
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
    profileDisplayName: input.operatorDisplayName,
    profileFirstName: getOperatorFirstName(input.operatorDisplayName),
    profileInitials: getOperatorInitials(input.operatorDisplayName),
    profileSelfRoleSubtitle: formatSelfRoleSubtitle(input.selfRole ?? null),
    omittedNavbarControls: [...OMITTED_NAVBAR_CONTROLS],
    sidebarNav: getOperatorSidebarNav(activeNavId, input.navTargets, {
      hideTeamPermissions: input.hideTeamPermissions,
      hideBillingCredits: input.hideBillingCredits,
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
