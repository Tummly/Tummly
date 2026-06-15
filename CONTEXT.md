# Tummly

Tummly is a restaurant guest-relationship platform. Operators capture feedback, manage offers, and run campaigns across single or multi-location hospitality businesses.

## Onboarding

**Trial Request**:
A prospective operator's application to start a guided trial, submitted from the marketing site. Requires email verification before Tummly reviews the request.
_Avoid_: Register, sign up, registration

**Account Setup**:
The post-approval flow where an invited operator creates credentials and configures their workspace, accessed via an invite token.
_Avoid_: Register, onboarding form

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
The operator's mobile number on file after Account Setup is complete. Eligible for SMS sign-in OTP without a separate phone-verification step.
_Avoid_: Verified mobile, 2FA phone

Screen inventory and Figma parity checklist: [docs/sign_in_flows.md](docs/sign_in_flows.md)
