# Product documentation changelog

Reverse chronological record of behaviour and documentation status changes.

## 2026-08-28 — Billing pack v3.0 in repo

### Added

- `docs/product/billing-pack-v3.0/` — copy of the approved UK pricing and billing pack (sign-off, completed questionnaire, pricebook JSON, audit, workbook, handoff, manifest)

### Updated

- `docs/product/channel-credits-questionnaire.md` — points at the completed pack; empty answer boxes removed
- `docs/product/README.md` — index rows for the pack and the questionnaire status

## 2026-08-06 — Channel credits / cost analysis (pre-Campaigns)

### Added

- `docs/product/channel-credits-questionnaire.md` — product questionnaire for Pilot/Starter/Growth/Group credits, Soft lock, burn rates, allowances
- `docs/product/channel-cost-analysis.md` — Resend / Twilio UK / Azure OpenAI unit COGS and volume scenarios (no sell-price recommendations)

### Domain

- Glossary updates in root `CONTEXT.md`: Subscription plan tiers, credit pools, Soft lock direction, Activation fulfilment pack

## 2026-07-01 — Codebase audit (docs/product)

**Product version:** 2026.07.01 (unchanged — documentation corrections only)

### Corrected against codebase

- **admin.md:** Approve leaves `APPROVED` (not `INVITE_SENT`); decline/more-info use `PUT /api/admin/update-status`; manual resend uses reminder template; drawer section names; stored vs normalized status values; QA purge dual gate; activation download is SVG
- **trial-request.md:** API path prefix; OTP resend limit (5) and exact cooldown message
- **sign-in.md:** Account lock (5 attempts); workspace selection dormant reason; `/auth/me` operator-only; admin lock gap on universal-login
- **activation-and-fulfilment.md:** Activation verify lockout; hash retained; calendar-day period; pending activation without code; workspace paths whitelisted but unimplemented
- **operator-setup.md:** `ApprovalToken` validation; `/setup-account` router entry
- **guest-feedback.md:** Client-only contact validation; Terms + Privacy on form
- **marketing-site.md:** PublicOnlyRoute partial behaviour; cookie preferences on `/cookie-settings`; register routes in sitemap
- **analytics.md:** GA requires env measurement ID
- **security-and-rbac.md:** Lock policy shipped; JWT `Owner` vs client `USER`; RoleRoute redirect; AspNetCoreRateLimit not wired

## 2026-07-01 — Initial Batch 1

**Product version:** 2026.07.01

### Added

- `docs/product/README.md` — index, status legend, lifecycle diagram
- `docs/product/trial-request.md` — Trial Request flow (Shipped)
- `docs/product/admin.md` — admin dashboard and review actions (Shipped)
- `docs/product/operator-setup.md` — single/multi setup and provisioning (Shipped / Planned split)
- `docs/product/sign-in.md` — Sign-in, OTP, password reset (Shipped)
- `docs/product/activation-and-fulfilment.md` — activation code and fulfilment (Shipped / Operational / Planned)

### Documented as Planned (not Shipped)

- Setup complete / welcome email after Operator Setup
- Per-location Starter QR materials generation and shipment
- In-app fulfilment status and delivery tracking
- QR reorder workflow
- Self-print PDF packs for operators

### Legacy

- Marked `sign_in_flows.md` and `guest-loop-audit.md` as superseded by product docs (headers added)

## 2026-07-01 — Batch 2

**Product version:** 2026.07.01 (unchanged — documentation only)

### Added

- `docs/product/marketing-site.md` — section walkthrough + Claims register (Hard blockers flagged)
- `docs/product/guest-feedback.md` — guest form, thank-you, Planned offers/opt-in
- `docs/product/analytics.md` — Shipped `page_view` + Target event map
- `docs/product/security-and-rbac.md` — permissions matrix, Planned audit log
- `docs/product/support-playbooks.md` — topic index; SOPs marked TBD

### Updated

- `docs/product/README.md` — full index, expanded status summary with marketing blockers
