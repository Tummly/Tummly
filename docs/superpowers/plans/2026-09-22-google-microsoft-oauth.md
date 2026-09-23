# Google and Microsoft OAuth Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Wire Continue with Google and Continue with Microsoft on Sign-in and Sign-up via backend Authorization Code OAuth, with Terms after provider return for new users and the same OTP / trusted-device rules as password Sign-in for returning operators.

**Architecture:** Browser hits `GET /api/auth/external/{provider}/start`, API redirects to Google or Microsoft, callback stores a one-time ticket and redirects to the SPA. Sign-in tickets exchange into the same universal-login-shaped payload (JWT or OTP challenge). Sign-up tickets require Terms accept, then create/resume `PendingSignup` at `Verified` with no password; provisioning writes `UserExternalLogin` and a nullable `PasswordHash`.

**Tech Stack:** ASP.NET Core 8, EF Core, xUnit, React, React Router, Vitest, existing AuthService OTP / trusted-device helpers, `Frontend__BaseUrl` for redirects.

**Spec:** [docs/superpowers/specs/2026-09-22-google-microsoft-oauth-design.md](../specs/2026-09-22-google-microsoft-oauth-design.md)

## Global Constraints

- Providers: **Google** and **Microsoft** only.
- No auto-link of social to an existing password `User`.
- Social Sign-up: no local password; `PasswordHash` null on User and PendingSignup.
- Sign-in OTP / trusted device: **same rules as password Sign-in**.
- Admin / Support emails: reject social (password-only).
- Trust only verified provider emails.
- SPA never holds OAuth client secrets.
- Prefer existing UI primitives (`Button`, `FormCheckboxLabel`, `SignupModalShell` / `AuthShell` patterns).
- Report to the human in ASD-STE100 Simplified Technical English.
- Commit only when the human asks to commit (skip Step “Commit” unless asked).
- Subagents: Cursor Auto / `inherit` only (no Opus / GPT model overrides).

---

## File map

| File | Role |
|---|---|
| `backend/TummlyBackend/Models/UserExternalLogin.cs` | Provider link rows (new) |
| `backend/TummlyBackend/Models/ExternalAuthTicket.cs` | OAuth state + one-time exchange tickets (new) |
| `backend/TummlyBackend/Models/PendingSignup.cs` | Add `AuthProvider`, `ProviderSubject` |
| `backend/TummlyBackend/Models/User.cs` | Make `PasswordHash` nullable |
| `backend/TummlyBackend/Data/ApplicationDbContext.cs` | DbSets + indexes |
| `backend/TummlyBackend/Migrations/*_AddExternalAuth.cs` | EF migration |
| `backend/TummlyBackend/Options/ExternalAuthOptions.cs` | Config binding (new) |
| `backend/TummlyBackend/Interfaces/IExternalOAuthProviderClient.cs` | Build auth URL + exchange code (new) |
| `backend/TummlyBackend/Services/ExternalOAuthProviderClient.cs` | Google + Microsoft HTTP (new) |
| `backend/TummlyBackend/Interfaces/IExternalAuthService.cs` | Start / callback / exchange / accept-terms (new) |
| `backend/TummlyBackend/Services/ExternalAuthService.cs` | Match rules + tickets (new) |
| `backend/TummlyBackend/DTOs/Auth/ExternalAuthDtos.cs` | Exchange + accept-terms DTOs (new) |
| `backend/TummlyBackend/Controllers/ExternalAuthController.cs` | Public OAuth endpoints (new) |
| `backend/TummlyBackend/Interfaces/IAuthService.cs` | Add `CompleteExternalOperatorSignInAsync` |
| `backend/TummlyBackend/Services/AuthService.cs` | OTP/trust path without password |
| `backend/TummlyBackend/Services/SignupService.cs` | Social onboarding without password |
| `backend/TummlyBackend/Services/GuestLoopProvisioningService.cs` | Allow null hash; write `UserExternalLogin` |
| `backend/TummlyBackend/Program.cs` | Options + DI |
| `backend/TummlyBackend/.env.example` | Document ExternalAuth keys |
| `backend/TummlyBackend.Tests/Services/ExternalAuthServiceTests.cs` | Match + Terms + exchange (new) |
| `backend/TummlyBackend.Tests/Services/SignupServiceSocialTests.cs` | Social onboarding (new) |
| `src/api/externalAuthApi.ts` | Exchange + accept-terms clients (new) |
| `src/lib/externalAuthStart.ts` | Build start URL from `VITE_API_BASE_URL` (new) |
| `src/components/auth/AuthSocialContinueButtons.tsx` | Wire start navigation |
| `src/pages/auth/LoginOAuthCompletePage.tsx` | Exchange → Login step machine (new) |
| `src/pages/auth/SignupOAuthTermsPage.tsx` | Terms gate (new) |
| `src/pages/auth/LoginPage.tsx` | Surface `oauthError` query |
| `src/pages/auth/SignupPage.tsx` | Surface `oauthError` query |
| `src/components/signup/SignupAccountStep.tsx` | Hide password for social |
| `src/schemas/signupOnboarding.ts` | Social schema without password |
| `src/pages/routes/AppRoutes.tsx` | New routes |
| `src/lib/externalAuthStart.test.ts` | Unit tests (new) |
| `src/lib/operatorGuests/...` | Do not touch |
| `docs/product/sign-in.md` / `self-service-pilot.md` | Status → Shipped when done |

