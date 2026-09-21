# Launch Guest Form Permission Evidence Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Persist Guest Form permission evidence (`basis`, form/wording/PN versions, wording snapshot) on ledger grants/withdraws, and reject invalid Email/UK-mobile contacts on submit.

**Architecture:** Nullable evidence columns on `LocationGuestPermissionLedgerEntry` plus a `PermissionLedgerEvidence` record and a new `RecordEvent(..., evidence)` overload. `GuestFormPermissionApplyService` is the only writer that fills evidence. `ScanController.SubmitFeedback` hardens contact validation before upsert. Optional CSV columns for operator export.

**Tech Stack:** ASP.NET Core, EF Core migration, xUnit InMemory tests, existing `PhoneNumberHelper` / `EmailAddressAttribute`.

**Spec:** [docs/superpowers/specs/2026-09-21-launch-guest-form-permission-evidence-design.md](../specs/2026-09-21-launch-guest-form-permission-evidence-design.md)

## Global Constraints

- Source string stays **`guest-form`** (do not rename to `guest_form`).
- Evidence stamped on **Guest Form submit only**; other writers leave evidence columns null.
- Keep FeedbackFollowUp restaurant toggle gate; do not change grant rules.
- Marketing withdraw: **null** `Basis`; still stamp versions + snapshot.
- Contact: reject unless valid Email or UK mobile; store trimmed submitted string (no E.164 rewrite).
- No backfill of historical ledger rows.
- Commit only when the human asks (skip Commit steps unless asked).
- Subagents: Cursor Auto / `inherit` only.
- Report to the human in ASD-STE100 Simplified Technical English.

---

## File map

| File | Role |
|---|---|
| `backend/TummlyBackend/Models/LocationGuestPermissionLedgerEntry.cs` | Evidence columns + basis constants |
| `backend/TummlyBackend/Models/PermissionLedgerEvidence.cs` | Evidence record |
| `backend/TummlyBackend/Interfaces/ILocationGuestPermissionLedgerService.cs` | Evidence `RecordEvent` overload |
| `backend/TummlyBackend/Services/LocationGuestPermissionLedgerService.cs` | Implement overload |
| `backend/TummlyBackend/Data/ApplicationDbContext.cs` | MaxLength config for new columns |
| `backend/TummlyBackend/Migrations/*_AddPermissionLedgerEvidenceColumns.*` | EF migration |
| `backend/TummlyBackend/Helpers/GuestFormPermissionEvidence.cs` | Version constants + snapshot builders |
| `backend/TummlyBackend/Helpers/GuestFormContactValidate.cs` | Email / UK mobile → `ContactType` |
| `backend/TummlyBackend/Services/GuestFormPermissionApplyService.cs` | Stamp evidence on each event |
| `backend/TummlyBackend/Controllers/ScanController.cs` | Contact gate before upsert |
| `backend/TummlyBackend/Services/PrivacyConsentPermissionRecordsExportService.cs` | CSV basis + version columns |
| Tests under `backend/TummlyBackend.Tests/` | Ledger, apply, contact, export |

---

### Task 1: Ledger evidence columns + RecordEvent overload

**Files:**
- Modify: `backend/TummlyBackend/Models/LocationGuestPermissionLedgerEntry.cs`
- Create: `backend/TummlyBackend/Models/PermissionLedgerEvidence.cs`
- Modify: `backend/TummlyBackend/Interfaces/ILocationGuestPermissionLedgerService.cs`
- Modify: `backend/TummlyBackend/Services/LocationGuestPermissionLedgerService.cs`
- Modify: `backend/TummlyBackend/Data/ApplicationDbContext.cs` (ledger fluent config)
- Create: EF migration `AddPermissionLedgerEvidenceColumns` (+ Designer + snapshot)
- Test: `backend/TummlyBackend.Tests/Services/LocationGuestPermissionLedgerServiceTests.cs`

**Interfaces:**
- Consumes: existing `RecordEvent(LocationGuest, ...)` without evidence
- Produces:

```csharp
// Models/PermissionLedgerEvidence.cs
namespace TummlyBackend.Models
{
    public sealed record PermissionLedgerEvidence(
        string? Basis,
        string GuestFormVersion,
        string WordingVersion,
        string PrivacyNoticeVersion,
        string WordingSnapshot
    );
}

// LocationGuestPermissionLedgerEntry — add properties:
[MaxLength(64)]
public string? Basis { get; set; }

[MaxLength(32)]
public string? GuestFormVersion { get; set; }

[MaxLength(32)]
public string? WordingVersion { get; set; }

[MaxLength(32)]
public string? PrivacyNoticeVersion { get; set; }

[MaxLength(512)]
public string? WordingSnapshot { get; set; }

// Basis constants (same file as EventKinds / Sources, or nested static class):
public static class LocationGuestPermissionBases
{
    public const string Consent = "consent";
    public const string ServiceFollowUpNotice = "service_follow_up_notice";
}
```

New interface overload (LocationGuest navigation form — Guest Form path):

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

- [ ] **Step 1: Write the failing test**

In `LocationGuestPermissionLedgerServiceTests.cs`:

```csharp
[Fact]
public async Task RecordEvent_WithEvidence_PersistsBasisVersionsAndSnapshot()
{
    var guest = await SeedLocationGuestAsync(
        LocationGuestMarketingPreference.NotRecorded
    );
    var at = new DateTime(2026, 9, 21, 12, 0, 0, DateTimeKind.Utc);
    var evidence = new PermissionLedgerEvidence(
        Basis: LocationGuestPermissionBases.Consent,
        GuestFormVersion: "guest-form-v1",
        WordingVersion: "email-marketing-v1",
        PrivacyNoticeVersion: "privacy-notice-v1",
        WordingSnapshot: "Yes, email me occasional offers and updates from Cafe."
    );

    _ledger.RecordEvent(
        guest,
        guest.RestaurantLocationId,
        LocationGuestPermissionKind.EmailMarketing,
        LocationGuestPermissionLedgerEventKinds.Grant,
        LocationGuestPermissionLedgerSources.GuestForm,
        at,
        evidence
    );
    await _context.SaveChangesAsync();

    var row = await _context.LocationGuestPermissionLedgerEntries
        .AsNoTracking()
        .SingleAsync();
    Assert.Equal(LocationGuestPermissionBases.Consent, row.Basis);
    Assert.Equal("guest-form-v1", row.GuestFormVersion);
    Assert.Equal("email-marketing-v1", row.WordingVersion);
    Assert.Equal("privacy-notice-v1", row.PrivacyNoticeVersion);
    Assert.Equal(evidence.WordingSnapshot, row.WordingSnapshot);
}

[Fact]
public async Task RecordEvent_WithoutEvidence_LeavesEvidenceColumnsNull()
{
    var guest = await SeedLocationGuestAsync(
        LocationGuestMarketingPreference.NotRecorded
    );
    var at = new DateTime(2026, 9, 21, 12, 0, 0, DateTimeKind.Utc);

    _ledger.RecordEvent(
        guest.Id,
        guest.RestaurantLocationId,
        LocationGuestPermissionKind.EmailMarketing,
        LocationGuestPermissionLedgerEventKinds.Grant,
        LocationGuestPermissionLedgerSources.GuestForm,
        at
    );
    await _context.SaveChangesAsync();

    var row = await _context.LocationGuestPermissionLedgerEntries
        .AsNoTracking()
        .SingleAsync();
    Assert.Null(row.Basis);
    Assert.Null(row.GuestFormVersion);
    Assert.Null(row.WordingVersion);
    Assert.Null(row.PrivacyNoticeVersion);
    Assert.Null(row.WordingSnapshot);
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run:

```bash
dotnet test backend/TummlyBackend.Tests/TummlyBackend.Tests.csproj \
  --filter "FullyQualifiedName~LocationGuestPermissionLedgerServiceTests.RecordEvent_WithEvidence|FullyQualifiedName~LocationGuestPermissionLedgerServiceTests.RecordEvent_WithoutEvidence"
