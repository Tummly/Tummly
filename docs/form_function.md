# Tummly — Site-wide Form Function

**Status:** Complete (all 6 implementation issues done)  
**Last updated:** 2026-06-14  
**Stack:** React Hook Form + Zod + shadcn `Form`

This document consolidates the architectural decision, product requirements, implementation log, domain glossary, migration issues, validation-timing work, and future enhancements for Tummly's site-wide form adoption.

---

## Domain glossary

Tummly is a restaurant guest-relationship platform. Operators capture feedback, manage offers, and run campaigns across single or multi-location hospitality businesses.

| Term | Definition | Avoid in docs |
|------|------------|---------------|
| **Trial Request** | A prospective operator's application to start a guided trial, submitted from the marketing site. Requires email verification before Tummly reviews the request. | Register, sign up, registration |
| **Account Setup** | The post-approval flow where an invited operator creates credentials and configures their workspace, accessed via an invite token. | Register, onboarding form |
| **Sign-in** | Authentication for returning operators or admins, including password reset and OTP verification for user accounts. | Login (acceptable in UI copy only) |

---

## Architectural decision

### Problem

Auth and onboarding flows were built with manual `useState` and hand-written validators, duplicated across five surfaces with inconsistent error UX (`alert()`, field errors, global banners).

### Decision

Adopt **React Hook Form + Zod** site-wide:

- Centralized schemas in `src/schemas/`
- shadcn `Form` as the bridge (`Form`, `FormField`, `FormItem`, `FormControl`, `FormMessage`)
- Thin wrappers in `src/components/form/` (`FormFloatingInput`, `FormFloatingSelect`, `FormCheckboxLabel`)
- Base floating-label components remain free of RHF coupling

### Options considered

| Option | Verdict |
|--------|---------|
| **React Hook Form + Zod** | **Chosen** — best shadcn alignment, efficient re-renders |
| TanStack Form | Viable but weaker shadcn ecosystem |
| Formik | Rejected — heavier re-renders, dated patterns |

### Wizard state (hybrid model)

| Flow | `useForm` pattern | OTP |
|------|-------------------|-----|
| Trial Request | One instance for the wizard | Outside RHF (local state) |
| Account Setup (single + multi) | One instance; `useFieldArray` for multi-location | N/A |
| Sign-in | One instance per screen/step view | Outside RHF |
| Standalone password reset | One instance per page | N/A |

### Validation timing

Site-wide defaults in `src/lib/form.ts`:

```ts
export const defaultFormValidationOptions = {
  mode: "onSubmit",
  reValidateMode: "onChange",
}
```

**Behaviour:** No field errors until the first failed submit attempt. After that, per-field revalidation on every change — errors clear as soon as the value passes.

**Wizard step gates:** Account Setup validates the current step on Continue via `trigger(stepFields)`. A failed Continue marks that step as attempted so those fields revalidate on change — because `trigger()` alone does not set RHF `isSubmitted`. Trial Request, Sign-in, and password reset rely on `handleSubmit` for the first attempt.

**Wizard bridge:** `WizardLiveValidationProvider` + `attemptedFields` set + wrapper `trigger()` on change for attempted fields.

**Cross-field pairs:** Password ↔ confirm password revalidate together on change (`getCrossFieldPeers` in `src/lib/form.ts`) so mismatch errors clear as soon as both fields align.

**Post-submit typing:** Fields that already failed revalidate on every change — errors update live (including mid-typing feedback) until the value passes.

### Schemas

- Match backend FluentValidation rules; frontend may be stricter
- Reuse backend message strings in `src/schemas/messages.ts`
- Payload mapping functions ensure correct DTO shape before API calls

### Error handling

- No `alert()` for validation or API errors
- Field-level: `FormMessage` / `FieldErrorSlot`
- Form-level API failures: `setError("root", { message })`
- Success may use toast where already established

### Migration order (completed)

1. Form foundation and primitives
2. Trial Request (`HeroTrialForm`) — reference implementation
3. Standalone password reset (`ResetPasswordPage`)
4. Account Setup single location (`RegisterSinglePage`)
5. Account Setup multi-location (`RegisterMultiPage`)
6. Sign-in wizard (`LoginPage`)

---

## Product requirements (summary)

### Problem statement