---

### Task 1: Data model + migration

**Files:**
- Create: `backend/TummlyBackend/Models/UserExternalLogin.cs`
- Create: `backend/TummlyBackend/Models/ExternalAuthTicket.cs`
- Modify: `backend/TummlyBackend/Models/User.cs`
- Modify: `backend/TummlyBackend/Models/PendingSignup.cs`
- Modify: `backend/TummlyBackend/Data/ApplicationDbContext.cs`
- Create: EF migration `AddExternalAuth`

**Interfaces:**
- Consumes: existing EF patterns
- Produces: `UserExternalLogin`, `ExternalAuthTicket`, nullable `User.PasswordHash`, `PendingSignup.AuthProvider` / `ProviderSubject`

- [ ] **Step 1: Write failing model smoke test**

Create `backend/TummlyBackend.Tests/Services/ExternalAuthModelTests.cs`:

```csharp
[Fact]
public void User_PasswordHash_IsNullable()
{
    var prop = typeof(User).GetProperty(nameof(User.PasswordHash));
    Assert.True(Nullable.GetUnderlyingType(prop!.PropertyType) != null
        || prop.PropertyType == typeof(string));
    // After change: string? — assert PropertyType allows null via NullabilityInfoContext
    var ctx = new NullabilityInfoContext();
    Assert.Equal(NullabilityState.Nullable, ctx.Create(prop).WriteState);
}

[Fact]
public void PendingSignup_HasAuthProviderFields()
{
    Assert.NotNull(typeof(PendingSignup).GetProperty("AuthProvider"));
    Assert.NotNull(typeof(PendingSignup).GetProperty("ProviderSubject"));
}
```

- [ ] **Step 2: Run test — expect fail**

Run: `dotnet test backend/TummlyBackend.Tests/TummlyBackend.Tests.csproj --filter "FullyQualifiedName~ExternalAuthModelTests" -v n`

Expected: FAIL (property missing / non-nullable).

- [ ] **Step 3: Add models**

`UserExternalLogin.cs`:

```csharp
namespace TummlyBackend.Models
{
    public static class ExternalAuthProviders
    {
        public const string Google = "Google";
        public const string Microsoft = "Microsoft";
    }

    public class UserExternalLogin
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        [MaxLength(32)]
        public string Provider { get; set; } = "";
        [MaxLength(200)]
        public string ProviderSubject { get; set; } = "";
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
```

`ExternalAuthTicket.cs`:

```csharp
public static class ExternalAuthTicketPurposes
{
    public const string OAuthState = "OAuthState";
    public const string SignInExchange = "SignInExchange";
    public const string SignUpExchange = "SignUpExchange";
}

public class ExternalAuthTicket
{
    public Guid Id { get; set; }
    [MaxLength(128)]
    public string Token { get; set; } = ""; // high-entropy; store hashed if preferred — v1 store raw with short TTL
    [MaxLength(32)]
    public string Purpose { get; set; } = "";
    public string PayloadJson { get; set; } = "{}";
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? ConsumedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
```

Change `User.PasswordHash` to `string?` and remove `[Required]`.

Add to `PendingSignup`:

```csharp
[MaxLength(32)]
public string? AuthProvider { get; set; }

[MaxLength(200)]
public string? ProviderSubject { get; set; }
```

In `ApplicationDbContext.OnModelCreating`:

```csharp
modelBuilder.Entity<UserExternalLogin>(e =>
{
    e.HasIndex(x => new { x.Provider, x.ProviderSubject }).IsUnique();
    e.HasIndex(x => new { x.UserId, x.Provider }).IsUnique();
    e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
});
modelBuilder.Entity<ExternalAuthTicket>(e =>
{
    e.HasIndex(x => x.Token).IsUnique();
    e.HasIndex(x => x.ExpiresAtUtc);
});
```

Add `DbSet<UserExternalLogin>` and `DbSet<ExternalAuthTicket>`.

- [ ] **Step 4: Add migration**

Run: `dotnet ef migrations add AddExternalAuth --project backend/TummlyBackend`

Confirm designer allows `Users.PasswordHash` nullable.

- [ ] **Step 5: Re-run model tests — expect pass**

Run: `dotnet test backend/TummlyBackend.Tests/TummlyBackend.Tests.csproj --filter "FullyQualifiedName~ExternalAuthModelTests" -v n`

Expected: PASS.

- [ ] **Step 6: Commit** (only if human asked)

```bash
git add backend/TummlyBackend/Models backend/TummlyBackend/Data backend/TummlyBackend/Migrations backend/TummlyBackend.Tests/Services/ExternalAuthModelTests.cs
git commit -m "$(cat <<'EOF'
feat(auth): add external login and OAuth ticket tables

EOF
)"
```

---

### Task 2: Provider client + options (testable)

**Files:**
- Create: `backend/TummlyBackend/Options/ExternalAuthOptions.cs`
- Create: `backend/TummlyBackend/Interfaces/IExternalOAuthProviderClient.cs`
- Create: `backend/TummlyBackend/Services/ExternalOAuthProviderClient.cs`
- Create: `backend/TummlyBackend.Tests/Services/ExternalOAuthProviderClientTests.cs`
- Modify: `backend/TummlyBackend/Program.cs`
- Modify: `backend/TummlyBackend/.env.example`

**Interfaces:**
- Consumes: `IHttpClientFactory`, `IOptions<ExternalAuthOptions>`, `IConfiguration` (`Frontend__BaseUrl`)
- Produces:
  - `ExternalAuthOptions` with nested `Google` / `Microsoft` (`ClientId`, `ClientSecret`, `RedirectUri`)
  - `IExternalOAuthProviderClient.BuildAuthorizationUrl(string provider, string state) → string`
  - `IExternalOAuthProviderClient.ExchangeCodeAsync(string provider, string code, CancellationToken) → ExternalOAuthProfile`
  - `ExternalOAuthProfile { Provider, Subject, Email, EmailVerified, FullName }`

- [ ] **Step 1: Write failing URL builder test**

```csharp
[Fact]
public void BuildAuthorizationUrl_Google_IncludesClientIdAndState()
{
    var options = Options.Create(new ExternalAuthOptions
    {
        Google = new ExternalAuthProviderOptions
        {
            ClientId = "g-client",
            ClientSecret = "g-secret",
            RedirectUri = "https://api.example/api/auth/external/google/callback",
        },
    });
    var client = new ExternalOAuthProviderClient(options, /* mock HttpClient factory unused */);
    var url = client.BuildAuthorizationUrl(ExternalAuthProviders.Google, "state-abc");
    Assert.Contains("client_id=g-client", url);
    Assert.Contains("state=state-abc", url);
    Assert.Contains("accounts.google.com", url);
}
```

- [ ] **Step 2: Run — expect fail** (type missing)

- [ ] **Step 3: Implement options + client**

Google authorize: `https://accounts.google.com/o/oauth2/v2/auth` with `response_type=code`, `scope=openid email profile`, `access_type=online`.

Microsoft authorize: `https://login.microsoftonline.com/common/oauth2/v2.0/authorize` with `scope=openid email profile`.

Token exchange (POST form):
- Google: `https://oauth2.googleapis.com/token` then `https://openidconnect.googleapis.com/userinfo` (or id_token claims).
- Microsoft: `https://login.microsoftonline.com/common/oauth2/v2.0/token` then Graph `/me` or id_token.

