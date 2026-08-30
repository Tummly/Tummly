# Tummly UK Pricing and Billing — Final Audit v3.0

**Audit status:** COMPLETE  
**Overall verdict:** APPROVED FOR DEVELOPMENT  
**Supersedes:** v2.0 pack  
**Audit date:** 2026-08-06  
**Authority:** Amin Ordikhani  
**Market:** United Kingdom only

____

## 1. Scope reviewed

The final audit covered:

• Master production sign-off  
• Completed channel-credits questionnaire  
• Unit-economics workbook and formulas  
• JSON billing configuration  
• Team handoff message  
• Plan pricing and entitlements  
• VAT and invoice rules  
• Stripe payment and subscription logic  
• AI, Email and SMS credit definitions  
• Top-up prices and expiry  
• Campaign reservation and partial-send logic  
• Pilot, failed-payment, Soft-lock and Dormant states  
• Fixed-cost, support and Pilot-subsidy sensitivity  
• Provider architecture and multi-tenant risk  
• Cross-file consistency

____

## 2. Audit verdict

The v2.0 pack was directionally strong but was not the safest possible production handoff.

The v3.0 pack corrects the remaining issues and is the version that should be sent to Salman and Mohamed.

Public prices, plan allowances and top-up prices remain unchanged. The corrections primarily improve implementation safety, financial modelling and provider architecture.

____

## 3. Material findings and resolutions

### Finding 1 — Shared Resend Broadcast contacts are not tenant-safe enough for Tummly

Resend’s current Marketing model uses global Contacts linked to one email address across a team. A Contact can belong to multiple Segments, and contact properties and global unsubscribe state are shared.

Tummly’s guest identity, consent and suppression are restaurant-specific.

A shared Resend Broadcast contact could therefore create:

• Cross-restaurant name or property collisions  
• Over-broad global unsubscribe  
• Ambiguous tenant attribution  
• Shared reputation exposure  
• Incomplete per-tenant analytics

**Resolution**

• Do not hard-wire Tummly Campaigns to shared Resend Broadcast Contacts.  
• Build a provider-neutral Campaign Email adapter.  
• Keep Tummly as the source of truth for eligibility, consent, suppression, unsubscribe and tenant attribution.  
• Do not enable live Campaign Email until the provider route confirms tenant-safe marketing delivery.  
• This is a provider binding and deployment check, not a commercial decision or reason to stop development.

### Finding 2 — Expected-use formula understated fixed Email-platform cost

The v2.0 workbook multiplied the entire channel-cost block by expected utilisation, including a fixed Campaign Email reserve.

**Resolution**

• The formula has been corrected.  
• Variable per-unit costs scale with utilisation.  
• Fixed platform and support budgets remain fixed.  
• A dedicated scenario model now tests low, base and high operating-cost cases.

### Finding 3 — Email economics were not sufficiently robust against contact-based pricing

Resend Marketing pricing is contact-based, while Tummly Email credits are recipient-send based.

A 50,000-credit pack cannot safely be priced from a contact-based plan without controlling active-contact cost.

**Resolution**

• Customer billing remains one Email credit per accepted Campaign recipient.  
• Provider economics are separated from customer credits.  
• The production architecture uses a provider adapter rather than a fixed Broadcast-contact dependency.  
• The workbook uses a conservative per-send planning reserve and separate platform-cost scenarios.  
• Provider costs are monitored operationally and do not require public price recoding.

### Finding 4 — Fixed overhead and Pilot acquisition economics were incomplete

The v2.0 margin view did not fully stress:

• Hosting and provider base costs  
• Support and operations  
• Starter-kit subsidy  
• Pilot conversion rate  
• Break-even paid-account count

**Resolution**

The v3.0 workbook adds:

• Low, base and high platform-cost scenarios  
• Support budget per paid account  
• Fixed-cost break-even  
• Pilot subsidy and conversion payback  
• One physical starter-kit entitlement per Billing Account lifetime  
• £25 landed starter-kit cost cap  
• A qualification gate before dispatch

### Finding 5 — Price and rule changes needed stronger immutability

The previous pack said the values were versioned but did not fully specify pricebook behaviour.

**Resolution**

• Every subscription, allocation, invoice and usage record stores a `pricebook_version`.  
• Existing Stripe Prices are never edited.  
• Future prices require new lookup keys and a new effective-dated pricebook.  
• Existing customers remain on their contracted price until migrated deliberately.  
• Development is therefore not trapped if a future commercial change becomes necessary.

### Finding 6 — Credit allocation edge cases were incomplete

The following were added:

• Included credits consumed before purchased credits  
• Purchased credits consumed earliest-expiry-first  
• Atomic reservation to prevent concurrent overspend  
• Reservations linked to exact allocation entries  
• No new monthly allocation after failed renewal  
• Annual plans release credits monthly  
• 29th–31st billing anchors use Stripe’s billing-anchor outcome  
• Upgrade allocations use whole-unit prorated incremental allowances  
• Downgrades and Location removals take effect at renewal  
• Refund and chargeback reversals cannot create an unaudited negative balance

### Finding 7 — Public “unlimited” wording created unnecessary risk

The product should not make an unqualified unlimited-data claim without a defined operating policy.

**Resolution**

Public wording now says:

> Guest records are included with no per-record charge at launch, subject to fair use, account status and the retention policy.

Campaign Drafts have no separate manual limit, but AI-generated work consumes AI actions.

### Finding 8 — VAT sign-off language needed greater precision

The application should be developed with 20% VAT enabled, but Tummly must not issue a live VAT invoice using invented registration data.

**Resolution**

• Development is approved now.  
• VAT is built and tested as active.  
• Live paid Checkout fails closed until the actual HMRC VAT number and effective date are configured.  
• This is a factual deployment prerequisite, not a request to reopen pricing.

____

## 4. Financial verdict

The plan architecture remains commercially defensible.

The model retains:

• £39 Starter  
• £99 Growth  
• £199 Group  
• £39 Additional Group Location  
• 15% annual discount  
• Separate AI, Email and SMS pools  
• Prepaid top-ups  
• No postpaid overage  
• No generic service fee

Using the v3.0 conservative cost assumptions:

• All paid plans remain contribution-positive at full included-channel use.  
• Growth and Group retain stronger operating leverage as Locations increase.  
• SMS remains the main variable-cost exposure.  
• Top-up packs remain contribution-positive after estimated payment and Billing fees.  
• Base-case break-even is achievable at a modest paid-account count, but depends on support discipline and Pilot conversion.  
• The free physical starter kit must remain qualified and limited to one per Billing Account lifetime.

The business should review prices after 10 and 50 paid operators using actual:

• SMS segments  
• Marketing Email provider cost  
• AI cost  
• Support time  
• Pilot conversion  
• Starter-kit landed cost  
• Churn  
• Top-up adoption

This review does not require rebuilding the billing engine because pricebooks and entitlements are versioned.

____

## 5. Final development decision

**APPROVED FOR DEVELOPMENT**

No further product or commercial decision is required before implementation.

The team must not wait for another pricing pack.

The following are factual go-live configuration items:

• Actual HMRC VAT registration number  
• Actual HMRC VAT effective date  
• Stripe Product and Price IDs  
• Stripe webhook secret  
• Resend/Twilio/Azure live credentials  
• Confirmed tenant-safe Campaign Email provider binding

These do not reopen the approved prices, allowances, burn rules or lifecycle logic.
