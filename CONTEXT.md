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
The final step of Operator Setup (single- and multi-location) where Tummly prepares each location's five default **QR code**s (four placement types plus Smart Guest) with distinct **QR link**s. The operator sees a progress animation that awaits actual per-location generation; they are not asked to configure touchpoints, feedback tags, thank-you copy, or offers during this step. The private feedback form is standard for all locations and requires no per-location configuration. Phase 3 (starter QR materials) generates the account **Activation Code** today; per-location **Starter QR materials** generation is planned for a later release. Operators do not receive downloadable QR PNGs from provisioning or the dashboard.
_Avoid_: Guest Loop configuration, step-3 form, rollout configuration

**Guest Loop provisioning phases**:
The three ordered preparation steps shown during Guest Loop provisioning: (1) Smart Guest Link / default **QR code**s — real backend mint of five Active codes per location, (2) private feedback form — presentational only (standard form, no per-location configuration), (3) starter QR materials — today generates the account **Activation Code** (real backend work); in a future release will also generate per-location **Starter QR materials** for physical print and shipment. The animation awaits completion of phases 1 and 3 before advancing.
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

**Activation period badge**:
Customer-facing Home chrome for the remaining **Activation period**: countdown days, calendar end date (`Ends {D MMM YYYY}`), and a **Choose a plan** affordance. Shown only in the Operator Home “Your Guest Loop is live” hero — not in the navbar and not on other dashboard pages. Hidden when Activation expiry is missing or the period has ended. **Choose a plan** is presentational until a plan surface exists (not a live navigation target in this slice). Distinct from the admin **Activation status badge**.
_Avoid_: Trial badge, Advanced trial badge (as a separate concept), Activation status badge

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

**Your role**:
The Trial Request form field label for the applicant's self-selected job function. The stored/API value is **Self role**.
_Avoid_: Job title field

**Self role**:
The job function the applicant picks for themselves on the Trial Request form (field label: **Your role**). Canonical option values: Owner / operator; Founder / director; General manager; Area / operations manager; Marketing / growth; Admin / support; Agency / consultant; Other. Exposed to the signed-in operator as `selfRole` on `/auth/me`. Distinct from the account permission role on the User record (`user.Role`, e.g. `Owner`). In Operator dashboard account chrome, slash-joined labels show only the segment before the slash (e.g. Owner / operator → **Owner**, Founder / director → **Founder**); labels without a slash show as-is (e.g. **General manager**); **Other** is omitted (name only, no subtitle).
_Avoid_: yourRole, job title, account role, store manager, registration role

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

**QR type**:
The catalog kind of a per-location **QR code**. Default types: Counter card, Packaging sticker, Delivery insert, Window sticker, Smart Guest, and **Digital guest link**. For every type except **Digital guest link**, at most one non-archived **QR code** of that type may exist per **Owned location**. **Digital guest link** allows many per location, unique by **Link name** among non-archived codes. Encodes placement or channel intent; placement is not a separate domain entity.
_Avoid_: QR placement (as an entity), QR category, touchpoint type

**QR code**:
A per–Owned location instance of a **QR type**, with its own **QR link**. Defaults are created per location (four physical placement types plus Smart Guest). Operators may also create **Digital guest link** codes from Capture. Operators do not download QR PNGs — physical stickers are obtained via the **Tummly Shop**. Home still emphasizes the **Smart Guest Link**; **Capture** exposes copy for each Active or Paused **QR link** and Pause/Activate (with confirm). Distinct from **Starter QR materials**.
_Avoid_: QR image, code image, QR placement (as an entity), downloadable QR

**QR link**:
The public URL/token for one **QR code**. Peers share the same guest-route shape (`/scan/{token}`); each **QR code** has its own opaque token so scans can be attributed by **QR type**. Replaces the older model of a single token on `RestaurantLocation`. **Rotate** remints a new token on the same **QR code**; the old URL stops resolving.
_Avoid_: Scan URL, guest URL, public link, source wrapper

**Smart Guest Link**:
The **QR link** of the Smart Guest **QR type** for an **Owned location** — the operator-facing name for that default code's URL. Still what Home surfaces as the location's primary guest link. On Capture it remains a row in **QR placements** (not in **Digital guest links**). Same guest form as other **QR link**s; only the token (and thus source attribution) differs.
_Avoid_: Scan URL, guest URL, public link

**Digital guest link**:
A **QR code** of **QR type** Digital guest link — a tracked `/scan/{token}` for digital channels (social, email, WhatsApp, website, online ordering, etc.), not physical print materials. Many per **Owned location**, unique by **Link name** among non-archived codes. Created from Capture via **Create digital guest link**. Same scan/feedback pipeline as other **QR code**s; Capture shows them in the **Digital guest links** section.
_Avoid_: Digital QR placement, custom link (when meaning this type), smart link (when meaning Smart Guest)

**Link name**:
The operator-chosen display name of a **Digital guest link** **QR code**. Unique among non-archived Digital guest links at that **Owned location**. Not used for physical placement types or Smart Guest.
_Avoid_: Placement name (when meaning a digital link), custom placement name (deferred physical Custom path)

**Starter QR materials**:
A formatted print-ready package per **Owned location** containing that location's **QR code**s (e.g. table tents, sticker sheets, printable PDFs) for physical placement in the venue. Planned for a future release: generated during Guest Loop provisioning phase 3, printed, and shipped to each location's **Address**. Not in the current release — today phase 3 only generates the account **Activation Code**.
_Avoid_: QR pack, print materials

