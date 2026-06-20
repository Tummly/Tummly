# Backend: Operator dashboard API (location list + feedback stats)

Status: done

## Parent

Build plan from `docs/pending-work.md` Section 4 (Phase 5). Design decisions: ADR-0002 (workspace per-restaurant, dormant), ADR-0003 (feedback per-location), CONTEXT.md "Operator dashboard" and "Workspace selection".

## What to build

Two new authenticated endpoints for the operator dashboard:

1. **`GET /api/restaurant/locations`** — returns the authenticated operator's restaurant's locations. Each item includes `id`, `locationName`, `address`, `linkToken` (so the frontend can construct the Smart Guest Link preview URL client-side), and any other relevant display fields. The operator is identified from the JWT; their `OwnedRestaurants` → `Locations` is the data source. Since every operator owns one restaurant today, this returns that restaurant's locations.

2. **`GET /api/feedback?locationId={id}`** — returns feedback stats and recent submissions for a specific location. Response shape:

```json
{
  "total": 42,
  "recent": [
    {
      "id": 101,
      "guestName": "Jane Doe",
      "guestContact": "jane@example.com",
      "contactType": "Email",
      "comment": "Great service!",
      "createdAt": "2026-06-20T12:00:00Z"
    }
  ]
}
```

`recent` is the top 5 submissions by `CreatedAt` descending. Both endpoints require authentication + ownership check (the location must belong to the authenticated operator's restaurant).

## Acceptance criteria

- [x] `GET /api/restaurant/locations` exists, requires `[Authorize]`
- [x] Returns the authenticated operator's locations with `id`, `locationName`, `address`, `linkToken`
- [x] Returns 401 if unauthenticated
- [x] `GET /api/feedback?locationId={id}` exists, requires `[Authorize]`
- [x] Returns `{ total, recent: [...] }` where `recent` is top 5 by `CreatedAt` desc
- [x] Each recent item includes `id`, `guestName`, `guestContact`, `contactType`, `comment`, `createdAt`
- [x] Ownership check: operator requesting a location they don't own → 403
- [x] Location with no feedback returns `{ total: 0, recent: [] }`
- [x] Project builds, existing tests pass

## Blocked by

- `02-feedback-submission-backend.md` — needs the `Feedback` table to query
