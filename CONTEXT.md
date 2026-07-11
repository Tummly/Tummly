# Tummly

Tummly is a restaurant guest-relationship platform. Operators capture feedback, manage offers, and run campaigns across single or multi-location hospitality businesses.

## Onboarding

**Trial Request**:
A prospective operator's application to start a guided trial, submitted from the marketing site. Requires email verification before Tummly reviews the request. Requires a **Main location** address.
_Avoid_: Register, sign up, registration

**Main location**:
The operator's primary venue address captured on the Trial Request form (field label: **Main location**). Required. UK-wide coverage. Shown in Operator details during trial request review for admin use only — it does not prefill Operator Setup. Distinct from **Address** on a RestaurantLocation, which is captured later during Operator Setup. When chosen from address lookup, **Main location** holds the street-level detail including town (e.g. `42 High Street, Manchester`); **Town/City** and **Postcode** are captured as separate fields and auto-filled from the same lookup result. When the operator chooses **Use my address instead**, they enter **Main location** as free text and fill **Town/City** and **Postcode** manually. **Town/City** and **Postcode** appear only after the operator commits to a lookup suggestion or **Use my address instead**; until then only **Main location** is shown. After commit, editing **Main location** does not hide **Town/City** or **Postcode**; picking a new lookup suggestion re-auto-fills those fields.
_Avoid_: Trial address, primary location, venue address

**Trial request review**:
The admin workflow for evaluating a verified Trial Request — approve, request more info, or decline. Each outcome updates the request status and may trigger an email to the applicant.
_Avoid_: Reject, moderation, vetting

**Trial review status**:
The canonical lifecycle state of a Trial Request. Six values: **EmailVerified** (initial, after email verification), **MoreInfoRequested** (admin paused review for more info), **Approved** (admin approved; **Operator Setup invitation** sent), **InviteSent** (invite resent or reminder fired), **Declined** (terminal — declined requests cannot be approved again), **AccountCreated** (terminal — operator completed **Operator Setup**). Stored on `TrialRequest.Status`. Backend code uses a typed enum; the model default is `EmailVerified`. Replaces the previous mixed-case string values (`"EMAIL_VERIFIED"`, `"Email Verified"`, `"Account Created"`, etc.).
_Avoid_: Trial state, request stage, trial phase

**Trial review transition**:
The backend module that owns the **Trial review status** enum, the legal-transition table, and `ApplyTransition` — the single method that mutates a Trial Request's status in response to a **Trial review decision**. Guards illegal transitions (e.g. approve-after-decline), validates decision-specific required fields (reason for Decline / Request more info), writes reviewer identity and timestamps, rotates the **Operator Setup invitation** token on Approve / ResendInvite, persists in one transaction, then dispatches email after commit. The **Operator Setup invitation reminder** background job routes through the same module with `decision = ResendInvite` and a `System` admin identity. Absorbs the previous `SendOperatorSetupInvitationAsync` helper and the four parallel `AdminService` review methods.
_Avoid_: Trial state machine, review service, status updater

**Trial review decision**:
The four admin actions that drive a **Trial review transition**: **Approve**, **Decline**, **Request more info**, **ResendInvite**. Each maps to exactly one transition from the current **Trial review status** per the legal-transition table. `ResendInvite` is allowed only from `Approved` or `InviteSent`.
_Avoid_: Review action, status change, review command

**Trial request received email**:
The email sent to the applicant immediately after they successfully verify their email on the Trial Request form — when the application becomes a verified Trial Request awaiting **Trial request review**. Acknowledges receipt and sets expectations for review timing and next steps. Distinct from the OTP email (sent before verification) and the **Operator Setup invitation** (sent only after approval).
_Avoid_: Confirmation email, welcome email, trial signup email

**Operator details**:
The admin drawer opened from a trial-request table row. Shows the full application, applicant contact, review status, and review history for that Trial Request, plus the same review actions available in the row menu (approve, request more info, decline, resend invitation, delete). Titled **Operator details** in the admin UI whether or not Operator Setup is complete. Shows **Main location**, **Town/City**, and **Postcode** from the Trial Request under the Application section. Stays open when an admin runs an action; the drawer content updates in place after each optimistic change. After the operator account exists, shows an **Activation status** badge in the drawer header and an **Activation** section (status detail, **Activation Code** with copy and download, **Extend activation** when **Activation expired**). The trial-requests table row also shows an **Activation status** badge.
_Avoid_: Account details, applicant profile, trial request modal

