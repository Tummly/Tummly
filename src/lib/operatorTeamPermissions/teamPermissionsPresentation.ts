export const TEAM_PERMISSIONS_TAB_IDS = [
  "members",
  "roles-permissions",
  "invitations",
  "access-activity",
] as const

export type TeamPermissionsTabId = (typeof TEAM_PERMISSIONS_TAB_IDS)[number]

export const TEAM_PERMISSIONS_PAGE_COPY = {
  title: "Team & permissions",
  subtitle:
    "Invite team members and control what they can access across locations, guests, feedback, campaigns, billing and settings.",
  invite: "Invite team member",
  viewNotes: "View permission notes",
  notesTitle: "View permission notes",
  notesDone: "Done",
  notesBody:
    "Cells store No access, View, Manage, or Scoped. Figma labels Limited, View reports, Redeem only, and Manage* are display notes, not extra stored levels. Only Owner may edit the Admin column. Other permission role columns stay No access on Team & permissions.",
  inviteTitle: "Invite team member",
  inviteSubtitle:
    "Add someone to your Tummly workspace and choose what they can access.",
  email: "Email address",
  fullName: "Full name",
  role: "Role",
  locationAccess: "Location access",
  message: "Message",
  messagePlaceholder: "Optional note",
  sendInvite: "Send invite",
  cancel: "Cancel",
  allLocations: "All locations",
  selectedLocations: "Selected locations",
  allLocationsHelper: "Includes locations you add later.",
  membersTitle: "Team members",
  membersSubtitle: "People who can access this workspace.",
  searchPlaceholder: "Search by name or email",
  filters: "Filters",
  emptyTitle: "No matching team members",
  emptyHelper: "Try a different search or clear filters.",
  clearFilters: "Clear all filters",
  loadError: "Could not load team members.",
  retry: "Retry",
  locationAccessCardTitle: "Location access",
  locationAccessCardSubtitle:
    "Control which locations each team member can view or manage.",
  changeRole: "Change role",
  changeLocation: "Change location access",
  deactivate: "Deactivate",
  reactivate: "Reactivate",
  remove: "Remove",
  save: "Save",
  statActive: "active",
  statPending: "pending",
  statManagers: "managers",
  statRestricted: "restricted",
  statOwner: "1 owner",
  teamMembers: "Team members",
  pendingInvites: "Pending invites",
  locationManagers: "Location managers",
  limitedAccessUsers: "Limited access users",
  owners: "Owners",
  matrixTitle: "Permission matrix",
  matrixSubtitle:
    "Review what each role can view or manage. Owners always have full access.",
  saveChanges: "Save changes",
  productArea: "Product area",
  noAccessDisplay: "—",
} as const

export const TEAM_PERMISSIONS_SELECT_MENU_CLASS = "z-[130]"

export const PERMISSION_MATRIX_ROLES = [
  "Owner",
  "Admin",
  "Area Manager",
  "Location Manager",
  "Marketing",
  "Staff",
  "Billing Admin",
  "Reporting Only",
] as const

const RESTAURANT_WIDE_AREAS = new Set([
  "account-workspace",
  "team-permissions",
  "billing-credits",
  "privacy-consent",
])

const NEVER_NO_ACCESS_AREAS = new Set([
  "account-workspace",
  "team-permissions",
])

export const STORED_PERMISSION_LEVELS = [
  "No access",
  "View",
  "Manage",
  "Scoped",
] as const

export type StoredPermissionLevel = (typeof STORED_PERMISSION_LEVELS)[number]

export function legalAdminLevels(areaId: string): StoredPermissionLevel[] {
  return STORED_PERMISSION_LEVELS.filter((level) => {
    if (level === "No access") {
      return !NEVER_NO_ACCESS_AREAS.has(areaId)
    }
    if (level === "Scoped") {
      return !RESTAURANT_WIDE_AREAS.has(areaId)
    }
    return true
  })
}

export function displayPermissionLabel(
  role: string,
  areaId: string,
  storedLevel: string
): string {
  if (storedLevel === "No access") {
    return TEAM_PERMISSIONS_PAGE_COPY.noAccessDisplay
  }
  if (role === "Admin" && areaId === "team-permissions" && storedLevel === "Manage") {
    return "Manage*"
  }
  if (role === "Staff" && areaId === "offers" && storedLevel === "Scoped") {
    return "Redeem only"
  }
  if (role === "Reporting Only" && areaId === "feedback" && storedLevel === "View") {
    return "View reports"
  }
  if (storedLevel === "View" && limitedViewCells.has(`${role}:${areaId}`)) {
    return "Limited"
  }
  return storedLevel
}

const limitedViewCells = new Set([
  "Marketing:guests",
  "Billing Admin:reports",
  "Marketing:billing-credits",
  "Area Manager:privacy-consent",
  "Location Manager:privacy-consent",
  "Marketing:privacy-consent",
  "Billing Admin:privacy-consent",
  "Billing Admin:ai-assistant",
])

export function resolveTeamPermissionsTabId(
  raw: string | null | undefined,
  privacyConsentHasAccess: boolean
): TeamPermissionsTabId {
  if (raw === "access-activity" && !privacyConsentHasAccess) {
    return "members"
  }
  if (
    raw != null
    && (TEAM_PERMISSIONS_TAB_IDS as readonly string[]).includes(raw)
  ) {
    return raw as TeamPermissionsTabId
  }
  return "members"
}

export function deactivateConfirmCopy(name: string): {
  title: string
  body: string
  primaryLabel: string
} {
  return {
    title: `Deactivate ${name}?`,
    body: "They cannot open this workspace until you reactivate them. The membership stays. Writable Key contacts move to the Account owner.",
    primaryLabel: "Deactivate",
  }
}

export function removeConfirmCopy(name: string): {
  title: string
  body: string
  primaryLabel: string
} {
  return {
    title: `Remove ${name}?`,
    body: "This cannot be undone. You may invite this email again later. Writable Key contacts move to the Account owner.",
    primaryLabel: "Remove",
  }
}
