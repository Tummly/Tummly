# Backend: Guest feedback submission endpoint + Feedback model

Status: done

## Parent

Build plan from `docs/pending-work.md` Section 4 (Phase 1 + Phase 3). Design decisions: ADR-0001 (opaque token), ADR-0003 (feedback model shape), CONTEXT.md "Private feedback form" and "Smart Guest Link".

## What to build

The guest-facing backend. A guest scans a QR code → hits the Smart Guest Link → the backend resolves the token to location metadata and serves it for the form → the guest submits feedback → the backend stores it.

A new `Feedback` model:

```
Feedback
├── Id                     (int, PK)
├── RestaurantLocationId   (int, FK → RestaurantLocation)
├── GuestName              (string, required, max 150)
├── GuestContact           (string, required, max 100 — email or phone, single field)
├── ContactType            (enum: Email | Phone | Unknown — heuristic, set at submission)
├── Comment                (string, required, max 1000)
└── CreatedAt              (DateTime, defaults to UtcNow)
```

A new `ScanController` with two public (unauthenticated) endpoints:

- `GET /api/scan/{token}` — looks up `RestaurantLocation` by `LinkToken`, returns `{ restaurantName, locationName }`. Returns 404 if the token doesn't resolve.
- `POST /api/scan/{token}/feedback` — resolves the token to a location, validates the three required fields (`GuestName`, `GuestContact`, `Comment`), detects `ContactType` heuristically (`@` present → `Email`, digits-only → `Phone`, else `Unknown`), creates a `Feedback` row. Per-token rate limit: max 10 submissions per hour.

The contact field is a single string holding either an email or a phone number — the guest's choice. Do not validate against a strict email or phone format; only check non-empty and max length. The `ContactType` classification is heuristic, not validation.

## Acceptance criteria

- [x] `Feedback` model exists with the fields listed above
- [x] `ContactType` is an enum with `Email`, `Phone`, `Unknown` values
- [x] `DbSet<Feedback>` is registered in `ApplicationDbContext` with FK to `RestaurantLocation`
- [x] EF Core migration creates the `Feedback` table
- [x] `ScanController` exists with `GET /api/scan/{token}` and `POST /api/scan/{token}/feedback`
- [x] `GET /api/scan/{token}` returns `{ restaurantName, locationName }` for a valid token, 404 for invalid
- [x] `POST /api/scan/{token}/feedback` validates all 3 fields are non-empty and within max length
- [x] `ContactType` is detected heuristically at submission time (`@` → Email, digits-only → Phone, else Unknown)
- [x] Per-token rate limit: max 10 submissions per hour; 429 response when exceeded
- [x] Neither endpoint requires authentication
- [x] Submitting feedback for a valid token creates a `Feedback` row with the correct `RestaurantLocationId`
- [x] Submitting feedback for an invalid token returns 404

## Blocked by

- `01-provisioning-token-generation.md` — needs `LinkToken` on `RestaurantLocation` to resolve tokens
