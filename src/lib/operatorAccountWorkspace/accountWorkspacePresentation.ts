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

export const LEGAL_STRUCTURE_OPTIONS = [
  { value: "sole-trader", label: "Sole trader" },
  { value: "partnership", label: "Partnership" },
  { value: "limited-company", label: "Limited company (Ltd)" },
  { value: "llp", label: "LLP" },
  { value: "plc", label: "PLC" },
  { value: "other", label: "Other" },
] as const

export type LegalStructureValue =
  (typeof LEGAL_STRUCTURE_OPTIONS)[number]["value"]

export const WEEK_STARTS_ON_OPTIONS = [
  { value: "monday", label: "Monday" },
  { value: "tuesday", label: "Tuesday" },
  { value: "wednesday", label: "Wednesday" },
  { value: "thursday", label: "Thursday" },
  { value: "friday", label: "Friday" },
  { value: "saturday", label: "Saturday" },
  { value: "sunday", label: "Sunday" },
] as const

export type WeekStartsOnValue =
  (typeof WEEK_STARTS_ON_OPTIONS)[number]["value"]

export const DEFAULT_REPORTING_PERIOD_OPTIONS = [
  { value: "7days", label: "7 days" },
  { value: "30days", label: "30 days" },
  { value: "thisMonth", label: "This month" },
] as const

export type DefaultReportingPeriodValue =
  (typeof DEFAULT_REPORTING_PERIOD_OPTIONS)[number]["value"]

export function normalizeWeekStartsOn(
  value: string | null | undefined
): WeekStartsOnValue {
  const trimmed = (value ?? "").trim().toLowerCase()
  const match = WEEK_STARTS_ON_OPTIONS.find(
    (option) => option.value === trimmed
  )
  return match?.value ?? "monday"
}

export function normalizeReportingPeriod(
  value: string | null | undefined
): DefaultReportingPeriodValue {
  const trimmed = (value ?? "").trim()
  const match = DEFAULT_REPORTING_PERIOD_OPTIONS.find(
    (option) => option.value.toLowerCase() === trimmed.toLowerCase()
  )
  return match?.value ?? "7days"
}