**Activation status badge**:
A simplified admin label shown in the trial-requests table and **Operator details** header once the operator account exists: **Activated** when the account is within the **Activation period**; **Not activated** otherwise (covers **Pending activation** and **Activation expired**). Hidden before Operator Setup creates the account. Full status detail lives in the **Activation** section of **Operator details**.
_Avoid_: Trial badge, active tag, verified badge

**Decline**:
An admin decision that closes a Trial Request with status `DECLINED`. Requires written admin feedback (`DeclineReason`) before confirmation; that feedback is stored on the request and included in the decline email to the applicant. Declined requests cannot be approved again. Internal `AdminNotes` are not collected on this flow — the operator-facing message is the audit record.
_Avoid_: Reject, deny, turn down

**Request more info**:
An admin decision that pauses review with status `MORE_INFO_REQUESTED`. Requires written admin feedback (`MoreInfoMessage`) before confirmation; that feedback is stored on the request and included in the more-info email to the applicant. Internal `AdminNotes` are not collected on this flow — the operator-facing message is the audit record.
_Avoid_: Need info, pending documents, hold

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
The final step of Operator Setup (single- and multi-location) where Tummly prepares each location's Smart Guest Link and QR code. The operator sees a progress animation that awaits actual per-location generation of the link and QR; they are not asked to configure touchpoints, feedback tags, thank-you copy, or offers during this step. The private feedback form is standard for all locations and requires no per-location configuration. Phase 3 (starter QR materials) generates the account **Activation Code** today; per-location **Starter QR materials** generation is planned for a later release.
_Avoid_: Guest Loop configuration, step-3 form, rollout configuration

**Guest Loop provisioning phases**:
The three ordered preparation steps shown during Guest Loop provisioning: (1) Smart Guest Link — real backend generation per location, (2) private feedback form — presentational only (standard form, no per-location configuration), (3) starter QR materials — today generates the account **Activation Code** (real backend work); in a future release will also generate per-location **Starter QR materials** for physical print and shipment. The animation awaits completion of phases 1 and 3 before advancing.
_Avoid_: Loading screen, fake progress

**Sign-in**:
Authentication for returning operators, **Admin**, or **Support**, including password reset and OTP verification for operator accounts. Operators in **Pending activation** may complete Sign-in but are held at the **Activation Code** screen by the **Activation gate** until **Account activation** succeeds. Operators in **Activation expired** are turned away at Sign-in with no session. **Admin** and **Support** sign in with email and password only — no Sign-in OTP, no **Activation gate**.
_Avoid_: Login (acceptable in UI copy only)

**Activation gate**:
The access rule that blocks the **Operator dashboard** and operator APIs until **Account activation** succeeds, and blocks Sign-in entirely once **Activation expired**. **Admin** and **Support** are not subject to the Activation gate.
_Avoid_: Activation middleware, paywall, trial lock

**Account password**:
The credential an operator creates during Operator Setup or password reset. Accepted only when the password strength indicator reaches **Good** or better.
_Avoid_: Passphrase, secret, PIN

**Password strength**:
Five-tier indicator (Very weak → Weak → Good → Strong → Excellent) shown while choosing an Account password. **Good** is the minimum to save the password: at least 8 characters with uppercase and a number or symbol. **Excellent** is the top tier: 12+ characters with uppercase, a number, and a symbol. The same rules apply on Operator Setup and password reset.
_Avoid_: Password score, zxcvbn, complexity meter

**First Sign-in**:
The operator's first successful Sign-in after Operator Setup is complete — the first time they obtain a session through `/login`, not trial email verification or invite setup.
_Avoid_: First login, first visit

**Trusted device**:
A browser the operator has opted to remember for 30 days after completing OTP verification. Subsequent Sign-ins from that browser may skip OTP until trust expires.
_Avoid_: Remember me cookie, device fingerprint

**Sign-in OTP**:
The one-time code sent after password validation to confirm the operator's identity. Delivered by email by default; SMS is an alternate channel from the choose-method step.
_Avoid_: 2FA code, MFA token

