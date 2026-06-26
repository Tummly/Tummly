# Tummly

Tummly is a restaurant guest-relationship platform. Operators capture feedback, manage offers, and run campaigns across single or multi-location hospitality businesses.

## Onboarding

**Trial Request**:
A prospective operator's application to start a guided trial, submitted from the marketing site. Requires email verification before Tummly reviews the request.
_Avoid_: Register, sign up, registration

**Operator Setup invitation**:
The email sent when a Trial Request is approved (or resent by an admin) containing the link to begin Operator Setup. Uses a single email template for approval, manual resend, and automatic reminders.
_Avoid_: Invite email, setup link email, welcome email

**Operator Setup invitation reminder**:
An automatic re-send of the Operator Setup invitation when an approved operator has not completed Operator Setup within 14 days of the last invitation. Each reminder rotates the invite token, extends the invitation window by 14 days, and uses the same email template as approval and manual resend. Repeats on that 14-day cycle until Operator Setup is complete.
_Avoid_: Drip email, nudge email, auto-resend

**Operator Setup**:
The post-approval flow where an invited operator creates credentials and configures their workspace, accessed via an invite token. For single-location operators, Operator Setup is a three-step wizard: credentials, restaurant confirmation, then Guest Loop provisioning. The progress stepper labels these steps Account, Restaurant, and Ready — Ready is the operator-facing name for Guest Loop provisioning. For multi-location operators, Operator Setup is a four-step wizard: credentials, group confirmation, location entry, then Guest Loop provisioning. The progress stepper labels these steps Account, Group, Locations, and Ready. The operator may correct their full name on the credentials step; the submitted name becomes the account holder's name on file. On the Confirm restaurant step (single-location), business category and restaurant phone are prefilled from the Trial Request but may be edited. On the Confirm group step (multi-location), group name, business category, and number of locations are prefilled from the Trial Request where available but may be edited. Multi-location Operator Setup uses the same full-page shell for every wizard step, even when step content differs from single-location.
_Avoid_: Register, onboarding form

**Guest Loop provisioning**:
The final step of Operator Setup (single- and multi-location) where Tummly prepares each location's Smart Guest Link and QR code. The operator sees a progress animation that awaits actual per-location generation of the link and QR; they are not asked to configure touchpoints, feedback tags, thank-you copy, or offers during this step. The private feedback form is standard for all locations and requires no per-location configuration. Starter QR materials remain presentational only.
_Avoid_: Guest Loop configuration, step-3 form, rollout configuration

**Guest Loop provisioning phases**:
The three ordered preparation steps shown during Guest Loop provisioning: (1) Smart Guest Link — real backend generation per location, (2) private feedback form — presentational only (standard form, no per-location configuration), (3) starter QR materials — presentational only (out of current scope). The animation awaits completion of phase 1 before advancing.
_Avoid_: Loading screen, fake progress

**Sign-in**:
Authentication for returning operators or admins, including password reset and OTP verification for user accounts.
_Avoid_: Login (acceptable in UI copy only)

**First Sign-in**:
The operator's first successful Sign-in after Operator Setup is complete — the first time they obtain a session through `/login`, not trial email verification or invite setup.
_Avoid_: First login, first visit

**Trusted device**:
A browser the operator has opted to remember for 30 days after completing OTP verification. Subsequent Sign-ins from that browser may skip OTP until trust expires.
_Avoid_: Remember me cookie, device fingerprint

**Sign-in OTP**:
The one-time code sent after password validation to confirm the operator's identity. Delivered by email by default; SMS is an alternate channel from the choose-method step.
_Avoid_: 2FA code, MFA token

**Verified phone**:
The operator's mobile number on file after Operator Setup is complete. Eligible for SMS sign-in OTP without a separate phone-verification step. For single-location Operator Setup, the restaurant phone number is required on the Confirm restaurant step and is prefilled from the Trial Request mobile number.
_Avoid_: Verified mobile, 2FA phone

**Business category**:
The operator's hospitality type (e.g. takeaway, café, pub). Chosen at Trial Request and confirmed again during Operator Setup. Canonical options: Takeaway / quick-service restaurant; Café / coffee shop; Bakery / dessert shop; Casual dining restaurant; Food truck / mobile food business; Pub / bar / hospitality venue; Multi-site restaurant group; Other.
_Avoid_: Industry, vertical, business type

## Marketing site

**Legal page**:
A public marketing-site page presenting Tummly's legal copy — Privacy Policy (`/privacy`), Terms of Service (`/terms`), or Cookie Policy (`/cookie-settings`). All three use the same long-form layout (title, description, table of contents, numbered sections). Cookie settings is informational legal prose about cookies, not an interactive preference centre. Uses the same chrome as the homepage (site navigation header and marketing footer). Accessible to visitors and signed-in operators alike; not gated behind Sign-in or blocked for signed-in users. Footer and inline nav links use short labels **Privacy**, **Terms**, and **Cookie settings**; page titles (H1) are **Privacy Policy**, **Terms of Service**, and **Cookie Policy** respectively.
_Avoid_: Legal document, policy page, compliance page, cookie preference centre

## Guest-facing

**Smart Guest Link**:
The public URL a guest accesses by scanning a location's QR code. The URL is `https://tummly.com/scan/{token}` where `{token}` is an opaque random per-location value generated during Guest Loop provisioning and stored on `RestaurantLocation`. The token is not the location's numeric primary key — it prevents link enumeration and survives location renames without invalidating printed QR codes. The frontend handles the `/scan/{token}` route; the backend resolves the token to location metadata and serves the feedback form.
_Avoid_: Scan URL, guest URL, public link

