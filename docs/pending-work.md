# Pending Work

Findings from backend provisioning exploration, sign-in routing trace, and dashboard inspection.
Design decisions resolved via grill-with-docs session (2026-06-20). See `CONTEXT.md` for glossary and `docs/adr/` for architectural decisions.
**Last updated:** 2026-06-20 (all phases + deferred items resolved)

---

## 1. Completed — Sign-in routing fixes

| # | Issue | File | Status |
|---|-------|------|--------|
| F1 | `persistSelectedLocation` not imported → `ReferenceError` when `/me` returns a `selectedLocationId` | `src/lib/sessionRouting.ts:128` | Fixed |
| F2 | `completeUserSession` dropped `selectedLocationId` → fresh-OTP Multi operator always landed on bare `/multi-dashboard` instead of `/multi-dashboard?location={id}` | `src/pages/utils/authHelpers.ts:184` | Fixed |

Both fixes are dormant today — the backend never sends `selectedLocationId` (see ADR-0002: workspace selection is per-restaurant, dormant). The fixes are correct for the future contract but no current code path exercises them.

---

## 2. Completed — Code review fixes

| # | Issue | File | Status |
|---|-------|------|--------|
| F3 | `parseLocationUploadFile` threw unhandled errors on corrupt uploads | `src/lib/locationUpload/parseLocationUploadFile.ts` | Fixed |
| F4 | `xlsx@0.18.5` CVEs (prototype pollution + ReDoS) | `package.json` | Fixed — vendored `xlsx-0.20.3` |
| F5 | `xlsx` (~400KB) statically bundled in main chunk | `src/components/guest-loop/GuestLoopLocationsStep.tsx` | Fixed — `React.lazy` + gated mount |
| F6 | `set-state-in-effect` lint rule violation | `src/components/guest-loop/GuestLoopLocationsStep.tsx:48` | Fixed |
| F7 | `preserve-caught-error` lint — rethrow without `{ cause: error }` | `RegisterMultiPage.tsx`, `RegisterSinglePage.tsx`, `GuestLoopAccountSetupPrototype.tsx` | Fixed |
| F8 | Unused `LOCATION_UPLOAD_TEMPLATE_FILENAME` export | `src/lib/locationUpload/locationUploadConstants.ts` | Fixed |
| F9 | No upper bound on upload row count | `src/lib/locationUpload/parseLocationUploadFile.ts` | Fixed — `LOCATION_UPLOAD_MAX_ROWS = 100` |

**Note:** `vendor/xlsx-0.20.3.tgz` is untracked. Must `git add vendor/xlsx-0.20.3.tgz` before committing.

---

## 3. Design decisions (resolved)

### ADR-0001 — Smart Guest Link uses opaque per-location token
The Smart Guest Link is `https://tummly.com/scan/{token}`, not `/scan/{locationId}`. The token is a 32-character opaque random string stored on `RestaurantLocation.LinkToken`, generated during provisioning. Prevents link enumeration and survives location renames without invalidating printed QR codes.

### ADR-0002 — Workspace selection is per-restaurant, not per-location
Workspace selection is keyed by Restaurant, not by RestaurantLocation. Today every operator owns one restaurant, so workspace selection is dormant — the frontend UI and DTOs exist but the backend never sends `workspaceSetupRequired`. `SelectWorkspaceDto` currently holds `LocationId`; it will need to hold `RestaurantId` when multi-restaurant ownership is introduced. Location switching happens inside the dashboard via an in-dashboard location switcher, not at sign-in.

### ADR-0003 — Feedback model shape
Guest feedback is stored per-location (`Feedback.RestaurantLocationId`), attributed to the location whose token was used. The form captures three required fields: guest name, guest contact (single string, email or phone), and a comment. A `ContactType` column (`Email` | `Phone` | `Unknown`) records a heuristic classification at submission time.

### Additional decisions
- **QR generation timing:** Lazy — the link token is generated at provisioning; the QR PNG is rendered on-demand at first download from the dashboard.
- **Guest route architecture:** SPA route + JSON API. `GET /api/scan/{token}` returns location metadata; `POST /api/scan/{token}/feedback` accepts submissions.
- **Endpoint auth:** Operator QR endpoints get `[Authorize]` + ownership check. Guest endpoints are public (token is the secret) with per-token rate limiting on feedback submission.
- **Guest link domain:** Reuse `Frontend:BaseUrl` config — stop hardcoding `https://tummly.com` in `QrController.cs:54`.
- **Provisioning transaction:** Wrap `AuthController.SetupAccount` in `BeginTransactionAsync`/`CommitTransactionAsync`.
- **Multi-dashboard initial state:** First location's data (by `CreatedAt` ascending); in-dashboard location switcher for changing.
- **Dead code:** Delete `AuthService.CompleteAccountSetupAsync` stub, `TrialController.complete-setup`, `OnboardingController`. Keep `GuestLoopSetup` config columns (nullable, inert, future-use).