Map:
- Google `sub` → Subject; `email_verified` must be true.
- Microsoft `id` / `oid` → Subject; require verified email claim.

Throw `InvalidOperationException("Provider email is not verified.")` when not verified.

Register in Program.cs:

```csharp
builder.Services.Configure<ExternalAuthOptions>(
    builder.Configuration.GetSection("ExternalAuth"));
builder.Services.AddHttpClient(nameof(ExternalOAuthProviderClient));
builder.Services.AddScoped<IExternalOAuthProviderClient, ExternalOAuthProviderClient>();
```

`.env.example`:

```bash
ExternalAuth__Google__ClientId=
ExternalAuth__Google__ClientSecret=
ExternalAuth__Google__RedirectUri=https://api.qa.tummly.com/api/auth/external/google/callback
ExternalAuth__Microsoft__ClientId=
ExternalAuth__Microsoft__ClientSecret=
ExternalAuth__Microsoft__RedirectUri=https://api.qa.tummly.com/api/auth/external/microsoft/callback
```

- [ ] **Step 4: Run URL test — expect pass**

- [ ] **Step 5: Commit** (only if human asked)

---

### Task 3: ExternalAuthService — match rules + tickets

**Files:**
- Create: `backend/TummlyBackend/Interfaces/IExternalAuthService.cs`
- Create: `backend/TummlyBackend/Services/ExternalAuthService.cs`
- Create: `backend/TummlyBackend/DTOs/Auth/ExternalAuthDtos.cs`
- Create: `backend/TummlyBackend.Tests/Services/ExternalAuthServiceTests.cs`
- Modify: `backend/TummlyBackend/Program.cs` (register service)

**Interfaces:**
- Consumes: `ApplicationDbContext`, `IExternalOAuthProviderClient`, `IAuthService`, `IConfiguration` (`Frontend:BaseUrl`), `IOptions<ExternalAuthOptions>`
- Produces:
  - `Task<string> BuildStartRedirectAsync(string provider, string? returnPath, CancellationToken)`
  - `Task<string> HandleCallbackAsync(string provider, string? code, string? state, string? error, CancellationToken)` → frontend absolute URL
  - `Task<object> ExchangeSignInAsync(ExternalAuthExchangeDto dto, CancellationToken)`
  - `Task<SignupSessionResponse> AcceptTermsAsync(ExternalAuthAcceptTermsDto dto, CancellationToken)`

DTO shapes:

```csharp
public sealed class ExternalAuthExchangeDto
{
    public string Token { get; set; } = "";
    public bool RememberDevice { get; set; } = true;
    public string? DeviceToken { get; set; }
}

public sealed class ExternalAuthAcceptTermsDto
{
    public string Token { get; set; } = "";
    public bool TermsAccepted { get; set; }
}
```

Ticket payloads (JSON):
- OAuthState: `{ "provider", "returnPath", "nonce" }`
- SignInExchange: `{ "userId" }`
- SignUpExchange: `{ "email", "provider", "providerSubject", "fullName" }`

- [ ] **Step 1: Write failing match-rule tests**

Use in-memory EF (same pattern as `SignupServiceTests`). Fake `IExternalOAuthProviderClient` that returns a fixed profile. Fake or real `IAuthService` for exchange.

```csharp
[Fact]
public async Task Callback_LinkedSubject_RedirectsSignInExchange()
{
    // Seed User + UserExternalLogin Google/sub-1
    // HandleCallback with matching profile
    // Assert redirect contains "/login/oauth/complete?token="
    // Assert ticket Purpose == SignInExchange
}

[Fact]
public async Task Callback_ExistingUserEmail_NotLinked_RedirectsAccountExists()
{
    // Seed User email a@b.com without external login
    // Profile email a@b.com new subject
    // Assert redirect contains "oauthError=account_exists"
}

[Fact]
public async Task Callback_AdminEmail_RedirectsStaffRejected()
{
    // Seed Admin with email
    // Assert oauthError=staff_password_only (or account_exists — pick one code; use staff_password_only)
}

[Fact]
public async Task Callback_NewEmail_RedirectsSignUpTerms()
{
    // Assert "/signup/oauth/terms?token="
}

[Fact]
public async Task AcceptTerms_CreatesVerifiedPending_NoPassword()
{
    // Create SignUpExchange ticket; AcceptTerms TermsAccepted=true
    // Assert PendingSignup Status=Verified, PasswordHash null, AuthProvider set, TermsAccepted true
}

[Fact]
public async Task AcceptTerms_TermsFalse_Throws()
{
    await Assert.ThrowsAsync<Exception>(() => service.AcceptTermsAsync(new() { Token = t, TermsAccepted = false }));
}

[Fact]
public async Task Exchange_ExpiredToken_Throws()
{
    // ticket ExpiresAtUtc in past
}

[Fact]
public async Task Exchange_SecondUse_Throws()
{
    // consume once then again
}
```