Validation rules were duplicated across flows. Error handling was inconsistent. Multi-step wizards lacked shared patterns for step gating, cross-field rules, and dynamic field lists.

### Solution

One form stack for every data-entry surface: shared Zod schemas, shadcn Form bridge, thin wrappers, phased migration starting with Trial Request as reference.

### User stories (38 total)

Grouped by flow:

**Trial Request (1–8, 34):** Inline validation before submit; backend-parity messages; URL validation for business link; OTP feedback, resend, email change; confirmation screen; terms acceptance required.

**Account Setup (9–16, 33, 35):** Step-by-step Continue gating; invite email pre-filled and read-only; password confirmation; back navigation without data loss; dynamic locations (multi); per-location validation; conditional offer fields; rollout toggle; inline API errors.

**Sign-in (17–23, 36):** Credential validation; field-level errors; admin direct route vs user OTP; forgot-password email validation; in-flow reset; familiar OTP UX.

**Developer / maintainer (24–32, 37–38):** Shared primitives; backend parity; reference implementation; floating labels decoupled from RHF; accessible errors; loading states; API error field mapping; OTP separate from wizard payload; ADR on stack choice.

### Implementation decisions

**Dependencies:** `react-hook-form`, `zod`, `@hookform/resolvers`

**Schema layer:** `src/schemas/primitives.ts` + flow-specific schemas (`trialRequest`, `accountSetupSingle`, `accountSetupMulti`, `signIn`, `resetPassword`)

**UI integration:** Wrappers compose `FormField` with existing floating-label components; preserve visual design and error slot layout.

**API error mapping:** Map server field errors when available; otherwise root-level error. Reuse existing API client modules.

### Testing

| Seam | Scope |
|------|-------|
| Zod schema unit tests (Vitest) | Primary — primitives + flow schemas |
| OTP helper unit tests | API message mapping |
| Payload contract tests | Parsed output matches DTO shape |
| Component tests | Deferred |
| E2E | Out of scope |

**Current coverage:** 62 tests across 7 files; `npm test` and `npm run typecheck` pass.

### Out of scope

- Non-form UI (e.g. admin dashboard search)
- Backend validation rule changes
- E2E browser tests
- Future dashboard settings / campaign editors
- Replacing `react-hot-toast`
- Google OAuth Sign-in (UI present, not wired)

---

## Implementation log

### Issue 01 — Form foundation and primitives ✅

**Completed:** 2026-06-14

- Installed RHF, Zod, `@hookform/resolvers`, Vitest
- Added shadcn `Form` primitives (`src/components/ui/form.tsx`)
- Created `FormFloatingInput`, `FormFloatingSelect`, `FormCheckboxLabel`
- Created `src/schemas/primitives.ts`, `src/schemas/messages.ts`
- Vitest config + primitive schema tests

**Key files:** `src/components/form/*`, `src/schemas/primitives.ts`, `vitest.config.ts`

---

### Issue 02 — Trial Request (reference) ✅

**Completed:** 2026-06-14

- Migrated `HeroTrialForm` to RHF + `trialRequestSchema`
- OTP stays in local state (resend, cooldown, attempt limits unchanged)
- Replaced `alert()` with inline/root errors
- Added `src/schemas/trialRequest.ts` + tests
- Added OTP helper tests (`hero-trial-otp.test.ts`)

**Note:** Trial Request is a single-screen form step (not multi-step Continue); validation runs on submit via `zodResolver`.

**Follow-up fix:** Hero background zoom on error display — stabilized background container height in `Hero.tsx` (`h-[clamp(...)]` per breakpoint) so `object-cover` does not re-crop when form errors change content height.

---

### Issue 03 — Standalone password reset ✅

**Completed:** 2026-06-14

- Migrated `ResetPasswordPage` to RHF + `resetPasswordFormSchema`
- Cross-field password confirmation via Zod refine
- Missing token shows inline error with back link
- Success shows inline message, redirects to Sign-in after 1.5s

**Key files:** `src/schemas/resetPassword.ts`, `src/pages/auth/ResetPasswordPage.tsx`

---

### Issue 04 — Account Setup (single location) ✅

**Completed:** 2026-06-14