**Private feedback form**:
The guest-facing form displayed when a guest visits the Smart Guest Link. Standard for all locations — the same form content is served regardless of which location's QR code was scanned; only the displayed **Location name**, **Address**, and **Brand logo** differ. Header shows Brand logo, Location name, then Address (Address line omitted when blank). Body copy and the **Offers opt-out** checkbox use Location name (and Address in the subtitle) — not `Restaurant.Name`. Captures guest name, guest contact (email or phone, single field), a feedback message, and **Offers opt-out**. Layout: feedback message card first (placeholder "Add your own feedback…", with speech-to-text mic), then a "Your details" card (name, contact, Offers opt-out). Legal links on this form use the formal labels **Terms & Conditions** and **Privacy Notice** (exception to the short **Terms** / **Privacy** labels used elsewhere). Per-location or per-restaurant configuration of form fields is not in scope. The backend resolves location metadata (Location name and Address) in the same response that renders the form, and accepts feedback submissions via a POST endpoint keyed by location.
_Avoid_: Feedback survey, guest survey, review form

**Offers opt-out**:
A per-Feedback boolean the guest sets on the Private feedback form — whether they prefer not to receive offers via the contact details they provided. UI is a pre-checked checkbox whose label uses the Location name; unticking records the opt-out. Stored as `OffersOptOut` (default `false`). Remains the audit of what was chosen on that submission; durable operator-facing consent lives on **Location Guest offers opt-out**. Replaces the earlier planned "guest list opt-in" polarity.
_Avoid_: Guest list opt-in, marketing consent, offers opt-in, soft opt-in (as the field name)

**Location Guest offers opt-out**:
The durable consent flag on a **Location Guest** — whether that location may message the Guest with offers. Updated from **Offers opt-out** on Feedback submissions for that location. Drives **Marketing status** for the Location Guest. Distinct from Master-level suppression (deferred).
_Avoid_: Opt-in flag, marketing consent field, guest list opt-in, location consent (as the field name)

**Feedback**:
One guest submission captured via the Private feedback form for an Owned location after a **QR link** scan. Owns the guest-provided fields (name, contact, comment, **Offers opt-out**), submission time, required `QrCodeId` (source **QR code** / **QR type** via join), **Feedback workflow status**, and — as they are introduced — **AI classification** (sentiment and **Detected Tags**), operator corrections, **Feedback internal notes**, and per-submission activity history. References a **Location Guest** and does not own it. Latest activity and the future Feedback page are entry points onto Feedback; they do not own those details. Activity history records things that happened on that Feedback (e.g. received; later classified, corrected, note added, note deleted, **Feedback workflow status** changed) — not pending pipeline hints. Note body edits and same-to-same status no-ops do not create activity history rows. For the Feedback details slice, activity history is derived from Feedback facts (submission time, **Feedback internal notes** including soft-deletes, operator **classification correction** rows, and workflow status change facts) rather than a separate Feedback activity store. Correction, note, and status-change events carry the operator display name, timestamp, and action (including from/to sentiment for corrections and from/to status for workflow changes).
_Avoid_: Review, rating, comment (when meaning the whole submission)

**Feedback internal note**:
One operator-only note attached to a single **Feedback** submission (Feedback details and other Feedback details entry points). Many notes may exist per Feedback. Any operator who may add notes at that Owned location may edit the body in place or soft-delete the note (hidden in product UI, retained for audit; no operator restore). Soft-delete produces a note-deleted activity beat while the original note-added beat remains; edits do not. Never shown to the guest. Owned by the Feedback (not the **Location Guest**) — may exist even when Feedback has no Location Guest link. Neither is a view or copy of a **Location Guest note**.
_Avoid_: Internal note (without Feedback owner), guest note, Location Guest note (when meaning a Feedback-scoped note), append-only note (when meaning immutable)

**Feedback workflow status**:
The canonical operator follow-up lifecycle of one **Feedback**. Three persisted values: **New**, **In progress**, **Resolved**. Initial value on guest submit is always **New**, independent of **AI classification** outcome or sentiment. An operator may set any of the three from any of the three (including skip and reopen); same-to-same is a no-op. Changes only via explicit operator action — opening **Feedback details** or selecting an inbox row does not auto-advance status. Each real change produces an activity-history beat (actor, timestamp, from → to). Inbox tabs: **All** (no status filter); **Needs attention** (derived); **New** / **In progress** / **Resolved** (exact status match). Distinct from **AI classification** status, from derived **Needs attention**, from the 24-hour freshness badge **New** (Feedback), and from the inbox **All** tab (unfiltered view, not a status).
_Avoid_: Feedback status (alone), review status, ticket status, Needs attention (as a stored status)

**Needs attention** (Feedback):
A derived operator signal on one **Feedback**: **AI classification** is Succeeded with Negative sentiment **and** **Feedback workflow status** is not **Resolved**. Not persisted. Surfaces as an inbox tab/filter, header CTA count, and a badge on **Feedback details** — never as a **Feedback workflow status** value. Distinct from Location Guest **Needs recovery**.
_Avoid_: Needs recovery (when meaning Feedback), attention status, priority status, workflow Needs attention

## Operator workspace

**Workspace selection**:
The post-authentication step where a multi-restaurant operator chooses which restaurant to work in. Triggered when the backend sets `workspaceSetupRequired` on the sign-in response. The operator sees a list of their restaurants, picks one, and is redirected to that restaurant's dashboard. Single-restaurant operators (single- and multi-location alike) skip this step entirely and land directly on their dashboard. Today every operator owns one restaurant, so workspace selection is dormant — the UI and API exist but are not triggered. The fields (`workspaceSetupRequired`, `selectedRestaurantId`) are not sent by the backend until multi-restaurant ownership is introduced.
_Avoid_: Location picker, workspace picker