- [ ] **Step 2: Run — expect fail**

- [ ] **Step 3: Implement `ExternalAuthService`**

`BuildStartRedirectAsync`:
1. Validate provider ∈ {Google, Microsoft}.
2. Create `ExternalAuthTicket` purpose `OAuthState`, token = `Convert.ToHexString(RandomNumberGenerator.GetBytes(32))`, TTL 10 min, payload returnPath (default `/login`).
3. Return provider authorization URL with that token as `state`.

`HandleCallbackAsync`:
1. If `error` is access_denied → redirect `{FrontendBase}/login?oauthError=cancelled` (or signup if returnPath was signup).
2. Load state ticket; mark consumed; reject if missing/expired/wrong purpose.
3. Exchange code via provider client.
4. Apply match rules from the spec (linked subject → SignIn ticket; User email → account_exists; Admin/Support → staff_password_only; Complete PendingSignup → account_exists; else SignUp ticket).
5. Redirect to frontend with token or error query.

`AcceptTermsAsync`:
1. Consume SignUpExchange ticket.
2. Require `TermsAccepted`.
3. Create or resume incomplete PendingSignup for email; set `AuthProvider`, `ProviderSubject`, `TermsAccepted`, `Status=Verified`, `EmailVerifiedAt=UtcNow`, clear password hash; return session response.

`ExchangeSignInAsync`:
1. Consume SignInExchange ticket.
2. Call `IAuthService.CompleteExternalOperatorSignInAsync(userId, rememberDevice, deviceToken)` (Task 4).

Frontend base: `configuration["Frontend:BaseUrl"]` trimmed, no trailing slash.

- [ ] **Step 4: Run tests — expect pass** (AuthService method can be stubbed temporarily returning `{ loginType = "USER", otpChannel = "email" }` until Task 4)

- [ ] **Step 5: Commit** (only if human asked)

---

### Task 4: AuthService — external Sign-in completion (OTP / trust)

**Files:**
- Modify: `backend/TummlyBackend/Interfaces/IAuthService.cs`
- Modify: `backend/TummlyBackend/Services/AuthService.cs`
- Modify: `backend/TummlyBackend.Tests/Services/AuthServiceTrustedDeviceTests.cs` (or new `AuthServiceExternalSignInTests.cs`)
- Modify: `backend/TummlyBackend/Services/AuthService.cs` — `ValidateUserCredentialsAsync` null-safe password

**Interfaces:**
- Consumes: existing `TrustedDeviceHelper`, `SendSignInOtpAsync`, `GenerateToken`, `RefreshTokenHelper`, `EnsureOperatorCanSignIn`
- Produces: `Task<object> CompleteExternalOperatorSignInAsync(int userId, bool rememberDevice, string? deviceToken)` — same anonymous object shape as `UniversalLoginAsync` USER branch

- [ ] **Step 1: Write failing tests**

```csharp
[Fact]
public async Task CompleteExternal_FirstSignIn_ReturnsOtpChallenge()
{
    // Seed user HasCompletedFirstSignIn=false, PasswordHash=null, approved, email verified
    // Call CompleteExternalOperatorSignInAsync
    // Assert result has otpChannel, no token
}

[Fact]
public async Task CompleteExternal_TrustedDevice_ReturnsJwt()
{
    // Seed HasCompletedFirstSignIn=true + trusted device token
    // Assert token + refreshToken present
}

[Fact]
public async Task UniversalLogin_NullPasswordHash_ThrowsInvalidCredentials()
{
    // Seed social-only user; password login throws "Invalid email or password."
}
```

- [ ] **Step 2: Run — expect fail**

- [ ] **Step 3: Implement**

