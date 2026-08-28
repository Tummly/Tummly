# Tummly UK Pricing and Billing Production Pack v3.0

**Status:** APPROVED FOR DEVELOPMENT  
**Audit date:** 2026-08-06  
**Supersedes:** v2.0

## Files and use cases

1. `Tummly_UK_Pricing_Billing_Final_Audit_v3.0.md`  
   Executive audit record. Explains what was checked, what was corrected and why v3.0 is the approved pack.

2. `Tummly_UK_Pricing_Billing_Production_Signoff_v3.0.md`  
   Master product, commercial, VAT, payment, credit and engineering specification. This is the primary source of truth.

3. `Tummly_Channel_Credits_Questionnaire_COMPLETED_v3.0.md`  
   Direct completed answer to the original developer questionnaire. Use it to close the decision gate that requested product-owned numbers.

4. `Tummly_UK_Pricing_Unit_Economics_v3.0.xlsx`  
   Formula-based financial model. Includes plans, top-ups, low/base/high operating scenarios, Pilot payback, pricebook, controls and sources.

5. `tummly_uk_billing_config_v3.0.json`  
   Machine-readable commercial configuration for engineering. It includes prices in pence, entitlements, burn rates, lifecycle, provider adapter, tax and ledger rules.

6. `Tummly_UK_Billing_Team_Handoff_Message_v3.0.md`  
   Slack-ready two-paragraph message to Salman and Mohamed.

7. `MANIFEST_SHA256_v3.0.txt`  
   File-integrity hashes for the pack.

## Implementation order

1. Read the final audit.
2. Treat the master sign-off as authoritative.
3. Use the completed questionnaire to close the original open decision record.
4. Create Stripe Products and Prices from the workbook Pricebook sheet.
5. Implement the JSON configuration and internal ledger.
6. Update the designs.
7. Insert live VAT and provider credentials.
8. Run the acceptance criteria in the master sign-off.
