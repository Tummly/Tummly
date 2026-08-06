/**
 * Capture presentation — single shell, multi root, multi nested shell, shared body section chrome.
 * Desktop Figma: single `3438:40498`, multi root `3889:19648`, multi nested `3889:45672`.
 */

import {
  OPERATOR_SHELL_MENU_ITEM_CLASS,
  OPERATOR_SHELL_MENU_PANEL_CLASS,
  OPERATOR_TABLE_ROW_ACTIONS_ITEM_CLASS,
  OPERATOR_TABLE_ROW_ACTIONS_PANEL_CLASS,
  OPERATOR_TABLE_ROW_ACTIONS_SEPARATOR_CLASS,
} from "@/lib/operatorHome/shellResponsivePresentation"

export const CAPTURE_PAGE_STACK_CLASS = "flex flex-col gap-5"

export const CAPTURE_PAGE_HEADER_ROW_CLASS =
  "flex flex-col items-start justify-between gap-4 lg:flex-row lg:items-start"

export const CAPTURE_PAGE_HEADER_COPY_CLASS =
  "flex min-w-0 flex-1 flex-col gap-3.5 leading-[0]"

export const CAPTURE_PAGE_TITLE_CLASS =
  "m-0 text-2xl font-bold leading-10 text-op-card-title-color sm:text-[32px]"

export const CAPTURE_PAGE_SUBTITLE_CLASS =
  "m-0 text-base font-medium leading-5 text-op-text-muted"

export const CAPTURE_NESTED_SUBTITLE_CLASS =
  "m-0 text-sm font-medium leading-normal text-op-text-muted"

export const CAPTURE_PAGE_ACTIONS_CLASS =
  "flex shrink-0 flex-wrap items-center gap-3"

export const CAPTURE_PAGE_ACTION_BUTTON_CLASS = "shrink-0 disabled:opacity-50"

export const CAPTURE_BREADCRUMB_NAV_CLASS =
  "flex flex-wrap items-center gap-2.5 text-base font-medium"

export const CAPTURE_BREADCRUMB_LINK_CLASS =
  "text-op-text-primary hover:text-op-text-primary/90"

export const CAPTURE_BREADCRUMB_MUTED_LINK_CLASS =
  "text-op-text-muted hover:text-op-text-primary"

export const CAPTURE_BREADCRUMB_CURRENT_CLASS = "text-op-text-muted"

export const CAPTURE_SECTION_CLASS =
  "flex flex-col gap-6 overflow-clip rounded-op-lg border border-op-card-border bg-op-card-background p-4 sm:gap-8 sm:p-5 md:gap-10 md:p-6 dark:shadow-none"

export const CAPTURE_SECTION_HEADER_CLASS =
  "flex flex-col gap-2"

/** Capture performance header — title/description + date control. */
export const CAPTURE_PERFORMANCE_HEADER_ROW_CLASS =
  "flex flex-col items-start justify-between gap-4 sm:flex-row sm:items-center"

export const CAPTURE_SECTION_TITLE_CLASS =
  "m-0 text-lg font-bold leading-normal text-op-card-title-color sm:text-xl"

export const CAPTURE_SECTION_SUBTITLE_CLASS =
  "m-0 max-w-[437px] text-op-sm font-medium leading-5 text-op-card-subtitle-color"

export const CAPTURE_SECTION_PLACEHOLDER_CLASS =
  "min-h-[120px] rounded-op-md bg-op-background-secondary"

/** Guest experience summary rows — label left, value right. */
export const CAPTURE_GUEST_EXPERIENCE_ROWS_CLASS =
  "flex w-full flex-col gap-[18px]"

export const CAPTURE_GUEST_EXPERIENCE_ROW_CLASS =
  "flex w-full items-center justify-between gap-4 text-base"

export const CAPTURE_GUEST_EXPERIENCE_LABEL_CLASS =
  "shrink-0 font-semibold leading-normal text-op-text-muted"

export const CAPTURE_GUEST_EXPERIENCE_VALUE_CLASS =
  "min-w-0 text-right font-medium leading-normal text-op-text-primary"

export const CAPTURE_GUEST_EXPERIENCE_ACTIONS_CLASS =
  "flex flex-wrap items-center gap-3"

/** QR materials inner card — page-bg fill inside the section card. */
export const CAPTURE_MATERIALS_INNER_CARD_CLASS =
  "flex max-w-full flex-col gap-[29px] overflow-clip rounded-op-md bg-op-background-primary p-[18px]"

