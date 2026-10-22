# Commercial catalog is DB Admin-published

UK SaaS commercial rates (catalog plans, top-up packs, Group extra location, and custom-plan unit rates) live in SQL as versioned rows. Staff publish a catalog version from Admin. Runtime `IPricebookCatalog` reads the **Published** version (or an account’s contracted version). The billing-pack JSON under `docs/product/billing-pack-v3.0/` is **seed and history only**, not the live runtime source.

We rejected keeping repo JSON as runtime source (ADR 0035): product requires Admin-editable rates without a deploy. We rejected editing a single mutable row with no version history: mid-flight Active accounts would silently change price. We rejected dual JSON-plus-DB as equal sources: two places to change the same numbers. Revolut plan-variation UUIDs for Starter / Growth / Group remain env- or Admin-stored identifiers; the app does not create Revolut products. Custom à-la-carte plans bill via cycle orders (separate from native subscription variations).

Supersedes runtime behaviour of [0035](./0035-pricebook-is-repo-json.md) for price loading. Pack JSON remains the seed for the first Published version.