```

Expected: FAIL (missing type / overload / properties).

- [ ] **Step 3: Implement model + overload + fluent config**

1. Add properties + `LocationGuestPermissionBases` on/near `LocationGuestPermissionLedgerEntry`.
2. Add `PermissionLedgerEvidence` record.
3. Add interface overload; implement by copying the navigation `RecordEvent` and assigning evidence fields from the record (null-check `evidence`; throw `ArgumentNullException` if null).
4. In `ApplicationDbContext` ledger section, set max lengths matching attributes (`Basis` 64, versions 32, snapshot 512). Nullable = true.

- [ ] **Step 4: Add EF migration**

From `backend/TummlyBackend`:

```bash
dotnet ef migrations add AddPermissionLedgerEvidenceColumns
```

Confirm `Up` adds five nullable columns on `LocationGuestPermissionLedgerEntries` with correct lengths. Do not backfill.

- [ ] **Step 5: Run tests to verify they pass**

Same `dotnet test` filter as Step 2. Expected: PASS.

- [ ] **Step 6: Commit** (skip unless human asks)

---

### Task 2: Evidence helper + GuestFormPermissionApplyService stamps

**Files:**
- Create: `backend/TummlyBackend/Helpers/GuestFormPermissionEvidence.cs`
- Modify: `backend/TummlyBackend/Helpers/GuestFormConsentCopy.cs` (optional: add marketing label builders if kept DRY)
- Modify: `backend/TummlyBackend/Services/GuestFormPermissionApplyService.cs`
- Test: `backend/TummlyBackend.Tests/Helpers/GuestFormPermissionEvidenceTests.cs`
- Test: `backend/TummlyBackend.Tests/Services/GuestFormPermissionApplyServiceTests.cs` (create if missing; or extend ledger tests that call apply)

**Interfaces:**
- Consumes: `PermissionLedgerEvidence`, `RecordEvent(..., evidence)`, `Restaurant` wording fields, `GuestFormConsentCopy.FeedbackFollowUpWording`
- Produces:

```csharp
public static class GuestFormPermissionEvidence
{
    public const string GuestFormVersion = "guest-form-v1";
    public const string PrivacyNoticeVersion = "privacy-notice-v1";
    public const string EmailMarketingWordingVersion = "email-marketing-v1";
    public const string SmsMarketingWordingVersion = "sms-marketing-v1";
    public const string FeedbackFollowUpWordingVersion = "feedback-follow-up-v1";

    public static PermissionLedgerEvidence ForFeedbackFollowUpGrant(
        string restaurantName
    );

    public static PermissionLedgerEvidence ForMarketingGrant(
        ContactType contactType,
        string restaurantName,
        string? restaurantCustomWording
    );

    public static PermissionLedgerEvidence ForMarketingWithdraw(
        ContactType contactType,
        string restaurantName,
        string? restaurantCustomWording
    );
}
```

**Snapshot rules (exact):**

1. FeedbackFollowUp grant — full intro matching frontend `buildGuestFormIntroCopy`:

```text
Your feedback is shared privately with {Restaurant}. They may contact you about this feedback using the details you provide.
```

Use trimmed restaurant name or `"this restaurant"` when empty. Basis = `service_follow_up_notice`.

2. Marketing grant — if `restaurantCustomWording` is non-whitespace, use trimmed custom text; else launch checkbox label matching frontend:

- Email: `Yes, email me occasional offers and updates from {Restaurant}. You can unsubscribe at any time.`
- SMS: `Yes, text me occasional offers and updates from {Restaurant}. You can opt out at any time.`

Basis = `consent`. WordingVersion = email or sms constant from `ContactType`.

3. Marketing withdraw — same snapshot rule as grant for that channel; **Basis = null**.

Truncate snapshot to 512 chars if needed (defensive).

- [ ] **Step 1: Write the failing helper + apply tests**

`GuestFormPermissionEvidenceTests.cs`:

```csharp
[Fact]
public void ForMarketingGrant_PrefersRestaurantCustomWording()
{
    var evidence = GuestFormPermissionEvidence.ForMarketingGrant(
        ContactType.Email,
        "Cafe",
        "Custom email wording from operator"
    );
    Assert.Equal(LocationGuestPermissionBases.Consent, evidence.Basis);
    Assert.Equal("Custom email wording from operator", evidence.WordingSnapshot);
    Assert.Equal(
        GuestFormPermissionEvidence.EmailMarketingWordingVersion,
        evidence.WordingVersion
    );
}

