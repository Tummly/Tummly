# Tummly UK Pricing, Credits, VAT and Billing — Production Sign-off v3.0

**Decision status:** APPROVED FOR DEVELOPMENT  
**Commercial status:** SIGNED OFF  
**Sign-off authority:** Amin Ordikhani  
**Sign-off date:** 2026-08-06  
**Launch market:** United Kingdom only  
**Currency:** GBP only  
**Tax basis:** 20% VAT, exclusive pricing  
**Pricebook:** `TUMMLY-UK-GBP-2026-08-V3`  
**Supersedes:** all v1.0 and v2.0 pricing and billing packs

Development must proceed from this pack. No further product or commercial approval is required.

____

## 1. Final commercial model

Tummly uses:

1. A recurring Guest Loop subscription.
2. Monthly included AI, Email and SMS allowances.
3. Prepaid top-up packs above the allowance.
4. Separate channel balances.
5. Hard stops by channel rather than postpaid overage.
6. One account-level pool with Location attribution.
7. One immutable, effective-dated pricebook.
8. 20% VAT added to all launch charges.
9. No generic service fee.
10. No automatic paid conversion from Pilot.

____

## 2. UK-only launch

Supported:

• UK business accounts  
• UK Locations  
• UK delivery addresses  
• GBP billing  
• UK VAT  
• UK SMS routing

Not exposed in v1:

• Foreign currencies  
• International tax rules  
• Reverse-charge workflows  
• Overseas delivery  
• International plan pricing

Cards issued outside the UK may be accepted where Stripe supports them, but the customer and operating Locations must remain UK based.

____

## 3. Public plans

All prices are exclusive of VAT.

### Pilot — £0 for 30 days

Public copy:

> Try Guest Loop at one verified Location for 30 days. No payment card required and no automatic paid renewal.

Included:

• 1 Location  
• 2 operator users  
• 1 Published Guest Form plus 1 Draft  
• Up to 5 active QR placements  
• 1 active Offer  
• No separate manual Campaign Draft limit  
• 20 AI actions once  
• 500 Email credits once  
• 20 SMS credits once  
• One qualifying physical starter kit per Billing Account lifetime  
• Self-print QR assets  
• Pilot guidance

Pilot rules:

• Starts when the Activation Code is redeemed or an authorised admin activates the Pilot.  
• Does not begin merely when an account is created.  
• Credits do not replenish.  
• Top-ups are unavailable.  
• No automatic paid renewal.  
• The starter kit is not dispatched until business, Location, branding, Guest Form, placement intent and delivery address are verified.  
• Starter-kit landed-cost cap: £25.  
• If a starter kit was fulfilled during Pilot, no second free kit is provided after conversion.

### Starter — £39/month + VAT

Gross at 20% VAT: **£46.80/month**

Included:

• 1 Location  
• 3 operator users  
• 1 Published Guest Form plus 1 Draft  
• Up to 10 active QR placements  
• Up to 3 active Offers  
• Guest records included with no per-record charge at launch, subject to fair use, account status and retention  
• No separate manual Campaign Draft limit  
• Private Feedback and recovery  
• Weekly Brief  
• 100 AI actions/month  
• 2,500 Email credits/month  
• 100 SMS credits/month  
• Standard Email support

### Growth — £99/month + VAT

Gross at 20% VAT: **£118.80/month**

Included:

• Up to 3 Locations  
• 10 operator users  
• 1 Published Guest Form plus 1 Draft per Location  
• Up to 25 active QR placements per Location  
• Up to 10 active Offers account-wide  
• Guest records included with no per-record charge at launch, subject to fair use, account status and retention  
• Saved audiences  
• Multi-Location Capture, Feedback and reporting  
• Location and consolidated Weekly Briefs  
• 500 AI actions/month  
• 10,000 Email credits/month  
• 350 SMS credits/month  
• Priority Email support

### Group — £199/month + VAT

Gross at 20% VAT: **£238.80/month**

Included:

• Up to 5 Locations  
• 25 operator users  
• 1 Published Guest Form plus 1 Draft per Location  
• Up to 50 active QR placements per Location  
• Up to 25 active Offers account-wide  
• Guest records included with no per-record charge at launch, subject to fair use, account status and retention  
• Consolidated multi-Location reporting  
• Area-manager and Location-scoped permissions  
• Group Campaign scope and account-wide audiences  
• 1,500 AI actions/month  
• 25,000 Email credits/month  
• 700 SMS credits/month  
• Priority support and assisted onboarding

