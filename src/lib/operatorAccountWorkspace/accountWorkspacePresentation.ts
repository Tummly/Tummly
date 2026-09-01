import {
  OPERATOR_SHELL_MENU_ITEM_CLASS,
  OPERATOR_SHELL_MENU_PANEL_CLASS,
} from "@/lib/operatorHome/shellResponsivePresentation"

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

/**
 * Campaign Email / send-test From display name — same order as
 * `CampaignSenderDisplayName.Resolve` (stored → workspace → location).
 */
export function resolveCampaignSenderDisplayName(params: {
  storedSenderName: string | null | undefined
  workspaceName: string | null | undefined
  locationName: string | null | undefined
}): string {
  const sender = (params.storedSenderName ?? "").trim()
  if (sender !== "") {
    return sender
  }
  const workspace = (params.workspaceName ?? "").trim()
  if (workspace !== "") {
    return workspace
  }
  return (params.locationName ?? "").trim()
}

export const ACCOUNT_WORKSPACE_DEFAULT_COUNTRY = "United Kingdom"

/** Figma Main Bg/Subtitle — `#7c7c7c` / `--op-color-gray-550` in light and dark. */
export const ACCOUNT_WORKSPACE_PAGE_SUBTITLE_CLASS =
  "m-0 text-base font-medium leading-normal text-[var(--op-color-gray-550)]"

/**
 * Page column — fill the shell pane; 40px between header and tabs.
 * Parent shell gutters are `flex min-h-full flex-col`.
 */
export const ACCOUNT_WORKSPACE_PAGE_STACK_CLASS =
  "flex min-h-full flex-1 flex-col gap-10"

/** Tab row rule — light `#e5e5e5` / dark `#262626` via `--op-divider`. */
export const ACCOUNT_WORKSPACE_TABS_RULE_CLASS =
  "relative z-10 border-b border-op-divider"

/**
 * Break shell pane gutters so the tab rule + body wash span the main column.
 * Re-pad with {@link ACCOUNT_WORKSPACE_SHELL_PAD_X}.
 */
export const ACCOUNT_WORKSPACE_FULL_BLEED_X =
  "-mx-4 sm:-mx-6 md:-mx-8 lg:-mx-[70px]"

/** Cancel shell bottom gutter so the tab body can reach the pane edge. */
export const ACCOUNT_WORKSPACE_FULL_BLEED_BOTTOM =
  "-mb-10 lg:-mb-[70px]"

export const ACCOUNT_WORKSPACE_SHELL_PAD_X =
  "px-4 sm:px-6 md:px-8 lg:px-[70px]"

export const ACCOUNT_WORKSPACE_SHELL_PAD_BOTTOM =
  "pb-10 lg:pb-[70px]"

/**
 * Line tabs — green active underline (`after`) sits on the divider (`-mb-px`).
 * Prefer `after` over `border-b` so TabsTrigger `twMerge` does not drop the bar.
 * Override TabsTrigger `group-data-horizontal/tabs:after:bottom-[-5px]` (higher
 * specificity) so the bar is not pushed under the opaque tab body wash.
 */
export const ACCOUNT_WORKSPACE_TAB_LIST_CLASS =
  "h-auto w-full justify-start gap-2.5 rounded-none bg-transparent p-0"

export const ACCOUNT_WORKSPACE_TAB_TRIGGER_CLASS =
  "-mb-px flex-none rounded-none border-0 bg-transparent px-3.5 pt-0 pb-2.5 text-sm font-medium text-[var(--op-color-gray-550)] shadow-none after:absolute after:inset-x-0 after:z-10 after:h-0.5 after:bg-op-button-primary-background after:opacity-0 after:transition-opacity group-data-horizontal/tabs:after:bottom-0 hover:bg-transparent hover:text-foreground focus-visible:border-transparent focus-visible:ring-0 data-active:bg-transparent data-active:font-semibold data-active:text-foreground data-active:shadow-none data-active:after:opacity-100 data-[state=active]:bg-transparent data-[state=active]:font-semibold data-[state=active]:text-foreground data-[state=active]:after:opacity-100 dark:data-active:bg-transparent dark:data-[state=active]:bg-transparent"

/**
 * Tab body wash — Figma `#181818` / `--op-color-gray-996`; light uses
 * `--op-background-secondary` (`#ebebeb`).
 */
export const ACCOUNT_WORKSPACE_TAB_BODY_CLASS =
  "flex min-h-0 flex-1 flex-col gap-6 bg-op-background-secondary pt-10 dark:bg-op-color-gray-996"

/**
 * Identity card — Figma 5735:78763 fill `#1c1c1c` / `--op-color-gray-992`;
 * light uses surface primary (white).
 */
export const ACCOUNT_WORKSPACE_IDENTITY_CARD_CLASS =
  "flex flex-col gap-10 overflow-clip rounded-op-lg border border-op-card-border bg-op-surface-primary p-6 dark:bg-op-color-gray-992 dark:shadow-none"

export const ACCOUNT_WORKSPACE_IDENTITY_TITLE_CLASS =
  "m-0 text-xl font-semibold leading-normal text-op-card-title-color"

export const ACCOUNT_WORKSPACE_IDENTITY_SUBTITLE_CLASS =
  "m-0 text-sm font-medium leading-normal text-[var(--op-color-gray-550)]"

export const ACCOUNT_WORKSPACE_FIELD_HELPER_CLASS =
  "m-0 text-xs font-medium leading-4 text-[var(--op-color-gray-550)]"

