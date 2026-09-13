# Channel COGS — live configuration (2026-09-13)

**Audience:** Product / commercial (technical COGS only)  
**Scope:** Current vendor accounts and Azure QA (`rg-tummly-qa`). No production resource group exists yet.  
**Does not set:** burn rates, plan allowances, margins, or sell prices (see [billing pack v3.0](./billing-pack-v3.0/)).  
**Last refresh:** 2026-09-13 (Twilio account re-checked Active / Full, balance £35).

---

## Snapshot

| Vendor | Account state | Working unit COGS |
|--------|---------------|-------------------|
| Resend | **Free** | $0 / email inside Free caps |
| Twilio | **Active (Full)** · balance **£35** | UK outbound **£0.042325 / segment**; OTP Verify adds **$0.05** per successful verification |
| Azure OpenAI | Live QA | **gpt-5-mini** · **$0.25 / $2.00 / $0.025** per 1M tokens (input / output / cached input) |
| Ideal Postcodes | Paid pack | **£0.054 / credit** (400 for £21.60) |
| Azure platform (QA) | Budget | MTD spend **£5.33** of **£80** monthly stop budget |

---

## 1. Email — Resend

| Field | Value |
|-------|--------|
| Plan | **Free** (confirmed) |
| Monthly fee | **$0** |
| Included | **3,000 / month**, **100 / day** hard cap |
| Overage | None on Free (must upgrade to send past caps) |
| From (QA) | `Tummly <onboarding@resend.dev>` |
| Reply-To | `engineering@tummly.com` |
| QA redirect | All outbound → `engineering@tummly.com` |
| Domain | `tummly.com` not yet used as From (still Resend test sender) |

**Campaign Email:** uses the **transactional** Resend Email API (same path as recovery / system mail). Not Resend Marketing / Broadcast contact pricing.

Auth, trial, setup, and Campaign volume share one Free pool. The **100 / day** cap binds before monthly volume at early scale.

Paid list (for when Free is left — public pricing, not current account):

| Plan | Fee | Included | Overage / 1,000 |
|------|-----|----------|-----------------|
| Pro | $20 / $35 | 50k / 100k | $0.90 |
| Scale | from $90 | from 100k | $0.90 → lower at higher tiers |

---

## 2. SMS — Twilio

| Field | Value |
|-------|--------|
| Account | Tummly WebApp · **Active** · type **Full** · balance **£35.00** (2026-09-13) |
| OTP | **Twilio Verify** · service “OTP Verification for Tummly” · 6-digit SMS · region **GB** |
| Guest Recovery / Campaign From | **Not set** (`RecoveryFromNumber` missing on QA) |
| Leased UK numbers | **0** |
| Messaging Services / alpha senders | **0** |
| Engagement Suite / SMS pumping | Not evidenced as enabled |

### UK Messaging rates (Twilio Pricing API on this account, GBP)

| Item | Price |
|------|--------|
| Outbound — mobile / local | **£0.042325 / segment** |
| Outbound — short code | **£0.039604 / segment** |
| Inbound | **£0.005668 / segment** |
| UK local number lease | **£0.87 / month** |
| UK mobile number lease | **£1.89 / month** |
| Alphanumeric Sender ID | Free (registration rules still apply) |

Public USD list (~$0.056 / segment) matches this GBP rate at typical FX. Pricing API returned one current price per number type across listed UK carriers (no separate carrier add-on in that payload).

### Verify OTP (list)

**$0.05** per successful verification **plus** UK SMS channel fee (~**£0.042** / segment). Channel fee applies on SMS send attempts.

### Production SMS sender — open

Production sender approach is **not decided**. After product / ops choose one path, update this record with:

1. **Sender configuration** — e.g. UK local number, UK mobile number, alphanumeric Sender ID, Messaging Service pool.  
2. **Fixed / registration cost** — monthly lease (see table above) plus any one-time registration, brand, or compliance fees Twilio or UK carriers charge for that path.  
3. **App binding** — set `TwilioSettings__RecoveryFromNumber` (and Messaging Service SID if used) on QA and production.

Until that is done, guest Recovery SMS and Campaign SMS have **no live From identity**; variable segment rates above still apply once sends start.

One long SMS can be **2+ segments**.

---

## 3. AI — Azure OpenAI

| Field | Value |
|-------|--------|
| Resource | `rg-foundry-openai` · AIServices · **UK South** · SKU **S0** |
| Deployment | **`gpt-5-mini`** |
| Model version | **`2025-08-07`** |
| Capacity SKU | **GlobalStandard** · capacity **10** |
| App config | `FeedbackClassification__DeploymentName=gpt-5-mini` (shared) |

