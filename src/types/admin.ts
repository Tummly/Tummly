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
  mainLocation: string;
  townCity: string;
  mainLocationPostcode: string;
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
  operatorUserId?: number | null;
  activationStatus?: "activated" | "not_activated" | null;
  activationStatusDetail?: "pending" | "active" | "expired" | null;
  activationExpiresAt?: string | null;
  activationCode?: string | null;
}

export interface AdminTrialRequestsResponse {
  success: boolean;
  data: AdminTrialRequest[];
}

export interface ExtendActivationPayload {
  days: 14 | 30;
  idempotencyKey: string;
  restaurantId: number;
}

export interface AdminPilotAccount {
  restaurantId: number;
  ownerUserId: number;
  restaurantName: string;
  ownerEmail: string;
  billingStatus: string;
  pilotStartedAtUtc: string | null;
  pilotPeriodEnd: string | null;
  extensionCount: number;
  extensionDaysTotal: number;
  canExtend: boolean;
  cannotExtendReason: string | null;
}

export interface AdminTrialReviewTransitionResponse {
  success: boolean;
  message: string;
  emailDispatched: boolean;
  emailWarning?: string | null;
  newStatus?: string;
  setupLink?: string | null;
  expiresAt?: string | null;
}