**Operator dashboard**:
The authenticated area where an operator manages their business. Single-location operators land on `/single-dashboard`; multi-location operators land on `/multi-dashboard` and switch between their restaurant's locations via an in-dashboard location switcher. The admin dashboard (`/admin-dashboard`) is the only fully-built dashboard. Composition: a persistent shell (navbar, SideNav, Owned-location switcher) wraps a swappable page body (Home, Guests, and **Capture**; Feedback, Campaigns, and other primary destinations later; management destinations under the **Settings nav group** later).
_Avoid_: Admin panel, control panel

**Capture**:
The Operator dashboard destination for managing **QR code**s (UI: **QR placements** and **Digital guest links**), engagement KPIs, and guest-experience summary. Single-location Capture and multi-location nested per-location Capture share one body; multi-location operators also have a **Capture overview** root with **Location performance** across all **Owned location**s.
_Avoid_: QR manager, placements page (when meaning the whole destination)

**Digital guest links**:
The Capture body section that lists operator-created **Digital guest link** **QR code**s for the selected **Owned location** (not Smart Guest; Smart Guest stays under **QR placements**).
_Avoid_: Digital placements table, guest links manager

**Capture overview**:
The multi-location Capture root section that shows restaurant-wide Capture KPIs across all **Owned location**s for the **Multi Capture overview date range** (Active locations, Active QR placements, and engagement totals). Independent of **Location performance** search and filters.
_Avoid_: Capture analytics (when meaning only this strip), multi Capture KPIs (as the glossary noun)

**Location performance**:
The multi-location Capture root table of **Owned location** rows with per-location Capture metrics, status, search, filters, sort, and pagination. Entry to nested per-location Capture. Distinct from the per-location **QR placements** table.
_Avoid_: Locations Capture table, multi placements table

**Capture location status**:
Persisted Active / Paused flag for Capture on an **Owned location** as a whole. Distinct from an individual **QR code**’s Active / Paused / Archived status. **Pause location capture** sets the location to Paused and pauses then-Active codes (placements, Smart Guest, Digital guest links), remembering that restore set; **Activate location capture** sets the location to Active and restores only that set. While the location is Paused, per-code Pause/Activate is locked; guest resolve stays per-code only. **Capture location snapshot**, **Capture overview**, and **Location performance** expose the persisted Capture location status (and restore-set size on locations).
_Avoid_: Location Active (ambiguous with operational location), venue pause

**QR placement (UI)**:
Operator-facing label on Capture for a row that represents one **QR code** of a catalog **QR type**. Not a separate domain entity from **QR code** / **QR type**.
_Avoid_: Placement entity, touchpoint record

**Guest**:
The operator-facing umbrella noun for someone who has engaged with the restaurant through Tummly (feedback, offers, and related activity). In the data model a Guest is either a **Master Guest** or a **Location Guest**; UI copy still says guests. Distinct from a single **Feedback** submission, which may contribute fields to a Guest. Distinct from Help Centre **query submitter type**, which must not be called Guest.
_Avoid_: Customer, diner, CRM contact, guest profile (as the canonical noun), visitor

**Master Guest**:
The restaurant-scoped durable identity for a Guest, keyed by normalized email or phone. Shared across Owned locations so the same person is not duplicated as disconnected identities. Not what the Guests page lists per location.
_Avoid_: Global guest, guest account, CRM contact, person record

**Location Guest**:
The membership of a **Master Guest** at one **Owned location** — what that location’s operators see and manage on the Guests page (activity, consent, **Guest tags**, **Location Guest notes**, and list rows for that venue). Created when the Master Guest first interacts at that location.
_Avoid_: Venue guest, location membership (as the product noun), site guest, local guest

**Location Guest delete**:
Hard-delete of one **Location Guest** by an owning operator. Owned-location authz and the `(Location Guest, location)` pair check live in the delete module. Removes **Location Guest notes**, Guest-tag memberships, and **Location Guest activity events**; unlinks Feedback (`LocationGuestId` null) so Feedback PII snapshots remain; removes the **Master Guest** only when no other Location Guests remain for that Master.
_Avoid_: Soft delete, GDPR erase (when meaning this operator action), cascade delete Feedback

**Location Guest note**:
One operator-only note attached to a **Location Guest**. Many notes may exist per Location Guest. Any operator who may add notes at that Owned location may edit the body in place or soft-delete the note (hidden in product UI, retained for audit; no operator restore). Soft-delete records a **Location Guest activity event**; body edits do not. Never shown to the guest. Owned by the Location Guest (not the **Master Guest**) — a note at one venue does not appear on the same person’s profile at another. UI may say Notes, Recent notes, or Internal notes; the glossary noun stays Location Guest note. Distinct from a **Feedback internal note**, which is about one Feedback submission — neither is a view or copy of the other.
_Avoid_: Internal note (as the canonical noun), guest note, Master Guest note, Feedback note, CRM note, append-only note (when meaning immutable)

**Location Guest activity event**:
One append-only timeline row for something that happened to a **Location Guest** (joined, feedback submitted, **Location Guest note** added or soft-deleted, Guest tag applied/removed, profile edited, classification succeeded/failed). Persisted in the Location Guest activity store; listed on the Guest Profile Activity tab. Does not record **Location Guest note** body edits or **Feedback internal note** mutations — those belong on the note itself or on per-**Feedback** activity history. Distinct from Home **Latest activity** and from per-**Feedback** activity history.
_Avoid_: Guest activity log, audit trail, timeline event (when meaning this store)

