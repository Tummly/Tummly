# Tummly

Tummly is a restaurant guest-relationship platform. Operators capture feedback, manage offers, and run campaigns across single or multi-location hospitality businesses.

## Onboarding

**Trial Request**:
A prospective operator's application to start a guided trial, submitted from the marketing site. Requires email verification before Tummly reviews the request.
_Avoid_: Register, sign up, registration

**Account Setup**:
The post-approval flow where an invited operator creates credentials and configures their workspace, accessed via an invite token. For single-location operators, Account Setup is a three-step wizard: credentials, restaurant confirmation, then Guest Loop provisioning. The operator may correct their full name on the credentials step; the submitted name becomes the account holder's name on file. On the Confirm restaurant step, business category and restaurant phone are prefilled from the Trial Request but may be edited.
_Avoid_: Register, onboarding form

**Guest Loop provisioning**:
The final step of single-location Account Setup where Tummly prepares the operator's Smart Guest Link, private feedback form, and starter QR materials. The operator sees a progress animation; they are not asked to configure touchpoints, feedback tags, thank-you copy, or offers during this step.
_Avoid_: Guest Loop configuration, step-3 form

**Guest Loop provisioning phases**:
The three ordered preparation steps shown during Guest Loop provisioning: (1) Smart Guest Link, (2) private feedback form, (3) starter QR materials. Today these phases are presentational; in future each phase may correspond to real backend work.
_Avoid_: Loading screen, fake progress

**Sign-in**:
Authentication for returning operators or admins, including password reset and OTP verification for user accounts.
_Avoid_: Login (acceptable in UI copy only)

**First Sign-in**:
The operator's first successful Sign-in after Account Setup is complete — the first time they obtain a session through `/login`, not trial email verification or invite setup.
_Avoid_: First login, first visit

**Trusted device**:
A browser the operator has opted to remember for 30 days after completing OTP verification. Subsequent Sign-ins from that browser may skip OTP until trust expires.
_Avoid_: Remember me cookie, device fingerprint

**Sign-in OTP**:
The one-time code sent after password validation to confirm the operator's identity. Delivered by email by default; SMS is an alternate channel from the choose-method step.
_Avoid_: 2FA code, MFA token

**Verified phone**:
The operator's mobile number on file after Account Setup is complete. Eligible for SMS sign-in OTP without a separate phone-verification step. For single-location Account Setup, the restaurant phone number is required on the Confirm restaurant step and is prefilled from the Trial Request mobile number.
_Avoid_: Verified mobile, 2FA phone

**Business category**:
The operator's hospitality type (e.g. takeaway, café, pub). Chosen at Trial Request and confirmed again during Account Setup. Canonical options: Takeaway / quick-service restaurant; Café / coffee shop; Bakery / dessert shop; Casual dining restaurant; Food truck / mobile food business; Pub / bar / hospitality venue; Multi-site restaurant group; Other.
_Avoid_: Industry, vertical, business type