export const CAPTURE_MATERIALS_INNER_COPY_CLASS =
  "flex w-full flex-col gap-1.5"

export const CAPTURE_MATERIALS_INNER_TITLE_CLASS =
  "m-0 text-base font-medium leading-[22px] text-op-text-primary"

export const CAPTURE_MATERIALS_INNER_HELPER_CLASS =
  "m-0 text-xs font-medium leading-4 text-op-text-muted"

export const CAPTURE_MATERIALS_ACTIONS_CLASS =
  "flex flex-wrap items-center gap-3"

/** Guest experience preview overlay — full-viewport in-app chrome. */
export const CAPTURE_GUEST_PREVIEW_OVERLAY_CLASS =
  "fixed inset-0 z-[130] flex flex-col overflow-y-auto bg-op-surface-primary text-op-text-primary"

export const CAPTURE_GUEST_PREVIEW_HEADER_CLASS =
  "flex shrink-0 items-start justify-between gap-6 px-6 py-6"

export const CAPTURE_GUEST_PREVIEW_HEADER_COPY_CLASS =
  "flex min-w-0 flex-col gap-6"

/** Title → subtitle — Figma 8px. */
export const CAPTURE_GUEST_PREVIEW_TITLE_STACK_CLASS = "flex flex-col gap-2"

export const CAPTURE_GUEST_PREVIEW_TITLE_CLASS =
  "m-0 text-2xl font-semibold leading-normal text-op-card-title-color"

export const CAPTURE_GUEST_PREVIEW_SUBTITLE_CLASS =
  "m-0 text-sm font-medium leading-normal text-op-text-muted"

/** Meta columns (Location / QR / Guest form / Connected offer) — 12px. */
export const CAPTURE_GUEST_PREVIEW_META_ROW_CLASS =
  "flex flex-wrap items-start gap-3"

export const CAPTURE_GUEST_PREVIEW_META_ITEM_CLASS =
  "flex flex-col gap-1.5"

export const CAPTURE_GUEST_PREVIEW_META_LABEL_CLASS =
  "text-sm font-medium leading-normal text-op-text-muted"

export const CAPTURE_GUEST_PREVIEW_META_VALUE_CLASS =
  "text-sm font-medium leading-normal text-op-text-primary"

export const CAPTURE_GUEST_PREVIEW_HEADER_ACTIONS_CLASS =
  "flex shrink-0 items-center gap-3"

export const CAPTURE_GUEST_PREVIEW_BODY_CLASS =
  "flex flex-1 flex-col rounded-t-[20px] border-t border-[var(--op-color-gray-200)] bg-op-background-primary"

export const CAPTURE_GUEST_PREVIEW_TOOLBAR_CLASS =
  "flex shrink-0 flex-wrap items-center justify-between gap-4 px-6 pt-8"

/**
 * Figma `4267:63923` — card track, 12px pad, 6px radius.
 * Light border #E5E5E5 (`gray-200`). Override TabsList height/pad defaults.
 */
export const CAPTURE_GUEST_PREVIEW_PAGE_TABS_LIST_CLASS =
  "h-auto gap-0 rounded-op-lg border border-[var(--op-color-gray-200)] bg-op-surface-primary p-3 text-[var(--op-color-gray-550)] group-data-horizontal/tabs:h-auto"

/**
 * Figma Plan chips — shrink-wrap + 12px pad (not equal-flex TabsTrigger defaults).
 */
export const CAPTURE_GUEST_PREVIEW_PAGE_TAB_TRIGGER_CLASS =
  "h-auto flex-none cursor-pointer rounded-op-md border-transparent px-3 py-3 text-sm font-medium text-[var(--op-color-gray-550)] shadow-none after:hidden hover:text-[var(--op-color-gray-550)] data-active:bg-op-background-secondary data-active:text-op-text-primary data-active:shadow-none dark:text-[var(--op-color-gray-550)] dark:hover:text-[var(--op-color-gray-550)] dark:data-active:border-transparent dark:data-active:bg-op-background-secondary dark:data-active:text-op-text-primary"

/** Freestanding Desktop/Mobile row — no gap, no shared border (Figma `3934:74524`). */
export const CAPTURE_GUEST_PREVIEW_DEVICE_GROUP_CLASS =
  "gap-0 rounded-none border-0 bg-transparent p-0 shadow-none"

