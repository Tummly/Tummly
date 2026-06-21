# Plan: Extract IProvisioningService + absorb invite-token duplication

## TL;DR

> Guest Loop provisioning is 337 inline lines in `AuthController.SetupAccount` that bypasses the `IAuthService` seam. Invite-token validation is duplicated across 3 call sites with 2 backend endpoints. This plan extracts both into a new `IProvisioningService` module, gives the concept a test surface, and collapses the frontend from 2 round-trips to 1.

**Quick overview:**
- **Core objective:** Create a deep `IProvisioningService` module owning Guest Loop provisioning + invite-token validation
- **Key deliverables:** New service, controller delegation, frontend endpoint consolidation, dead code cleanup
- **Estimated effort:** Medium — backend extraction + frontend consolidation + test coverage
- **Parallel execution:** NO — sequential backend-first, then frontend
- **Critical path:** Service → Controller → Frontend → Tests

---

## Context

### Original problem statement
`POST /api/auth/setup-account` is 337 lines of inline provisioning logic in `AuthController` — 6 validation checks, trial-request lookup, 4 guard checks, entity creation loop, token generation, and transaction wrapping. It bypasses `IAuthService` entirely (DbContext injected directly into the controller). The concept "Guest Loop provisioning" (CONTEXT.md) has no named module, no seam, no test surface.

Additionally, invite-token validation appears 3 times: in `SetupAccount` (192–308), in `ValidateInvite` (467–566), and in `TrialService.ValidateSetupTokenAsync` (451–585). The frontend calls 2 endpoints for the same token (`/validate-invite` + `/validate-setup-token`).

