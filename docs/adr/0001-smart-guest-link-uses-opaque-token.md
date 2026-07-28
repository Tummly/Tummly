# Smart Guest Link uses an opaque per-location token

> **Superseded in part by [ADR-0020](0020-per-location-qr-codes-opaque-tokens.md).** The opaque `/scan/{token}` invariant remains. Tokens are no longer stored on `RestaurantLocation.LinkToken` (one per location); they live on `QrCode.Token` with five peer codes per Owned location. Historical notes below describe the original single-token design.

The Smart Guest Link is the public URL a guest reaches by scanning a location's QR code. We decided to key it on an opaque random token stored per `RestaurantLocation`, not the location's sequential numeric primary key. URLs become `https://tummly.com/scan/{token}`.

The numeric ID made links enumerable — incrementing the integer exposed every Tummly location and enabled automated feedback spam. A random token makes enumeration computationally infeasible and survives location renames without breaking printed QR codes. We rejected a human-readable slug because slug collisions across operators require uniqueness logic and renaming a location would invalidate already-printed QRs. Operators never type the URL (they scan QR codes), so readability carries no value.

The token is generated once during Guest Loop provisioning and stored on `RestaurantLocation`. It does not replace the numeric primary key, which remains the internal identifier.

## Implementation notes

- **Migration backfill:** `AddLinkTokenToRestaurantLocation` assigned existing rows via `CONVERT(nvarchar(32), NEWID(), 2)` — 32-character hexadecimal strings.
- **Runtime generation:** `SmartGuestLinkService.GenerateTokenAsync()` produces 32-character crypto-random alphanumeric strings with uniqueness retry.
- Both formats satisfy the opaque, non-enumerable invariant. Existing hex tokens are **not** re-tokenized, so printed QR codes keep working.
- **Later cutover:** `AddQrCodesRetireLinkToken` moved each location’s `LinkToken` onto a Smart Guest `QrCode` and minted four placement codes, then dropped `LinkToken` — see ADR-0020.
