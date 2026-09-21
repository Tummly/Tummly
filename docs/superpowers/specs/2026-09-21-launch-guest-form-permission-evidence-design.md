# Launch Guest Form permission evidence

**Status:** Design approved (chat 2026-09-21); plan at `docs/superpowers/plans/2026-09-21-launch-guest-form-permission-evidence.md`  
**Authority:** Tummly Launch Reconciliation — Engineering Build Directive (Guest Form evidence)  
**Clears:** Ledger grant/withdraw from Guest Form lacks `basis`, wording/PN/form versions, and exact wording snapshot; API contact validation is weaker than “Email OR UK mobile”

## Problem

Guest Form opt-in UI and grant/withdraw rules largely match launch truth:

- Unchecked marketing by default; channel-specific checkbox copy
- Follow-up notice (no checkbox) separate from marketing
- Channel switch resets marketing choice
- First-time untick stays Not recorded (not withdrawn)
- Source string is already `guest-form`

Gaps vs directive evidence list:

1. `LocationGuestPermissionLedgerEntry` has no `Basis`, Guest Form / wording / Privacy Notice versions, or wording snapshot.
2. `RecordEvent` cannot carry evidence; `GuestFormPermissionApplyService` only stamps source + kind + time.
3. Submit API `DetectContactType` accepts weak contact shapes / `Unknown`; frontend already requires valid Email or UK mobile.

## Decision

**Approach 1:** Add nullable evidence columns on the ledger entry + a `PermissionLedgerEvidence` record and a new `RecordEvent(..., evidence)` overload. Only Guest Form submit fills evidence. Harden submit contact validation to reject invalid Email / UK mobile with `400`.

### Locked product rules (chat)

| Topic | Choice |
|-------|--------|
| Storage | Columns on `LocationGuestPermissionLedgerEntry` (not JSON blob; not separate evidence table) |
| Source string | Keep **`guest-form`** as canonical (do not rename to `guest_form`) |
| Versioning | Launch **version id constants** + **wording snapshot** text on the row |
| FeedbackFollowUp grant | Keep restaurant `FeedbackFollowUpPermissionEnabled` gate; stamp evidence only when we grant |
| Contact | Harden API: reject unless valid Email or UK mobile (align with frontend); do not rewrite phone to E.164 for storage |
| Who stamps evidence | **Guest Form submit only**; operator / email-unsubscribe / sms-stop leave evidence columns null |
| Marketing withdraw basis | **null** basis; still stamp versions + snapshot of the unticked control |

## Data model

Nullable columns on `LocationGuestPermissionLedgerEntry` (EF migration; existing rows stay null):

| Column | Type (indicative) | Guest Form value |
|--------|-------------------|------------------|
| `Basis` | string(64), nullable | `consent` (marketing grant); `service_follow_up_notice` (FeedbackFollowUp grant); **null** on marketing withdraw |
| `GuestFormVersion` | string(32), nullable | Launch constant e.g. `guest-form-v1` |
| `WordingVersion` | string(32), nullable | Channel constant e.g. `email-marketing-v1` / `sms-marketing-v1` / `feedback-follow-up-v1` |
| `PrivacyNoticeVersion` | string(32), nullable | Launch constant e.g. `privacy-notice-v1` |
| `WordingSnapshot` | string(512), nullable | Exact wording shown. Marketing: restaurant custom consent wording if non-empty, else launch checkbox label. Follow-up: fixed notice with restaurant name. |

New type (indicative):

```csharp
public sealed record PermissionLedgerEvidence(
    string? Basis,
    string GuestFormVersion,
    string WordingVersion,
    string PrivacyNoticeVersion,
    string WordingSnapshot
);
```

`ILocationGuestPermissionLedgerService` gains an overload:

```csharp
void RecordEvent(
    LocationGuest locationGuest,
    int restaurantLocationId,
    LocationGuestPermissionKind permissionKind,
    string eventKind,
    string source,
    DateTime occurredAt,
    PermissionLedgerEvidence evidence,
    int? actorUserId = null
);
```

Existing `RecordEvent` overloads unchanged (evidence columns remain null).

## Submit flow

### Contact gate (`ScanController.SubmitFeedback`)

Before guest upsert:

1. Trim contact.
2. If valid Email → `ContactType.Email`.
3. Else if valid UK mobile (same rules as frontend `tryNormalizePhoneToE164` / existing phone helper) → `ContactType.Phone`.
4. Else → `400` Bad Request; no upsert, no ledger write, no Feedback row.

Replace weak `@` / digit heuristic accepts. Do **not** change stored contact format beyond trim.

### Permission apply (`GuestFormPermissionApplyService`)

Keep current `LedgerEventsForGuestFormSubmit` rules:

- FeedbackFollowUp grant only when restaurant FeedbackFollowUp Enabled
- Marketing grant/withdraw only when matching channel Enabled
- Marketing grant when checkbox checked; withdraw only if prior state was Granted; first untick writes nothing

For each event written, call the evidence overload:

| Event | Basis | Snapshot |
|-------|-------|----------|
| FeedbackFollowUp grant | `service_follow_up_notice` | Fixed follow-up notice text (restaurant name interpolated) |
| Email/SMS marketing grant | `consent` | Restaurant custom wording if non-empty; else launch checkbox label |
| Marketing withdraw | null | Same snapshot rule as the matching marketing control |

Shared launch constants for `GuestFormVersion`, `PrivacyNoticeVersion`, and per-channel `WordingVersion` live in one helper (plan names exact strings). Bump only when Product changes launch copy.

`actorUserId` stays null (guest-initiated).

## Surfaces

**Required:** Persist evidence on the Guest Form write path.

**Preferred if cheap:** Include basis + version columns on Permission records CSV export and/or list DTO. If not cheap in this slice, defer UI/export surfacing; storage still satisfies the directive evidence list.

## Tests

- Guest Form grant stores basis + versions + snapshot; marketing withdraw stores null basis + versions + snapshot.
- Submit rejects invalid contact with 400 and writes no ledger/Feedback.
- Submit accepts valid Email and valid UK mobile.
- Existing Guest Form permission behaviour unchanged (toggle gate, first untick, channel marketing).
- Migration: new columns nullable; old rows remain null without backfill.

## Out of scope

- Changing FeedbackFollowUp grant rules or restaurant toggle semantics
- Backfilling evidence on historical ledger rows
- Stamping evidence from operator / email-unsubscribe / sms-stop writers
- Renaming source to `guest_form`
- Normalizing stored UK mobile to E.164
- Legal/FAQ copy changes (paused per directive)
- Cookie inventory, a11y QA, Shop cutoff, paid cancel, UK residency (separate P0s)

## Success criteria

1. Every new Guest Form ledger grant/withdraw that is written includes version ids + wording snapshot; grants include the correct `basis`.
2. Invalid contact cannot create Guest / Feedback / ledger events via submit.
3. Source remains `guest-form`; eligibility / rollup behaviour unchanged except for rejected invalid contacts.
