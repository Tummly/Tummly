export interface AdminOperatorLocation {
  locationName: string;
  address: string;
  postcode?: string | null;
  locationPhone?: string | null;
  localContact?: string | null;
}

export interface AdminTrialRequest {
  id: number;
  businessName: string;
  businessCategory: string;
  locations: string;
  businessLink?: string | null;
  fullName: string;
  email: string;
  mobile: string;
  role: string;
  goal: string;
  isEmailVerified: boolean;
  isApproved: boolean;
  isAccountCreated: boolean;
  accountType: string;
  status: string;
  createdAt: string;
  approvedAt?: string | null;
  reviewedAt?: string | null;
  reviewedBy?: string | null;
  declinedAt?: string | null;
  declineReason?: string | null;
  moreInfoRequestedAt?: string | null;
  moreInfoMessage?: string | null;
  inviteSentAt?: string | null;
  inviteExpiresAt?: string | null;
  accountCreatedAt?: string | null;
  primaryAddress?: string | null;
  primaryPostcode?: string | null;
  operatorLocations?: AdminOperatorLocation[];
}

export interface AdminTrialRequestsResponse {
  success: boolean;
  data: AdminTrialRequest[];
}

export interface UpdateTrialStatusPayload {
  trialRequestId: number;
  status: string;
  adminNotes?: string;
  moreInfoMessage?: string;
  declineReason?: string;
}
