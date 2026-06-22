# Operator Setup — Address lookup & postcode reconciliation

Stress-test scenarios for manual QA after the feature ships. Derived from design grill session.

## Address autocomplete (async select)

| # | Scenario | Steps | Expected |
|---|----------|-------|----------|
| A1 | Happy path — select suggestion | Type partial address → wait for spinner → pick a suggestion with empty postcode | Address = street + town; Postcode auto-filled |
| A2 | Free text / override | Type an address → pick **Use my address instead** from dropdown | Address stored exactly as typed; override flag set; equivalent to rejecting a reconciliation lock |
| A3 | Spinner / loading | Type 3+ characters, slow network | Dropdown shows circular spinner while fetching (400ms debounce); no stale results flash |
| A4 | Autofill disabled | Focus Address field with browser autofill enabled | `autoComplete="off"` on Address and Postcode; browser does not populate from saved credentials |
| A5 | Design parity | Compare Address field to current `FormFloatingInput` | Same floating label, MapPin when empty, padding, error slot — only behaviour changes |
| A6 | Postcode conflict warning | Enter postcode `B1 1AA` → select suggestion with postcode `M1 4AB` | Inline warning: "Selected Address doesn't match with postcode"; Address updated to selection |
| A7 | Postcode pre-filled, matches | Enter postcode `M1 4AB` → select suggestion also at `M1 4AB` | No warning; no unnecessary reconciliation override |
| A8 | Locked field override | After reconciliation lock → focus Address (read-only) → open menu → **Use my address instead** | Prior typed text restored as subtitle; field editable again; override flag set |

## Postcode reconciliation (on blur)

| # | Scenario | Steps | Expected |
|---|----------|-------|----------|
| P1 | Match — no lock | Address `125 High Street, Manchester` + postcode `M1 4AB` where API agrees on street | Address unchanged; field stays editable |
| P2 | Mismatch — override & lock | Address `London` + postcode `EH1 1AA` (Edinburgh) | Address replaced with postcode lookup result (street + town); field locked; inline note shown |
| P3 | Override escape hatch | Trigger P2 → open Address dropdown → select **Use my address instead** | Address unlocks with operator's typed text; override flag set; can submit |
| P4 | Reconcile after postcode change | Trigger P2 → edit postcode to a matching one → blur | Re-reconciles; lock clears if new result matches |
| P5 | Fuzzy match — unit in block | Address `Unit 4, 125 High Street, Manchester` + postcode returning multiple flats including Unit 4 | No override; Address stays as entered |
| P6 | API range vs specific number | Address `125 High Street` + postcode returns `123–127 High Street` range | Fuzzy overlap → no override (or only override if no overlap — verify agreed rule) |
| P7 | Bad API grouping | Reconciliation overrides to `123–127 High Street` but operator knows `125` is correct | Override link restores operator address; submit succeeds |
| P8 | Multiple premises — best match | Postcode with 8+ results; operator address close to one | Closest match used; no extra picker |
| P9 | Multiple premises — no close match | Postcode with many results; operator address unrelated | First result used + note about multiple addresses; override available |
| P10 | Invalid postcode format | Enter `not-a-postcode` → blur | Existing Zod validation error; no API call |
| P11 | Valid format, unknown postcode | Enter `ZZ99 9ZZ` → blur | Graceful error (no crash); Address not locked |

## Surfaces (all three must behave identically)

| # | Scenario | Steps | Expected |
|---|----------|-------|----------|
| S1 | Single-operator Confirm restaurant | Complete step 2 with lookup + reconciliation | Payload stores Address + Postcode correctly |
| S2 | Multi-operator location card | Add location 2; use autocomplete + reconciliation | Same behaviour per card |
| S3 | Bulk upload review | Import CSV with mixed addresses → edit one row's postcode in review dialog | Reconciliation on blur for that row only; not fired for all rows on dialog open |
| S4 | Bulk upload — no API storm | Import 20 locations → open review | Zero automatic API calls until operator interacts with a field |

## Backend / infrastructure

| # | Scenario | Steps | Expected |
|---|----------|-------|----------|
| B1 | API key not exposed | Inspect network tab during setup | Only `/api/address/*` calls; no Ideal Postcodes key in frontend |
| B2 | Rate limiting | Rapid-fire suggest requests from one IP | 429 or throttle after limit; setup still usable at normal pace |
| B3 | Response cache | Repeat identical suggest query twice | Second request faster; Ideal Postcodes not double-billed (verify via logs/metrics) |
| B4 | UK-wide coverage | Scottish (`EH1 1AA`), Welsh (`CF10 1AA`), English (`SW1A 1AA`) postcodes | All resolve; no England-only filter |
| B5 | Autocomplete cache TTL | Repeat same query within 1 hour, then after 1 hour | Cached within TTL; refreshed after TTL expiry |
| B6 | Postcode cache TTL | Resolve same postcode twice within 24 hours, then after 24 hours | Cached within TTL; refreshed after TTL expiry |
| B7 | Normalized cache keys | Query with case/space differences (`125 high`, ` 125 High `) and postcode variants (`M14AB`, `M1 4AB`) | Same normalized cache entry reused |

## Storage & submit

| # | Scenario | Steps | Expected |
|---|----------|-------|----------|
| T1 | API-selected address format | Select suggestion → complete setup | `RestaurantLocation.Address` = street + town; `Postcode` separate |
| T2 | Free-text address format | Free-text entry + manual postcode → submit | Address stored exactly as typed |
| T3 | Override after mismatch | Override locked address → submit | Submitted values reflect operator's chosen text, not overridden value |
| T4 | Override flag in payload | Use "Use my address instead" → submit | Payload includes per-location override flag; backend stores as-is |
| T5 | Invalid postcode server-side | Bypass client validation → submit bad postcode | Backend rejects with postcode format error |
| T6 | Ideal Postcodes down at submit | Complete form while lookup worked; API down at submit | Setup still succeeds (no hard block on mismatch) |
