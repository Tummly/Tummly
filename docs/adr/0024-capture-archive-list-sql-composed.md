# Capture Archive list uses SQL-composed filters, not an unbounded dump

**Capture Archive** must stay exact at the query seam (pagination totals, filter/sort including metric sorts, Archived-by facets, Restore conflict flags) without loading every archived **QR code** for the restaurant into memory or re-implementing the list kernel in TypeScript. We deepen a **Capture Archive list module** so filter, sort, page, and all-time engagement aggregates compose as EF-translatable predicates / joins; `canRestore` still projects after the page hydrates against live Active/Paused occupancy (small set). We extend `GET /api/capture/placements/archived` with Guests-style query params and a paged response (including `archiverOptions`). The frontend **Capture Archive module** becomes a refetch-driven consumer of that page. Archived-date presets resolve with operator `utcOffsetMinutes` (Guests-aligned), replacing the previous client UTC-day semantics.

We rejected keeping the unbounded dump while only paging display rows in the client — that leaves the load and dual list kernels in place. We rejected a soft-max without a real pager. We rejected hydrating scan/feedback aggregates only after `Skip`/`Take` while advertising highest-scans / highest-feedback / most-recent-activity sorts. We rejected a separate archivers catalog endpoint for this cut (facets ride the list response). Location performance SQL-compose and Capture location snapshot remain separate ADRs.

## Consequences

- List predicates and metric sorts must stay EF-translatable; display string formatting may remain a thin client mapper after the page returns.
- Add or confirm a covering index on archived QR lookup (`RestaurantLocationId`, `Status`, `ArchivedAt`) with this cut if query plans need it; scan/feedback covering indexes wait until the join shape is proven.
- Future architecture reviews should not re-suggest an in-memory full-scope Capture Archive list without new evidence.
- Sibling: ADR-0016 (Guests list SQL-compose). Frontend cutover is specified beside this deepen (Capture Archive module refetch + Guests-style pager).
