export interface LocationItem {
  id: number;
  locationName: string;
  address: string;
  guestUrl: string;
  locationPhone?: string | null;
  localContact?: string | null;
  createdAt: string;
}

export interface LocationsResponse {
  success: boolean;
  locations: LocationItem[];
}

export type ContactType = "Email" | "Phone" | "Unknown";

export type ClassificationStatus = "Pending" | "Succeeded" | "Failed";

export type FeedbackSentiment = "positive" | "neutral" | "negative";

export interface FeedbackItem {
  id: number;
  guestName: string;
  guestContact: string;
  contactType: ContactType;
  comment: string;
  createdAt: string;
  classificationStatus: ClassificationStatus;
  /** Non-null only when classificationStatus is Succeeded. */
  sentiment: FeedbackSentiment | null;
  /** Non-null only when Succeeded (may be []). Null when Pending or Failed. */
  detectedTags: string[] | null;
}

export interface FeedbackResponse {
  success: boolean;
  total: number;
  recent: FeedbackItem[];
}

export interface HomePerformanceResponse {
  success: boolean;
  feedbackSubmitted: number;
  /** Equal-length window immediately before the selected range. */
  feedbackSubmittedPrevious: number;
  guestsJoined: number;
  /** Equal-length window immediately before the selected range. */
  guestsJoinedPrevious: number;
  qrScans: number;
  /** Equal-length window immediately before the selected range. */
  qrScansPrevious: number;
}

/** GET /api/feedback/summary — location Feedback summary KPIs for a half-open range. */
export interface FeedbackSummaryResponse {
  success: boolean;
  total: number;
  positive: number;
  neutral: number;
  negative: number;
  totalPrevious: number;
  positivePrevious: number;
  neutralPrevious: number;
  negativePrevious: number;
  /** Needs attention count for the same location + range (filter-independent). */
  needsAttentionTotal: number;
}

/** GET /api/feedback/inbox — location Feedback inbox page. */
export interface FeedbackInboxListItem {
  id: number;
  createdAt: string;
  comment: string;
  guestName: string;
  contactType: string;
  locationName: string;
  qrSource: string | null;
  classificationStatus: "Pending" | "Succeeded" | "Failed";
  sentiment: "positive" | "neutral" | "negative" | null;
  detectedTags: string[] | null;
  workflowStatus: FeedbackWorkflowStatus;
  needsAttention: boolean;
  locationGuestId: number | null;
}

export interface FeedbackInboxTabCounts {
  all: number;
  needsAttention: number;
  new: number;
  inProgress: number;
  resolved: number;
}

export interface FeedbackInboxDigitalGuestLink {
  id: number;
  linkName: string;
}

export interface FeedbackInboxListResponse {
  success: boolean;
  items: FeedbackInboxListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
  tabCounts: FeedbackInboxTabCounts;
  digitalGuestLinks: FeedbackInboxDigitalGuestLink[];
}

/** GET /api/capture/performance — retired; use CaptureLocationSnapshotResponse. */
export interface CapturePerformanceResponse {
  success: boolean;
  qrScans: number;
  qrScansPrevious: number;
  feedbackSubmitted: number;
  feedbackSubmittedPrevious: number;
  marketingOptIns: number;
  marketingOptInsPrevious: number;
  /** Always 0 until claim events exist. */
  offerClaims: number;
  /** False until offer-claim facts are real. */
  offerClaimsHasRealData: boolean;
}

export interface CaptureOverviewResponse {
  success: boolean;
  activeLocations: number;
  totalLocations: number;
  activeQrPlacements: number;
  qrScans: number;
  qrScansPrevious: number;
  feedbackSubmitted: number;
  feedbackSubmittedPrevious: number;
  marketingOptIns: number;
  marketingOptInsPrevious: number;
  /** Always 0 until claim events exist. */
  offerClaims: number;
  /** False until offer-claim facts are real. */
  offerClaimsHasRealData: boolean;
}

/** Capture location status on Location performance rows. */
export type CaptureLocationStatus = "Active" | "Paused";

/** Wire sort ids for GET /api/capture/locations. */
export type CaptureLocationsSortId =
  | "highest-qr-scans"
  | "highest-submission-rate"
  | "highest-marketing-opt-ins"
  | "highest-offer-claims"
  | "most-active-placements"
  | "most-recent-activity"
  | "location-name-az";