**Pending activation**:
The account state after Operator Setup is complete but before the operator has entered a valid **Activation Code**. The operator may complete Sign-in (password and Sign-in OTP when required) but cannot reach the **Operator dashboard** until **Account activation** succeeds.
_Avoid_: Unactivated, trial pending, awaiting code

**Account activation**:
The operator action of entering a valid **Activation Code** on the **Activation Code screen** during Sign-in. On success the account leaves **Pending activation**, the **Activation period** begins, and the operator may access the **Operator dashboard**. The screen appears after Sign-in OTP (when required) or trust skip, and before **Workspace selection** and the **Operator dashboard**. Copy and layout per Figma `557:4543`. Wrong codes show an inline error; there is no skip and no self-service resend in v1.
_Avoid_: Account unlock, trial start, verify account

**Activation Code screen**:
The mandatory Sign-in step where an operator in **Pending activation** enters their **Activation Code**. Figma node `557:4543`. Title: **Your setup is complete**. Body explains the onboarding pack with QR materials and activation code; input placeholder **Activation code**; primary button **Enter activation code** (inline beside input). Help: **Need help getting started?** with **Help Centre** and **contact support** links. Uses `AuthShell`. No sign-out or skip on this screen. Wrong-code copy not in Figma — inline API error only.
_Avoid_: Activation page, unlock screen, trial gate UI

**Activation Code**:
A backend-generated code tied to one operator account, created during Guest Loop provisioning phase 3. Eight characters from an unambiguous uppercase alphanumeric charset (excludes `0`, `O`, `1`, `I`, `L`), displayed grouped as `XXXX-XXXX`; the dash is cosmetic and input accepts with or without it. One code per account; consumed on successful **Account activation**. Stored hashed on the backend; plain text visible only at generation and in admin **Operator details**. **Activation fulfillment** prints and ships the same code to every **Owned location** address from Operator Setup. Admins can view, copy, and download a print-ready asset before shipment. Distinct from per-location **Starter QR materials**, which will ship as separate venue packs in a future release. Distinct from **Sign-in OTP** (six-digit, channel-delivered).
_Avoid_: Invite code, setup code, OTP

**Activation period**:
The 30-day window after successful **Account activation** during which the operator has full **Operator dashboard** access. Starts at the activation timestamp; ends exactly 30 × 24 hours later (UTC). Customer-facing copy may say "30-day free trial"; domain language uses **Activation period** to distinguish from **Trial Request**. Does not start until **Account activation** succeeds — **Pending activation** has no time limit.
_Avoid_: Free trial, trial window, grace period

**Activation expired**:
The account state when the **Activation period** has ended. Subsequent Sign-in attempts are rejected with the message that the 30-day free trial is over. The operator cannot reach the **Operator dashboard**; an active session is ended on the next blocked API call. No operator self-service recovery in v1; an admin may **Extend activation** from **Operator details** to restore access without issuing a new **Activation Code**.
_Avoid_: Trial ended, deactivated account, suspended

**Extend activation**:
An admin action in **Operator details** that restores dashboard access for an **Activation expired** account by setting a new **Activation period** end date (default: now + 30 days UTC; admin may override). Does not regenerate or re-ship the **Activation Code** — the original code was already consumed at **Account activation**.
_Avoid_: Renew trial, reactivate code, extend trial

**Activation fulfillment**:
The physical delivery of the account **Activation Code** to the operator. The same code is printed and shipped to every **Owned location** — each venue **Address** captured during Operator Setup (Confirm restaurant for single-location; location cards or bulk upload for multi-location). Fulfillment is a separate operational step after provisioning; admins can view and download the code before shipment. Distinct from future per-location **Starter QR materials**, which will be separate print packs per venue.
_Avoid_: QR shipment, welcome pack, onboarding kit

**Operator contact phone**:
The operator's UK phone number captured on the Trial Request form (field label: **Mobile number** — kept intentionally, though landlines are accepted when provided) and confirmed during Operator Setup — Primary contact phone on the Confirm group step (multi-location) or Restaurant phone number on the Confirm restaurant step (single-location). Optional at every step. When provided, must be a valid UK number. Stored on the User account and as the restaurant's public phone when supplied. Prefilled from the Trial Request when available. When omitted throughout onboarding, the account is created without a phone on file and Sign-in OTP is email-only.
_Avoid_: Primary phone, mobile number, verified mobile