**QR code**:
The PNG image encoding a location's Smart Guest Link. The link token is generated during Guest Loop provisioning; the PNG itself is rendered on-demand when the operator first downloads it from the dashboard via `GET /api/qr/download?locationId={id}` using QRCoder (ECC level Q, 20px per module). The download filename is `QR_{LocationName}.png`.
_Avoid_: QR image, code image

**Starter QR materials**:
A formatted print-ready package containing the QR code (e.g. table tents, sticker sheets, printable PDFs) intended for physical placement in a location. Not generated today — the provisioning phase for starter materials is presentational only and out of current scope.
_Avoid_: QR pack, print materials

**Private feedback form**:
The guest-facing form displayed when a guest visits the Smart Guest Link. Standard for all locations — the same form content is served regardless of which location's QR code was scanned; only the displayed restaurant/location name differs. Captures three fields: guest name, guest contact (email or phone, single field), and a feedback message. Per-location or per-restaurant configuration of form fields is not in scope. The backend resolves location metadata (restaurant and location name) in the same response that renders the form, and accepts feedback submissions via a POST endpoint keyed by location.
_Avoid_: Feedback survey, guest survey, review form

## Operator workspace

**Workspace selection**:
The post-authentication step where a multi-restaurant operator chooses which restaurant to work in. Triggered when the backend sets `workspaceSetupRequired` on the sign-in response. The operator sees a list of their restaurants, picks one, and is redirected to that restaurant's dashboard. Single-restaurant operators (single- and multi-location alike) skip this step entirely and land directly on their dashboard. Today every operator owns one restaurant, so workspace selection is dormant — the UI and API exist but are not triggered. The fields (`workspaceSetupRequired`, `selectedRestaurantId`) are not sent by the backend until multi-restaurant ownership is introduced.
_Avoid_: Location picker, workspace picker

**Operator dashboard**:
The authenticated area where an operator manages their business. Single-location operators land on `/single-dashboard`; multi-location operators land on `/multi-dashboard` and switch between their restaurant's locations via an in-dashboard location switcher. The admin dashboard (`/admin-dashboard`) is the only fully-built dashboard.
_Avoid_: Admin panel, control panel

**Owned location**:
A RestaurantLocation whose parent Restaurant is owned by the signed-in operator (`Restaurant.OwnerUserId` matches the authenticated User). Location-scoped operator APIs keyed by `locationId` require this relationship before returning data for that location.
_Avoid_: Authorized location, location access

**Address**:
The street-level location of a RestaurantLocation, captured during Operator Setup on the field labeled "Address". UK-wide coverage. The operator may select a suggested address from lookup or choose **Use my address instead** to keep their entered text (free text). That free-text choice is the same as overriding a postcode reconciliation lock. Addresses from lookup or postcode reconciliation are stored as street plus town (postcode is stored separately in Postcode). Free-text addresses are stored exactly as entered. Selecting a suggestion auto-fills Postcode when that field is empty; if Postcode is already filled and differs from the suggestion, an inline warning is shown: "Selected Address doesn't match with postcode". Address lookup and postcode reconciliation apply on the Confirm restaurant step, each location card, and the bulk-upload review dialog.
_Avoid_: Location field, street address

**Postcode**:
The UK postcode of a RestaurantLocation, captured alongside Address during Operator Setup. Used to validate and reconcile the entered Address against postcode lookup results.
_Avoid_: ZIP code, postal code

**Address–postcode reconciliation**:
When a valid UK postcode is entered and the field loses focus, Tummly resolves it to an address and compares that result to the operator's Address. If the postcode matches but the street-level detail does not overlap, Address is replaced with the postcode lookup result and the field is locked. While locked, the Address control stays focusable: the operator opens the same async select menu and chooses **Use my address instead** to restore their entered text. When a postcode maps to multiple premises, Tummly picks the closest match to the operator's entered Address; if none are close enough, the first result is used and the operator may override. The operator can also unlock Address by changing Postcode (which re-reconciles on the next blur). Reconciliation is a client-side UX concern; the backend validates UK postcode format on submit and accepts an optional per-location override flag when the operator chose **Use my address instead**. The backend does not hard-block submit on address–postcode mismatch.
_Avoid_: Address validation, postcode check

**Address lookup cache**:
Tummly caches duplicate Ideal Postcodes requests on the backend to reduce latency and vendor cost. Autocomplete suggestions are cached in memory for one hour by normalized query string; postcode-resolution results are cached in memory for twenty-four hours by normalized postcode.
_Avoid_: Browser cache, frontend cache

## Backend provisioning

**`POST /api/auth/setup-account`**:
The primary provisioning endpoint called at the end of Operator Setup. Creates a `User`, `Restaurant`, one or more `RestaurantLocation` rows (each with a generated Smart Guest Link token), and a stub `GuestLoopSetup`. Single and multi-location operators follow the same code path — the backend loops over `dto.Locations` regardless of account type. The QR PNG is not generated during this step; it is rendered on-demand at first download. The private feedback form is standard for all locations and requires no per-location generation.
_Avoid_: Complete setup, finalize account

**`GuestLoopSetup`**:
The per-restaurant configuration row created during provisioning. Holds `Touchpoints`, `FeedbackTags`, `ThankYouMessage`, offer fields (`OfferHeadline`, `OfferDetails`, `OfferExpiry`, `OfferRedemption`, `OfferUsageLimit`), and boolean flags (`SendPhysicalQrMaterials`, `AutoSendReviewRequests`). During provisioning, only the two boolean defaults are set — all offer/feedback/touchpoint fields are left NULL. The legacy `POST /api/onboarding/guest-loop` endpoint does populate these fields but is not called by the provisioning flow.
_Avoid_: Guest Loop config, rollout config
