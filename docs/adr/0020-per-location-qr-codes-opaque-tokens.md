# Per-location QR codes use opaque tokens (multi-link model)

Guest entry stays `/scan/{token}` with an opaque random token — the invariant from ADR-0001. What changed is **where tokens live** and **how many** exist per **Owned location**.

Each location has five peer **QR code**s (Counter card, Packaging sticker, Delivery insert, Window sticker, Smart Guest), each with its own token on `QrCode.Token`. **Smart Guest Link** is the operator-facing name for the Smart Guest type’s **QR link** only (copy + preview on Home). Placement types are for physical materials via the **Tummly Shop**; operators do not download QR PNGs from the dashboard.

`RestaurantLocation.LinkToken` is retired. Existing LinkTokens were backfilled onto each location’s Smart Guest `QrCode` so printed Smart Guest URLs keep working. Guest Loop provisioning mints all five **Active** codes per new location.

Scan resolve looks up `QrCodes` (Active only for new guest traffic). **Feedback** and successful scan events store `QrCodeId` for source attribution — not a denormalized QR type and not a location-level token column.

We rejected keeping a dual-source window (`LinkToken` + `QrCode`) because two writers invite drift. We rejected a separate `QrLink` table while code and token stay 1:1. We rejected operator PNG download in favour of Shop stickers.

**Supersedes** the storage and cardinality parts of [ADR-0001](0001-smart-guest-link-uses-opaque-token.md) (token on `RestaurantLocation`, one token per location). ADR-0001’s opaque-token-over-numeric-id rationale remains in force.

## Consequences

- Uniqueness: global unique on `QrCode.Token`; filtered unique on `(RestaurantLocationId, QrType)` for Active+Paused (only Archived frees the type slot).
- `GET /api/restaurant/locations` `guestUrl` is built from the location’s Active Smart Guest token.
- Capture sidenav stub is the future home for QR management UI; this ADR does not specify that UI.
- Update product docs and CONTEXT when touching guest entry or provisioning — do not reintroduce `LinkToken` or dashboard QR PNG download.