export const ACCOUNT_WORKSPACE_DEFAULT_COUNTRY = "United Kingdom"

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
  businessDetailsSaveSuccess: "Business details saved.",
  businessDetailsSaveError:
    "Could not save business details. Please try again.",
  keyContactsSaveSuccess: "Key contacts saved.",
  keyContactsSaveError: "Could not save key contacts. Please try again.",
  workspaceDefaultsSaveSuccess: "Workspace defaults saved.",
  workspaceDefaultsSaveError:
    "Could not save workspace defaults. Please try again.",
  workspaceDefaultsTitle: "Workspace defaults",
  workspaceDefaultsSubtitle:
    "Set restaurant-wide defaults used for weekly briefs, recommendations and campaign email.",
  weekStartsOn: "Week starts on",
  defaultReportingPeriod: "Default reporting period",
  defaultCampaignSenderName: "Default Campaign Sender Name",
  defaultCampaignSenderNameHelper:
    "Shown as the From display name on Campaign Email and Campaign send test.",
  defaultTimezone: "Default timezone",
  defaultCurrency: "Default currency",
  defaultLanguage: "Default language",
  dateFormat: "Date format",
  primaryResponsibilitiesTitle: "Primary responsibilities",
  primaryResponsibilitiesSubtitle:
    "Choose authorised users who should own account, billing, privacy and support responsibilities.",
  accountOwner: "Account owner",
  billingContact: "Billing contact",
  privacyContact: "Privacy contact",
  supportContact: "Support contact",
  selectUserPlaceholder: "Select user",
  keyContactsTeamHelper:
    "More people appear here after they join in Team & permissions.",
  manageGuestFacingBrand: "Manage guest-facing brand",
  uploadImage: "Upload image",
  businessIdentityTitle: "Business identity",
  businessIdentitySubtitle:
    "Record the legal and trading details used for account administration, contracts and billing.",
  businessAddressTitle: "Business address",
  businessAddressSubtitle:
    "Enter the registered or principal business address used for account administration and legal correspondence.",
  legalStructure: "Legal structure",
  legalStructurePlaceholder: "Select",
  legalBusinessName: "Legal business name",
  legalBusinessNameHelper:
    "The registered person or organisation responsible for this Tummly account.",
  tradingName: "Trading name",
  tradingNameHelper:
    "The business name commonly used with customers, if different from the legal name.",
  sameAsLegalBusinessName: "Same as legal business name",
  companyNumber: "Company number",
  companyNumberHelper:
    "The official registration number for the legal business, where applicable.",
  vatNumber: "VAT number",
  vatNumberHelper: "Used for billing and tax records where applicable.",
  countryOfRegistration: "Country of registration",
  countryOfRegistrationHelper:
    "The country where the legal business is registered.",
  addressLine1: "Address line 1",
  addressLine2: "Address line 2",
  townCity: "Town or city",
  county: "County",
  postcode: "Postcode",
  country: "Country",
  ukPostcodeError: "Enter a valid UK postcode.",
  accountControlsStatusTitle: "Account status",
  accountControlsStatusSubtitle:
    "Review the current status of your Tummly workspace.",
  viewBilling: "View billing",
  viewActivity: "View activity",
  dataOwnershipTitle: "Data ownership",
  dataOwnershipSubtitle:
    "Your restaurant owns its identifiable guest data.",
  dataOwnershipBody:
    "Tummly helps you collect, manage and use guest feedback, consent records, offers and campaign activity for this workspace. Tummly does not sell guest contact details or share identifiable guest data with other restaurants.",
  viewPrivacySettings: "View privacy settings",
  exportGuestData: "Export guest data",
  exportGuestDataTitle: "Export guest data",
  exportGuestDataSubtitle:
    "Download identifiable guest profiles and consent records for every location in this workspace.",
  exportGuestDataFileFormat: "File format",
  exportGuestDataFormatExcel: "Excel (.xlsx)",
  exportGuestDataFormatCsv: "CSV (.csv)",
  exportGuestDataDownload: "Download",
  exportGuestDataPreparing: "Preparing download…",
  exportGuestDataSuccess: "Guest data exported.",
  exportGuestDataError: "Could not export guest data. Please try again.",
  dangerZoneTitle: "Danger zone",
  dangerZoneSubtitle:
    "These actions can affect access, guest forms, campaigns and account data. Only account owners can make these changes.",
  dangerZoneBody:
    "Pause takes effect immediately after you confirm. Transfer ownership, account export, and account closure create a Support request. They do not change the account until Support completes the work.",
  dangerZoneOwnerOnlyHelper:
    "Only the account owner can make these changes.",
  pauseWorkspace: "Pause workspace",
  resumeWorkspace: "Resume workspace",
  pauseTitle: "Pause workspace?",
  pauseBody:
    "This sets Workspace status to Paused. Guest forms, Campaigns, Offers, and later billing for this Restaurant stop. You can still open Account & workspace to resume.",
  resumeTitle: "Resume workspace?",
  resumeBody:
    "This sets Workspace status to Active and starts guest forms and outbound product work again.",
  pauseSuccess: "Workspace paused.",
  resumeSuccess: "Workspace resumed.",
  pauseError: "Could not pause the workspace. Please try again.",
  resumeError: "Could not resume the workspace. Please try again.",
  transferOwnership: "Transfer ownership",
  requestAccountExport: "Request account export",
  requestAccountClosure: "Request account closure",
  transferOwnershipTitle: "Transfer ownership?",
  transferOwnershipBody:
    "This sends a Support request. It does not change the Account owner now. Tummly Support will contact you to complete the transfer.",
  transferOwnershipConfirm: "Transfer ownership",
  requestAccountExportTitle: "Request account export?",
  requestAccountExportBody:
    "This sends a Support request for a full account data package. It does not download guest data now. Use Export guest data for a self-serve guest file.",
  requestAccountExportConfirm: "Request account export",
  requestAccountClosureTitle: "Request account closure?",
  requestAccountClosureBody:
    "This sends a Support request to close the account. It does not pause or delete this workspace now.",
  requestAccountClosureConfirm: "Request account closure",
  transferOwnershipSuccess: "Ownership transfer request sent.",
  requestAccountExportSuccess: "Account export request sent.",
  requestAccountClosureSuccess: "Account closure request sent.",
  accountRequestError: "Could not send the Support request. Please try again.",
  accountRequestAlreadyOpen:
    "This request is already open.",
  accountRequestViewThread: "View thread",
} as const

export type AccountRequestKindKey =
  | "TransferOwnership"
  | "AccountExport"
  | "AccountClosure"

export function accountRequestConfirmLabels(kind: AccountRequestKindKey): {
  title: string
  body: string
  primaryLabel: string
} {
  switch (kind) {
    case "TransferOwnership":
      return {
        title: ACCOUNT_WORKSPACE_PAGE_COPY.transferOwnershipTitle,
        body: ACCOUNT_WORKSPACE_PAGE_COPY.transferOwnershipBody,
        primaryLabel: ACCOUNT_WORKSPACE_PAGE_COPY.transferOwnershipConfirm,
      }
    case "AccountExport":
      return {
        title: ACCOUNT_WORKSPACE_PAGE_COPY.requestAccountExportTitle,
        body: ACCOUNT_WORKSPACE_PAGE_COPY.requestAccountExportBody,
        primaryLabel: ACCOUNT_WORKSPACE_PAGE_COPY.requestAccountExportConfirm,
      }
    case "AccountClosure":
      return {
        title: ACCOUNT_WORKSPACE_PAGE_COPY.requestAccountClosureTitle,
        body: ACCOUNT_WORKSPACE_PAGE_COPY.requestAccountClosureBody,
        primaryLabel: ACCOUNT_WORKSPACE_PAGE_COPY.requestAccountClosureConfirm,
      }
  }
}

/** Select menus above leave-dirty / rename dialogs on this page. */
export const ACCOUNT_WORKSPACE_SELECT_MENU_CLASS = "z-[130]"

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

export function defaultAccountWorkspaceCountry(
  value: string | null | undefined
): string {
  if (value == null || value.trim() === "") {
    return ACCOUNT_WORKSPACE_DEFAULT_COUNTRY
  }
  return value
}

export function isUnitedKingdomCountry(value: string): boolean {
  return (
    value.trim().toLowerCase() === ACCOUNT_WORKSPACE_DEFAULT_COUNTRY.toLowerCase()
  )
}
