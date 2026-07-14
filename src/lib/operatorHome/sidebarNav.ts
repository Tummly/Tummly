export type OperatorSidebarNavId =
  | "home"
  | "guests"
  | "capture"
  | "feedback"
  | "campaigns"
  | "offers"
  | "reports"
  | "settings"

export interface OperatorSidebarNavItem {
  id: OperatorSidebarNavId
  label: string
  navigable: boolean
  active: boolean
}

export const OPERATOR_SIDEBAR_NAV: ReadonlyArray<{
  id: OperatorSidebarNavId
  label: string
}> = [
  { id: "home", label: "Home" },
  { id: "guests", label: "Guests" },
  { id: "capture", label: "Capture" },
  { id: "feedback", label: "Feedback" },
  { id: "campaigns", label: "Campaigns" },
  { id: "offers", label: "Offers" },
  { id: "reports", label: "Reports" },
  { id: "settings", label: "Settings" },
] as const

/** Sidebar chrome for Operator Dashboard — only Home is navigable for now. */
export function getOperatorSidebarNav(
  activeId: OperatorSidebarNavId = "home"
): OperatorSidebarNavItem[] {
  return OPERATOR_SIDEBAR_NAV.map((item) => ({
    id: item.id,
    label: item.label,
    navigable: item.id === "home",
    active: item.id === activeId,
  }))
}