/** Figma device buttons — 10px top/bottom, 16px left/right (beat ToggleGroup spacing=0 + icon pad). */
export const CAPTURE_GUEST_PREVIEW_DEVICE_ITEM_CLASS =
  "h-auto min-w-0 cursor-pointer gap-2 rounded-op-sm px-[16px] py-[10px] has-data-[icon=inline-start]:pl-[16px] has-data-[icon=inline-end]:pr-[16px] group-data-[spacing=0]/toggle-group:px-[16px] group-data-[spacing=0]/toggle-group:py-[10px] group-data-[spacing=0]/toggle-group:has-data-[icon=inline-start]:pl-[16px] group-data-[spacing=0]/toggle-group:has-data-[icon=inline-end]:pr-[16px] text-sm font-medium text-[var(--op-color-gray-850)] shadow-none hover:bg-transparent hover:text-[var(--op-color-gray-850)] data-[state=on]:bg-transparent data-[state=on]:text-op-text-primary data-[state=on]:shadow-none [&_svg:not([class*='size-'])]:size-[length:var(--op-icon-md)]"

export const CAPTURE_GUEST_PREVIEW_CANVAS_CLASS =
  "px-6 pb-8 pt-8"

/**
 * Figma `4855:103174` Feedback form — guest preview frame.
 * #141414 fill, 6px #171717 border, 12px radius, clip overflow.
 */
export const CAPTURE_GUEST_PREVIEW_FRAME_CLASS =
  "overflow-clip rounded-[12px] border-[6px] border-solid border-[var(--op-color-gray-1000)] bg-guest-feedback-bg"

/** Mobile device preview — 393px wide, full viewport tall (phone column). */
export const CAPTURE_GUEST_PREVIEW_MOBILE_FRAME_CLASS =
  "mx-auto flex min-h-dvh w-full max-w-[393px] flex-col"

/** Keep shell at least one viewport tall in preview (overrides content collapse). */
export const CAPTURE_GUEST_PREVIEW_SHELL_CLASS = "min-h-dvh"

/**
 * Force GuestFeedbackShell onto its mobile layout inside a wide viewport
 * (shell breakpoints are viewport-based).
 */
export const CAPTURE_GUEST_PREVIEW_SHELL_MOBILE_CONTENT_CLASS =
  "max-w-[min(100%,393px)] pb-6 pt-[clamp(4.5rem,14vw,5.125rem)] sm:max-w-[min(100%,393px)] sm:pb-6 sm:pt-[clamp(4.5rem,14vw,5.125rem)] md:max-w-[min(100%,393px)] md:pb-6 lg:max-w-[min(100%,393px)]"

/** Empty body inside Capture performance (header + date remain). */
export const CAPTURE_PERFORMANCE_EMPTY_BODY_CLASS =
  "flex min-h-[291px] w-full flex-col items-center justify-center"

/** Placements section header — title/description + Add CTA when table has rows. */
export const CAPTURE_PLACEMENTS_HEADER_ROW_CLASS =
  "flex flex-col items-start justify-between gap-4 sm:flex-row sm:items-center"

export const CAPTURE_PLACEMENTS_EMPTY_BODY_CLASS =
  "flex min-h-[291px] w-full flex-col items-center justify-center gap-[30px]"

export const CAPTURE_PLACEMENTS_EMPTY_COPY_STACK_CLASS =
  "flex flex-col items-center gap-2.5 text-center"

/** Figma table header fill — gray-150 light / gray-985 dark. */
export const CAPTURE_PLACEMENTS_TABLE_FRAME_CLASS =
  "overflow-x-auto rounded-[2px] border border-op-card-border"

export const CAPTURE_PLACEMENTS_TABLE_CLASS =
  "w-full border-collapse text-sm"

export const CAPTURE_PLACEMENTS_HEAD_ROW_CLASS =
  "border-0 hover:bg-transparent"

export const CAPTURE_PLACEMENTS_HEAD_CELL_CLASS =
  "h-[43px] border border-op-border-default bg-[var(--op-color-gray-150)] px-[18px] py-3 text-left align-middle text-sm font-semibold leading-[19px] whitespace-nowrap text-op-text-primary dark:bg-[var(--op-color-gray-985)]"

export const CAPTURE_PLACEMENTS_HEAD_ACTIONS_CELL_CLASS =
  "h-[43px] border border-op-border-default bg-[var(--op-color-gray-150)] px-[18px] py-3 text-center align-middle text-sm font-semibold leading-[19px] whitespace-nowrap text-op-text-primary dark:bg-[var(--op-color-gray-985)]"

