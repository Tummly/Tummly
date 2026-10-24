# WS1 Guest consent and Offers Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Issue the Standard Thank-You Offer independent of marketing consent, remove the unlock "second ask", make service replies (with or without a recovery Offer) depend on Feedback follow-up only, and pin Guest Form, Claim and Redemption rules with tests.

**Architecture:** Backend first. One shared helper (`ServiceReplyEligibility`) holds the service-reply rule, and compose, recovery-Offer and email delivery all call it. The unlock path (endpoint, token, DTO, UI) is deleted, not hidden. The operator recovery UI drops its marketing gates so it matches the server. No EF migration.

**Tech Stack:** .NET 10 / EF Core 8 (xUnit, InMemory `TummlyWebApplicationFactory`), React + Vite + TS (vitest, node env, `*.test.ts` only).

**Spec:** `.scratch/ws1-guest-consent-offers/PRD.md` (rules R1–R5, acceptance 04.1–04.3, 05.1–05.4).

## Global Constraints

- Branch `ws1-guest-consent-offers` from `origin/qa` (e87f540a or later). No EF migration and no model-snapshot change.
- Before you write code, watch each new test fail. Full suites: **no new failures** against the base lists (backend `/tmp/b_norm.txt`, 124 names; frontend: `assistantWaitGerunds`, `createFeedbackDetailsModule`). Base tests that WS1 deletes or repairs leave the list. Record each one in the PR.
- API field `GuestName` stays. Public wire field `offersOptOut` stays (it means "marketing not granted").
- Do not run `npx prettier`. The repo has no config, and prettier defaults rewrite whole files. Frontend style: no semicolons, double quotes.
- Reuse `src/components/ui/*`. Do not add one-off controls.
- Join Offer: do not build it (decision A). Recovery Offer and service replies: no marketing consent (decision A).
- Service-reply denial message, exact: `Guest has not granted feedback follow-up permission.`

## Review Focus

1. **Follow-up withdrawn between compose and email delivery.** The queued email must be skipped at delivery, not sent. Test: Task 6 `Delivery_SkipsWhenFollowUpWithdrawnAfterCompose`.
2. **A guest with marketing withdrawn but follow-up granted.** The recovery Offer send must succeed on Email and SMS. Test: Task 5 `SendAndIssue_Succeeds_WhenMarketingWithdrawnButFollowUpGranted` (theory over channel).
3. **Network retry of the same Guest Form submit.** It must return the first result and write no second Feedback, consent event or Claim. Test: Task 3 `SubmitFeedback_DuplicateWithinWindow_WritesNothingNew`.
4. **Two contacts in one field** (`alex@example.com 07123456789`). Reject on server and client. Tests: Task 1.
5. **Offer issue throws after Feedback is saved.** The guest gets plain success and classification still starts. Test: Task 2 `SubmitFeedback_OfferIssueThrows_KeepsFeedbackAndReturnsPlainSuccess`.

---

### Task 1: Guest Form first name and one contact (R1.1, R1.2, R1.5)

**Files:**
- Modify: `backend/TummlyBackend/Helpers/GuestFormContactValidate.cs`
- Modify: `backend/TummlyBackend/Controllers/ScanController.cs` (SubmitFeedback name messages)
- Modify: `src/schemas/messages.ts` (`guestFeedback.guestName.required`), `src/schemas/guestFeedback.ts` (max message), `src/components/guest-feedback/GuestFeedbackForm.tsx:363` (placeholder)
- Test: `backend/TummlyBackend.Tests/Helpers/GuestFormContactValidateTests.cs`, `backend/TummlyBackend.Tests/Integration/SmartGuestLinkEndpointsTests.cs`, `src/schemas/guestFeedback.test.ts`

**Interfaces:**
- Produces: `GuestFormContactValidate.TryResolve(string?, out ContactType)`. The signature does not change. The function is stricter.

