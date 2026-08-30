# Channel cost analysis — Email, SMS, AI

**Status:** Planned (supporting material for billing / Campaigns; no metering shipped)  
**Audience:** Product / commercial (primary), engineering (COGS reference)  
**Related:** [channel-credits-questionnaire.md](./channel-credits-questionnaire.md), `CONTEXT.md`  
**Last cost check:** 2026-08-06 (public vendor pages; verify before contracts)

This document states **Tummly’s vendor cost of goods (COGS)** for guest-facing channels. It does **not** set burn rates, plan allowances, margins, or sell prices — those live in [billing pack v3.0](./billing-pack-v3.0/).

---

## 1. Purpose

Before Campaign Section work:

1. Show what each send / AI call costs Tummly today.
2. Give volume scenarios so product can set credit math.
3. Point commercial questions to [billing pack v3.0](./billing-pack-v3.0/).

---

## 2. Settled commercial shape (from grilling)

| Item | Value |
|------|--------|
| Plans | **Pilot** £0 · **Starter** £39 · **Growth** £99 · **Group** £199 / month |
| Credit pools | **AI credits** · **Email credits** · **SMS credits** (separate) |
| Metered scope | Guest-facing only (campaigns, recovery, AI drafts) |
| Not metered (this model) | Sign-in OTP, system / trial / setup email |
| Activation | Pack = QR stickers + Activation Code → **Pilot** |
| Access after Activation period | **Soft lock** (detail in questionnaire) |
| Billing unit | Open — questionnaire section 1 |

---

## 3. Vendor map (shipped stack)

| Channel | Vendor | Used for today | Guest-facing Campaign use |
|---------|--------|----------------|---------------------------|
| Email | **Resend** (SMTP fallback exists) | OTP, trial review, setup, password, recovery guest email (as built) | Campaign email (planned) |
| SMS | **Twilio** | Sign-in OTP (alt); guest recovery SMS (UI estimates 1 credit) | Campaign SMS (planned) |
| AI | **Azure OpenAI** (mini-tier deployment) | Feedback classification; recovery draft generation | Campaign copy / brief assist (planned) |

Platform AI (classification) and guest AI (drafts) share the same Azure path. Whether classification burns **operator** AI credits is a product question — see questionnaire section 5.

---

## 4. Unit COGS

Prices are **vendor list prices in USD** unless noted. Convert to GBP with the FX rate on the day you set sell prices. Vendors may add tax, carrier fees, or change rates without notice.

### 4.1 Email — Resend (transactional)

