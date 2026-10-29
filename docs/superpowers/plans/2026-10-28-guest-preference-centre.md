# Guest Preference Centre Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the one-restaurant Unsubscribe page with a cross-restaurant guest preference centre (Tummly stream + restaurant campaigns), per the Figma frames in the spec.

**Architecture:** A new platform-level `TummlyGuest` (contacts, Tummly-stream events, location mutes) finds its restaurant `LocationGuest`s at read time by normalized contact. All restaurant consent changes go through the existing `ILocationGuestPermissionLedgerService` with source `preference-centre`. A public controller uses a short HMAC session token in header `X-Guest-Preferences-Session`.

**Tech Stack:** ASP.NET Core 8 + EF Core (SQL Server; InMemory in tests) + xUnit; React + Vite + Tailwind + shadcn/ui + Vitest + Testing Library.

**Spec:** `docs/superpowers/specs/2026-10-28-guest-preference-centre-design.md` (read it first; decisions D1–D8, assumptions A1–A5).

## Global Constraints

- Worktree: `.claude/worktrees/guest-preference-centre`, branch `feat/guest-preference-centre` (base `qa`). Never `cd` to the main checkout.
- Ledger source string: `"preference-centre"`. OTP purpose string: `"guest_preferences"`.
- Restorable withdraw sources (D5): `"email-unsubscribe"`, `"preference-centre"` only. Never restore after `"bounce"`, `"complaint"`, `"operator"`, `"sms-stop"`, `"suppression"`, `"legacy-marketing-preference"`.
- Session header: `X-Guest-Preferences-Session`. Session lifetime 30 min sliding. Signing secret config key `GuestPreferences:SessionSigningSecret`, fallback `UnsubscribeLink.ResolveSigningSecret(config)`; HMAC payload carries a `"typ":"gps"` marker so an unsubscribe token can never pass as a session.
- `POST session/start` and `POST contacts` always return **202** with the same body `{ "sent": true }`, contact known or not.
- Contacts in responses and logs are masked (`LogContactMask` in logs; `ContactMask` helper in responses: `so•••••@example.com`, `•••• ••• 789`). Never return a raw contact or a Location Guest id.
- EF migrations: `dotnet ef migrations add` only (`CODING_STANDARDS.md` §EF migrations). New FKs to `RestaurantLocations` use `DeleteBehavior.Cascade`; FKs between Tummly Guest tables cascade from `TummlyGuests`. No FK to `Users`.
- UI: reuse `src/components/ui/*` (`Button`, `CheckboxLabel`, `FloatingLabelInput`, `AlertDialog`, `Dialog`, `InputOTP`, `Accordion`, `Input`, `Spinner`). No raw `<button>`. Dialogs above the page. Fonts per `docs/agents/typography.md` (headline = Plus Jakarta Sans for titles, functional = Helvetica Neue for body). Page canvas reuses `GuestFeedbackShell` (`showPoweredBy={false}`) and the `guest-feedback-*` colour tokens.
- Before any `get_design_context` call, invoke the `figma:figma-design-to-code` skill. Figma URLs are in the spec header.
- Backend test command: `dotnet test backend/TummlyBackend.Tests --filter <Name>`. Frontend: `npx vitest run <path>`.
- Commit after each task; message ends with `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.

## Review Focus

1. **Contact owned by two Tummly Guests** (guest verified email first, then mobile from another session): verifying must merge, not 500 on the unique index — Task 4 test `LinkContact_MergesOtherGuest_WhenContactOwnedElsewhere`.
2. **Same email at two restaurants with different casing / whitespace** (`" Sophie@Example.com"`): both restaurants must appear — Task 4 test `FindLocationGuests_MatchesAcrossRestaurants_CaseInsensitive`.
3. **UK mobile typed as `07700 900123` vs stored `+447700900123`**: must match — Task 4 test `FindLocationGuests_MatchesPhone_NationalFormat`.
4. **Old email link (`/unsubscribe?t=`) opened after deploy**: must land on the preference page signed in — Task 9 test `redirects /unsubscribe?t= to /preferences?t=` and Task 6 test `SessionLink_ReturnsSessionToken_ForUnsubscribeToken`.
5. **Save pressed with no changes** (box shown ticked only from restaurant consent): must write nothing, especially no Tummly `grant` — Task 5 test `SetChannels_NoKeys_WritesNothing` and Task 10 test `Save sends only changed keys`.

---

### Task 0: Fix baseline backend test compile

**Files:**
- Modify: `backend/TummlyBackend.Tests/Services/AdminShopCatalogServiceTests.cs:296-311`

- [ ] **Step 1:** Replace both `actorAdminUserId: 1, actorIdentity: "admin@example.com"` argument pairs with `new AdminShopActor(1, "admin@example.com")` (signature: `SetAvailableInShopAsync(string skuId, bool availableInShop, AdminShopActor actor, string? approvalReference = null, CancellationToken ct = default)`).
- [ ] **Step 2:** Run `dotnet build backend/TummlyBackend.Tests` → 0 errors. Run `dotnet test backend/TummlyBackend.Tests --filter AdminShopCatalogServiceTests` → PASS. Then run the full backend suite once and save the list of failing tests to `/tmp/gpc-backend-baseline.txt` (these are pre-existing; later tasks compare against it).
- [ ] **Step 3:** Commit `test(shop): pass AdminShopActor in catalog test (baseline compile fix)`.

---

### Task 1: Domain model, migration, glossary

**Files:**
- Create: `backend/TummlyBackend/Models/TummlyGuest.cs` (classes `TummlyGuest`, `TummlyGuestContact`, `TummlyGuestMarketingEvent`, `TummlyGuestLocationMute`, static `TummlyGuestContactTypes { Email="email"; Phone="phone" }`, static `TummlyGuestChannels { Email="email"; Sms="sms" }`)
- Modify: `backend/TummlyBackend/Models/LocationGuestPermissionLedgerEntry.cs` (add `PreferenceCentre = "preference-centre"` to `LocationGuestPermissionLedgerSources`)
- Modify: `backend/TummlyBackend/Helpers/OtpPurposes.cs` (add `GuestPreferences = "guest_preferences"`)
- Modify: `backend/TummlyBackend/Data/ApplicationDbContext.cs` (4 DbSets + `OnModelCreating` config)
- Create: migration `AddTummlyGuestPreferenceCentre` via `dotnet ef migrations add AddTummlyGuestPreferenceCentre --project backend/TummlyBackend`
- Modify: `CONTEXT.md` (add **Tummly Guest**, **Tummly marketing**, **Location mute** exactly as spec §4; add "Global guest" stays under Master Guest _Avoid_)
- Test: `backend/TummlyBackend.Tests/Services/TummlyGuestModelTests.cs`

**Interfaces — Produces:** columns exactly as spec §5. `TummlyGuestContact.NormalizedValue` max 320; `FirstName` max 100; `Source` max 64; `WordingSnapshot` max 512. Unique `(ContactType, NormalizedValue)`; unique `(TummlyGuestId, RestaurantLocationId)`; index `(TummlyGuestId, Channel, OccurredAt)` on events.

- [ ] **Step 1:** Write test `Model_SavesGuestWithContactEventAndMute` (InMemory context: add guest + 1 contact + 1 event + 1 mute, save, reload with includes, assert counts 1/1/1) and `Model_HasUniqueIndexOnContactTypeAndValue` (assert `context.Model.FindEntityType(typeof(TummlyGuestContact)).GetIndexes()` contains a unique index over `ContactType, NormalizedValue`).
- [ ] **Step 2:** Run → FAIL (types missing).
- [ ] **Step 3:** Add models, DbSets, config; generate the migration; check `check-migration-designer-pairs.sh` passes.
- [ ] **Step 4:** Run tests → PASS.
- [ ] **Step 5:** Commit `feat(guest-prefs): Tummly Guest tables and ledger source`.

---

### Task 2: Contact normalizing and masking helpers

**Files:**
- Create: `backend/TummlyBackend/Helpers/GuestPreferenceContact.cs`
- Test: `backend/TummlyBackend.Tests/Helpers/GuestPreferenceContactTests.cs`

**Interfaces — Produces:**
- `static bool TryParse(string? raw, out string contactType, out string normalizedValue)` — email: trim + lower-invariant, must match `^[^\s@]+@[^\s@]+\.[^\s@]+$`; phone: `PhoneNumberHelper.TryNormalizeToE164(raw, PhoneNumberHelper.DefaultRegion, out e164)` and must start with `+44` (UK only, per Figma copy). Anything else → false.
- `static string Mask(string contactType, string normalizedValue)` — email: first 2 chars of local part + `•••••` + `@domain`; phone: `•••• ••• ` + last 3 digits.

- [ ] **Step 1:** Tests: `TryParse_Email_TrimsAndLowers` (`" Sophie@Example.com "` → `email`, `sophie@example.com`); `TryParse_UkMobile_NationalFormat` (`"07700 900123"` → `phone`, `+447700900123`); `TryParse_NonUkPhone_False` (`"+12025550123"`); `TryParse_Garbage_False` (`"abc"`); `Mask_Email` (`sophie@example.com` → `so•••••@example.com`); `Mask_Phone` (`+447700900123` → `•••• ••• 123`).
- [ ] **Step 2:** Run → FAIL. **Step 3:** Implement. **Step 4:** Run → PASS.
- [ ] **Step 5:** Commit `feat(guest-prefs): contact parse and mask helpers`.

---

### Task 3: Session token

**Files:**
- Create: `backend/TummlyBackend/Helpers/GuestPreferencesSessionToken.cs`
- Test: `backend/TummlyBackend.Tests/Helpers/GuestPreferencesSessionTokenTests.cs`

**Interfaces — Produces:**
- `const string HeaderName = "X-Guest-Preferences-Session"`; `static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(30)`.
- `static string Create(int tummlyGuestId, int contactId, DateTime issuedAtUtc, string secret)` — same `base64url(payload).base64url(hmac)` shape as `UnsubscribeToken`; payload `{ "typ":"gps", "tg":int, "c":int, "exp":long }`.
- `static bool TryVerify(string? token, string secret, DateTime nowUtc, out int tummlyGuestId, out int contactId)`.
- `static string? ResolveSecret(IConfiguration config)` — `GuestPreferences:SessionSigningSecret`, else `UnsubscribeLink.ResolveSigningSecret(config)`.

- [ ] **Step 1:** Tests: `RoundTrip_ReturnsIds`; `Expired_After30Minutes_False` (issued now, verify at now+30m+1s); `Tampered_False`; `UnsubscribeToken_IsRejected` (a valid `UnsubscribeToken.Create(...)` with the same secret → false, because `typ` is missing).
- [ ] **Step 2–4:** Run FAIL → implement (reuse base64url + `CryptographicOperations.FixedTimeEquals` pattern from `UnsubscribeToken.cs`) → PASS.
- [ ] **Step 5:** Commit `feat(guest-prefs): session token`.

---

### Task 4: Tummly Guest identity service (find, link, merge, match)

**Files:**
- Create: `backend/TummlyBackend/Interfaces/ITummlyGuestIdentityService.cs`
- Create: `backend/TummlyBackend/Services/TummlyGuestIdentityService.cs`
- Modify: `backend/TummlyBackend/Program.cs` (register scoped, next to `IGuestInitiatedMarketingWithdrawService` at ~line 689)
- Test: `backend/TummlyBackend.Tests/Services/TummlyGuestIdentityServiceTests.cs`

**Interfaces — Consumes:** Task 1 models, Task 2 `GuestPreferenceContact`.
**Produces:**
```csharp
public sealed record ProvenContact(int TummlyGuestId, int ContactId);
Task<ProvenContact> FindOrCreateByContactAsync(string contactType, string normalizedValue, CancellationToken ct);
// Contact verified by the guest who owns tummlyGuestId. If another Tummly Guest owns it, merge that guest into tummlyGuestId (contacts, mutes union, events), delete it. Returns the contact row id.
Task<int> LinkContactAsync(int tummlyGuestId, string contactType, string normalizedValue, CancellationToken ct);
// All Location Guests whose Master Guest NormalizedEmail / NormalizedPhone equals one of the guest's contacts, any restaurant, AND that have at least one Grant of EmailMarketing or SmsMarketing in the ledger (A5). Includes RestaurantLocation + Restaurant.
Task<IReadOnlyList<LocationGuest>> FindLocationGuestsAsync(int tummlyGuestId, CancellationToken ct);
```
Phone match: move `MasterGuestMatchesE164` and `TryResolveComparableE164` from `GuestInitiatedMarketingWithdrawService` into new `public static class GuestPhoneMatch` in `backend/TummlyBackend/Helpers/GuestPhoneMatch.cs` (no `InternalsVisibleTo` exists), and call it from both services. Pre-filter in SQL by last 9 digits (same as the withdraw service), then match in memory.

- [ ] **Step 1:** Tests:
  - `FindOrCreate_CreatesOnce_ThenReturnsSame`
  - `LinkContact_AddsSecondContact`
  - `LinkContact_MergesOtherGuest_WhenContactOwnedElsewhere` (guest A has email, guest B has phone + mute on location 7 + 1 event; link phone to A → B deleted; A has 2 contacts, mute on 7, the event)
  - `FindLocationGuests_MatchesAcrossRestaurants_CaseInsensitive` (2 restaurants, MasterGuest.NormalizedEmail `sophie@example.com` in both, each with a GuestForm grant → 2 results)
  - `FindLocationGuests_MatchesPhone_NationalFormat` (MasterGuest.NormalizedPhone `+447700900123`, contact `+447700900123` → 1)
  - `FindLocationGuests_ExcludesNeverGranted` (Location Guest with no ledger grant → excluded)
- [ ] **Step 2–4:** FAIL → implement (one `SaveChanges`; merge in one unit of work) → PASS.
- [ ] **Step 5:** Commit `feat(guest-prefs): identity service with merge and cross-restaurant match`.

---

### Task 5: Preference service (read + write rules D2, D5, D6, A2)

**Files:**
- Create: `backend/TummlyBackend/Interfaces/IGuestPreferenceService.cs`
- Create: `backend/TummlyBackend/Services/GuestPreferenceService.cs`
- Create: `backend/TummlyBackend/Helpers/GuestPreferenceRestoreRule.cs` (pure)
- Create: `backend/TummlyBackend/DTOs/GuestPreferences/GuestPreferencesDtos.cs`
- Modify: `backend/TummlyBackend/Program.cs` (register scoped)
- Test: `backend/TummlyBackend.Tests/Helpers/GuestPreferenceRestoreRuleTests.cs`, `backend/TummlyBackend.Tests/Services/GuestPreferenceServiceTests.cs`

**Interfaces — Consumes:** Task 4; `ILocationGuestPermissionLedgerService` (`RecordEvent`, `GetCurrentStatesBatchAsync`, `SyncMarketingPreferenceRollupAsync`).
**Produces:**
```csharp
// Pure. history = this guest's ledger rows for one kind, any order.
static bool GuestPreferenceRestoreRule.CanRestore(IReadOnlyList<LocationGuestPermissionLedgerEntry> history);
// true iff: ≥1 Grant exists AND latest row is Withdraw AND latest.Source ∈ {email-unsubscribe, preference-centre}

