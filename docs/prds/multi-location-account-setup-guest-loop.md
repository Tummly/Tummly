# PRD: Multi-Location Account Setup (Guest Loop)

**Status:** Ready for agent  
**Triage label:** `ready-for-agent`

---

## Problem Statement

Multi-location operators who receive an approved Account Setup invite still land on a legacy, inline-styled wizard that does not match the Guest Loop design system used for single-location operators. The current flow exposes a fourth step labelled “Rollout” where operators configure touchpoints, feedback forms, thank-you copy, and offers — behaviour that contradicts the product direction for Guest Loop provisioning. The experience is inconsistent, hard to maintain, and misaligned with Figma and the domain glossary.

Operators need a cohesive four-step Account Setup experience — credentials, group confirmation, location entry, and Guest Loop provisioning — that reuses the same shell, step primitives, and provisioning animations as single-location Account Setup, while supporting multiple locations and correct API integration.

---

## Solution

Replace the legacy multi-location Account Setup UI with a Guest Loop–aligned four-step wizard wrapped in the shared full-page shell. Operators create credentials, confirm their restaurant group, add one or more rollout locations, then watch Guest Loop provisioning prepare their workspace. The setup API is called during the Ready step in parallel with provisioning animations, matching single-location behaviour. Rollout configuration is removed from the operator-facing flow; the backend receives a slim payload with `rolloutApproach: "Multi"`.

---

## User Stories

1. As a multi-location operator arriving from an approval email, I want to see a polished Account Setup experience consistent with single-location operators, so that I trust I am in the right product.

2. As a multi-location operator on Step 1 (Account), I want my email prefilled and read-only, so that I know which invite I am accepting.

3. As a multi-location operator on Step 1, I want my full name prefilled but editable, so that I can correct it before it becomes my name on file.

4. As a multi-location operator on Step 1, I want a password field with a visible strength meter and guidance, so that I can create a secure password confidently.

5. As a multi-location operator on Step 1, I want to confirm my password and accept Terms and Privacy Notice, so that I can proceed legally and safely.

6. As a multi-location operator on Step 1, I want the primary button disabled until all fields pass validation, without showing premature field errors, so that the form feels calm and predictable.

7. As a multi-location operator on Step 1, I want copy that reflects a multi-location approval (“Your multi-location setup request has been approved”), so that the message matches my Trial Request type.

8. As a multi-location operator on Step 1, I want the progress stepper to show Account, Group, Locations, and Ready with Account active, so that I understand the full journey ahead.

9. As a multi-location operator on Step 1, I want a “Create account” button (not “Continue”), so that the CTA matches the design for this step.

10. As a multi-location operator on Step 1, I want no back button, so that I am not encouraged to leave before creating credentials.

11. As a multi-location operator on Step 2 (Group), I want a back button to return to Step 1, so that I can revise my credentials if needed.

12. As a multi-location operator on Step 2, I want to confirm my restaurant group or brand name, so that Tummly creates the correct shared workspace.

13. As a multi-location operator on Step 2, I want to select my business category from the canonical list, so that my workspace is categorized correctly.

14. As a multi-location operator on Step 2, I want business category prefilled from my Trial Request when available, so that I do not re-enter information I already provided.

15. As a multi-location operator on Step 2, I want to select how many locations I operate (location-count band), so that Tummly understands the scale of my group.

16. As a multi-location operator on Step 2, I want number of locations prefilled from my Trial Request when available, so that I do not re-select a band I already chose.

17. As a multi-location operator on Step 2, I want number of locations to be required before I can continue, so that group setup is complete.

18. As a multi-location operator on Step 2, I want an optional primary contact phone field prefilled from my Trial Request mobile number, so that I can keep or change it easily.

19. As a multi-location operator on Step 2, I want an optional website or social link field that starts empty, so that I can add one only if I choose.

20. As a multi-location operator on Step 2, I want helper text explaining I can add rollout locations next and more later from my workspace, so that I understand the flow.