### Additional Group Location — £39/month + VAT

Gross at 20% VAT: **£46.80/month**

Adds:

• 1 Location  
• 2 operator users  
• 1 Published Guest Form plus 1 Draft  
• Up to 50 active QR placements  
• 100 AI actions/month  
• 5,000 Email credits/month  
• 100 SMS credits/month

Self-serve Group limit: 30 Locations.

____

## 4. Annual billing

Annual discount: **15%**

| Plan | Net annual price | VAT | Gross annual price |
|---|---:|---:|---:|
| Starter | £398.00 | £79.60 | £477.60 |
| Growth | £1,010.00 | £202.00 | £1,212.00 |
| Group | £2,030.00 | £406.00 | £2,436.00 |
| Additional Group Location | £398.00 | £79.60 | £477.60 |

Rules:

• Paid in advance.  
• Credits released monthly.  
• No twelve-month credit allocation on day one.  
• Cancellation takes effect at annual renewal.  
• Monthly-to-annual and annual-to-monthly changes take effect at renewal.  
• Same-cadence plan upgrades may take effect immediately.  
• Downgrades take effect at renewal.

____

## 5. AI actions

Public label:

**AI actions**

One action is charged only when one usable operator-triggered output is returned.

| Action | AI actions |
|---|---:|
| Automatic Feedback classification | 0 |
| Automatic safety, permission or suppression check | 0 |
| Standard scheduled Weekly Brief | 0 |
| AI Assistant answer | 1 |
| Recovery response Draft | 1 |
| Campaign copy or brief | 1 |
| Operator-triggered summary | 1 |
| Operator-triggered recommendation | 1 |
| Regenerate | 1 |
| Failed, timed-out or blocked request | 0 |

Guest voice transcription does not consume restaurant AI actions in v1.

____

## 6. Email credits and provider architecture

One Email credit equals:

**one eligible Campaign recipient accepted by the Campaign Email provider**

Burn rules:

| Action | Email credits |
|---|---:|
| Authentication, setup and system Email | 0 |
| One-to-one private Feedback recovery Email | 0 |
| Campaign Email accepted for one eligible recipient | 1 |
| Recipient removed before provider submission | 0 |
| Provider-accepted Email that later bounces | 1 |
| Idempotently prevented duplicate | 0 additional |

Recovery Email controls:

• Valid Feedback follow-up permission  
• One initial send  
• One operator-reviewed resend maximum  
• No bulk recovery use  
• Suppression always enforced

### Campaign Email architecture

Do not use shared Resend Broadcast Contacts as Tummly’s guest source of truth.

Build:

• Provider-neutral `CampaignEmailProvider` adapter  
• Tummly-owned recipient queue  
• Tummly-owned consent and suppression  
• Tenant- and Location-scoped unsubscribe  
• One-click unsubscribe headers  
• Recipient-level provider and audit references  
• Configurable provider binding  
• Feature flag for live Campaign Email

Initial technical route:

• Transactional/system Email: Resend Email API  
• Campaign Email: provider adapter binding confirmed during deployment

Production Campaign Email is enabled only when:

• The provider permits the intended multi-tenant marketing use  
• Tenant-specific unsubscribe has passed end-to-end testing  
• Complaint and bounce monitoring is active  
• The live provider account and sending domain are configured

This is a technical provider prerequisite and does not change customer pricing.

Provider protection:

• Stop or restrict a tenant when complaint rate reaches 0.08% or bounce rate reaches 4%, or sooner where risk warrants.  
• Keep thresholds configurable so a stricter provider policy can be applied without code changes.

____

## 7. SMS credits

One SMS credit equals:

**one Twilio billable SMS segment**

| Action | SMS credits |
|---|---:|
| Operator authentication SMS | 0 guest credits |
| Recovery SMS | Accepted segments |
| Campaign SMS | Accepted segments per eligible recipient |
| Recipient removed before submission | 0 |
| Provider-accepted message later undelivered | Accepted segments remain charged |
| Idempotently prevented duplicate | 0 additional |

