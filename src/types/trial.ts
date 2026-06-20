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
}

export interface CompleteSetupPayload {
  token: string;
  fullName: string;
  password: string;
  confirmPassword: string;
  groupName: string;
  businessCategory: string;
  primaryPhone?: string;
  businessLink?: string;
  locations: CompleteSetupLocation[];
  touchpoints?: string;
  feedbackTags?: string;
  guestPrompt?: string;
  thankYouMessage?: string;
  offerType?: string;
  offerTitle?: string;
  offerMessage?: string;
  offerExpiry?: string;
  redemptionMethod?: string;
  usageLimit?: string;
}