---

## 4. Build plan (sequenced)

### Phase 1 — Schema + Models (foundation)
Everything downstream depends on this.

| Task | Detail |
|------|--------|
| Add `LinkToken` to `RestaurantLocation` | `nvarchar(32)`, unique index, non-null for new rows. Generated at provisioning. |
| Add `Feedback` model | `Id`, `RestaurantLocationId` (FK), `GuestName` (required, max 150), `GuestContact` (required, max 100), `ContactType` (enum: `Email`, `Phone`, `Unknown`), `Comment` (required, max 1000), `CreatedAt`. |
| Add `DbSet<Feedback>` to `ApplicationDbContext` | Configure FK relationship to `RestaurantLocation`. |
| EF Core migration | Add `LinkToken` column (nullable → backfill existing rows → set non-null) + `Feedback` table. |

### Phase 2 — Provisioning fixes (depends on Phase 1)
| Task | Detail |
|------|--------|
| Transaction-wrap `AuthController.SetupAccount` | `BeginTransactionAsync` / `CommitTransactionAsync` with try/catch rollback. All writes (user, restaurant, locations, guest loop) inside one transaction. |
| Generate `LinkToken` per location | In the `dto.Locations` loop, generate a 32-char cryptographically random string per `RestaurantLocation` before `SaveChangesAsync`. |
| Delete dead endpoints | `AuthService.CompleteAccountSetupAsync` (stub), `TrialController.complete-setup` + `TrialService.CompleteAccountSetupAsync`, `OnboardingController` (all three endpoints). |

### Phase 3 — Guest-facing backend (depends on Phase 1)
| Task | Detail |
|------|--------|
| `ScanController` — `GET /api/scan/{token}` | Look up `RestaurantLocation` by `LinkToken`. Return `{ restaurantName, locationName }`. 404 if token not found. No auth. |
| `ScanController` — `POST /api/scan/{token}/feedback` | Resolve token → location. Validate: `GuestName` non-empty, `GuestContact` non-empty (max 100), `Comment` non-empty (max 1000). Detect `ContactType` heuristically (`@` → Email, digits-only → Phone, else Unknown). Create `Feedback` row. Per-token rate limit (max 10 submissions/hour). No auth. |

### Phase 4 — Operator QR fixes (depends on Phase 1)
| Task | Detail |
|------|--------|
| Add `[Authorize]` to `QrController` | Both `GET /api/qr/info` and `GET /api/qr/download`. Add ownership check: authenticated user's `OwnedRestaurants` must contain the location's `RestaurantId`. |
| Use `Frontend:BaseUrl` in QR URL | `QrController.cs:54`: replace `$"https://tummly.com/scan/{locationId}"` with `$"{frontendBaseUrl}/scan/{location.LinkToken}"`. Read `Frontend:BaseUrl` from `IConfiguration`. |
| Fix `WorkspaceController.cs:34` | Replace hardcoded `http://localhost:5204/api/qr/download?locationId={id}` with config-based URL. |
| Sanitize QR filename | `QR_{location.LocationName}.png` → `UrlEncode` or sanitize the location name in the `Content-Disposition` header. |

### Phase 5 — Operator dashboard API (depends on Phase 1)
| Task | Detail |
|------|--------|
| `GET /api/restaurant/locations` | Authenticated. Returns the operator's restaurant's locations: `[{ id, locationName, address, linkToken, ... }]`. Includes `linkToken` so the dashboard can construct the Smart Guest Link preview URL. |
| `GET /api/feedback?locationId={id}` | Authenticated + ownership check. Returns `{ total: N, recent: [{ id, guestName, guestContact, contactType, comment, createdAt }, ...] }`. Recent = top 5 by `CreatedAt` desc. |

