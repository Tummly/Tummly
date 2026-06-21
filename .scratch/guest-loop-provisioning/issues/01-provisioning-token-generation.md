# Backend: Provisioning generates per-location Smart Guest Link tokens

Status: done

## Parent

Build plan from `docs/pending-work.md` Section 4 (Phase 1 + Phase 2). Design decisions: ADR-0001 (opaque token), ADR-0002 (workspace per-restaurant), CONTEXT.md "Guest Loop provisioning" and "Smart Guest Link".

## What to build

When an operator completes Operator Setup via `POST /api/auth/setup-account`, each `RestaurantLocation` created gets a 32-character opaque `LinkToken` — a cryptographically random string stored on the location row. The entire provisioning flow (user, restaurant, locations with tokens, guest loop) is wrapped in a DB transaction; if any step fails, nothing commits.

The token is the Smart Guest Link key (per ADR-0001). The URL becomes `{Frontend:BaseUrl}/scan/{token}` — the numeric `RestaurantLocation.Id` is no longer used in guest links.

This slice also removes three dead/divergent setup endpoints that compete with the canonical `POST /api/auth/setup-account`:

- `AuthService.CompleteAccountSetupAsync` — stub that returns `true` without doing anything (`AuthService.cs:798-803`)
- `POST /api/trial/complete-setup` + `TrialService.CompleteAccountSetupAsync` — divergent provisioning path that ignores most DTO fields and creates one "Main Branch" location (`TrialController.cs:178`, `TrialService.cs:593-727`)
- `OnboardingController` — all three legacy endpoints (`single-setup`, `multi-setup`, `guest-loop`) (`OnboardingController.cs`)

`GuestLoopSetup` config columns (`Touchpoints`, `FeedbackTags`, `ThankYouMessage`, `Offer*`) are kept — nullable and inert, per the design decision that form configuration is out of scope.

## Acceptance criteria

- [x] `RestaurantLocation` has a `LinkToken` property (`string`, 32 chars)
- [x] EF Core migration adds `LinkToken` column as `nvarchar(32)` with a unique index
- [x] Migration handles existing rows: add column as nullable → backfill with generated tokens → set non-null
- [x] `AuthController.SetupAccount` generates a 32-char cryptographically random `LinkToken` for each `RestaurantLocation` in the `dto.Locations` loop
- [x] All writes in `SetupAccount` (user, restaurant, locations, guest loop) are wrapped in `BeginTransactionAsync` / `CommitTransactionAsync` with try/catch rollback
- [x] `POST /api/auth/complete-setup` endpoint and `AuthService.CompleteAccountSetupAsync` method are deleted
- [x] `POST /api/trial/complete-setup` endpoint and `TrialService.CompleteAccountSetupAsync` method are deleted
- [x] `OnboardingController` is deleted entirely (all three endpoints)
- [x] `ITrialService` and `IAuthService` interfaces are updated to remove deleted method signatures
- [x] Project builds with no compilation errors after deletions
- [x] Existing tests pass (adjust any tests that referenced deleted endpoints)
- [x] `GuestLoopSetup` config columns remain in the model and migration — do NOT remove them

## Blocked by

None — can start immediately.
