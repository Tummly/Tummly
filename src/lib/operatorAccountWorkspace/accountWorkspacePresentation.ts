export const ACCOUNT_WORKSPACE_TAB_IDS = [
  "account-details",
  "business-details",
  "key-contacts",
  "workspace-defaults",
  "account-controls",
] as const

export type AccountWorkspaceTabId =
  (typeof ACCOUNT_WORKSPACE_TAB_IDS)[number]

export const ACCOUNT_WORKSPACE_FORM_TAB_IDS = [
  "account-details",
  "business-details",
  "key-contacts",
  "workspace-defaults",
] as const

export type AccountWorkspaceFormTabId =
  (typeof ACCOUNT_WORKSPACE_FORM_TAB_IDS)[number]

export const ACCOUNT_WORKSPACE_TAB_LABELS: Record<
  AccountWorkspaceTabId,
  string
> = {
  "account-details": "Account details",
  "business-details": "Business details",
  "key-contacts": "Key contacts",
  "workspace-defaults": "Workspace defaults",
  "account-controls": "Account controls",
}

export const ACCOUNT_WORKSPACE_PAGE_COPY = {
  title: "Account & workspace",
  subtitle:
    "Manage the business identity and authorised contacts used to operate this Tummly workspace.",
  saveChanges: "Save changes",
  viewAccountActivity: "View account activity",
  renameTitle: "Change workspace name?",
  renameBody:
    "This changes the name shown to authorised workspace users. Guest-facing names, legal details and location names will not change.",
  renameConfirm: "Change workspace name",
  leaveDirtyTitle: "Unsaved changes",
  leaveDirtyBody:
    "You have unsaved edits on this tab. Save them before you leave, or cancel to discard them and continue.",
  leaveDirtySave: "Save changes",
  cancel: "Cancel",
  saveSuccess: "Account details saved.",
  saveError: "Could not save account details. Please try again.",
  manageGuestFacingBrand: "Manage guest-facing brand",
  uploadImage: "Upload image",
} as const

export function resolveAccountWorkspaceTabId(
  raw: string | null | undefined
): AccountWorkspaceTabId {
  if (
    raw != null
    && (ACCOUNT_WORKSPACE_TAB_IDS as readonly string[]).includes(raw)
  ) {
    return raw as AccountWorkspaceTabId
  }
  return "account-details"
}

export function isAccountWorkspaceFormTab(
  tabId: AccountWorkspaceTabId
): tabId is AccountWorkspaceFormTabId {
  return (
    ACCOUNT_WORKSPACE_FORM_TAB_IDS as readonly string[]
  ).includes(tabId)
}

/** Last saved line — Figma example uses Europe/London until defaults land. */
export function formatAccountWorkspaceLastSaved(
  iso: string | null | undefined,
  timeZone = "Europe/London"
): string {
  if (iso == null || iso === "") {
    return "Last saved —"
  }

  const date = new Date(iso)
  if (Number.isNaN(date.getTime())) {
    return "Last saved —"
  }

  const datePart = new Intl.DateTimeFormat("en-GB", {
    day: "numeric",
    month: "long",
    year: "numeric",
    timeZone,
  }).format(date)

  const timePart = new Intl.DateTimeFormat("en-GB", {
    hour: "numeric",
    minute: "2-digit",
    hour12: true,
    timeZone,
  }).format(date)

  return `Last saved ${datePart} at ${timePart} · ${timeZone}`
}
