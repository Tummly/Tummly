import {
  computeActivationDaysRemaining,
  formatActivationPeriodBadge,
} from "@/lib/operatorHome/activationPeriod"
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
  selfRole?: string | null
  locations: OperatorHomeLocationOption[]
  selectedLocationId: number
  locationSwitcherInteractive: boolean
  activeNavId?: OperatorSidebarActiveId
  navTargets?: OperatorSidebarNavTargets
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

  const daysRemaining = computeActivationDaysRemaining(
    input.activationExpiresAt,
    now
  )
  const activeNavId = input.activeNavId ?? "home"

  return {
    activationPeriodBadge: formatActivationPeriodBadge(
      daysRemaining,
      input.activationExpiresAt
    ),
    profileDisplayName: input.operatorDisplayName,
    profileFirstName: getOperatorFirstName(input.operatorDisplayName),
    profileInitials: getOperatorInitials(input.operatorDisplayName),
    profileSelfRoleSubtitle: formatSelfRoleSubtitle(input.selfRole ?? null),
    omittedNavbarControls: [...OMITTED_NAVBAR_CONTROLS],
    sidebarNav: getOperatorSidebarNav(activeNavId, input.navTargets),
    locationSwitcher: {
      interactive: input.locationSwitcherInteractive,
      selectedLocationId: selected?.id ?? input.selectedLocationId,
      selectedLocationName: selected?.name ?? "",
      options: input.locations,
    },
  }
}
