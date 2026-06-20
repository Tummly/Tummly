# Frontend: Guest feedback form page

Status: done

## Parent

Build plan from `docs/pending-work.md` Section 4 (Phase 6). Design decisions: ADR-0003 (feedback model shape), CONTEXT.md "Private feedback form" and "Smart Guest Link".

## What to build

The guest-facing frontend. A new public route `/scan/:token` that renders the private feedback form. On mount, the page calls `GET /api/scan/{token}` to fetch the restaurant and location name, then renders the form with the location name displayed. The form has three inputs: guest name, guest contact (email or phone, single field), and feedback message. On submit, it calls `POST /api/scan/{token}/feedback`.

The form is standard for all locations — the same content regardless of which location's QR was scanned. Only the displayed restaurant/location name differs (fetched from the backend in the same response that precedes form render — no separate fetch after the form is visible).

This is a public route — no auth guard. It must be added to `AppRoutes.tsx` outside of `ProtectedRoute` / `RoleRoute`.

## Acceptance criteria

- [x] `/scan/:token` route exists in `AppRoutes.tsx`, public (no auth guard)
- [x] On mount, the page calls `GET /api/scan/{token}` to fetch `{ restaurantName, locationName }`
- [x] Restaurant and location name are displayed on the form before the guest can interact with inputs
- [x] Form has three inputs: name, contact (email or phone), message — all required
- [x] Submit calls `POST /api/scan/{token}/feedback` with `{ guestName, guestContact, comment }`
- [x] Loading state (skeleton or spinner) shows while fetching location metadata
- [x] 404 / "link not found" state shows if the token is invalid or the API returns 404
- [x] Success state shows after a successful submission (e.g. "Thank you for your feedback")
- [x] Error state shows if submission fails (e.g. rate limited, server error) with a retry option
- [x] Form is disabled during submission to prevent double-submit
- [x] Basic client-side validation: all 3 fields non-empty before enabling submit

## Blocked by

- `02-feedback-submission-backend.md` — needs both `GET /api/scan/{token}` and `POST /api/scan/{token}/feedback` endpoints