### Interview summary
**Key discussions:**
- Scope: Provisioning + invite-token in one module (absorbs candidate #3 completely)
- Seam placement: New `IProvisioningService` (separate from `IAuthService`)
- ValidateInviteToken result: Rich result carrying AccountType, Email, RestaurantName, GroupName, TrialRequestId, ExpiresAt
- Token generation: Absorbed into provisioning module (does not become a separate Smart Guest Link token module)
- Error handling: Typed exceptions (`InviteTokenNotFoundException`, `InviteTokenExpiredException`, `AccountAlreadyCreatedException`)
- Deletion scope: Delete dead code as part of this change (`TrialController.ValidateSetupToken`, `TrialService.ValidateSetupTokenAsync`, `useSetupTokenValidation.ts`)

**Research findings:**
- `IAuthService` has `AdminLoginAsync`, `UserLoginAsync`, `UniversalLoginAsync`, OTP, password — no provisioning
- `TrialService.ValidateSetupTokenAsync` returns `Task<object>` (anonymous type) with 8 `Console.WriteLine` debug statements
- Frontend `useSetupTokenValidation.ts` is used by both Register pages; `SetupAccountPage` calls `/validate-invite` separately
- `isAccountAlreadyProvisionedMessage` check in frontend uses string matching — changes to HTTP 409

---

## Work Objectives

### Core objective
Create a deep `IProvisioningService` module owning Guest Loop provisioning + invite-token validation, with a typed interface, typed exceptions, and a test surface. Controller becomes a thin delegate. Frontend consolidates from 2 API calls to 1.

### Concrete deliverables
- `IProvisioningService` interface with `ValidateInviteTokenAsync` + `ProvisionAsync`
- `GuestLoopProvisioningService` implementation
- `InviteTokenResult` DTO
- 4 exception types
- Controller delegation
- Frontend endpoint consolidation
- Dead code deletion
- Test coverage

### Definition of done
- [ ] `AuthController.SetupAccount` is ≤15 lines (delegate only)
- [ ] `IProvisioningService` has two methods with typed signatures
- [ ] `TrialController.ValidateSetupToken` is deleted
- [ ] `TrialService.ValidateSetupTokenAsync` is deleted
- [ ] `useSetupTokenValidation.ts` is deleted
- [ ] Frontend calls `GET /validate-invite?token=...` (one round-trip)
- [ ] All 137 existing tests pass
- [ ] New tests cover `IProvisioningService` interface

### Must have
- Typed exceptions (not string matching)
- Rich `InviteTokenResult` (not minimal)
- Transaction wrapping entity creation
- `GenerateLinkToken` absorbed into provisioning module

### Must NOT have (guardrails)
- Do NOT change `IAuthService` interface
- Do NOT change the database schema
- Do NOT change the frontend guest-feedback flow
- Do NOT delete `TrialController` entirely if it has other actions (verify first)
- Do NOT delete `TrialService` entirely if it has other methods (verify first)
- Do NOT introduce repository abstraction (in-process, direct DbContext is fine)

---

## Verification Strategy

> ZERO HUMAN INTERVENTION — all verification is agent-executed.

### Test decision
- **Infrastructure exists:** YES (backend has `dotnet test`, frontend has Vitest)
- **Automated tests:** YES — after implementation
- **Agent-executed QA:** YES — `dotnet build`, `dotnet test`, `npm run test`

### QA policy
Every task MUST include agent-executed QA scenarios.
Evidence saved to `.sisyphus/evidence/task-{N}-{scenario-slug}.{ext}`.

- **Backend:** `dotnet build --no-restore`, `dotnet test --no-restore`
- **Frontend:** `npm run test`, `npx tsc --noEmit`

---

## Execution Strategy

### Sequential execution (recommended for extraction)

```
Phase 1: Research (verify dead code) ──────────────────────────── Wave 1
  └─ T1: Verify TrialController/TrialService have no other consumers

Phase 2: Backend extraction ────────────────────────────────────── Wave 2 (parallel)
  ├─ T2: Create IProvisioningService + DTOs + exceptions
  ├─ T3: Create GuestLoopProvisioningService implementation
  ├─ T4: Refactor AuthController to delegate to IProvisioningService
  └─ T5: Delete dead code (TrialController, TrialService, ITrialService)

Phase 3: Frontend consolidation ────────────────────────────────── Wave 3 (after backend)
  ├─ T6: Update trialApi.ts — add validateInviteToken, remove verifySetupToken
  ├─ T7: Update SetupAccountPage.tsx — use GET endpoint, pass prefill as props
  ├─ T8: Update RegisterSinglePage.tsx — receive props, 409 handling
  ├─ T9: Update RegisterMultiPage.tsx — receive props, 409 handling
  └─ T10: Delete useSetupTokenValidation.ts

Phase 4: Tests + verification ──────────────────────────────────── Wave 4 (after all)
  ├─ T11: Write GuestLoopProvisioningService tests
  └─ T12: Full build + test + typecheck

Phase 5: Commit ────────────────────────────────────────────────── Final
  └─ T13: Git commit
```

### Dependency matrix

| Task | Depends on | Blocks |
|------|-----------|--------|
| T1 | — | T2–T5 |
| T2 | T1 | T3, T4, T5 |
| T3 | T2 | T4 |
| T4 | T3 | T6–T10 |
| T5 | T4 | T6–T10 |
| T6 | T4 | T7 |
| T7 | T6 | T8, T9 |
| T8 | T7 | T10 |
| T9 | T7 | T10 |
| T10 | T8, T9 | T11 |
| T11 | T10 | T12 |
| T12 | T11 | T13 |
| T13 | T12 | — |

---

## TODOs

> Implementation + Test + QA — each task includes ALL three.

- [ ] 1. Verify dead code scope

  **What to do:**
  - Read `TrialController.cs` — list all actions besides `ValidateSetupToken`
  - Read `TrialService.cs` — list all methods besides `ValidateSetupTokenAsync`
  - Read `ITrialService.cs` — list all interface methods
  - Check if any other controller/service calls `ValidateSetupTokenAsync`
  - Check if any other frontend code imports `useSetupTokenValidation`

  **Must NOT do:**
  - Do not delete anything yet — just verify

  **Recommended Agent Profile:**
  - **Skills:** [`dev-browser`, `file-editor`]
  - **Agent:** `dev-browser` — grep callers, check imports

  **Parallelization:**
  - **Can Run In Parallel:** NO
  - **Parallel Group:** Sequential (first)
  - **Blocks:** T2–T5
  - **Blocked By:** None

  **References:**
  - `backend/TummlyBackend/Controllers/TrialController.cs` — list all actions
  - `backend/TummlyBackend/Services/TrialService.cs` — list all methods
  - `backend/TummlyBackend/Interfaces/ITrialService.cs` — list all interface methods
  - `src/hooks/useSetupTokenValidation.ts` — check all imports

  **Acceptance Criteria:**
  - [ ] All actions/methods listed
  - [ ] All callers identified
  - [ ] Dead code scope confirmed

  **QA Scenarios:**

  ```
  Scenario: Verify TrialController actions
    Tool: Bash (rg)
    Steps:
      1. rg "TrialController" backend/TummlyBackend/Controllers/ --count
      2. rg "ValidateSetupToken" backend/ --count
    Expected Result: Only TrialController.cs and TrialService.cs have references
    Evidence: .sisyphus/evidence/task-1-trial-references.txt

  Scenario: Verify useSetupTokenValidation imports
    Tool: Bash (rg)
    Steps:
      1. rg "useSetupTokenValidation" src/ --count
    Expected Result: Only RegisterSinglePage.tsx and RegisterMultiPage.tsx import it
    Evidence: .sisyphus/evidence/task-1-hook-imports.txt
  ```

  **Commit:** NO

---

- [ ] 2. Create IProvisioningService interface + DTOs + exceptions

  **What to do:**
  - Create `backend/TummlyBackend/DTOs/Provisioning/InviteTokenResult.cs`
  - Create `backend/TummlyBackend/Exceptions/InviteTokenNotFoundException.cs`
  - Create `backend/TummlyBackend/Exceptions/InviteTokenNotApprovedException.cs`
  - Create `backend/TummlyBackend/Exceptions/InviteTokenExpiredException.cs`
  - Create `backend/TummlyBackend/Exceptions/AccountAlreadyCreatedException.cs`
  - Create `backend/TummlyBackend/Interfaces/IProvisioningService.cs`
  - Register service in `Program.cs` (scoped)

  **Must NOT do:**
  - Do not implement the service yet — just the interface
  - Do not change `IAuthService`
  - Do not change the database schema

  **Recommended Agent Profile:**
  - **Skills:** [`dotnet-build`]
  - **Agent:** `dev-browser` — verify naming conventions

  **Parallelization:**
  - **Can Run In Parallel:** NO
  - **Parallel Group:** Sequential
  - **Blocks:** T3
  - **Blocked By:** T1

  **References:**
  - `backend/TummlyBackend/Interfaces/IAuthService.cs` — naming convention
  - `backend/TummlyBackend/Controllers/AuthController.cs:122–459` — understand what `ValidateInviteTokenAsync` and `ProvisionAsync` need to return
  - `backend/TummlyBackend/Services/TrialService.cs:451–585` — understand the current `ValidateSetupTokenAsync` return shape
  - `backend/TummlyBackend/DTOs/Auth/SetupAccountDto.cs` — understand the provisioning payload

  **Acceptance Criteria:**
  - [ ] `IProvisioningService` has two methods
  - [ ] `InviteTokenResult` has 6 properties
  - [ ] All 4 exception types compile
  - [ ] `Program.cs` registers the service
  - [ ] `dotnet build --no-restore` succeeds

  **QA Scenarios:**

  ```
  Scenario: Backend compiles
    Tool: Bash
    Steps:
      1. cd backend/TummlyBackend && dotnet build --no-restore
    Expected Result: Build succeeded. 0 Error(s)
    Evidence: .sisyphus/evidence/task-2-build.txt

  Scenario: Interface has correct shape
    Tool: Bash (rg)
    Steps:
      1. rg "ValidateInviteTokenAsync" backend/TummlyBackend/Interfaces/IProvisioningService.cs
      2. rg "ProvisionAsync" backend/TummlyBackend/Interfaces/IProvisioningService.cs
    Expected Result: Both methods present with typed signatures
    Evidence: .sisyphus/evidence/task-2-interface.txt
  ```

  **Commit:** YES
  - Message: `feat(provisioning): add IProvisioningService interface, DTOs, and exceptions`
  - Files: `backend/TummlyBackend/Interfaces/IProvisioningService.cs`, `backend/TummlyBackend/DTOs/Provisioning/InviteTokenResult.cs`, `backend/TummlyBackend/Exceptions/*.cs`, `backend/TummlyBackend/Program.cs`

---

- [ ] 3. Implement GuestLoopProvisioningService

  **What to do:**
  - Create `backend/TummlyBackend/Services/GuestLoopProvisioningService.cs`
  - Implement `ValidateInviteTokenAsync`:
    1. Find `TrialRequest` by `ApprovalToken`
    2. Guard checks → throw typed exceptions
    3. Return `InviteTokenResult`
  - Implement `ProvisionAsync`:
    1. Find `TrialRequest` by `dto.Token`
    2. Guard checks → same exceptions
    3. Create `User` (email from trial request, password bcrypt)
    4. Create `Restaurant` + loop `RestaurantLocation` + `GuestLoopSetup` stub
    5. Generate 32-char token per location (internal `GenerateLinkToken`)
    6. Mark `IsAccountCreated = true`
    7. Wrap in transaction

  **Must NOT do:**
  - Do not change the entity creation logic — just move it
  - Do not change the token generation logic — just move it
  - Do not add `Console.WriteLine`
  - Do not use `ITrialService` — the module owns the validation

  **Recommended Agent Profile:**
  - **Skills:** [`dev-browser`, `dotnet-build`]
  - **Agent:** `dev-browser` — read AuthController, extract logic

  **Parallelization:**
  - **Can Run In Parallel:** NO
  - **Parallel Group:** Sequential
  - **Blocks:** T4
  - **Blocked By:** T2

  **References:**
  - `backend/TummlyBackend/Controllers/AuthController.cs:122–459` — extract SetupAccount logic
  - `backend/TummlyBackend/Controllers/AuthController.cs:799–818` — extract GenerateLinkToken
  - `backend/TummlyBackend/Services/TrialService.cs:451–585` — understand current validation logic (do NOT copy Console.WriteLine)
  - `backend/TummlyBackend/Data/ApplicationDbContext.cs` — DbContext usage patterns

  **Acceptance Criteria:**
  - [ ] `GuestLoopProvisioningService` compiles
  - [ ] `ValidateInviteTokenAsync` returns `InviteTokenResult`
  - [ ] `ProvisionAsync` creates all entities in a transaction
  - [ ] `GenerateLinkToken` is a private method inside the service
  - [ ] `dotnet build --no-restore` succeeds

  **QA Scenarios:**

  ```
  Scenario: Backend compiles
    Tool: Bash
    Steps:
      1. cd backend/TummlyBackend && dotnet build --no-restore
    Expected Result: Build succeeded. 0 Error(s)
    Evidence: .sisyphus/evidence/task-3-build.txt

  Scenario: No Console.WriteLine in new service
    Tool: Bash (rg)
    Steps:
      1. rg "Console.WriteLine" backend/TummlyBackend/Services/GuestLoopProvisioningService.cs
    Expected Result: 0 matches
    Evidence: .sisyphus/evidence/task-3-no-console.txt
  ```

  **Commit:** YES
  - Message: `feat(provisioning): implement GuestLoopProvisioningService`
  - Files: `backend/TummlyBackend/Services/GuestLoopProvisioningService.cs`

---

- [ ] 4. Refactor AuthController to delegate to IProvisioningService

  **What to do:**
  - Add `IProvisioningService` dependency to `AuthController`
  - Replace `SetupAccount` action body with delegation:
    ```csharp
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    await _provisioning.ProvisionAsync(userId, dto);
    return Ok(new { accountType = dto.AccountType });
    ```
  - Replace `ValidateInvite` POST with new GET endpoint:
    ```csharp
    [HttpGet("validate-invite")]
    public async Task<IActionResult> ValidateInvite([FromQuery] string token)
    {
        var result = await _provisioning.ValidateInviteTokenAsync(token);
        return Ok(result);
    }
    ```
  - Remove inline validation logic, guard checks, entity creation, GenerateLinkToken

  **Must NOT do:**
  - Do not change the `SetupAccountDto` shape
  - Do not change the HTTP status codes (keep 400 for bad requests)
  - Do not change the `SetupAccount` endpoint URL or method

  **Recommended Agent Profile:**
  - **Skills:** [`dotnet-build`]
  - **Agent:** `dev-browser` — verify controller compiles

  **Parallelization:**
  - **Can Run In Parallel:** NO
  - **Parallel Group:** Sequential
  - **Blocks:** T5, T6–T10
  - **Blocked By:** T3

  **References:**
  - `backend/TummlyBackend/Controllers/AuthController.cs:122–459` — the SetupAccount action to refactor
  - `backend/TummlyBackend/Controllers/AuthController.cs:467–566` — the ValidateInvite action to replace
  - `backend/TummlyBackend/Controllers/AuthController.cs:799–818` — GenerateLinkToken (will be deleted from controller)

  **Acceptance Criteria:**
  - [ ] `AuthController.SetupAccount` is ≤15 lines
  - [ ] `AuthController` has `IProvisioningService` dependency
  - [ ] `ValidateInvite` is a GET endpoint returning rich result
  - [ ] No inline provisioning logic remains in controller
  - [ ] `dotnet build --no-restore` succeeds

  **QA Scenarios:**

  ```
  Scenario: Controller is thin
    Tool: Bash (rg)
    Steps:
      1. rg "GenerateLinkToken" backend/TummlyBackend/Controllers/AuthController.cs
      2. rg "TrialRequest" backend/TummlyBackend/Controllers/AuthController.cs
    Expected Result: 0 matches — all provisioning logic removed
    Evidence: .sisyphus/evidence/task-4-controller-thin.txt

  Scenario: Backend compiles
    Tool: Bash
    Steps:
      1. cd backend/TummlyBackend && dotnet build --no-restore
    Expected Result: Build succeeded. 0 Error(s)
    Evidence: .sisyphus/evidence/task-4-build.txt
  ```

  **Commit:** YES
  - Message: `refactor(provisioning): delegate SetupAccount to IProvisioningService`
  - Files: `backend/TummlyBackend/Controllers/AuthController.cs`

---

- [ ] 5. Delete dead code

  **What to do:**
  - Delete `ValidateSetupToken` action from `TrialController.cs` (or whole controller if it has no other actions)
  - Delete `ValidateSetupTokenAsync` method from `TrialService.cs` (or whole service if it has no other methods)
  - Delete `ValidateSetupTokenAsync` method from `ITrialService.cs` (or whole interface)
  - Remove `Console.WriteLine` from `TrialService` if any methods remain
  - Remove DI registration for deleted methods/services if needed

  **Must NOT do:**
  - Do not delete any method that has other callers (verify in T1)

  **Recommended Agent Profile:**
  - **Skills:** [`dotnet-build`]
  - **Agent:** `dev-browser` — verify no remaining callers

  **Parallelization:**
  - **Can Run In Parallel:** NO
  - **Parallel Group:** Sequential
  - **Blocks:** T6–T10
  - **Blocked By:** T4

  **References:**
  - `backend/TummlyBackend/Controllers/TrialController.cs` — delete ValidateSetupToken
  - `backend/TummlyBackend/Services/TrialService.cs` — delete ValidateSetupTokenAsync
  - `backend/TummlyBackend/Interfaces/ITrialService.cs` — delete ValidateSetupTokenAsync
  - Task T1 evidence — verify no other callers

  **Acceptance Criteria:**
  - [ ] `TrialController.ValidateSetupToken` is gone
  - [ ] `TrialService.ValidateSetupTokenAsync` is gone
  - [ ] `ITrialService.ValidateSetupTokenAsync` is gone
  - [ ] No `Console.WriteLine` in remaining `TrialService` methods
  - [ ] `dotnet build --no-restore` succeeds

  **QA Scenarios:**

  ```
  Scenario: Dead code removed
    Tool: Bash (rg)
    Steps:
      1. rg "ValidateSetupToken" backend/ --count
      2. rg "Console.WriteLine" backend/TummlyBackend/Services/TrialService.cs
    Expected Result: 0 matches for ValidateSetupToken; Console.WriteLine only in remaining methods (if any)
    Evidence: .sisyphus/evidence/task-5-dead-code.txt

  Scenario: Backend compiles
    Tool: Bash
    Steps:
      1. cd backend/TummlyBackend && dotnet build --no-restore
    Expected Result: Build succeeded. 0 Error(s)
    Evidence: .sisyphus/evidence/task-5-build.txt
  ```

  **Commit:** YES
  - Message: `chore(provisioning): delete dead TrialController/TrialService validation code`
  - Files: `backend/TummlyBackend/Controllers/TrialController.cs`, `backend/TummlyBackend/Services/TrialService.cs`, `backend/TummlyBackend/Interfaces/ITrialService.cs`

---

- [ ] 6. Update trialApi.ts

  **What to do:**
  - Add `validateInviteToken(token: string)` function — calls `GET /api/auth/validate-invite?token=...`
  - Remove `verifySetupToken(token: string)` function (or rename it)
  - Update any imports in other files

  **Must NOT do:**
  - Do not change other API functions
  - Do not change the authApi module

  **Recommended Agent Profile:**
  - **Skills:** [`frontend`]
  - **Agent:** `dev-browser` — verify TypeScript compiles

  **Parallelization:**
  - **Can Run In Parallel:** NO
  - **Parallel Group:** Sequential
  - **Blocks:** T7
  - **Blocked By:** T4

  **References:**
  - `src/api/trialApi.ts` — current API functions
  - `src/pages/auth/SetupAccountPage.tsx:32` — current `/validate-invite` call
  - `src/hooks/useSetupTokenValidation.ts:48` — current `validateSetupToken` call

  **Acceptance Criteria:**
  - [ ] `validateInviteToken` function exists
  - [ ] `verifySetupToken` function is removed
  - [ ] `npx tsc --noEmit` passes

  **QA Scenarios:**

  ```
  Scenario: TypeScript compiles
    Tool: Bash
    Steps:
      1. npx tsc --noEmit
    Expected Result: No errors
    Evidence: .sisyphus/evidence/task-6-tsc.txt
  ```

  **Commit:** NO (batch with T7–T10)

---

- [ ] 7. Update SetupAccountPage.tsx

  **What to do:**
  - Replace `POST /api/auth/validate-invite` call with `GET /api/auth/validate-invite?token=...`
  - Store rich result in state (`InviteTokenResult`)
  - Pass prefill data as props to child pages (instead of child pages calling their own API)
  - Remove any `isAccountAlreadyProvisionedMessage` string matching

  **Must NOT do:**
  - Do not change the wizard-step logic (single vs multi routing)
  - Do not change the form submission logic

  **Recommended Agent Profile:**
  - **Skills:** [`frontend`]
  - **Agent:** `dev-browser` — verify React component renders

  **Parallelization:**
  - **Can Run In Parallel:** NO
  - **Parallel Group:** Sequential
  - **Blocks:** T8, T9
  - **Blocked By:** T6

  **References:**
  - `src/pages/auth/SetupAccountPage.tsx` — current implementation
  - `src/pages/auth/RegisterSinglePage.tsx:31–35` — SetupAccountResponse interface
  - `src/pages/auth/RegisterMultiPage.tsx` — similar interface

  **Acceptance Criteria:**
  - [ ] Uses `GET /validate-invite?token=...` instead of `POST /validate-invite`
  - [ ] Stores rich result in state
  - [ ] Passes prefill data as props
  - [ ] `npx tsc --noEmit` passes

  **QA Scenarios:**

  ```
  Scenario: TypeScript compiles
    Tool: Bash
    Steps:
      1. npx tsc --noEmit
    Expected Result: No errors
    Evidence: .sisyphus/evidence/task-7-tsc.txt
  ```

  **Commit:** NO (batch with T8–T10)

---

- [ ] 8. Update RegisterSinglePage.tsx

  **What to do:**
  - Receive prefill data as props (from parent `SetupAccountPage`)
  - Remove `useSetupTokenValidation` import and call
  - Change `isAccountAlreadyProvisionedMessage` check to `response.status === 409`
  - Update `SetupAccountResponse` interface (if needed)

  **Must NOT do:**
  - Do not change the form fields or validation
  - Do not change the provisioning flow

  **Recommended Agent Profile:**
  - **Skills:** [`frontend`]
  - **Agent:** `dev-browser` — verify React component renders

  **Parallelization:**
  - **Can Run In Parallel:** YES (with T9)
  - **Parallel Group:** T8 + T9
  - **Blocks:** T10
  - **Blocked By:** T7

  **References:**
  - `src/pages/auth/RegisterSinglePage.tsx` — current implementation
  - `src/hooks/useSetupTokenValidation.ts` — being deleted

  **Acceptance Criteria:**
  - [ ] No `useSetupTokenValidation` import
  - [ ] `response.status === 409` check
  - [ ] `npx tsc --noEmit` passes

  **QA Scenarios:**

  ```
  Scenario: TypeScript compiles
    Tool: Bash
    Steps:
      1. npx tsc --noEmit
    Expected Result: No errors
    Evidence: .sisyphus/evidence/task-8-tsc.txt
  ```

  **Commit:** NO (batch with T9–T10)

---

- [ ] 9. Update RegisterMultiPage.tsx

  **What to do:**
  - Same as T8 but for multi-location variant
  - Receive prefill data as props
  - Remove `useSetupTokenValidation` import and call
  - Change `isAccountAlreadyProvisionedMessage` check to `response.status === 409`

  **Must NOT do:**
  - Do not change the form fields or validation
  - Do not change the provisioning flow

  **Recommended Agent Profile:**
  - **Skills:** [`frontend`]
  - **Agent:** `dev-browser` — verify React component renders

  **Parallelization:**
  - **Can Run In Parallel:** YES (with T8)
  - **Parallel Group:** T8 + T9
  - **Blocks:** T10
  - **Blocked By:** T7

  **References:**
  - `src/pages/auth/RegisterMultiPage.tsx` — current implementation
  - `src/hooks/useSetupTokenValidation.ts` — being deleted

  **Acceptance Criteria:**
  - [ ] No `useSetupTokenValidation` import
  - [ ] `response.status === 409` check
  - [ ] `npx tsc --noEmit` passes

  **QA Scenarios:**

  ```
  Scenario: TypeScript compiles
    Tool: Bash
    Steps:
      1. npx tsc --noEmit
    Expected Result: No errors
    Evidence: .sisyphus/evidence/task-9-tsc.txt
  ```

  **Commit:** NO (batch with T8, T10)

---

- [ ] 10. Delete useSetupTokenValidation.ts

  **What to do:**
  - Delete `src/hooks/useSetupTokenValidation.ts`
  - Verify no other imports exist
  - Verify TypeScript compiles

  **Must NOT do:**
  - Do not delete any other hooks

  **Recommended Agent Profile:**
  - **Skills:** [`frontend`]
  - **Agent:** `dev-browser` — verify file deleted and no broken imports

  **Parallelization:**
  - **Can Run In Parallel:** NO
  - **Parallel Group:** Sequential
  - **Blocks:** T11
  - **Blocked By:** T8, T9

  **References:**
  - `src/hooks/useSetupTokenValidation.ts` — file to delete

  **Acceptance Criteria:**
  - [ ] File deleted
  - [ ] No broken imports
  - [ ] `npx tsc --noEmit` passes

  **QA Scenarios:**

  ```
  Scenario: File deleted, no broken imports
    Tool: Bash
    Steps:
      1. Test-Path "src/hooks/useSetupTokenValidation.ts"
      2. rg "useSetupTokenValidation" src/ --count
    Expected Result: False for Test-Path; 0 matches for rg
    Evidence: .sisyphus/evidence/task-10-deleted.txt

  Scenario: TypeScript compiles
    Tool: Bash
    Steps:
      1. npx tsc --noEmit
    Expected Result: No errors
    Evidence: .sisyphus/evidence/task-10-tsc.txt
  ```

  **Commit:** YES (batch with T6–T9)
  - Message: `refactor(frontend): consolidate invite-token validation to single endpoint`
  - Files: `src/api/trialApi.ts`, `src/pages/auth/SetupAccountPage.tsx`, `src/pages/auth/RegisterSinglePage.tsx`, `src/pages/auth/RegisterMultiPage.tsx`, `src/hooks/useSetupTokenValidation.ts` (deleted)

---

- [ ] 11. Write GuestLoopProvisioningService tests

  **What to do:**
  - Create `backend/TummlyBackend.Tests/Services/GuestLoopProvisioningServiceTests.cs` (or similar test project path)
  - Test `ValidateInviteTokenAsync`:
    - Valid token → returns `InviteTokenResult`
    - Null token → `InviteTokenNotFoundException`
    - Not approved → `InviteTokenNotApprovedException`
    - Expired → `InviteTokenExpiredException`
    - Account already created → `AccountAlreadyCreatedException`
  - Test `ProvisionAsync`:
    - Valid data → creates User, Restaurant, Locations, GuestLoopSetup
    - Invalid token → throws exception
    - Token generation → 32-char, unique
  - Use in-memory database

  **Must NOT do:**
  - Do not test through the controller (test the interface, not the HTTP layer)
  - Do not mock `ApplicationDbContext` (use in-memory provider)

  **Recommended Agent Profile:**
  - **Skills:** [`dotnet-test`]
  - **Agent:** `dev-browser` — verify tests pass

  **Parallelization:**
  - **Can Run In Parallel:** NO
  - **Parallel Group:** Sequential
  - **Blocks:** T12
  - **Blocked By:** T10

  **References:**
  - `backend/TummlyBackend/Services/GuestLoopProvisioningService.cs` — implementation to test
  - `backend/TummlyBackend/Interfaces/IProvisioningService.cs` — interface (test surface)
  - Existing test files in backend — follow naming conventions

  **Acceptance Criteria:**
  - [ ] Test file exists
  - [ ] All tests pass
  - [ ] Tests cover both methods
  - [ ] Tests use in-memory database

  **QA Scenarios:**

  ```
  Scenario: Tests pass
    Tool: Bash
    Steps:
      1. cd backend/TummlyBackend && dotnet test --no-restore
    Expected Result: All tests passed. 0 Failed.
    Evidence: .sisyphus/evidence/task-11-tests.txt
  ```

  **Commit:** YES
  - Message: `test(provisioning): add GuestLoopProvisioningService tests`
  - Files: `backend/TummlyBackend.Tests/Services/GuestLoopProvisioningServiceTests.cs`

---

- [ ] 12. Full build + test + typecheck

  **What to do:**
  - Run `dotnet build --no-restore` (backend)
  - Run `dotnet test --no-restore` (backend tests)
  - Run `npx tsc --noEmit` (frontend TypeScript)
  - Run `npm run test` (frontend tests)
  - Verify no regressions

  **Must NOT do:**
  - Do not skip any verification step

  **Recommended Agent Profile:**
  - **Skills:** [`dotnet-build`, `dotnet-test`, `frontend`]
  - **Agent:** `dev-browser` — run all verification commands

  **Parallelization:**
  - **Can Run In Parallel:** NO
  - **Parallel Group:** Sequential
  - **Blocks:** T13
  - **Blocked By:** T11

  **References:**
  - `backend/TummlyBackend/` — backend project
  - `src/` — frontend project

  **Acceptance Criteria:**
  - [ ] Backend builds: 0 errors
  - [ ] Backend tests: all pass
  - [ ] Frontend TypeScript: no errors
  - [ ] Frontend tests: all pass
  - [ ] No regressions

  **QA Scenarios:**

  ```
  Scenario: Full verification
    Tool: Bash
    Steps:
      1. cd backend/TummlyBackend && dotnet build --no-restore
      2. cd backend/TummlyBackend && dotnet test --no-restore
      3. npx tsc --noEmit
      4. npm run test
    Expected Result: All pass, 0 errors, 0 failures
    Evidence: .sisyphus/evidence/task-12-full-verification.txt
  ```

  **Commit:** NO

---

- [ ] 13. Git commit

  **What to do:**
  - Run `git add -A`
  - Run `git commit -m "feat(provisioning): extract IProvisioningService, absorb invite-token duplication"`
  - Verify commit succeeded

  **Must NOT do:**
  - Do not push unless explicitly asked
  - Do not amend the commit

  **Recommended Agent Profile:**
  - **Skills:** [`git`]
  - **Agent:** `dev-browser` — verify commit

  **Parallelization:**
  - **Can Run In Parallel:** NO
  - **Parallel Group:** Sequential (last)
  - **Blocks:** None
  - **Blocked By:** T12

  **References:**
  - Git status — verify all files staged
  - Previous commits — match message format

  **Acceptance Criteria:**
  - [ ] Commit message follows conventional format
  - [ ] All files committed
  - [ ] No uncommitted changes

  **QA Scenarios:**

  ```
  Scenario: Commit succeeded
    Tool: Bash
    Steps:
      1. git log -1 --oneline
      2. git status --short
    Expected Result: Latest commit has correct message; working tree clean
    Evidence: .sisyphus/evidence/task-13-commit.txt
  ```

  **Commit:** NO (this IS the commit)

---

## Final Verification Wave

> 4 agents — ALL must APPROVE. Evidence collected to `.sisyphus/evidence/final-{agent}-{scenario}.ext`.

| Agent | Focus | Scenarios |
|-------|-------|-----------|
| `proactive-engineer` | Backend compilation | Build 0 errors, new service compiles |
| `test-engineer` | Tests pass | Backend tests all pass, frontend tests all pass |
| `ux-perfectionist` | Frontend integration | TypeScript compiles, no broken imports, endpoint consolidated |
| `systematic-verifier` | Full E2E | Backend + frontend + all tests |

**Deploy gate:** ALL agents APPROVE. ANY agent REJECTS → auto-fix loop → re-run.

---

## Success Criteria

### Verification commands
```bash
cd backend/TummlyBackend && dotnet build --no-restore  # Expected: Build succeeded. 0 Error(s)
cd backend/TummlyBackend && dotnet test --no-restore    # Expected: All tests passed. 0 Failed.
npx tsc --noEmit                                         # Expected: No errors
npm run test                                            # Expected: All tests pass
```

### Final checklist
- [ ] `AuthController.SetupAccount` is ≤15 lines
- [ ] `IProvisioningService` has two typed methods
- [ ] `InviteTokenResult` has 6 properties
- [ ] 4 exception types exist
- [ ] `TrialController.ValidateSetupToken` deleted
- [ ] `TrialService.ValidateSetupTokenAsync` deleted
- [ ] `useSetupTokenValidation.ts` deleted
- [ ] Frontend calls `GET /validate-invite?token=...` (one round-trip)
- [ ] All existing tests pass
- [ ] New tests cover `IProvisioningService`
- [ ] No `Console.WriteLine` in new service
- [ ] No inline provisioning logic in controller