### Token rates (Azure retail meters for this deployment, USD / 1M tokens)

| Direction | Rate |
|-----------|------|
| Input | **$0.25** |
| Output | **$2.00** |
| Cached input | **$0.025** |

### Workloads on this deployment

- Feedback classification  
- Recovery drafts  
- Campaign message draft · campaign recommendation  
- Home recommendation · offer recommendation  
- Weekly brief  
- Assistant live answer · advisory reason  

### Illustrative call COGS (token estimate only)

| Workload | Rough tokens | Approx. USD / call |
|----------|--------------|--------------------|
| Feedback classification | ~500–1,500 | **~$0.0004–$0.0015** |
| Recovery draft | ~1,000–3,000 | **~$0.001–$0.004** |
| Campaign AI assist | ~2,000–6,000 | **~$0.002–$0.008** |

**Speech:** Azure Speech Fast Transcription on the same Cognitive account — retail **$0.36 / hour** of audio.

---

## 4. Address lookup — Ideal Postcodes

| Field | Value |
|-------|--------|
| Purchase | One-time **400 credits** for **£21.60** |
| Unit COGS | **£0.054 / credit** |
| Consumed | **24** (6%) |
| Remaining | **376** |
| Spend so far | **~£1.30** |

Typeahead search is free; a credit is spent on a successful full address return. Backend caches duplicate autocomplete / postcode calls to cut spend.

---

## 5. Platform infrastructure (Azure QA)

Not App Service — **Azure Container Apps**. Resource group: **`rg-tummly-qa`** only.

| Resource | Config | Retail / note |
|----------|--------|----------------|
| API container | 0.5 vCPU · 1 Gi · min=max **1** · UK South | ~$16–$55 / mo if always on (idle vs active) |
| SQL | Standard **S0** · Central US · 30 GiB | ~$0.48 / day ≈ **~$14.50 / mo** |
| Static Web Apps | **Free** | $0 |
| ACR | **Basic** | ~$5 / mo |
| Storage | Standard_LRS Hot | Low at current volume |
| Log Analytics | PerGB2018 · 30-day | $2.88 / GB after free allotment |
| SignalR | In-process ASP.NET; **no** Azure SignalR; Redis not set | No SignalR Service fee |
| Cost guard | Budget **£80 / mo** · Automation stop | MTD **£5.33 GBP** (2026-09-13) |

Revolut on QA is **sandbox** only — not production payment COGS.

---

## 6. Indicative production Azure cost / scaling model

**Status:** Engineering estimate only. Production is **not** provisioned. Figures use Azure retail meters (USD) and the same stack shape as QA (Container Apps + Azure SQL + Static Web Apps + ACR + storage + Log Analytics + shared OpenAI). They **exclude** Resend, Twilio, Ideal Postcodes, Revolut fees, support labour, and QR print.

### 6.1 Assumptions (change these and the totals move)

| Driver | Assumed behaviour |
|--------|-------------------|
| “Active paying account” | One Restaurant / billing account with operators using the dashboard |
| Frontend | Static Web Apps **Standard** (~**$9 / mo**) once production needs custom domain / SLA |
| API | Azure Container Apps, UK South; scale **replica count** (0.5 vCPU · 1 Gi each) |
| Database | Azure SQL **Standard DTU** first; step up S0 → S2 → S3 (UK South retail used below) |
| Realtime | In-process SignalR until multi-replica; then **Redis** (Basic C1 ~**$50 / mo**) for backplane |
| Registry | ACR Basic (~**$5 / mo**) → Standard (~**$20 / mo**) at higher deploy volume |
| Logs | Log Analytics grows with traffic (~**$10 → $200+ / mo**) |
| OpenAI | Usage-based; modelled separately as a **band** per account, not a fixed line |

**Retail anchors used**

| Component | Approx. retail |
|-----------|----------------|
| ACA 0.5 vCPU + 1 Gi (mostly active) | ~**$45–$55 / replica / mo** |
| SQL S0 / S2 / S3 (UK South) | ~**$18 / $91 / $181 / mo** |
| SWA Standard | **$9 / mo** |
| ACR Basic / Standard | ~**$5 / $20 / mo** |
| Redis Basic C1 | ~**$50 / mo** |
| OpenAI gpt-5-mini | **$0.25 / $2.00 / 1M** in/out (see §3) |

### 6.2 Suggested footprint by account tier

| Active paying accounts | API replicas (indicative) | SQL | Redis | ACR | Notes |
|------------------------|---------------------------|-----|-------|-----|--------|
| **100** | 1–2 | S0–S2 | Optional | Basic | Near current QA shape; add SWA Standard |
| **500** | 2–3 | S2–S3 | Likely | Basic | Multi-replica → plan Redis for SignalR |
| **1,000** | 3–5 | S3 (or GP later) | Yes | Basic/Standard | Stronger SQL + logs |
| **5,000** | 6–12+ | S3+ / GP | Yes (larger) | Standard | Revisit SQL tier, OpenAI capacity, observability |