**Location phone**:
A per-location UK phone number on a RestaurantLocation, distinct from Operator contact phone. Optional during Operator Setup (manual entry and bulk upload). When provided, must be a valid UK number. Stored on `RestaurantLocation.LocationPhone`.
_Avoid_: Location mobile, site phone

**Verified phone**:
The operator's phone number on file after Operator Setup is complete, when one was provided. Eligible for SMS Sign-in OTP without a separate phone-verification step. When no phone was provided, Sign-in OTP is email-only.
_Avoid_: Verified mobile, 2FA phone

**Business category**:
The operator's hospitality type (e.g. takeaway, café, pub). Chosen at Trial Request and confirmed again during Operator Setup. Canonical options: Takeaway / quick-service restaurant; Café / coffee shop; Bakery / dessert shop; Casual dining restaurant; Food truck / mobile food business; Pub / bar / hospitality venue; Multi-site restaurant group; Other.
_Avoid_: Industry, vertical, business type

## Platform staff

**Admin**:
Internal Tummly staff who sign in at **Sign-in** and work from the **Admin dashboard** (`/admin-dashboard`). Responsible for **Trial request review**, operator account oversight, and activation support. Not an operator account.
_Avoid_: Superuser, back office, platform user

**Support**:
Internal Tummly staff who sign in at **Sign-in** and work from the **Support dashboard** (`/support-dashboard`). Responsible for handling **Help Centre queries** only. Cannot access **Trial request review**, **Operator details**, or activation actions — escalates those to **Admin**. Signs in like **Admin**: email and password only — no Sign-in OTP, no **Activation gate**. Works from a shared query inbox — paginated table of queries (including **query submitter type**), a dedicated query detail page with chat-style thread, **Support reply**, and **query status** changes. Not an operator account. Distinct from **Admin**.
_Avoid_: Customer service rep, help desk agent

**Query submitter type**:
Whether a **Help Centre query** is from an **Operator** (linked to an operator account) or a **Contact** (unsigned / unlinked **Contact us** submitter). Shown on the **Support dashboard** inbox as column/filter **Type**. Used to prioritise Operator requests.
_Avoid_: Guest, Restaurant, Kind, submitter kind

## Help Centre

**Help Centre**:
The public support area at `/help-center` — hero, search, and a browsable list of **Help Centre articles** for self-service, plus routes to submit **Help Centre queries**. Article content is static in v1 (not CMS-driven). Search filters **Help Centre articles** by title and body client-side only — it does not search queries or pre-fill **Contact us**. Only submissions from the **Contact us** form create **Help Centre queries** for **Support**. Accessible without **Sign-in**; signed-in operators (including on the **Activation Code screen**) submit with account context auto-attached on **Contact us**. Distinct from `mailto:support@tummly.com` links in email templates — those remain a manual fallback in v1, not an automated intake channel.
_Avoid_: Help center, support portal

**Help Centre article**:
A self-service help page under `/help-center/articles/:slug` — step-by-step or explanatory content for a single topic (e.g. setting up a Smart Guest Link). Does not create a **Help Centre query**. May link to **Contact us** when the reader still needs help.
_Avoid_: FAQ page, knowledge-base article, help doc

**Help Centre query**:
An inbound support request created from the **Contact us** form (`/help-center/contact`). Captures a **query topic**, submitter contact fields (name, email, optional phone), business name, an optional **query location** (signed-in operators only), an initial message, and optional **query attachments** (signed-in operators only). After submit, the submitter sees a generic confirmation screen — not topic-specific copy. Unsigned submitters provide contact fields manually and do not see **query location** or **query attachments** — one-shot intake only; they cannot continue the thread in the app. Signed-in operators have name, email, and business name prefilled, may optionally choose a **query location** from their **Owned location**s (same pattern as the operator dashboard location select), may attach files to aid diagnosis, and the query is linked to their operator account; they may view their queries and post follow-ups from **My queries** (`/help-center/my-queries`). Handled by **Support** from the **Support dashboard** as an in-app **query thread**; each **Support reply** is delivered to the submitter by email. The full conversation is stored in the app. Inbound email to `support@tummly.com` does not create or update queries in v1. Moves through **query status** values as Support works it.
_Avoid_: Ticket, case, support request (acceptable in UI copy only)

