# Launch Email Unsubscribe + SMS STOP Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Public Email unsubscribe (signed one-click + restaurant-scoped form) and Twilio SMS STOP both withdraw marketing via the existing permission ledger so eligibility updates immediately.

**Architecture:** Shared `GuestInitiatedMarketingWithdrawService` appends ledger `Withdraw` + rollup sync + optional `guest-marketing-unsubscribed` activity. Public unsubscribe API + SPA page consume signed tokens / form posts. Twilio inbound webhook validates signature, maps To→restaurant, withdraws `SmsMarketing` only.

**Tech Stack:** ASP.NET Core anonymous controllers, HMAC tokens (JwtSettings secret), Twilio request validation, React Router public page, xUnit + Vitest.

**Spec:** [docs/superpowers/specs/2026-09-21-launch-email-unsubscribe-sms-stop-design.md](../specs/2026-09-21-launch-email-unsubscribe-sms-stop-design.md)

## Global Constraints

- Email unsubscribe **and** SMS STOP in this build.
- Email: signed one-click + bare `/unsubscribe` form fallback.
- STOP withdraws **SmsMarketing only** (not Feedback follow-up; not email).
- Form / STOP match scope = **restaurant-scoped**.
- One-click token = exact Location Guest.
- Ledger + rollup = source of truth; **no** parallel suppression table.
- Unmapped Twilio To → log, 200, **no** withdraw.
- Commit only when the human asks (skip Commit steps unless asked).
- Subagents: Cursor Auto / `inherit` only.
- Report to the human in ASD-STE100 Simplified Technical English.

---

## File map

| File | Role |
|---|---|
| `backend/.../Models/LocationGuestPermissionLedgerEntry.cs` | Add ledger sources `email-unsubscribe`, `sms-stop` |
| `backend/.../Services/GuestInitiatedMarketingWithdrawService.cs` | Shared withdraw |
| `backend/.../Helpers/UnsubscribeToken.cs` | HMAC create/verify for one-click |
| `backend/.../Controllers/PublicUnsubscribeController.cs` | Anonymous confirm + form API |
| `backend/.../Controllers/TwilioSmsInboundController.cs` | STOP webhook |
| `backend/.../Configurations/TwilioSettings.cs` | `InboundNumberRestaurants` map |
| `backend/.../Helpers/EmailTemplates/BaseNonTransactionalEmailTemplate.cs` | Optional `unsubscribeHref` |
| Call sites that send guest marketing/recovery email | Pass signed unsubscribe URL when LG known |
| `src/pages/...` + `AppRoutes.tsx` | Public `/unsubscribe` page |
| Tests | Service, API, webhook, presentation |

---

### Task 1: Ledger sources + shared withdraw service

**Files:**
- Modify: `backend/TummlyBackend/Models/LocationGuestPermissionLedgerEntry.cs` (`LocationGuestPermissionLedgerSources`)
- Create: `backend/TummlyBackend/Interfaces/IGuestInitiatedMarketingWithdrawService.cs`
- Create: `backend/TummlyBackend/Services/GuestInitiatedMarketingWithdrawService.cs`
- Modify: `backend/TummlyBackend/Program.cs` (DI scoped)
- Modify: `backend/TummlyBackend/Helpers/LocationGuestPermissionPresentation.cs` (display labels for new sources)
- Test: `backend/TummlyBackend.Tests/Services/GuestInitiatedMarketingWithdrawServiceTests.cs`

**Interfaces:**
- Consumes: `ILocationGuestPermissionLedgerService`, `ApplicationDbContext`
- Produces:

```csharp
public interface IGuestInitiatedMarketingWithdrawService
{
    Task<GuestInitiatedWithdrawResult> WithdrawForLocationGuestAsync(
        int locationGuestId,
        int restaurantId,
        LocationGuestPermissionKind kind,
        string ledgerSource,
        CancellationToken cancellationToken = default
    );

    Task<GuestInitiatedWithdrawResult> WithdrawForRestaurantContactAsync(
        int restaurantId,
        string normalizedEmailOrE164,
        bool isEmail,
        LocationGuestPermissionKind kind,
        string ledgerSource,
        CancellationToken cancellationToken = default
    );
}

public sealed record GuestInitiatedWithdrawResult(
    int GuestsTouched,
    int WithdrawalsWritten,
    bool ActivityEmitted
);
```

