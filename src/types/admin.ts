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
  reason: string;
  expectedExtensionCount: number;
}

export interface AdminPilotExtensionHistoryItem {
  occurredAtUtc: string;
  days: number;
  actorIdentity: string;
  /** Null for extensions made before reasons were required. */
  reason: string | null;
}

export interface AdminPilotAllowance {
  /** Credit channel: `ai`, `email` or `sms`. */
  channel: string;
  granted: number;
  used: number;
  remaining: number;
}

export interface AdminPilotAccount {
  restaurantId: number;
  ownerUserId: number;
  restaurantName: string;
  ownerEmail: string;
  billingStatus: string;
  /** Original start of the Pilot term. */
  pilotStartedAtUtc: string | null;
  /** Original end of the Pilot term. Never changes after start. */
  pilotOriginalEndUtc: string | null;
  /** Effective expiry. Moves with each extension. */
  pilotPeriodEnd: string | null;
  daysUsed: number;
  hasPaidHistory: boolean;
  extensionCount: number;
  extensionDaysTotal: number;
  extensionHistory: AdminPilotExtensionHistoryItem[];
  /** Null when the allowance balance cannot be read. */
  allowances: AdminPilotAllowance[] | null;
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