**Query attachment**:
A file uploaded by a signed-in operator when submitting a **Help Centre query** from **Contact us** — screenshots, exports, or other evidence (images and PDF in v1). Optional; up to five files, ten megabytes each, fifty megabytes total per query; JPEG, PNG, WebP, GIF, and PDF only. Stored in object storage with metadata in the database. Visible to **Support** in the query thread on the **Support dashboard** and to the submitting operator on **My queries** (filename and download). Not available on guest **Contact us** or on **Operator query reply** follow-ups in v1.
_Avoid_: Upload, attachment file, evidence file

**Query location**:
The optional venue an operator associates with a **Help Centre query**, chosen from their **Owned location**s on **Contact us**. Shown only when the submitter is a signed-in operator. Uses the same location-select pattern as the operator dashboard. Omitted for unsigned visitors and when the operator leaves it unset.
_Avoid_: Location field, site, venue

**Query topic**:
The category chosen on **Contact us** from the **I need help with** list — always selected manually by the submitter; never pre-filled from **Help Centre article** navigation or search. Canonical options: I need help setting up Tummly; My QR code is not working; My printed materials are damaged or missing; I need to reorder QR materials; I need help with guest feedback; I need help with an offer or redemption; I need help with a campaign; I have a billing or credits question; I need help with consent, privacy or data; I want to request a demo; Something else. Stored on every **Help Centre query**; shown in the **Support dashboard** inbox (table column label **Issue** is UI-only).
_Avoid_: Subject line, category, issue type (as a domain term)

**My queries**:
The signed-in operator view listing that operator's **Help Centre queries** at `/help-center/my-queries`. Each row shows **query topic**, **query status**, and last updated. Opening a query goes to a thread page with the full message history. The operator may post an **Operator query reply** when the query is open (**New**, **In progress**, or **Waiting on customer**); the thread is read-only when **Resolved** or **Closed**. Uses the same marketing chrome as **Help Centre** (site navbar and footer). Available to any signed-in operator, including on the **Activation Code screen** path. Not available to unsigned visitors.
_Avoid_: My tickets, support history

**Query status**:
The lifecycle state of a **Help Centre query**. Canonical values: **New** (unread, unassigned work); **In progress** (Support is actively handling); **Waiting on customer** (Support replied and needs a response from the submitter); **Escalated to Admin** (needs **Admin** action — activation, trial review, etc. — that **Support** cannot perform); **Resolved** (answer delivered, no further action expected); **Closed** (complete, archived). Every **Help Centre query** enters the **Support dashboard** as **New** regardless of **query topic** — **Support** triages all submissions and escalates manually when needed. **Support** may change status from the **Support dashboard**. Setting **Escalated to Admin** sends an email notification to Admin with query summary and escalation context; Admin actions still happen in **Operator details** / trial review — not in the Support dashboard. Transitioning into **Resolved** sends a **resolution email** to the submitter with a short conversation excerpt; **Operator** submitters also get a link to the query thread on **My queries**.
_Avoid_: Ticket state, case status

**Resolution email**:
The transactional email sent to the submitter when a **Help Centre query** transitions into **Resolved**. Includes a short excerpt of the query thread and, for **Operator** submitters, a link to the full conversation on **My queries**. **Contact** submitters receive the excerpt without a My queries deep link. Not sent on **Closed**. Distinct from a **Support reply** email.
_Avoid_: Closure email, resolved notification (acceptable in UI copy only)

**Support reply**:
A message added to a **Help Centre query** thread by **Support** from the **Support dashboard**. Triggers an email to the submitter's address on file. Distinct from an **Operator query reply** (follow-up from the submitter on **My queries**), **Admin** actions, and automated transactional emails.
_Avoid_: Email response, agent message

**Operator query reply**:
A follow-up message on an existing **Help Centre query** posted by a signed-in operator from **My queries**. Visible to **Support** in the query thread. When posted, the **query status** moves from **Waiting on customer** to **In progress** automatically, and **Support** receives an email notification with query summary and a link to the **Support dashboard**.
_Avoid_: Customer reply, user message

## Marketing site

**Marketing homepage**:
The public landing page at `/` — trial-request hero, product sections, FAQs, and marketing footer. Distinct from Legal pages, Sign-in, Operator Setup, and operator dashboards.
_Avoid_: Landing page, home page, marketing site (when you mean this page specifically)

