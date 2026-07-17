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
  detectedIssues: string[] | null;
}

export interface FeedbackResponse {
  success: boolean;
  total: number;
  recent: FeedbackItem[];
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
  detectedIssues: string[] | null;
}

export type CorrectFeedbackClassificationRequest = {
  sentiment: FeedbackSentiment;
};

export interface CorrectFeedbackClassificationResponse {
  success: boolean;
  id: number;
  classificationStatus: ClassificationStatus;
  sentiment: FeedbackSentiment | null;
  detectedIssues: string[] | null;
}

export interface ChecklistAcksResponse {
  success: boolean;
  locationId: number;
  guestFormPreviewed: boolean;
  qrPlacementGuideViewed: boolean;
  guestFormPreviewedAt: string | null;
  qrPlacementGuideViewedAt: string | null;
}

export type UpdateChecklistAcksRequest = {
  guestFormPreviewed?: boolean;
  qrPlacementGuideViewed?: boolean;
};

