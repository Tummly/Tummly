/** Guest preference centre copy — Figma 7191:814, 7185:392, 7194:1073. */
export const GUEST_PREFERENCES_COPY = {
  title: "Manage your Tummly marketing preferences",
  lead: "Manage how you receive Tummly dining news and offers, and update your restaurant preferences.",
  entryLine: "Enter your email address or UK mobile number to continue.",
  linkExpired:
    "This link has expired. Enter your email address or UK mobile number to continue.",
  contactLinked: "Email confirmed. Go back to your preferences tab.",
  contactInputLabel: "Email address or UK mobile number",
  continueLabel: "Continue",
  contactInvalid: "Enter a valid email address or UK mobile number.",
  requestFailed: "We could not send that. Try again.",
  codeWrong: "That code is not correct or has expired.",
  codeFailed: "We could not check that code. Try again.",
  verifyEmailTitle: "Verify your Email address",
  verifyEmailBody: (maskedEmail: string) =>
    `We sent a confirmation link to ${maskedEmail}. Open the link to confirm the Email address.`,
  resendLinkLabel: "Resend link",
  changeEmailLabel: "Change Email",
  verifyMobileEntryTitle: "Verify your mobile number",
  verifyMobileAddTitle: "Verify your new mobile number",
  verifyMobileBody: "Enter the six-digit code sent to your number.",
  verificationCodeLabel: "Six-digit code",
  verifyLabel: "Verify",
  resendCodeLabel: "Resend code",
  changeNumberLabel: "Change number",

  detailsTitle: "Your details",
  firstNameLabel: "First name",
  editLabel: "Edit",
  saveNameLabel: "Save",
  verifiedContactLabel: "Verified contact",
  addContactLabel: "Add another contact method",

  channelsTitle: "How Tummly contacts you",
  emailLabel: "Email marketing",
  emailDescription: "Receive dining news and offers by Email.",
  smsLabel: "SMS marketing",
  smsDescription: "Receive dining news and offers by text.",

  restaurantsTitle: "Restaurant preferences",
  restaurantsDescription:
    "Choose which restaurants you’d prefer not to hear about.",
  manageRestaurantsLabel: "Manage restaurants",
  backLabel: "Back",
  restaurantsLead:
    "Choose which restaurants or Locations may feature in the Tummly marketing messages you receive.",
  restaurantSearchPlaceholder: "Search by restaurant name or location",
  restaurantsEmptyLabel: "No restaurants yet.",
  restaurantsNoMatchLabel: "No restaurants match your search.",
  /** `count` is the number of Locations under one brand. */
  brandLocationCount: (count: number) =>
    count === 1 ? "1 Location" : `${count} Locations`,
  locationMuted: "Muted",
  locationNotMuted: "Not muted",
  muteLabel: "Mute",
  unmuteLabel: "Unmute",
  muteAllBrandLocationsLabel: "Mute all brand locations",
  unmuteAllBrandLocationsLabel: "Unmute all brand locations",

  savePreferencesLabel: "Save preferences",
  stopAllLabel: "Stop all Tummly marketing",

  stopAllDialogTitle: "Stop all Tummly marketing?",
  stopAllDialogBody:
    "You’ll stop receiving Tummly marketing emails and texts. Replies about your feedback won’t be affected.",
  stopAllKeepLabel: "Keep my preferences",
  stopAllConfirmLabel: "Stop all Tummly marketing",

  preferencesSaved: "Preferences saved.",
  saveFailed: "We could not save your changes. Try again.",
  loadFailed: "We could not load your preferences. Try again.",
  tryAgainLabel: "Try again",

  legalNavLabel: "Legal",
  termsLabel: "Terms & Conditions",
  privacyLabel: "Privacy Notice",
} as const