Extract shared post-credential USER branch from `UniversalLoginAsync` into a private method used by password and external paths.

In `ValidateUserCredentialsAsync`, if `user.PasswordHash` is null/empty, treat as invalid password (do not call `BCrypt.Verify`).

`CompleteExternalOperatorSignInAsync`:
1. Load user; apply lock / email verified / approved / `EnsureOperatorCanSignIn` checks (same messages).
2. Same trust / OTP / JWT logic as `UniversalLoginAsync` after credential success.
3. Do not increment failed login attempts on success path.

- [ ] **Step 4: Run AuthService external + trusted device tests — expect pass**

- [ ] **Step 5: Commit** (only if human asked)

---

### Task 5: Controller endpoints

**Files:**
- Create: `backend/TummlyBackend/Controllers/ExternalAuthController.cs`
- Create: `backend/TummlyBackend.Tests/Integration/ExternalAuthEndpointsTests.cs` (optional light: start redirects 302 when configured)

**Interfaces:**
- Consumes: `IExternalAuthService`
- Produces: routes under `api/auth/external`

```csharp
[ApiController]
[Route("api/auth/external")]
public class ExternalAuthController : ControllerBase
{
    [HttpGet("{provider}/start")]
    public async Task<IActionResult> Start(string provider, [FromQuery] string? returnPath, CancellationToken ct)
    {
        var url = await _externalAuth.BuildStartRedirectAsync(provider, returnPath, ct);
        return Redirect(url);
    }

    [HttpGet("{provider}/callback")]
    public async Task<IActionResult> Callback(
        string provider,
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        CancellationToken ct)
    {
        var url = await _externalAuth.HandleCallbackAsync(provider, code, state, error, ct);
        return Redirect(url);
    }

    [HttpPost("exchange")]
    public async Task<IActionResult> Exchange([FromBody] ExternalAuthExchangeDto dto, CancellationToken ct)
    {
        try
        {
            var result = await _externalAuth.ExchangeSignInAsync(dto, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("accept-terms")]
    public async Task<IActionResult> AcceptTerms([FromBody] ExternalAuthAcceptTermsDto dto, CancellationToken ct)
    {
        try
        {
            var session = await _externalAuth.AcceptTermsAsync(dto, ct);
            return Ok(new { success = true, data = session });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}
```

- [ ] **Step 1: Add controller + DI already registered**

- [ ] **Step 2: Manual smoke** — with fake client in Development optional; unit coverage from Task 3 is enough if integration is heavy.

- [ ] **Step 3: Commit** (only if human asked)

---

### Task 6: Signup + provisioning for social (no password)

**Files:**
- Modify: `backend/TummlyBackend/Services/SignupService.cs`
- Modify: `backend/TummlyBackend/DTOs/Signup/SaveSignupOnboardingDto.cs` (make Password / ConfirmPassword optional)
- Modify: `backend/TummlyBackend/Services/GuestLoopProvisioningService.cs`
- Create: `backend/TummlyBackend.Tests/Services/SignupServiceSocialTests.cs`
- Modify: `backend/TummlyBackend.Tests/Services/SignupProvisionTests.cs`

**Interfaces:**
- Consumes: `PendingSignup.AuthProvider` / `ProviderSubject`
- Produces: social onboarding save without password; User created with null hash + `UserExternalLogin`

- [ ] **Step 1: Write failing tests**

```csharp
[Fact]
public async Task SaveOnboarding_SocialPending_AllowsEmptyPassword_AndProvisions()
{
    // Seed Verified pending with AuthProvider=Google, ProviderSubject=sub-1, PasswordHash=null
    // SaveOnboarding with password="" / confirm="" and valid profile
    // Assert User.PasswordHash null, UserExternalLogin exists for Google/sub-1, Status Complete
}

[Fact]
public async Task SaveOnboarding_EmailPendingPasswordRequired_StillEnforced()
{
    // Verified pending without AuthProvider still requires password ≥ 8
}
```

- [ ] **Step 2: Run — expect fail** (provision throws missing password hash)

- [ ] **Step 3: Implement**

In `ValidateOnboardingPayload` / `SaveOnboardingAsync`:
- If `pending.AuthProvider` is set, skip password match/length; leave `PasswordHash` null.
- Else keep current password rules and hash.

