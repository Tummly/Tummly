# Auth left-column screens — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Restyle the left column of password auth steps to match Sign-in / Figma (no cards), add `/verify-email` for trial signup verify with OTP, and hand the guided trial off to that page — without changing `AuthShell` hero chrome or sign-in unknown-device OTP.

**Architecture:** Keep `AuthShell` + `AuthHeroPanel` + `AuthFooter` as-is. Rewrite left-column step components to mirror `SignInForm` (MarketingLogo, serif title, flat layout). Add `VerifyEmailPage` that reuses trial `verifyOtpRequest` / `resendOtpRequest`. After trial submit, navigate to `/verify-email?email=…` instead of rendering `HeroTrialOtpStep` inline.

**Tech Stack:** React, React Router, react-hook-form, Zod, Vitest, existing shadcn `Button` / `FormFloatingInput` / `InputOTP`, Lucide icons, Tailwind.

**Spec:** [docs/superpowers/specs/2026-09-17-auth-left-column-screens-design.md](../specs/2026-09-17-auth-left-column-screens-design.md)

## Global Constraints

- Do **not** edit `AuthShell.tsx`, `AuthHeroPanel.tsx`, brush assets, or `AuthFooter.tsx` for this plan (already restyled).
- Do **not** change sign-in OTP (`SignInVerifyOtpStep`, login OTP APIs).
- No backend magic-link / new verify-email send path — reuse trial OTP APIs.
- Left column must match Sign-in patterns: no bordered card, no `cardShadow`.
- Prefer Lucide + existing UI primitives over one-off Figma SVG exports.
- Commit only when the human asks to commit.
- Report to the human in ASD-STE100 Simplified Technical English.

---

## File map

| File | Role |
|---|---|
| `src/components/auth/AuthFormHeader.tsx` | Shared logo + title + optional body for flat auth left columns |
| `src/components/auth/AuthBackToLogin.tsx` | ← Back to login row (Lucide `ArrowLeft` + link) |
| `src/components/auth/ForgotPasswordRequestStep.tsx` | Restyle: Reset your password |
| `src/components/auth/ForgotPasswordEmailSentStep.tsx` | Restyle: Check your inbox + resend |
| `src/pages/auth/ForgotPasswordPage.tsx` | Pass email + resend handler into sent step |
| `src/components/auth/ResetPasswordCreateStep.tsx` | Restyle: Create a new password |
| `src/components/auth/ResetPasswordSuccessStep.tsx` | Restyle: Password updated |
| `src/pages/auth/ResetPasswordPage.tsx` | Restyle invalid-token left column |
| `src/components/auth/VerifyEmailStep.tsx` | Figma Verify UI + OTP entry |
| `src/pages/auth/VerifyEmailPage.tsx` | Route page: email gate, OTP state, success handoff |
| `src/pages/routes/AppRoutes.tsx` | Register `/verify-email` |
| `src/components/home/HeroTrialForm.tsx` | Navigate to `/verify-email` after submit; drop inline OTP step |
| `src/lib/verifyEmailFlow.ts` | Parse email from search/state; build verify URL |
| `src/lib/verifyEmailFlow.test.ts` | Unit tests for email parse / URL helper |

---

### Task 1: Shared left-column chrome

**Files:**
- Create: `src/components/auth/AuthFormHeader.tsx`
- Create: `src/components/auth/AuthBackToLogin.tsx`

**Interfaces:**
- Produces:
  - `AuthFormHeader({ title: string; description?: ReactNode; descriptionClassName?: string })`
  - `AuthBackToLogin({ to?: string })` default `to="/login"`, label `Back to login`

- [ ] **Step 1: Add `AuthFormHeader`**

Match `SignInForm` logo + heading stack:

```tsx
import { Link } from "react-router-dom"
import type { ReactNode } from "react"

import { MarketingLogo } from "@/components/marketing/MarketingLogo"
import { cn } from "@/lib/utils"

type AuthFormHeaderProps = {
  title: string
  description?: ReactNode
  descriptionClassName?: string
}

export function AuthFormHeader({
  title,
  description,
  descriptionClassName,
}: AuthFormHeaderProps) {
  return (
    <div className="flex w-full flex-col gap-10">
      <Link
        to="/"
        className="inline-flex w-fit shrink-0 rounded-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary/30"
      >
        <MarketingLogo
          onLight
          width={145}
          height={37}
          className="h-[37px] w-auto"
        />
      </Link>

      <div className="flex flex-col gap-3 text-[#141414]">
        <h1 className="m-0 font-serif text-[clamp(1.75rem,4vw,2.25rem)] font-medium leading-normal">
          {title}
        </h1>
        {description ? (
          <p
            className={cn(
              "m-0 text-base leading-[22px]",
              descriptionClassName
            )}
          >
            {description}
          </p>
        ) : null}
      </div>
    </div>
  )
}
```

- [ ] **Step 2: Add `AuthBackToLogin`**

```tsx
import { ArrowLeft } from "lucide-react"
import { Link } from "react-router-dom"

import { Button } from "@/components/ui/button"

type AuthBackToLoginProps = {
  to?: string
}

export function AuthBackToLogin({ to = "/login" }: AuthBackToLoginProps) {
  return (
    <Button
      type="button"
      variant="link"
      size="link-sm"
      asChild
      className="h-auto gap-3 self-start p-0 text-sm font-medium text-[#555] no-underline hover:no-underline"
    >
      <Link to={to}>
        <ArrowLeft className="size-3.5 shrink-0" aria-hidden />
        Back to login
      </Link>
    </Button>
  )
}
```

- [ ] **Step 3: Smoke-check imports compile**

Run: `npx tsc -p tsconfig.app.json --noEmit` (or project’s usual typecheck).  
Expected: no errors from the new files.

- [ ] **Step 4: Commit** (only when human asks)

---

### Task 2: Restyle Reset your password + Check your inbox

**Files:**
- Modify: `src/components/auth/ForgotPasswordRequestStep.tsx`
- Modify: `src/components/auth/ForgotPasswordEmailSentStep.tsx`
- Modify: `src/pages/auth/ForgotPasswordPage.tsx`

**Interfaces:**
- Consumes: `AuthFormHeader`, `AuthBackToLogin`
- Produces:
  - `ForgotPasswordEmailSentStep({ email: string; onResend: () => Promise<void>; resending?: boolean })`
  - `ForgotPasswordPage` stores submitted email and passes it to the sent step; resend calls `requestPasswordReset` again

- [ ] **Step 1: Rewrite `ForgotPasswordRequestStep` (flat layout)**

Remove card border/shadow. Structure:

```tsx
<div className="flex w-full flex-col gap-10">
  <AuthFormHeader
    title="Reset your password"
    description="Enter the email address for your Tummly account. If an account matches, we'll send instructions to reset your password."
  />
  <Form {...form}>
    <form className="flex flex-col gap-9" ...>
      <FormFloatingInput label="Work email" ... />
      <FieldErrorSlot error={rootError} />
      <Button className="h-[45px] min-h-[45px] w-full rounded-[4px] bg-[#14a74a] ...">
        Send reset link
      </Button>
      <div className="h-px w-full bg-[#d2d2d2]" />
      <AuthBackToLogin />
    </form>
  </Form>
</div>
```

Keep existing `useEffect` that clears root errors on email change. Keep `form` / `onSubmit` props.

- [ ] **Step 2: Rewrite `ForgotPasswordEmailSentStep`**

Figma copy (full email, not masked):

- Title: `Check your inbox`
- Body: `If a Tummly account exists for {email}, we've sent a password reset link.`
- Row: `Didn't get it?` + green underlined `Send another link` button calling `onResend`
- Divider + `AuthBackToLogin`

