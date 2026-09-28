# Tummly email templates (React Email)

Author HTML emails in React. Preview locally, then export static HTML for the .NET `EmailService` to fill and send.

## Commands (from repo root)

```bash
npm run email:dev      # preview at http://localhost:3333
npm run email:export   # render → backend/TummlyBackend/Assets/emails/templates/
```

## Templates

| File | Subject / purpose |
| --- | --- |
| `team-invitation.tsx` | Team invite |
| `otp.tsx` | Verification code |
| `contact-enquiry-received.tsx` | Help Centre contact confirmation |
| `new-device-sign-in.tsx` | New device sign-in alert |
| `reset-password.tsx` | Password reset link |
| `password-changed.tsx` | Password changed notice |
| `guest-response.tsx` | Guest response / Campaign / Offer unlocked (Figma). Optional `{{offer_block}}`. |
| `campaign.tsx` | Campaign preview (same chrome as guest-response) |
| `weekly-brief.tsx` | Weekly Brief ready |
| `trial-request-received.tsx` | Trial request received |
| `trial-decline.tsx` | Trial decline |
| `trial-more-info.tsx` | Trial more-info request |
| `billing-account-notice.tsx` | Billing account notice |
| `help-centre-support-reply.tsx` | Support reply to submitter |
| `help-centre-resolved.tsx` | Query resolved |
| `help-centre-new-query.tsx` | New query (internal) |
| `help-centre-escalation.tsx` | Escalation (internal) |
| `help-centre-operator-reply.tsx` | Operator reply (internal) |
| `pilot-*.tsx` / `payment-*.tsx` / `usage-*.tsx` | Pilot and billing notices |
| `shop-order-confirmed.tsx` | Shop order confirmed (Paid, incl. complimentary) |
| `shop-order-dispatched.tsx` | Shop order dispatched (Processing → InTransit) |

## Public chrome images

`npm run email:export` copies static PNGs to:

- `backend/TummlyBackend/Assets/emails/` (API serves `GET /email/{file}`)
- `public/email/` (frontend CDN when `Frontend:BaseUrl` hosts the SPA)

Email HTML prefers `PublicApi:BaseUrl` for image `src` (see `EmailAssets`). Loopback hosts auto-embed as CID so Resend smoke still shows logos.

## Layout

| Path | Role |
| --- | --- |
| `emails/*.tsx` | Templates (shown in preview) |
| `emails/_components/` | Shared tokens / chrome (ignored by preview sidebar) |
| `emails/static/` | Images served as `/static/...` in preview |
| `out/` | Local export (gitignored) |

## Backend wiring

1. Edit a `.tsx` template (token defaults like `{{workspace_name}}` are for export).
2. Run `npm run email:export`.
3. C# loads the HTML under `Assets/emails/templates/` and replaces tokens at send time.

Logo URLs use `{Frontend:BaseUrl}/email/tummly-logo-dark.png` (Gmail strips `data:` images).

After template edits, always re-export before relying on sent mail.
