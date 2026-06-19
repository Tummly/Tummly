# Tummly

Tummly is a restaurant guest-relationship platform. Operators capture feedback, manage offers, and run campaigns across single or multi-location hospitality businesses.

## Onboarding

**Trial Request**:
A prospective operator's application to start a guided trial, submitted from the marketing site. Requires email verification before Tummly reviews the request.
_Avoid_: Register, sign up, registration

**Account Setup**:
The post-approval flow where an invited operator creates credentials and configures their workspace, accessed via an invite token. For single-location operators, Account Setup is a three-step wizard: credentials, restaurant confirmation, then Guest Loop provisioning. The progress stepper labels these steps Account, Restaurant, and Ready — Ready is the operator-facing name for Guest Loop provisioning. For multi-location operators, Account Setup is a four-step wizard: credentials, group confirmation, location entry, then Guest Loop provisioning. The progress stepper labels these steps Account, Group, Locations, and Ready. The operator may correct their full name on the credentials step; the submitted name becomes the account holder's name on file. On the Confirm restaurant step (single-location), business category and restaurant phone are prefilled from the Trial Request but may be edited. On the Confirm group step (multi-location), group name, business category, and number of locations are prefilled from the Trial Request where available but may be edited. Multi-location Account Setup uses the same full-page shell for every wizard step, even when step content differs from single-location.
_Avoid_: Register, onboarding form

**Guest Loop provisioning**:
The final step of Account Setup (single- and multi-location) where Tummly prepares the operator's Smart Guest Link, private feedback form, and starter QR materials. The operator sees a progress animation; they are not asked to configure touchpoints, feedback tags, thank-you copy, or offers during this step.
_Avoid_: Guest Loop configuration, step-3 form, rollout configuration

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

## Guest-facing

**Smart Guest Link**:
The public URL a guest accesses by scanning a location's QR code. Today the URL is `https://tummly.com/scan/{locationId}` — built ad-hoc at QR generation time using the location's numeric primary key. No slug or per-location token is stored. The frontend handles the `/scan/{locationId}` route; the backend has no `/scan` endpoint yet.
_Avoid_: Scan URL, guest URL, public link

**Starter QR materials**:
The QR code PNG image generated for a location, encoding the Smart Guest Link. Generated on demand via `GET /api/qr/download?locationId={id}` using QRCoder (ECC level Q, 20px per module). Not pre-generated during provisioning. The download filename is `QR_{LocationName}.png`.
_Avoid_: QR code image, QR asset

**Private feedback form**:
The guest-facing form displayed when a guest visits the Smart Guest Link. Includes rating, issue tags, optional comment, guest details, and consent wording. The form configuration lives on `GuestLoopSetup` (per-restaurant, not per-location). No backend endpoint for feedback form rendering or feedback submission exists yet — the form is a frontend concern.
_Avoid_: Feedback survey, guest survey, review form

## Operator workspace

**Workspace selection**:
The post-authentication step where a multi-location operator chooses which location to work in. Triggered when the backend sets `workspaceSetupRequired` on the sign-in response. The operator sees a list of their locations (fetched from `GET /api/auth/workspaces`), picks one (`POST /api/auth/select-workspace`), and is redirected to `/multi-dashboard?location={id}`. The selected location is persisted to `localStorage["selectedLocationId"]`. Single-location operators skip this step entirely.
_Avoid_: Location picker, workspace picker

**Operator dashboard**:
The authenticated area where an operator manages their business. Single-location operators land on `/single-dashboard`; multi-location operators land on `/multi-dashboard?location={id}`. Both dashboards are currently non-functional placeholders — no API integration, no real data, no location switching UI. The admin dashboard (`/admin-dashboard`) is the only fully-built dashboard.
_Avoid_: Admin panel, control panel

## Backend provisioning

**`POST /api/auth/setup-account`**:
The primary provisioning endpoint called at the end of Account Setup. Creates a `User`, `Restaurant`, one or more `RestaurantLocation` rows, and a stub `GuestLoopSetup`. Single and multi-location operators follow the same code path — the backend loops over `dto.Locations` regardless of account type. No Smart Guest Link, QR code, or feedback form is generated during this step; those are either on-demand (QR) or frontend-only (feedback form).
_Avoid_: Complete setup, finalize account

**`GuestLoopSetup`**:
The per-restaurant configuration row created during provisioning. Holds `Touchpoints`, `FeedbackTags`, `ThankYouMessage`, offer fields (`OfferHeadline`, `OfferDetails`, `OfferExpiry`, `OfferRedemption`, `OfferUsageLimit`), and boolean flags (`SendPhysicalQrMaterials`, `AutoSendReviewRequests`). During provisioning, only the two boolean defaults are set — all offer/feedback/touchpoint fields are left NULL. The legacy `POST /api/onboarding/guest-loop` endpoint does populate these fields but is not called by the provisioning flow.
_Avoid_: Guest Loop config, rollout config
