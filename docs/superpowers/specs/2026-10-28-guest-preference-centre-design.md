# Guest preference centre (replaces Unsubscribe) — design

Date: 2026-10-28
Branch: `feat/guest-preference-centre` (from `qa` at `3e04c46f`)
Status: draft for review

## Figma

- Base screen: https://www.figma.com/design/IQfpCZBNsQLbRAaPhnfmul/Guest-Loop-MVP?node-id=7191-814&m=dev
- Stop-all dialog: https://www.figma.com/design/IQfpCZBNsQLbRAaPhnfmul/Guest-Loop-MVP?node-id=7194-1073&m=dev
- Edit first name: https://www.figma.com/design/IQfpCZBNsQLbRAaPhnfmul/Guest-Loop-MVP?node-id=7185-392&m=dev
- Manage restaurants: https://www.figma.com/design/IQfpCZBNsQLbRAaPhnfmul/Guest-Loop-MVP?node-id=7191-598&m=dev
- Add another contact method: https://www.figma.com/design/IQfpCZBNsQLbRAaPhnfmul/Guest-Loop-MVP?node-id=7196-1157&m=dev
- Verify mobile (6-digit code): https://www.figma.com/design/IQfpCZBNsQLbRAaPhnfmul/Guest-Loop-MVP?node-id=7196-1226&m=dev
- Verify email (confirm link): https://www.figma.com/design/IQfpCZBNsQLbRAaPhnfmul/Guest-Loop-MVP?node-id=7198-1284&m=dev

## 1. Goal

Replace the one-restaurant Unsubscribe page with a guest preference centre.
One guest sees and controls marketing from **Tummly** and from **every
restaurant location** where they gave marketing permission.

## 2. Current state (code on `qa`)

- `/unsubscribe?t=` (signed token: Location Guest + restaurant, 90 days) or
  `/unsubscribe?restaurantId=` (unverified email form).
- Confirm withdraws **Email marketing** at one restaurant only (token: one
  Location Guest; form: every Location Guest of that restaurant with that
  email). Other restaurants are not touched.
- **Master Guest** is restaurant-scoped. No cross-restaurant identity exists.
- No `List-Unsubscribe` header on email.

## 3. Decisions (agreed 2026-10-28)

| # | Topic | Decision |
|---|---|---|
| D1 | What the page controls | **Both**: a new Tummly marketing stream **and** existing restaurant campaigns. |
| D2 | Mute one location | Withdraws Email + SMS marketing at that location (permission ledger) **and** excludes the location from the Tummly stream. Unmute restores both (see D5). |
| D3 | Entry and proof | Signed link in each marketing email opens the page. With no link, the guest verifies first (email: confirm link; UK mobile: 6-digit SMS code). The unverified email form is removed. |
| D4 | Added contact | Linked to the Tummly-level guest record only. Restaurant Master Guest rows are **not** changed. |
| D5 | Tick a channel on | **Restore only**: grant the Tummly stream for that channel; re-grant restaurant consent only at un-muted locations where the guest granted that channel before **and** the latest withdraw was guest-made (`email-unsubscribe` or `preference-centre`). Never undo `bounce`, `complaint`, `operator`, `sms-stop`, `suppression`. Never create consent at a location with no prior grant. |
| D6 | Checkbox display | On when the channel is granted for the Tummly stream **or** at any un-muted location. Save writes only the boxes the guest changed. |
| D7 | Scope | Consent only. No Tummly-stream sender, template or admin tool. |
| D8 | Approach | New platform-level **Tummly Guest** record (approach A). |

Assumptions (correct during review if wrong):

- A1. First name lives on the Tummly Guest only. An edit does not change any `LocationGuest.Name`.
- A2. Stop all = untick Email and SMS (D5/D6 rules), for all locations and the Tummly stream. Feedback follow-up is not changed (dialog copy).
- A3. The verify-email copy "Your Feedback is saved" is from the feedback flow. This page uses copy without the feedback sentence.
- A4. SMS campaigns keep STOP-reply only. No preference link in SMS in this work.
- A5. The page lists a location only when the Location Guest has at least one historic **grant** of Email or SMS marketing (the Figma: "restaurants a guest is enrolled in").

## 4. Domain terms (add to `CONTEXT.md`)

- **Tummly Guest** — platform-level identity of a person who uses the
  preference centre. Owns one or more **verified contacts**. Distinct from
  **Master Guest** (restaurant-scoped). Restaurants never see it.
