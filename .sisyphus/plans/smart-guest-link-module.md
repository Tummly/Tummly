# Plan: Extract ISmartGuestLinkService — token generate, resolve, and URL shape

## TL;DR

> Smart Guest Link token generation, resolution, and URL shape are spread across 5+ files in two languages. ADR-0001’s invariants (opaque, non-enumerable, random) are enforced nowhere centrally. This plan creates a standalone `ISmartGuestLinkService` owning `GenerateToken`, dual resolve paths, and `BuildGuestUrl`; callers delegate; operator API returns `guestUrl` instead of raw `linkToken`.

**Quick overview:**
- **Core objective:** One backend module for the Smart Guest Link token concept (ADR-0001)
- **Key deliverables:** `ISmartGuestLinkService`, controller delegation, `guestUrl` on locations API, dashboard frontend update, ADR note, tests
- **Estimated effort:** Medium — backend extraction + frontend type/prop update + test migration
- **Parallel execution:** NO — sequential backend-first, then frontend
- **Critical path:** Service → Provisioning inject → Scan/Qr/Restaurant → Frontend → Tests
- **Prerequisite:** Candidate #1 provisioning module complete (`GuestLoopProvisioningService` currently holds private `GenerateLinkToken`)

---

## Context

### Original problem statement (Architecture review Candidate #2)

Smart Guest Link token **generation** (crypto random in provisioning), **resolution** (duplicated in `ScanController` GET + POST), and **URL shape** (`QrController` uses `Frontend:BaseUrl`; `DashboardContent` uses `window.location.origin`) can drift independently. The migration backfill used `NEWID()` hex — a third strategy the ADR did not contemplate.

**Affected files today:**
- `backend/TummlyBackend/Services/GuestLoopProvisioningService.cs` — private `GenerateLinkToken()`
- `backend/TummlyBackend/Controllers/ScanController.cs` — inline resolve ×2
- `backend/TummlyBackend/Controllers/QrController.cs` — `GetFrontendBaseUrl()` + `/scan/{token}` concat
- `backend/TummlyBackend/Controllers/RestaurantController.cs` — selects `LinkToken`
- `backend/TummlyBackend/Migrations/20260620111110_AddLinkTokenToRestaurantLocation.cs` — `NEWID()` backfill
- `src/components/dashboard/DashboardContent.tsx` — `` `${window.location.origin}/scan/${linkToken}` ``

### Interview summary (grill-with-docs, 2026-06-21)