Composer requirements:

• Character count  
• GSM-7 or Unicode state  
• One segment: up to 160 GSM-7 or 70 UCS-2 characters  
• Concatenated segment estimate: 153 GSM-7 or 67 UCS-2 characters per segment  
• Eligible recipients  
• Estimated total credits  
• Current balance  
• Balance after send

____

## 8. Top-up packs

All prices are exclusive of VAT.

### AI

| Pack | Net | VAT | Gross |
|---|---:|---:|---:|
| 100 | £5.00 | £1.00 | £6.00 |
| 500 | £15.00 | £3.00 | £18.00 |
| 2,000 | £39.00 | £7.80 | £46.80 |

### Email

| Pack | Net | VAT | Gross |
|---|---:|---:|---:|
| 5,000 | £10.00 | £2.00 | £12.00 |
| 20,000 | £30.00 | £6.00 | £36.00 |
| 50,000 | £60.00 | £12.00 | £72.00 |

### SMS

| Pack | Net | VAT | Gross |
|---|---:|---:|---:|
| 100 | £12.00 | £2.40 | £14.40 |
| 500 | £55.00 | £11.00 | £66.00 |
| 1,000 | £100.00 | £20.00 | £120.00 |
| 5,000 | £450.00 | £90.00 | £540.00 |

5,000 SMS requires Group or manual approval.

Purchased credits:

• Require an active paid plan  
• Expire 12 months after purchase  
• Are consumed after included credits  
• Are consumed earliest-expiry-first  
• Are non-transferable  
• Have no cash value  
• Are normally non-refundable after allocation

____

## 9. Credit allocation and reservation

Included credits:

• Release on the subscription anniversary.  
• Do not roll over.  
• Monthly plans release after successful renewal payment.  
• Annual plans release monthly while the annual subscription remains active and paid.  
• A failed monthly renewal does not create a new allowance.  
• Stripe’s authoritative billing anchor is used for 29th–31st dates.

Consumption order:

1. Current-period included credits  
2. Purchased credits, earliest expiry first

Campaign reservation:

1. Revalidate account and Campaign.
2. Revalidate each recipient.
3. Remove invalid, opted-out, suppressed, ineligible and duplicate recipients.
4. Calculate Email recipients or SMS segments.
5. Atomically reserve the full estimate.
6. Submit.
7. Settle accepted units.
8. Release unused units.
9. Record recipient-level outcomes.

Concurrency:

• Reservation must be transactional.  
• Two simultaneous Campaigns cannot spend the same credits.  
• Every reservation references the exact allocation entries used.  
• Release returns units to the same allocation if still valid.  
• Expired allocations are not revived.

Partial send:

• Status: `partially_sent`  
• Stop uncontrolled processing  
• Charge accepted units only  
• Release unused reservation  
• Show accepted, skipped, failed and not-attempted counts  
• Require reviewed retry  
• Prevent duplicates

____

## 10. Upgrade, downgrade and Location changes

### Same-cadence upgrade

• Immediate  
• Prorated net charge plus VAT  
• Allocate only the positive incremental allowance  
• Whole credits only  
• Formula: floor of incremental monthly allowance multiplied by remaining-period ratio  
• Never allocate a second full monthly allowance

### Downgrade

• Effective at renewal  
• Must resolve excess Locations and users before completion  
• Existing period entitlements remain until renewal

### Add Group Location

• Immediate  
• Prorated charge  
• Prorated incremental credits  
• Location entitlement active after payment succeeds

### Remove Group Location

• Effective at renewal  
• Historical Location data retained according to account state and retention  
• No new activity after entitlement ends

### Billing cadence change

• Monthly to annual: renewal  
• Annual to monthly: annual renewal

____

## 11. Out-of-credit behaviour

Warnings:

• 80%  
• 90%  
• 100%

At 100%:

• Block the affected channel only  
• No negative balance  
• No postpaid overage  
• Keep unaffected channels available  
• Keep historical data available  
• Offer top-up or plan change

____

## 12. VAT and money handling

Production VAT configuration:

• Jurisdiction: GB  
• Rate: 20%  
• Tax behaviour: exclusive  
• Currency: GBP  
• Money storage: integer pence  
• VAT calculated and rounded at invoice-line level  
• Subscriptions: standard rated  
• Credit packs: standard rated  
• QR materials and delivery charged by Tummly: standard rated  
• Paid setup and digital assets: standard rated

Development proceeds with VAT enabled.

Before the first live paid transaction, configure:

• Actual VAT registration number  
• Actual VAT effective date  
• Legal entity name  
• Registered address

Paid Checkout fails closed if required VAT details are absent.

Top-up VAT is charged when the pack is purchased. Credit consumption does not create a second VAT charge.

Invoice numbering:

• Invoice: `TM-YYYY-000001`  
• Credit note: `TCN-YYYY-000001`

VAT invoice fields:

• Unique sequential number  
• Invoice date and tax point  
• Supplier legal name and address  
• VAT number  
• Customer business name and address  
• Line description and quantity  
• Net amount  
• VAT rate and amount  
• Gross total  
• GBP  
• Payment status

____

## 13. Stripe implementation

Use:

• Stripe Checkout or Payment Element for first paid conversion  
• Stripe Billing for subscriptions  
• Stripe Customer Portal for payment methods, invoices and cancellation  
• Tummly-controlled flow for plan and Location changes  
• Webhooks as payment source of truth

Do not allow unrestricted plan changes in the Stripe Portal.

Required webhook handling:

• `checkout.session.completed`  
• `invoice.paid`  
• `invoice.payment_failed`  
• `customer.subscription.created`  
• `customer.subscription.updated`  
• `customer.subscription.deleted`  
• `charge.refunded`  
• `charge.dispute.created`  
• `charge.dispute.closed`

Entitlements activate from successful payment events, not only Checkout completion.

Requirements:

• Verify webhook signatures  
• Use idempotency keys  
• Store Stripe IDs, not card data  
• Reconcile Stripe and internal ledger daily  
• Never allocate credits twice

____

## 14. Failed payment, chargeback and account state

Dunning:

• Day 0: `past_due`, notify, first retry  
• Day 3: retry and persistent warning  
• Day 7: block new Campaign and recovery sends  
• Day 10: Soft lock  
• Day 24: Dormant

Chargeback:

• Restrict new paid usage and purchases  
• Preserve historical data  
• Reverse unused top-up credits linked to the disputed payment  
• If disputed credits were consumed, restrict the account and require manual resolution  
• Do not create an unaudited negative balance

Pilot expiry:

• Day 30: Soft lock  
• Day 45: Dormant  
• Dormant retention: 90 days

During Soft lock, existing Feedback links remain live.

During Dormant status, QR links show a clear restaurant-branded unavailable state.

____

## 15. Starter-kit economics

Physical starter-kit entitlement:

• One per Billing Account lifetime  
• Pilot or first paid activation, whichever occurs first  
• Not one per plan renewal  
• Not one per additional Location  
• Additional Location self-print assets are included  
• Physical reorders are paid

Dispatch gate:

• Verified business  
• Verified UK Location  
• Published Guest Form  
• Confirmed placement intent  
• Approved artwork  
• Valid delivery address  
• No duplicate prior entitlement

Landed-cost cap: **£25**

If the current supplier quote exceeds the cap, operations must reduce pack scope or obtain a lower quote before dispatch. This does not change the software billing model.

____

## 16. Pricebook versioning

Pricebook ID:

`TUMMLY-UK-GBP-2026-08-V3`

Rules:

• Store money in pence.  
• Never edit a live Stripe Price amount.  
• Create a new Price and lookup key for future price changes.  
• Store `pricebook_version` on subscriptions, invoices, entitlements, allocations and top-up orders.  
• Existing paid periods retain their contracted price.  
• Future migration is explicit and auditable.  
• No hard-coded price scattered through application code.  
• UI reads effective pricebook configuration.

This design allows future changes without rebuilding the billing engine.

____

## 17. Margin and monitoring rules

Internal conservative planning assumptions:

• SMS: £0.06 per accepted segment  
• AI: £0.003 per completed action  
• Campaign/transactional Email: £0.001 per accepted recipient  
• Base support/operations budget: £10 per paid account/month  
• Base support budget per additional Location: £4/month  
• Base shared platform budget: £500/month  
• Starter-kit cost cap: £25  
• Pilot onboarding budget: £15