export const CAPTURE_PLACEMENTS_BODY_ROW_CLASS =
  "border-0 hover:bg-transparent"

export const CAPTURE_PLACEMENTS_BODY_CELL_CLASS =
  "border border-op-border-default px-[18px] py-3 align-middle text-sm font-normal leading-[19px] text-op-text-primary"

export const CAPTURE_PLACEMENTS_NAME_CELL_CLASS =
  "border border-op-border-default px-[18px] py-3 align-middle text-sm font-semibold leading-[19px] text-op-text-primary"

export const CAPTURE_PLACEMENTS_LAST_SCAN_CELL_CLASS =
  "border border-op-border-default px-[18px] py-3 align-middle text-sm font-normal leading-[19px] text-op-text-muted"

export const CAPTURE_PLACEMENTS_ACTIONS_CELL_CLASS =
  "w-[88px] border border-op-border-default px-6 py-3 text-center align-middle"

/** Row kebab trigger — pair with `variant="op-ghost"` (no hover wash, per design system). */
export const CAPTURE_PLACEMENT_ROW_ACTIONS_TRIGGER_CLASS = "size-8"

/**
 * Table row ⋮ Actions panel — Figma `4213:61228` (white card + dividers).
 * Dialog Select menus keep shell chrome separately below.
 */
export const CAPTURE_PLACEMENT_ROW_ACTIONS_MENU_CLASS = `${OPERATOR_TABLE_ROW_ACTIONS_PANEL_CLASS} min-w-[257px]`

export const CAPTURE_PLACEMENT_ROW_ACTIONS_ITEM_CLASS =
  OPERATOR_TABLE_ROW_ACTIONS_ITEM_CLASS

export const CAPTURE_PLACEMENT_ROW_ACTIONS_SEPARATOR_CLASS =
  OPERATOR_TABLE_ROW_ACTIONS_SEPARATOR_CLASS

/**
 * Portaled Select menus inside Capture dialogs — Account/shell chrome;
 * `z-[130]` sits above Dialog (`z-[120]`).
 */
export const CAPTURE_DIALOG_SELECT_MENU_CLASS = `${OPERATOR_SHELL_MENU_PANEL_CLASS} min-w-40 gap-0 px-0 py-1 z-[130] p-0`

/** Select group — flush with shell panel (override default SelectGroup padding). */
export const CAPTURE_DIALOG_SELECT_GROUP_CLASS = "p-0"

/** Select section label inside shell menus. */
export const CAPTURE_DIALOG_SELECT_LABEL_CLASS =
  "px-3 py-2 text-xs font-medium text-op-text-muted"

/**
 * Select option row — shell item chrome; hide check indicator; selected uses
 * primary text like Account / CaptureLocationControl.
 */
export const CAPTURE_DIALOG_SELECT_ITEM_CLASS = [
  OPERATOR_SHELL_MENU_ITEM_CLASS,
  "pr-3 focus:bg-black/5 focus:text-inherit dark:focus:bg-white/5",
  "data-[state=checked]:bg-transparent data-[state=checked]:font-medium data-[state=checked]:text-primary",
  "data-[state=checked]:focus:bg-transparent data-[state=checked]:focus:text-primary",
  "data-[state=checked]:hover:bg-transparent data-[state=checked]:hover:text-primary",
  "[&>span.absolute]:hidden",
].join(" ")

export const CAPTURE_EMPTY_SHELL_CLASS =
  "flex min-h-[291px] flex-1 flex-col items-center justify-center rounded-op-lg border border-op-card-border bg-op-card-background p-6"

export const CAPTURE_EMPTY_TITLE_CLASS =
  "m-0 text-base font-medium leading-normal text-op-empty-title-color"

export const CAPTURE_EMPTY_HELPER_CLASS =
  "m-0 max-w-[450px] text-center text-sm font-medium leading-[18px] text-op-text-muted"

/**
 * Capture performance KPI strip — Figma `4855:100088`.
 * Continuous row with vertical dividers (not separate page-bg cards).
 */
export const CAPTURE_KPI_STRIP_CLASS = "w-full"

export const CAPTURE_KPI_ROW_CLASS =
  "grid grid-cols-1 gap-6 sm:grid-cols-2 lg:flex lg:flex-row lg:items-center lg:gap-[30px]"

