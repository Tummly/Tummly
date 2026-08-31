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
  emailPlaceholder: "Enter",
  fullName: "Full name",
  fullNamePlaceholder: "Enter",
  role: "Role",
  rolePlaceholder: "Select",
  locationAccess: "Location access",
  locationAccessPlaceholder: "Select",
  message: "Message",
  messagePlaceholder: "Optional note",
  sendInvite: "Send invite",
  cancel: "Cancel",
  allLocations: "All locations",
  selectedLocations: "Selected locations",
  allLocationsHelper: "Includes locations you add later.",
  membersTitle: "Team members",
  membersSubtitle:
    "Manage who can access this workspace and what each person can do.",
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
  locationAccessSelectedOnly: "Selected only",
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
  teamMembers: "Team members",
  pendingInvites: "Pending invites",
  locationManagers: "Location managers",
  limitedAccessUsers: "Limited access users",
  owners: "Owners",
  columnName: "Name",
  columnEmail: "Email",
  columnRole: "Role",
  columnLocationAccess: "Location access",
  columnStatus: "Status",
  columnLastActive: "Last active",
  columnActions: "Actions",
  lastActiveEmpty: "—",
  view: "View",
  editRole: "Edit role",
  editAccess: "Edit access",
  suspend: "Suspend",
  editMemberTitle: "Edit team member",
  editMemberSubtitle:
    "Update this person's role and what locations they can access.",
  viewMemberTitle: "View team member",
  viewMemberSubtitle:
    "Review this person's role and what locations they can access.",
  columnUser: "User",
  columnAccess: "Access",
  columnLocations: "Locations",
  statusActive: "Active",
  statusInactive: "Inactive",
  matrixTitle: "Permission matrix",
  matrixSubtitle:
    "Review what each role can view or manage across workspace roles.",
  matrixSubtitleEditable:
    "Review what each role can view or manage. Owners can adjust Admin access per product area.",
  saveChanges: "Save changes",
  productArea: "Product area",
  noAccessDisplay: "—",
  invitationsTitle: "Pending invitations",
  invitationsSubtitle:
    "People who have been invited but have not accepted yet.",
  invitationsEmptyTitle: "No pending invitations",
  invitationsEmptyHelper:
    "Invite a team member to add someone to this workspace.",
  columnInvitedBy: "Invited by",
  columnSent: "Sent",
  columnExpires: "Expires",
  expired: "Expired",
  resend: "Resend",
  revoke: "Cancel",
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

/** Invite dialog — Figma 3762:23806 (1019px frame, 32px padding). */
export const TEAM_PERMISSIONS_INVITE_DIALOG_CONTENT_CLASS =
  "gap-[60px] rounded-op-md bg-op-surface-primary p-8 text-op-text-primary sm:max-w-[1019px]"

export const TEAM_PERMISSIONS_INVITE_BODY_STACK_CLASS =
  "flex w-full flex-col gap-10"

export const TEAM_PERMISSIONS_INVITE_FORM_STACK_CLASS =
  "flex w-full flex-col gap-5"

export const TEAM_PERMISSIONS_INVITE_FORM_ROW_CLASS =
  "flex w-full flex-col gap-5 sm:flex-row sm:items-start sm:gap-5"

export const TEAM_PERMISSIONS_INVITE_FIELD_STACK_CLASS =
  "flex min-w-0 flex-1 flex-col gap-2"

export const TEAM_PERMISSIONS_INVITE_DIVIDER_CLASS =
  "m-0 h-px w-full shrink-0 border-0 bg-[rgba(74,74,76,0.4)]"

export const TEAM_PERMISSIONS_INVITE_MESSAGE_SECTION_CLASS =
  "flex h-[188px] w-full flex-col"

export const TEAM_PERMISSIONS_INVITE_MESSAGE_FIELD_CLASS =
  "flex min-h-0 flex-1 flex-col gap-2"

export const TEAM_PERMISSIONS_INVITE_TEXTAREA_CLASS =
  "field-sizing-fixed min-h-0 flex-1 resize-none rounded-[4px] border-op-input-border bg-transparent px-[15px] py-[15px] text-sm text-op-text-primary placeholder:text-op-input-placeholder dark:bg-transparent dark:disabled:bg-transparent"

/** Members tab stats card — Figma 3762:22008. */
export const TEAM_PERMISSIONS_STATS_CARD_CLASS =
  "overflow-clip rounded-op-lg border border-op-card-border bg-op-surface-primary p-6 dark:bg-op-color-gray-992 dark:shadow-none"

/** 20px vertical rhythm between stat rows and dividers. */
export const TEAM_PERMISSIONS_STATS_STACK_CLASS =
  "flex w-full flex-col gap-5"

/** Two stat pairs per row — 40px column gutter at sm+. */
export const TEAM_PERMISSIONS_STATS_ROW_CLASS =
  "flex w-full flex-col gap-5 sm:flex-row sm:items-center sm:gap-10"

/** Owners sits in the left column only (half width minus half the 40px gutter). */
export const TEAM_PERMISSIONS_STATS_OWNERS_ROW_CLASS =
  "flex w-full sm:w-[calc(50%-1.25rem)] sm:items-center"

export const TEAM_PERMISSIONS_STATS_PAIR_CLASS =
  "flex min-w-0 flex-1 items-center justify-between gap-4"

export const TEAM_PERMISSIONS_STATS_LABEL_CLASS =
  "m-0 shrink-0 text-base font-semibold leading-normal text-[var(--op-color-gray-550)]"

export const TEAM_PERMISSIONS_STATS_VALUE_CLASS =
  "m-0 text-right text-base font-medium leading-normal text-foreground"

export const TEAM_PERMISSIONS_STATS_DIVIDER_CLASS =
  "m-0 h-px w-full shrink-0 border-0 bg-op-card-border"

export function formatTeamPermissionsOwnersStat(count: number): string {
  return count === 1 ? "1 owner" : `${count} owners`
}

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

export function suspendConfirmCopy(name: string): {
  title: string
  body: string
  primaryLabel: string
} {
  return {
    title: `Suspend ${name}?`,
    body: "They will be removed from this workspace. This cannot be undone. Writable Key contacts move to the Account owner.",
    primaryLabel: "Suspend",
  }
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