**Legal page**:
A public marketing-site page presenting Tummly's legal copy — Privacy Policy (`/privacy`), Terms of Service (`/terms`), or Cookie Policy (`/cookie-policy`). All three use the same long-form layout (title, document-specific intro as description, effective date, header download control for the matching **Legal document**, table of contents, numbered sections). The Terms of Service page includes Supplementary Terms A–C as additional top-level sections after the main numbered terms. Top-level sections appear in the TOC; numbered subsections (e.g. 1.1) stay in the section body. Tables from the Legal document are summarised in prose on-page; the full tables are in the download. Cross-references in body copy to other Legal pages are in-app links. On-page titles stay **Privacy Policy**, **Terms of Service**, and **Cookie Policy** even when the downloadable Legal document uses a different formal name (e.g. Privacy Notice, Terms and Conditions). Uses the same chrome as the homepage (site navigation header and marketing footer). Accessible to visitors and signed-in operators alike; not gated behind Sign-in or blocked for signed-in users. Footer, acceptance checkboxes, and other inline nav links use short labels **Privacy**, **Terms**, **Cookie Policy**, and **Cookie settings** (Cookie settings opens the preference dialog, not a Legal page).
_Avoid_: Policy page, compliance page, Privacy Notice (as a link label — use Privacy; the formal Notice name belongs to the Legal document)

**Cookie settings**:
The in-app dialog where a visitor manages cookie preferences (essential always on; optional categories such as analytics). Distinct from the Cookie Policy **Legal page** at `/cookie-policy`, which presents the legal copy and Legal document download. Opened from footers alongside Cookie Policy, and from the cookie banner. There is no `/cookie-settings` page — that path is a 404.
_Avoid_: Cookie preference centre, cookie policy (when you mean the preference controls)

**Legal document**:
The counsel-approved downloadable original that corresponds to a **Legal page** (Privacy, Terms, or Cookie Policy). Distinct from the on-page legal copy rendered on the Legal page itself. Downloadable by anyone — not gated behind Sign-in.
_Avoid_: Legal file, policy attachment, Word doc (when you mean the downloadable original)

## Guest-facing

**Smart Guest Link**:
The public URL a guest accesses by scanning a location's QR code. The URL is `https://tummly.com/scan/{token}` where `{token}` is an opaque random per-location value generated during Guest Loop provisioning and stored on `RestaurantLocation`. The token is not the location's numeric primary key — it prevents link enumeration and survives location renames without invalidating printed QR codes. The frontend handles the `/scan/{token}` route; the backend resolves the token to location metadata and serves the feedback form.
_Avoid_: Scan URL, guest URL, public link

**QR code**:
The PNG image encoding a location's Smart Guest Link. The link token is generated during Guest Loop provisioning; the PNG itself is rendered on-demand when the operator first downloads it from the dashboard via `GET /api/qr/download?locationId={id}` using QRCoder (ECC level Q, 20px per module). The download filename is `QR_{LocationName}.png`.
_Avoid_: QR image, code image

**Starter QR materials**:
A formatted print-ready package per **Owned location** containing that location's **QR code** (e.g. table tents, sticker sheets, printable PDFs) for physical placement in the venue. Planned for a future release: generated during Guest Loop provisioning phase 3, printed, and shipped to each location's **Address**. Not in the current release — today phase 3 only generates the account **Activation Code**.
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
When a valid UK postcode is entered and the field loses focus, Tummly resolves it to an address and compares that result to the operator's Address. If the postcode matches but the street-level detail does not overlap, Address is replaced with the postcode lookup result and the field is locked. While locked, the Address control stays focusable: the operator opens the same async select menu and chooses **Use my address instead** to restore their entered text. When a postcode maps to multiple premises, Tummly picks the closest match to the operator's entered Address; if none are close enough, the first result is used and the operator may override. The operator can also unlock Address by changing Postcode (which re-reconciles on the next blur). Reconciliation is a client-side UX concern; the backend validates UK postcode format on submit and accepts an optional per-location override flag when the operator chose **Use my address instead**. The backend does not hard-block submit on address–postcode mismatch. Reconciliation applies on the Confirm restaurant step, each location card, and the bulk-upload review dialog. It does not apply on the Trial Request **Main location** capture.
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