**Location Guest activity recorder**:
The module that appends **Location Guest activity events** for a domain write (kind and payload). Callers own the unit of work (SaveChanges). Does not decide whether the domain write happens.
_Avoid_: Activity emitter, activity publisher, SignalR activity push

**Guest tag**:
One operator-facing label on a **Location Guest**, drawn from that restaurant’s **Guest tag catalog**. Same concept whether applied via Guests Add Tag or via union when **AI classification** Succeeds with **Detected Tags**. Distinct from a **Detected Tag**, which lives only on Feedback under **AI classification**. On Succeeded classification (and backfill of existing Succeeded Feedback), each Detected Tag is also applied as a Guest tag on that Feedback’s Location Guest — additive union only. Sentiment-only classification correction does not change Guest tags; removing a Guest tag is only via explicit Guests tagging flows.
_Avoid_: Detected Tag, Detected tags, AI tag, auto-tag (when meaning a Guest tag); Feedback tag, FeedbackTags; label, segment, Smart Group (when meaning a Guest tag); customer tag, CRM tag

**Guest tag catalog**:
The restaurant-scoped set of available **Guest tag** definitions used by Add Tag search/create and Guests Filters. Not per-location and not the applied set on one Location Guest. An entry appears when an operator creates a tag, or when a **Detected Tag** is lazily ensured on classification Succeeded / backfill (not pre-seeded for every Detected Tag up front).
_Avoid_: Tag library, tag dictionary, restaurant tags

**AI-sourced**:
Boolean on a **Guest tag catalog** entry. `true` only when AI first introduced that entry by ensuring a **Detected Tag** into the catalog. `false` when an operator created the entry. AI must not flip or overwrite an existing catalog entry — operator-created wins even if a matching Detected Tag label appears later.
_Avoid_: AI tag (as the noun for the whole Guest tag)

**Smart Group**:
A product-defined segment of **Location Guests** shown as a tab on the Guests page. Membership rules are fixed by product, not operator-created lists. The closed product set, in UI order: All guests; New guests; Needs recovery; Positive feedback; Offer not redeemed; Recent redeemers; Dormant guests. Section title in the UI: **Smart groups**.
_Avoid_: Segment, audience, saved filter, guest list, tag group (when meaning these tabs)

**New guests**:
The Smart Group of **Location Guests** first captured within the last 13 days (rolling, UTC). Distinct from **Guest overview** first-captured totals under the **Guest overview date range**, and from the Home activity signal **New guest joined**.
_Avoid_: New this month (when meaning this tab), recently acquired

**New guest joined**:
The Home Latest activity signal that a **Location Guest** was first created at an Owned location (the **Master Guest** may already exist from another venue). Distinct from the **New guests** Smart Group and from the Performance overview **Guests joined** KPI.
_Avoid_: Guest created, new profile, guest signup, first visit event

**Marketing status**:
The operator-facing label of whether a **Location Guest** may be contacted for offers or campaigns and by which channel (e.g. Eligible — Email). Derived from **Location Guest offers opt-out**, reachable contact, and suppression — not a free-text tag. Distinct from per-Feedback **Offers opt-out**, which feeds Location Guest offers opt-out over time.
_Avoid_: Consent status, marketing consent, opt-in state, eligibility badge (as the field name)

**Guest details**:
The operator drawer opened from the Smart Groups table via guest-name click or row **View guest** that shows a summary of one **Location Guest** (identity, contact and permissions, relationship summary, recent feedback, offers and campaigns, internal notes, recent activity). Loads that Location Guest from the backend (not from the list row as source of truth). Escalates to the full **Guest Profile** page via **View full profile**. Live CTAs in the Guests entry slice: **View full profile**, **Add note**, **Open feedback** (when latest feedback exists), and **View full activity**; **Create campaign**, **Start recovery**, and **View engagement history** stay pending. **Open feedback** and **View full activity** close **Guest details** and navigate to **Guest Profile** with one-shot router location state (`openFeedbackId` / Activity tab) — not durable query deep links. Distinct from **Guest Profile** (full-page surface) and from **Feedback details** (one Feedback).
_Avoid_: Guest preview, guest drawer, quick view, guest summary, Guest Profile (when meaning this drawer)

**Guest overview**:
The Guests-page summary section that shows four metrics for the selected **Guest overview date range**: **Total guests**, **New this month**, **Marketing eligible**, and **Needs recovery**. Does not filter the guest table, Smart Groups, search, or Filters. **Total guests**, **New this month**, and **Marketing eligible** share first-captured cohort scoping when a non–All-time window is selected. **Needs recovery** is the exception: its overview window is keyed on Succeeded Negative **Feedback** submission time (see **Needs recovery**).
_Avoid_: KPIs, stats strip, guest analytics (when meaning this section)

**Guest overview date range**:
The operator-selected time window that scopes **Guest overview** KPI counts for the current Operator dashboard visit. Presets: All time (default), Last 7 days, Last 30 days, This month, and Custom (same local-calendar window rules as **Home performance date range**). Independent of table Filters date and of **Home performance date range**. Does not filter the guest table, Smart Groups, search, or Filters. All time = no overview window (full effective location scope for each KPI’s own rule).
_Avoid_: guestsDateRange, overview filter (as the product name); Date axis (that term belongs to Guests Filters, not Guest overview)

**Total guests**:
The Guest overview count of **Location Guests** first captured within the **Guest overview date range** (All time = full effective location scope).
_Avoid_: Guest count, all profiles (as the metric name)

**New this month**:
The Guest overview count of **Location Guests** first captured within the last 30 days (rolling, UTC), further scoped to the selected **Guest overview date range**. Distinct from the **New guests** Smart Group (13-day rolling window).
_Avoid_: New guests (when meaning this overview card — that name is the Smart Group)

