# Revolut QA sandbox rehearsal (test cards only)

Use this before Production keys. QA pays only on Revolut **Sandbox** with
[test cards](https://developer.revolut.com/docs/guides/merchant/test-and-go-live/testing/test-cards).
Do **not** put Production Merchant secrets on QA.

**Interactive setup:** from the repo root run
`./scripts/wizard-revolut-qa-sandbox.sh` — it walks Sandbox Business signup,
Merchant secret, VAT fields, plan variations, webhook, and optional ACA apply.
Values land in gitignored `infra/qa/secrets.qa.env`.

Related: go-live Production checklist
[`REVOLUT-GO-LIVE.md`](./REVOLUT-GO-LIVE.md); ACA apply
[`apply-aca-secrets.ps1`](./apply-aca-secrets.ps1); empty keys
[`secrets.revolut.env.example`](./secrets.revolut.env.example); catalog script
[`scripts/revolut-create-plan-variations/README.md`](../../scripts/revolut-create-plan-variations/README.md).

Payment path: Tummly confirm → backend creates Merchant order/subscription →
operator redirects to Revolut **Hosted Checkout** (`checkout_url`) → user enters
card on Revolut → webhook `ORDER_COMPLETED` + retrieve gate activates Tummly
(entitlements, `TM-` invoice, credits). No Merchant public key on the frontend.

---

## 0. What you need from Revolut (HITL)

Production API secret / public key alone is **not** enough for QA.

| Item | Source |
| --- | --- |
| Sandbox Business Merchant account | Revolut Business → Sandbox (separate from Production) |
| Sandbox Merchant **secret** | Sandbox API credentials (not the Production secret) |
| Sandbox webhook `signing_secret` | Create webhook on Sandbox → QA API URL |
| Eight sandbox `plan_variation_id`s | `scripts/revolut-create-plan-variations/` with sandbox secret |

Public / publishable key: **not used** for this HPP flow.

---

## 1. Create sandbox catalog

```bash
export REVOLUT_SECRET_KEY=sk_…   # Sandbox only
export REVOLUT_API_BASE_URL=https://sandbox-merchant.revolut.com
export REVOLUT_API_VERSION=2026-04-20
./scripts/revolut-create-plan-variations/create-plan-variations.sh --apply \
  --out /tmp/revolut-sandbox-plan-variations.env
```

Keep the printed `Revolut__PlanVariations__*` lines for step 3. Do not commit them.

---

## 2. Register Sandbox webhook

In Revolut Sandbox dashboard, create a webhook:

- URL:
  `https://ca-tummly-qa-api.agreeablewater-62e50314.uksouth.azurecontainerapps.io/api/webhooks/revolut`
  (or `https://api.qa.tummly.com/api/webhooks/revolut` if that DNS hits the same app)
- Events: same set as lock 04 / `REVOLUT-GO-LIVE.md` § D
- Copy `signing_secret` → `Revolut__WebhookSigningSecret`

---

## 3. Fill QA secrets (Sandbox only)

Copy lines from [`secrets.revolut.env.example`](./secrets.revolut.env.example)
into gitignored `infra/qa/secrets.qa.env`:

```bash
# Required for QA test-card rehearsal
Revolut__RequireSandboxHost=true
Revolut__ApiBaseUrl=https://sandbox-merchant.revolut.com
Revolut__ApiVersion=2026-04-20
Revolut__SecretKey=…                 # Sandbox secret
Revolut__WebhookSigningSecret=…      # Sandbox webhook
# Paste all eight Revolut__PlanVariations__* from step 1

# Seller VAT / legal — any complete non-empty pack values (shown on QA TM PDFs)
TUMMLY_VAT_REGISTRATION_NUMBER=…
TUMMLY_VAT_EFFECTIVE_DATE=…
TUMMLY_LEGAL_NAME=…
TUMMLY_REGISTERED_ADDRESS=…
```

`Revolut__RequireSandboxHost=true` makes the API refuse Merchant create if
`ApiBaseUrl` is the live host — so Production keys cannot take real money on QA.

Apply:

```powershell
cd infra/qa
./apply-aca-secrets.ps1
```

Confirm revision ready, then:

```bash
./scripts/probe-qa-revolut-sandbox.sh
# expect: hostMode=sandbox, status=ready, createBlockedCode=null
```

Or: `curl -sS https://api.qa.tummly.com/health/revolut | jq`

---

## 4. Sandbox test cards (HPP)

Only on Sandbox. Any CVV + any future expiry (Revolut docs).

| Outcome | PAN | Brand |
| --- | --- | --- |
| Success | `4929420573595709` | Visa |
| Success | `5281438801804148` | Mastercard |

Error / 3DS / stuck cards: Revolut
[Test cards](https://developer.revolut.com/docs/guides/merchant/test-and-go-live/testing/test-cards)
(error table). For 3DS-fail cards, order amount must meet Revolut’s threshold
(e.g. ≥ £25 GBP).

---

## 5. Flow rehearsal matrix (Operator on QA)

Use an **Owner** with **Manage** on Billing & credits. After each pay path,
wait for webhook (or refresh) — do not treat HPP land alone as activation.

| # | Flow | Where | Expect |
| --- | --- | --- | --- |
| 1 | First paid conversion (Pilot → Starter/Growth/Group) | Manage plan → confirm | HPP → success card → **Active**, included mint, `TM-` on Payment & invoices, Plan & subscription snapshot |
| 2 | Same-cadence upgrade (pay now) | Manage plan | HPP → plan + `plan_migration` credits |
| 3 | Downgrade / cadence change (schedule) | Manage plan | **No** HPP; scheduled line; Revolut `change-plan` `at_cycle_end` when correlated |
| 4 | Cancel plan | Manage plan | Schedule **Cancels on {Renewal date}**; no HPP |
| 5 | Extra Group Location add | Manage plan (Group) | HPP → count + credits |
| 6 | Extra Location remove | Manage plan | Schedule only |
| 7 | Credit top-up | Manage plan → Credit top-ups | HPP → allocate; Credits & usage |
| 8 | Update payment method | Payment & invoices | HPP card update → masked method |
| 9 | Invoice View / Download | Payment & invoices | Tummly VAT PDF (`TM-`) |
| 10 | Billing activity | Activity tab | Rows for pay / mint / notes |

Native (no Revolut money) still runs on the same Billing Account: schedule
clear, contacts, usage snapshot, Lock Alert chrome when lifecycle applies.

Dunning / refund / dispute: optional after core table; need sandbox overdue /
Admin refund paths.

---

## 6. Stuck or fail-closed

| Symptom | Check |
| --- | --- |
| `503` + `vat_not_ready` | Four `TUMMLY_*` keys on ACA |
| `503` + `revolut_not_ready` | Secret / ApiBaseUrl / ApiVersion |
| `503` + `revolut_sandbox_required` | `RequireSandboxHost=true` but host is not sandbox |
| `503` + `plan_variation_missing` | Eight sandbox UUIDs mounted |
| HPP ok, no entitlements | Webhook URL/signing secret; `/health/revolut` webhook flag; stuck revision (`probe-qa-api-revision.sh`) |
| Test card declined | Confirm Sandbox host (not Production) |

---

## Out of this doc

- Pasting real secrets into git
- Production keys on QA
- Live go-live (use `REVOLUT-GO-LIVE.md` after sandbox rehearsal passes)