export const CAPTURE_KPI_DIVIDER_CLASS =
  "hidden h-[76px] w-px shrink-0 self-center bg-op-card-border lg:block"

export const CAPTURE_KPI_CELL_CLASS = "flex min-w-0 flex-1 flex-col"

export const CAPTURE_KPI_CONTENT_CLASS =
  "flex min-w-0 w-full flex-col items-stretch gap-0.5 pb-[4.25px]"

/** Six Capture overview KPI cells — multi Capture root strip (same chrome as performance). */
export const CAPTURE_OVERVIEW_KPI_ROW_CLASS =
  "grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-3 xl:flex xl:flex-row xl:items-center xl:gap-[30px]"

export const CAPTURE_OVERVIEW_KPI_DIVIDER_CLASS =
  "hidden h-[76px] w-px shrink-0 self-center bg-op-card-border xl:block"

export const OPERATOR_CAPTURE_SINGLE_COPY = {
  title: "Capture",
  description:
    "Manage the QR placements and Smart Guest Links used to collect private feedback and consented guest details at this location.",
} as const

export const OPERATOR_CAPTURE_MULTI_ROOT_COPY = {
  title: "Capture",
  description:
    "Manage the QR placements and Smart Guest Links used to collect private feedback and consented guest details across your locations.",
} as const

export const OPERATOR_CAPTURE_MULTI_SECTION_COPY = {
  overview: {
    title: "Capture overview",
    description:
      "See how QR activity and guest conversion are performing across all locations.",
    emptyTitle: "No locations yet",
    emptyHelper:
      "Add a location to start tracking QR placements and capture performance across your venues.",
  },
  locationPerformance: {
    title: "Location performance",
    description:
      "Compare QR activity and guest conversion across your locations.",
    emptyTitle: "No locations yet",
    emptyHelper:
      "Add a location to start tracking QR placements and capture performance across your venues.",
    noResultsTitle: "No locations found",
    noResultsHelper:
      "Try changing your search or removing some filters.",
  },
} as const

/**
 * Multi Capture Location performance row ⋯ catalog order — grilling 11.
 * Pause/Activate location capture mutations land in ticket 22 (chrome/stub here).
 */
export const OPERATOR_CAPTURE_LOCATION_ROW_ACTION_DEFS = [
  {
    id: "view-location-capture",
    label: "View location capture",
  },
  {
    id: "create-digital-guest-link",
    label: "Create digital guest link",
  },
  {
    id: "preview-guest-experience",
    label: "Preview guest experience",
  },
  {
    id: "order-print-materials",
    label: "Order print materials",
  },
  {
    id: "pause-location-capture",
    label: "Pause location capture",
  },
  {
    id: "activate-location-capture",
    label: "Activate location capture",
  },
] as const

export type OperatorCaptureLocationRowActionId =
  (typeof OPERATOR_CAPTURE_LOCATION_ROW_ACTION_DEFS)[number]["id"]

/** @deprecated Prefer building per-row actions via the Multi Capture page module. */
export const OPERATOR_CAPTURE_LOCATION_ROW_ACTIONS =
  OPERATOR_CAPTURE_LOCATION_ROW_ACTION_DEFS.map((action) => ({
    ...action,
    enabled:
      action.id === "view-location-capture"
      || action.id === "create-digital-guest-link",
  }))

export const OPERATOR_CAPTURE_NESTED_COPY = {
  description:
    "Manage capture performance, guest experience and QR placements for this location.",
} as const

export const OPERATOR_CAPTURE_BREADCRUMB_COPY = {
  capture: "Capture",
  allLocations: "All locations",
} as const