- Migrated `RegisterSinglePage` — 3-step wizard, one `useForm`
- Steps 1–2: `trigger(stepFields)` on Continue
- Step 3: `handleSubmit` on Complete Setup
- Token validation pre-fills email, fullName, restaurantName
- Password strength watches RHF password field

**Key files:** `src/schemas/accountSetupSingle.ts`, `src/pages/auth/RegisterSinglePage.tsx`

---

### Issue 05 — Account Setup (multi-location) ✅

**Completed:** 2026-06-14

- Migrated `RegisterMultiPage` — 4-step wizard, one `useForm`
- `useFieldArray` for dynamic locations
- Conditional offer field validation via `superRefine`
- Per-location required fields with row-level errors
- Payload maps to `CompleteSetupPayload`

**Key files:** `src/schemas/accountSetupMulti.ts`, `src/pages/auth/RegisterMultiPage.tsx`

---

### Issue 06 — Sign-in wizard ✅

**Completed:** 2026-06-14

- Migrated `LoginPage` — four `useForm` instances (login, forgot-email, reset-email, reset-password)
- Schemas: `signInCredentialsSchema`, `signInEmailSchema`, reuses `resetPasswordFormSchema`
- Admin direct route vs user OTP unchanged
- OTP patterns match Trial Request
- All `alert()` removed

**Key files:** `src/schemas/signIn.ts`, `src/pages/auth/LoginPage.tsx`

---

### Post-migration — Submit-first validation timing ✅

**Completed:** 2026-06-14

Replaced site-wide `mode: "onTouched"` with submit-first, then per-field revalidation:

| Layer | Mechanism | Applies to |
|-------|-----------|------------|
| RHF defaults | `mode: "onSubmit"`, `reValidateMode: "onChange"` | All forms after failed `handleSubmit` |
| Wizard bridge | `attemptedFields` + `WizardLiveValidationProvider` + wrapper `trigger()` | Account Setup Continue gates |

**Key files:**

- `src/lib/form.ts` — `defaultFormValidationOptions`, `addAttemptedFields`
- `src/components/form/WizardLiveValidationContext.tsx`
- Updated wrappers: `FormFloatingInput`, `FormFloatingSelect`, `FormCheckboxLabel` (`liveValidate` prop + context)

**Applied to:** `HeroTrialForm`, `ResetPasswordPage`, `LoginPage` (×4), `RegisterSinglePage`, `RegisterMultiPage`

---

## Code map

```
src/
├── lib/form.ts                          # Shared validation options + wizard helpers
├── schemas/
│   ├── messages.ts                      # Centralized validation messages
│   ├── primitives.ts                    # email, password, mobile, URL, OTP
│   ├── trialRequest.ts
│   ├── resetPassword.ts
│   ├── accountSetupSingle.ts
│   ├── accountSetupMulti.ts
│   └── signIn.ts
├── components/
│   ├── form/
│   │   ├── FormFloatingInput.tsx
│   │   ├── FormFloatingSelect.tsx
│   │   ├── FormCheckboxLabel.tsx
│   │   └── WizardLiveValidationContext.tsx
│   ├── home/HeroTrialForm.tsx
│   └── ui/form.tsx                      # shadcn Form bridge
└── pages/auth/
    ├── LoginPage.tsx
    ├── ResetPasswordPage.tsx
    ├── RegisterSinglePage.tsx
    └── RegisterMultiPage.tsx
```

---

## Validation behaviour by form

| Form | First validation | Per-field after failure |
|------|------------------|-------------------------|
| Trial Request | Submit (`handleSubmit`) | RHF `onChange` |
| Sign-in / Reset Password | Submit | RHF `onChange` |
| Account Setup steps 1–3 | Continue → `trigger()` | Context + wrapper `trigger` on change |
| Account Setup final step | Submit | RHF `onChange` for wrapped fields |

---

## Review notes (2026-06-14)

### Verdict

Ship-ready. Core wizard `trigger()` / `isSubmitted` gap solved cleanly without breaking standard forms.

### Working well

- Correct split: native RHF for `handleSubmit` flows, context for wizard gates
- All 8 `useForm` instances use `defaultFormValidationOptions`
- Field name parity in `attemptedFields` matches `name` props (including `locations.0.address`)
- Selects in wizards revalidate on `onValueChange` when live
- Typecheck + 62 tests pass

### Known gaps (acceptable, documented)

