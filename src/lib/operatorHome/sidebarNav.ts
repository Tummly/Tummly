import {
  isNavigableOperatorSidebarPrimaryNavId,
  isNavigableOperatorSidebarSettingsChildId,
  operatorDashboardNavPath,
  type OperatorDashboardMode,
} from "@/lib/operatorHome/operatorDashboardPaths";

export type OperatorSidebarPrimaryNavId =
  | "home"
  | "guests"
  | "capture"
  | "feedback"
  | "campaigns"
  | "offers"
  | "reports";

export type OperatorSidebarSettingsChildId =
  | "account-workspace"
  | "locations"
  | "team-permissions"
  | "billing-credits"
  | "privacy-consent"
  | "brand-guest-form";

export type OperatorSidebarFooterNavId = "tummly-shop";

/** Any SideNav row id (primary, Settings child, or footer chrome). */
export type OperatorSidebarNavId =
  | OperatorSidebarPrimaryNavId
  | OperatorSidebarSettingsChildId
  | OperatorSidebarFooterNavId;

/** Ids that may be the active Operator dashboard section. */
export type OperatorSidebarActiveId =
  | OperatorSidebarPrimaryNavId
  | OperatorSidebarSettingsChildId
  | OperatorSidebarFooterNavId;

export interface OperatorSidebarNavItem {
  id: OperatorSidebarNavId;
  label: string;
  navigable: boolean;
  active: boolean;
  to?: string;
}

export interface OperatorSidebarSettingsGroup {
  id: "settings";
  label: "Settings";
  /** Settings is disclosure chrome only — never a destination. */
  navigable: false;
  /** Settings itself never carries aria-current / active. */
  active: false;
  children: OperatorSidebarNavItem[];
  /** When true, UI must show children even if persistence says closed. */
  forceExpanded: boolean;
}

export interface OperatorSidebarNavModel {
  primary: OperatorSidebarNavItem[];
  settings: OperatorSidebarSettingsGroup;
  footer: OperatorSidebarNavItem[];
}

export const OPERATOR_SIDEBAR_PRIMARY_NAV: ReadonlyArray<{
  id: OperatorSidebarPrimaryNavId;
  label: string;
}> = [
  { id: "home", label: "Home" },
  { id: "guests", label: "Guests" },
  { id: "capture", label: "Capture" },
  { id: "feedback", label: "Feedback" },
  { id: "campaigns", label: "Campaigns" },
  { id: "offers", label: "Offers" },
  { id: "reports", label: "Reports" },
] as const;

export const OPERATOR_SIDEBAR_SETTINGS_CHILDREN: ReadonlyArray<{
  id: OperatorSidebarSettingsChildId;
  label: string;
}> = [
  { id: "account-workspace", label: "Account & workspace" },
  { id: "locations", label: "Locations" },
  { id: "team-permissions", label: "Team & permissions" },
  { id: "billing-credits", label: "Billing & credits" },
  { id: "privacy-consent", label: "Privacy & consent" },
  { id: "brand-guest-form", label: "Brand & guest form" },
] as const;

export const OPERATOR_SIDEBAR_SHOP = {
  id: "tummly-shop" as const,
  label: "Tummly Shop",
};

const SETTINGS_CHILD_IDS = new Set<string>(
  OPERATOR_SIDEBAR_SETTINGS_CHILDREN.map((item) => item.id),
);

export function isSettingsChildId(
  id: string,
): id is OperatorSidebarSettingsChildId {
  return SETTINGS_CHILD_IDS.has(id);
}

/**
 * Display open state for Settings disclosure.
 * An active Settings child always forces the group open.
 */
export function resolveSettingsDisclosureOpen(
  persistedOpen: boolean,
  forceOpen: boolean,
): boolean {
  return forceOpen || persistedOpen;
}

export type OperatorSidebarNavTargets = {
  mode: OperatorDashboardMode;
  locationId: number;
};

export type OperatorSidebarNavOptions = {
  hideTeamPermissions?: boolean;
};

/** Sidebar chrome for Operator Dashboard — navigable primary destinations. */
export function getOperatorSidebarNav(
  activeId: OperatorSidebarActiveId = "home",
  navTargets?: OperatorSidebarNavTargets,
  options?: OperatorSidebarNavOptions,
): OperatorSidebarNavModel {
  const primary = OPERATOR_SIDEBAR_PRIMARY_NAV.map((item) => ({
    id: item.id,
    label: item.label,
    navigable: isNavigableOperatorSidebarPrimaryNavId(item.id),
    active: item.id === activeId,
    to:
      navTargets != null && isNavigableOperatorSidebarPrimaryNavId(item.id)
        ? operatorDashboardNavPath(
            navTargets.mode,
            item.id,
            navTargets.locationId,
          )
        : undefined,
  }));

  const children = OPERATOR_SIDEBAR_SETTINGS_CHILDREN.filter((item) => {
    if (item.id === "team-permissions" && options?.hideTeamPermissions) {
      return false
    }
    return true
  }).map((item) => ({
    id: item.id,
    label: item.label,
    navigable: isNavigableOperatorSidebarSettingsChildId(item.id),
    active: item.id === activeId,
    to:
      navTargets != null
      && isNavigableOperatorSidebarSettingsChildId(item.id)
        ? operatorDashboardNavPath(
            navTargets.mode,
            item.id,
            navTargets.locationId,
          )
        : undefined,
  }));

  const forceExpanded = children.some((child) => child.active);

  return {
    primary,
    settings: {
      id: "settings",
      label: "Settings",
      navigable: false,
      active: false,
      children,
      forceExpanded,
    },
    footer: [
      {
        id: OPERATOR_SIDEBAR_SHOP.id,
        label: OPERATOR_SIDEBAR_SHOP.label,
        navigable: navTargets != null,
        active: activeId === OPERATOR_SIDEBAR_SHOP.id,
        to:
          navTargets != null
            ? operatorDashboardNavPath(
                navTargets.mode,
                OPERATOR_SIDEBAR_SHOP.id,
                navTargets.locationId,
              )
            : undefined,
      },
    ],
  };
}