- **Tummly marketing** — marketing that Tummly itself may send. Consent is
  per channel on the Tummly Guest. No sender exists yet (D7).
- **Location mute** — a Tummly Guest's choice that one Owned location must not
  market to them and must not feature in Tummly marketing.

## 5. Data model (new tables, EF migration per `CODING_STANDARDS.md`)

```
TummlyGuest
  Id, FirstName (nvarchar 100, null), CreatedAt, UpdatedAt

TummlyGuestContact
  Id, TummlyGuestId (FK, cascade), ContactType (email|phone),
  NormalizedValue (nvarchar 320), VerifiedAt, CreatedAt
  UNIQUE (ContactType, NormalizedValue)       -- one owner per contact

TummlyGuestMarketingEvent          -- append-only, like the Location ledger
  Id, TummlyGuestId (FK), Channel (email|sms), EventKind (grant|withdraw),
  Source (preference-centre), WordingSnapshot (nvarchar 512, null),
  OccurredAt, CreatedAt

TummlyGuestLocationMute
  Id, TummlyGuestId (FK), RestaurantLocationId (FK, cascade), MutedAt
  UNIQUE (TummlyGuestId, RestaurantLocationId)
```

New ledger source: `LocationGuestPermissionLedgerSources.PreferenceCentre = "preference-centre"`.
New OTP purpose: `OtpPurposes.GuestPreferences = "guest_preferences"`.

Restaurant data does not change shape. The Tummly Guest finds its Location
Guests at read time: `MasterGuest.NormalizedEmail` / `NormalizedPhone` equal to
any verified contact, across all restaurants.

**Merge.** A guest who verifies a contact that already belongs to another
Tummly Guest has proven both. The system moves the other record's contacts,
mutes (union) and marketing events into the current record, then deletes it.
One transaction.

## 6. Identity and session

1. **Signed link (existing token, unchanged format).** `/preferences?t=<token>`.
   The server verifies the HMAC token (`UnsubscribeToken`), loads the Location
   Guest's Master Guest and takes its normalized email as the proven contact.
   Old `/unsubscribe?t=` links redirect to `/preferences?t=` (sent emails keep
   working for 90 days).
2. **No link.** `/preferences` shows "Email address or UK mobile number".
   - Email: send a confirm link (`/preferences?v=<otpId>.<code>`, 10 min =
     `OtpChallengeService.CodeLifetime`, single use via `OtpVerification` row)
     to that address. The link carries no email address.
   - Mobile: issue a 6-digit code (`OtpChallengeService`, purpose
     `guest_preferences`, SMS channel via existing SMS sender).
   - Response is always the same ("If this contact is on file…") so the
     endpoint does not reveal who is a guest. Rate limit per IP and per contact.
3. On proof, the server finds or creates the Tummly Guest for that contact and
   returns a **preference session token** (HMAC, payload: Tummly Guest id +
   proven contact id + expiry, 30 min). The frontend keeps it in
   `sessionStorage` and sends header `X-Guest-Preferences-Session: <token>`
   (the app uses no cookies; API is on `api.*`). A separate header keeps the
   operator JwtBearer handler and the operator axios interceptor out of this
   flow. The token is never an operator JWT (different signing secret). Each
   authenticated response returns a renewed token in the same header
   (sliding 30 min).

## 7. API (`PublicGuestPreferencesController`, `[AllowAnonymous]` + `X-Guest-Preferences-Session` check)

| Method | Path | Purpose |
|---|---|---|
| POST | `/api/public/preferences/session/link` | body `{ token }` → verify signed link, return session token |
| POST | `/api/public/preferences/session/start` | body `{ contact }` → send link or code (always 202) |
| POST | `/api/public/preferences/session/verify-code` | body `{ contact, code }` → return session token |
| POST | `/api/public/preferences/session/verify-email` | body `{ v }` (`<otpId>.<code>`) → return session token |
| GET | `/api/public/preferences` | name, masked contacts, channel state (D6), summary counts |
| PUT | `/api/public/preferences/name` | body `{ firstName }` |
| PUT | `/api/public/preferences/channels` | body `{ email?: bool, sms?: bool }` — only changed keys (D6) |
| POST | `/api/public/preferences/stop-all` | Stop all (A2) |
| GET | `/api/public/preferences/restaurants?q=` | brands → locations, muted flag, logo |
| PUT | `/api/public/preferences/locations/{id}/mute` | body `{ muted }` |
| PUT | `/api/public/preferences/restaurants/{id}/mute` | body `{ muted }` — all locations of the brand |
| POST | `/api/public/preferences/contacts` | body `{ contact }` → send link or code for the new contact |
| POST | `/api/public/preferences/contacts/verify-code` | body `{ contact, code }` → link (merge if needed) |
| POST | `/api/public/preferences/contacts/verify-email` | body `{ v }` → link (merge if needed) |