- [ ] **Step 1: Write the failing tests**

Backend theory `TryResolve_RejectsTwoContacts` with the inline data `"alex@example.com 07123456789"`, `"07123456789 alex@example.com"`, `"alex@example.com, bob@example.com"`, `"alex@example.com;bob@example.com"`, `"a@b@example.com"`, and `"07123456789 07987654321"`. Each case asserts `false` and `ContactType.Unknown`. Keep the existing positive cases, and add a case that `"07123 456789"` stays a valid Phone.

Integration `SubmitFeedback_BlankName_ReturnsFirstNameRequired`: the response is 400 and `message == "First name is required."`. Integration `SubmitFeedback_StoresCommentTextUnchanged`: submit the comment `"  Line one\nLine two — café  "` and assert that the stored `Feedback.Comment == "Line one\nLine two — café"` (trim only).

Frontend `guestFeedback.test.ts`: `guestFeedbackSchema.safeParse({...valid, guestContact: "alex@example.com 07123456789"}).success === false`. A blank name gives the error message `"First name is required."`.

- [ ] **Step 2: Run the tests and make sure they fail**

Run: `cd backend && dotnet test TummlyBackend.Tests --filter "FullyQualifiedName~GuestFormContactValidateTests|FullyQualifiedName~SmartGuestLinkEndpointsTests.SubmitFeedback_BlankName|FullyQualifiedName~SmartGuestLinkEndpointsTests.SubmitFeedback_StoresCommentTextUnchanged"` and `npx vitest run src/schemas/guestFeedback.test.ts`
Expected: two-contact email cases return true. The name message is "Name is required." / "Your name is required.".

- [ ] **Step 3: Implement**

Email branch: accept only when `trimmed` has no whitespace, `,` or `;`, has exactly one `@`, `EmailAddressAttribute` passes, and `MailAddress.TryCreate(trimmed, out var m) && m.Address == trimmed`. The Phone branch does not change. Server messages: `"First name is required."` and `"First name must be 150 characters or fewer."`. Client: `required: "First name is required."`, max `"First name must be 150 characters or fewer."`, placeholder `"First name"`, and `autoComplete="given-name"`.

- [ ] **Step 4: Run the tests and make sure they pass** (same commands). Expected: PASS.

- [ ] **Step 5: Commit** — `git commit -m "Guest Form: first name, reject two contacts"`

---

### Task 2: Standard Offer independent of marketing (R2.1–R2.3, R2.5)

**Files:**
- Modify: `backend/TummlyBackend/Services/OfferIssueService.cs:99-171` (remove the `IsMarketingOfferBlockedAsync` call in `IssueOnThankYouSubmitAsync`)
- Modify: `backend/TummlyBackend/Controllers/ScanController.cs:415-481` (try/catch around issue; classification notify before issue; inject `ILogger<ScanController>`)
- Test: `backend/TummlyBackend.Tests/Services/OfferIssueServiceTests.cs`, `backend/TummlyBackend.Tests/Integration/SmartGuestLinkEndpointsTests.cs`

**Interfaces:**
- Consumes: none.
- Produces: `IssueOnThankYouSubmitAsync(...)`. The signature does not change. It no longer reads marketing state.

- [ ] **Step 1: Write the failing tests**

