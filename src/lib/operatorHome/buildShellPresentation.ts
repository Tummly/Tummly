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
import type {
  OperatorHomeLocationOption,
  OperatorShellPresentation,
  OperatorSidebarNavId,
} from "@/types/operatorHome"

const OMITTED_NAVBAR_CONTROLS = [
  "search",
  "ai-copilot",
  "help",
] as const

/** Shell-facing inputs from the Operator workspace session (+ active page chrome). */
export type BuildOperatorShellPresentationInput = {
  operatorDisplayName: string
  activationExpiresAt: string | null
  selfRole?: string | null
  locations: OperatorHomeLocationOption[]
  selectedLocationId: number
  locationSwitcherInteractive: boolean
  activeNavId?: OperatorSidebarNavId
  pageTitle?: string
}

/**
 * Derive shell chrome from Operator workspace session inputs.
 * Page title / active nav default to Home until another page module supplies them.
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
  const pageTitle = input.pageTitle ?? "Home"

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
    sidebarNav: getOperatorSidebarNav(activeNavId),
    locationSwitcher: {
      interactive: input.locationSwitcherInteractive,
      selectedLocationId: selected?.id ?? input.selectedLocationId,
      selectedLocationName: selected?.name ?? "",
      options: input.locations,
    },
    pageTitle,
  }
}