/** Same label weight as Campaign Message Subject / Filter sheet fields. */
export const ACCOUNT_WORKSPACE_FIELD_LABEL_CLASS =
  "m-0 text-sm font-semibold leading-5 text-op-text-primary"

/**
 * Text inputs — Campaign Message Subject chrome
 * (`CAMPAIGN_MESSAGE_INPUT_CLASS` + `h-12`).
 */
export const ACCOUNT_WORKSPACE_TEXT_INPUT_CLASS =
  "h-12 rounded-[4px] border-op-input-border bg-transparent px-[15px] text-sm text-op-text-primary placeholder:text-op-input-placeholder dark:bg-transparent dark:disabled:bg-transparent"

/**
 * Select triggers — Figma Overlay+Border (`p-[15px]`, `rounded-[4px]`,
 * `border-op-input-border`); placeholder + chevron use input placeholder grey.
 */
export const ACCOUNT_WORKSPACE_SELECT_TRIGGER_CLASS =
  "!h-[50px] !min-h-[50px] w-full justify-between rounded-[4px] border border-op-input-border bg-transparent px-[15px] text-left text-sm font-normal text-op-text-primary shadow-none hover:bg-transparent data-placeholder:text-op-input-placeholder disabled:cursor-not-allowed disabled:bg-op-input-disabled-background disabled:opacity-100 aria-expanded:bg-transparent dark:bg-transparent dark:hover:bg-transparent dark:aria-expanded:bg-transparent dark:disabled:bg-op-input-disabled-background [&_svg]:text-op-input-placeholder"

export const ACCOUNT_WORKSPACE_IDENTITY_DIVIDER_CLASS =
  "m-0 h-0.5 w-full shrink-0 border-0 bg-op-card-border"

/** Read-only Account structure options (API labels). */
export const ACCOUNT_STRUCTURE_OPTIONS = [
  { value: "Single location", label: "Single location" },
  { value: "Multi-location", label: "Multi-location" },
] as const

/** Read-only Main operating country options (product default). */
export const MAIN_OPERATING_COUNTRY_OPTIONS = [
  { value: "United Kingdom", label: "United Kingdom" },
] as const

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
    "Set the default preferences Tummly should use across your workspace. You can still manage location-specific settings separately.",
  weekStartsOn: "Week starts on",
  defaultReportingPeriod: "Default reporting period",
  defaultCampaignSenderName: "Default campaign sender name",
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
  workspaceIdentityTitle: "Workspace identity",
  workspaceIdentitySubtitle:
    "Manage the internal identity and account structure used by authorised Tummly users.",
  workspaceName: "Workspace name",
  workspaceNameHelper:
    "The internal account name shown to authorised Tummly users. Guest-facing names are managed in Brand & Guest Form and at location level.",
  guestFacingBusinessName: "Guest-facing business name",
  guestFacingBusinessNameHelper:
    "Shown to guests by default across Guest Forms and other restaurant-branded experiences. Individual Locations can use a different display name where configured.",
  accountStructure: "Account structure",
  businessCategory: "Business category",
  businessCategoryHelper:
    "Used to tailor future templates, onboarding guidance and product recommendations. Changing the category updates future suggestions only. It does not change existing live content.",
  mainOperatingCountry: "Main operating country",
  workspaceLogo: "Workspace logo",
  workspaceLogoHelper:
    "Used to represent your restaurant inside Tummly and on guest-facing experiences.",
  uploadImage: "Upload image",
  businessIdentityTitle: "Business identity",
  businessIdentitySubtitle:
    "Record the legal and trading details used for account administration, contracts and billing.",
  businessAddressTitle: "Business address",
  businessAddressSubtitle:
    "Enter the registered or principal business address used for account administration and legal correspondence.",
  legalStructure: "Business structure",
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
  addressLine2Placeholder: "Add address line 2 - optional",
  townCity: "City or town",
  townCityPlaceholder: "City or town",
  county: "County",
  postcode: "Postcode",
  postcodePlaceholder: "Postcode",
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

/**
 * Select menus above leave-dirty / rename dialogs — operator shell panel chrome
 * (same spine as Capture / Feedback dialog selects).
 */
export const ACCOUNT_WORKSPACE_SELECT_MENU_CLASS = `${OPERATOR_SHELL_MENU_PANEL_CLASS} z-[130] gap-0 p-0`

/** Select rows — square wash, no accent pill (shell menu item). */
export const ACCOUNT_WORKSPACE_SELECT_ITEM_CLASS = `${OPERATOR_SHELL_MENU_ITEM_CLASS} h-auto w-full justify-start pr-8 text-left text-sm font-normal text-op-text-primary focus:bg-black/5 focus:text-op-text-primary dark:focus:bg-white/5 dark:focus:text-op-text-primary`

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

export type AccountWorkspacePlanStatusPresentation =
  | { kind: "link"; label: string; href: string }
  | { kind: "text"; label: string }

/** Plan status row — link when the operator may open Billing & credits tabs. */
export function resolveAccountWorkspacePlanStatusPresentation(options: {
  planStatus: string
  billingCreditsAccess: "none" | "view" | "manage"
  planSubscriptionHref: string
}): AccountWorkspacePlanStatusPresentation {
  if (
    options.billingCreditsAccess === "view"
    || options.billingCreditsAccess === "manage"
  ) {
    return {
      kind: "link",
      label: options.planStatus,
      href: options.planSubscriptionHref,
    }
  }

  return {
    kind: "text",
    label: options.planStatus,
  }
}