- Rename `IssueOnThankYouSubmit_OptOut_SkipsIssue` → `IssueOnThankYouSubmit_OptOut_StillIssuesStandardOffer`. It asserts NotNull, `Source == GuestFormThankYou`, and 1 row.
- `IssueOnThankYouSubmit_MarketingOnAndOff_SameOffer`: two guests (opt-in and opt-out) at the same Location get the same `CatalogOfferId` and `Title`.
- `IssueOnThankYouSubmit_NoLiveOffer_ReturnsNull`: if this case is not already covered, add it with no `ThankYouCatalogOfferId`.
- Keep the existing repeat tests (lines ~240–375). They pin the existing Claim on repeat and suppression after redeem/expiry/void.
- Integration `SubmitFeedback_WithActiveThankYouAttach_WhenOptOut_IssuesStandardOffer`: `offersOptOut: true` → 200, `offer.claimCode` non-null, and no `unlockOffer` property in the response.
- Integration `SubmitFeedback_OfferIssueThrows_KeepsFeedbackAndReturnsPlainSuccess`: `_factory.WithWebHostBuilder` replaces `IOfferIssueService` with a stub. The stub's `IssueOnThankYouSubmitAsync` throws `OfferIssueCodeAllocationException`. Assert 200, `success == true`, `offer` null, and 1 Feedback row stored.
- Integration `SubmitFeedback_RepeatSameGuest_ReturnsExistingClaim`: two submits with the same contact and different comments → the same `claimCode`, the second has `alreadyClaimed == true`, and `OfferIssues` count is 1.

- [ ] **Step 2: Run the tests and make sure they fail**

Run: `dotnet test TummlyBackend.Tests --filter "FullyQualifiedName~OfferIssueServiceTests|FullyQualifiedName~SmartGuestLinkEndpointsTests"`
Expected: the opt-out cases return null. The throw case returns 500.

- [ ] **Step 3: Implement**

Delete the marketing block at `OfferIssueService.cs:124-132`. In SubmitFeedback, move `await _classificationWork.NotifyAsync(feedback.Id)` to right after the persist loop. Wrap the issue call in `try { ... } catch (Exception ex) when (ex is not OperationCanceledException)`, log a warning with `feedback.Id`, and set `issued = null`. Do not add an Offer notification. R2.5 holds because no notification is written on issue.

- [ ] **Step 4: Run the tests and make sure they pass** (same command). Expected: PASS. `SubmitFeedback_WithActiveThankYouAttach_CreatesGuestFormThankYouIssue` (fails on base) now passes too.

- [ ] **Step 5: Commit** — `git commit -m "Issue Standard Thank-You Offer without marketing consent"`

---

### Task 3: Remove the unlock path and stop duplicate submits (R1.4, R1.6, R2.4)

**Files:**
- Delete: `backend/TummlyBackend/Helpers/ThankYouOfferUnlockToken.cs`, `backend/TummlyBackend/DTOs/Scan/ScanUnlockThankYouOfferDto.cs` (and `UnlockThankYouOfferRequest` if it is in that file), `backend/TummlyBackend.Tests/Helpers/ThankYouOfferUnlockTokenTests.cs`
- Modify: `backend/TummlyBackend/Controllers/ScanController.cs` (delete `UnlockThankYouOffer`, `TryBuildUnlockOfferAsync`, the `unlockOffer` local and response field; add the duplicate guard), `backend/TummlyBackend/Services/OfferIssueService.cs` + `Interfaces/IOfferIssueService.cs` (delete `TryGetUnlockableThankYouOfferTitleAsync`)
- Modify tests: `OfferIssueServiceTests.cs` (delete the 3 `TryGetUnlockable…` tests), `SmartGuestLinkEndpointsTests.cs` (delete `UnlockThankYouOffer_AfterOptOutSubmit_IssuesOffer`), `Integration/DemoRestaurantGuestFormTests.cs` (delete the unlock-403 test)
- Test: `SmartGuestLinkEndpointsTests.cs`

**Interfaces:**
- Produces: the SubmitFeedback response shape `{ success, message, offer }` (no `unlockOffer`). Task 4 relies on this.

- [ ] **Step 1: Write the failing tests**