- [ ] **Step 3: Wire `ForgotPasswordPage`**

```tsx
const [sentEmail, setSentEmail] = useState("")

const onSubmit = async (values: SignInEmailValues) => {
  // existing try/catch
  const email = await requestPasswordReset(values) // already returns payload.email
  setSentEmail(email)
  setStep(FORGOT_PASSWORD_STEPS.EMAIL_SENT)
}

const onResend = async () => {
  await requestPasswordReset({ email: sentEmail })
}

// EMAIL_SENT:
<ForgotPasswordEmailSentStep
  email={sentEmail}
  onResend={onResend}
/>
```

Handle resend errors with local state or a small toast-free inline status on the sent step if needed (prefer inline status text).

- [ ] **Step 4: Manual check**

Open `/forgot-password` → submit → confirm Check your inbox shows email and Back to login.  
Expected: no card chrome; matches Sign-in left column language.

- [ ] **Step 5: Commit** (only when human asks)

---

### Task 3: Restyle Create password + Password updated + invalid token

**Files:**
- Modify: `src/components/auth/ResetPasswordCreateStep.tsx`
- Modify: `src/components/auth/ResetPasswordSuccessStep.tsx`
- Modify: `src/pages/auth/ResetPasswordPage.tsx`

**Interfaces:**
- Consumes: `AuthFormHeader`
- Keep: `form` / `onSubmit` props; `PasswordStrengthMeter` may remain below New password (product safety); labels become `New password` / `Confirm new password`; primary CTA `Reset password`

- [ ] **Step 1: Rewrite `ResetPasswordCreateStep`**

Flat layout with `AuthFormHeader`:

- Title: `Create a new password`
- Body: `Create a new password for your Tummly account.`
- Fields: `New password`, `Confirm new password` (keep password type + strength meter)
- Button: `Reset password` (loading: `Please wait...`)
- Keep root `FieldErrorSlot` and confirm-password trigger `useEffect`

- [ ] **Step 2: Rewrite `ResetPasswordSuccessStep`**

```tsx
<div className="flex w-full flex-col gap-10">
  <AuthFormHeader
    title="Password updated"
    description="Your Tummly account password has been changed."
  />
  <Button variant="link" size="link-sm" asChild className="self-start font-medium text-primary underline underline-offset-2">
    <Link to="/login">Go to login</Link>
  </Button>
</div>
```

No full-width green button (Figma is link-only).

- [ ] **Step 3: Restyle invalid-token block in `ResetPasswordPage`**

Replace `cardClassName` block with flat `AuthFormHeader` + body + links (`Request a new reset link`, `Back to login` / `AuthBackToLogin`). Delete unused card class constants.

- [ ] **Step 4: Manual check**

- `/reset-password` (no token) → invalid link flat UI  
- `/reset-password?token=…` → create form flat UI  
Expected: no cards; success shows Go to login link

- [ ] **Step 5: Commit** (only when human asks)

---

### Task 4: Verify-email helpers + route page shell

**Files:**
- Create: `src/lib/verifyEmailFlow.ts`
- Create: `src/lib/verifyEmailFlow.test.ts`
- Create: `src/pages/auth/VerifyEmailPage.tsx`
- Create: `src/components/auth/VerifyEmailStep.tsx`
- Modify: `src/pages/routes/AppRoutes.tsx`

**Interfaces:**
- Produces:
  - `buildVerifyEmailPath(email: string): string` → `/verify-email?email=${encodeURIComponent(email)}`
  - `readVerifyEmail(searchParams: URLSearchParams, stateEmail?: string | null): string | null`
  - `VerifyEmailStep` props: email, otpCode, submitting, feedback, resendSecondsRemaining, canResend, onOtpChange, onVerify, onResend, onUseDifferentAccount
  - Route path: `verify-email`

- [ ] **Step 1: Write failing tests for helpers**