export const OPERATOR_CAPTURE_SECTION_COPY = {
  performance: {
    title: "Capture performance",
    description:
      "See how guests move from opening a guest form to submitting feedback, joining your guest list and claiming an offer.",
    emptyTitle: "No capture activity yet",
    emptyHelper:
      "Place your QR materials to begin collecting guest form opens and guest responses.",
  },
  guestExperience: {
    title: "Guest experience",
    description:
      "Review the forms and offers currently connected to QR placements at this location.",
    guestFormsLabel: "Guest forms",
    qrPlacementsLabel: "QR placements",
    connectedOffersLabel: "Connected offers",
    needsAttentionLabel: "Needs attention",
    lastJourneyUpdateLabel: "Last journey update",
    previewCta: "Preview guest experience",
    manageGuestFormsCta: "Manage guest forms",
    viewOffersCta: "View offers",
  },
  placements: {
    title: "QR placements",
    description: "Track where each QR code is used and how guests respond.",
    emptyTitle: "No QR placements yet",
    emptyHelper:
      "Add your first placement to generate a tracked QR code for this location.",
    addCta: "Add QR placement",
  },
  digitalGuestLinks: {
    title: "Digital guest links",
    description:
      "Create and track digital links that can be shared across your online channels.",
    emptyTitle: "No digital guest links yet",
    emptyHelper:
      "Create a digital guest link to share across your online channels. Performance for each link will show up here.",
    createCta: "Create digital guest link",
  },
  materials: {
    title: "QR materials",
    description: "Order printed QR materials for this location.",
    printedTitle: "Printed materials",
    printedHelper:
      "Order professionally printed QR materials for your restaurant.",
    orderCta: "Order print materials",
    viewOrdersCta: "View orders",
  },
} as const

export const OPERATOR_CAPTURE_DIGITAL_GUEST_LINKS_COLUMNS = {
  guestLink: "Guest link",
  status: "Status",
  qrScans: "Guest form opens",
  feedbackSubmitted: "Feedback submitted",
  marketingOptIns: "Marketing opt-ins",
  offerClaims: "Offer claims",
  lastScan: "Last scan",
  actions: "Actions",
} as const

export const OPERATOR_CAPTURE_CREATE_DIGITAL_GUEST_LINK_COPY = {
  title: "Create digital guest link",
  description:
    "Create tracked links for digital channels like social media, email, WhatsApp, your website or online ordering pages.",
  linkNameLabel: "Link name",
  linkNamePlaceholder: "Enter link name",
  linkNameRequired: "Link name is required.",
  linkNameMax: "Link name must be at most 100 characters.",
  linkNameDuplicate:
    "A digital guest link with this name already exists at this location.",
  internalDescriptionLabel: "Internal description",
  internalDescriptionPlaceholder: "Enter internal description",
  internalDescriptionMax:
    "Internal description must be at most 500 characters.",
  channelLabel: "Where will you use it?",
  channelPlaceholder: "Select channel",
  channelRequired: "Select a channel.",
  guestFormLabel: "What should guests see after opening the link?",
  guestFormValue: "Default guest feedback form",
  connectedOfferLabel: "Connected offer",
  connectedOfferPlaceholder: "Select offer - optional",
  statusLabel: "Status",
  statusPlaceholder: "Select status",
  locationLabel: "Locations",
  locationPlaceholder: "Select location",
  locationRequired: "Select a location.",
  submitCta: "Create guest link",
  cancelCta: "Cancel",
  successToast: "Digital guest link created",
  failureToast: "Could not create digital guest link. Please try again.",
  linkNameMaxLength: 100,
  internalDescriptionMaxLength: 500,
} as const

export const DIGITAL_GUEST_LINK_CHANNEL_OPTIONS = [
  { value: "SocialMedia", label: "Social media" },
  { value: "Email", label: "Email" },
  { value: "WhatsApp", label: "WhatsApp" },
  { value: "Website", label: "Website" },
  { value: "OnlineOrdering", label: "Online ordering" },
  { value: "Other", label: "Other" },
] as const

export const DIGITAL_GUEST_LINK_STATUS_OPTIONS = [
  { value: "Active", label: "Active" },
  { value: "Paused", label: "Paused" },
] as const

export const OPERATOR_CAPTURE_DIGITAL_GUEST_LINK_ROW_ACTIONS = {
  viewDetails: "View details",
  preview: "Preview",
  pause: "Pause",
  activate: "Activate",
  copyLink: "Copy link",
  archive: "Archive",
} as const

export const OPERATOR_CAPTURE_HEADER_ACTIONS_COPY = {
  orderPrintMaterials: "Order print materials",
  createDigitalGuestLink: "Create digital guest link",
  previewGuestExperience: "Preview guest experience",
  archivedPlacements: "Archived QR placements",
  editGuestForm: "Edit guest form",
} as const

/** Stub Guest form name until per-QR form product exists. */
export const CAPTURE_GUEST_FORM_STUB = "Default guest form" as const

