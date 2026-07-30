/**
 * Capture presentation — single shell, multi root, multi nested shell, shared body section chrome.
 * Desktop Figma: single `3438:40498`, multi root `3889:19648`, multi nested `3889:45672`.
 */

import {
  OPERATOR_SHELL_MENU_ITEM_CLASS,
  OPERATOR_SHELL_MENU_PANEL_CLASS,
} from "@/lib/operatorHome/shellResponsivePresentation"

export const CAPTURE_PAGE_STACK_CLASS = "flex flex-col gap-5"

export const CAPTURE_PAGE_HEADER_ROW_CLASS =
  "flex flex-col items-start justify-between gap-4 lg:flex-row lg:items-start"

export const CAPTURE_PAGE_HEADER_COPY_CLASS =
  "flex max-w-[516px] flex-col gap-3.5 leading-[0]"

export const CAPTURE_PAGE_TITLE_CLASS =
  "m-0 text-2xl font-bold leading-10 text-op-card-title-color sm:text-[32px]"

export const CAPTURE_PAGE_SUBTITLE_CLASS =
  "m-0 text-base font-medium leading-5 text-op-text-muted"

export const CAPTURE_NESTED_SUBTITLE_CLASS =
  "m-0 text-sm font-medium leading-normal text-op-text-muted"

export const CAPTURE_PAGE_ACTIONS_CLASS =
  "flex shrink-0 flex-wrap items-center gap-3"

export const CAPTURE_PAGE_ACTION_BUTTON_CLASS = "disabled:opacity-50"

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
  "fixed inset-0 z-[130] flex flex-col overflow-hidden bg-op-background-primary text-op-text-primary"

export const CAPTURE_GUEST_PREVIEW_HEADER_CLASS =
  "flex shrink-0 items-start justify-between gap-6 border-b border-op-border-default px-6 py-6"

export const CAPTURE_GUEST_PREVIEW_HEADER_COPY_CLASS =
  "flex min-w-0 flex-col gap-6"

export const CAPTURE_GUEST_PREVIEW_TITLE_CLASS =
  "m-0 text-2xl font-semibold leading-normal text-op-card-title-color"

export const CAPTURE_GUEST_PREVIEW_SUBTITLE_CLASS =
  "m-0 text-sm font-medium leading-normal text-op-text-muted"

export const CAPTURE_GUEST_PREVIEW_META_ROW_CLASS =
  "flex flex-wrap items-start gap-8"

export const CAPTURE_GUEST_PREVIEW_META_ITEM_CLASS =
  "flex flex-col gap-1.5"

export const CAPTURE_GUEST_PREVIEW_META_LABEL_CLASS =
  "text-sm font-medium leading-normal text-op-text-muted"

export const CAPTURE_GUEST_PREVIEW_META_VALUE_CLASS =
  "text-sm font-medium leading-normal text-op-text-primary"

export const CAPTURE_GUEST_PREVIEW_HEADER_ACTIONS_CLASS =
  "flex shrink-0 items-center gap-3"

export const CAPTURE_GUEST_PREVIEW_BODY_CLASS =
  "flex min-h-0 flex-1 flex-col overflow-hidden"

export const CAPTURE_GUEST_PREVIEW_TOOLBAR_CLASS =
  "flex shrink-0 flex-wrap items-center justify-between gap-4 px-6 pt-8"

export const CAPTURE_GUEST_PREVIEW_PAGE_TABS_LIST_CLASS =
  "h-auto gap-0 rounded-op-md border border-op-border-default bg-transparent p-1"

export const CAPTURE_GUEST_PREVIEW_PAGE_TAB_TRIGGER_CLASS =
  "h-9 rounded-[calc(var(--radius-op-md)-2px)] px-3 text-sm font-medium text-op-text-muted data-active:bg-op-surface-secondary data-active:text-op-text-primary data-active:shadow-none dark:data-active:border-transparent dark:data-active:bg-op-surface-secondary"

export const CAPTURE_GUEST_PREVIEW_DEVICE_GROUP_CLASS =
  "gap-0 rounded-op-md border border-op-border-default p-1"

export const CAPTURE_GUEST_PREVIEW_DEVICE_ITEM_CLASS =
  "h-9 rounded-[calc(var(--radius-op-md)-2px)] px-3 text-sm font-medium text-op-text-muted hover:bg-transparent hover:text-op-text-muted data-[state=on]:bg-op-surface-secondary data-[state=on]:text-op-text-primary"

export const CAPTURE_GUEST_PREVIEW_CANVAS_CLASS =
  "min-h-0 flex-1 overflow-y-auto px-6 pb-8 pt-8"

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
 * Row actions panel — same shell chrome as Account / Performance date /
 * Guests filter-sort-actions menus and {@link CaptureLocationControl}.
 */
export const CAPTURE_PLACEMENT_ROW_ACTIONS_MENU_CLASS = `${OPERATOR_SHELL_MENU_PANEL_CLASS} min-w-40 gap-0 px-0 py-1`

export const CAPTURE_PLACEMENT_ROW_ACTIONS_ITEM_CLASS = OPERATOR_SHELL_MENU_ITEM_CLASS

export const CAPTURE_EMPTY_SHELL_CLASS =
  "flex min-h-[291px] flex-1 flex-col items-center justify-center rounded-op-lg border border-op-card-border bg-op-card-background p-6"

export const CAPTURE_EMPTY_TITLE_CLASS =
  "m-0 text-base font-medium leading-normal text-op-text-primary"

export const CAPTURE_EMPTY_HELPER_CLASS =
  "m-0 max-w-[450px] text-center text-sm font-medium leading-[18px] text-op-text-muted"

/** Five Capture KPI cards — Figma individual cells with page-bg fill. */
export const CAPTURE_KPI_STRIP_CLASS = "w-full"

export const CAPTURE_KPI_ROW_CLASS =
  "grid grid-cols-1 gap-2.5 sm:grid-cols-2 lg:grid-cols-5"

/** Six Capture overview KPI cards — multi Capture root strip. */
export const CAPTURE_OVERVIEW_KPI_ROW_CLASS =
  "grid grid-cols-1 gap-2.5 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-6"

export const CAPTURE_KPI_CELL_CLASS =
  "flex min-w-0 items-center justify-between gap-3 rounded-op-md bg-op-background-primary p-5"

export const CAPTURE_KPI_CONTENT_CLASS =
  "flex min-w-0 flex-col items-start gap-0.5 pb-[4.25px]"

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
 * Multi Capture Location performance row ⋯ — Figma `3889:19648` annotations.
 * Pause location capture is deferred (not live in this slice).
 */
export const OPERATOR_CAPTURE_LOCATION_ROW_ACTIONS = [
  {
    id: "view-location-capture",
    label: "View location capture",
    enabled: true,
  },
  {
    id: "add-qr-placement",
    label: "Add QR placement",
    enabled: false,
  },
  {
    id: "preview-guest-experience",
    label: "Preview guest experience",
    enabled: false,
  },
  {
    id: "order-print-materials",
    label: "Order print materials",
    enabled: false,
  },
] as const

export type OperatorCaptureLocationRowActionId =
  (typeof OPERATOR_CAPTURE_LOCATION_ROW_ACTIONS)[number]["id"]

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

export const OPERATOR_CAPTURE_HEADER_ACTIONS_COPY = {
  addPlacement: "Add QR placement",
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

export const CAPTURE_PLACEMENT_DETAIL_SECTION_CLASS =
  "flex flex-col gap-5 border-t border-op-border-default p-[22px]"
