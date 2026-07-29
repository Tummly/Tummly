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

/** GET /api/capture/performance — location totals for Capture KPI cards. */
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

/** Wire QR type from GET /api/capture/placements. */
export type CapturePlacementQrType =
  | "CounterCard"
  | "PackagingSticker"
  | "DeliveryInsert"
  | "WindowSticker"
  | "SmartGuest";

/** Wire status for Active / Paused placements (Archived excluded). */
export type CapturePlacementStatus = "Active" | "Paused";

export interface CapturePlacementItem {
  qrCodeId: number;
  qrType: CapturePlacementQrType;
  status: CapturePlacementStatus;
  qrLinkUrl: string;
  qrScans: number;
  feedbackSubmitted: number;
  marketingOptIns: number;
  /** Always 0 until claim events exist. */
  offerClaims: number;
  /** All-time max scan instant; null when never scanned. */
  lastScanAt: string | null;
}

/** GET /api/capture/placements — Active/Paused QR codes + windowed metrics. */
export interface CapturePlacementsResponse {
  success: boolean;
  placements: CapturePlacementItem[];
}

/** POST /api/capture/placements/:qrCodeId/(pause|resume) — status flip only. */
export interface CapturePlacementStatusMutationResponse {
  success: boolean;
  qrCodeId: number;
  status: CapturePlacementStatus;
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
  classificationStatus: ClassificationStatus;
  sentiment: FeedbackSentiment | null;
  detectedTags: string[] | null;
  locationGuestId: number | null;
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

export type FeedbackDetailsActivityEventDto = {
  kind:
    | "feedback_received"
    | "note_added"
    | "note_deleted"
    | "classification_corrected";
  at: string;
  actorDisplayName?: string | null;
  fromSentiment?: FeedbackSentiment | null;
  toSentiment?: FeedbackSentiment | null;
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