[Fact]
public void ForMarketingWithdraw_HasNullBasis()
{
    var evidence = GuestFormPermissionEvidence.ForMarketingWithdraw(
        ContactType.Phone,
        "Cafe",
        null
    );
    Assert.Null(evidence.Basis);
    Assert.Contains("text me occasional offers", evidence.WordingSnapshot);
}
```

`GuestFormPermissionApplyServiceTests.cs` (new file; InMemory seed like ledger tests):

```csharp
[Fact]
public async Task ApplyOnSubmitAsync_MarketingGrant_StampsConsentEvidence()
{
    // Seed restaurant with EmailMarketingPermissionEnabled=true,
    // FeedbackFollowUpPermissionEnabled=true, Name="Cafe".
    // Unsaved or saved LocationGuest; marketingConsentGranted=true; ContactType.Email.
    // Assert Added ledger rows:
    // - EmailMarketing grant: Basis=consent, GuestFormVersion, EmailMarketingWordingVersion, PN version, snapshot contains "email me"
    // - FeedbackFollowUp grant: Basis=service_follow_up_notice, FeedbackFollowUpWordingVersion, snapshot contains "shared privately"
}

[Fact]
public async Task ApplyOnSubmitAsync_MarketingWithdraw_StampsNullBasis()
{
    // Prior EmailMarketing grant on guest; marketingConsentGranted=false.
    // Assert withdraw row: Basis null; versions + snapshot present.
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test backend/TummlyBackend.Tests/TummlyBackend.Tests.csproj \
  --filter "FullyQualifiedName~GuestFormPermissionEvidenceTests|FullyQualifiedName~GuestFormPermissionApplyServiceTests"
```

Expected: FAIL.

- [ ] **Step 3: Implement helper + apply wiring**

In `GuestFormPermissionApplyService.ApplyOnSubmitAsync`, when calling `RecordEvent`, pass evidence:

```csharp
foreach (var (kind, eventKind) in events)
{
    var evidence = BuildEvidence(
        kind,
        eventKind,
        contactType,
        restaurant
    );
    _ledger.RecordEvent(
        locationGuest,
        restaurantLocationId,
        kind,
        eventKind,
        LocationGuestPermissionLedgerSources.GuestForm,
        occurredAt,
        evidence
    );
}
```

`BuildEvidence` private static: map kind+eventKind to the three helper methods; for FeedbackFollowUp only grant is emitted.

Restaurant custom wording:

- Email → `restaurant.EmailConsentWording`
- Phone → `restaurant.SmsConsentWording`

- [ ] **Step 4: Run tests to verify they pass**

Same filter. Expected: PASS. Also re-run:

```bash
dotnet test backend/TummlyBackend.Tests/TummlyBackend.Tests.csproj \
  --filter "FullyQualifiedName~LocationGuestPermissionLedgerServiceTests"
```

Expected: PASS (including unsaved-guest apply test).

- [ ] **Step 5: Commit** (skip unless human asks)

---

### Task 3: Harden submit contact validation

**Files:**
- Create: `backend/TummlyBackend/Helpers/GuestFormContactValidate.cs`
- Modify: `backend/TummlyBackend/Controllers/ScanController.cs` (`SubmitFeedback`, remove or stop using weak `DetectContactType` for submit)
- Test: `backend/TummlyBackend.Tests/Helpers/GuestFormContactValidateTests.cs`
- Test: extend `backend/TummlyBackend.Tests/Integration/GuestFormPermissionAndChannelEligibilityTests.cs` **or** add focused integration tests for invalid contact → 400

**Interfaces:**
- Consumes: `PhoneNumberHelper.TryNormalizeToE164`, `System.ComponentModel.DataAnnotations.EmailAddressAttribute`
- Produces:

```csharp
public static class GuestFormContactValidate
{
    /// <summary>
    /// True when contact is a valid Email or UK mobile.
    /// Sets contactType to Email or Phone. Does not rewrite the contact string.
    /// </summary>
    public static bool TryResolve(
        string? contact,
        out ContactType contactType
    );
}
```

Rules:

1. Trim; empty → false, `Unknown`.
2. If contains `@` and `new EmailAddressAttribute().IsValid(trimmed) == true` → Email.
3. Else if `PhoneNumberHelper.TryNormalizeToE164(trimmed, PhoneNumberHelper.DefaultRegion, out _)` → Phone.
4. Else false.

In `SubmitFeedback`, after length checks and **before** rate-limit consume / upsert:

```csharp
if (!GuestFormContactValidate.TryResolve(dto.GuestContact, out var contactType))
{
    return BadRequest(new
    {
        success = false,
        message = "Enter a valid email or UK mobile number.",
    });
}
```

Use `contactType` downstream; delete private `DetectContactType` if unused.

- [ ] **Step 1: Write the failing tests**

```csharp
[Theory]
[InlineData("guest@example.com", ContactType.Email)]
[InlineData("07700900123", ContactType.Phone)]
[InlineData("+447700900123", ContactType.Phone)]
public void TryResolve_AcceptsValid(string contact, ContactType expected)
{
    Assert.True(GuestFormContactValidate.TryResolve(contact, out var type));
    Assert.Equal(expected, type);
}

[Theory]
[InlineData("")]
[InlineData("not-an-email")]
[InlineData("12345")]
[InlineData("+15551234567")] // US — not UK
public void TryResolve_RejectsInvalid(string contact)
{
    Assert.False(GuestFormContactValidate.TryResolve(contact, out var type));
    Assert.Equal(ContactType.Unknown, type);
}
```

Integration: POST feedback with `GuestContact = "nope"` → 400; assert no new `Feedback` / ledger rows for that token.

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test backend/TummlyBackend.Tests/TummlyBackend.Tests.csproj \
  --filter "FullyQualifiedName~GuestFormContactValidateTests"
```

Expected: FAIL.

- [ ] **Step 3: Implement helper + ScanController gate**

- [ ] **Step 4: Run tests to verify they pass**

Include Guest Form integration filter:

```bash
dotnet test backend/TummlyBackend.Tests/TummlyBackend.Tests.csproj \
  --filter "FullyQualifiedName~GuestFormContactValidateTests|FullyQualifiedName~GuestFormPermissionAndChannelEligibilityTests"
```

Expected: PASS.

- [ ] **Step 5: Commit** (skip unless human asks)

---

### Task 4: Permission records CSV — basis + versions

**Files:**
- Modify: `backend/TummlyBackend/Services/PrivacyConsentPermissionRecordsExportService.cs`
- Modify: `backend/TummlyBackend.Tests/Integration/PrivacyConsentPermissionRecordsExportEndpointsTests.cs` (or service-level test if present)

**Interfaces:**
- Consumes: new nullable columns on ledger entries
- Produces: CSV headers extended:

```csharp
private static readonly string[] Headers =
[
    "Guest",
    "Permission",
    "Current state",
    "Location",
    "Source",
    "Basis",
    "Guest form version",
    "Wording version",
    "Privacy notice version",
    "Recorded",
];
```

Select + emit the four evidence fields (empty string when null). Keep soft-max behaviour.

- [ ] **Step 1: Write / update failing export assertion**

Seed a Guest Form grant with evidence filled; export CSV; assert header contains `Basis` and a data cell equals `consent`.

- [ ] **Step 2: Run test to verify it fails**

- [ ] **Step 3: Implement header + row mapping**

- [ ] **Step 4: Run test to verify it passes**

- [ ] **Step 5: Commit** (skip unless human asks)

---

### Task 5: Smoke + final review

**Files:** none required beyond fixes from review.

- [ ] **Step 1: Focused backend smoke**

```bash
dotnet test backend/TummlyBackend.Tests/TummlyBackend.Tests.csproj \
  --filter "FullyQualifiedName~LocationGuestPermissionLedgerServiceTests|FullyQualifiedName~GuestFormPermissionEvidenceTests|FullyQualifiedName~GuestFormPermissionApplyServiceTests|FullyQualifiedName~GuestFormContactValidateTests|FullyQualifiedName~GuestFormPermissionAndChannelEligibilityTests|FullyQualifiedName~PrivacyConsentPermissionRecordsExport"
```

Expected: all PASS.

- [ ] **Step 2: Spec coverage check**

Confirm against `2026-09-21-launch-guest-form-permission-evidence-design.md`:

| Spec item | Task |
|-----------|------|
| Evidence columns + overload | 1 |
| Guest Form stamps basis/versions/snapshot | 2 |
| Contact 400 gate | 3 |
| CSV preferred surface | 4 |
| No rename source / no follow-up rule change / no backfill | Constraints |

- [ ] **Step 3: Final review via subagent** (if using subagent-driven-development)

- [ ] **Step 4: Commit** (skip unless human asks)

---

## Self-review (plan author)

1. **Spec coverage:** Model, RecordEvent, apply stamps, contact gate, CSV surface, tests, out-of-scope constraints — covered.
2. **Placeholders:** None intentional; version string literals locked to `guest-form-v1` / `privacy-notice-v1` / channel `*-v1`.
3. **Type consistency:** `PermissionLedgerEvidence` and `LocationGuestPermissionBases` names match across tasks.
