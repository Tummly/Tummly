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
  invitationsEmptyTitle: "No pending invitations",
  invitationsEmptyHelper:
    "Invite a team member to add someone to this workspace.",
  expired: "Expired",
  resend: "Resend",
  revoke: "Revoke",
  accessActivityTitle: "Security & access activity",
  accessActivitySubtitle:
    "Review recent changes to team access and permissions.",
  accessActivityEmptyTitle: "No access activity yet.",
  accessActivityEmptyHelper:
    "Changes to invitations, members and permissions will show here.",
  viewFullAuditLog: "View full audit log",
  accessActivitySheetTitle: "Access activity",
} as const

export const ACCESS_ACTIVITY_AREA_LABELS: Record<string, string> = {
  "account-workspace": "Account & workspace",
  locations: "Locations",
  "team-permissions": "Team & permissions",
  capture: "Capture",
  feedback: "Feedback",
  guests: "Guests",
  campaigns: "Campaigns",
  offers: "Offers",
  reports: "Reports",
  "tummly-shop": "Tummly Shop",
  "billing-credits": "Billing & credits",
  "privacy-consent": "Privacy & consent",
  "ai-assistant": "AI Assistant",
}

export type AccessActivityKind =
  | "invitation-sent"
  | "invitation-resent"
  | "invitation-revoked"
  | "invitation-accepted"
  | "role-changed"
  | "location-scope-changed"
  | "member-deactivated"
  | "member-reactivated"
  | "member-removed"
  | "permission-cell-changed"

export type AccessActivitySnapshot = {
  kind: AccessActivityKind | string
  actorDisplayName: string
  targetDisplayName: string | null
  fromValue: string | null
  toValue: string | null
}

function possessive(name: string): string {
  return `${name}'s`
}

function parsePermissionCell(value: string | null): {
  area: string
  level: string
} {
  if (value == null || !value.includes(":")) {
    return { area: value ?? "", level: "" }
  }
  const index = value.indexOf(":")
  const areaId = value.slice(0, index)
  const level = value.slice(index + 1)
  return {
    area: ACCESS_ACTIVITY_AREA_LABELS[areaId] ?? areaId,
    level,
  }
}

export function formatAccessActivityCopy(
  row: AccessActivitySnapshot
): string {
  const actor = row.actorDisplayName
  const target = row.targetDisplayName ?? ""
  switch (row.kind) {
    case "invitation-sent":
      return `${actor} invited ${target} as ${row.fromValue} (${row.toValue}).`
    case "invitation-resent":
      return `${actor} resent the invitation to ${target}.`
    case "invitation-revoked":
      return `${actor} revoked the invitation to ${target}.`
    case "invitation-accepted":
      return `${target} accepted the invitation.`
    case "role-changed":
      return `${actor} changed ${possessive(target)} role from ${row.fromValue} to ${row.toValue}.`
    case "location-scope-changed":
      return `${actor} changed ${possessive(target)} location access from ${row.fromValue} to ${row.toValue}.`
    case "member-deactivated":
      return `${actor} deactivated ${target}.`
    case "member-reactivated":
      return `${actor} reactivated ${target}.`
    case "member-removed":
      return `${actor} removed ${target}.`
    case "permission-cell-changed": {
      const from = parsePermissionCell(row.fromValue)
      const to = parsePermissionCell(row.toValue)
      return `${actor} changed Admin permission for ${from.area} from ${from.level} to ${to.level}.`
    }
    default:
      return ""
  }
}

function londonYmd(date: Date): string {
  return new Intl.DateTimeFormat("en-CA", {
    timeZone: "Europe/London",
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
  }).format(date)
}

function previousYmd(ymd: string): string {
  const [year, month, day] = ymd.split("-").map(Number)
  return new Date(Date.UTC(year, month - 1, day - 1))
    .toISOString()
    .slice(0, 10)
}

function londonTime(date: Date): string {
  return new Intl.DateTimeFormat("en-GB", {
    timeZone: "Europe/London",
    hour: "2-digit",
    minute: "2-digit",
    hourCycle: "h23",
  }).format(date)
}

export function formatAccessActivityOccurredAt(
  iso: string,
  now: Date
): string {
  const occurred = new Date(iso)
  const time = londonTime(occurred)
  const occurredDay = londonYmd(occurred)
  const today = londonYmd(now)
  if (occurredDay === today) {
    return `Today, ${time}`
  }
  if (occurredDay === previousYmd(today)) {
    return `Yesterday, ${time}`
  }
  const datePart = new Intl.DateTimeFormat("en-GB", {
    timeZone: "Europe/London",
    day: "numeric",
    month: "short",
    year: "numeric",
  }).format(occurred)
  return `${datePart}, ${time}`
}

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

export function revokeConfirmCopy(email: string): {
  title: string
  body: string
  primaryLabel: string
} {
  return {
    title: `Revoke invitation to ${email}?`,
    body: "The invite link stops working. No email is sent. You may invite this email again.",
    primaryLabel: "Revoke",
  }
}