**Marketing eligible**:
The Guest overview count of **Location Guests** first captured within the **Guest overview date range** that have valid permission, a reachable contact method, and no suppression.
_Avoid_: Contactable, opted in, eligible count (as the metric name)

**Needs recovery**:
The derived membership of a **Location Guest** that has at least one **Feedback** whose **Succeeded** **AI classification** sentiment is currently Negative — the temporary stand-in for “unresolved negative feedback” until a recovery-exit domain exists. Not “latest sentiment is Negative” alone (a later Positive Feedback does not clear membership). Membership recomputes from current sentiment: an operator **classification correction** that leaves no Succeeded Negative Feedbacks clears membership. The **Needs recovery** Smart Group lists all members in location scope (independent of **Guest overview date range**) and is not mutually exclusive with **Positive feedback** (a guest can match both). The Guest overview **Needs recovery** KPI is special-cased: All time = count of members; with a preset/custom window = count of Location Guests that have ≥1 currently Succeeded Negative Feedback whose **submission time** falls in that window (first-captured may be outside the window). Other entry reasons, open recovery actions, and explicit resolve/exit are deferred.
_Avoid_: Recovery queue, negative guests (as the metric name); Positive feedback (the Smart Group — different membership rule)

**Settings nav group**:
A disclosure in the Operator SideNav that groups future management destinations. It is not itself a destination or landing page.
_Avoid_: Settings page, Settings landing, Operator Settings (when meaning a single SideNav route)

**Tummly Shop**:
Operator surface for purchasing physical QR stickers and related materials. SideNav footer chrome exists today; full shop/fulfillment is not part of every product slice. Physical **QR code** stickers are obtained here — operators do not download QR PNGs from the dashboard.
_Avoid_: Store, marketplace (when meaning the SideNav footer item)

**Brand logo**:
The operator-uploaded mark for their business, managed later under the **Settings nav group** (blob-backed). Shown on the Owned-location switcher and on the **Private feedback form** header for every location under that restaurant — the same mark on both surfaces. Until that upload exists, both surfaces use one shared placeholder mark for all operators — not per-location art, not scraped favicons.
_Avoid_: Location logo, avatar, restaurant icon

**Operator appearance preference**:
The operator's Light / Dark / System chrome choice for the **Operator dashboard** only. Device-local (browser). Applies only inside the Operator dashboard shells; Home, Sign-in, Activation, Workspace selection, Operator Setup, admin, Help Centre, and guest surfaces stay light regardless of this preference and of the OS color scheme. Default when unset is System (OS-following inside the shell only). Chosen from the account menu **Theme Switch** drill-down (System / Dark / Light); selecting an option updates the preference in place — the submenu stays open until the operator goes back or dismisses the menu.
_Avoid_: Site theme, global dark mode, app theme

**Operator workspace session**:
The shell-scoped module for one Operator dashboard visit. Owns bootstrap of Owned locations and operator profile (display name, Activation expiry), selected Owned location (including persistence), and the inputs the shell needs for chrome. Stays mounted while the operator remains in the dashboard; page bodies depend on it rather than re-fetching locations and profile. Does not own page-specific loads (Home feedback, checklist acks, future Feedback lists) or UI chrome preferences (sidebar collapse, Settings nav group disclosure, **Operator appearance preference**).
_Avoid_: Operator Home session, dashboard controller, auth store

**Operator Home page module**:
The Home-scoped module for the Operator dashboard Home body. Depends on the Operator workspace session’s selected Owned location. Owns Home location-scoped loads (feedback snapshot; **Performance overview** KPI counts for the **Home performance date range**; Finish-setting-up acknowledgements via an internal ack module), Latest activity Feedback details via an internal Feedback details module, a narrowed Home body view-model (selected venue, Smart Guest Link caps, setup steps, KPIs, activity — not shell chrome or static empty-shell section props), Preview guest form (Smart Guest Link, then acknowledge on the ack module), and Copy Smart Guest Link (clipboard copy of the selected location’s Smart Guest Link; toast on success). Does not own Download QR. Static empty shells (Needs attention, Live offers, Recommended, Weekly brief) are owned by section components. Swappable later for other page modules without tearing down the shell or workspace session.
_Avoid_: Operator Home session (when meaning shared shell state), Home controller as the owner of locations/profile

**Performance overview**:
The Operator Home section that shows guest-engagement KPIs for the selected Owned location (QR scans, Feedback submitted, Guests joined, Offer redemptions) and the date-range control that scopes live KPI counts. Distinct from Guest overview on the Guests page.
_Avoid_: Stats strip, Home analytics, KPI dashboard (when meaning this section)

**Home performance date range**:
The operator-selected time window that scopes **Performance overview** live KPI counts for the current Operator dashboard visit. Stored as `homePerformanceDateRange` on the visit-scoped dashboard UI store. Defaults to Last 7 days on first land of a dashboard visit; not persisted across visits or in `localStorage`. Does not filter Latest activity.
_Avoid_: dashboardDateRange, KPI filter (as the store key), all-time stats window

**Capture performance**:
The Capture section that shows location-level engagement KPIs for the selected **Owned location** (Guest form opens — UI label for QR-scan counts — Form starts, Feedback submitted, Marketing opt-ins, Offer claims) scoped by the **Capture performance date range**. Totals sum across all **QR type**s at that location; the **QR placements** and **Digital guest links** tables break the same window down per code. Distinct from Home **Performance overview**.
_Avoid_: Capture analytics, placements KPIs (when meaning the location summary cards)

