# Lane D phase 1 — Signup session and code security Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A signup session token is issued only after proof of the email (code or provider ticket), is short-lived and purpose-scoped, never travels in URLs, and every verification code is purpose-bound, hashed, CSPRNG-generated and protected by per-code, per-email and per-IP abuse limits.

**Architecture:** One shared `OtpChallengeService` issues and verifies codes for all four flows (signup, sign-in, trial, team invite), binding each code to email + purpose + subject + generation and storing only an HMAC hash. `SignupService` stops returning tokens from Start, rotates the token on each successful proof, enforces DB-authoritative per-email windows, and validates token age and status on every signup-progress endpoint. ASP.NET Core rate limiting adds per-IP limits on the signup endpoints, partitioned by the forwarded client IP.

**Tech Stack:** .NET 10 / EF Core 8 (xUnit 2, InMemory `TummlyWebApplicationFactory`, Testcontainers SQL Server lane), `Microsoft.AspNetCore.RateLimiting` (built in), `System.Security.Cryptography` (`RandomNumberGenerator`, `HMACSHA256`, `CryptographicOperations.FixedTimeEquals`), React + Vite + TS (vitest).

**Spec:** `/run/media/salman/D/Freelance/Tummly/.scratch/lane-d1-signup-security/PRD.md` (main checkout; `.scratch` is gitignored). Requirements R1–R17, acceptance matrix rows 1–18. Approved with corrections by Product Design, 27 Oct 2026.

## Global Constraints

- Worktree `.worktrees/lane-d1-signup-security`, branch `lane-d1-signup-security` from `origin/qa` 8979bb4d. The branch has NO upstream; never push to `qa`. PR to `qa` only when the user asks.
- EF migration: exactly ONE, and it is the LAST schema step (Task 8). Generate it with the tool: `cd backend/TummlyBackend && DOTNET_ROLL_FORWARD=Major dotnet ef migrations add LaneD1SignupSecurity`. Never hand-write `*.Designer.cs` or the snapshot. Run `backend/TummlyBackend/scripts/check-migration-designer-pairs.sh` and `check-migrations-list-vs-files.sh`. Follow `CODING_STANDARDS.md`. Before merge the migration is generated again after a rebase on `qa`.
- Task 1 records the base failing test lists. Watch each new test fail before writing code. Full suites at the end: **no new failure names** against those lists.
- Code limits (copy exactly): code length 6 digits, code life 10 minutes, 5 wrong attempts per code, resend cooldown 60 seconds, per-email window 24 hours with max 10 codes sent and max 15 wrong attempts, signup token life 24 hours, per-IP 10 requests / minute per signup endpoint, 30 requests / minute across all signup endpoints.
- Purposes (copy exactly): `signup`, `sign_in`, `trial`, `team_invite`. Signup subject = `PendingSignup.Id.ToString("N")`; sign-in subject = `User.Id.ToString()`; trial subject = `TrialRequest.Id.ToString()`; team invite subject = invite id `ToString()`.
- Hash: `CodeHash = lower-hex HMAC-SHA256(key, $"{purpose}|{subjectId}|{generation}|{code}")`. Config key `Otp:HashKey` (env `Otp__HashKey`). Testing / Development fallback key `tests-only-otp-hash-key`. Missing key outside Development/Testing → app start fails (same pattern as `GuestContactHasher`, `Program.cs:353`, `:1314-1316`).
- Token header: `X-Signup-Session`. A `sessionToken` query parameter is refused with 401.
- Error body shape for every signup endpoint: `{ success:false, code:"<code>", message:"<text>" }` (+ `retryAfterUtc` when locked). Exact codes and texts:
  - `code_sent` (200 Start / Resend): message `If this email can be used, we sent a 6-digit code.`
  - `otp_invalid` (400): `That code doesn't match. Check your email and try again.`
  - `otp_expired` (400): `That code has expired. Resend a new code to continue.`
  - `otp_attempts_exceeded` (400): `Too many wrong codes. Request a new code.`
  - `challenge_locked` (429): `Too many attempts for this email. Try again later.` + `retryAfterUtc`
  - `rate_limited` (429): `Too many attempts. Wait a moment and try again.` + `Retry-After` header
  - `resend_cooldown` (429): `Please wait before requesting another code.`
  - `code_delivery_failed` (503): `We could not send the code. Try again in a minute.`
  - `signup_session_expired` (401): `Your signup session has expired. Enter your email to continue.`
  - `signup_error` (500): `Something went wrong. Try again.`
