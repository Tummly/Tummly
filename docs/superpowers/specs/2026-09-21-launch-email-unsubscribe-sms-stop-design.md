# Launch Email unsubscribe + SMS STOP

**Status:** Design approved (chat 2026-09-21); awaiting spec review before plan/build  
**Authority:** Tummly Launch Reconciliation — Engineering Build Directive (hard blocks)  
**Clears:** Public Email unsubscribe E2E missing; SMS STOP / opt-out does not write Tummly withdrawal before SMS eligibility

## Problem

- Marketing (and other non-transactional) emails already render `{baseUrl}/unsubscribe` in `BaseNonTransactionalEmailTemplate`, but there is **no** public route, page, or API. Guests cannot complete unsubscribe from the link.
- Operator Guests can withdraw marketing via `GuestMarketingPreferenceUpdateService` (permission ledger + rollup + `guest-marketing-unsubscribed` activity). That path is authenticated operator-only.
- Outbound recovery SMS uses Twilio (`TwilioRecoveryGuestSmsDelivery` + `RecoveryFromNumber`). There is **no** inbound Messaging webhook to honour STOP / opt-out keywords by writing withdrawal into Tummly before later SMS eligibility checks.

Directive:

- Public Email unsubscribe must work end-to-end and immediately update eligibility.
- Provider SMS STOP / opt-out must write suppression/withdrawal into Tummly before any later SMS eligibility check.

## Decision

**Approach A:** Shared guest-initiated withdraw service that reuses the existing permission ledger + marketing preference rollup (no parallel suppression table). Public Email unsubscribe (signed one-click + form fallback) and Twilio inbound SMS STOP both call that service.

### Locked product rules (chat)

| Topic | Choice |
|-------|--------|
| Scope | Email unsubscribe **and** SMS STOP in one build |
| Email UX | Signed **one-click** link + bare `/unsubscribe` **form** fallback |
| STOP effect | **SMS marketing only** (not Feedback follow-up; not email) |
| Match scope (form / STOP) | **Restaurant-scoped** (not all tenants; not cross-restaurant) |
| One-click token | Exact **Location Guest** from token |

## Shared withdraw service

**Name (indicative):** `GuestInitiatedMarketingWithdrawService`

Responsibilities:

1. Resolve target Location Guest(s) under the allowed scope.
2. Append ledger `Withdraw` for the requested `LocationGuestPermissionKind` (`EmailMarketing` or `SmsMarketing`).
3. Source constants (exact strings to lock in plan): e.g. `email_unsubscribe`, `sms_stop`.
4. `actorUserId = null` (guest-initiated).
5. Call `SyncMarketingPreferenceRollupAsync` in the same unit of work.
6. When rollup becomes opted out, emit `LocationActivityKinds.GuestMarketingUnsubscribed` (same spirit as operator opt-out path).
7. **Idempotent:** already withdrawn → success; do not invent duplicate harm; may skip extra activity noise if already opted out (plan picks exact activity rule).

Campaign / recovery eligibility continues to read ledger / preference — **no** new suppression table.

## Email unsubscribe

### One-click (preferred)

- When composing non-transactional / marketing mail, replace bare footer with a **signed** URL, e.g. `{baseUrl}/unsubscribe?t={token}`.
- Token (HMAC): `locationGuestId`, `restaurantId` (or locationId), issued-at / expiry. No plaintext email in the query.
- Public confirm (GET show page + POST confirm, or single GET that confirms — plan picks UX; must be safe against CSRF for state-changing GET if used: prefer POST confirm after landing page).
- On success: withdraw **EmailMarketing** for that Location Guest only → eligibility updates immediately.

### Bare `/unsubscribe` form (fallback)

- Public SPA route `/unsubscribe` (and API the page calls).
- Guest enters email; restaurant context from query param and/or picker when ambiguous.
- Resolve Location Guests with that email under the **restaurant** → withdraw EmailMarketing for each match.
- Generic success copy; avoid enumerating whether the address exists where practical.

### Template change

- Update `BaseNonTransactionalEmailTemplate` (and any campaign email builders that hardcode the bare path) to emit the signed link when Location Guest context is available; otherwise keep `/unsubscribe` form fallback.

## SMS STOP (Twilio inbound)

### Webhook

- Public `POST` endpoint (e.g. `/api/webhooks/twilio/sms-inbound`).
- Validate Twilio request signature; reject invalid with non-2xx.
- Always return **200** + empty/minimal TwiML for accepted signature path so Twilio does not retry endlessly after business no-ops.

### Keyword handling

- Treat body as STOP when it matches Twilio Advanced Opt-Out style keywords: `STOP`, `STOPALL`, `UNSUBSCRIBE`, `CANCEL`, `END`, `QUIT` (trim, case-insensitive).
- Non-STOP bodies: acknowledge 200; no ledger write.
- **`START` / re-opt-in via SMS:** out of scope.

### Restaurant resolve + withdraw

- Map inbound **To** (Tummly number) → `restaurantId` via config and/or restaurant field.
  - Today `TwilioSettings.RecoveryFromNumber` is **global** — insufficient alone for restaurant scope.
  - Launch config: explicit map or per-restaurant inbound/from number(s) used for SMS that can receive STOP.
- Normalise inbound **From** (guest) to E164 UK.
- Find Location Guests with that mobile under the restaurant → withdraw **SmsMarketing** only via shared service.

### Unmapped To number

- Log; return 200 to Twilio; **do not** withdraw (fail closed on restaurant scope).
- Ops must configure the number→restaurant map before STOP is effective in that environment.

## Security

- HMAC unsubscribe tokens; short TTL; opaque token only in URL.
- Twilio signature validation on inbound webhook.
- Public endpoints: no operator auth; rate-limit friendly; generic confirmation copy where possible.

## Tests

- One-click: valid token → EmailMarketing withdrawn; campaign eligibility blocks email for that guest; expired/tampered token fails closed.
- Form: restaurant + email → withdraw matches; idempotent re-submit; no cross-restaurant bleed.
- STOP: mapped To + STOP keyword → SmsMarketing withdrawn; eligibility blocks SMS; non-STOP ignored; bad signature rejected; unmapped To → no withdraw + 200.
- Shared service idempotent when already withdrawn.

## Out of scope

- SMS `START` / text re-opt-in
- RFC 8058 List-Unsubscribe one-click headers / mailto (optional later)
- Feedback-follow-up withdraw via STOP
- Cross-restaurant / global contact withdraw
- Changes to operator Guests marketing preference UI (already works)
- Full Twilio Advanced Opt-Out console-only mode without Tummly ledger write (rejected by directive)

## Success criteria

1. Guest can open a marketing-email unsubscribe link and end email marketing eligibility for the intended Location Guest without operator login.
2. Bare `/unsubscribe` form works when no token is present (restaurant-scoped).
3. Twilio STOP on a mapped number writes SmsMarketing withdrawal into Tummly before the next SMS eligibility check.
4. No parallel suppression table; ledger + rollup remain source of truth.
5. Unmapped STOP numbers do not silently withdraw across restaurants.