export interface CaptureLocationItem {
  locationId: number;
  locationName: string;
  status: CaptureLocationStatus;
  activePlacementsCount: number;
  /** QR ids remembered for Activate location capture; 0 when none. */
  pauseRestoreQrCodeCount: number;
  qrScans: number;
  feedbackSubmitted: number;
  marketingOptIns: number;
  /** Always 0 until claim events exist. */
  offerClaims: number;
  /** All-time max(scan, feedback) over Active+Paused QR set; null when none. */
  lastActivityAt: string | null;
}

/** GET /api/capture/locations — paginated Location performance rows. */
export interface CaptureLocationsResponse {
  success: boolean;
  items: CaptureLocationItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export type CaptureLocationsQueryParams = {
  from: string;
  to: string;
  q?: string;
  status?: CaptureLocationStatus[];
  locationIds?: number[];
  sort?: CaptureLocationsSortId;
  page?: number;
  pageSize?: number;
};

/** Wire QR type from GET /api/capture/placements. */
export type CapturePlacementQrType =
  | "CounterCard"
  | "PackagingSticker"
  | "DeliveryInsert"
  | "WindowSticker"
  | "SmartGuest"
  | "DigitalGuestLink"

/** Wire channel for Digital guest links (PascalCase enum names). */
export type CaptureDigitalGuestLinkChannel =
  | "SocialMedia"
  | "Email"
  | "WhatsApp"
  | "Website"
  | "OnlineOrdering"
  | "Other"

/** Wire status for Active / Paused placements (Archived excluded). */
export type CapturePlacementStatus = "Active" | "Paused"

/** Wire status including Archived for Archive + Detail drawer after archive. */
export type CaptureQrCodeStatus = CapturePlacementStatus | "Archived"

export interface CapturePlacementItem {
  qrCodeId: number
  qrType: CapturePlacementQrType
  status: CapturePlacementStatus
  /** Present for Digital guest links; null for catalog / Smart Guest. */
  linkName?: string | null
  /** Present for Digital guest links; null for catalog / Smart Guest. */
  channel?: CaptureDigitalGuestLinkChannel | null
  /** Optional display label for channel when the API sends one. */
  channelLabel?: string | null
  /** Operator-only internal description on the QR code. */
  internalDescription?: string | null
  createdAt?: string | null
  createdByDisplayName?: string | null
  updatedAt?: string | null
  updatedByDisplayName?: string | null
  qrLinkUrl: string
  qrScans: number
  feedbackSubmitted: number
  marketingOptIns: number
  /** Always 0 until claim events exist. */
  offerClaims: number
  /** All-time max scan instant; null when never scanned. */
  lastScanAt: string | null
}

/** GET /api/capture/placements/archived — account-wide archived codes. */
export interface CaptureArchivedPlacementItem {
  qrCodeId: number
  locationId: number
  locationName: string
  qrType: CapturePlacementQrType
  status: "Archived"
  linkName?: string | null
  channel?: CaptureDigitalGuestLinkChannel | null
  internalDescription?: string | null
  qrLinkUrl: string
  archivedAt: string | null
  archivedByDisplayName: string | null
  qrScans: number
  feedbackSubmitted: number
  lastScanAt: string | null
  canRestore: boolean
}

/** GET /api/capture/placements/archived — paged Capture Archive list (ADR-0024). */
export interface CaptureArchivedPlacementsResponse {
  success: boolean
  placements: CaptureArchivedPlacementItem[]
  totalCount: number
  page: number
  pageSize: number
  /** Distinct Archived-by names across owned archived codes (not page-scoped). */
  archiverOptions: string[]
}

/** PATCH /api/capture/placements/:qrCodeId/internal-description */
export interface CapturePlacementInternalDescriptionResponse {
  success: boolean
  qrCodeId: number
  internalDescription: string | null
  updatedAt: string
  updatedByDisplayName: string | null
}

/** POST /api/capture/placements/:qrCodeId/archive */
export interface CapturePlacementArchiveResponse {
  success: boolean
  qrCodeId: number
  status: "Archived"
  archivedAt: string
  archivedByDisplayName: string | null
}

/** POST /api/capture/placements/:qrCodeId/restore */
export interface CapturePlacementRestoreResponse {
  success: boolean
  qrCodeId: number
  status: "Paused"
  qrLinkUrl: string
}

export type CapturePlacementRestoreConflictReason =
  | "type_slot_occupied"
  | "link_name_occupied"

export type CapturePlacementRestoreErrorBody = {
  success: false
  message: string
  reason?: CapturePlacementRestoreConflictReason
}

/** GET /api/capture/placements — retired; use CaptureLocationSnapshotResponse. */
export interface CapturePlacementsResponse {
  success: boolean;
  /** Persisted Capture location status for this Owned location. */
  captureLocationStatus: CaptureLocationStatus;
  placements: CapturePlacementItem[];
  /**
   * All-time latest Feedback on any Active/Paused code at the location.
   * Null when none.
   */
  lastJourneyUpdate: {
    createdAt: string;
    guestName: string;
  } | null;
}

/** GET /api/capture/locations/:locationId/preview-options — Preview picker facts. */
export interface CapturePreviewOptionsItem {
  qrCodeId: number
  qrType: CapturePlacementQrType
  status: CapturePlacementStatus
  linkName?: string | null
}

export interface CapturePreviewOptionsResponse {
  items: CapturePreviewOptionsItem[]
}

/**
 * GET /api/capture/locations/:locationId/snapshot — Capture location snapshot
 * (KPI totals + Active/Paused rows for one Capture performance date range).
 */
export interface CaptureLocationSnapshotResponse {
  success: boolean
  captureLocationStatus: CaptureLocationStatus
  qrScans: number
  qrScansPrevious: number
  feedbackSubmitted: number
  feedbackSubmittedPrevious: number
  marketingOptIns: number
  marketingOptInsPrevious: number
  /** Always 0 until claim events exist. */
  offerClaims: number
  /** False until offer-claim facts are real. */
  offerClaimsHasRealData: boolean
  placements: CapturePlacementItem[]
  /**
   * All-time latest Feedback on any Active/Paused code at the location.
   * Null when none.
   */
  lastJourneyUpdate: {
    createdAt: string
    guestName: string
  } | null
}

/** POST /api/capture/locations/:locationId/(pause|activate). */
export interface CaptureLocationCaptureMutationResponse {
  success: boolean
  locationId: number
  status: CaptureLocationStatus
  pausedCount?: number
  activatedCount?: number
  pauseRestoreQrCodeCount: number
}

/** POST /api/capture/placements/:qrCodeId/(pause|resume) — status flip only. */
export interface CapturePlacementStatusMutationResponse {
  success: boolean;
  qrCodeId: number;
  status: CapturePlacementStatus;
}

/** POST /api/capture/placements/digital-guest-links body. */
export type CreateDigitalGuestLinkRequest = {
  linkName: string
  internalDescription?: string | null
  channel: CaptureDigitalGuestLinkChannel
  status: CapturePlacementStatus
}

/** POST /api/capture/placements/digital-guest-links success body. */
export interface CreateDigitalGuestLinkResponse {
  success: boolean
  qrCodeId: number
  qrType: "DigitalGuestLink"
  status: CapturePlacementStatus
  linkName: string
  channel: CaptureDigitalGuestLinkChannel
  internalDescription: string | null
  qrLinkUrl: string
}

/** POST create Digital guest link conflict / validation error body. */
export type CreateDigitalGuestLinkErrorBody = {
  success: false
  message: string
  field?: "linkName" | "channel" | "status" | "internalDescription"
}

/** POST /api/capture/placements/:qrCodeId/rotate — remint Token on same id. */
export interface CapturePlacementRotateResponse {
  success: boolean
  qrCodeId: number
  status: CapturePlacementStatus
  qrLinkUrl: string
}

export type HomeLatestActivityFeedbackItem = {
  kind: "feedback";
  /** Linked Location Guest when the submission created/matched one; null otherwise. */
  locationGuestId: number | null;
} & FeedbackItem;

export type HomeLatestActivityGuestJoinedItem = {
  kind: "guest-joined";
  locationGuestId: number;
  guestName: string;
  offersOptOut: boolean;
  createdAt: string;
};

export type HomeLatestActivityItem =
  | HomeLatestActivityFeedbackItem
  | HomeLatestActivityGuestJoinedItem;

export interface HomeLatestActivityResponse {
  success: boolean;
  items: HomeLatestActivityItem[];
}

export interface FeedbackDetailsResponse {
  success: boolean;
  id: number;
  guestName: string;
  guestContact: string;
  contactType: ContactType;
  comment: string;
  createdAt: string;
  locationName: string;
  address: string;
  /**
   * Operator-facing QR source label (QR type or Digital guest link Link name).
   * Null/omitted when unknown — header omits the QR segment.
   */
  qrSource?: string | null;
  classificationStatus: ClassificationStatus;
  sentiment: FeedbackSentiment | null;
  detectedTags: string[] | null;
  locationGuestId: number | null;
  /** Persisted operator follow-up lifecycle. Omitted by older fixtures → treat as new. */
  workflowStatus?: FeedbackWorkflowStatus;
  /** Derived: Succeeded Negative ∧ ≠ Resolved. Omitted by older fixtures → derive client-side. */
  needsAttention?: boolean;
  /** Newest-first Feedback internal notes (may be omitted by older fixtures). */
  internalNotes?: FeedbackInternalNoteItem[];
  /** Derived timeline; may be omitted by older fixtures. */
  activityHistory?: FeedbackDetailsActivityEventDto[];
}

export interface FeedbackInternalNoteItem {
  id: number;
  body: string;
  authorDisplayName: string;
  createdAt: string;
  /** ISO timestamp when last edited; omitted/null when never edited. */
  updatedAt?: string | null;
}

export type FeedbackWorkflowStatus = "new" | "in_progress" | "resolved";

export type FeedbackDetailsActivityEventDto = {
  kind:
    | "feedback_received"
    | "note_added"
    | "note_deleted"
    | "classification_corrected"
    | "workflow_status_changed"
    | "feedback_closed_out";
  at: string;
  actorDisplayName?: string | null;
  fromSentiment?: FeedbackSentiment | null;
  toSentiment?: FeedbackSentiment | null;
  fromWorkflowStatus?: FeedbackWorkflowStatus | null;
  toWorkflowStatus?: FeedbackWorkflowStatus | null;
  closeOutIntent?: "mark_resolved" | "mark_no_action_needed" | null;
  closeOutReason?:
    | "positive_no_follow_up"
    | "duplicate_submission"
    | "test_or_invalid"
    | "already_handled_outside"
    | "no_appropriate_follow_up"
    | "other"
    | null;
};

export interface CreateFeedbackInternalNoteResponse {
  success: boolean;
  note: FeedbackInternalNoteItem;
}

export type CorrectFeedbackClassificationRequest = {
  sentiment: FeedbackSentiment;
};

export interface CorrectFeedbackClassificationResponse {
  success: boolean;
  id: number;
  classificationStatus: ClassificationStatus;
  sentiment: FeedbackSentiment | null;
  detectedTags: string[] | null;
  activityEvent?: FeedbackDetailsActivityEventDto | null;
}

export type SetFeedbackWorkflowStatusRequest = {
  workflowStatus: FeedbackWorkflowStatus;
};

export interface SetFeedbackWorkflowStatusResponse {
  success: boolean;
  id: number;
  workflowStatus: FeedbackWorkflowStatus;
  needsAttention: boolean;
  activityEvent?: FeedbackDetailsActivityEventDto | null;
}

export type CloseOutFeedbackRequest = {
  intent: "mark_resolved" | "mark_no_action_needed";
  reason:
    | "positive_no_follow_up"
    | "duplicate_submission"
    | "test_or_invalid"
    | "already_handled_outside"
    | "no_appropriate_follow_up"
    | "other";
  noteBody?: string;
};

export interface CloseOutFeedbackResponse {
  success: boolean;
  id: number;
  workflowStatus: FeedbackWorkflowStatus;
  needsAttention: boolean;
  activityEvent: FeedbackDetailsActivityEventDto;
  noteActivityEvent?: FeedbackDetailsActivityEventDto | null;
  note?: FeedbackInternalNoteItem | null;
}

export interface ChecklistAcksResponse {
  success: boolean;
  locationId: number;
  guestFormPreviewed: boolean;
  qrPlacementGuideViewed: boolean;
  logoUploaded: boolean;
  guestFormPreviewedAt: string | null;
  qrPlacementGuideViewedAt: string | null;
  logoUploadedAt: string | null;
}

export type UpdateChecklistAcksRequest = {
  guestFormPreviewed?: boolean;
  qrPlacementGuideViewed?: boolean;
  logoUploaded?: boolean;
};

export interface GuestsOverview {
  totalGuests: number;
  /** Present once the list API returns it; mapper defaults missing to 0. */
  newThisMonth?: number;
  marketingEligible: number;
  needsRecovery: number;
}

export interface GuestsRow {
  id: string;
  name: string;
  email: string | null;
  mobile: string | null;
  marketingStatus: string;
  locationName: string;
  latestFeedbackSentiment: string;
  feedbackSubmissionCount: number;
  lastInteractionLabel: string;
  lastInteractionAt: string | null;
  capturedAt: string;
  /** Guest tag catalog ids currently on this Location Guest. */
  tagIds?: number[];
}

export interface GuestsResponse {
  success: boolean;
  locationId: number;
  smartGroup: string;
  q: string;
  sort: string;
  page: number;
  pageSize: number;
  totalFilteredCount: number;
  /**
   * Omitted when the client requests `includeAggregates=false` (table-only
   * refetches). Callers should carry forward prior overview KPIs.
   */
  overview?: GuestsOverview | null;
  /**
   * Omitted when the client requests `includeAggregates=false`. Callers should
   * carry forward prior Smart Group tab counts.
   */
  smartGroupCounts?: Record<string, number> | null;
  rows: GuestsRow[];
}

export type GuestProfileContactChannel = "email" | "sms";

export type GuestProfileContactStatus =
  | "eligible"
  | "unsubscribed"
  | "not_provided";

export type GuestProfileContactDetailKind =
  | "consent_captured"
  | "unsubscribed";

export interface GuestProfileContactEligibilityRow {
  channel: GuestProfileContactChannel;
  status: GuestProfileContactStatus;
  detailKind: GuestProfileContactDetailKind | null;
  detailAt: string | null;
}

export interface GuestProfileGuestTag {
  id: number;
  name: string;
}

export interface GuestProfileSummary {
  email: string | null;
  mobile: string | null;
  firstCapturedAt: string;
  locationName: string;
  feedbackSubmissionCount: number;
  offerClaimsAndRedemptions: number;
  lastInteractionAt: string | null;
  lastInteractionLabel: string;
  guestTags: GuestProfileGuestTag[];
}

export interface GuestProfileOverviewDetails {
  guestSinceAt: string;
  totalInteractions: number;
  feedbackReceived: number;
  offersClaimed: number;
  campaignsSent: number;
  lastActivityAt: string | null;
}

/** Capped Overview Latest feedback preview row (≤3 on detail GET). */
export interface GuestProfileLatestFeedbackItem {
  id: number;
  createdAt: string;
  comment: string;
  locationName: string;
  classificationStatus: ClassificationStatus;
  /** Non-null only when classificationStatus is Succeeded. */
  sentiment: FeedbackSentiment | null;
  /** Non-null only when Succeeded (may be []). Null when Pending or Failed. */
  detectedTags: string[] | null;
}

/** Guest-scoped Feedbacks tab list row (same classification honesty as Overview). */
export interface GuestFeedbacksListItem {
  id: number;
  createdAt: string;
  comment: string;
  locationName: string;
  classificationStatus: ClassificationStatus;
  sentiment: FeedbackSentiment | null;
  detectedTags: string[] | null;
}

export interface GuestFeedbacksListResponse {
  items: GuestFeedbacksListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

/** Guest-scoped Activity tab timeline row (Location Guest activity event store). */
export interface GuestActivityListItem {
  id: number;
  kind: string;
  occurredAt: string;
  feedbackId: number | null;
  locationName: string;
  tagName: string | null;
  guestTagId: number | null;
  authorDisplayName: string | null;
  sentiment: string | null;
  changedFields: string[] | null;
}

export interface GuestActivityListResponse {
  items: GuestActivityListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

/** Capped Overview Recent notes preview row (≤3 on detail GET). */
export interface GuestProfileRecentNoteItem {
  id: number;
  body: string;
  authorDisplayName: string;
  createdAt: string;
  /** ISO timestamp when last edited; omitted/null when never edited. */
  updatedAt?: string | null;
}

export interface GuestNotesListResponse {
  items: GuestProfileRecentNoteItem[];
  totalCount: number;
}

export interface CreateGuestNoteResponse {
  success: boolean;
  note: GuestProfileRecentNoteItem;
}

export interface PatchGuestIdentityRequest {
  firstName: string;
  lastName: string;
  email: string | null;
  phone: string | null;
}

export interface PatchGuestIdentityResponse {
  success: boolean;
  changedFields: string[];
}

export interface GuestProfileResponse {
  success: boolean;
  locationId: number;
  id: number;
  name: string;
  marketingStatus: string;
  offersOptOut: boolean;
  guestSinceAt: string;
  lastActivityAt: string | null;
  lastInteractionLabel: string;
  profileSummary: GuestProfileSummary;
  overviewDetails: GuestProfileOverviewDetails;
  contactEligibility: GuestProfileContactEligibilityRow[];
  latestFeedback: GuestProfileLatestFeedbackItem[];
  recentNotes: GuestProfileRecentNoteItem[];
}

