# Pricebook is repo JSON identified by pricebook.id

UK commercial prices, allowances, top-ups, burn rates, and lookup keys live in `docs/product/billing-pack-v3.0/tummly_uk_billing_config_v3.0.json`. The backend loads that file. Rows stamp `pricebook.id` as `pricebook_version`. There is no price catalog table.

We rejected a database catalog as the runtime source: operators could edit £39 / £99 / £199 outside git, and the pack would drift. We rejected dual JSON-plus-snapshot-row: two places to change the same numbers. Revolut product ids stay off this file; Revolut map ticket **06** resolves lookup keys per environment.

Product lock: `.scratch/credit-ledger-backend/issues/02-pricebook-configuration-and-versioning.md`.
