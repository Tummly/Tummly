# Support readiness

Index of support topics and system capabilities. **Full playbooks are TBD** — this file tracks what the product can do today vs what requires manual process.

## Status summary

| Area | Status |
|------|--------|
| Topic index | Shipped (this file) |
| Detailed SOPs / scripts | TBD |
| In-app support portal | Planned |
| Ticketing integration | Planned |

## What support can do in the product today

| Action | Where | Doc reference |
|--------|-------|---------------|
| Approve / decline / request more info | Admin dashboard | [admin.md](./admin.md) |
| Resend Operator Setup invitation | Admin dashboard | [admin.md](./admin.md) |
| View Operator details | Admin drawer | [admin.md](./admin.md) |
| Copy / download Activation Code asset | Operator details | [activation-and-fulfilment.md](./activation-and-fulfilment.md) |
| Extend activation period | Operator details | [admin.md](./admin.md) |
| Purge trial data (QA only) | Admin delete | [admin.md](./admin.md) |

Operators copy the **Smart Guest Link** and preview the guest form from Home; they do **not** download QR PNGs. Physical stickers are obtained via the **Tummly Shop** (fulfillment later). Capture sidenav is a coming-soon stub.

---

## Support topics (TBD)

Each topic needs: trigger, owner role, manual steps, system actions, escalation, Planned automation.

| # | Topic | System today | Status |
|---|-------|--------------|--------|
| 1 | **Setup issues** — invite not received, expired link, blank setup page | Resend invite; check `InviteExpiresAt`; ErrorBoundary on setup routes | TBD |
| 2 | **Email delivery** — OTP, invite, transactional not arriving | Resend endpoints; check Resend/Twilio logs | TBD |
| 3 | **Activation fulfilment** — pack not received, wrong address | Admin download asset; no tracking in app | TBD |
| 4 | **Activation code** — lost code, code does not work | Admin copy from drawer; single-use activation | TBD |
| 5 | **Sign-in / OTP** — cannot sign in, SMS not working | Email OTP default; SMS requires phone on file | TBD |
| 6 | **Activation expired** | Admin extend activation | TBD |
| 7 | **Password reset** | Self-service `/forgot-password` (operators only — no admin path) | TBD |
| 8 | **QR replacement** — damaged sticker, wrong venue | Tummly Shop / Starter QR materials (planned); token rotation planned | TBD |
| 9 | **Privacy requests** — access, deletion, opt-out | No in-app workflow; manual DB/process | TBD |
| 10 | **Billing / credits / plan changes** | No billing in product | TBD — N/A for v1 |
| 11 | **Account problems** — locked, wrong email, duplicate trial | Limited admin tools; trial purge QA-only | TBD |
| 12 | **Starter QR reorders** | Not in app; marketing mentions future reorders / Shop | TBD |
| 13 | **Marketing claim disputes** | See [marketing-site.md](./marketing-site.md) Claims register | TBD |

---

## Escalation placeholders

| Severity | Examples | TBD |
|----------|----------|-----|
| P1 — cannot operate | Activation expired during service, mass email outage | On-call contact, SLA |
| P2 — degraded | Single operator cannot sign in | Resolution time |
| P3 — informational | How to get / replace QR stickers | Help centre content |

---

## Related documentation

| Need | Document |
|------|----------|
| Full operator lifecycle | [README.md](./README.md) lifecycle diagram |
| Admin actions | [admin.md](./admin.md) |
| Sign-in troubleshooting | [sign-in.md](./sign-in.md) |
| Fulfilment | [activation-and-fulfilment.md](./activation-and-fulfilment.md) |
| Security / access | [security-and-rbac.md](./security-and-rbac.md) |

## Not yet live

| Item | Status |
|------|--------|
| Support playbooks (full prose) | TBD per stakeholder decision |
| Help Centre URLs linked from activation screen | Partial — links in UI; content TBD |
| Status page | Planned |
