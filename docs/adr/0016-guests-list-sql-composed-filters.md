# Guests list uses SQL-composed filters, not in-memory DeriveRow

The Operator Guests list and export must stay exact at the query seam (pagination totals, Smart Group counts, **Guest overview**, CSV soft-max) without loading every **Location Guest** for the effective location scope into memory. We deepen `GuestsListService` so filter, sort, page, and counts compose as EF-translatable predicates / aggregates; `LocationGuestProjections` still shapes display strings after a page (or export batch) hydrates. The HTTP contract stays frozen — no frontend change required for this deepen.

We rejected a denormalized list read model for now: deferred Smart Groups and recovery membership are still product-incomplete, and a projection would add write-path fan-out before the live filter set is stable. We rejected keeping full-scope `ToListAsync` + in-memory `DeriveRow` for counts while only SQL-paging the table body — that leaves the unbounded load in place. We rejected a separate export path: list and export share one filtered query builder (export omits page `Skip`/`Take`, caps at `ExportSoftMaxRows`). The shallow `GuestsExportService` pass-through is collapsed onto the list module (one adapter = hypothetical seam).

## Consequences

- Shared list predicates must stay EF-translatable and aligned with `LocationGuestProjections` marketing / eligibility rules so Profile labels and list filters do not drift.
- Add index `LocationGuests (RestaurantLocationId, CreatedAt)` with this cut; Feedback covering indexes wait until the join shape is proven.
- Future architecture reviews should not re-suggest an in-memory full-scope Guests list without new evidence; a materialised read model remains a later option if write-side projection cost becomes cheaper than complex SQL.