Ledger sources (exact):

```csharp
public const string EmailUnsubscribe = "email-unsubscribe";
public const string SmsStop = "sms-stop";
```

**Behaviour:**
1. Load Location Guest(s); enforce restaurant ownership via `RestaurantLocation.RestaurantId`.
2. Read current states; if kind already `Withdrawn`, skip ledger write for that guest (still count as touched).
3. Else `RecordEvent(..., Withdraw, ledgerSource, utcNow, actorUserId: null)` then `SyncMarketingPreferenceRollupAsync`.
4. If rollup is `OptedOut` **and** it was not already opted out before this call, add `LocationActivity` with `GuestMarketingUnsubscribed` (ActorUserId null, ActorDisplayName null or `"Guest"`).
5. Single `SaveChangesAsync` at end.
6. Contact match: email → `MasterGuest.NormalizedEmail`; phone → `MasterGuest.NormalizedPhone` (E164). Join LocationGuests → locations for restaurant.

- [ ] **Step 1: Write failing tests**

```csharp
[Fact]
public async Task WithdrawForLocationGuest_WritesEmailMarketingWithdraw_AndOptsOut()
{
    // Seed LG with EmailMarketing Granted + MarketingPreference Allowed
    var result = await _sut.WithdrawForLocationGuestAsync(
        locationGuestId, restaurantId,
        LocationGuestPermissionKind.EmailMarketing,
        LocationGuestPermissionLedgerSources.EmailUnsubscribe
    );
    Assert.Equal(1, result.WithdrawalsWritten);
    var states = await _permissions.GetCurrentStatesAsync(locationGuestId);
    Assert.Equal(LocationGuestPermissionState.Withdrawn, states[LocationGuestPermissionKind.EmailMarketing]);
    var lg = await _context.LocationGuests.SingleAsync();
    Assert.Equal(LocationGuestMarketingPreference.OptedOut, lg.MarketingPreference);
    Assert.Equal(1, await _context.LocationActivities.CountAsync(a =>
        a.Kind == LocationActivityKinds.GuestMarketingUnsubscribed));
}

[Fact]
public async Task WithdrawForLocationGuest_IsIdempotent_WhenAlreadyWithdrawn()
{
    // First withdraw, second withdraw
    Assert.Equal(0, second.WithdrawalsWritten);
    Assert.Equal(1, await _context.LocationGuestPermissionLedgerEntries.CountAsync(e =>
        e.EventKind == LocationGuestPermissionLedgerEventKinds.Withdraw));
}

[Fact]
public async Task WithdrawForRestaurantContact_Email_OnlyTouchesThatRestaurant()
{
    // Two restaurants, same NormalizedEmail → only restaurant A withdrawn
}
```

- [ ] **Step 2: Run RED**

```bash
cd backend && dotnet test TummlyBackend.Tests/TummlyBackend.Tests.csproj --filter "FullyQualifiedName~GuestInitiatedMarketingWithdrawServiceTests" -v n
```

Expected: FAIL (type missing).

- [ ] **Step 3: Implement service + sources + DI**

- [ ] **Step 4: Run GREEN** — same filter, PASS.

- [ ] **Step 5: Commit** (only if human asked)

---

### Task 2: Unsubscribe HMAC token + public Email API

**Files:**
- Create: `backend/TummlyBackend/Helpers/UnsubscribeToken.cs`
- Create: `backend/TummlyBackend/Controllers/PublicUnsubscribeController.cs`
- Create: DTOs under `backend/TummlyBackend/DTOs/PublicUnsubscribe/`
- Test: `backend/TummlyBackend.Tests/Helpers/UnsubscribeTokenTests.cs`
- Test: `backend/TummlyBackend.Tests/Integration/PublicUnsubscribeEndpointsTests.cs`

**Interfaces:**
- Consumes: `IGuestInitiatedMarketingWithdrawService`, `IConfiguration` (`JwtSettings:Secret`)
- Produces:

```csharp
public static class UnsubscribeToken
{
    public const int DefaultTtlHours = 90 * 24; // 90 days

    public static string Create(
        int locationGuestId,
        int restaurantId,
        DateTime issuedAtUtc,
        string secret,
        TimeSpan? ttl = null
    );

    public static bool TryVerify(
        string token,
        string secret,
        out int locationGuestId,
        out int restaurantId,
        out string? error
    );
}
```

Token format (URL-safe): `base64url(payload).base64url(hmacsha256)`  
Payload JSON: `{ "lg": locationGuestId, "r": restaurantId, "iat": unix, "exp": unix }`

**Controller** `[AllowAnonymous]` `[Route("api/public/unsubscribe")]`:

```csharp
// GET api/public/unsubscribe/preview?t=...
// → { valid: true, restaurantName } or { valid: false } (no PII)

// POST api/public/unsubscribe/confirm
// body: { token: "..." }
// → withdraw EmailMarketing for token LG; generic { success: true }

// POST api/public/unsubscribe/form
// body: { email: "...", restaurantId: 123 }
// → normalize email; WithdrawForRestaurantContactAsync EmailMarketing;
//    always { success: true } (do not reveal match count)
```

CSRF: state change is **POST only**. GET preview is read-only.

- [ ] **Step 1: Failing token + endpoint tests** (tamper, expiry, happy confirm, form restaurant isolation, form always success when zero matches)

- [ ] **Step 2: RED**

- [ ] **Step 3: Implement token + controller + register in Program if needed**

- [ ] **Step 4: GREEN**

- [ ] **Step 5: Commit** (only if human asked)

---

### Task 3: Frontend `/unsubscribe` page

**Files:**
- Create: `src/pages/public/UnsubscribePage.tsx` (or under `src/components/public/`)
- Create: `src/api/publicUnsubscribeApi.ts`
- Modify: `src/pages/routes/AppRoutes.tsx` — public route `path="unsubscribe"`
- Test: Vitest for copy / query handling if presentation helper extracted

**Behaviour:**
1. If `?t=` present: load preview; show restaurant name + Confirm button → POST confirm → success message.
2. If no token: show form (email + restaurantId from `?restaurantId=` if present). If `restaurantId` missing, show: “Use the Unsubscribe link in your email, or ask the restaurant for their unsubscribe page.” Still allow email field only when `restaurantId` query is present.
3. Success copy is generic (“You’re unsubscribed from marketing emails for this restaurant.”).
4. No operator chrome; match simple public legal page shell if one exists (`LegalPageShell` or minimal layout).

- [ ] **Step 1: Failing route/render test or API client parse test**

- [ ] **Step 2: RED**

- [ ] **Step 3: Implement page + route + API client**

- [ ] **Step 4: GREEN** (`npx vitest run` on new tests)

- [ ] **Step 5: Commit** (only if human asked)

---

### Task 4: Email template signed unsubscribe URL

**Files:**
- Modify: `backend/TummlyBackend/Helpers/EmailTemplates/BaseNonTransactionalEmailTemplate.cs`
- Modify callers that send guest marketing / campaign / recovery email where `LocationGuestId` is known (search `BaseNonTransactionalEmailTemplate.Generate` and campaign email builders)
- Test: unit test that Generate uses `unsubscribeHref` when provided; default remains `{baseUrl}/unsubscribe`

**Change:**

```csharp
public static string Generate(
    ...
    GuestResponseEmailOfferBlock? offer = null,
    string? unsubscribeHref = null
)
{
    var unsubscribeUrl = string.IsNullOrWhiteSpace(unsubscribeHref)
        ? $"{baseUrl}/unsubscribe"
        : unsubscribeHref.Trim();
    // footer uses unsubscribeUrl
}
```

Helper for callers:

```csharp
// UnsubscribeLink.ForLocationGuest(frontendBaseUrl, locationGuestId, restaurantId, secret)
// → $"{baseUrl}/unsubscribe?t={token}"
```

Wire at least one real marketing/campaign send path that has LocationGuestId (prefer campaign email composer / guest response if marketing). Recovery guest response emails should also get signed links when LG present. Invoice-only templates may keep bare `/unsubscribe` or omit change if not marketing.