### Phase 6 — Frontend: guest feedback form (depends on Phase 3)
| Task | Detail |
|------|--------|
| `/scan/{token}` route | New route in `AppRoutes.tsx`. Public (no auth guard). |
| Feedback form component | On mount: call `GET /api/scan/{token}` → get `{ restaurantName, locationName }`. Render form with location name displayed. Three inputs: name, contact (email or phone), message. Submit to `POST /api/scan/{token}/feedback`. Success state after submission. |
| Loading + error states | Skeleton while fetching metadata. 404 page if token invalid. Error message if submission fails. |

### Phase 7 — Frontend: operator dashboard (depends on Phase 5 + Phase 4)
| Task | Detail |
|------|--------|
| Multi-dashboard location switcher | Fetch `GET /api/restaurant/locations` on mount. Dropdown populated with locations. Default to first location (by `CreatedAt` ascending). Switching updates the selected location's data. |
| Per-location: QR download | Button calling `GET /api/qr/download?locationId={id}` (authenticated). Downloads `QR_{LocationName}.png`. |
| Per-location: Smart Guest Link preview | Construct `https://tummly.com/scan/{linkToken}` from the location list response. "Open" button opens in new tab. |
| Per-location: feedback stats + recent | Call `GET /api/feedback?locationId={id}` on location select. Display total count + recent feedback list. |
| Single-dashboard | Same as multi-dashboard but no location switcher (single location). Fetch the one location's data directly. |

---

## 5. Deferred / open items — resolved

All items verified against codebase and resolved via grill-with-docs session (2026-06-20). No ADRs needed — none meet all three criteria (hard to reverse, surprising, real trade-off).

| # | Item | Resolution |
|---|------|------------|
| B4 | No location count enforcement — a "Single" operator could submit 5 locations | **Add backend guard.** If `AccountType == "Single"` and `dto.Locations.Count > 1`, return BadRequest. Multi operators may submit 1+ locations (add more later). |
| B5 | `IncludeInRollout` is write-only — stored but never read | **Stop writing it.** Remove from `SetupAccountDto`, stop mapping in `AuthController`, remove frontend field. DB column stays (migration to drop is out of scope). |
| B6 | `RolloutApproach` ignored — DTO field exists but unused | **Remove from both DTOs** (`SetupAccountDto.cs`, `CompleteSetupDto.cs`). No model column, no mapping — pure dead code. |
| B13 | `SkiaSharp` dependency unused in `.csproj` | **Remove package reference.** Zero usage confirmed; `PngByteQRCode` doesn't require it. |
| B21 | No `ErrorBoundary` on setup routes | **Add ErrorBoundary, wrap full-viewport routes.** Setup, login, scan, forgot/reset password — routes without site chrome where white screen is catastrophic. Show friendly error + retry. |
| B22 | No redirect-back-to-intended-URL | **Skip.** Protected surface is just 3 dashboards; post-login routing already lands on the correct one by accountType. Revisit if app grows more protected routes. |
| B23 | `/me` failure shows sign-in form despite valid token | **Fall back to stored accountType.** If `/me` fails but token + accountType exist in auth store, redirect to appropriate dashboard. Better UX on transient network failures. |
| B24 | `getPostVerifyDashboardPath` deprecated but still exported/tested | **Delete function + tests.** No production callers; tests only test the deprecated wrapper, not the underlying `getPostLoginDestination`. |
| B25 | `PublicOnlyRoute` doesn't block signed-in USERs | **Block all signed-in users.** Change condition from `token && role === "ADMIN"` to just `token`. Operator preview of guest feedback form unaffected — `/scan/:token` is a standalone public route outside `PublicOnlyRoute`. |
| B11 | QR filename not sanitized | **Resolved.** Phase 4: `Uri.EscapeDataString` + character stripping. |

---

## 6. Reframed items (were gaps, now design decisions)