1. **Checkbox revalidation on submit-only forms** — `FormCheckboxLabel` manual-triggers only via wizard context. After failed submit on Trial Request, checking "agree" may not clear error until blur.
2. **Raw `FormField` usage** — `thankYouMessage` textarea, touchpoint/feedback checkboxes, `includeInRollout` toggle bypass wizard live validation. Final-submit flows get RHF `onBlur` for textarea; array toggles only revalidate on next submit.
3. **Redundant double-trigger** — After failed final submit in Account Setup, both RHF native and wrapper `trigger` may run on blur (harmless).
4. **`attemptedFields` never resets** — Fields stay live once a step fails, including after navigating back (intentional).
5. **Select vs input on standard forms** — After failed submit, selects revalidate on blur only, not on pick (wizards are better here).

---

## Future enhancements

Prioritized polish items identified during review. Not scheduled.

### P1 — Checkbox revalidation on submit-only forms

Trigger validation on `onCheckedChange` when `formState.isSubmitted` is true (not only via wizard context). Affects Trial Request terms checkbox and Account Setup step 1 after failed submit.

### P2 — `FormTextarea` wrapper *(deferred)*

Extract a thin wrapper with the same live-validation pattern for `thankYouMessage` and other raw textarea `FormField` usages in Account Setup. Deferred — wrappers cover high-traffic inputs.

### P3 — Select revalidation site-wide *(resolved)*

`reValidateMode: "onChange"` handles standard forms; wizard selects already trigger on `onValueChange`.

### P4 — Array field wrappers

Touchpoint and feedback tag checkboxes in `RegisterSinglePage` use raw `toggleArrayValue` without blur/change revalidation. Consider a `FormCheckboxGroup` or trigger on toggle when form has been submitted.

### P5 — Consolidate Sign-in / standalone reset duplication

Login page and `ResetPasswordPage` share reset logic. Document shared patterns or extract a shared hook/component if divergence grows.

### P6 — Server field-error mapping

Map API validation errors onto specific form fields when the response includes field-level detail (PRD user story 31). Partially done; extend where API supports it.

### P7 — Component tests (optional)

Add React Testing Library smoke tests for form wrappers if RTL is adopted project-wide.

### P8 — E2E (out of scope for initiative)

Playwright/Cypress coverage for critical onboarding paths.

---

## Commands

```bash
npm run typecheck   # TypeScript
npm test            # Vitest (schema + OTP helper tests)
npm run build       # Production build
```

---

## Conversation timeline

| Date | Milestone |
|------|-----------|
| 2026-06-14 | ADR + PRD approved; local issue tracking in `.scratch/` |
| 2026-06-14 | Issues 01–06 completed sequentially |
| 2026-06-14 | Hero background layout-shift fix on Trial Request errors |
| 2026-06-14 | Site-wide submit-first validation timing implemented |
| 2026-06-14 | Revalidation review completed; future enhancements logged |
| 2026-06-14 | Documentation consolidated into this file |
| 2026-06-14 | Validation timing: `reValidateMode: "onChange"` (errors clear on type, not blur) |
| 2026-06-14 | Cross-field pairs: password ↔ confirm revalidate together on change |
| 2026-06-14 | Post-submit mid-typing errors accepted (live update on every keystroke) |
| 2026-06-14 | Raw `FormField` usages deferred — wrappers cover high-traffic inputs |
| 2026-06-14 | New-form convention: documented in `form_function.md`, not enforced |

---

## Validation contract (canonical pattern)

Use this for new and migrated forms. Documented only — not lint-enforced.

| Rule | Implementation |
|------|----------------|
| No errors before first attempt | `defaultFormValidationOptions` (`mode: "onSubmit"`) |
| Errors clear on type after failure | `reValidateMode: "onChange"` |
| Wizard step gates | `trigger(stepFields)` on Continue; failed step → `addAttemptedFields` |
| Wizard live revalidation | `WizardLiveValidationProvider` + wrapper `trigger` on change |
| Password pairs | `getCrossFieldPeers` — revalidate sibling on change |
| Default inputs | `FormFloatingInput`, `FormFloatingSelect`, `FormCheckboxLabel` |
| Raw `FormField` | Acceptable for edge cases (textarea, custom toggles); not the default |
