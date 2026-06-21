# Backend: Authenticated QR download with token-based link

Status: done

## Parent

Build plan from `docs/pending-work.md` Section 4 (Phase 4). Design decisions: ADR-0001 (opaque token), CONTEXT.md "QR code", "Smart Guest Link", and "`POST /api/auth/setup-account`". Reframed findings: B8 (domain hardcoded), B9 (unauthenticated), B10 (localhost URL), B11 (filename not sanitized).

## What to build

The operator-facing QR download flow is secured and the QR encodes the token-based Smart Guest Link instead of the hardcoded numeric-ID URL.

Three changes to `QrController`:

1. **Add `[Authorize]`** to both `GET /api/qr/info` and `GET /api/qr/download`. Add an ownership check: the authenticated user's `OwnedRestaurants` must contain the location's `RestaurantId`. A different operator's location → 403.

2. **Use `Frontend:BaseUrl` config** instead of the hardcoded `https://tummly.com` at `QrController.cs:54`. The QR URL becomes `{Frontend:BaseUrl}/scan/{location.LinkToken}` — read `Frontend:BaseUrl` from `IConfiguration` (same pattern as `AdminService.GetFrontendBaseUrl()`). Fetch the `RestaurantLocation` by `Id` to read its `LinkToken`.

3. **Sanitize the QR filename** — `QR_{location.LocationName}.png` in the `Content-Disposition` header. Apply `UrlEncode` or sanitize newlines, quotes, and non-ASCII characters to produce a valid header value.

Also fix `WorkspaceController.cs:34` — replace the hardcoded `http://localhost:5204/api/qr/download?locationId={id}` with a config-based URL (use `Frontend:BaseUrl` for the domain, or construct the API URL from configuration).

## Acceptance criteria

- [x] `QrController` has `[Authorize]` on both `info` and `download` endpoints
- [x] Ownership check: authenticated operator must own the location's restaurant; else 403
- [x] Unauthenticated requests to either QR endpoint return 401
- [x] QR URL is constructed from `Frontend:BaseUrl` config + `location.LinkToken`, not hardcoded `tummly.com`
- [x] QR PNG encodes `{Frontend:BaseUrl}/scan/{linkToken}` — verifiable by decoding the generated PNG
- [x] `Content-Disposition` filename is sanitized (no raw newlines, quotes, or non-ASCII in the header)
- [x] `WorkspaceController.cs:34` no longer hardcodes `localhost:5204` — uses config-based URL
- [x] Project builds, existing tests pass

## Blocked by

- `01-provisioning-token-generation.md` — needs `LinkToken` on `RestaurantLocation` to encode in the QR