Source: [resend.com/pricing](https://resend.com/pricing) (checked 2026-08-06).

| Resend plan | Monthly fee | Included emails | Overage / 1,000 emails | Effective ¢ / email (included) |
|-------------|-------------|-----------------|------------------------|--------------------------------|
| Free | $0 | 3,000 (100 / day cap) | — | $0.00 (capped) |
| Pro | $20 | 50,000 | $0.90 | **$0.0004** |
| Pro | $35 | 100,000 | $0.90 | **$0.00035** |
| Scale | $90 | 100,000 | $0.90 | **$0.0009** |
| Scale | $160 | 200,000 | $0.80 | **$0.0008** |
| Scale | $350 | 500,000 | $0.70 | **$0.0007** |
| Scale | $650 | 1,000,000 | $0.65 | **$0.00065** |

**Working COGS for planning (single email on Pro 50k):** ≈ **$0.0004 / email** (~£0.0003 at ~1.3 USD/GBP).  
**Overage COGS:** **$0.0009 / email**.

**Marketing / broadcast:** Resend also sells **contact-based** marketing plans (not per email). If Campaigns use broadcasts, COGS is driven by **contact count**, not send count — product must choose transactional vs marketing (questionnaire section 3.3).

**Note:** Auth and system emails share the same Resend account. Campaign volume will dominate once live; still budget OTP/setup as a fixed overhead (~low thousands / month at early scale).

### 4.2 SMS — Twilio United Kingdom

Source: [twilio.com/sms/pricing/gb](https://www.twilio.com/en-us/sms/pricing/gb) (checked 2026-08-06).

| Item | Price (USD) |
|------|-------------|
| Outbound SMS — mobile number | **$0.056 / segment** |
| Outbound SMS — alphanumeric sender ID | **$0.056 / segment** |
| Outbound SMS — short code | $0.0524 / segment |
| Inbound SMS | $0.0075 / segment |
| Failed message processing | $0.001 / failed message |
| Engagement Suite (link shorten / schedule) | $0.015 / outbound (first 1,000 free / month) |
| SMS pumping protection | $0.025 / message (if enabled) |
| UK local number lease | ~$1.15 / month |
| UK mobile number lease | ~$2.50 / month |
| Alphanumeric sender ID | Free (registration rules still apply) |

**Working COGS for planning:** ≈ **$0.056 / segment** (~**4–5p** / segment at typical FX), **plus** any carrier pass-through fees Twilio lists for UK.

One long Campaign SMS can be **2+ segments**. Product must define whether **1 SMS credit = 1 segment** (questionnaire section 4).

### 4.3 AI — Azure OpenAI

Tummly code uses Azure OpenAI **Chat Completions** on a **mini-tier** deployment for:

- Feedback classification (structured outputs)
- Recovery draft generation

Public Azure pricing pages often render dynamically. Independent trackers (2026) list **GPT-4o mini**-class rates near:

| Direction | Approx. USD / 1M tokens |
|-----------|-------------------------|
| Input | **$0.15** |
| Output | **$0.60** |
| Cached input (where available) | ~$0.075 |

**Verify** the exact deployment model and regional rate in the Azure portal before locking sell prices.

**Illustrative call COGS** (order-of-magnitude only):

| Workload | Rough tokens (in + out) | Approx. COGS / call |
|----------|-------------------------|--------------------|
| Feedback classification (short comment + JSON) | ~500–1,500 | **~$0.0002–$0.001** |
| Recovery draft (prompt + message body) | ~1,000–3,000 | **~$0.0005–$0.002** |
| Campaign copy assist (planned, longer) | ~2,000–6,000 | **~$0.001–$0.004** |

AI unit COGS is **much cheaper than one SMS segment**. Credit scarcity for AI is a product design choice, not a vendor cost floor.

---

## 5. Volume scenarios (Tummly COGS only)

Assumptions for the tables below:

- Email: Resend Pro effective **$0.0004 / email** (within 50k included).
- SMS: **$0.056 / segment**; **1 segment per message** (optimistic — long copy costs more).
- AI: **$0.001 / call** midpoint for draft or classification.

These are **platform costs**, not operator credit charges.

### 5.1 Email

| Monthly guest emails | Approx. Resend COGS |
|----------------------|---------------------|
| 1,000 | $0.40 |
| 10,000 | $4 |
| 50,000 | $20 (fills Pro 50k tier) |
| 100,000 | ~$35–$90 depending on Pro vs Scale tier |

### 5.2 SMS

| Monthly guest SMS segments | Approx. Twilio COGS |
|----------------------------|---------------------|
| 100 | $5.60 |
| 500 | $28 |
| 1,000 | $56 |
| 5,000 | $280 |
| 10,000 | $560 |

SMS dominates variable cost. Plan allowances and hard-stop rules matter most here.

### 5.3 AI

| Monthly AI calls | Approx. Azure COGS (at $0.001 / call) |
|------------------|--------------------------------------|
| 1,000 | $1 |
| 10,000 | $10 |
| 50,000 | $50 |
| 100,000 | $100 |

### 5.4 Mixed Campaign example (illustrative)

One Campaign: **2,000 email** + **500 SMS** (1 segment each) + **1 AI brief** for the operator.

| Line | COGS |
|------|------|
| Email 2,000 × $0.0004 | $0.80 |
| SMS 500 × $0.056 | $28.00 |
| AI 1 × $0.001 | ~$0.00 |
| **Total** | **~$28.80** |

SMS is ~97% of that Campaign’s variable COGS.

### 5.5 Subscription fee vs channel COGS (context only)

| Plan | Monthly fee | SMS segments that consume the fee at $0.056 (ignore other costs) |
|------|-------------|------------------------------------------------------------------|
| Pilot | £0 | N/A — funded by pack / CAC; limit via free credits |
| Starter | £39 | ~ order of **~800–1,000** segments before fee is “spent” on SMS alone (FX-dependent) |
| Growth | £99 | ~ **~2,000–2,500** segments |
| Group | £199 | ~ **~4,000–5,000** segments |

Product must set included SMS credits **below** a safe fraction of these numbers so subscription margin stays after email, AI, infra, support, and QR pack amortisation.

---

## 6. Other infra / COGS (out of channel tables, still relevant)

| Cost | Notes |
|------|--------|
| Azure App Service / DB / storage / SignalR | Fixed + scale; not per message |
| Ideal Postcodes | Address lookup; cached to cut cost |
| QR print + ship (Pilot pack) | Operational COGS for Activation fulfilment — questionnaire section 9.3 |
| Support time | Soft lock and billing questions increase tickets |

---

## 7. Product fill-in (pack v3.0 is the source)

Do **not** invent numbers here. Do not copy pack tables into this file. Commercial burn rates, allowances, and sell prices live in [billing pack v3.0](./billing-pack-v3.0/). Start at the [completed questionnaire](./billing-pack-v3.0/Tummly_Channel_Credits_Questionnaire_COMPLETED_v3.0.md) and [tummly_uk_billing_config_v3.0.json](./billing-pack-v3.0/tummly_uk_billing_config_v3.0.json). The [channel-credits-questionnaire.md](./channel-credits-questionnaire.md) file is a pointer only.

### 7.1 Burn rates (product)

| Action | AI credits | Email credits | SMS credits |
|--------|------------|---------------|-------------|
| Feedback classification | | | |
| Recovery AI draft | | | |
| Recovery email send | | | |
| Recovery SMS send | | | |
| Campaign AI assist | | | |
| Campaign email / recipient | | | |
| Campaign SMS / recipient or segment | | | |

### 7.2 Monthly included allowances (product)

| Plan | AI | Email | SMS |
|------|----|-------|-----|
| Pilot | | | |
| Starter | | | |
| Growth | | | |
| Group | | | |

### 7.3 Overage / top-up / Soft lock matrix

_Paste product answers or link to the completed questionnaire._

---

## 8. Engineering implications (after product sign-off)

When the questionnaire is complete:

1. Persist plan + three credit balances (billing unit as decided).
2. Estimate and reserve credits **before** Campaign send; show estimate in UI.
3. Meter SMS by the product’s unit (segment vs send).
4. Replace display-only strings (“No email credits required”, “1 SMS credit”, “Uses 1 AI action”) with live balances.
5. Implement Soft lock gates separately from credit metering (access vs usage).
6. Keep OTP / system email off the guest credit ledger.

---

## 9. Sources

| Vendor | URL | Checked |
|--------|-----|---------|
| Resend pricing | https://resend.com/pricing | 2026-08-06 |
| Twilio SMS UK | https://www.twilio.com/en-us/sms/pricing/gb | 2026-08-06 |
| Azure OpenAI | https://azure.microsoft.com/pricing/details/cognitive-services/openai-service/ | 2026-08-06 (verify deployment in portal) |

Re-check prices before any commercial launch or contract.
