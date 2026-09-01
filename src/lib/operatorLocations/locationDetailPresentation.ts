/** Location detail — Figma header `5748:104523`, Overview `3762:14377`, Setup `5754:104612`, Guest Loop `5754:105010`, Team access `5754:105408`, Location controls `5754:106202`. */

import type {
  LocationLifecycleStatus,
  LocationSetupStatus,
} from "@/lib/operatorLocations/locationsPresentation"
import {
  LOCATION_LIFECYCLE_LABELS,
  LOCATIONS_CARD_CLASS,
} from "@/lib/operatorLocations/locationsPresentation"
import {
  CAPTURE_PLACEMENTS_BODY_CELL_CLASS,
  CAPTURE_PLACEMENTS_BODY_ROW_CLASS,
  CAPTURE_PLACEMENTS_HEAD_CELL_CLASS,
  CAPTURE_PLACEMENTS_HEAD_ROW_CLASS,
  CAPTURE_PLACEMENTS_LAST_SCAN_CELL_CLASS,
  CAPTURE_PLACEMENTS_NAME_CELL_CLASS,
  CAPTURE_PLACEMENTS_TABLE_CLASS,
  CAPTURE_PLACEMENTS_TABLE_FRAME_CLASS,
} from "@/lib/operatorCapture/capturePresentation"
import {
  GUESTS_DETAIL_DIVIDER_CLASS,
  GUESTS_DETAIL_FIELD_CLASS,
  GUESTS_DETAIL_FIELD_LABEL_CLASS,
  GUESTS_DETAIL_FIELD_VALUE_CLASS,
  GUESTS_DETAIL_ROW_PAIR_CLASS,
  GUESTS_DETAIL_ROWS_STACK_CLASS,
  GUESTS_PAGE_ACTION_BUTTON_CLASS,
  GUESTS_SECTION_SUBTITLE_CLASS,
  GUESTS_SECTION_TITLE_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"

export {
  LOCATIONS_CARD_CLASS as LOCATION_DETAIL_CARD_CLASS,
  CAPTURE_PLACEMENTS_TABLE_FRAME_CLASS as LOCATION_DETAIL_TABLE_FRAME_CLASS,
  CAPTURE_PLACEMENTS_TABLE_CLASS as LOCATION_DETAIL_TABLE_CLASS,
  CAPTURE_PLACEMENTS_HEAD_ROW_CLASS as LOCATION_DETAIL_TABLE_HEAD_ROW_CLASS,
  CAPTURE_PLACEMENTS_HEAD_CELL_CLASS as LOCATION_DETAIL_TABLE_HEAD_CELL_CLASS,
  CAPTURE_PLACEMENTS_BODY_ROW_CLASS as LOCATION_DETAIL_TABLE_BODY_ROW_CLASS,
  CAPTURE_PLACEMENTS_BODY_CELL_CLASS as LOCATION_DETAIL_TABLE_BODY_CELL_CLASS,
  CAPTURE_PLACEMENTS_NAME_CELL_CLASS as LOCATION_DETAIL_TABLE_NAME_CELL_CLASS,
  CAPTURE_PLACEMENTS_LAST_SCAN_CELL_CLASS as LOCATION_DETAIL_TABLE_MUTED_CELL_CLASS,
  GUESTS_DETAIL_ROWS_STACK_CLASS as LOCATION_DETAIL_METRIC_STACK_CLASS,
  GUESTS_DETAIL_ROW_PAIR_CLASS as LOCATION_DETAIL_METRIC_PAIR_CLASS,
  GUESTS_DETAIL_FIELD_CLASS as LOCATION_DETAIL_METRIC_FIELD_CLASS,
  GUESTS_DETAIL_FIELD_LABEL_CLASS as LOCATION_DETAIL_METRIC_LABEL_CLASS,
  GUESTS_DETAIL_FIELD_VALUE_CLASS as LOCATION_DETAIL_METRIC_VALUE_CLASS,
  GUESTS_DETAIL_DIVIDER_CLASS as LOCATION_DETAIL_METRIC_DIVIDER_CLASS,
  GUESTS_SECTION_TITLE_CLASS as LOCATION_DETAIL_SECTION_TITLE_CLASS,
  GUESTS_SECTION_SUBTITLE_CLASS as LOCATION_DETAIL_SECTION_SUBTITLE_CLASS,
  GUESTS_PAGE_ACTION_BUTTON_CLASS as LOCATION_DETAIL_ACTION_BUTTON_CLASS,
}

export const LOCATION_DETAIL_TAB_IDS = [
  "overview",
  "setup-details",
  "guest-loop",
  "team-access",
  "location-controls",
] as const

export type LocationDetailTabId = (typeof LOCATION_DETAIL_TAB_IDS)[number]

export const LOCATION_DETAIL_TAB_LABELS: Record<LocationDetailTabId, string> = {
  overview: "Overview",
  "setup-details": "Setup & details",
  "guest-loop": "Guest Loop",
  "team-access": "Team access",
  "location-controls": "Location controls",
}

export function resolveLocationDetailTabId(
  raw: string | null | undefined
): LocationDetailTabId {
  if (
    raw === "overview"
    || raw === "setup-details"
    || raw === "guest-loop"
    || raw === "team-access"
    || raw === "location-controls"
  ) {
    return raw
  }
  return "overview"
}

export const LOCATION_DETAIL_PAGE_COPY = {
  breadcrumbLocations: "Locations",
  editLocation: "Edit location",
  createQrCode: "Create QR code",
  viewFeedback: "View feedback",
  viewGuests: "View guests",
  overviewTitle: "Overview",
  assignedQrTitle: "Assigned QR codes",
  assignedQrSubtitle:
    "Track the QR codes and Smart Guest Links used at this location.",
  offersTitle: "Offers & campaigns",
  offersSubtitle: "Review the offers and campaigns running for this location.",
  createOffer: "Create offer",
  createCampaign: "Create campaign",
  qrEmptyTitle: "No QR codes yet",
  qrEmptyHelper:
    "Create a QR code or Smart Guest Link for this location to start tracking scans.",
  offersEmptyTitle: "No offers or campaigns yet",
  offersEmptyHelper:
    "Create an offer or campaign for this location to see performance here.",
  placeholderTabBody: "This section will be available in a later release.",
  locationSetupTitle: "Location setup",
  guestActivityTitle: "Guest activity",
  guestActivitySubtitle:
    "See recent guest capture, feedback and offer activity for this location.",
  latestFeedbackTitle: "Latest feedback",
  latestFeedbackEmptyTitle: "No feedback yet",
  latestFeedbackEmptyHelper:
    "Feedback submitted at this location will appear here.",
  startRecovery: "Start recovery",
  viewGuest: "View guest",
  viewRedemptions: "View redemptions",
  teamAccessTitle: "Team access",
  teamAccessSubtitle:
    "Choose which team members can access this location's guests, feedback, QR codes and reports.",
  assignTeamMember: "Assign team member",
  teamAccessEmptyTitle: "No team members assigned",
  teamAccessEmptyHelper:
    "Assign a team member to give them access to this location.",
  locationStatusTitle: "Location status",
  dangerZoneTitle: "Danger zone",
  dangerZoneSubtitle:
    "These actions can affect QR codes, guest capture, campaigns and reporting for this location.",
  pauseLocation: "Pause location",
  resumeLocation: "Resume location",
  archiveLocation: "Archive location",
  restoreLocation: "Restore location",
  controlsUnavailable: "—",
  loadError: "Could not load this location.",
  notFound: "Location not found.",
  thisMonthSuffix: "this month",
} as const

export type LocationSetupChecklistItemId =
  | "locationDetailsAdded"
  | "qrCodePublishedLive"
  | "guestFormConnected"
  | "teamAccessAssigned"
  | "guestPrivacyNotice"
  | "firstOfferCreated"
  | "atLeastOneQrCreated"

export type LocationSetupChecklistStatusId =
  | "complete"
  | "optional"
  | "incomplete"
  | "not-started"

export const LOCATION_SETUP_CHECKLIST_LABELS: Record<
  LocationSetupChecklistItemId,
  string
> = {
  locationDetailsAdded: "Location details added",
  qrCodePublishedLive: "QR code published/live",
  guestFormConnected: "Guest form connected",
  teamAccessAssigned: "Team access assigned",
  guestPrivacyNotice: "Guest privacy notice",
  firstOfferCreated: "First offer created",
  atLeastOneQrCreated: "At least one QR code created",
}

export const LOCATION_SETUP_CHECKLIST_STATUS_LABELS: Record<
  LocationSetupChecklistStatusId,
  string
> = {
  complete: "Complete",
  optional: "Optional",
  incomplete: "Incomplete",
  "not-started": "Not started",
}

/** Left column then right column — Figma Setup & details `5754:104612`. */
export const LOCATION_SETUP_CHECKLIST_ROWS: Array<
  [LocationSetupChecklistItemId, LocationSetupChecklistItemId | null]
> = [
  ["locationDetailsAdded", "qrCodePublishedLive"],
  ["guestFormConnected", "teamAccessAssigned"],
  ["guestPrivacyNotice", "firstOfferCreated"],
  ["atLeastOneQrCreated", null],
]

export function formatLocationSetupChecklistStatus(
  status: LocationSetupChecklistStatusId
): string {
  return LOCATION_SETUP_CHECKLIST_STATUS_LABELS[status]
}

export function buildLocationSetupChecklist(input: {
  lifecycleStatus: LocationLifecycleStatus
  setupStatus: LocationSetupStatus
  managerName: string | null | undefined
  qrCount: number
  hasOffer: boolean
}): Record<LocationSetupChecklistItemId, LocationSetupChecklistStatusId> {
  const isDraft = input.lifecycleStatus === "draft"
  const managerAssigned = (input.managerName?.trim() ?? "") !== ""

  const locationDetailsAdded: LocationSetupChecklistStatusId = isDraft
    ? "not-started"
    : "complete"

  const guestFormConnected: LocationSetupChecklistStatusId = isDraft
    ? "not-started"
    : "complete"

  let qrCodePublishedLive: LocationSetupChecklistStatusId
  let guestPrivacyNotice: LocationSetupChecklistStatusId
  let atLeastOneQrCreated: LocationSetupChecklistStatusId

  if (isDraft) {
    qrCodePublishedLive = "not-started"
    guestPrivacyNotice = "not-started"
    atLeastOneQrCreated = "not-started"
  } else if (input.setupStatus === "ready") {
    qrCodePublishedLive = "complete"
    guestPrivacyNotice = "complete"
    atLeastOneQrCreated = "complete"
  } else if (input.setupStatus === "needs-attention") {
    qrCodePublishedLive = "incomplete"
    guestPrivacyNotice = "incomplete"
    atLeastOneQrCreated = input.qrCount > 0 ? "complete" : "incomplete"
  } else if (input.setupStatus === "blocked") {
    qrCodePublishedLive = "incomplete"
    guestPrivacyNotice = "incomplete"
    atLeastOneQrCreated = input.qrCount > 0 ? "complete" : "incomplete"
  } else {
    qrCodePublishedLive = "not-started"
    guestPrivacyNotice = "not-started"
    atLeastOneQrCreated = input.qrCount > 0 ? "complete" : "not-started"
  }

  return {
    locationDetailsAdded,
    qrCodePublishedLive,
    guestFormConnected,
    teamAccessAssigned: managerAssigned ? "complete" : "optional",
    guestPrivacyNotice,
    firstOfferCreated: input.hasOffer ? "complete" : "optional",
    atLeastOneQrCreated,
  }
}

export type LocationGuestActivityChecklistItemId =
  | "guestProfilesCreated"
  | "offerClaims"
  | "consentOptIns"
  | "offerRedemptions"
  | "feedbackSubmitted"
  | "unsubscribes"
  | "needsRecovery"

export type LocationGuestActivityChecklistStatusId =
  | "complete"
  | "optional"
  | "needs-action"

export const LOCATION_GUEST_ACTIVITY_CHECKLIST_LABELS: Record<
  LocationGuestActivityChecklistItemId,
  string
> = {
  guestProfilesCreated: "Guest profiles created",
  offerClaims: "Offer claims",
  consentOptIns: "Consent opt-ins",
  offerRedemptions: "Offer redemptions",
  feedbackSubmitted: "Feedback submitted",
  unsubscribes: "Unsubscribes",
  needsRecovery: "Needs recovery",
}

export const LOCATION_GUEST_ACTIVITY_CHECKLIST_STATUS_LABELS: Record<
  LocationGuestActivityChecklistStatusId,
  string
> = {
  complete: "Complete",
  optional: "Optional",
  "needs-action": "Needs action",
}

/** Left column then right column — Figma Guest Loop `5754:105010`. */
export const LOCATION_GUEST_ACTIVITY_CHECKLIST_ROWS: Array<
  [
    LocationGuestActivityChecklistItemId,
    LocationGuestActivityChecklistItemId | null,
  ]
> = [
  ["guestProfilesCreated", "offerClaims"],
  ["consentOptIns", "offerRedemptions"],
  ["feedbackSubmitted", "unsubscribes"],
  ["needsRecovery", null],
]

export function formatLocationGuestActivityChecklistStatus(
  status: LocationGuestActivityChecklistStatusId
): string {
  return LOCATION_GUEST_ACTIVITY_CHECKLIST_STATUS_LABELS[status]
}

export function buildLocationGuestActivityChecklist(input: {
  guestsCaptured: number
  optIns: number
  feedback: number
  offersClaimed: number
  offersRedeemed: number
  pendingRecoveryCount: number
  pendingFeedbackActionCount: number
}): Record<
  LocationGuestActivityChecklistItemId,
  LocationGuestActivityChecklistStatusId
> {
  const countStatus = (
    count: number
  ): LocationGuestActivityChecklistStatusId =>
    count > 0 ? "complete" : "optional"

  return {
    guestProfilesCreated: countStatus(input.guestsCaptured),
    offerClaims: countStatus(input.offersClaimed),
    consentOptIns: countStatus(input.optIns),
    offerRedemptions: countStatus(input.offersRedeemed),
    feedbackSubmitted:
      input.pendingFeedbackActionCount > 0
        ? "needs-action"
        : countStatus(input.feedback),
    unsubscribes: "optional",
    needsRecovery:
      input.pendingRecoveryCount > 0 ? "needs-action" : "complete",
  }
}

export type LocationDetailLatestFeedbackRow = {
  id: string
  feedbackId: number
  comment: string
  guestName: string
  sentiment: "positive" | "neutral" | "negative" | null
  timeLabel: string
  canStartRecovery: boolean
  locationGuestId: number | null
}

/** Latest feedback list row chrome — Figma Guest Loop `5754:105010`. */
export const LOCATION_DETAIL_LATEST_FEEDBACK_ROW_CLASS =
  "flex items-start justify-between gap-4 border-b border-op-card-border py-6 last:border-b-0"

export const LOCATION_DETAIL_LATEST_FEEDBACK_ICON_CLASS =
  "flex size-8 shrink-0 items-center justify-center rounded-full bg-op-background-secondary"

export const LOCATION_DETAIL_LATEST_FEEDBACK_ACTIONS_CLASS =
  "flex flex-wrap items-center gap-4"

export const LOCATION_DETAIL_LATEST_FEEDBACK_ACTION_CLASS =
  "h-auto min-h-0 p-0 text-sm font-medium text-op-text-primary underline-offset-4 hover:underline"

export type LocationDetailTeamAccessRow = {
  id: string
  name: string
  role: string
  accessLabel: string
  lastActiveLabel: string
}

/** Legacy manager-only stub — detail GET supplies teamAccessRows in production. */
export function buildLocationTeamAccessRows(input: {
  managerName: string | null | undefined
  managerUserId: number | null | undefined
}): LocationDetailTeamAccessRow[] {
  const managerName = input.managerName?.trim() ?? ""
  if (managerName === "") {
    return []
  }

  return [
    {
      id: String(input.managerUserId ?? "location-manager"),
      name: managerName,
      role: "Manager",
      accessLabel: "This location only",
      lastActiveLabel: "—",
    },
  ]
}

export type LocationControlsStatusFieldId =
  | "locationStatus"
  | "billingStatus"
  | "guestForm"
  | "lastScan"
  | "qrCodes"
  | "lastFeedback"
  | "privacySetup"

export const LOCATION_CONTROLS_STATUS_LABELS: Record<
  LocationControlsStatusFieldId,
  string
> = {
  locationStatus: "Location status",
  billingStatus: "Billing status",
  guestForm: "Guest form",
  lastScan: "Last scan",
  qrCodes: "QR codes",
  lastFeedback: "Last feedback",
  privacySetup: "Privacy setup",
}

/** Left column then right column — Figma Location controls `5754:106202`. */
export const LOCATION_CONTROLS_STATUS_ROWS: Array<
  [LocationControlsStatusFieldId, LocationControlsStatusFieldId | null]
> = [
  ["locationStatus", "billingStatus"],
  ["guestForm", "lastScan"],
  ["qrCodes", "lastFeedback"],
  ["privacySetup", null],
]

export type LocationControlsLifecycleActionId =
  | "pause"
  | "resume"
  | "archive"
  | "restore"

export type LocationControlsDangerAction = {
  id: LocationControlsLifecycleActionId
  label: string
  variant: "op-secondary" | "op-tertiary"
  enabled: boolean
}

function formatLocationControlsBillingStatus(
  lifecycleStatus: LocationLifecycleStatus
): string {
  switch (lifecycleStatus) {
    case "active":
    case "paused":
      return "Active"
    case "draft":
      return "Pending"
    case "archived":
      return "Inactive"
  }
}

function formatLocationControlsGuestFormStatus(
  lifecycleStatus: LocationLifecycleStatus
): string {
  switch (lifecycleStatus) {
    case "active":
      return "Live"
    case "paused":
      return "Paused"
    case "draft":
      return "Draft"
    case "archived":
      return "Archived"
  }
}

function formatLocationControlsPrivacySetupStatus(
  setupStatus: LocationSetupStatus
): string {
  switch (setupStatus) {
    case "ready":
      return "Complete"
    case "needs-attention":
    case "blocked":
      return "Incomplete"
    case "not-started":
      return "Not started"
  }
}

function formatLocationControlsQrCodesLabel(liveQrCount: number): string {
  return `${liveQrCount.toLocaleString("en-GB")} live`
}

export function buildLocationControlsStatus(input: {
  lifecycleStatus: LocationLifecycleStatus
  setupStatus: LocationSetupStatus
  liveQrCount: number
  lastScanLabel?: string | null
  lastFeedbackLabel?: string | null
  emptyLabel?: string
}): Record<LocationControlsStatusFieldId, string> {
  const emptyLabel =
    input.emptyLabel ?? LOCATION_DETAIL_PAGE_COPY.controlsUnavailable

  return {
    locationStatus: LOCATION_LIFECYCLE_LABELS[input.lifecycleStatus],
    billingStatus: formatLocationControlsBillingStatus(input.lifecycleStatus),
    guestForm: formatLocationControlsGuestFormStatus(input.lifecycleStatus),
    lastScan: input.lastScanLabel?.trim() || emptyLabel,
    qrCodes: formatLocationControlsQrCodesLabel(input.liveQrCount),
    lastFeedback: input.lastFeedbackLabel?.trim() || emptyLabel,
    privacySetup: formatLocationControlsPrivacySetupStatus(input.setupStatus),
  }
}

/** Danger-zone CTAs mirror the locations list lifecycle menu. */
export function locationControlsDangerActions(
  lifecycleStatus: LocationLifecycleStatus
): LocationControlsDangerAction[] {
  const copy = LOCATION_DETAIL_PAGE_COPY

  switch (lifecycleStatus) {
    case "active":
      return [
        {
          id: "pause",
          label: copy.pauseLocation,
          variant: "op-secondary",
          enabled: true,
        },
        {
          id: "archive",
          label: copy.archiveLocation,
          variant: "op-tertiary",
          enabled: false,
        },
      ]
    case "paused":
      return [
        {
          id: "resume",
          label: copy.resumeLocation,
          variant: "op-secondary",
          enabled: true,
        },
        {
          id: "archive",
          label: copy.archiveLocation,
          variant: "op-tertiary",
          enabled: true,
        },
      ]
    case "archived":
      return [
        {
          id: "restore",
          label: copy.restoreLocation,
          variant: "op-secondary",
          enabled: true,
        },
        {
          id: "archive",
          label: copy.archiveLocation,
          variant: "op-tertiary",
          enabled: false,
        },
      ]
    case "draft":
      return [
        {
          id: "pause",
          label: copy.pauseLocation,
          variant: "op-secondary",
          enabled: false,
        },
        {
          id: "archive",
          label: copy.archiveLocation,
          variant: "op-tertiary",
          enabled: false,
        },
      ]
  }
}

export type LocationDetailOverviewMetricId =
  | "qrScans"
  | "formStarts"
  | "feedback"
  | "guestsCaptured"
  | "optIns"
  | "offersClaimed"
  | "offersRedeemed"

export const LOCATION_DETAIL_OVERVIEW_METRIC_LABELS: Record<
  LocationDetailOverviewMetricId,
  string
> = {
  qrScans: "QR scans",
  formStarts: "Form starts",
  feedback: "Feedback",
  guestsCaptured: "Guests captured",
  optIns: "Opt-ins",
  offersClaimed: "Offers claimed",
  offersRedeemed: "Offers redeemed",
}

/** Left column then right column, matching Figma pair rows. */
export const LOCATION_DETAIL_OVERVIEW_METRIC_ROWS: Array<
  [LocationDetailOverviewMetricId, LocationDetailOverviewMetricId | null]
> = [
  ["qrScans", "optIns"],
  ["formStarts", "offersClaimed"],
  ["feedback", "offersRedeemed"],
  ["guestsCaptured", null],
]

export type LocationDetailQrRow = {
  id: string
  name: string
  placement: string
  statusLabel: string
  scans: string
  starts: string
  submissions: string
  optIns: string
  claims: string
  lastScannedLabel: string
}

export type LocationDetailOfferCard = {
  id: string
  kind: "campaign" | "offer"
  statusLabel: string
  title: string
  meta: string
  primaryCta: string
  secondaryCta: string
}

export function formatLocationDetailHeaderMeta(input: {
  city: string
  qrCount: number
  guestCount: number
}): string {
  const city = input.city.trim() || "—"
  const qrLabel =
    input.qrCount === 1 ? "1 QR code" : `${input.qrCount} QR codes`
  const guestLabel =
    input.guestCount === 1
      ? "1 guest captured"
      : `${input.guestCount.toLocaleString("en-GB")} guests captured`
  return `${city} · ${qrLabel} · ${guestLabel}`
}

export function formatLocationDetailMonthMetric(value: number): string {
  return `${value.toLocaleString("en-GB")} ${LOCATION_DETAIL_PAGE_COPY.thisMonthSuffix}`
}

export type LocationDetailHeaderModel = {
  locationId: number
  name: string
  city: string
  lifecycleStatus: LocationLifecycleStatus
  headerMeta: string
}

export function buildEmptyOverviewMetrics(): Record<
  LocationDetailOverviewMetricId,
  number
> {
  return {
    qrScans: 0,
    formStarts: 0,
    feedback: 0,
    guestsCaptured: 0,
    optIns: 0,
    offersClaimed: 0,
    offersRedeemed: 0,
  }
}