- `UnlockRoute_IsRemoved_Returns404`: POST `/api/scan/{token}/thank-you-offer/unlock` with body `{ "unlockToken": "x" }` → 404.
- `SubmitFeedback_Decline_CompletesWithoutSecondAsk`: `offersOptOut: true`, no live Offer → 200, `success` true, and the JSON has no `unlockOffer` property.
- `SubmitFeedback_DuplicateWithinWindow_WritesNothingNew`: submit the same token, contact and comment twice (marketing granted, live Offer). Assert 1 Feedback, the `LocationGuestPermissionLedgerEntries` count after call 2 equals the count after call 1, 1 OfferIssue, the `Notifications` count does not change, and call 2 returns the same `offer.claimCode` with `alreadyClaimed == true`.
- `SubmitFeedback_SameContactNewComment_IsNotDuplicate`: a different comment → 2 Feedback rows.

- [ ] **Step 2: Run the tests and make sure they fail**

Run: `dotnet test TummlyBackend.Tests --filter "FullyQualifiedName~SmartGuestLinkEndpointsTests"`
Expected: unlock → 403/400, duplicate → 2 Feedback rows.

- [ ] **Step 3: Implement**

Delete the listed code. Guard: after reCAPTCHA and before the rate-limit counter adds, query `_context.Feedbacks.AsNoTracking()` for a row with the same `RestaurantLocationId`, `GuestContact == dto.GuestContact.Trim()`, `Comment == dto.Comment.Trim()` and `CreatedAt >= UtcNow - DuplicateSubmitWindow`, where `private static readonly TimeSpan DuplicateSubmitWindow = TimeSpan.FromMinutes(10)`. If a row is found, write nothing. Build `offer` from `IssueOnThankYouSubmitAsync(location.Id, row.LocationGuestId, row.Id, UtcNow, allowNewIssue: false)` (read-only), and return the normal 200 shape. A DB check (not memory cache) keeps the guard correct across replicas.

- [ ] **Step 4: Run the tests and make sure they pass**, then `dotnet build` (no references remain). Expected: PASS. The base failure `ThankYouOfferUnlockTokenTests.*` leaves the list (deleted).

- [ ] **Step 5: Commit** — `git commit -m "Remove thank-you unlock second ask; ignore duplicate Guest Form submits"`

---

### Task 4: Guest Form frontend without unlock (R1.3, R1.4, R2.4)

**Files:**
- Delete: `src/components/guest-feedback/GuestFeedbackUnlockOffer.tsx`
- Modify: `src/api/scanApi.ts` (delete `GuestUnlockThankYouOffer`, `unlockGuestThankYouOffer`, `parseUnlockThankYouOffer`, `unlockOffer` fields), `src/pages/public/GuestFeedbackPage.tsx` (delete the `"unlock"` phase and its state), `src/lib/guestFeedback/guestFeedbackUnlockPresentation.ts` (delete `GuestFeedbackUnlockChannel`, `GuestFeedbackUnlockCopy*`, `buildGuestFeedbackUnlockCopy`; keep the display helpers used by `GuestFeedbackSuccess.tsx`)
- Test: `src/api/scanApi.test.ts`, `src/lib/guestFeedback/guestFeedbackUnlockPresentation.test.ts`, `src/lib/guestFeedback/guestFormConsentPresentation.test.ts`

**Interfaces:**
- Consumes: the Task 3 response shape.
- Produces: `submitGuestFeedback(token, values, opts) → { offer: GuestThankYouOffer | null }`.

- [ ] **Step 1: Write the failing tests**

- `scanApi.test.ts`: "submit result has no unlockOffer key" → `expect(Object.keys(result)).not.toContain("unlockOffer")` even when the server sends one.
- `guestFormConsentPresentation.test.ts`: if these are not already covered, add "Email contact shows only the email channel" and "switching Email → Phone returns sms".
- Delete the unlock-copy tests.

- [ ] **Step 2: Run the tests and make sure they fail** — `npx vitest run src/api/scanApi.test.ts src/lib/guestFeedback`. Expected: `unlockOffer` is present.

- [ ] **Step 3: Implement** — delete the listed code. In `GuestFeedbackPage`, a submit with `offer == null` goes to `"success"`.