In `ResolveLastStepHint` for Verified: if social (`AuthProvider` set), hint `"create-password"` becomes `"account"` or keep `"create-password"` but FE will hide password — prefer hint `"account"` when social.

In `ProvisionFromPendingAsync`:
- Allow null `PasswordHash` only when `AuthProvider` is set.
- After `CreateOperatorAccountAsync`, insert:

```csharp
await _context.UserExternalLogins.AddAsync(new UserExternalLogin
{
    UserId = createdUserId,
    Provider = pending.AuthProvider!,
    ProviderSubject = pending.ProviderSubject!,
    CreatedAtUtc = DateTime.UtcNow,
});
```

Pass `PasswordHash: pending.PasswordHash` (nullable) into `ProvisionAccountInput`; update record type to `string?`.

- [ ] **Step 4: Run signup social + provision tests — expect pass**

- [ ] **Step 5: Commit** (only if human asked)

---

### Task 7: Frontend — start URL + social buttons

**Files:**
- Create: `src/lib/externalAuthStart.ts`
- Create: `src/lib/externalAuthStart.test.ts`
- Modify: `src/components/auth/AuthSocialContinueButtons.tsx`

**Interfaces:**
- Consumes: `import.meta.env.VITE_API_BASE_URL` (already includes `/api`)
- Produces: `buildExternalAuthStartUrl(provider: "google" | "microsoft", returnPath: string): string`

```ts
export function buildExternalAuthStartUrl(
  provider: "google" | "microsoft",
  returnPath: string
): string {
  const base = (import.meta.env.VITE_API_BASE_URL as string).replace(/\/$/, "")
  const url = new URL(`${base}/auth/external/${provider}/start`)
  url.searchParams.set("returnPath", returnPath)
  return url.toString()
}
```

Wire buttons:

```tsx
type AuthSocialContinueButtonsProps = {
  layout?: "stack" | "row"
  returnPath: string
}

onClick={() => {
  window.location.assign(buildExternalAuthStartUrl("google", returnPath))
}}
```

Pass `returnPath="/login"` from `SignInForm`, `returnPath="/signup"` from `SignupForm`.

Remove `title="Coming soon"` and stub comments.

- [ ] **Step 1: Write Vitest for URL builder**

```ts
it("builds google start url with returnPath", () => {
  // mock import.meta.env if needed; or test pure helper with base arg overload
})
```

Prefer helper signature `buildExternalAuthStartUrl(provider, returnPath, apiBase)` for testability; wrap env in thin export.

- [ ] **Step 2: Run `npm test -- src/lib/externalAuthStart.test.ts` — fail then pass**

- [ ] **Step 3: Wire buttons**

- [ ] **Step 4: Commit** (only if human asked)

---

### Task 8: Frontend — OAuth complete + Terms pages

**Files:**
- Create: `src/api/externalAuthApi.ts`
- Create: `src/pages/auth/LoginOAuthCompletePage.tsx`
- Create: `src/pages/auth/SignupOAuthTermsPage.tsx`
- Modify: `src/pages/routes/AppRoutes.tsx`
- Modify: `src/pages/auth/LoginPage.tsx`
- Modify: `src/pages/auth/SignupPage.tsx`
- Modify: `src/pages/utils/authHelpers.ts` if needed to reuse session apply helpers

**Interfaces:**
- Consumes: `POST /auth/external/exchange`, `POST /auth/external/accept-terms`
- Produces: routes `/login/oauth/complete`, `/signup/oauth/terms`

- [ ] **Step 1: API helpers**

```ts
export async function exchangeExternalAuth(payload: {
  token: string
  rememberDevice: boolean
  deviceToken?: string
}) {
  const response = await axiosInstance.post("/auth/external/exchange", payload, {
    skipAuthRedirect: true,
  })
  return response.data
}

export async function acceptExternalAuthTerms(payload: {
  token: string
  termsAccepted: boolean
}): Promise<SignupSession> {
  const response = await axiosInstance.post(
    "/auth/external/accept-terms",
    payload,
    { skipAuthRedirect: true }
  )
  return asSignupSession(requireDataObject(response.data))
}
```

- [ ] **Step 2: `LoginOAuthCompletePage`**

On mount: read `token` from query; call exchange with `getDeviceToken()` and `rememberDevice: true`; reuse the same success handling as LoginPage credentials success (set auth store, navigate OTP / activation / dashboard). On failure navigate `/login?oauthError=failed`.