export const OPERATOR_CAPTURE_GUEST_PREVIEW_COPY = {
  title: "Guest experience preview",
  description:
    "See what guests will experience after scanning this QR placement.",
  locationLabel: "Location",
  placementLabel: "QR placement",
  guestFormLabel: "Guest form",
  guestFormValue: CAPTURE_GUEST_FORM_STUB,
  connectedOfferLabel: "Connected offer",
  editGuestFormCta: "Edit guest form",
  openPreviewInNewTabCta: "Open preview in new tab",
  feedbackPageTab: "Feedback page",
  thankYouPageTab: "Thank you page",
  desktopDevice: "Desktop",
  mobileDevice: "Mobile",
  closeLabel: "Close",
} as const

export const CAPTURE_GUEST_PREVIEW_PAGE_TAB = {
  feedback: "feedback",
  thankYou: "thank-you",
} as const

export const CAPTURE_GUEST_PREVIEW_DEVICE = {
  desktop: "desktop",
  mobile: "mobile",
} as const

export const OPERATOR_CAPTURE_PLACEMENTS_COLUMNS = {
  placement: "Placement",
  status: "Status",
  qrScans: "Guest form opens",
  feedbackSubmitted: "Feedback submitted",
  marketingOptIns: "Marketing opt-ins",
  offerClaims: "Offer claims",
  lastScan: "Last scan",
  actions: "Actions",
} as const

/** Placement Detail drawer — Figma `3889:28072`. */
export const OPERATOR_CAPTURE_PLACEMENT_DETAIL_COPY = {
  editGuestFormCta: "Edit guest form",
  previewGuestExperienceCta: "Preview guest experience",
  copyGuestLink: "Copy guest link",
  rotateQrCode: "Rotate QR code",
  archivePlacement: "Archive placement",
  orderPrintMaterials: "Order print materials",
  performanceTitle: "Performance",
  guestFormOpensLabel: "Guest form opens:",
  feedbackSubmittedLabel: "Feedback submitted:",
  marketingOptInsLabel: "Marketing opt-ins:",
  offerClaimsLabel: "Offer claims:",
  submissionRateLabel: "Submission rate:",
  lastScanLabel: "Last scan:",
  statusLabel: "Status:",
  connectedGuestFormLabel: "Connected guest form:",
  createdLabel: "Created:",
  lastUpdatedLabel: "Last updated:",
  connectedOfferLabel: "Connected offer:",
  whereUsedLabel: "Where will you use it?",
  internalDescriptionTitle: "Internal description",
  addNoteCta: "Add note",
  viewDetails: "View details",
  moreActionsLabel: "More placement actions",
  closeLabel: "Close",
} as const

/** Rotate confirm dialogue — Figma `4252:60151`. */
export const OPERATOR_CAPTURE_ROTATE_CONFIRM_COPY = {
  title: "Rotate QR code?",
  description:
    "Rotating this QR code will deactivate the current code and create a new one. Any printed materials using the current QR code will stop working and must be replaced.",
  placementLabel: "Placement:",
  locationLabel: "Location:",
  currentStatusLabel: "Current status:",
  lastScanLabel: "Last scan:",
  acknowledgment:
    "I understand that existing printed materials using this QR code will stop working.",
  confirmCta: "Rotate QR code",
  cancelCta: "Cancel",
  successToast:
    "QR code rotated. Old code is no longer active. Order new print materials.",
} as const

/** Preview picker — Figma `4439:54464` layout; copy from grilling 10. */
export const OPERATOR_CAPTURE_GUEST_PREVIEW_PICKER_COPY = {
  title: "Select a placement or digital guest link",
  description:
    "Choose the physical QR placement or digital guest link you want to preview. Each may use a different guest form or offer.",
  fieldLabel: "Placements & digital links",
  placeholder: "Select placement or link",
  confirmCta: "Preview selected",
  cancelCta: "Cancel",
} as const

/** Preview picker shell — same surface tokens as Create digital guest link. */
export const CAPTURE_GUEST_PREVIEW_PICKER_DIALOG_CONTENT_CLASS =
  "gap-[60px] rounded-op-md bg-op-surface-secondary p-8 text-op-text-primary sm:max-w-[560px]"

export const CAPTURE_GUEST_PREVIEW_PICKER_DIALOG_TITLE_CLASS =
  "pr-0 text-2xl font-bold tracking-normal text-op-text-primary"

export const CAPTURE_GUEST_PREVIEW_PICKER_DIALOG_DESCRIPTION_CLASS =
  "max-w-[376px] text-sm font-medium leading-[18px] text-op-text-muted"

