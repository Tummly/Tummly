# Auth left-column screens (password + verify email)

Date: 2026-09-17  
Status: draft for review

## Goal

Restyle the left column of auth screens that sit under the existing `AuthShell` (hero and shell chrome stay as they are). Match Sign-in typography, spacing, and flat layout (no bordered card). Add a signup **Verify your email** screen on `AuthShell` and hand the guided trial off to it. Keep sign-in unknown-device OTP unchanged.

## Figma references

| Screen | Node |
|---|---|
| Verify your email | `4974:22152` |
| Reset your password | `4974:22182` |
| Check your inbox | `4974:22235` |
| Create a new password | `4974:22287` |
| Password updated | `4974:22333` |

File: [Tummly — Marketing Website](https://www.figma.com/design/UP1DqyGGGrxx80Dp7jReTT/Tummly---Marketing-Website)

## Scope

### In

- Restyle left content for the four password steps.
- Add `/verify-email` with Figma copy and actions.
- After successful trial form submit, navigate to `/verify-email` with the email.
- Keep trial OTP verify/resend APIs working (OTP entry on Verify or a tight follow-on).
- Leave sign-in unknown-device OTP alone.

### Out

- Changes to `AuthShell`, `AuthHeroPanel`, brush, or footer chrome (already restyled).
- Backend magic-link / new verify-email send path.
- Changes to sign-in OTP challenge UI or APIs.

## Current product context

- **Sign-in OTP:** unknown device → stay on login flow (`SignInVerifyOtpStep`). Do not move or replace.
- **Signup today:** guided trial on `/` (`HeroTrialForm`) → inline 6-digit OTP (`HeroTrialOtpStep`) → success. There is no classic self-serve register + magic-link verify page.
- Figma **Verify your email** is a resend / different-account layout. Product still needs OTP digits for the current trial APIs, so Verify must keep an OTP entry path.

## Architecture

Reuse `AuthShell` for all screens. Only replace left-column children.

```text
AuthShell
├── left: MarketingLogo + step content (this work)
├── AuthFooter (unchanged)
└── AuthHeroPanel (unchanged)
```

### Password screens

| Screen | Component | Behaviour |
|---|---|---|
| Reset your password | `ForgotPasswordRequestStep` | Work email, Send reset link, ← Back to login |
| Check your inbox | `ForgotPasswordEmailSentStep` | Soft copy with email, Didn’t get it? / Send another link, ← Back to login |
| Create a new password | `ResetPasswordCreateStep` | New / Confirm password, Reset password |
| Password updated | `ResetPasswordSuccessStep` | Go to login link |

Remove bordered card + card shadow. Match `SignInForm` patterns: serif heading, body text, `FormFloatingInput`, green primary `Button`, divider + secondary links where Figma shows them.

Keep existing page wiring and APIs (`ForgotPasswordPage`, `ResetPasswordPage`, reset schemas, submit helpers).

### Verify your email

- New route: `/verify-email`.
- New left-column UI (e.g. `VerifyEmailStep` / `VerifyEmailPage`).
- Copy: title, body with email, **Resend verification email**, **Use a different account**.
- OTP: collect 6-digit code on this page (or immediate follow-on) using existing trial verify/resend helpers.
- Email source: query param and/or navigation state from trial submit. If missing → redirect to `/` trial form.

### Trial handoff

1. User submits trial form successfully.
2. App navigates to `/verify-email` with email.
3. Resend → existing trial `resendOtpRequest`.
4. Verify OTP → existing `verifyOtpRequest`; on success → existing trial success path.
5. Use a different account → clear OTP state, return to `/` trial form.

Do not change unknown-device sign-in OTP.

## Errors and edge cases

**Password**

- Keep current field/root error handling.
- Invalid/missing reset token: keep existing create-password error path; restyle only.
- Check-your-inbox always uses soft “if an account exists…” copy; resend uses the same request API.

**Verify / trial**

- No email → redirect to `/`.
- Resend/verify failures → same OTP feedback mapping as `HeroTrialOtpStep`.
- Too many attempts → existing block message.
- Already verified → trial success (same as today).
- Different account → clear OTP state, back to trial form.

## Testing

- Manual desktop check of all five left columns against Figma (copy, spacing, links, buttons).
- Forgot-password: request → inbox → (optional) resend → back to login.
- Reset-password: create → success → go to login; invalid token still shows error.
- Trial: submit → land on `/verify-email` → resend → verify OTP → success; different account returns to form; missing email redirects to `/`.
- Sign-in unknown-device OTP still works on login (no regression).

## Success criteria

- Left columns match Figma layout language used on Sign-in (no cards).
- Password flows keep working with existing APIs.
- Trial email verify runs through AuthShell Verify screen with OTP still functional.
- Sign-in OTP unchanged.