21. As a multi-location operator on Step 2, I want the “Confirm group” button disabled until group name, business category, and number of locations are valid, so that I cannot skip required group details.

22. As a multi-location operator on Step 2, I want the progress stepper to show Account complete and Group active, so that I see my progress accurately.

23. As a multi-location operator on Step 3 (Locations), I want a back button to return to Step 2, so that I can fix group details.

24. As a multi-location operator on Step 3, I want to add at least one location with name, address, and postcode, so that my first rollout has a physical presence in Tummly.

25. As a multi-location operator on Step 3, I want address and postcode side by side with a map-pin affordance on address, so that the layout matches the design and saves vertical space on larger screens.

26. As a multi-location operator on Step 3, I want optional location phone and local contact fields, so that I can add site-specific contacts when useful.

27. As a multi-location operator on Step 3, I want each location in a collapsible card labelled “Location N”, so that multiple locations remain scannable.

28. As a multi-location operator on Step 3, I want the first location expanded by default, so that I immediately see what to fill in.

29. As a multi-location operator on Step 3, I want newly added locations to expand by default, so that I can fill them in right away.

30. As a multi-location operator on Step 3, I want to add another location via “Add location”, so that I can include multiple sites in my first rollout.

31. As a multi-location operator on Step 3, I want to remove a location when I have more than one, so that I can undo a mistaken entry.

32. As a multi-location operator on Step 3, I want to be unable to delete my only remaining location, so that I always have at least one location to submit.

33. As a multi-location operator on Step 3, I want no per-location “include in rollout” toggle, so that the form stays simple (all added locations are included).

34. As a multi-location operator on Step 3, I want to see “Upload locations instead” in the UI, so that I know bulk upload will exist in future.

35. As a multi-location operator on Step 3, I want “Upload locations instead” to log to the console when clicked (stub), so that the affordance is visible without half-built file-upload behaviour.

36. As a multi-location operator on Step 3, I want “Continue to rollout” disabled until all required location fields validate, so that I cannot advance with incomplete data.

37. As a multi-location operator on Step 3, I want the progress stepper to show Account and Group complete and Locations active, so that I see where I am in the flow.

38. As a multi-location operator on Step 4 (Ready), I want Guest Loop provisioning animations identical to single-location, so that the experience feels consistent.

39. As a multi-location operator on Step 4, I want to see the three provisioning phases (Smart Guest Link, private feedback form, starter QR materials), so that I understand what Tummly is preparing.

40. As a multi-location operator on Step 4, I want account setup to run in the background while provisioning animates, so that I am not blocked on a blank screen.

41. As a multi-location operator on Step 4, I want the back button visible but disabled while provisioning succeeds, so that I do not accidentally leave a completed setup.

42. As a multi-location operator on Step 4, I want the back button enabled if provisioning fails, so that I can return to Locations and retry.

43. As a multi-location operator on Step 4, I want to open my workspace when provisioning completes, so that I can start using Tummly.

44. As a multi-location operator on Step 4, I want the Ready step marked complete in the stepper after provisioning finishes, so that the journey feels finished.

45. As a multi-location operator on any step, I want the shared shell with navbar, corner illustration, support footer, and legal footer, so that the experience matches sign-in and single-location Account Setup.

46. As a multi-location operator on any step, I want responsive layout without hardcoded Figma pixel widths, so that the flow works on smaller viewports.

47. As a multi-location operator whose setup token is invalid or expired, I want a clear error state, so that I know to contact support or use a new link.

48. As a multi-location operator whose token is for single-location Account Setup, I want to be redirected to the correct route, so that I do not complete the wrong flow.

49. As Tummly, I want multi-location setup payloads to omit rollout configuration fields, so that the API contract matches the simplified operator experience.

50. As Tummly, I want `rolloutApproach: "Multi"` in the setup payload, so that the backend can distinguish multi-location setup from single-location.

51. As Tummly, I want every submitted location to include `includeInRollout: true`, so that all operator-entered locations join the first rollout without a UI toggle.