These are internal planning inputs, not public prices.

Monitoring:

• Review after 10 paid operators  
• Review after 50 paid operators  
• Review if average SMS segments exceed 1.25  
• Review if more than 25% of paid accounts exhaust SMS  
• Review if Campaign Email provider cost exceeds 20% of Email pack revenue  
• Review if Pilot-to-paid conversion falls below 25%  
• Review if starter-kit landed cost exceeds £25  
• Review if base-case contribution after allocated support falls below 50%

A review may create a future pricebook. It does not alter the active paid period.

____

## 18. Permissions

Account Owner:

• Buy plan and top-ups  
• Upgrade, downgrade and cancel  
• Manage billing contacts  
• View invoices and usage

Billing Admin:

• Buy top-ups  
• Update payment method  
• View invoices and usage  
• Cannot delete the account unless separately authorised

Location Manager:

• View Location usage  
• Draft Campaigns  
• Cannot buy credits or change plan unless granted billing permission

Reporting User:

• Read-only reporting  
• No billing changes

Tummly Support:

• View billing state  
• Cannot reveal full payment data  
• Manual credit adjustment requires elevated permission, reason and audit

____

## 19. Analytics and accounting data

Required events:

• `pricing_viewed`  
• `plan_selected`  
• `checkout_started`  
• `checkout_completed`  
• `checkout_failed`  
• `subscription_created`  
• `subscription_renewed`  
• `subscription_upgraded`  
• `subscription_downgrade_scheduled`  
• `subscription_cancelled`  
• `credit_allocated`  
• `credit_reserved`  
• `credit_consumed`  
• `credit_released`  
• `credit_expired`  
• `topup_purchased`  
• `topup_refunded`  
• `campaign_blocked_insufficient_credit`  
• `campaign_partially_sent`  
• `soft_lock_entered`  
• `dormant_entered`  
• `invoice_issued`  
• `credit_note_issued`  
• `manual_credit_adjusted`

Monthly management reporting:

• Net subscription revenue  
• VAT collected  
• Top-up revenue  
• Payment and Billing fees  
• SMS, Email and AI COGS  
• Hosting and fixed provider cost  
• Support cost  
• Starter-kit subsidy  
• Contribution by plan  
• Contribution per active Location  
• Pilot conversion  
• Top-up adoption  
• Credit utilisation  
• Churn  
• Chargebacks and refunds

The data model must support statutory accounting without trying to replace the accountant.

____

## 20. Acceptance criteria

Approved implementation requires:

• UK-only scope enforced  
• GBP-only billing  
• 20% VAT exclusive pricing  
• Paid Checkout blocked until real VAT values exist  
• Public prices match this pack  
• Annual prices match this pack  
• Entitlements match this pack  
• Top-up packs match this pack  
• AI, Email and SMS burn rules match this pack  
• Shared Resend Broadcast Contacts are not used as tenant guest truth  
• Campaign Email provider adapter exists  
• Email consent and unsubscribe remain tenant scoped  
• SMS segments are estimated correctly  
• Included credits do not roll over  
• Purchased credits expire after 12 months  
• Purchased credits use earliest-expiry-first  
• Pilot starts on activation, not account creation  
• Pilot does not auto-convert  
• Only one starter kit per Billing Account lifetime  
• Campaigns reserve credits atomically  
• Skipped recipients cost zero  
• Partial send is explicit  
• Negative balances are impossible  
• Monthly renewal payment controls credit release  
• Annual credits release monthly  
• Upgrade credits are prorated incrementally  
• Downgrades take effect at renewal  
• Stripe webhooks are verified and idempotent  
• VAT invoices and credit notes are generated correctly  
• Pricebook version is stored on all commercial records  
• Soft-lock and Dormant states work  
• All placeholder prices and balances are removed

____

## 21. Go-live configuration checklist

These values must be inserted before live paid use:

• HMRC VAT number  
• HMRC VAT effective date  
• Legal entity name and registered address  
• Stripe Product and Price IDs  
• Stripe webhook secret  
• Live Resend, Twilio and Azure credentials  
• Campaign Email provider binding  
• Verified sending domain  
• Complaint and bounce monitoring  
• Live invoice email and support contact

This checklist does not reopen the signed-off commercial model.