- Verify-page text (copy exactly): `If this email can be used, we sent a 6-digit code. If you already have a Tummly account, sign in instead.` with links "Sign in" (`/login`) and "Forgot password" (existing reset route).
- Never log or return a token or a code. Log emails only through `LogContactMask`. Log IPs only as a hash (`IGuestContactHasher` is NOT reused; use `OtpCodeHasher.HashIp`).
- No Pilot start path, no plan or payment change (WS3b, D4). Email verification does not grant authority, Pilot or paid entitlement.
- Commit messages end with `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.
- Frontend style: no semicolons, double quotes; reuse `src/components/ui/`; no raw `<button>`; do not run `npx prettier`.
- Test commands: backend `cd backend && dotnet test TummlyBackend.Tests --filter "FullyQualifiedName~<Name>"`; SQL lane `cd backend && dotnet test TummlyBackend.Tests --filter "FullyQualifiedName~SqlServer.<Name>"` (needs Docker); frontend `npx vitest run <path>`; typecheck `npm run typecheck`; build `npm run build`.

## Review Focus

1. **Platform proxy hides the client IP.** Behind Azure Container Apps every request arrives from the ingress, and `ForwardedHeadersOptions` trusts only loopback (`Program.cs:249-253`). A limiter keyed on `RemoteIpAddress` would throttle ALL users together. Test: Task 6 `RateLimit_PartitionsByForwardedClientIp` (two different right-most `X-Forwarded-For` values each get their own 10).
2. **Same right code submitted twice at once** (double click, two tabs). Exactly one token; the second gets `otp_invalid`. Test: Task 2 `Verify_SameCodeTwice_SecondInvalid` and Task 8 SQL `ConcurrentVerify_OneSuccess`.
3. **Code with a leading zero** (`"012345"`). Generation pads to 6 digits; verify accepts it. Test: Task 2 `Issue_CodeIsSixDigitsWithLeadingZeros` and `Verify_LeadingZeroCode_Verified`.
4. **User mid-signup at deploy** (legacy token). Onboarding page must send the user to email entry with the expired text, not show a raw error. Test: Task 7 `onboarding_sessionExpired_redirectsToSignupWithMessage`.
5. **Email case and spaces differ between Start and Verify** (`" Owner@Example.com "`). Both normalise to the same challenge. Test: Task 3 `Verify_EmailCaseAndSpaces_Normalised`.

---

### Task 1: Baseline lists and data model fields

**Files:**
- Modify: `backend/TummlyBackend/Models/OtpVerification.cs`, `backend/TummlyBackend/Models/PendingSignup.cs`, `backend/TummlyBackend/Data/ApplicationDbContext.cs` (OtpVerification config + index)
- Create: `backend/TummlyBackend/Helpers/OtpPurposes.cs`
- Test: `backend/TummlyBackend.Tests/Services/PendingSignupModelTests.cs` (extend)

**Interfaces:**
- Produces: `OtpVerification.Purpose (string?)`, `.SubjectId (string?, max 64)`, `.Generation (int)`, `.CodeHash (string?, max 64)`, `.FailedAttempts (int)`; `PendingSignup.SessionTokenIssuedAtUtc (DateTime?)`, `.CodeGeneration (int)`, `.AbuseWindowStartUtc (DateTime?)`, `.CodesSentInWindow (int)`, `.FailedVerifiesInWindow (int)`; `static class OtpPurposes { const string Signup = "signup", SignIn = "sign_in", Trial = "trial", TeamInvite = "team_invite"; }`. Index `OtpVerifications(Email, Purpose, IsUsed)`.

- [ ] **Step 1:** Record base failures: full backend suite and full frontend suite on this base; save failing test names to `.superpowers/sdd/<plan-workspace>/baseline-backend.txt` and `baseline-frontend.txt`; record `npm run typecheck` error-line count.
- [ ] **Step 2: Failing test** `PendingSignupModelTests.NewSecurityFields_DefaultToZeroAndNull`: a new `PendingSignup` has `CodeGeneration == 0`, `CodesSentInWindow == 0`, `FailedVerifiesInWindow == 0`, `SessionTokenIssuedAtUtc == null`, `AbuseWindowStartUtc == null`; a new `OtpVerification` has `FailedAttempts == 0`, `Generation == 0`, `Purpose == null`.
- [ ] **Step 3:** Add the fields, `OtpPurposes`, and the DbContext config (max lengths 32 for `Purpose`, 64 for `SubjectId` and `CodeHash`, the index). No migration in this task.
- [ ] **Step 4:** Test passes; backend builds.
- [ ] **Step 5:** Commit `feat(signup-security): code and signup security fields`.

### Task 2: Shared code challenge service

**Files:**
- Create: `backend/TummlyBackend/Services/OtpCodeHasher.cs`, `backend/TummlyBackend/Interfaces/IOtpChallengeService.cs`, `backend/TummlyBackend/Services/OtpChallengeService.cs`
- Modify: `backend/TummlyBackend/Helpers/GenerateOtp.cs` (CSPRNG), `backend/TummlyBackend/Program.cs` (register hasher singleton + service scoped; resolve hasher at start next to `IGuestContactHasher`)
- Test: `backend/TummlyBackend.Tests/Services/OtpChallengeServiceTests.cs`

**Interfaces:**
- Consumes: Task 1 fields, `OtpPurposes`.
- Produces:
  ```csharp
  public interface IOtpCodeHasher { string HashCode(string purpose, string subjectId, int generation, string code); string HashIp(string ip); }
  public sealed class OtpCodeHasher : IOtpCodeHasher { public const string ConfigurationKey = "Otp:HashKey"; public const string TestingFallbackKey = "tests-only-otp-hash-key"; public static OtpCodeHasher FromConfiguration(IConfiguration c, IHostEnvironment e); }
  public sealed record OtpIssueRequest(string Email, string Purpose, string SubjectId, int Generation, int? UserId = null, string Channel = OtpVerification.ChannelEmail);
  public sealed record OtpIssueResult(int OtpVerificationId, string Code);
  public sealed record OtpVerifyRequest(string Email, string Purpose, string SubjectId, int Generation, string Code);
  public enum OtpVerifyOutcome { Verified, Invalid, Expired, AttemptsExceeded }
  public sealed record OtpVerifyResult(OtpVerifyOutcome Outcome, int? OtpVerificationId);
  public interface IOtpChallengeService {
      Task<OtpIssueResult> IssueAsync(OtpIssueRequest request, CancellationToken ct = default);
      Task<OtpVerifyResult> VerifyAsync(OtpVerifyRequest request, CancellationToken ct = default);
      Task InvalidateAsync(int otpVerificationId, CancellationToken ct = default);
  }
  public static class GenerateOtp { public static string CreateOtp(); } // RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6")
  ```
  `IssueAsync` normalises the email (`Trim().ToLowerInvariant()`), removes unused rows with the same email AND purpose (never other purposes), adds a row with `CodeHash`, `OtpCode = string.Empty`, `Purpose`, `SubjectId`, `Generation`, `ExpiresAt = now + 10 min`, saves, and returns the plain code in memory only. `VerifyAsync` picks the newest unused row for email + purpose + subject; generation mismatch → `Invalid`; expired → `Expired`; hash compare with `CryptographicOperations.FixedTimeEquals`; wrong → `FailedAttempts++`, at 5 set `IsUsed = true` and return `AttemptsExceeded`, else `Invalid`; right → mark used with a conditional update (`ExecuteUpdateAsync` where `IsUsed == false` when relational; plain set for InMemory); 0 rows updated → `Invalid`. Use `TimeProvider` for now.

- [ ] **Step 1: Failing tests** in `OtpChallengeServiceTests`: `Issue_CodeIsSixDigitsWithLeadingZeros` (regex `^\d{6}$` over 200 codes), `Issue_StoresHashNotCode` (row `OtpCode == ""`, `CodeHash` = expected HMAC), `Verify_RightCode_Verified`, `Verify_LeadingZeroCode_Verified` (seed code `"012345"`), `Verify_WrongPurpose_Invalid`, `Verify_WrongSubject_Invalid`, `Verify_OlderGeneration_Invalid`, `Verify_Expired_Expired` (advance fake time 11 min), `Verify_FiveWrong_AttemptsExceeded_ThenRightCodeInvalid`, `Verify_SameCodeTwice_SecondInvalid`, `Issue_SignInDoesNotRemoveSignupCode`, `Verify_NullPurposeLegacyRow_Invalid`, `Hasher_FromConfiguration_MissingKeyInProduction_Throws`.
- [ ] **Step 2:** Run; all fail (types missing).
- [ ] **Step 3:** Implement the hasher, service, CSPRNG `GenerateOtp`, DI registration and start-time resolution.
- [ ] **Step 4:** Tests pass.
- [ ] **Step 5:** Commit `feat(signup-security): purpose-bound hashed code challenges`.

### Task 3: Signup Start, Resend and Verify with abuse limits

**Files:**
- Create: `backend/TummlyBackend/Helpers/SignupAbuseLimits.cs`, `backend/TummlyBackend/Services/SignupFlowException.cs`, `backend/TummlyBackend/DTOs/Signup/SignupCodeSentResponse.cs`
- Modify: `backend/TummlyBackend/Services/SignupService.cs` (`StartAsync`, `ResendOtpAsync`, `VerifyOtpAsync`, `EnforceAndSendOtpAsync`, remove `ReplaceUnusedOtpsAsync`), `backend/TummlyBackend/Interfaces/ISignupService.cs`
- Test: `backend/TummlyBackend.Tests/Services/SignupServiceTests.cs` (rewrite Start/Verify/Resend tests; `:178` `StartAsync_ResumesVerified_SameSession_NoNewRow_NoOtp` is replaced), constructor updates in `SignupServiceSocialTests.cs`, `SignupSessionLoginTests.cs`, `SignupProvisionTests.cs`

**Interfaces:**
- Consumes: `IOtpChallengeService`, `OtpPurposes.Signup`, Task 1 fields.
- Produces:
  ```csharp
  public static class SignupAbuseLimits { TimeSpan Window = 24h; int MaxCodesPerWindow = 10; int MaxFailedVerifiesPerWindow = 15; TimeSpan ResendCooldown = 60s; TimeSpan TokenLifetime = 24h; }
  public sealed class SignupFlowException : Exception { public string Code { get; } public int StatusCode { get; } public DateTime? RetryAfterUtc { get; } public SignupFlowException(string code, string message, int statusCode = 400, DateTime? retryAfterUtc = null); }
  public sealed class SignupCodeSentResponse { public string Email { get; set; } = ""; public string Status { get; set; } = "code_sent"; }
  Task<SignupCodeSentResponse> StartAsync(StartSignupDto dto);          // ISignupService
  Task<SignupCodeSentResponse> ResendOtpAsync(string email);           // ISignupService (was Task)
  Task<SignupSessionResponse> VerifyOtpAsync(VerifySignupOtpDto dto);  // unchanged signature
  ```
  `SignupService` constructor gains `IOtpChallengeService otp`, `TimeProvider clock`, `ILogger<SignupService> logger` (append after existing parameters).
  Rules: Start / Resend never return a token. Existing User, Complete row, or (Resend only) no row for the email → return `code_sent`, send nothing. Abandoned restart keeps the window counters. Window: if `AbuseWindowStartUtc` is null or older than 24 h, reset both counters and set it to now. Sending when `CodesSentInWindow >= 10` or `FailedVerifiesInWindow >= 15` → `challenge_locked` (429, `retryAfterUtc = AbuseWindowStartUtc + 24h`). Cooldown 60 s → `resend_cooldown`. Each send: `CodeGeneration++`, `IssueAsync(subject = Id.ToString("N"), generation = CodeGeneration)`, `CodesSentInWindow++`; if the email send throws → `InvalidateAsync`, `CodesSentInWindow--`, `code_delivery_failed` (503). Verify: row by normalised email with status in {EmailPending, Verified, OnboardingComplete, AwaitingPayment, Provisioning}, else `otp_invalid`; locked → `challenge_locked`; outcome Invalid/Expired/AttemptsExceeded → `FailedVerifiesInWindow++` and the matching code; Verified → EmailPending becomes Verified (others keep status), new `SessionToken = Guid.NewGuid()`, `SessionTokenIssuedAtUtc = now`. Existing resend cap (5 → Abandoned) stays. Log events (Global Constraints) at each step.

- [ ] **Step 1: Failing tests:** `Start_NewEmail_SendsCode_NoToken`, `Start_MidFlowVerified_SendsNewCode_NoToken_StatusUnchanged`, `Start_ExistingUser_GenericResponse_NoEmail`, `Start_CompleteRow_GenericResponse_NoEmail`, `Verify_RightCode_IssuesNewToken_OldTokenChanged`, `Verify_MidFlowRow_KeepsStatus_NewToken`, `Verify_EmailCaseAndSpaces_Normalised`, `Verify_FiveWrong_AttemptsExceeded`, `Window_FifteenWrongAcrossResends_Locked_ResendDoesNotReset`, `Window_AbandonedRestart_DoesNotResetCounters`, `Window_EleventhCode_Locked_WithRetryAfter`, `Window_After24h_Resets`, `Send_EmailFails_CodeUnusable_NotCounted_503`, `Logs_NeverContainCodeOrToken` (capture logger output; assert neither the plain code nor the token string appears).
- [ ] **Step 2:** Run; fail.
- [ ] **Step 3:** Implement.
- [ ] **Step 4:** Tests pass; the rest of `SignupService*` tests pass after constructor updates.
- [ ] **Step 5:** Commit `fix(signup-security): no token from Start, proof-only tokens, layered code limits`.

### Task 4: Token validation, session endpoints and fixed errors

**Files:**
- Modify: `backend/TummlyBackend/Services/SignupService.cs` (`FindBySessionTokenAsync`, `GetBySessionAsync`, `SessionLoginAsync`/`ClaimSessionLoginAsync`, all remaining `throw new Exception(` → `SignupFlowException` with the same user text and code `signup_invalid_request`, status 400), `backend/TummlyBackend/Controllers/SignupController.cs`, `backend/TummlyBackend/DTOs/Signup/SignupResumeResponse.cs` (remove `SessionToken`)
- Test: `backend/TummlyBackend.Tests/Integration/SignupSessionSecurityEndpointsTests.cs` (new), `backend/TummlyBackend.Tests/Integration/SignupOnboardingCustomPlanTests.cs` (header instead of query)

**Interfaces:**
- Consumes: Task 3 types.
- Produces: controller reads `[FromHeader(Name = "X-Signup-Session")] Guid? signupSession` on `onboarding`, `session`, `retry-provision`, `provisioning-status`; `session-login` keeps the body DTO. Any request with a `sessionToken` query key → 401 `signup_session_expired`. One private mapper `IActionResult ToError(Exception ex)`: `SignupFlowException` → its status/code/message (+`retryAfterUtc`); `SignupConflictException` → 409 with its code; anything else → log, 500 `signup_error`. Every token refusal logs `signup.token_rejected` with a reason (`missing`, `query_string`, `legacy`, `expired`, `abandoned`, `complete`, `claimed`) and never the token.
  Token valid when: row found, `SessionTokenIssuedAtUtc != null`, `now - issued <= 24h`, status not Abandoned, and status not Complete — except `SessionLoginAsync`, which accepts Complete once; the claim also sets `SessionTokenIssuedAtUtc = null` (revoked). `GetBySessionAsync` on a Complete row returns `SignupResumeResponse { Status }` only (all other fields null/empty).

- [ ] **Step 1: Failing tests** (integration, header `X-Signup-Session`): `Start_ResponseHasNoToken` (body JSON has no `sessionToken`), `Session_LegacyTokenNullIssued_401`, `Session_Token25hOld_401`, `Session_AbandonedRow_401`, `Session_CompleteRow_StatusOnly`, `SessionLogin_ClaimRevokesToken_SecondCall401`, `Onboarding_QueryStringToken_401`, `Onboarding_HeaderToken_Works`, `SignupToken_AsBearerOnAuthMe_401`, `UnexpectedError_Returns500SignupError_NoExceptionText`, `Validation_PasswordsDoNotMatch_400_SameText`.
- [ ] **Step 2:** Run; fail.
- [ ] **Step 3:** Implement.
- [ ] **Step 4:** New and existing signup integration tests pass.
- [ ] **Step 5:** Commit `fix(signup-security): scoped expiring signup token in header, fixed errors`.

### Task 5: Provider signup and the other code flows

**Files:**
- Modify: `backend/TummlyBackend/Services/ExternalAuthService.cs` (`AcceptTermsAsync`: after ticket consume, `SessionToken = Guid.NewGuid()`, `SessionTokenIssuedAtUtc = now`), `backend/TummlyBackend/Services/AuthService.cs` (sign-in email codes via `IOtpChallengeService` purpose `sign_in`, subject user id, generation 0; SMS rows get `Purpose = sign_in`, Twilio verify unchanged; remove `new Random()` and the all-purpose delete at `:446-460`), `backend/TummlyBackend/Services/TrialService.cs` (purpose `trial`, subject trial request id), `backend/TummlyBackend/Services/TeamInvitationAcceptService.cs` (purpose `team_invite`, subject invite id; remove `Random.Shared`)
- Test: `backend/TummlyBackend.Tests/Services/ExternalAuthServiceTests.cs`, `AuthServiceOtpPurposeTests.cs` (new), `TrialServiceVerifyOtpTests.cs`, `backend/TummlyBackend.Tests/Integration/TeamInvitationEndpointsTests.cs`

**Interfaces:**
- Consumes: `IOtpChallengeService`, `OtpPurposes`. Constructors of `AuthService`, `TrialService`, `TeamInvitationAcceptService` gain `IOtpChallengeService` (append); update test constructions.
- Produces: no new public API. Each flow keeps its existing response texts for its own users.

- [ ] **Step 1: Failing tests:** `AcceptTerms_ResumedPending_RotatesToken_SetsIssuedAt`, `AcceptTerms_InvalidTicket_NoToken`, `SignIn_SignupCodeRefused`, `SignIn_RequestCode_DoesNotRemoveSignupCode`, `SignIn_FiveWrong_RequiresNewCode`, `Trial_SignupCodeRefused`, `TeamInvite_SignupCodeRefused`, `TeamInvite_CodeIsCsprngSixDigits`.
- [ ] **Step 2:** Run; fail.
- [ ] **Step 3:** Implement.
- [ ] **Step 4:** New tests and all existing Auth / Trial / Team / ExternalAuth tests pass.
- [ ] **Step 5:** Commit `fix(signup-security): purpose-bound codes for sign-in, trial and invitations`.

### Task 6: Per-IP rate limits on signup endpoints

**Files:**
- Create: `backend/TummlyBackend/Helpers/SignupClientIp.cs`, `backend/TummlyBackend/Configurations/SignupRateLimitSettings.cs`
- Modify: `backend/TummlyBackend/Program.cs` (`AddRateLimiter`, `UseRateLimiter` after `UseForwardedHeaders`), `backend/TummlyBackend/Controllers/SignupController.cs` (`[EnableRateLimiting("signup")]` on the six endpoints), `backend/TummlyBackend.Tests/Integration/TummlyWebApplicationFactory.cs` (`["SignupRateLimit:PerEndpointPerMinute"] = "1000"`, `["SignupRateLimit:CombinedPerMinute"] = "1000"`)
- Test: `backend/TummlyBackend.Tests/Integration/SignupRateLimitEndpointsTests.cs` (own factory via `WithWebHostBuilder` with the real 10 / 30), `backend/TummlyBackend.Tests/Helpers/SignupClientIpTests.cs`

**Interfaces:**
- Produces: `SignupRateLimitSettings { int PerEndpointPerMinute = 10; int CombinedPerMinute = 30; int TrustedProxyHops = 1; }` bound from `SignupRateLimit`; `static string SignupClientIp.Resolve(HttpContext ctx, int trustedProxyHops)` — the entry `trustedProxyHops` from the right of `X-Forwarded-For` when present (right-most = 1), else `Connection.RemoteIpAddress`, else `"unknown"`. Policy `"signup"`: a chained limiter = fixed window per (client IP + endpoint path) at `PerEndpointPerMinute`, and fixed window per client IP at `CombinedPerMinute`. Rejection: 429, `Retry-After`, body `rate_limited` (Global Constraints); log `signup.rate_limited` with `OtpCodeHasher.HashIp`.

- [ ] **Step 1: Failing tests:** `SignupClientIp_RightMostForwardedEntry`, `SignupClientIp_SpoofedLeftEntriesIgnored`, `SignupClientIp_NoHeader_UsesRemoteIp`, `RateLimit_EleventhStartInAMinute_429WithRetryAfter`, `RateLimit_CombinedAcrossEndpoints_31st_429`, `RateLimit_PartitionsByForwardedClientIp`, `RateLimit_NonSignupEndpoint_NotLimited`.
- [ ] **Step 2:** Run; fail.
- [ ] **Step 3:** Implement.
- [ ] **Step 4:** Tests pass; full signup integration tests still pass (factory override).
- [ ] **Step 5:** Commit `feat(signup-security): per-IP signup rate limits`.

### Task 7: Frontend — header token, verify flow, error texts

**Files:**
- Modify: `src/api/signupApi.ts` (send `X-Signup-Session` header; `startSignup` returns `{ email, status }`; `verifySignupOtp` returns the session with token; `asSignupResume` without `sessionToken`; carry `code` and `retryAfterUtc` on thrown errors as `saveSignupOnboarding` already does for `code`), `src/pages/auth/SignupPage.tsx` (do not save a token after Start; remove the `email already in use` branch), `src/pages/auth/SignupVerifyPage.tsx` (guard needs only `email`; save the token from verify, then navigate; show the verify-page text and links; map codes `otp_invalid`, `otp_expired`, `otp_attempts_exceeded`, `challenge_locked` (show local time from `retryAfterUtc`), `rate_limited`, `resend_cooldown`, `code_delivery_failed`; the locked and rate-limited messages also show links "Sign in" (`/login`), "Forgot password" and "Contact us" (`/help-center/contact`)), `src/lib/signupSession.ts` (helper `isSignupSessionExpiredError(error): boolean`), `src/pages/auth/SignupOnboardingPage.tsx`, `SignupProvisioningPage.tsx`, `SignupCompletePage.tsx`, `SignupChoosePlanRedirectPage.tsx` (on `signup_session_expired`: clear the token, navigate to `/signup` with the expired text)
- Test: `src/api/signupApi.test.ts` (new), `src/lib/signupSession.test.ts`, `src/pages/auth/SignupVerifyPage.test.tsx` (new), `src/pages/auth/SignupOnboardingPage.test.tsx` (new, focused)

**Interfaces:**
- Consumes: backend codes and texts (Global Constraints).
- Produces: `startSignup(payload): Promise<{ email: string; status: "code_sent" }>`; `getSignupSession(sessionToken)` etc. keep their signatures but send the header and no `params`.

- [ ] **Step 1: Failing tests:** `getSignupSession_sendsHeader_noQueryParam`, `startSignup_returnsNoToken`, `verifyPage_rendersGenericAccountText_withSignInAndForgotLinks`, `verifyPage_success_savesTokenFromVerify_navigatesOnboarding`, `verifyPage_challengeLocked_showsRetryTime`, `verifyPage_rateLimited_showsWaitText_andRecoveryLinks`, `onboarding_sessionExpired_redirectsToSignupWithMessage`.
- [ ] **Step 2:** Run; fail.
- [ ] **Step 3:** Implement.
- [ ] **Step 4:** Tests pass; `npm run typecheck` error count not above base; `npm run build` OK.
- [ ] **Step 5:** Commit `fix(signup-security): signup pages use header token and proof-only sessions`.

### Task 8: Migration, legacy-token invalidation and SQL lane tests

**Files:**
- Create (tool): `backend/TummlyBackend/Migrations/<timestamp>_LaneD1SignupSecurity.cs` + Designer; snapshot update (tool)
- Modify: the generated migration `Up` only to add the data step after the schema operations:
  ```sql
  UPDATE PendingSignups SET SessionToken = NEWID(), SessionTokenIssuedAtUtc = NULL WHERE Status <> 'Complete';
  UPDATE OtpVerifications SET IsUsed = 1 WHERE IsUsed = 0;
  ```
  (keep it in a `const string LegacyTokenInvalidationSql` like `WSEDeliveryStopLedger.BackfillSql`). `Down` drops the columns and index only.
- Test: `backend/TummlyBackend.Tests/SqlServer/LaneD1SignupSecuritySqlTests.cs`

**Interfaces:**
- Consumes: all model fields from Task 1.

- [ ] **Step 1: Failing SQL tests:** `Migration_LegacyVerifiedRow_TokenRotated_IssuedNull` (seed a Verified row before the migration step, run `LegacyTokenInvalidationSql`, old token no longer matches and `SessionTokenIssuedAtUtc IS NULL`), `Migration_CompleteRow_Unchanged`, `Migration_OpenCodes_MarkedUsed`, `ConcurrentVerify_OneSuccess` (two parallel `VerifyAsync` with the right code on SQL Server → exactly one `Verified`).
- [ ] **Step 2:** Generate the migration with the tool; add the data step; run the two migration scripts.
- [ ] **Step 3:** SQL tests pass; `dotnet ef migrations script` for this migration applies on the SQL lane.
- [ ] **Step 4:** Commit `feat(signup-security): migration with legacy token invalidation`.

### Task 9: Full verification and QA handoff

**Files:**
- Create: `.scratch/lane-d1-signup-security/qa-handoff.md` in the MAIN checkout (`/run/media/salman/D/Freelance/Tummly/.scratch/…`)

- [ ] **Step 1:** Full backend suite, full frontend suite, `npm run typecheck`, `npm run build`, both migration scripts. Compare failing names with Task 1 lists: no new names.
- [ ] **Step 2:** Write `qa-handoff.md`: (1) set the QA secret `Otp__HashKey` (random 32+ bytes, secretref, same method as `GuestSuppression__HashKey`) BEFORE the new revision starts; (2) users mid-signup at deploy must enter their email again; (3) QA script = acceptance matrix rows 1–18 with curl / browser steps and a synthetic email; (4) check on QA that two different client IPs get separate limits (row 10, Review Focus 1).
- [ ] **Step 3:** Commit `docs(signup-security): QA handoff` only if any repo file changed; the handoff file itself is in `.scratch` (gitignored).