52. As Tummly, I want the validate-setup-token endpoint to return the location-count band for multi-location invites, so that Step 2 can prefill number of locations.

---

## Implementation Decisions

### Wizard structure

- Four steps with stepper labels: **Account**, **Group**, **Locations**, **Ready** (Ready = Guest Loop provisioning).
- Orchestration page mirrors single-location: one `react-hook-form` instance, step state, attempted-fields for live validation, token validation hook, provisioning phase state, and `runProvisioningPhases` on the final step.
- Replace legacy `SetupAccountShell` + inline styles with `GuestLoopShell` for all steps.

### Reused modules (extend, do not duplicate)

- **Shell & chrome:** `GuestLoopShell`, `GuestLoopBackButton`, support/legal footers (via shell).
- **Step primitives:** `GuestLoopStepHeader`, `GuestLoopStepFooter`, `GuestLoopProgressStepper`, `GuestLoopStepButton`.
- **Step 1:** Parameterize `GuestLoopPasswordStep` with props for description copy, submit label (`Create account`), and `steps` (multi stepper constant). Generalize `useGuestLoopStepCanSubmit` to accept any form type + step schema (not only single-location form values).
- **Step 4:** Reuse `GuestLoopReadyStep` and `runProvisioningPhases` unchanged in behaviour from single-location.
- **Form controls:** `FormFloatingInput`, `FormFloatingSelect`, `FormCheckboxLabel`, `PasswordStrengthMeter`, `BUSINESS_CATEGORY_OPTIONS`, `LOCATION_COUNT_OPTIONS`.

### New modules

- **`GUEST_LOOP_MULTI_STEPS`:** Four-step progress definition (Account, Group, Locations, Ready).
- **`GuestLoopGroupStep`:** Group name, business category, number of locations (required), optional primary phone, optional website/social link, helper text, Confirm group CTA.
- **`GuestLoopLocationsStep`:** Repeatable location cards, Add location (functional), Upload locations (console.log stub), Continue to rollout CTA.
- **`GuestLoopLocationCard` (or equivalent):** Collapsible card per location index; delete when count > 1; fields per Figma.

### Step 1 specifics

- Description: multi-location approval copy.
- CTA: `Create account`.
- Validation: existing step-1 fields + silent Zod gate via generalized `useGuestLoopStepCanSubmit`.

### Step 2 specifics

- Required: `groupName`, `businessCategory`, `numLocations`.
- Optional: `primaryPhone`, `businessLink`.
- Prefill from validate-setup-token: `groupName` ← business name, `businessCategory`, `numLocations` ← `locations` (multi only), `primaryPhone` ← mobile. **Do not** prefill `businessLink`.
- Introduce `accountSetupMultiStep2Schema` including `numLocations`; update step-2 field list accordingly.

### Step 3 specifics

- Minimum one location; required per row: `locationName`, `address`, `postcode`. Optional: `locationPhone`, `localContact`.
- Remove `includeInRollout` from UI; payload always sends `true`.
- Add location: append via `useFieldArray`; new card expanded.
- Collapse: chevron toggles expand/collapse.
- Delete: available when `locations.length > 1`.
- Upload: visible control; `onClick` → `console.log` stub only (no file picker, no parsing).
- CTA label: **Continue to rollout** (navigates to Ready / Guest Loop provisioning).
- Dynamic step-3 validation field list based on location count (existing helper pattern).

### Step 4 / API timing

- On entering Ready, run `runProvisioningPhases` with setup API call inside (same pattern as single-location).
- Back on Ready: disabled unless `provisioningError`; on error, back returns to Locations and resets provisioning state.
- Success: `Open workspace` → navigate to `/login?setup=complete`.

### Schema & payload cleanup

