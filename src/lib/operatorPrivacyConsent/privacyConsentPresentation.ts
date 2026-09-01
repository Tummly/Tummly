/** Privacy & consent — Figma 5746:100224 / 3853:27279 / 5746:100280. */

export {
  ACCOUNT_WORKSPACE_IDENTITY_CARD_CLASS as PRIVACY_CONSENT_CARD_CLASS,
} from "@/lib/operatorAccountWorkspace/accountWorkspacePresentation"

export const PRIVACY_CONSENT_PAGE_COPY = {
  title: "Privacy & consent",
  subtitle:
    "Manage guest privacy, permission settings, opt-outs and records for your restaurant workspace.",
  privacySetupStatusTitle: "Privacy setup status",
  privacySetupStatusSubtitle:
    "Check the privacy settings required for the guest experiences and communication channels you use.",
  requirementColumn: "Requirement",
  statusColumn: "Status",
  guestPermissionsTitle: "Guest permissions",
  guestPermissionsSubtitle:
    "Choose which permissions guests can give when they complete your guest form.",
  guestPermissionsFootnote:
    "Changes affect which permission choices are presented to future guests. Existing permission records are preserved.",
  statusLabel: "Status:",
  usedInLabel: "Used in:",
  collectedThroughLabel: "Collected through:",
  statusEnabled: "Enabled",
  statusNotUsed: "Not used",
  permissionRecordsTitle: "Permission records",
  permissionRecordsSubtitle:
    "Review the permissions guests have granted or withdrawn, including when, where and how they were recorded.",
  permissionRecordsSearchPlaceholder: "Search guest",
  permissionRecordsFiltersLabel: "Filters",
  permissionRecordsFiltersTitle: "Filter permission records",
  permissionRecordsEmptyTitle: "No permission records found",
  permissionRecordsEmptyHelper: "Try a different search or clear filters.",
  permissionRecordsClearSearchAndFilters: "Clear search and filters",
  permissionRecordsView: "View",
  privacyActivityTitle: "Privacy activity",
  privacyActivitySubtitle:
    "Review recent changes to guest permissions and privacy settings.",
  privacyActivityEmptyTitle: "No activity yet",
  privacyActivityEmptyBody:
    "Privacy and consent activity will appear here.",
} as const

/** Nested permission tile — Figma Concent card fill `#171717`. */
export const GUEST_PERMISSION_TILE_CLASS =
  "flex min-w-0 flex-1 flex-col justify-between gap-6 rounded-op-md bg-op-background-secondary p-6 dark:bg-[var(--op-color-gray-996)]"

export const GUEST_PERMISSION_TILE_GRID_CLASS =
  "grid grid-cols-1 gap-5 lg:grid-cols-3"

/** Figma toggle 38×16 with 12px thumb (858:1220). */
export const GUEST_PERMISSION_SWITCH_CLASS =
  "h-4 w-[38px] border-0 data-checked:bg-op-button-primary-background data-unchecked:bg-[var(--op-color-gray-550)] dark:data-unchecked:bg-[var(--op-color-gray-550)] [&_[data-slot=switch-thumb]]:size-3 [&_[data-slot=switch-thumb]]:bg-white dark:[&_[data-slot=switch-thumb]]:bg-white data-checked:[&_[data-slot=switch-thumb]]:!translate-x-[22px] data-unchecked:[&_[data-slot=switch-thumb]]:!translate-x-[2px]"

export type PrivacyConsentTabId =
  | "privacy-setup"
  | "guest-permissions"
  | "permission-records"
  | "activity"

export const PRIVACY_CONSENT_TAB_IDS = [
  "privacy-setup",
  "guest-permissions",
  "permission-records",
  "activity",
] as const satisfies readonly PrivacyConsentTabId[]

export const PRIVACY_CONSENT_TAB_LABELS: Record<PrivacyConsentTabId, string> = {
  "privacy-setup": "Privacy setup",
  "guest-permissions": "Guest permissions",
  "permission-records": "Permission records",
  activity: "Activity",
}

export function resolvePrivacyConsentTabId(
  raw: string | null | undefined
): PrivacyConsentTabId {
  if (
    raw === "guest-permissions"
    || raw === "permission-records"
    || raw === "activity"
    || raw === "privacy-setup"
  ) {
    return raw
  }
  return "privacy-setup"
}

export type PrivacySetupRequirementId =
  | "privacy-notice"
  | "guest-permission-wording"
  | "email-marketing"
  | "sms-marketing"
  | "feedback-follow-up"

export type PrivacySetupStatusLabel =
  | "Configured"
  | "Enabled"
  | "Not used"

export type PrivacySetupStatusRow = {
  id: PrivacySetupRequirementId
  requirement: string
  status: PrivacySetupStatusLabel
}