- [ ] **Step 4: Run the tests and make sure they pass**, then `npx tsc -b --noEmit` (or the repo `typecheck` script). Expected: PASS with no type errors.

- [ ] **Step 5: Commit** — `git commit -m "Guest Form: remove unlock screen"`

---

### Task 5: One service-reply rule at compose (R3.1–R3.4)

**Files:**
- Create: `backend/TummlyBackend/Helpers/ServiceReplyEligibility.cs`
- Modify: `Services/FeedbackGuestResponsesService.cs` (replace `EnsureFeedbackFollowUpAllowedAsync` + `EnsureEmailMarketingAllowedAsync`), `Services/FeedbackRecoveryOffersService.cs:297-353` (replace `EnsureChannelSendAllowedAsync`), `Services/OfferIssueService.cs:273-281` (delete the marketing block in `CreateRecoveryIssueAsync`), `Helpers/LocationGuestChannelPermissionGate.cs` (delete `IsMarketingOfferAllowed` and the private `IsMarketingOfferBlockedAsync` if nothing else uses them)
- Modify tests: `Integration/FeedbackGuestResponseEndpointsTests.cs` (repair `SeedOwnerWithFeedbackAsync`), `Integration/FeedbackRecoveryOfferEndpointsTests.cs`, `Services/OfferIssueServiceTests.cs`
- Test: `backend/TummlyBackend.Tests/Helpers/ServiceReplyEligibilityTests.cs`

**Interfaces:**
- Produces:
  ```csharp
  public static class ServiceReplyEligibility
  {
      public const string NotAllowedMessage =
          "Guest has not granted feedback follow-up permission.";
      // Restaurant FeedbackFollowUp enabled AND guest FeedbackFollowUp Granted
      // (effective state, legacy rollup aware) AND contact resolves via
      // GuestFormContactValidate to the channel's ContactType. WS2c adds
      // bounce/complaint suppression here.
      public static Task<bool> IsAllowedAsync(
          ApplicationDbContext context,
          Feedback feedback,
          FeedbackGuestResponseChannel channel,
          CancellationToken cancellationToken);
  }
  ```
  Callers throw `new ArgumentException(ServiceReplyEligibility.NotAllowedMessage)` when the result is false. Load the states with `new LocationGuestPermissionLedgerService(context).GetCurrentStatesAsync` and `ResolveEffectiveStates`, as `GuestResponseEmailDeliveryWork` already does.

- [ ] **Step 1: Write the failing tests**

- `ServiceReplyEligibilityTests` (InMemory context):
  - `Allowed_WhenFollowUpGranted_MarketingNotRecorded` (Email and SMS theory) → true
  - `Allowed_WhenMarketingWithdrawn_FollowUpGranted` → true
  - `Denied_WhenFollowUpWithdrawn` → false
  - `Denied_WhenRestaurantFollowUpDisabled` → false
  - `Denied_WhenNoLocationGuest` → false
  - `Denied_WhenChannelDoesNotMatchContact` (Phone contact, Email channel) → false
- Repair `SeedOwnerWithFeedbackAsync`: create a `LocationGuest` for the contact, link `Feedback.LocationGuestId`, and record a `FeedbackFollowUp` Grant (source `GuestForm`). Add `bool followUpGranted = true` and `LocationGuestPermissionState? marketing = null` parameters. The 8 base-failing `FeedbackGuestResponseEndpointsTests` must now pass.
- `SendGuestResponse_Email_Succeeds_WithoutMarketingOptIn` → 200, 0 Campaign Email credit entries.
- `SendGuestResponse_Returns400_WhenFollowUpWithdrawn` (Email and SMS theory) → 400 with `NotAllowedMessage`.
- `FeedbackRecoveryOfferEndpointsTests`: rename `SendAndIssue_Returns400_WhenGuestOffersOptOut` → `SendAndIssue_Succeeds_WhenMarketingWithdrawnButFollowUpGranted` (theory Email/SMS; 200, 1 Recovery OfferIssue). Add `SendAndIssue_Returns400_WhenFollowUpWithdrawn`.
- `OfferIssueServiceTests.IssueOnRecoverySend_OptOut_SkipsIssue` → rename to `IssueOnRecoverySend_OptOut_StillIssues` (NotNull).