**Capture performance date range**:
The operator-selected time window that scopes **Capture performance** KPI counts and **QR placements** table count columns for the current Operator dashboard visit. Same preset vocabulary as **Home performance date range** (Last 7 days default; Last 30 days; This month; Custom ≤ 180 days; no All time). Independent of Home, Guests, and **Multi Capture overview date range**. Does not scope **Last scan** (all-time).
_Avoid_: captureDateRange (as the product name), shared dashboard date range

**Capture location snapshot**:
The single per-**Owned location** Capture read for one **Capture performance date range**: location KPI totals (current and previous equal-length window), Active/Paused **QR code** rows with per-code current-window metrics, **Capture location status**, and last journey update. Current location totals are the sum of those rows. Distinct from **Capture overview**, **Location performance**, **Capture preview-options**, and the **Capture multi-location reads module**.
_Avoid_: Capture performance endpoint, combined placements+performance load, Capture location body DTO (as the product noun)

**Capture preview-options**:
The lean per-**Owned location** Capture read that lists Active/Paused **QR code**s as guest-experience Preview picker facts only (`qrCodeId`, `qrType`, `status`, `linkName`) — no date window, no engagement metrics, no KPI totals. Used by the multi-location Capture root Preview flow. Distinct from **Capture location snapshot** (body load) and from **Location performance**.
_Avoid_: preview facts endpoint, placements-for-picker, guest experience options DTO (as the product noun)

**Capture preview-options module**:
The backend module that owns **Capture preview-options** for a signed-in operator and Owned location (authz, Active/Paused filter, label projection). Controllers are thin HTTP adapters. Does not own snapshot KPIs, Location performance list composition, or **QR lifecycle module** writes.
_Avoid_: Capture preview service, guest experience preview backend, lean placements query (as the glossary noun)

**Windowed engagement aggregate**:
Shared Capture kernel that scopes Active/Paused **QR code**s and counts date-windowed scans / Feedback (including previous equal-length windows) for **Capture location snapshot** and **Capture overview**. Does not own Location performance list composition or HTTP adapters.
_Avoid_: Capture metrics helper (as the glossary noun), shared KPI query service

**QR lifecycle module**:
The backend module that owns Capture **QR code** writes for a signed-in operator and Owned location: Create digital guest link, Update internal description, Pause/Resume, Rotate, Archive, Restore, and **Pause / Activate location capture** (including restore-set persistence). Controllers are thin HTTP adapters that keep Owned-location authz and map typed domain results to status codes. Does not own **Capture location snapshot**, **Capture preview-options**, **Capture Archive** list composition, guest resolve, or Operator dashboard UI. Includes description patch as Capture QR write hitchhiker on the same seam.
_Avoid_: Capture placements service (as the glossary noun), QR mutations controller logic, Capture writes facade

**Multi Capture overview date range**:
The operator-selected time window that scopes **Capture overview** engagement KPI counts and **Location performance** table count columns on the multi Capture root. Same preset vocabulary as **Capture performance date range**. Visit-scoped and independent of nested/single Capture’s range. Does not scope Active locations / Active QR placement stock counts or **Last activity** (all-time).
_Avoid_: multiCaptureDateRange (as the product name), shared Capture date range

**Capture Archive**:
The account-wide Capture screen that lists archived **QR code**s (catalog placements, Smart Guest, and **Digital guest links**) for the operator’s restaurant, with search, type/location filters, sort, pagination, Restore, and digital Duplicate. Distinct from the live per-location Capture body and from the multi-location **Capture overview** / **Location performance** root.
_Avoid_: Archived placements page, QR archive (when meaning a separate product from Capture), trash

**Placement Detail**:
The Capture drawer for one **QR code** (catalog **QR placement**, Smart Guest, or **Digital guest link**) — status, link, performance for the **Capture performance date range**, internal description, and row actions (Pause/Activate, Rotate where allowed, Archive, Copy, Preview). Opened from live Capture tables, **Capture Archive**, or post-action toasts. Distinct from **Guest details** and from **Feedback details**.
_Avoid_: Placement drawer, QR details modal, code inspector

**Operator Capture page module**:
The Capture-scoped module for the Operator dashboard Capture body (single-location and multi nested per-location). Depends on the Operator workspace session’s selected Owned location. Owns the **Capture location snapshot** load (one status for the body), **Capture performance date range**, placements list/view-model, Pause/Activate and Rotate confirms, Copy link, and in-app guest-experience preview. Orchestrates two internal modules — **Capture Archive module** and **Capture Placement Detail module**. Does not own shell chrome or the multi-location Capture root (**Capture overview** / **Location performance**).
_Avoid_: Capture session, QR controller, placements store

**Capture Archive module**:
Internal module that owns **Capture Archive** list interaction (search/filter/sort/page), refetch against the **Capture Archive list module** HTTP contract, Restore confirm, and archive-row commands. Holds only the current page view-model plus facet options from the list response — not an unbounded client cache of archived facts. Used inside the Operator Capture page module. Not a public dashboard module beside the Operator workspace session or page modules. Same internal-seam pattern as the **Guest details module**.
_Avoid_: Archive session, public Capture Archive page module, archived placements store, client-side archive DeriveRow

**Capture Placement Detail module**:
Internal module that owns **Placement Detail** open/close, selected code, description draft, and drawer-local view for one **QR code**. Used inside the Operator Capture page module. Cross-cutting writes (pause, rotate, archive, save description) are orchestrated by the page module; this module does not call the **Capture Archive module** directly. Not a public dashboard module beside the Operator workspace session or page modules. Same internal-seam pattern as the **Guest details module**.
_Avoid_: Placement Detail session, public drawer module, capture detail store