```ts
import { describe, expect, it } from "vitest"
import { buildVerifyEmailPath, readVerifyEmail } from "./verifyEmailFlow"

describe("verifyEmailFlow", () => {
  it("builds a path with encoded email", () => {
    expect(buildVerifyEmailPath("Mehmet@Example.com")).toBe(
      "/verify-email?email=Mehmet%40Example.com"
    )
  })

  it("prefers query email then state email", () => {
    const params = new URLSearchParams("email=a%40b.com")
    expect(readVerifyEmail(params, "c@d.com")).toBe("a@b.com")
    expect(readVerifyEmail(new URLSearchParams(), "c@d.com")).toBe("c@d.com")
    expect(readVerifyEmail(new URLSearchParams(), null)).toBeNull()
  })
})
```

- [ ] **Step 2: Run tests — expect fail**

Run: `npx vitest run src/lib/verifyEmailFlow.test.ts`  
Expected: FAIL (module missing)

- [ ] **Step 3: Implement helpers**

```ts
export function buildVerifyEmailPath(email: string) {
  const trimmed = email.trim()
  return `/verify-email?email=${encodeURIComponent(trimmed)}`
}

export function readVerifyEmail(
  searchParams: URLSearchParams,
  stateEmail?: string | null
): string | null {
  const fromQuery = searchParams.get("email")?.trim()
  if (fromQuery) return fromQuery.toLowerCase()
  const fromState = stateEmail?.trim()
  if (fromState) return fromState.toLowerCase()
  return null
}
```

- [ ] **Step 4: Re-run tests — expect pass**

- [ ] **Step 5: Implement `VerifyEmailStep`**

Figma chrome + OTP (required for trial APIs):

- `AuthFormHeader` title `Verify your email`
- Description: `Check your inbox for the verification email we sent to {email}.`
- Primary button: `Resend verification email` → `onResend` (disabled while submitting / cooldown; show cooldown text if needed)
- Divider
- Green underlined `Use a different account` → `onUseDifferentAccount`
- Below Figma block (same column): OTP input + Verify submit using the same `InputOTP` pattern as `HeroTrialOtpStep` / `SignInVerifyOtpStep`, wired to `onVerify` / `onOtpChange` / `feedback`

Use auth button sizing from Sign-in (`h-[45px]`, `rounded-[4px]`, `#14a74a`), not the pill trial button.

- [ ] **Step 6: Implement `VerifyEmailPage`**

```tsx
// Pseudocode structure
function VerifyEmailPage() {
  const [searchParams] = useSearchParams()
  const location = useLocation()
  const navigate = useNavigate()
  const email = readVerifyEmail(
    searchParams,
    (location.state as { email?: string } | null)?.email
  )

  useEffect(() => {
    if (!email) navigate("/", { replace: true })
  }, [email, navigate])

  // otp state, countdown, verify/resend handlers — port from HeroTrialForm OTP branch
  // on success: navigate("/", { state: { trialVerified: true, confirmationEmailSent } })
  // OR render a small success panel — prefer navigate to `/` with state that Hero can show success;
  // simplest reliable path: keep success UI on VerifyEmailPage after verify (reuse HeroTrialSuccessStep content adapted), OR navigate home.

  // Spec: success → existing trial success path.
  // Implement: after verify, set local step "success" and render HeroTrialSuccessStep inside AuthShell
  // with onReturnToTummly → navigate("/"), onSubmitAgain → navigate("/")
}
```

Port verify/resend attempt limits and feedback mapping from `HeroTrialForm` (`mapVerifyApiMessage`, `mapResendApiMessage`, `MAX_VERIFY_ATTEMPTS`, `RESEND_COOLDOWN_SECONDS`, `isAlreadyVerifiedFeedback`).

- [ ] **Step 7: Register route in `AppRoutes.tsx`**

Next to other auth routes:

```tsx
<Route
  path="verify-email"
  element={
    <ErrorBoundary>
      <VerifyEmailPage />
    </ErrorBoundary>
  }
/>
```

- [ ] **Step 8: Manual check**

Open `/verify-email` with no email → redirects to `/`.  
Open `/verify-email?email=test@example.com` → Verify UI + OTP.  
Expected: AuthShell chrome unchanged; left column flat.

- [ ] **Step 9: Commit** (only when human asks)

---

### Task 5: Trial handoff from `HeroTrialForm`

**Files:**
- Modify: `src/components/home/HeroTrialForm.tsx`

**Interfaces:**
- Consumes: `buildVerifyEmailPath` from `src/lib/verifyEmailFlow.ts`
- After successful `submitTrialRequest`, navigate instead of `setStep("otp")`
- Remove (or leave unused) inline `otp` step render; prefer delete dead OTP branch to avoid two UIs

- [ ] **Step 1: Change successful submit to navigate**

```tsx
import { useNavigate } from "react-router-dom"
import { buildVerifyEmailPath } from "@/lib/verifyEmailFlow"

// inside onSubmitTrialRequest success:
navigate(buildVerifyEmailPath(payload.email), {
  state: { email: payload.email },
})
```

Do **not** call `setStep("otp")` on success.

- [ ] **Step 2: Handle `already_verified`**

Keep existing `setStep("success")` for already_verified on submit (no navigate).

- [ ] **Step 3: Remove dead OTP UI branch**

Delete `step === "otp"` render and OTP-only state/handlers that are unused after handoff (`otpCode`, resend timer tied only to otp step, `HeroTrialOtpStep` import). Keep success step for `already_verified` and any return path that still needs it.

If removing OTP state breaks success-only flow, keep minimal state. Prefer a clean form | success union:

```ts
const [step, setStep] = useState<"form" | "success">("form")
```

- [ ] **Step 4: Manual check**

Submit trial form (or mock) → lands on `/verify-email?email=…`.  
Use a different account → returns to `/` form.  
Verify OTP → success UI.  
Sign-in unknown-device OTP on `/login` still works.

- [ ] **Step 5: Commit** (only when human asks)

---

### Task 6: G5 / visual fidelity pass

**Files:** none required unless fixes found  
**Checks:** all five left columns vs Figma nodes in the spec

- [ ] **Step 1: Desktop screenshots / live compare**

Routes:

1. `/verify-email?email=mehmet@example.com`
2. `/forgot-password`
3. `/forgot-password` after submit (Check your inbox)
4. `/reset-password?token=test` (Create)
5. success / invalid-token states on reset

- [ ] **Step 2: Confirm**

- Logo present on each left column  
- No card border/shadow  
- Primary green `#14a74a` buttons where Figma shows them  
- Back to login uses arrow + grey text  
- Hero panel unchanged  

- [ ] **Step 3: Fix any left-column gaps found**

- [ ] **Step 4: Commit** (only when human asks)

---

## Spec coverage checklist

| Spec requirement | Task |
|---|---|
| Restyle Reset your password | 2 |
| Restyle Check your inbox + resend | 2 |
| Restyle Create a new password | 3 |
| Restyle Password updated | 3 |
| Restyle invalid reset token (flat) | 3 |
| `/verify-email` + Figma actions | 4 |
| OTP still works on Verify | 4 |
| Missing email → `/` | 4 |
| Trial submit → Verify handoff | 5 |
| Different account → trial form | 4–5 |
| Sign-in OTP unchanged | Global + 5 check |
| AuthShell / hero untouched | Global |
| G5 visual check | 6 |

## Self-review notes

- No placeholders left in tasks.
- Helper names (`buildVerifyEmailPath`, `readVerifyEmail`) are consistent across Tasks 4–5.
- Figma Verify has no OTP field; plan keeps OTP on the same page under the Figma chrome so trial APIs stay usable without a backend change.