| # | Decision | Choice |
|---|----------|--------|
| 1 | Seam placement | **Standalone `ISmartGuestLinkService`** — not on `IProvisioningService` (provisioning only calls `GenerateToken`) |
| 2 | Who builds guest URL on frontend | **Backend returns `guestUrl`** via locations API |
| 3 | Resolve API shape | **Two methods:** read DTO + tracked entity for writes |
| 4 | Token collision | **Retry with cap**, then typed exception |
| 5 | `NEWID()` hex backfill rows | **Leave as-is** — do not re-tokenize (protects printed QRs) |
| 6 | `GetFrontendBaseUrl` dedup (Candidate #12) | **In scope for QR path only** — service reads config for `BuildGuestUrl`; Admin/Auth dedup deferred |
| 7 | Operator locations API | **`guestUrl` only** — drop `linkToken` from operator-facing responses |
| 8 | Document hex vs alphanumeric split | **Implementation note on ADR-0001** |
| 9 | `ScanController` scope | **Resolve delegation only** — no Candidate #9 feedback-submission extraction |

### Relationship to Candidate #1

Provisioning plan deliberately **absorbed generation** into `GuestLoopProvisioningService` as a stepping stone. This plan **moves generation** into `ISmartGuestLinkService` and injects it into provisioning — completing Candidate #2 without expanding provisioning’s surface.

---

## Work Objectives

### Core objective

Create `ISmartGuestLinkService` as the single owner of Smart Guest Link token invariants: generate (crypto, 32-char, uniqueness retry), resolve (read vs write), and build canonical guest URL (`Frontend:BaseUrl + /scan/{token}`). Controllers become thin delegates. Operator dashboard consumes `guestUrl`; no client-side URL construction.

### Concrete deliverables

- `ISmartGuestLinkService` interface
- `SmartGuestLinkService` implementation
- `GuestLinkLocationInfo` read DTO
- `LinkTokenGenerationException`
- `GuestLoopProvisioningService` injects service for `GenerateToken()` (delete private method)
- `ScanController` delegates resolve (both actions)
- `QrController` delegates `BuildGuestUrl()` (drop private `GetFrontendBaseUrl` usage for QR text)
- `RestaurantController` returns `guestUrl` per location (no `linkToken`)
- Frontend dashboard types + components use `guestUrl`
- ADR-0001 implementation note (hex backfill)
- Test coverage for `ISmartGuestLinkService`

### Definition of done

- [ ] `ISmartGuestLinkService` has four public methods with typed signatures
- [ ] No inline `LinkToken ==` queries remain in `ScanController`
- [ ] No inline `/scan/{token}` URL concat in `QrController` or `DashboardContent`
- [ ] `RestaurantController` locations response includes `guestUrl`, not `linkToken`
- [ ] `GuestLoopProvisioningService` has no private `GenerateLinkToken`
- [ ] ADR-0001 has implementation note on migration backfill vs runtime generation
- [ ] All existing tests pass
- [ ] New tests cover generate (length, charset, retry), resolve (read/write), and `BuildGuestUrl`

### Must have

- Token normalize: trim whitespace before resolve (centralized in service)
- `GenerateToken()`: 32 chars, crypto RNG, alphanumeric charset (existing algorithm)
- Uniqueness retry (cap 5) before assign; `LinkTokenGenerationException` if exhausted
- `BuildGuestUrl()`: reads and validates `Frontend:BaseUrl` (same rules as current `QrController`)
- `ResolveForGuestAsync`: `AsNoTracking`, includes restaurant name for feedback form
- `ResolveLocationForWriteAsync`: tracked entity for feedback insert

### Must NOT have (guardrails)

- Do NOT change database schema or re-tokenize `NEWID()` backfill rows
- Do NOT extract feedback submission module (Candidate #9)
- Do NOT deduplicate `GetFrontendBaseUrl` in AdminService/AuthService (Candidate #12 deferred)
- Do NOT change guest-feedback form UX or `scanApi.ts` public API paths
- Do NOT expose raw `linkToken` on operator locations API
- Do NOT introduce repository abstraction (direct `DbContext` is fine)

---

## Interface sketch

```csharp
public interface ISmartGuestLinkService
{
    /// <summary>Crypto-random 32-char token; retries on DB uniqueness collision.</summary>
    Task<string> GenerateTokenAsync();

    /// <summary>Read-only resolve for guest feedback form metadata.</summary>
    Task<GuestLinkLocationInfo?> ResolveForGuestAsync(string token);

    /// <summary>Tracked resolve for feedback persistence.</summary>
    Task<RestaurantLocation?> ResolveLocationForWriteAsync(string token);

    /// <summary>Canonical Smart Guest Link URL: {Frontend:BaseUrl}/scan/{token}</summary>
    string BuildGuestUrl(string linkToken);
}

public class GuestLinkLocationInfo
{
    public string RestaurantName { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
}
```

---

## Verification Strategy

> ZERO HUMAN INTERVENTION — all verification is agent-executed.

### Test decision

- **Infrastructure exists:** YES (`TummlyBackend.Tests`, Vitest)
- **Automated tests:** YES — after implementation
- **Agent-executed QA:** YES — `dotnet build`, `dotnet test`, `npm run test`

### QA policy

Evidence saved to `.sisyphus/evidence/sgl-task-{N}-{scenario-slug}.{ext}`.

- **Backend:** `dotnet build --no-restore`, `dotnet test --no-restore`
- **Frontend:** `npm run test`, `npm run typecheck`

---

## Execution Strategy

```
Phase 1: Backend service ─────────────────────────────────────── Wave 1
  ├─ T1: Create ISmartGuestLinkService + DTO + exception
  ├─ T2: Implement SmartGuestLinkService
  └─ T3: Register in Program.cs

Phase 2: Backend delegation ──────────────────────────────────── Wave 2
  ├─ T4: GuestLoopProvisioningService → inject GenerateTokenAsync
  ├─ T5: ScanController → dual resolve delegation
  ├─ T6: QrController → BuildGuestUrl delegation
  └─ T7: RestaurantController → guestUrl in response

Phase 3: Docs ────────────────────────────────────────────────── Wave 3
  └─ T8: ADR-0001 implementation note

Phase 4: Frontend ────────────────────────────────────────────── Wave 4
  ├─ T9: Update dashboard types (guestUrl replaces linkToken)
  └─ T10: Update single/multi Dashboard + DashboardContent

Phase 5: Tests + verification ────────────────────────────────── Wave 5
  ├─ T11: SmartGuestLinkServiceTests (move/adjust provisioning token tests)
  └─ T12: Full build + test + typecheck
```

### Dependency matrix

| Task | Depends on | Blocks |
|------|-----------|--------|
| T1 | — | T2, T3 |
| T2 | T1 | T3, T4–T7 |
| T3 | T2 | T4–T7 |
| T4 | T3 | T11 |
| T5 | T3 | T11 |
| T6 | T3 | T11 |
| T7 | T3 | T9 |
| T8 | — | T12 (can run parallel to T4–T7) |
| T9 | T7 | T10 |
| T10 | T9 | T11 |
| T11 | T4–T7, T10 | T12 |
| T12 | T8, T11 | — |

---

## TODOs

- [ ] 1. Create `ISmartGuestLinkService` + DTO + exception

  **What to do:**
  - Create `backend/TummlyBackend/Interfaces/ISmartGuestLinkService.cs`
  - Create `backend/TummlyBackend/DTOs/SmartGuestLink/GuestLinkLocationInfo.cs`
  - Create `backend/TummlyBackend/Exceptions/LinkTokenGenerationException.cs`

  **References:**
  - Interface sketch in this plan
  - `docs/adr/0001-smart-guest-link-uses-opaque-token.md`

  **Acceptance Criteria:**
  - [ ] Interface compiles with four methods
  - [ ] `GuestLinkLocationInfo` has restaurant + location name
  - [ ] `dotnet build --no-restore` succeeds

---

- [ ] 2. Implement `SmartGuestLinkService`

  **What to do:**
  - Create `backend/TummlyBackend/Services/SmartGuestLinkService.cs`
  - Move `GenerateLinkToken` algorithm from `GuestLoopProvisioningService` (same charset/length)
  - `GenerateTokenAsync`: retry up to 5 times checking `_context.RestaurantLocations.Any(l => l.LinkToken == token)`
  - `ResolveForGuestAsync`: trim token, `AsNoTracking`, `Include(Restaurant)`, return DTO or null
  - `ResolveLocationForWriteAsync`: trim token, tracked query, return entity or null
  - `BuildGuestUrl`: read `Frontend:BaseUrl` (copy validation from `QrController.GetFrontendBaseUrl`), return `{base}/scan/{token}`

  **Must NOT do:**
  - Do not re-tokenize existing rows
  - Do not add `Console.WriteLine`

  **Acceptance Criteria:**
  - [ ] All four methods implemented
  - [ ] `dotnet build --no-restore` succeeds

---

- [ ] 3. Register service in `Program.cs`

  **What to do:**
  - `builder.Services.AddScoped<ISmartGuestLinkService, SmartGuestLinkService>();`

  **Acceptance Criteria:**
  - [ ] Service registered scoped
  - [ ] `dotnet build --no-restore` succeeds

---

- [ ] 4. Refactor `GuestLoopProvisioningService` to inject `ISmartGuestLinkService`

  **What to do:**
  - Inject `ISmartGuestLinkService`
  - Replace `LinkToken = GenerateLinkToken()` with `LinkToken = await _smartGuestLink.GenerateTokenAsync()`
  - Delete private `GenerateLinkToken` method

  **Acceptance Criteria:**
  - [ ] No `GenerateLinkToken` in provisioning service
  - [ ] Provisioning tests still pass (may need mock or real service)

---

- [ ] 5. Refactor `ScanController` — resolve delegation only

  **What to do:**
  - Inject `ISmartGuestLinkService`
  - `GetLocationByToken`: call `ResolveForGuestAsync`, map to existing JSON shape, 404 on null
  - `SubmitFeedback`: call `ResolveLocationForWriteAsync`, 404 on null; leave validation/rate-limit/heuristic inline

  **Must NOT do:**
  - Do not extract `DetectContactType` or feedback submission (Candidate #9)

  **Acceptance Criteria:**
  - [ ] Zero `LinkToken ==` queries in `ScanController`
  - [ ] HTTP responses unchanged for valid/invalid tokens
  - [ ] `dotnet build --no-restore` succeeds

---

- [ ] 6. Refactor `QrController` — `BuildGuestUrl` delegation

  **What to do:**
  - Inject `ISmartGuestLinkService`
  - Replace `GetFrontendBaseUrl()` + string concat with `_smartGuestLink.BuildGuestUrl(loc.LinkToken)`
  - Delete private `GetFrontendBaseUrl` from `QrController` if no longer used

  **Acceptance Criteria:**
  - [ ] QR PNG encodes same URL shape as before (canonical production base)
  - [ ] No `/scan/` concat in `QrController`

---

- [ ] 7. Refactor `RestaurantController` — return `guestUrl`

  **What to do:**
  - Inject `ISmartGuestLinkService`
  - In locations projection: replace `l.LinkToken` with `guestUrl = _smartGuestLink.BuildGuestUrl(l.LinkToken)` (or build in select if service is instance-scoped in controller)

  **Acceptance Criteria:**
  - [ ] Response includes `guestUrl` per location
  - [ ] Response does NOT include `linkToken`

---

- [ ] 8. ADR-0001 implementation note

  **What to do:**
  - Append short section to `docs/adr/0001-smart-guest-link-uses-opaque-token.md`:
    - Migration backfill used `NEWID()` hex (32 chars)
    - Runtime generation uses crypto alphanumeric (32 chars)
    - Both satisfy opaque/non-enumerable; no re-tokenization to avoid breaking printed QRs

  **Must NOT do:**
  - Do not change ADR decision — note only

  **Acceptance Criteria:**
  - [ ] ADR-0001 documents the split

---

- [ ] 9. Update frontend dashboard types

  **What to do:**
  - `src/types/dashboard.ts`: replace `linkToken: string` with `guestUrl: string`
  - Update any API parsing that maps location fields

  **Acceptance Criteria:**
  - [ ] Types reflect `guestUrl`

---

- [ ] 10. Update dashboard components

  **What to do:**
  - `DashboardContent.tsx`: prop `guestUrl` instead of `linkToken`; use directly for “Open guest link” href
  - `single/Dashboard.tsx`, `multi/Dashboard.tsx`: pass `guestUrl` from location
  - Remove `` window.location.origin `/scan/` `` construction

  **Acceptance Criteria:**
  - [ ] No client-side Smart Guest Link URL building
  - [ ] `npm run test` passes

---

- [ ] 11. Write `SmartGuestLinkServiceTests`

  **What to do:**
  - Create `backend/TummlyBackend.Tests/Services/SmartGuestLinkServiceTests.cs`
  - Test `GenerateTokenAsync`: length 32, uniqueness retry, exception after cap
  - Test `ResolveForGuestAsync`: valid token, invalid token, trim
  - Test `ResolveLocationForWriteAsync`: returns tracked entity
  - Test `BuildGuestUrl`: shape `{base}/scan/{token}`, throws on missing config
  - Move token-length/uniqueness assertions from `GuestLoopProvisioningServiceTests` to here; keep provisioning tests for entity creation only

  **Acceptance Criteria:**
  - [ ] Tests use in-memory DB
  - [ ] `dotnet test --no-restore` all pass

---

- [ ] 12. Full verification

  **What to do:**
  - `dotnet build --no-restore`
  - `dotnet test --no-restore`
  - `npm run test`
  - `npm run typecheck`

  **Acceptance Criteria:**
  - [ ] Backend: 0 errors, all tests pass
  - [ ] Frontend: all tests pass; typecheck no new errors in touched files

---

## Success Criteria

### Verification commands

```bash
cd backend/TummlyBackend && dotnet build --no-restore
cd backend/TummlyBackend && dotnet test --no-restore
npm run test
npm run typecheck
```

### Final checklist

- [ ] `ISmartGuestLinkService` — four typed methods
- [ ] No scattered `LinkToken ==` in controllers (Scan)
- [ ] No scattered `/scan/` URL concat (Qr, DashboardContent)
- [ ] Operator API: `guestUrl` only
- [ ] Provisioning uses injected `GenerateTokenAsync`
- [ ] ADR-0001 implementation note present
- [ ] New service tests cover generate, resolve, build URL

---

## Decision log (for plan maintenance)

| Decision | Rationale |
|----------|-----------|
| Separate from `IProvisioningService` | Resolve + BuildUrl used by scan/QR/dashboard, not provisioning lifecycle |
| Two resolve methods | GET is read-only + include restaurant; POST needs tracked entity |
| Retry on collision | Unique index exists; explicit retry avoids opaque transaction failures |
| Keep hex backfill | Re-tokenizing breaks printed QRs |
| QR-only base URL dedup | Minimal scope; Admin/Auth remain Candidate #12 follow-up |
| No `linkToken` on operator API | Token is guest-facing; operators use `guestUrl` and QR by `locationId` |
| Scan resolve only | Candidate #9 is a separate plan |
