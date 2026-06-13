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