- Remove from multi form schema and defaults: `rolloutApproach` (as operator input), `guestPrompt`, `thankYouMessage`, offer fields, and related step-4 validation.
- `toMultiLocationSetupPayload` sends only:
  - `token`, credentials, `fullName`
  - `groupName`, `businessCategory`, optional `primaryPhone`, optional `businessLink`
  - `locations[]` with `includeInRollout: true` on every row
  - `rolloutApproach: "Multi"` (discriminator, not operator-configured)
- Align business category values with canonical `BUSINESS_CATEGORY_OPTIONS` (replace legacy hardcoded select options in old page).

### Token validation API (backend — partially done)

- `validate-setup-token` response includes `locations` (location-count band string, e.g. `"2-5"`) when `accountType` is `Multi`; `null` for single.
- Frontend `SetupTokenPrefill` includes optional `numLocations`, parsed from `locations` / `Locations`.

### Back button rules

| Step | Back visible | Back enabled |
|------|----------------|--------------|
| 1 Account | No | — |
| 2 Group | Yes | Yes |
| 3 Locations | Yes | Yes |
| 4 Ready | Yes | Only if provisioning error |

### Responsive & design

- Use clamp / flex patterns from single-location Guest Loop steps; avoid Figma fixed widths (e.g. 560px / 1724px artboard).
- Primary CTAs use `GuestLoopStepButton` (green primary when enabled).

---

## Testing Decisions

### What makes a good test here

Test **observable behaviour** at stable seams: parsed API shapes, Zod validation outcomes, and payload mapping. Do not test component CSS, animation timings, or internal step state machines. Prefer the highest seam that catches regressions without brittle DOM tests.

### Proposed test seams (check with stakeholder)

| Seam | What it guards | Priority |
|------|----------------|----------|
| **Multi setup Zod schemas** | Per-step `safeParse` success/failure for steps 1–3; full schema; password match refine | High |
| **`toMultiLocationSetupPayload`** | Slim DTO: no rollout-config fields; `rolloutApproach: "Multi"`; all locations `includeInRollout: true` | High |
| **`parseValidateSetupTokenResponse`** | `numLocations` from multi token payload; absent for single | Medium (partial coverage exists) |
| **`getAccountSetupMultiStep3FieldNames`** | Field list grows with location count | Low |

**Not in scope for automated tests in this PRD:** full page/wizard E2E, Framer Motion provisioning animations, `GuestLoopShell` layout, upload stub console output.

### Prior art

- `accountSetupSingle.test.ts` — step schemas + `toSingleLocationSetupPayload`
- `accountSetupMulti.test.ts` — update existing tests when schema/payload slimmed
- `setupToken.test.ts` — token prefill parsing

---

## Out of Scope

- CSV / XLSX bulk location upload (UI stub with `console.log` only).
- Rollout configuration UI (touchpoints, feedback form settings, thank-you message, starter offer).
- Per-location `includeInRollout` toggle.
- Prefilling website/social link from Trial Request.
- Multi-location prototype / throwaway route.
- Backend changes to `CompleteSetupDto` shape beyond validate-setup-token `locations` field (DTO may still accept optional rollout fields for backward compatibility; frontend stops sending them).
- Admin review flag when operator changes location count significantly from approved request (noted in Figma annotations; no blocking in this iteration).
- Postcode validation parity with single-location UK regex (follow existing multi schema unless explicitly aligned in implementation).

---

## Further Notes

- Figma references: Step 1 `971:3053`, Step 2 `971:3555`, Step 3 `971:4104`, Step 4 same as single-location Ready / provisioning screens.
- Domain glossary (`CONTEXT.md`) updated: multi-location Account Setup is Account → Group → Locations → Ready; Guest Loop provisioning applies to both operator types.
- Legacy `RegisterMultiPage` is ~2000 lines of inline-styled UI to be replaced, not incrementally patched.
- Single-location `GuestLoopPasswordStep` currently says “guided trial request” in copy; multi uses “multi-location setup request” — keep distinct via props.
- Step 3 button says “Continue to rollout” while Step 4 stepper label is “Ready”; operator-facing “rollout” here means advancing into workspace preparation, not the retired rollout-configuration step.