| # | Original finding | Resolution |
|---|-----------------|------------|
| B1 | Offer/feedback data silently discarded by `AuthController.SetupAccount` | **By design.** CONTEXT.md: during provisioning only boolean defaults are set. Form config is out of scope. The real gap (no configuration surface) is a future feature, not a provisioning defect. |
| B3 | No `workspaceSetupRequired` / `selectedLocationId` in auth responses | **Future-contract, not current-contract.** ADR-0002: workspace selection is per-restaurant and dormant. Backend doesn't send these fields today. `SelectWorkspaceDto` will need `RestaurantId` (not `LocationId`) when multi-restaurant is introduced. |
| B7 | No slug/per-location token — links use sequential numeric ID | **Resolved.** ADR-0001: opaque 32-char `LinkToken` on `RestaurantLocation`. |
| B8 | Domain hardcoded in `QrController.cs:54` | **Resolved.** Phase 4: use `Frontend:BaseUrl` config. |
| B9 | QR endpoint unauthenticated | **Resolved.** Phase 4: `[Authorize]` + ownership check. |
| B10 | QR materials URL hardcoded to localhost | **Resolved.** Phase 4: use config-based URL. |
| B12 | QR not generated during provisioning | **Confirmed by design.** Token generated at provisioning; PNG rendered on-demand at first download. |
| B14 | No `/scan/{id}` endpoint | **Resolved.** Phase 3: `ScanController` with `GET /api/scan/{token}`. |
| B15 | No feedback submission endpoint | **Resolved.** Phase 3: `POST /api/scan/{token}/feedback`. |
| B16 | `GuestLoopSetup` is per-restaurant, not per-location | **Confirmed as design decision.** Form is standard for all locations; config is out of scope. |
| B17 | Feedback fields left NULL during provisioning | **By design.** Matches CONTEXT.md. |
| B18 | Dead stub `POST /api/auth/complete-setup` | **Resolved.** Phase 2: delete. |
| B19 | Divergent `POST /api/trial/complete-setup` | **Resolved.** Phase 2: delete. |
| B20 | Legacy `OnboardingController` superseded | **Resolved.** Phase 2: delete. |

---

## 7. Key file reference

### Frontend
| File | Purpose |
|------|---------|
| `src/components/dashboard/single/Dashboard.tsx` | Single dashboard stub (9 lines) — Phase 7 replaces |
| `src/components/dashboard/multi/Dashboard.tsx` | Multi dashboard mockup (131 lines) — Phase 7 replaces |
| `src/components/dashboard/admin/Dashboard.tsx` | Admin dashboard — fully built (395 lines) |
| `src/pages/auth/LoginPage.tsx` | Sign-in wizard with OTP + workspace selection (693 lines) |
| `src/pages/utils/authHelpers.ts` | Session persistence, destination logic, response parsers |
| `src/lib/sessionRouting.ts` | `/me` fetch, already-authenticated destination routing |
| `src/lib/workspaceSetupFlow.ts` | Workspace list + selection API — dormant (ADR-0002) |
| `src/stores/authStore.ts` | Zustand auth store (only store in the app) |
| `src/pages/routes/AppRoutes.tsx` | Route definitions + guards — Phase 6 adds `/scan/:token` |

### Backend
| File | Purpose |
|------|---------|
| `backend/.../Controllers/AuthController.cs` | `POST setup-account` (primary provisioning) — Phase 2 modifies |
| `backend/.../Controllers/QrController.cs` | QR PNG download + info — Phase 4 modifies |
| `backend/.../Controllers/WorkspaceController.cs` | Dashboard summary (hardcoded QR URL) — Phase 4 fixes |
| `backend/.../Controllers/OnboardingController.cs` | Legacy setup endpoints — Phase 2 deletes |
| `backend/.../Controllers/TrialController.cs` | Divergent complete-setup — Phase 2 deletes |
| `backend/.../Services/AuthService.cs` | `VerifyOtpAsync`, `UniversalLoginAsync`, dead stub — Phase 2 deletes stub |
| `backend/.../Services/TrialService.cs` | Divergent `CompleteAccountSetupAsync` — Phase 2 deletes |
| `backend/.../Models/RestaurantLocation.cs` | Location model — Phase 1 adds `LinkToken` |
| `backend/.../Models/GuestLoopSetup.cs` | Per-restaurant config (columns kept, unused) |
| `backend/.../Data/ApplicationDbContext.cs` | EF Core DbContext — Phase 1 adds `DbSet<Feedback>` |
| `backend/.../DTOs/Auth/SetupAccountDto.cs` | Provisioning DTO (field naming mismatch with GuestLoopSetup — by design, config out of scope) |
| `backend/.../DTOs/Auth/SelectWorkspaceDto.cs` | Holds `LocationId` — future: change to `RestaurantId` (ADR-0002) |

### New files to create
| File | Purpose |
|------|---------|
| `backend/.../Controllers/ScanController.cs` | Guest-facing: `GET /api/scan/{token}`, `POST /api/scan/{token}/feedback` |
| `backend/.../Models/Feedback.cs` | Feedback model (Phase 1) |
| `src/pages/scan/ScanPage.tsx` (or similar) | Guest feedback form route (Phase 6) |