- [ ] **Step 2: Run the tests and make sure they fail**

Run: `dotnet test TummlyBackend.Tests --filter "FullyQualifiedName~ServiceReplyEligibility|FullyQualifiedName~FeedbackGuestResponseEndpointsTests|FullyQualifiedName~FeedbackRecoveryOfferEndpointsTests|FullyQualifiedName~OfferIssueServiceTests"`
Expected: compile error (no helper), then marketing-gate 400s.

- [ ] **Step 3: Implement** — add the helper and replace the three gates. Credits do not change (R3.4). Do not touch `EnsureOperatorBillingAllowsSendAsync` or the SMS reserve.

- [ ] **Step 4: Run the tests and make sure they pass** (same command). Expected: PASS.

- [ ] **Step 5: Commit** — `git commit -m "Service replies and recovery Offers need follow-up, not marketing"`

---

### Task 6: Same rule at email delivery (R3.1 delivery, R3.3, 05.4 failures)

**Files:**
- Modify: `backend/TummlyBackend/Services/GuestResponseEmailDeliveryWork.cs:~560-585` (replace the `CanSendOnChannel(..., "email")` check with `ServiceReplyEligibility.IsAllowedAsync`; delete `ResolveEffectivePermissionStatesAsync` if it is no longer used)
- Rename: `Services/GuestResponseEmailMarketingNotGrantedException.cs` → `GuestResponseEmailNotEligibleException.cs` (class and catch site, log text "service reply not eligible")
- Test: the existing delivery-work test file (find it with `grep -rl GuestResponseEmailDeliveryWork backend/TummlyBackend.Tests`)

**Interfaces:**
- Consumes: `ServiceReplyEligibility.IsAllowedAsync` (Task 5).

- [ ] **Step 1: Write the failing tests**

- `Delivery_SendsWhenMarketingNotGranted_FollowUpGranted` → the email service gets 1 call.
- `Delivery_SkipsWhenFollowUpWithdrawnAfterCompose`: the row is Pending, then a FeedbackFollowUp Withdraw is recorded, then the work runs → 0 email calls and the row leaves Pending in the same way as the current skip branch.
- If they are not already covered: `Delivery_ProviderFailure_SchedulesRetry` and `Delivery_RunTwice_SendsOnce`.

- [ ] **Step 2: Run the tests and make sure they fail** — expected: test 1 is skipped by the marketing check.

- [ ] **Step 3: Implement** — swap the check and rename the exception.

- [ ] **Step 4: Run the tests and make sure they pass.** Expected: PASS.

- [ ] **Step 5: Commit** — `git commit -m "Check follow-up, not marketing, at guest email delivery"`

---

### Task 7: Operator recovery UI follows the server rule (R3 UI, R4 likely cause)