Old `/api/public/unsubscribe/*` endpoints: `preview` and `confirm` stay for
one release for old links that hit the API directly; `form` is removed (D3).

## 8. Write rules (one service: `GuestPreferenceService`)

All restaurant-side writes go through `ILocationGuestPermissionLedgerService`
with source `preference-centre`, then `SyncMarketingPreferenceRollupAsync`,
in one `SaveChanges` (same unit of work as today's withdraw service). A
`LocationActivity` row is added when a rollup flips to OptedOut (as today).

- **Untick channel C:** Tummly event `withdraw C`; for every matched Location
  Guest with C not Withdrawn → ledger `withdraw C`.
- **Tick channel C:** Tummly event `grant C` with the checkbox wording
  snapshot; restore C per D5 at un-muted locations.
- **Mute location L:** add mute row; ledger `withdraw` Email + SMS at L.
- **Unmute L:** delete mute row; restore at L per D5, only for channels whose
  page checkbox is on (D6).
- **Stop all:** untick Email and SMS.

Campaign sending needs **no change**: `CampaignEligibilityService` already
skips Withdrawn guests.

## 9. Email footer and headers

- `UnsubscribeLink` builds `/preferences?t=` (same token).
- Campaign and guest-response emails add `List-Unsubscribe: <https://…/api/public/preferences/one-click?t=…>`
  and `List-Unsubscribe-Post: List-Unsubscribe=One-Click` (RFC 8058). The
  one-click POST withdraws **Email marketing at the sender location only**
  (same scope as today's token confirm). The full page remains for wider
  changes.

## 10. Frontend

- Route `/preferences` (new `GuestPreferencesPage`); `/unsubscribe` redirects,
  keeping the query string.
- Dark full-page layout per Figma; *headline* type for titles, *functional*
  for body (`docs/agents/typography.md`).
- Reuse: `Button`, `CheckboxLabel`, `FloatingLabelInput`, `AlertDialog`
  (Stop all), `Dialog` (verify), `InputOTP` (6 digits), `Accordion` (brands),
  `Input` (search), `Spinner`. No raw `<button>`.
- Views: base, edit name (inline), add contact (inline input → verify dialog),
  manage restaurants (sub-view with Back, search, accordion, Mute / Unmute,
  Mute all brand locations).
- Contacts shown masked (`so•••••@example.com`, `•••• ••• 789`).

## 11. Errors and limits

- Expired/invalid link → "This link has expired" + no-link entry form.
- Session expired → same entry form, keeps no state.
- Code: 5 wrong attempts → challenge invalidated (existing OTP rule); resend
  allowed after 60 s.
- Rate limit `session/start` and `contacts`: 5 per contact per hour, 20 per IP
  per hour (existing rate-limit policy style).
- Logs mask contacts (`LogContactMask`).

## 12. Testing

- Backend unit: restore rule (D5) per withdraw source; D6 display; merge; mute
  / unmute; stop all; token → contact; anti-enumeration response.
- Backend integration: each endpoint with and without session token
  (and with an operator JWT, which must fail); cross-restaurant
  read shows only matched, previously granted locations; campaign eligibility
  skips muted location.
- Frontend: page modes (link, no link, expired), Save sends changed keys only,
  Stop-all dialog, mute toggles, verify dialogs.

## 13. Delivery split (build tickets)

1. Identity and session (tables, Tummly Guest, link + code/link verify, cookie).
2. Preference page read + channels Save + Stop all.
3. Manage restaurants (list, search, mute / unmute, brand mute).
4. Add contact (verify, merge) + first-name edit.
5. Links and headers (`/preferences`, redirect, `List-Unsubscribe`, one-click).

## 14. Out of scope

- Sending Tummly marketing; admin UI for it.
- Tummly-marketing opt-in on the Guest Form.
- Preference link in SMS.
- Operator UI changes (operators see ledger events with source
  `preference-centre` through existing views).

## 15. Baseline on branch start

- Backend test project does not compile on `qa`
  (`AdminShopCatalogServiceTests.cs:300-311`, `SetAvailableInShopAsync`
  parameter `actorAdminUserId` missing).
- Frontend: 1 failing test (`assistantWaitGerunds.test.ts`), 3180 pass.