**Operator Multi Capture page module**:
The Capture-scoped module for the multi-location Capture root (`/multi-dashboard/capture`). Owns **Capture overview**, **Multi Capture overview date range**, **Location performance** list interaction (search, filters, sort, pagination, navigate to nested Capture), and multi-root guest-experience Preview (loads **Capture preview-options**, picker/overlay, clear-on-close cache eviction). Does not own nested per-location Capture body, **Capture Archive**, or shell chrome.
_Avoid_: Multi Capture session, locations Capture store, aggregated Capture controller

**Capture multi-location reads module**:
The backend module that owns restaurant-wide **Capture overview** aggregates and **Location performance** list composition (search, filters, sort, pagination, row metrics) for the operator’s **Owned location**s. Overview engagement counts use the shared **Windowed engagement aggregate** kernel. Overview totals stay independent of table search and filters. Does not own per-location **Capture location snapshot**, **Capture preview-options**, **QR lifecycle module** writes (including Pause/Activate location capture), **Capture Archive**, or Operator dashboard UI.
_Avoid_: Capture locations list service (as the glossary noun), multi Capture query service, Capture analytics backend, GuestsListService (wrong domain)

**Capture Archive list module**:
The backend module that owns account-wide **Capture Archive** list composition for the operator’s **Owned location**s: search, filters, sort, pagination, all-time per-code scan/feedback/last-scan aggregates used for metric sorts, Restore conflict (`canRestore`) projection against live occupancy, and Archived-by facet options. Does not own Archive UI, Restore/Duplicate mutation semantics beyond listing flags (Restore/Create writes live on the **QR lifecycle module**), per-location **Capture location snapshot**, or Operator dashboard page modules.
_Avoid_: archived placements dump, CapturePlacementsController archive Derive, buildCaptureArchive (as the list kernel)

**Operator Guests page module**:
The Guests-scoped module for the Operator dashboard Guests body. Depends on the Operator workspace session for shell context (selected Owned location). Owns Location Guest loads/view-model, Smart Groups table interaction for the live pass (fixtures retired), and one internal **Guest details module**. Does not own shell chrome (navbar, SideNav, Owned-location switcher) or page-specific action handlers deferred on Guests.
_Avoid_: Guests session, guests controller, guest CRM module

**Operator Guest Profile page module**:
The Location Guest–scoped module for the Operator dashboard Guest Profile and Edit surfaces. Depends on the Operator workspace session for selected Owned location. Lives for one guest-scoped visit (Profile and Edit routes under the same layout); destroyed when leaving those routes. Owns the Location Guest profile snapshot, notes, Edit commands, one internal Feedback details module, and internal Activity / Feedbacks tab modules, plus an explicit invalidate map after writes. Does not own shell chrome or the Guests list; Guests → Profile always loads profile fresh (no list-row seeding). Does not own **Guest details** (that lives on the Guests page module).
_Avoid_: Guest Profile session, guest session module, profile cache, Operator Guest Edit page module (as a peer lifetime)

**Operator Notifications module**:
The shell-scoped module for Operator Notifications on the Operator dashboard. Owns the signed-in operator’s inbox snapshot (list, unread/badge, preferences) and enables the navbar bell. Stays with the shell for the dashboard visit; not folded into the Operator workspace session and not Home-only.
_Avoid_: Notification store, bell controller, inbox session

**Notification**:
A per-operator inbox item about product, account, tips, weekly brief, or campaign/report/offer activity. Scoped to the signed-in operator (user-global), not to the selected Owned location. Unread until marked read.
_Avoid_: Alert, toast (delivery chrome), message, push

**Notification category**:
One of five preference buckets a Notification belongs to: Product updates; Account notices; Weekly brief reminders; Tips and playbooks; Campaign and report updates.
_Avoid_: Channel, topic, tab (tabs are a narrower UI filter)

**Notification preference**:
Per-operator on/off for a Notification category. When off, future Notifications in that category never land in that operator’s inbox; re-enabling affects future items only.
_Avoid_: Subscription, mute (when meaning the five category toggles)

**Notification capability**:
An optional permission a Notification type may require so future operator RBAC can hide that Notification from roles that lack it. Ungated types require none.
_Avoid_: Role claim (JWT Admin/Owner), ACL

**Notification CTA**:
Optional label and href on a Notification. Using the CTA navigates and marks that item read. Whole-card navigation is not part of this concept.
_Avoid_: Button, deep link (as the whole Notification concept)

**Feedback details module**:
Internal module that owns open/load/close (and classify/note / workflow-status commands) for one **Feedback**’s details. Each hosting page module owns its own instance — Operator Home, Operator Guests, Operator Guest Profile, and Operator Feedback page — never a shell-shared instance beside the Operator workspace session. Same internal-seam pattern as Finish-setting-up acknowledgements.
_Avoid_: Feedback session, public feedback module, Latest activity store, shell Feedback details

**Guest details module**:
Internal module that owns open/load/close (and note commands) for one **Location Guest**’s **Guest details** drawer. Used inside the Operator Guests page module. May open nested **Feedback details** while Guest details stays open underneath. Loads from the backend on open — not list-row seeding. Not a public dashboard module beside the Operator workspace session or page modules. Same internal-seam pattern as the **Feedback details module**.
_Avoid_: Guest Profile module (when meaning this drawer), guest preview store, Guests drawer session

