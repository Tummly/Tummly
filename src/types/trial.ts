export interface TrialRequestPayload {
  businessName: string;
  businessCategory: string;
  locations: string;
  businessLink?: string;
  fullName: string;
  email: string;
  mobile: string;
  role: string;
  goal: string;
  termsAccepted: boolean;
}

export interface VerifyOtpPayload {
  email: string;
  otpCode: string;
}

export interface CompleteSetupLocation {
  locationName: string;
  address: string;
  postcode?: string;
  locationPhone?: string;
  localContact?: string;
  includeInRollout: boolean;
}

export interface CompleteSetupPayload {
  token: string;
  password: string;
  confirmPassword: string;
  groupName: string;
  businessCategory: string;
  primaryPhone?: string;
  businessLink?: string;
  locations: CompleteSetupLocation[];
  rolloutApproach?: string;
  guestPrompt?: string;
  thankYouMessage?: string;
  offerType?: string;
  offerTitle?: string;
  offerMessage?: string;
  offerExpiry?: string;
  redemptionMethod?: string;
  usageLimit?: string;
}