- [ ] **Step 1: Failing template test for custom href**

- [ ] **Step 2: RED**

- [ ] **Step 3: Implement + update primary guest marketing send site(s)**

- [ ] **Step 4: GREEN**

- [ ] **Step 5: Commit** (only if human asked)

---

### Task 5: Twilio SMS STOP webhook + number map

**Files:**
- Modify: `backend/TummlyBackend/Configurations/TwilioSettings.cs` — add:

```csharp
/// <summary>
/// E.164 (or Twilio sender id) → restaurantId for inbound STOP routing.
/// Key format: digits with leading +, e.g. +447700900123.
/// </summary>
public Dictionary<string, int> InboundNumberRestaurants { get; set; } = new();
```

- Bind in `Program.cs` from config section `TwilioSettings:InboundNumberRestaurants` (or `Twilio__InboundNumberRestaurants__+44...=id` — document in `.env.example`)
- Create: `backend/TummlyBackend/Services/TwilioSmsInboundService.cs` (parse form, validate signature, keyword, withdraw)
- Create: `backend/TummlyBackend/Controllers/TwilioSmsInboundController.cs`

```csharp
[AllowAnonymous]
[HttpPost("/api/webhooks/twilio/sms-inbound")]
public async Task<IActionResult> Inbound()
{
    // read form: From, To, Body
    // validate signature using AuthToken + full URL + form params (Twilio.Security.RequestValidator)
    // if invalid → 403
    // if STOP keyword → resolve restaurant from To map → withdraw SmsMarketing for From E164
    // return 200 text/xml empty Response
}
```

STOP keywords exact set: `STOP`, `STOPALL`, `UNSUBSCRIBE`, `CANCEL`, `END`, `QUIT` (trim, case-insensitive, whole body or first word — lock **whole body trimmed** equals keyword).

Unmapped To: log warning; 200; no withdraw.

Also: if `To` equals `RecoveryFromNumber` and map empty, **still** no auto-all-restaurant withdraw (fail closed).

- [ ] **Step 1: Failing tests** — signature fail 403; STOP mapped withdraws SmsMarketing; non-STOP no write; unmapped no write + 200; eligibility blocked after STOP (optional focused eligibility assert)

- [ ] **Step 2: RED**

- [ ] **Step 3: Implement settings, service, controller, `.env.example` note**

- [ ] **Step 4: GREEN**

- [ ] **Step 5: Commit** (only if human asked)

---

### Task 6: Whole-feature smoke

**Files:** none (verify)

- [ ] **Step 1: Backend filter**

```bash
cd backend && dotnet test TummlyBackend.Tests/TummlyBackend.Tests.csproj --filter "FullyQualifiedName~GuestInitiatedMarketingWithdrawServiceTests|FullyQualifiedName~UnsubscribeTokenTests|FullyQualifiedName~PublicUnsubscribeEndpointsTests|FullyQualifiedName~TwilioSmsInbound" -v n
```

Expected: PASS.

- [ ] **Step 2: Frontend**

```bash
npx vitest run src/api/publicUnsubscribeApi.ts src/pages/public 2>/dev/null; npx vitest run --dir src --testPathPattern unsubscribe
```

Adjust to actual test paths. Expected: PASS.

- [ ] **Step 3: Spec checklist**

| Spec item | Task |
|-----------|------|
| Shared ledger withdraw | 1 |
| One-click + form API | 2 |
| Public page | 3 |
| Signed email footer | 4 |
| STOP webhook + map | 5 |
| No suppression table | all |
| Unmapped To fail closed | 5 |

- [ ] **Step 4: Commit** (only if human asked)

---

## Self-review (plan vs spec)

1. **Coverage:** Email one-click, form, STOP, shared service, restaurant scope, SmsMarketing-only, idempotent, security — all tasked. List-Unsubscribe headers / START — out of scope.
2. **Placeholders:** Token format, sources, keywords, POST-only confirm locked.
3. **Types:** `GuestInitiatedWithdrawResult` and `UnsubscribeToken` signatures consistent across tasks.