record GuestPreferencesOverviewDto(string? FirstName, IReadOnlyList<GuestPreferenceContactDto> Contacts, bool EmailOn, bool SmsOn, int RestaurantCount);
record GuestPreferenceContactDto(int Id, string Type, string Masked);
record GuestPreferenceChannelsRequest(bool? Email, bool? Sms);
record GuestPreferenceRestaurantDto(int RestaurantId, string Name, string? LogoUrl, IReadOnlyList<GuestPreferenceLocationDto> Locations);
record GuestPreferenceLocationDto(int LocationId, string Name, bool Muted);

Task<GuestPreferencesOverviewDto> GetOverviewAsync(int tummlyGuestId, CancellationToken ct);
Task SetFirstNameAsync(int tummlyGuestId, string firstName, CancellationToken ct);           // trim, 1..100, else ArgumentException
Task SetChannelsAsync(int tummlyGuestId, GuestPreferenceChannelsRequest req, CancellationToken ct);
Task StopAllAsync(int tummlyGuestId, CancellationToken ct);                                   // = SetChannels(false,false) but always writes Tummly withdraw
Task<IReadOnlyList<GuestPreferenceRestaurantDto>> ListRestaurantsAsync(int tummlyGuestId, string? q, CancellationToken ct);
Task SetLocationMuteAsync(int tummlyGuestId, int locationId, bool muted, CancellationToken ct); // KeyNotFoundException if location not in guest's list
Task SetRestaurantMuteAsync(int tummlyGuestId, int restaurantId, bool muted, CancellationToken ct);
```
Rules (spec §8): `EmailOn` = latest Tummly event for `email` is grant **or** any un-muted matched Location Guest has EmailMarketing `Granted` (D6). Tick on writes Tummly `grant` with `WordingSnapshot = "Email marketing — Receive dining news and offers by Email."` (SMS: `"SMS marketing — Receive dining news and offers by text."`), then restore. Untick writes Tummly `withdraw` + ledger withdraw where not Withdrawn. A `null` key in the request writes nothing for that channel. Mute = add row + ledger withdraw Email+SMS at that location; Unmute = delete row + restore only channels whose `*On` is true. Each rollup flip to OptedOut adds a `LocationActivity` (`ActorDisplayName = "Guest"`, `Kind = LocationActivityKinds.GuestMarketingUnsubscribed`, `Description = "Guest withdrew marketing permission at this location."`). One `SaveChangesAsync` per call. `q` filters case-insensitively on restaurant name or location name. `LogoUrl` via `BrandLogoRules.BuildAbsolutePublicUrl`.

- [ ] **Step 1:** Rule tests `CanRestore_True_AfterEmailUnsubscribe`, `CanRestore_True_AfterPreferenceCentre`, and `[Theory]` `CanRestore_False_After(source)` for `bounce`, `complaint`, `operator`, `sms-stop`, `suppression`; `CanRestore_False_WhenNeverGranted`; `CanRestore_False_WhenLatestIsGrant`.
- [ ] **Step 2:** Service tests: `Overview_EmailOn_FromRestaurantGrantOnly`; `SetChannels_NoKeys_WritesNothing` (assert 0 new ledger rows, 0 Tummly events); `SetChannels_EmailOff_WithdrawsEverywhereAndTummly`; `SetChannels_EmailOn_RestoresOnlyGuestMadeWithdrawals` (loc A withdrawn by email-unsubscribe → granted; loc B withdrawn by bounce → stays withdrawn; loc C never granted → no row); `SetChannels_EmailOn_SkipsMutedLocation`; `StopAll_WithdrawsBoth_KeepsFeedbackFollowUp`; `MuteLocation_WithdrawsEmailAndSms_AndExcludesFromOverview`; `UnmuteLocation_RestoresOnlyChannelsThatAreOn`; `MuteRestaurant_MutesAllItsMatchedLocations`; `MuteLocation_NotInList_Throws`; `ListRestaurants_FiltersByQuery`.
- [ ] **Step 3:** Run → FAIL. **Step 4:** Implement. **Step 5:** Run → PASS.
- [ ] **Step 6:** Commit `feat(guest-prefs): preference service and restore rule`.

---

### Task 6: Session endpoints, verify email template, rate limit

**Files:**
- Create: `backend/TummlyBackend/Controllers/PublicGuestPreferencesController.cs` (session part)
- Create: `backend/TummlyBackend/Services/GuestPreferencesVerificationService.cs` + interface (start / verify for both entry and add-contact)
- Create: `backend/TummlyBackend/Helpers/GuestPreferencesConfirmEmailTemplate.cs` (+ HTML in `Assets/emails/templates/guest-preferences-confirm.html`, copy below)
- Modify: `backend/TummlyBackend/Interfaces/IEmailService.cs`, `Services/EmailService.cs` (`Task SendGuestPreferencesConfirmEmailAsync(string toEmail, string confirmUrl)`), `TummlyBackend.Tests/Helpers/EmailServiceStubBase.cs` (no-op override)
- Modify: `backend/TummlyBackend/Program.cs` (register; rate-limit policy `"guest-preferences"`: 20 requests / IP / hour, partition pattern as `SignupRateLimitSettings` in `GlobalLimiter`)
- Test: `backend/TummlyBackend.Tests/Services/GuestPreferencesVerificationServiceTests.cs`, `backend/TummlyBackend.Tests/Integration/PublicGuestPreferencesSessionEndpointsTests.cs`

**Interfaces — Produces:**
```csharp
public enum GuestVerifyOutcome { Ok, Invalid, Expired }
Task StartAsync(string rawContact, int? linkToTummlyGuestId, CancellationToken ct);   // never throws for unknown/invalid contact; silently no-op when TryParse fails or >5 starts for the contact in the last hour
Task<(GuestVerifyOutcome Outcome, ProvenContact? Proven)> VerifyCodeAsync(string rawContact, string code, int? linkToTummlyGuestId, CancellationToken ct);
Task<(GuestVerifyOutcome Outcome, ProvenContact? Proven)> VerifyEmailLinkAsync(string v, int? linkToTummlyGuestId, CancellationToken ct); // v = "<otpId>.<code>"
```
Email: `IOtpChallengeService.IssueAsync(new OtpIssueRequest(email, OtpPurposes.GuestPreferences, SubjectId: linkTo?.ToString() ?? "entry", Generation: 0))` → URL `{Frontend:BaseUrl}/preferences?v={id}.{code}` (entry) or `/preferences?link={id}.{code}` (add contact). Verify: load row by id, then `VerifyAsync` with its email/purpose/subject. SMS: `ISmsService.SendOtpSmsAsync(e164)` / `VerifyOtpSmsAsync(e164, code)`; count starts per contact with an `OtpVerification` row (`Channel = sms`, `OtpCode = OtpVerification.TwilioManagedCode`, `Email = e164`). An entry start sends only when the contact matches ≥1 Master Guest (no mail to strangers) — response identical either way.

Endpoints (spec §7): `session/link` (body `{token}` → `UnsubscribeToken.TryVerify` → Master Guest `NormalizedEmail` → `FindOrCreateByContactAsync` → 200 `{ sessionToken }`; invalid → 200 `{ valid:false }`), `session/start` (202 `{sent:true}`), `session/verify-code`, `session/verify-email` (200 `{ sessionToken }` or 400 `{ error: "invalid" | "expired" }`). All under `[EnableRateLimiting("guest-preferences")]`.

Email copy (A3): subject `Confirm your email for Tummly`; body `Open this link to confirm your email address and manage your Tummly marketing preferences. The link expires in 10 minutes.`; button `Confirm email`.

- [ ] **Step 1:** Service tests: `Start_UnknownEmail_SendsNothing`; `Start_KnownEmail_SendsLink`; `Start_SixthInHour_SendsNothing`; `VerifyEmailLink_Ok_CreatesGuest`; `VerifyEmailLink_Reused_Invalid`; `VerifyCode_WrongCode_Invalid` (fake `ISmsService` returns false).
- [ ] **Step 2:** Integration tests: `SessionLink_ReturnsSessionToken_ForUnsubscribeToken`; `SessionLink_Invalid_ReturnsValidFalse`; `SessionStart_UnknownAndKnown_SameResponse` (status + body equal); `VerifyEmail_ThenToken_Verifies` (decode returned token with `GuestPreferencesSessionToken.TryVerify`).
- [ ] **Step 3–5:** FAIL → implement → PASS.
- [ ] **Step 6:** Commit `feat(guest-prefs): session start, verify and link endpoints`.

---

### Task 7: Preference and contact endpoints

**Files:**
- Modify: `backend/TummlyBackend/Controllers/PublicGuestPreferencesController.cs`
- Create: `backend/TummlyBackend/Helpers/GuestPreferencesSessionFilter.cs` (action filter: reads header, verifies, puts `TummlyGuestId` in `HttpContext.Items["TummlyGuestId"]`, writes renewed token to the response header; missing/invalid → 401 `{ error: "session" }`)
- Test: `backend/TummlyBackend.Tests/Integration/PublicGuestPreferencesEndpointsTests.cs`

**Interfaces — Consumes:** Task 5 service, Task 6 verification service. Endpoints exactly as spec §7 rows GET `/`, PUT `name`, PUT `channels`, POST `stop-all`, GET `restaurants?q=`, PUT `locations/{id}/mute`, PUT `restaurants/{id}/mute`, POST `contacts`, POST `contacts/verify-code`, POST `contacts/verify-email`. Mapping: `ArgumentException` → 400, `KeyNotFoundException` → 404. Expose the header to the browser: add `.WithExposedHeaders(GuestPreferencesSessionToken.HeaderName)` to the CORS policy in `Program.cs` (~line 1361). The request header needs no change if the policy already uses `AllowAnyHeader()`; else add it to `WithHeaders`.

- [ ] **Step 1:** Tests: `Get_WithoutSession_401`; `Get_WithOperatorJwt_401` (send `Authorization: Bearer <StubJwtService token>` only); `Get_ReturnsMaskedContactNoRawEmail` (body text must not contain `sophie@example.com`); `Get_RenewsSessionHeader`; `PutChannels_EmailFalse_CampaignEligibilitySkipsGuest` (call `CampaignEligibilityService` for the seeded location → reason `"withdrawn"`); `PutLocationMute_OtherGuestsLocation_404`; `PostContacts_SameResponseForAnyInput`.
- [ ] **Step 2–4:** FAIL → implement → PASS.
- [ ] **Step 5:** Commit `feat(guest-prefs): preference and contact endpoints`.

---

### Task 8: Links, List-Unsubscribe headers, one-click, remove form

**Files:**
- Modify: `backend/TummlyBackend/Helpers/UnsubscribeLink.cs` (`/unsubscribe?t=` → `/preferences?t=`; `ForRestaurant` → `/preferences`; add `static string OneClickUrl(string apiBaseUrl, string token)` → `{apiBase}/api/public/preferences/one-click?t={token}`)
- Modify: `backend/TummlyBackend/Services/EmailService.cs` (`SendEmailAsync` + `SendViaResendAsync` gain `IReadOnlyDictionary<string,string>? headers = null`, mapped to Resend JSON `headers`; SMTP path adds them to the message)
- Modify: `IEmailService.SendCampaignGuestEmailAsync` and `SendGuestResponseEmailAsync` (new optional `string? oneClickUnsubscribeUrl = null` → headers `List-Unsubscribe: <url>` and `List-Unsubscribe-Post: List-Unsubscribe=One-Click`); callers `CampaignOutboundEmailSender.cs:85`, `GuestResponseEmailDeliveryWork.cs:636` pass it when a signed token exists. New config key `Api:PublicBaseUrl` (e.g. `https://api.qa.tummly.com`; add commented `Api__PublicBaseUrl=` to `backend/TummlyBackend/.env.example`). When it is empty, send no `List-Unsubscribe` headers (local dev).
- Modify: `PublicGuestPreferencesController.cs` (`[HttpPost("one-click")]` `[Consumes("application/x-www-form-urlencoded")]`, query `t` → `IGuestInitiatedMarketingWithdrawService.WithdrawForLocationGuestAsync(..., EmailMarketing, EmailUnsubscribe)`; always 200)
- Modify: `PublicUnsubscribeController.cs` (delete `form` action and `UnsubscribeFormRequest`)
- Modify tests: `UnsubscribeLinkTests.cs`, `PublicUnsubscribeEndpointsTests.cs` (remove form tests), stubs implementing `IEmailService`
- Test: `backend/TummlyBackend.Tests/Integration/PublicGuestPreferencesOneClickTests.cs`, `backend/TummlyBackend.Tests/Services/EmailServiceHeadersTests.cs`

