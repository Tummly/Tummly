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
} as const

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