Prefer extracting a shared `applyUniversalLoginResult(result)` from LoginPage if duplication is large — only if needed for a clean call.

- [ ] **Step 3: `SignupOAuthTermsPage`**

Use `SignupModalShell`. Checkbox Terms + Privacy (same links as SignupForm). Continue calls `acceptExternalAuthTerms`, `saveSignupSessionToken`, navigate to `/signup/onboarding` (skip verify).

- [ ] **Step 4: Routes**

```tsx
<Route path="login/oauth/complete" element={<LoginOAuthCompletePage />} />
<Route path="signup/oauth/terms" element={<SignupOAuthTermsPage />} />
```

- [ ] **Step 5: oauthError banners**

On LoginPage / SignupPage, if `searchParams.get("oauthError") === "account_exists"`, set root error: `An account with this email already exists. Sign in with email and password.`

Map `cancelled` → soft message; `failed` / `staff_password_only` → clear generic copy.

- [ ] **Step 6: Manual UI check** (buttons navigate to API start when backend up)

- [ ] **Step 7: Commit** (only if human asked)

---

### Task 9: Frontend — Guest Loop social account step

**Files:**
- Modify: `src/schemas/signupOnboarding.ts`
- Modify: `src/schemas/signupOnboarding.test.ts` (or create if missing)
- Modify: `src/components/signup/SignupAccountStep.tsx`
- Modify: `src/pages/auth/SignupOnboardingPage.tsx`
- Modify: `src/api/signupApi.ts` — include `authProvider` on resume if backend exposes it

**Interfaces:**
- Consumes: session resume field `authProvider` (add to `SignupResumeResponse` / GetBySession)
- Produces: account step without password fields when social

- [ ] **Step 1: Backend resume exposes provider**

Add `AuthProvider` to `SignupResumeResponse` and `GetBySessionAsync` mapping.

- [ ] **Step 2: Frontend schema**

```ts
export const signupAccountStepSocialSchema = z.object({
  email: emailSchema,
  firstName: ...,
  lastName: ...,
})
// password schemas only for non-social
```

- [ ] **Step 3: UI**

If social: hide password + confirm + strength meter; adjust `canContinue` schema; on save send empty password / omit and let API accept.

- [ ] **Step 4: Vitest for social schema**

- [ ] **Step 5: Commit** (only if human asked)

---

### Task 10: Product docs + ops checklist

**Files:**
- Modify: `docs/product/sign-in.md` — Social Sign-in status → Shipped (when feature lands)
- Modify: `docs/product/self-service-pilot.md` — Social start → Shipped
- Modify: `docs/superpowers/specs/2026-09-17-self-serve-signup-design.md` — note OAuth superseded by 2026-09-22 spec (one line)
- Modify: `backend/TummlyBackend/.env.example` (if not done in Task 2)

- [ ] **Step 1: Update status tables**

- [ ] **Step 2: Add short ops note under Sign-in doc**

Register redirect URIs per environment for Google Cloud OAuth client and Microsoft Entra app registration matching `ExternalAuth__*__RedirectUri`.

- [ ] **Step 3: Commit** (only if human asked)

---

## Self-review (plan vs spec)

| Spec requirement | Task |
|------------------|------|
| Backend Authorization Code | 2, 3, 5 |
| Sign-up + Sign-in | 3, 7, 8 |
| No local password for social | 1, 6, 9 |
| Same OTP / trust rules | 4 |
| Block existing email no auto-link | 3 |
| Unknown email on Sign-in → Sign-up | 3 (SignUp ticket) |
| Terms after provider | 3, 8 |
| Staff reject | 3 |
| UserExternalLogin on provision | 6 |
| Verified email only | 2 |
| Config in .env.example | 2, 10 |
| Tests | 1–4, 6–9 |

No intentional TBDs. Provider HTTP details live in Task 2; keep secrets out of git.

---

## Execution handoff

Plan complete and saved to `docs/superpowers/plans/2026-09-22-google-microsoft-oauth.md`.

**Two execution options:**

1. **Subagent-Driven (recommended)** — fresh subagent per task, review between tasks  
2. **Inline Execution** — run tasks in this session with checkpoints  

Which approach?