**Files:**
- Modify: `src/lib/operatorFeedback/startRecoveryPresentation.ts` (`buildStartRecoveryIntents`: delete `isMarketingEligibleForOffer`, `OFFERS_OPT_OUT_REASON`, `MARKETING_NOT_AVAILABLE_REASON`; Respond* and Respond-with-offer need only contact + follow-up)
- Modify: `src/lib/operatorFeedback/feedbackDetailRecoveryPresentation.ts` (`deriveFeedbackDetailRecoveryActions`: after the contact + follow-up checks → `respondEnabled: true, addOfferEnabled: true`; in disabled branches `addOfferHelper` = the same reason as `respondDisableReason`; delete `emailMarketingRequiredReason` / `marketingNotAvailableHelper` copy if they are no longer used; keep `buildFeedbackDetailPermissionSummary`)
- Modify: `src/lib/operatorFeedback/recoveryDraftAction.ts` (delete `offersOptOut` toast + branch), `src/lib/operatorFeedback/recoveryComposerPresentation.ts` (delete `shouldDetachOfferOnEligibilityLoss`; `buildRecoveryComposerStatusBanner()` takes no arguments and returns `{ kind: "service-only", message: RECOVERY_COMPOSER_COPY.serviceReply }`; `mapRecoveryComposerSendFailure` maps a message that contains `"feedback follow-up permission"` to `no_permission`)
- Modify: `src/lib/operatorFeedback/createRespondToGuestModule.ts:~811`, `createRespondWithRecoveryOfferModule.ts:~1178, ~2505-2540` (delete the marketing channel state and the RC-03 detach)
- Test: the matching `*.test.ts` files for each module above

**Interfaces:**
- Produces: `RECOVERY_COMPOSER_COPY.serviceReply = "Service reply to this guest's Feedback. Marketing permission is not needed."` and `RECOVERY_COMPOSER_COPY.noPermission = "This guest has not asked for a reply to their Feedback."`

- [ ] **Step 1: Write the failing tests**

- `startRecoveryPresentation.test.ts`: "Email contact, follow-up granted, marketing not granted → all Respond intents enabled". "Follow-up not available → Respond-with-offer disabled with `Feedback follow-up is not available for this guest.`"
- `feedbackDetailRecoveryPresentation.test.ts`: "Email, follow-up granted, marketing not granted → respondEnabled and addOfferEnabled true".
- `recoveryComposerPresentation.test.ts`: "server follow-up denial maps to no_permission".
- `createRespondWithRecoveryOfferModule.test.ts`: "marketing withdrawn before send keeps the attached Offer".
- Update or delete the tests that assert the old marketing reasons (`"Guest has opted out of offers"`, marketing banner, detach).

- [ ] **Step 2: Run the tests and make sure they fail** — `npx vitest run src/lib/operatorFeedback`. Expected: Add offer disabled, marketing reasons.

- [ ] **Step 3: Implement** — apply the listed changes.

- [ ] **Step 4: Run the tests and make sure they pass**, then the full `npx vitest run` and the typecheck. Expected: only the 2 base frontend failures.

- [ ] **Step 5: Commit** — `git commit -m "Recovery UI: follow-up gates Respond and Add offer, not marketing"`

---

### Task 8: Atomic Redemption and Claim/Redemption evidence tests (R5, 05.3)

**Files:**
- Modify: `backend/TummlyBackend/Services/OfferIssueService.cs:403-405` (`RedeemClaimCodeAsync`)
- Test: `backend/TummlyBackend.Tests/Integration/OffersRedeemEndpointsTests.cs`, `backend/TummlyBackend.Tests/Services/OfferIssueServiceTests.cs`

**Interfaces:**
- Produces: `RedeemClaimCodeAsync`. The signature does not change. A lost race returns `Failed(OfferRedeemFailureReasons.AlreadyUsed)`.

- [ ] **Step 1: Write the tests** (verification tests can pass on the first run; record that)

- Service `Redeem_SecondConfirm_ReturnsAlreadyUsed`: redeem the same code twice → Ok, then `Failed(AlreadyUsed)`, and `RedeemedAtUtc` keeps the first time.
- Service `Redeem_StaleTrackedIssue_ReturnsAlreadyUsed`: context A loads the issue, context B (same InMemory DB name) redeems it, then A calls `RedeemClaimCodeAsync` → `AlreadyUsed`. Today this fails: A overwrites.
- Integration:
  - `CheckRedeem_DoesNotRedeem`: after check, `RedeemedAtUtc` is null.
  - `MarkRedeemed_Voided_Fails`
  - `MarkRedeemed_Expired_Fails`
  - `MarkRedeemed_UnknownCode_Fails`
  - `MarkRedeemed_TeamMemberWithoutOffersAccess_Returns403`
  - `CheckRedeem_GuestEmailOrPhoneAsCode_ReturnsInvalid`: theory with the guest's real contact strings.