/** Figma Privacy setup status rows (3853:27279) until the API lands. */
export const PRIVACY_SETUP_STATUS_DEMO_ROWS: readonly PrivacySetupStatusRow[] = [
  {
    id: "privacy-notice",
    requirement: "Privacy notice",
    status: "Configured",
  },
  {
    id: "guest-permission-wording",
    requirement: "Guest permission wording",
    status: "Configured",
  },
  {
    id: "email-marketing",
    requirement: "Email marketing",
    status: "Enabled",
  },
  {
    id: "sms-marketing",
    requirement: "SMS marketing",
    status: "Not used",
  },
  {
    id: "feedback-follow-up",
    requirement: "Feedback follow-up",
    status: "Enabled",
  },
]

export type GuestPermissionId =
  | "email-marketing"
  | "sms-marketing"
  | "feedback-follow-up"

export type GuestPermissionCard = {
  id: GuestPermissionId
  title: string
  description: string
  enabled: boolean
  usedIn: string
  collectedThrough: string
}

/** Figma Guest permissions tiles (5746:100280) until the API lands. */
export const GUEST_PERMISSIONS_DEMO_CARDS: readonly GuestPermissionCard[] = [
  {
    id: "email-marketing",
    title: "Email marketing",
    description:
      "Allow guests to agree to receive offers and updates by email.",
    enabled: true,
    usedIn: "Email Campaigns",
    collectedThrough: "Guest Forms",
  },
  {
    id: "sms-marketing",
    title: "SMS marketing",
    description: "Allows SMS offers, updates and campaigns",
    enabled: true,
    usedIn: "SMS Campaigns",
    collectedThrough: "Guest Forms",
  },
  {
    id: "feedback-follow-up",
    title: "Feedback follow-up",
    description:
      "Allow the restaurant to contact a guest about private feedback.",
    enabled: true,
    usedIn: "Private Feedback follow-up",
    collectedThrough: "Guest Forms",
  },
]

export function guestPermissionStatusLabel(
  enabled: boolean
): "Enabled" | "Not used" {
  return enabled
    ? PRIVACY_CONSENT_PAGE_COPY.statusEnabled
    : PRIVACY_CONSENT_PAGE_COPY.statusNotUsed
}

export type PermissionRecordCurrentState = "granted" | "withdrawn"

export type PermissionRecordRow = {
  id: string
  locationGuestId: number
  guestName: string
  permissionId: GuestPermissionId
  permissionLabel: string
  currentState: PermissionRecordCurrentState
  locationId: string
  locationLabel: string
  sourceLabel: string
  recordedLabel: string
  searchText: string
}

export const PERMISSION_RECORD_CURRENT_STATE_LABELS: Record<
  PermissionRecordCurrentState,
  string
> = {
  granted: "Granted",
  withdrawn: "Withdrawn",
}

/** Figma Permission records rows (5746:100788) until the API lands. */
export const PERMISSION_RECORDS_DEMO_ROWS: readonly PermissionRecordRow[] = [
  {
    id: "1",
    locationGuestId: 1,
    guestName: "Amira Khan",
    permissionId: "email-marketing",
    permissionLabel: "Email marketing",
    currentState: "granted",
    locationId: "camden",
    locationLabel: "Camden",
    sourceLabel: "Main guest Feedback form",
    recordedLabel: "22 Aug 2026, 14:26",
    searchText: "amira khan email marketing camden",
  },
  {
    id: "2",
    locationGuestId: 2,
    guestName: "Liam Chen",
    permissionId: "sms-marketing",
    permissionLabel: "SMS marketing",
    currentState: "withdrawn",
    locationId: "camden",
    locationLabel: "Camden",
    sourceLabel: "Guest Form",
    recordedLabel: "20 Aug 2026, 12:10",
    searchText: "liam chen sms marketing camden",
  },
  {
    id: "3",
    locationGuestId: 3,
    guestName: "Sophia Martinez",
    permissionId: "feedback-follow-up",
    permissionLabel: "Feedback follow-up",
    currentState: "granted",
    locationId: "camden",
    locationLabel: "Camden",
    sourceLabel: "Main guest Feedback form",
    recordedLabel: "19 Aug 2026, 18:41",
    searchText: "sophia martinez feedback follow-up camden",
  },
]

export type PrivacyActivityItem = {
  id: string
  timeLabel: string
  description: string
}

/** Figma Privacy activity rows (5746:101810) until the API lands. */
export const PRIVACY_ACTIVITY_DEMO_ITEMS: readonly PrivacyActivityItem[] = [
  {
    id: "1",
    timeLabel: "24 Aug 2026, 10:42",
    description: "James updated SMS marketing wording",
  },
  {
    id: "2",
    timeLabel: "24 Aug 2026, 10:42",
    description: "James updated SMS marketing wording",
  },
  {
    id: "3",
    timeLabel: "24 Aug 2026, 10:42",
    description: "James updated SMS marketing wording",
  },
  {
    id: "4",
    timeLabel: "Today, 10:42",
    description: "James updated SMS consent wording.",
  },
  {
    id: "5",
    timeLabel: "24 Aug 2026, 10:42",
    description: "James updated SMS marketing wording",
  },
]