- [ ] **Step 1:** Tests: `ForLocationGuest_UsesPreferencesPath`; `OneClick_WithdrawsEmailAtSenderLocationOnly` (2 restaurants; only token's location withdrawn); `OneClick_BadToken_200NoChange`; `UnsubscribeForm_Removed_404`; `Resend_Payload_IncludesListUnsubscribeHeaders` (capture the HTTP request with a fake `HttpMessageHandler`).
- [ ] **Step 2–4:** FAIL → implement → PASS. Run the full backend suite; compare with `/tmp/gpc-backend-baseline.txt` → no new failures.
- [ ] **Step 5:** Commit `feat(guest-prefs): preferences links, one-click and List-Unsubscribe`.

---

### Task 9: Frontend API client, session store, routes

**Files:**
- Create: `src/api/publicGuestPreferencesApi.ts` (+ `.test.ts`)
- Create: `src/lib/guestPreferences/guestPreferencesSession.ts` (+ `.test.ts`)
- Create: `src/lib/guestPreferences/guestPreferencesPageMode.ts` (+ `.test.ts`)
- Modify: `src/pages/routes/AppRoutes.tsx` (add top-level `<Route path="preferences" element={<ErrorBoundary><GuestPreferencesPage/></ErrorBoundary>} />` beside `scan/:token`, outside `MainLayout`; replace `unsubscribe` route with `<UnsubscribeRedirect />` that navigates to `/preferences` + same `location.search`, `replace`)
- Delete: `src/pages/public/UnsubscribePage.tsx`, `src/api/publicUnsubscribeApi.ts(+test)`, `src/lib/publicUnsubscribe/*`

**Interfaces — Produces:**
```ts
// session store (sessionStorage key "tummly.guestPreferences.session")
export function readGuestPreferencesSession(): string | null
export function writeGuestPreferencesSession(token: string): void
export function clearGuestPreferencesSession(): void

// page mode from URL
export type GuestPreferencesPageMode =
  | { kind: "link"; token: string }       // ?t=
  | { kind: "verify-email"; v: string }   // ?v=
  | { kind: "link-contact"; v: string }   // ?link=
  | { kind: "session" }                   // stored session, no params
  | { kind: "entry" }                     // nothing
export function resolveGuestPreferencesPageMode(params: URLSearchParams, hasSession: boolean): GuestPreferencesPageMode

// API (plain axios, NOT axiosInstance; header from session store; on response header present → writeGuestPreferencesSession; on 401 → clear + throw GuestPreferencesSessionExpiredError)
openSessionFromLink(token): Promise<{ valid: boolean }>
startSession(contact): Promise<void>
verifySessionCode(contact, code): Promise<void>
verifySessionEmail(v): Promise<void>
fetchOverview(): Promise<GuestPreferencesOverview>
updateFirstName(firstName): Promise<void>
updateChannels(changes: { email?: boolean; sms?: boolean }): Promise<void>
stopAllMarketing(): Promise<void>
fetchRestaurants(q?: string): Promise<GuestPreferenceRestaurant[]>
setLocationMuted(locationId, muted): Promise<void>
setRestaurantMuted(restaurantId, muted): Promise<void>
addContact(contact): Promise<void>
verifyContactCode(contact, code): Promise<void>
verifyContactEmail(v): Promise<void>
```
- [ ] **Step 1:** Tests: mode resolver for each param (+ `t` wins over session); API test `sends X-Guest-Preferences-Session header`, `stores renewed header`, `401 clears session and throws GuestPreferencesSessionExpiredError` (mock axios as in `publicUnsubscribeApi.test.ts`); route test `redirects /unsubscribe?t= to /preferences?t=` (MemoryRouter).
- [ ] **Step 2–4:** FAIL → implement → PASS. `npm run typecheck` clean.
- [ ] **Step 5:** Commit `feat(guest-prefs): frontend api, session store and routes`.

---

### Task 10: Preference page — base view, name edit, channels, Stop all

Invoke `figma:figma-design-to-code`, then `get_design_context` for nodes `7191:814`, `7194:1073`, `7185:392`.

**Files:**
- Create: `src/pages/public/GuestPreferencesPage.tsx` (mode switch only)
- Create: `src/components/guest-preferences/GuestPreferencesOverview.tsx`, `GuestPreferencesNameRow.tsx`, `GuestPreferencesChannels.tsx`, `GuestPreferencesStopAllDialog.tsx`
- Create: `src/content/guestPreferences/guestPreferencesCopy.ts` (all strings below)
- Test: `src/components/guest-preferences/GuestPreferencesOverview.test.tsx`

Copy (Figma): title `Manage your Tummly marketing preferences`; lead `Manage how you receive Tummly dining news and offers, and update your restaurant preferences.`; card titles `Your details`, `How Tummly contacts you`, `Restaurant preferences`; `First name`, `Edit`, `Save`; `Verified contact`; `Add another contact method`; `Email marketing` / `Receive dining news and offers by Email.`; `SMS marketing` / `Receive dining news and offers by text.`; `Choose which restaurants you'd prefer not to hear about.`; `Manage restaurants`; `Save preferences`; `Stop all Tummly marketing`; dialog title `Stop all Tummly marketing?`, body `You'll stop receiving Tummly marketing emails and texts. Replies about your feedback won't be affected.`, buttons `Keep my preferences` / `Stop all Tummly marketing`; footer links `Terms & Conditions` (`/terms`) · `Privacy Notice` (`/privacy`). Success toast (`sonner`): `Preferences saved.`

- [ ] **Step 1:** Tests: `renders name, masked contact and channel state from overview`; `Save sends only changed keys` (untick Email → `updateChannels` called with `{ email: false }` exactly); `Save with no change does not call updateChannels`; `Stop all opens dialog; Keep closes without call; confirm calls stopAllMarketing`; `Edit name shows input and Save calls updateFirstName`; `session expired shows entry form`.
- [ ] **Step 2–4:** FAIL → implement → PASS.
- [ ] **Step 5:** Commit `feat(guest-prefs): preference page base view`.

---

### Task 11: Entry, verification and add-contact flows

Invoke `figma:figma-design-to-code`, then `get_design_context` for `7196:1157`, `7196:1226`, `7198:1284`.

**Files:**
- Create: `src/components/guest-preferences/GuestPreferencesEntry.tsx` (no-link form; same input as add contact)
- Create: `src/components/guest-preferences/GuestPreferencesContactInput.tsx` (`FloatingLabelInput` label `Email address or UK mobile number` + `Continue`)
- Create: `src/components/guest-preferences/GuestPreferencesVerifyMobileDialog.tsx` (`InputOTP` 6 digits; `Verify`; `Resend code` disabled 60 s; `Change number`)
- Create: `src/components/guest-preferences/GuestPreferencesVerifyEmailDialog.tsx` (title `Verify your Email address`; body `We sent a confirmation link to {masked}. Open the link to confirm the Email address.`; `Resend link`; `Change Email`)
- Create: `src/lib/guestPreferences/contactInput.ts` (+ test): `classifyContact(raw): "email" | "uk-mobile" | null` (client mirror of Task 2; UK = `+44…` or `07…`)
- Test: `src/components/guest-preferences/GuestPreferencesEntry.test.tsx`, `GuestPreferencesVerifyMobileDialog.test.tsx`

Mobile titles: entry `Verify your mobile number`, add-contact `Verify your new mobile number`; body `Enter the six-digit code sent to your number.` Invalid input error: `Enter a valid email address or UK mobile number.` Wrong code: `That code is not correct or has expired.` Link-mode invalid: `This link has expired. Enter your email address or UK mobile number to continue.`

- [ ] **Step 1:** Tests: `classifyContact` table (`a@b.co`→email, `07700 900123`→uk-mobile, `+447700900123`→uk-mobile, `+12025550123`→null); `email entry opens email dialog with masked address`; `mobile entry opens code dialog; 6 digits + Verify calls verifySessionCode`; `Resend code disabled for 60 s` (fake timers); `invalid link shows expired message and entry form`; `add contact success refetches overview`.
- [ ] **Step 2–4:** FAIL → implement → PASS.
- [ ] **Step 5:** Commit `feat(guest-prefs): entry and contact verification flows`.

---

### Task 12: Manage restaurants view

Invoke `figma:figma-design-to-code`, then `get_design_context` for `7191:598`.

**Files:**
- Create: `src/components/guest-preferences/GuestPreferencesRestaurants.tsx` (Back `Button`, title `Manage restaurants`, lead `Choose which restaurants or Locations may feature in the Tummly marketing messages you receive.`, search `Input` placeholder `Search by restaurant name or location` (300 ms debounce → `fetchRestaurants(q)`), `Accordion` per brand: logo, name, `{n} Locations`; rows: location name, `Muted` / `Not muted`, action `Mute` / `Unmute`; footer button `Mute all brand locations` / `Unmute all brand locations` when all muted)
- Test: `src/components/guest-preferences/GuestPreferencesRestaurants.test.tsx`

View switch: page state `view: "overview" | "restaurants"` in `GuestPreferencesPage` (no new route). Empty list copy: `No restaurants yet.` Mute action colour uses the existing destructive token (compare Figma red with tokens; add a token only if none is close).

- [ ] **Step 1:** Tests: `renders brands collapsed except first`; `Mute calls setLocationMuted(id, true) and row shows Muted`; `Mute all brand locations calls setRestaurantMuted`; `search debounces and calls fetchRestaurants with q`; `Back returns to overview`.
- [ ] **Step 2–4:** FAIL → implement → PASS.
- [ ] **Step 5:** Commit `feat(guest-prefs): manage restaurants view`.

---

### Task 13: Whole-branch verification

- [ ] **Step 1:** `npm run typecheck && npm run lint && npx vitest run` → only the baseline failure `assistantWaitGerunds.test.ts`.
- [ ] **Step 2:** `dotnet test backend/TummlyBackend.Tests` → no failures beyond `/tmp/gpc-backend-baseline.txt`.
- [ ] **Step 3:** Run the app (`run` skill) and walk: old `/unsubscribe?t=` link → overview; untick Email → Save; Manage restaurants → Mute → Back; Stop all → dialog. Screenshot each.
- [ ] **Step 4:** Commit any fixes; report.
