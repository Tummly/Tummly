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
  guestsJoined: number;
}

export type HomeLatestActivityFeedbackItem = {
  kind: "feedback";
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
  newThisMonth: number;
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
  overview: GuestsOverview;
  smartGroupCounts: Record<string, number>;
  rows: GuestsRow[];
}

