# Plan-change credits are incremental on the open Included period

Same-cadence **Upgrade** and **Additional Group Location** add grant only `floor(increment × remaining Included period ratio)` as `plan_migration` rows on the open month. They do not remint a full monthly allowance and do not reset the **Included period** or the **Renewal date**. **Downgrade**, cadence change, extra remove, and **Cancel plan** are one **Scheduled change**: paid targets apply on successful renewal `ORDER_COMPLETED` before the included mint; cancel applies from the included-period job when `now ≥ Renewal date`.

We rejected a remaining-year ratio on Annual, a debit clawback at downgrade or extra remove, and applying cancel at each monthly slice. First **Pilot** → paid stays a full mint (ADR 0039), not this prorate.

Product lock: `.scratch/credit-ledger-backend/issues/07-upgrade-downgrade-cadence-change-and-additional-location.md`.