### 6.3 Indicative monthly platform cost (USD, excl. channel vendors)

Bands = low (lean / quiet) → mid (expected) → high (chatty operators, heavy logs, peak replicas).

| Active paying accounts | Platform infra (excl. OpenAI) | OpenAI usage band* | Combined indicative |
|------------------------|-------------------------------|--------------------|---------------------|
| **100** | **$80 – $180 – $280** | $20 – $80 – $200 | **~$100 – $260 – $480** |
| **500** | **$250 – $450 – $750** | $100 – $400 – $1,000 | **~$350 – $850 – $1,750** |
| **1,000** | **$450 – $850 – $1,400** | $200 – $800 – $2,000 | **~$650 – $1,650 – $3,400** |
| **5,000** | **$1,500 – $3,000 – $5,500** | $1,000 – $4,000 – $10,000 | **~$2,500 – $7,000 – $15,500** |

\*OpenAI band assumes rough **~$0.20 / $0.80 / $2.00 per account / month** of gpt-5-mini spend at low / mid / high engagement (classification + drafts + assistant). Replace with measured tokens once production has history.

**Rough GBP** (÷ ~1.3 USD/GBP, order-of-magnitude only):

| Accounts | Mid combined (GBP / mo) |
|----------|-------------------------|
| 100 | ~**£200** |
| 500 | ~**£650** |
| 1,000 | ~**£1,300** |
| 5,000 | ~**£5,400** |

### 6.4 What this model is not

- Not a quote, reserved instance, or Azure Hybrid Benefit price.  
- Not measured production Cost Analysis (none yet).  
- Not a substitute for a capacity test once real feedback / Campaign volume exists.  
- Channel COGS (email / SMS / Ideal Postcodes) stay in §1–4 and still dominate variable cost when SMS is live.

---

## 7. Volume scenarios (channel COGS)

Assumptions for planning **after** Free email ends (SMS account is already Full):

- Email: Resend Pro 50k effective **$0.0004 / email** (not current Free).  
- SMS: **£0.042325 / segment** · 1 segment per message (optimistic).  
- AI: **~$0.002 / call** midpoint at gpt-5-mini rates.

| Monthly guest emails (on Pro 50k) | Approx. Resend COGS |
|-----------------------------------|---------------------|
| 1,000 | $0.40 |
| 10,000 | $4 |
| 50,000 | $20 (fills Pro 50k) |

| Monthly SMS segments | Approx. Twilio COGS |
|----------------------|---------------------|
| 100 | ~£4.23 |
| 500 | ~£21.16 |
| 1,000 | ~£42.33 |
| 5,000 | ~£211.63 |

| Monthly AI calls @ ~$0.002 | Approx. Azure COGS |
|----------------------------|--------------------|
| 1,000 | $2 |
| 10,000 | $20 |
| 50,000 | $100 |

**Mixed Campaign (illustrative):** 2,000 email (Pro) + 500 SMS + 1 AI brief ≈ **$0.80 + ~£21.16 + ~$0.00** — SMS still dominates variable COGS.

**While still on Resend Free:** email line is **$0** until the 100/day or 3,000/month cap blocks sends.

---

## 8. Gaps / next checks

1. Resend — upgrade timing when Campaign volume exceeds Free caps.  
2. Twilio — **decide production SMS sender approach**; then record sender config + fixed/registration cost and set `RecoveryFromNumber`.  
3. Azure Cost Analysis portal — meter-level split for QA; production Cost Analysis once prod exists.  
4. Ideal Postcodes — top-up before remaining credits run out.  
5. Production Azure — not provisioned; use §6 as planning only until real spend exists.

---

## Sources

| Source | Date |
|--------|------|
| Operator confirmation (Resend Free; Ideal Postcodes 400 @ £21.60, 24 used) | 2026-09-13 |
| Twilio Account API — status **active**, type **Full**, balance **£35.00 GBP**; 0 phone numbers | 2026-09-13 |
| Azure CLI — deployments, ACA env, budget `budget-tummly-qa-costguard-30` | 2026-09-13 |
| Azure Retail Prices API — gpt-5-mini, SQL S0/S2/S3, ACA, ACR, Redis, SWA | 2026-09-13 |
| Twilio Pricing API (GB Messaging) | 2026-09-13 |
| Resend / Twilio Verify public list pages (paid / Verify fees) | 2026-09-13 |