**Feedback details** (drawer):
The shared operator drawer that shows one **Feedback** for an Owned location the operator controls. UI title is **Feedback details**. Entry points: Home Latest activity, Guests Guest details (stacked), Guest Profile / Edit, and the Feedback page inbox. Loads details from the backend (not from the list row as source of truth). Header venue is `{Location name} · {QR source}` when source is known (else Location name); Submission details use Location name under the Figma **Restaurant** label and **Address** under **Location** — not `Restaurant.Name`. Shows derived **New** (freshness) and **Needs attention** badges; absolute submitted time; live **Feedback workflow status** control; **Issue tags** (UI title for read-only **Detected Tags**); Contact state / Contact availability from contact facts; **Feedback reference** `FDB-{padded id}`; QR source from the Feedback’s `QrCodeId` join. Previous/Next list navigation is Feedback-page inbox only. **AI classification** lifecycle drives Pending / Succeeded / Failed empty states. **View guest profile** when a Location Guest link exists and the operator is not already on that guest’s Profile, Edit, or Guest details; correct classification when Succeeded; **Feedback internal notes** create / edit / soft-delete. Activity history includes received, note added/deleted, classification corrected, and workflow-status changes. Recovery / campaign / Message guest CTAs and Edit tags stay pending chrome.
_Avoid_: Feedback modal, activity detail, review drawer, Latest activity Feedback details (legacy name for the same shared surface), HomeFeedbackDetailsDrawer (legacy component name)

**New** (Feedback):
A freshness badge on a Feedback submission when `CreatedAt` is within the last 24 hours (rolling) — shown on **Feedback details** (including Home / Guests / Guest Profile entry points and the Feedback page drawer). Distinct from the **Feedback workflow status** value **New** (inbox **New** tab and Workflow status column/field). Distinct from sentiment badges and from Help Centre **Query status** value **New**.
_Avoid_: Unread, unseen, workflow New (when meaning this freshness badge)

**Detected Tag**:
One value from the product-fixed closed vocabulary of topic themes on a Feedback (e.g. Food quality, Wait time). Assigned as a multi-label set by **AI classification**, independent of sentiment — positive, neutral, and negative Feedback may all carry tags. On **Feedback details** the UI section title is **Issue tags** (read-only chips; Edit tags pending). Inbox / filters may still say Issue tags for the same vocabulary. `Other` is exclusive and never combined with another tag. Distinct from a **Guest tag** on a Location Guest; on classification Succeeded a Detected Tag may also be applied as a Guest tag, but Feedback classification UI does not manage Guest tags.
_Avoid_: Detected issue, problem theme, auto-tag (when meaning one vocabulary value); Guest tag, guest tag (when meaning a Feedback classification theme)

**Feedback reference**:
Operator-facing display id for one **Feedback**, formatted `FDB-{padded numeric id}`. Shown on **Feedback details** Submission details and copied by **Copy feedback reference**. Not a separate persisted identifier — derived from the Feedback primary key.
_Avoid_: Feedback UUID, public feedback token, external case number

**AI classification**:
The system-assigned sentiment (positive / neutral / negative) and **Detected Tags** for a Feedback submission, with an explicit lifecycle on Feedback: **Pending** → **Succeeded** | **Failed**. Succeeded always includes sentiment and a (possibly empty) Detected Tags set from the product-fixed vocabulary. Failed persists without inventing sentiment or tags. Some Failed cases (recoverable infrastructure / transient provider problems) may later return to **Pending** and then Succeeded or Failed again; operators keep seeing Failed during the wait and may briefly see Pending during a retry attempt. There is no separate Retrying status. Drives sentiment badges on Latest activity (only when Succeeded) and the AI classification / **Issue tags** sections in **Feedback details** (Pending empty states, Succeeded including calm empty tags, Failed unavailable). Guest Feedback submit never waits on the model — new Feedback starts Pending and classification is enqueued asynchronously.
_Avoid_: Detected issues, problem themes, sentiment tag (when meaning the full classification result), Retrying (as a product status)


**Finish-setting-up acknowledgements**:
Per–Owned location acknowledgements for Finish-setting-up actions that are not derived from other data (today: guest form previewed, QR placement guide viewed). Owned by an internal module inside the Operator Home page module — load, optimistic acknowledge, persist, and rollback. Snapshot is the raw ack fields plus load/ack busy/error, not the six setup steps. Setup step statuses stay derived in the Home view-model (acks as one input); derived steps (account ready, first feedback, offer, campaign) are not stored as acknowledgements. Not a public dashboard module beside the Operator workspace session and Operator Home page module.
_Avoid_: Checklist session, setup steps store, public acks module

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
The primary provisioning endpoint called at the end of Operator Setup. Creates a `User`, `Restaurant`, one or more `RestaurantLocation` rows, five Active **QR code**s per location (Counter card, Packaging sticker, Delivery insert, Window sticker, Smart Guest) each with a unique opaque token, and a stub `GuestLoopSetup`. Single and multi-location operators follow the same code path — the backend loops over `dto.Locations` regardless of account type. No QR PNG is generated during this step or afterward — operator-facing QR PNG generation has been retired. The private feedback form is standard for all locations and requires no per-location generation.
_Avoid_: Complete setup, finalize account

**`GuestLoopSetup`**:
The per-restaurant configuration row created during provisioning. Holds `Touchpoints`, `FeedbackTags`, `ThankYouMessage`, offer fields (`OfferHeadline`, `OfferDetails`, `OfferExpiry`, `OfferRedemption`, `OfferUsageLimit`), and boolean flags (`SendPhysicalQrMaterials`, `AutoSendReviewRequests`). During provisioning, only the two boolean defaults are set — all offer/feedback/touchpoint fields are left NULL. The legacy `POST /api/onboarding/guest-loop` endpoint does populate these fields but is not called by the provisioning flow.
_Avoid_: Guest Loop config, rollout config
