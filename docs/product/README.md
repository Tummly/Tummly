# Tummly product documentation

**Product version:** 2026.07.01  
**Last reviewed:** 2026-09-15 (Self-service Pilot docs lock)  
**Audience:** Managers, AI agents, onboarding engineers

Tummly is a restaurant guest-relationship platform. Operators capture feedback, manage offers, and run campaigns across single or multi-location hospitality businesses.

This documentation set describes **what the product does**, **what is shipped vs planned**, and **how flows connect** across frontend, backend, and operations. Definitions are inlined — do not depend on external glossary files. Canonical domain language lives in root `CONTEXT.md`.

## How to read these docs

| Badge | Meaning |
|-------|---------|
| **Shipped** | Live in the current release; behaviour verified against codebase |
| **Partial** | Some paths work; gaps or stubs documented |
| **Planned** | Specified target; not implemented or not fully wired |
| **Operational (manual)** | Process outside the app (support, fulfilment, legal) |

Each feature block includes: user flow, states, backend actions, edge cases, screens, emails, compliance notes, and launch blockers where relevant.

**Engineers:** Product docs are canonical for behaviour truth. Screen-level Figma parity and audit history live in legacy implementation notes (see [Legacy documentation](#legacy-documentation)).

## Document index

| Document | Scope | Batch |
|----------|-------|-------|
| [self-service-pilot.md](./self-service-pilot.md) | Public Self-service Pilot path, Email verification, Guest Loop onboarding | 1 |
| [marketing-site.md](./marketing-site.md) | Public pages, CTAs, claims register | 2 |
| [operator-setup.md](./operator-setup.md) | Demo/sales Operator Setup (invite after Trial review), provisioning | 1 |
| [team.md](./team.md) | Team invitation send / accept / Resend / Revoke | 1 |
| [admin.md](./admin.md) | Admin dashboard, demo/sales trial review, activation admin | 1 |
| [sign-in.md](./sign-in.md) | Sign-in, OTP, password reset, trusted device, shared Auth chrome | 1 |
| [activation-and-fulfilment.md](./activation-and-fulfilment.md) | Activation code, Activation period, starter kit, fulfilment | 1 |
| [guest-feedback.md](./guest-feedback.md) | Guest capture form, thank-you, offers | 2 |
| [analytics.md](./analytics.md) | Shipped page views + Target event map | 2 |
| [cookie-storage-inventory.md](./cookie-storage-inventory.md) | First-party + GA storage keys, consent categories | Launch evidence |
| [security-and-rbac.md](./security-and-rbac.md) | Roles, isolation, sessions, audit gaps | 2 |
| [support-playbooks.md](./support-playbooks.md) | Support topic index (SOPs TBD) | 2 |
| [channel-credits-questionnaire.md](./channel-credits-questionnaire.md) | Points at pack v3.0 completed answers (not a commercial source) | Completed |
| [billing-pack-v3.0/](./billing-pack-v3.0/) | Approved UK pricing and billing pack; pricebook JSON `tummly_uk_billing_config_v3.0.json` | Approved |
| [channel-cost-analysis.md](./channel-cost-analysis.md) | Resend / Twilio / Azure COGS for Email, SMS, AI (pre-Campaigns) | Planned |

## Status summary

| Domain | Shipped | Partial | Planned | Hard launch blockers |
|--------|---------|---------|---------|----------------------|
| Self-service Pilot | — | — | Full public path (marketing → onboarding → provisioning) | Soft until build map |
| Demo/sales Trial Request + admin review | Form, OTP, received email, approve/decline/more info, resend, extend activation | — | Audit log | Fog: keep vs archive review docs |
| Operator Setup (demo/sales) | Wizard, provisioning, three default QR codes per location | Bulk upload UX | Per-location starter QR packs / Shop fulfillment | None |
| Team invitation | — | — | Accept path + Invitations UI | Soft until Team & permissions build |
| Sign-in | Password, OTP, trusted device, reset | SMS OTP; workspace APIs | Social (Google + Microsoft) on marketing chrome | None |
| Activation | Code generation, activation gate, 30-day period | — | Welcome email, in-app fulfilment tracking | Fulfilment is operational |
| Guest feedback | 3-field form, thank-you | Operator inbox basic | Opt-in, offers, tags | None |
| Marketing site | All sections live | Claims vs product; CTAs still Trial-shaped in code | Self-service Pilot chrome | **Starter QR, guest list, offers/campaigns copy** — see [marketing-site.md](./marketing-site.md) |
| Analytics | Page views + consent | — | Custom events, Self-service funnels | None |
| Security | JWT, roles, isolation, activation gate, account lock (5 attempts) | Admin lock on universal-login | Audit log; Self-service anonymous surfaces | Audit only if contract requires |
| Support | Admin actions list | — | Playbooks, ticketing | None |

See [CHANGELOG.md](./CHANGELOG.md) for version history.

## End-to-end lifecycle

### Public path (Self-service Pilot)

```mermaid
flowchart LR
    HOME[Home / Pricing] --> VERIFY[Email verification]
    VERIFY --> ONB[Guest Loop onboarding]
    ONB --> PLAN[Plan choice]
    PLAN -->|Pilot| PROV[Guest Loop provisioning]
    PROV --> SI[Sign-in]
    SI --> ACT[Account activation]
    ACT --> DASH[Operator dashboard]

    PROV -.->|Phase 3| CODE[Activation Code]
    CODE -.->|Operational| FUL[Fulfilment to venues]
```

### Demo / sales path (retained)

```mermaid
flowchart LR
    TR[Trial Request] --> REV[Admin review]
    REV -->|Approve| INV[Setup invitation]
    INV --> OS[Operator Setup]
    OS --> PROV[Guest Loop provisioning]
    PROV --> SI[Sign-in]
    SI --> ACT[Account activation]
```

**Team invitation** accept joins an existing restaurant — parallel path; see [team.md](./team.md).

## Local quick start

```bash
# Frontend (repo root)
npm run dev

# Backend
cd backend/TummlyBackend && dotnet run
```

Deployment and environment variables: [backend/DEPLOYMENT.md](../../backend/DEPLOYMENT.md).

## Maintenance

Update product docs in the **same PR** when you change:

- A user flow (steps, states, emails)
- A route or API endpoint listed in these docs
- A feature's Shipped / Partial / Planned status

Record the change in [CHANGELOG.md](./CHANGELOG.md) and bump **Product version** when Shipped or Partial behaviour changes.

## Legacy documentation

These files are **implementation supplements**. Product truth lives in `docs/product/`.

| Legacy file | Superseded by | Still useful for |
|-------------|---------------|------------------|
| [sign_in_flows.md](../sign_in_flows.md) | [sign-in.md](./sign-in.md) | Figma screen IDs, OTP decision log |
| [guest-loop-audit.md](../guest-loop-audit.md) | [operator-setup.md](./operator-setup.md), [self-service-pilot.md](./self-service-pilot.md) | Deploy checklist, QA notes |
| [form_function.md](../form_function.md) | Domain product files | Form component stack (includes legacy Trial Request form notes) |
| [pending-work.md](../pending-work.md) | CHANGELOG + product status tables | Historical build plan |
