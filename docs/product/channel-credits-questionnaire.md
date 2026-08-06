# Channel credits — product questionnaire

**Status:** Planned (commercial model not in product)  
**Audience:** Product / commercial owners  
**Related:** [channel-cost-analysis.md](./channel-cost-analysis.md), `CONTEXT.md` (Subscription plan, Soft lock, credit pools)  
**Purpose:** Answer every open commercial question before Campaign wiring and credit metering.

Fill each answer box. Do not leave blanks — write “TBD / date” if blocked.

---

## Settled context (do not re-decide here)

| Item | Settled value |
|------|----------------|
| Subscription plans | **Pilot** £0 · **Starter** £39 · **Growth** £99 · **Group** £199 / month |
| Credit pools | Three separate pools: **AI credits**, **Email credits**, **SMS credits** |
| Metered scope | Guest-facing use only (campaigns, recovery, AI drafts for guests) |
| Out of scope | Sign-in OTP, trial/setup/system emails, operator auth SMS |
| Activation pack | QR stickers + **Activation Code** → activates **Pilot** |
| After Activation period | **Soft lock** (some dashboard data visible; not a hard Sign-in block). Detail below. |
| Vendor stack | Resend (email), Twilio (SMS), Azure OpenAI (AI) — costs in [channel-cost-analysis.md](./channel-cost-analysis.md) |

Engineering will **not** invent burn rates, allowances, margins, or sell prices. Product owns that math using the cost analysis.

---

## 1. Billing unit

**Q1.1** Is one subscription price charged **per operator account** or **per Owned location**?

Answer:

**Q1.2** If per account: do all Owned locations **share one credit pool**, or does each location get its own allowance?

Answer:

**Q1.3** Does **Group** change the billing unit (e.g. included location count + overage per extra venue)?

Answer:

---

## 2. Soft lock (Activation expired / unpaid Pilot)

Today’s shipped product **hard-locks** Sign-in when Activation period ends. Target is **Soft lock**.

**Q2.1** Which surfaces stay **visible** under Soft lock? (tick / list)

- [ ] Home summary / counts  
- [ ] Feedback list (no detail)  
- [ ] Feedback detail  
- [ ] Guests / Smart Groups  
- [ ] Guest details / Guest Profile  
- [ ] Capture  
- [ ] Campaigns list (read-only)  
- [ ] Settings  
- [ ] Other: _______________

Answer / notes:

**Q2.2** Which actions are **blocked** under Soft lock?

- [ ] Send guest recovery (email / SMS)  
- [ ] Generate AI draft  
- [ ] Create / send Campaign  
- [ ] Create / edit offers  
- [ ] Other: _______________

Answer:

**Q2.3** Soft lock ends when the operator selects which plan(s)? Starter only, or any paid plan?

Answer:

**Q2.4** Can an operator stay on **Pilot** after Activation period with Soft lock forever, or must they upgrade within N days?

Answer:

---

## 3. Email credits

UI today (display only): recovery email = “No email credits required”.

**Q3.1** Does **1:1 recovery email** burn Email credits?

- [ ] No (always free)  
- [ ] Yes — _____ credits per send  
- [ ] Yes only after N free sends / month  

Answer:

**Q3.2** Does **Campaign email** burn Email credits?

- [ ] Yes — _____ credits per recipient  
- [ ] Yes — _____ credits per send (batch)  
- [ ] No  

Answer:

**Q3.3** Are Campaign emails sent via Resend **transactional** plans or Resend **marketing / broadcast** (contact-based) pricing? This changes COGS — see cost analysis.

Answer:

---

## 4. SMS credits

Twilio UK outbound is billed **per segment** (~$0.056 / segment). Carrier fees may apply.

**Q4.1** What is **1 SMS credit**?

- [ ] 1 Twilio segment  
- [ ] 1 send attempt (Tummly absorbs multi-segment cost)  
- [ ] Other: _______________

Answer:

**Q4.2** Burn rate — recovery SMS vs Campaign SMS (credits each)?

| Action | Credits |
|--------|---------|
| Recovery SMS | |
| Campaign SMS (per recipient / per segment — state which) | |

**Q4.3** When a message spans multiple segments, does the UI show a **segment estimate** before send?

Answer:

---

## 5. AI credits

Tummly uses Azure OpenAI for (1) automatic **feedback classification** and (2) operator-triggered **recovery drafts**. Campaign AI assist is planned.

**Q5.1** What is **1 AI credit**?

Answer: (e.g. one draft generation, one campaign-copy assist, N tokens, …)

**Q5.2** Which actions burn AI credits?

| Action | Burns AI credits? | Credits if yes |
|--------|-------------------|----------------|
| Automatic feedback classification | | |
| Recovery AI draft generation | | |
| Campaign AI copy / brief assist | | |
| Other: _______________ | | |

**Q5.3** Regenerating a draft — new charge or included in the first credit?

Answer:

---

## 6. Included allowances (per plan, per month)

Fill monthly included credits. Use 0 if the channel is unavailable on that plan.

| Plan | AI credits | Email credits | SMS credits |
|------|------------|---------------|-------------|
| Pilot | | | |
| Starter (£39) | | | |
| Growth (£99) | | | |
| Group (£199) | | | |

**Q6.1** Do unused credits **roll over**? If yes, for how many months?

Answer:

**Q6.2** Do credits **reset** on the subscription anniversary date or calendar month?

Answer:

**Q6.3** Pilot free allowance — assigned at **Account activation** only, or topped up each month while on Pilot?

Answer:

---

## 7. Out of credits / overage

**Q7.1** When a pool hits zero mid-action, what happens?

| Channel | Hard stop | Soft warn + allow | Overage invoice | Block that channel only |
|---------|-----------|-------------------|-----------------|-------------------------|
| AI | | | | |
| Email | | | | |
| SMS | | | | |

**Q7.2** Are **credit top-up packs** sold in the dashboard shop? If yes, pack sizes and prices:

Answer:

**Q7.3** Fair-use caps beyond included credits? (legal terms allow fair-use)

Answer:

---

## 8. Campaigns (pre-build gates)

**Q8.1** Which channels does Campaign v1 support?

- [ ] Email  
- [ ] SMS  
- [ ] Both  
- [ ] Other: _______________

Answer:

**Q8.2** Must the operator see **estimated credit cost** before Campaign send?

Answer:

**Q8.3** If credits run out mid-Campaign (partial send), what is the rule?

Answer:

**Q8.4** Eligible audience — only guests with channel consent / no offers opt-out? Confirm product rule for credit charge on skipped recipients (charge 0).

Answer:

---

## 9. Sell price and packaging (product math)

Use [channel-cost-analysis.md](./channel-cost-analysis.md) for vendor COGS. Engineering does not set margin.

**Q9.1** How do you price one credit of each type to the operator? (bundle into plan only, per-credit top-up, or both)

Answer:

**Q9.2** Target sell price or pack prices (optional once decided):

| Credit type | Included in plan as… | Top-up pack price |
|-------------|----------------------|-------------------|
| AI | | |
| Email | | |
| SMS | | |

**Q9.3** Is Pilot funded mainly by **QR / fulfilment pack COGS**, subscription, or both? Approximate pack COGS if known:

Answer:

---

## 10. Sign-off

| Role | Name | Date | Notes |
|------|------|------|-------|
| Product | | | |
| Commercial / founder | | | |
| Engineering (ack only) | | | |

When this questionnaire is complete, update [channel-cost-analysis.md](./channel-cost-analysis.md) section 7 (Product fill-in) with the decided numbers, then Campaign metering can be designed against those facts.
