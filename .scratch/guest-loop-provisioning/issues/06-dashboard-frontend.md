# Frontend: Operator dashboard with location switcher + per-location data

Status: done

## Parent

Build plan from `docs/pending-work.md` Section 4 (Phase 7). Design decisions: ADR-0002 (workspace per-restaurant, in-dashboard location switcher), CONTEXT.md "Operator dashboard". Replaces the placeholder dashboards (D1-D5 in pending-work.md).

## What to build

Replaces the current placeholder operator dashboards (`src/components/dashboard/single/Dashboard.tsx` — 9 lines, and `src/components/dashboard/multi/Dashboard.tsx` — 131 lines static mockup) with functional dashboards that integrate with the backend APIs.

**Multi-dashboard (`/multi-dashboard`):**
- On mount, fetches `GET /api/restaurant/locations` and populates a location switcher dropdown
- Defaults to the first location (by `CreatedAt` ascending)
- When the operator switches locations, the dashboard updates to show the selected location's data
- Per location, displays:
  - **Feedback stats:** total submission count + recent 5 submissions (from `GET /api/feedback?locationId={id}`) — name, contact, comment, timestamp
  - **QR download:** button that calls `GET /api/qr/download?locationId={id}` (authenticated) and downloads `QR_{LocationName}.png`
  - **Smart Guest Link preview:** constructs `{baseUrl}/scan/{linkToken}` from the location list response and provides an "Open" button that opens the guest form in a new tab

**Single-dashboard (`/single-dashboard`):**
- Same as multi-dashboard but without the location switcher (the operator has one location)
- Fetches the single location's data directly

These dashboards render inside `MainLayout`'s `<Outlet>` — they should NOT render their own sidebar/header chrome (the current multi-dashboard mockup does this, causing duplicated chrome).

## Acceptance criteria

- [x] Multi-dashboard fetches `GET /api/restaurant/locations` on mount
- [x] Location switcher dropdown is populated with the operator's locations
- [x] Default selected location is the first by `CreatedAt` ascending
- [x] Switching locations updates all per-location data on the dashboard
- [x] Feedback stats show total count and recent 5 submissions (name, contact, type, comment, timestamp)
- [x] QR download button triggers `GET /api/qr/download?locationId={id}` and downloads the PNG
- [x] Smart Guest Link preview "Open" button opens `{baseUrl}/scan/{linkToken}` in a new tab
- [x] Single-dashboard shows the same per-location data without a location switcher
- [x] Neither dashboard renders its own sidebar/header — they use `MainLayout`'s chrome via `<Outlet>`
- [x] Loading states show while fetching data
- [x] Empty states show when a location has no feedback
- [x] Error states show if API calls fail

## Blocked by

- `04-qr-download-authenticated.md` — needs authenticated QR download endpoint
- `05-dashboard-api.md` — needs location list + feedback stats endpoints
