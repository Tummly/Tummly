# Tummly Channel Credits — Completed Product Questionnaire v3.0

**Status:** COMPLETED AND APPROVED FOR DEVELOPMENT  
**Authority:** Amin Ordikhani  
**Date:** 2026-08-06  
**Market:** United Kingdom only  
**Currency:** GBP  
**VAT:** 20%, exclusive

____

## 1. Billing unit

### Q1.1

One subscription per operator Billing Account.

### Q1.2

All included Locations share one AI, Email and SMS pool. Every unit remains attributed to its Location.

### Q1.3

Group includes five Locations. Additional Group Locations cost £39/month + VAT and add 100 AI actions, 5,000 Email credits and 100 SMS credits monthly.

____

## 2. Soft lock

### Q2.1

Read-only:

• Home  
• Feedback  
• Guests  
• Existing Capture and QR records  
• Existing Campaigns, Offers and Redemptions  
• Existing Reports  
• Settings  
• Billing  
• Support  
• Authorised privacy routes

### Q2.2

Block:

• Recovery Email and SMS  
• New AI generation  
• Campaign creation, editing, scheduling and sending  
• Offer creation, editing and publishing  
• New QR placements and print assets  
• General export  
• Shop purchases other than restoration

### Q2.3

Any successfully paid Starter, Growth or Group plan ends Soft lock.

### Q2.4

Pilot ends day 30. Soft lock lasts to day 44. Dormant begins day 45. Dormant retention is 90 days.

____

## 3. Email credits

### Q3.1

One-to-one private Feedback recovery Email is free at launch, subject to permission, suppression, one initial send and one reviewed resend.

### Q3.2

Campaign Email uses 1 Email credit per eligible recipient accepted by the provider. Skipped recipients use zero.

### Q3.3

Campaign Email uses a provider-neutral Tummly adapter.

Shared Resend Broadcast Contacts must not be used as the source of truth because guest identity and consent are tenant specific.

The live provider binding must support tenant-scoped unsubscribe and Tummly-owned suppression. This technical binding does not alter the credit model.

____

## 4. SMS credits

### Q4.1

1 SMS credit = 1 Twilio billable segment.

### Q4.2

• Recovery SMS: actual accepted segments  
• Campaign SMS: actual accepted segments per eligible recipient

### Q4.3

Yes. Show encoding, character count, segment estimate, recipients, total credits and balance before send.

____

## 5. AI credits

### Q5.1

1 AI action = 1 completed usable operator-triggered output.

### Q5.2

| Action | Charge |
|---|---:|
| Automatic Feedback classification | 0 |
| Standard scheduled Weekly Brief | 0 |
| AI Assistant answer | 1 |
| Recovery Draft | 1 |
| Campaign copy or brief | 1 |
| Operator summary or recommendation | 1 |
| Regenerate | 1 |

### Q5.3

A successful regeneration consumes one additional action. Failed, timed-out and blocked attempts use zero.

____

## 6. Included allowances

| Plan | AI | Email | SMS |
|---|---:|---:|---:|
| Pilot | 20 once | 500 once | 20 once |
| Starter £39 | 100/month | 2,500/month | 100/month |
| Growth £99 | 500/month | 10,000/month | 350/month |
| Group £199 | 1,500/month | 25,000/month | 700/month |

### Q6.1

Included credits do not roll over. Purchased top-ups expire after 12 months.

### Q6.2

Credits reset on the subscription anniversary. Monthly-plan credits release only after successful renewal payment. Annual-plan credits release monthly while active and paid.

### Q6.3

Pilot allowance is allocated once when the Activation Code is redeemed or the Pilot is authorised.

____

## 7. Out of credits and top-ups

### Q7.1

Hard stop the exhausted channel only. No postpaid overage. Warn at 80%, 90% and 100%.

### Q7.2

AI:

• 100 — £5 + VAT  
• 500 — £15 + VAT  
• 2,000 — £39 + VAT

Email:

• 5,000 — £10 + VAT  
• 20,000 — £30 + VAT  
• 50,000 — £60 + VAT

SMS:

• 100 — £12 + VAT  
• 500 — £55 + VAT  
• 1,000 — £100 + VAT  
• 5,000 — £450 + VAT, Group or approval only

### Q7.3

Fair-use protection applies to spam, fraud, abnormal retries, provider protection, complaint rates, bounce rates and excessive technical load. It is not a hidden replacement for visible usage limits.

____

## 8. Campaigns

### Q8.1

Campaign v1 supports Email and SMS.

### Q8.2

Yes. Show the final eligible audience, skipped count, Email recipients or SMS segments, estimated credits, current balance and balance after send.

### Q8.3

Reserve the full estimate atomically. Charge accepted units only. Release unused units. Unexpected incomplete processing becomes `partially_sent` and requires a reviewed retry.

### Q8.4

Only eligible recipients may be submitted. Skipped, suppressed, invalid, opted-out and duplicate recipients cost zero.

____

## 9. Sell price and packaging

### Q9.1

Paid plans include allowances. Additional usage is prepaid. No postpaid overage.

### Q9.2

Use the plan allowances and top-up prices in sections 6 and 7.

### Q9.3

Pilot is an acquisition cost.

• One qualifying starter kit per Billing Account lifetime  
• £25 landed-cost cap  
• Dispatch only after setup and address verification  
• No second free kit after paid conversion  
• Reorders are paid

____

## 10. Sign-off

| Role | Name | Date | Decision |
|---|---|---|---|
| Product and commercial authority | Amin Ordikhani | 2026-08-06 | Approved |
| Engineering | Salman Shahid | Implementation acknowledgement | Proceed |
| Design | Mohamed Mahmoud | Design reconciliation acknowledgement | Proceed |