export const CAPTURE_GUEST_PREVIEW_PICKER_FIELD_LABEL_CLASS =
  "text-sm font-semibold leading-5 text-op-text-primary"

/** Select trigger — shared with Create digital guest link field chrome. */
export const CAPTURE_DIALOG_FIELD_TRIGGER_CLASS =
  "h-auto min-h-[50px] w-full rounded border-op-input-border bg-transparent px-[15px] py-[15px] text-sm text-op-text-primary shadow-none data-placeholder:text-op-text-muted dark:bg-transparent dark:hover:bg-transparent"

/**
 * Capture dialog / drawer close — use `variant="op-collapse"` (Figma Close:
 * 42px / p-3 / 18px icon / collapse fill). Place in-header with
 * {@link CAPTURE_DIALOG_HEADER_ROW_CLASS}; do not use Dialog’s absolute
 * `top-9 right-9` close on `p-8` surfaces.
 */
export const CAPTURE_DIALOG_CLOSE_BUTTON_CLASS = "shrink-0"

/** Title + description | close — Figma header row `gap-[22px]` / `items-start`. */
export const CAPTURE_DIALOG_HEADER_ROW_CLASS =
  "flex items-start gap-[22px]"

export const CAPTURE_PLACEMENT_DETAIL_SECTION_CLASS =
  "flex flex-col gap-5 border-t border-op-border-default p-[22px]"

/** Pause / Activate confirm — Figma `4252:61096` / `4252:61700`. */
export const CAPTURE_PAUSE_ACTIVATE_DIALOG_CONTENT_CLASS =
  "gap-[60px] rounded-op-md bg-op-surface-secondary p-8 text-op-text-primary sm:max-w-[567px]"

export const CAPTURE_PAUSE_ACTIVATE_DIALOG_TITLE_CLASS =
  "pr-0 text-2xl font-bold leading-normal tracking-normal text-op-text-primary"

export const CAPTURE_PAUSE_ACTIVATE_DIALOG_BODY_CLASS =
  "text-base font-medium leading-[22px] tracking-normal text-op-text-muted"

export const CAPTURE_PAUSE_ACTIVATE_DIALOG_ROW_CLASS =
  "flex w-full items-center justify-between gap-4"

export const CAPTURE_PAUSE_ACTIVATE_DIALOG_ROW_LABEL_CLASS =
  "shrink-0 text-base font-medium text-op-text-primary"

export const CAPTURE_PAUSE_ACTIVATE_DIALOG_ROW_VALUE_CLASS =
  "min-w-0 text-right text-sm font-medium text-op-text-secondary"

export const CAPTURE_PAUSE_ACTIVATE_DIALOG_WARNING_CLASS =
  "rounded-op-md bg-[var(--op-capture-pause-warning-background)] p-[18px] text-base font-medium leading-[22px] text-[var(--op-capture-pause-warning-text)]"

export const CAPTURE_PAUSE_ACTIVATE_DIALOG_FOOTER_CLASS =
  "flex flex-row flex-wrap items-center justify-start gap-3"

/** Longer success toast after Pause / Activate (Figma `4252:62908`). */
export const CAPTURE_PAUSE_ACTIVATE_TOAST_DURATION_MS = 8_000

/** Archive screen — Figma `4282:65211` / empty `4285:66947`. */
export const OPERATOR_CAPTURE_ARCHIVE_COPY = {
  breadcrumbCapture: "Capture",
  title: "Archived QR placements",
  description:
    "View historical performance and manage QR placements that are no longer active.",
  backToCapture: "Back to Capture",
  searchPlaceholder: "Search archived placements",
  filtersLabel: "Filters",
  emptyTitle: "No archived placements",
  emptyHelper:
    "QR placements you archive will appear here with their historical performance.",
  noMatchTitle: "No matching archived placements",
  noMatchHelper: "Try clearing filters or search to see archived placements.",
  clearFilters: "Clear filters",
  columns: {
    placement: "Placement",
    location: "Location",
    archivedOn: "Archived on",
    archivedBy: "Archived by",
    qrScans: "QR scans",
    feedbackSubmitted: "Feedback submitted",
    lastScan: "Last scan",
    actions: "Actions",
  },
  rowActions: {
    viewDetails: "View details",
    restore: "Restore",
    restoreDisabled: "Restore unavailable — type or link name already in use",
    duplicateAsNew: "Duplicate as new",
  },
} as const