- [ ] **Step 2: Run the tests** — `dotnet test TummlyBackend.Tests --filter "FullyQualifiedName~Redeem"`. Expected: `Redeem_StaleTrackedIssue_ReturnsAlreadyUsed` FAILS. The other tests show which R5 rules already hold.

- [ ] **Step 3: Implement** — when `_context.Database.IsRelational()`: `ExecuteUpdateAsync` on `Id == issue.Id && RedeemedAtUtc == null && CancelledAtUtc == null`, set `RedeemedAtUtc = atUtc`. 0 rows → `AlreadyUsed`. Otherwise (InMemory) reload the row with `AsNoTracking` just before the save and return `AlreadyUsed` if `RedeemedAtUtc != null`. The SQL path is the real guarantee. QA proves it in Task 10.

- [ ] **Step 4: Run the tests and make sure they pass.** Expected: PASS.

- [ ] **Step 5: Commit** — `git commit -m "Redeem confirm is atomic; Claim and Redemption rule tests"`

---

### Task 9: Marketing withdrawal excluded at campaign send time (04.3)

**Files:**
- Test: `backend/TummlyBackend.Tests/Services/CampaignFireServiceTests.cs`

- [ ] **Step 1: Write the test** `FireAsync_GuestWithdrawsViaUnsubscribeAfterSchedule_IsExcludedAtSend`: `SeedScheduledCampaignAsync(frozenEligibleCount: 2, scheduledAtUtc: _now.AddMinutes(-5))`. Record a ledger Withdraw for guest 0's channel kind with source `EmailUnsubscribe` (or `SmsStop` for SMS seeds) through `LocationGuestPermissionLedgerService`, then call `FireAsync`. `_outbound.Calls` must contain only guest 1's destination.

- [ ] **Step 2: Run it.** If it passes, this records the existing behaviour as evidence. If it fails, fix `CampaignEligibilityService.ListChannelEligibleLocationGuestIdsAsync` so that it reads ledger state, and run the test again.

- [ ] **Step 3: Commit** — `git commit -m "Test: withdrawn guest excluded at campaign send"`

---

### Task 10: Respond / Add offer routing on QA, PR, and evidence (R4, acceptance)

This task runs after Tasks 1–9 are on a QA deploy. A person or an agent with a browser does it.

- [ ] **Step 1: Reproduce on QA** (`https://qa.tummly.com`, operator account). Open a Feedback with an Email contact where follow-up is granted and marketing is not granted. Click **Respond**, then **Add offer**, from the Feedback detail and from the Inbox row actions. Write the exact steps, the URL before and after, and the result into `.scratch/ws1-guest-consent-offers/R4-repro.md`.
- [ ] **Step 2:** If the routing is still wrong after Task 7, write a failing `*.test.ts` on the module that builds the route (`recoveryDraftAction.ts` `parseRecoveryDraftActionPayload` / the router state builder). Fix it, then commit `"Fix Respond/Add offer routing"`.
- [ ] **Step 3: Full suites.** Run backend `dotnet test` and diff the failures against `/tmp/b_norm.txt`. Run frontend `npx vitest run`. Expected: no new failures. List the base failures that are fixed or deleted.
- [ ] **Step 4:** Push, open a PR to `qa`, and link it to the thread. After the deploy, record the QA evidence for 04.1–04.3 and 05.1–05.4 in the PRD acceptance format (ID, build, environment, steps, expected, observed, PASS/FAIL, date, tester). This includes a parallel double Redeem confirm (`curl … & curl … & wait`): it must give one Ok and one `already_used`.
