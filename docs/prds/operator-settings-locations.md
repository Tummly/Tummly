# PRD: Operator Settings — Locations

**Status:** Ready for agent  
**Triage label:** `ready-for-agent`  
**Surface:** Operator dashboard → Settings → Locations  
**UI (shipped, demo data):** `src/components/dashboard/operator/Locations/`  
**Grilling lock date:** 2026-08-31  

---

## Figma

- Header: https://www.figma.com/design/IQfpCZBNsQLbRAaPhnfmul/Guest-Loop-MVP--01-Jul--2026-?node-id=5748-103523&m=dev
- Locations tab (table + KPIs): https://www.figma.com/design/IQfpCZBNsQLbRAaPhnfmul/Guest-Loop-MVP--01-Jul--2026-?node-id=3753-66374&m=dev
- Setup & readiness: https://www.figma.com/design/IQfpCZBNsQLbRAaPhnfmul/Guest-Loop-MVP--01-Jul--2026-?node-id=5748-103603&m=dev
- Activity: https://www.figma.com/design/IQfpCZBNsQLbRAaPhnfmul/Guest-Loop-MVP--01-Jul--2026-?node-id=5748-104239&m=dev

---

## Problem Statement

The Settings **Locations** page UI exists with demo rows. The backend only supports `POST /api/locations` (add). `RestaurantLocation` has no Settings lifecycle (Draft / Active / Paused / Archived), no nominated manager User, no required City, and no list / readiness / activity reads. Capture Pause is a separate concept and must not be confused with Settings Pause — but Settings Pause must still suspend guest-facing work at that venue.

Operators need a real Locations settings module: list and KPIs, setup attention, activity, and lifecycle writes that match the Figma row menus.

---

## Solution

Add a Settings **Locations** backend module (thin controller + domain services) under area `locations`.

1. Persist **Location lifecycle status**, optional **Manager user**, and required **City** (with Postcode) on `RestaurantLocation`.
2. Expose list + KPI + setup-attention + activity reads for the page module.
3. Expose lifecycle writes in the same first slice: Activate draft, Pause (cascade), Resume, Archive (from Paused only), Restore (to Paused), Delete draft, set manager.
4. Persist restaurant **Privacy consent ready** + consent wording; emit operator **and** Privacy / consent events into one **location activity** table (per-location where applicable).
5. Wire **Add location** and **Import locations** (bulk) from the page CTAs.
6. Replace frontend demo seeds with API adapters; server-compose search/filter/sort/page + aggregates (Guests-style).

**Out of this PRD:** Detail / navigation targets only (View location, Edit full surface, QR placements, Feedback, Reports as destination pages) — list returns ids so later routes can deep-link.

---

## Product decisions (locked)

### 1. Location lifecycle (stored)

Canonical stored enum on `RestaurantLocation` (name TBD in code, product values):

| Status | Meaning |
| --- | --- |
| **Draft** | Added; not live. Missing or not yet confirmed required fields to Activate. |
| **Active** | Live in the account for that venue. |
| **Paused** | Suspended; cascade in force until Resume. |
| **Archived** | Soft historical; hidden from live switcher; restorable. |

- Settings lifecycle is **distinct** from `CaptureLocationStatus` (Active / Paused for Capture only).
- **Migration:** every existing row → Settings **Active**. Do not copy Capture Paused into Settings on migrate.

### 2. Pause cascade (hybrid)

When Settings lifecycle becomes **Paused** (and while not **Active**):

| Surface | Behaviour |
| --- | --- |
| **Capture** | Call existing **Pause location capture** (QR restore-set). |
| **Guest form / QR guest resolve** | No new guest intake at that location (gate). |
| **Campaigns** | No schedule / send / resume fire for Campaigns at that `RestaurantLocationId` (gate). |
| **Offer issue** | No new issues whose attach path is that location (Campaign / Recovery / thank-you) (gate). |
| **Guests / Feedback inbox / staff redeem** | Remain available (ops memory). |

**Out of cascade:** Billing, Team, account-wide Offers catalog status, other locations.

**Resume:**

- Set lifecycle **Active**.
- Lift gates.
- Call **Activate location capture** (restore QR set).
- Do **not** auto-resume Campaigns or Offers the operator paused themselves (same spirit as **Resume workspace**).

### 3. Archive / Restore

- **Archive** only from **Paused**. Refuse Archive from Active or Draft.
- Soft status → **Archived**; keep cascade suspension (not Active); hide from location switcher and live Capture lists.
- **Restore** → **Paused** (still suspended). Operator must **Resume** to go live.
- Draft cannot be Archived (use Delete draft / Continue setup).

### 4. Draft and Activate

- **Settings Add location** creates **Draft**.
- **Account Setup / Guest Loop provisioning** creates **Active** (first venues must not strand in Draft).
- **Activate** (Draft → Active) requires: **location name**, **address**, **city**, **postcode**. Manager is optional. Setup checks (QR / privacy) do **not** block Activate.
- **Delete draft:** hard delete only when safe (no guest / feedback / campaign history). Otherwise refuse.

### 5. City and postcode

- **City** and **Postcode** are required when adding or activating a location.
- Persist `City` on `RestaurantLocation` (new column). Address remains.
- List column / city filter use stored City (and postcode as today).
- Gap: Guest Loop / Settings add UI and `AddOwnedLocationRequest` today collect Address + Postcode only — update create/import/activate contracts to require City.

### 6. Location manager

- Source of truth: optional `ManagerUserId` FK on `RestaurantLocation` (nomination).
- When set: Active membership; role Owner, Admin, Area Manager, or Location Manager; if NamedList scope, list must include this location.
- Display: nominated user’s display name; null → **—**.
- Do not use free-text `LocalContact` for the table column (`LocalContact` stays setup free-text).

### 7. Setup status (derived)

| Setup status | Rule |
| --- | --- |
| **Not started** | Lifecycle = Draft |
| **Needs attention** | Lifecycle = Active or Paused, and ≥1 attention check fails |
| **Ready** | Lifecycle = Active or Paused, and all checks pass |
| **Blocked** | Unused in v1 |

**Attention checks (both required in this cut):**

1. **No Active QR** — location has zero `QrCode` with status Active.
2. **Privacy review** — restaurant **Privacy consent** is not ready (`PrivacyConsentReadyAt` is null). While not ready, **every** Active or Paused owned location fails this check (restaurant grain, per Q11).

**Privacy consent ready (new restaurant fact):**

- Column on `Restaurant`: `PrivacyConsentReadyAt` (nullable UTC).
- Null → privacy incomplete.
- Set when an operator with Manage on `privacy-consent` (or Owner/Admin) saves Privacy consent wording / completes privacy review for the restaurant (minimal save path ships in this cut if the Privacy settings page is not yet live).
- Migration: existing restaurants → set `PrivacyConsentReadyAt = UtcNow` so live accounts are not flooded; **new** restaurants stay null until saved.

Setup tab returns **aggregated** attention items, each with:

- `id` (`no-active-qr` | `privacy-review`)
- `message` (count-aware copy)
- `locationIds` (ids behind the row, for **Review location**)

KPI **Setup needs attention** = count of locations whose derived setup status is Needs attention.

### 8. Last activity (table column)

- Latest **guest-facing** activity at that location (`LocationGuestActivity` kinds already recorded).
- API returns ISO timestamp (and optional kind); client formats label.
- Empty → **—**.

### 9. Activity tab (complete in this cut)

- Dedicated **location activity** store: restaurantId, optional locationId, actorUserId, actorDisplayName, kind, description or params, occurredAt.
- Feed `GET` returns newest-first rows with time + description (client may reformat time).

**Operator ops kinds (emit on write):**

- `location-created` (Add / Import row)
- `lifecycle-changed` (Activate, Pause, Resume, Archive, Restore)
- `manager-changed`
- `location-edited` (name / address / city / postcode)

**Privacy / consent kinds (emit in this cut):**

- `consent-copy-changed` — restaurant SMS/email consent wording saved (locationId null = restaurant-wide; if a later per-location override exists, set locationId).
- `privacy-review-completed` — `PrivacyConsentReadyAt` set.
- `guest-marketing-unsubscribed` — Location Guest marketing preference moves to OptedOut at a location (wire from existing preference update path).

Do not overload `RestaurantAccessActivity` (Team) or guest activity tables for the Activity **tab**; guest unsubscribe still uses guest preference as source of truth and **also** appends a location activity row.

### 10. Location switcher

- Show **Active** and **Paused** only.
- **Draft** and **Archived** hidden.
- **Paused:** lifecycle badge before the location name.
- **Active:** no badge.

### 11. Auth

- Area: `OperatorAreaIds.Locations`.
- Reads: View (or higher).
- Writes (add, import, activate, pause, resume, archive, restore, delete draft, manager): Manage.
- Privacy consent ready / wording save: Area `privacy-consent` Manage (Owner/Admin as today).

### 12. Create and Import (complete in this cut)

- **Add location** CTA → `POST /api/locations` (extended): requires name, address, city, postcode; creates **Draft**; respects location cap; emits `location-created`.
- **Import locations** CTA → bulk import API (multipart or JSON list) reusing Guest Loop upload validation rules (name, address, city, postcode, optional phone/local contact):
  - Each valid row inserts a **Draft** Owned location (same cap checks; fail closed or partial-success contract documented in implement).
  - Invalid rows returned with row errors; no silent skip without report.
  - Emits `location-created` per successful insert.
- Account Setup / Guest Loop provisioning path still creates **Active** and must collect **City** (update DTOs + UI).

### 13. Detail / navigation (explicitly out)

Row actions that only navigate (View location, View QR placements, View feedback, View reports, Continue setup → edit) may stay client route stubs until destination pages exist. This PRD does **not** ship those destination surfaces.

---

## User stories

1. As an operator with Locations access, I want to list my owned locations with lifecycle, setup status, manager, city/postcode, and last activity, so that I can manage venues from Settings.

2. As an operator, I want KPI counts for Active, Draft, Paused, and Setup needs attention, so that I see portfolio health at a glance.

3. As an operator, I want to search, filter, sort, and page the list, so that large groups stay usable.

4. As an operator, I want Setup & readiness to list privacy-review and no-active-QR items with location ids, so that Review location can open the right venues.

5. As an operator, I want an Activity feed of location ops and privacy/consent events, so that I can audit who changed what.

6. As an operator, I want Add location and Import locations to create Drafts with city and postcode, so that unfinished venues are not live.

7. As an operator, I want Activate when name, address, city, and postcode are complete, so that I can take a Draft live.

8. As an operator, I want Pause location to suspend Capture and outbound guest work at that venue until Resume, so that a closed site does not keep capturing or sending.

9. As an operator, I want Archive only after Pause, and Restore back to Paused, so that archive is deliberate and safe.

10. As an operator, I want to nominate a location manager, so that the table shows who owns the site.

11. As an operator, I want the location switcher to show Active and Paused (with a Paused badge), so that I do not select Draft or Archived venues by mistake.

---

## Slice 1 scope (build) — A–E complete; F out

**A. List + KPIs**

- Schema: `LifecycleStatus`, `ManagerUserId`, `City`; `Restaurant.PrivacyConsentReadyAt`; consent wording fields as needed for save + activity; location activity table.
- Migration: lifecycle → Active; existing restaurants → `PrivacyConsentReadyAt` set; City empty allowed on old rows until next Add/Activate/Edit (list shows “—”).
- `GET` list: search, filters (lifecycle / setup / city), sort (name), page; row fields; last activity ISO; city facets; KPI aggregates.

**B. Setup & readiness**

- Derive setup status + both attention checks.
- Attention payload: id, message, `locationIds`.
- Tab badge count = locations in Needs attention (or attention-item count — match UI chip; default = needs-attention location count).

**C. Activity tab**

- `GET` feed (time + description + kind + locationId).
- Writers for all operator ops kinds and Privacy/consent kinds listed above (including guest unsubscribe hook).

**D. Lifecycle writes**

- Activate, Pause (+ cascade), Resume, Archive, Restore, Delete draft, set/clear manager.
- Capture Pause/Activate via existing QR lifecycle module; Settings status remains separate.

**E. Create / import**

- Extend Add (Draft + City).
- Bulk Import API + wire Settings Import CTA / upload UX (reuse `locationUpload` validation where possible).
- Update provisioning/setup DTOs for required City.

**F. Out**

- View / Edit full page, QR placements, Feedback, Reports destination UIs.

Also in slice: integration tests; frontend off demo data; CONTEXT glossary updates.

**Still out (not A–E):**

- **Export location history**.
- Full Privacy & consent settings **page** chrome beyond the minimal ready/wording save needed for checks + activity (if a larger Privacy PRD exists, this cut only adds the facts/writers Locations needs).
- Capture UI banner when Settings is Paused (optional).

---

## Non-goals

- Merging Settings lifecycle into `CaptureLocationStatus` as one enum.
- Hard-deleting Archived locations in v1.
- Operator override of Setup status.
- Using Team NamedList to invent a “primary” manager without `ManagerUserId`.
- Cascading Pause to Billing or other locations.
- Shipping destination pages for row ⋮ navigation (F).

---

## Glossary notes (for CONTEXT.md follow-up)

Agents should add or update CONTEXT entries when implementing:

- **Location lifecycle status** (Settings) vs **Capture location status**.
- **Pause location** (Settings cascade) vs **Pause location capture** vs **Pause workspace**.
- **Location manager** (nomination FK) vs **Location Manager** (permission role) vs **Local contact** (free text).
- **Privacy consent ready** (restaurant) vs Privacy contact (Key contacts).

---

## Open follow-ups (not blocking this cut)

1. Existing rows with blank City — operator prompt vs address parse backfill (prefer honest “—” + require on Add/Activate/Edit).
2. Whether Capture UI should show a banner when Settings lifecycle is Paused.
3. Full Privacy & consent settings child UI (facts/writers may land first from this PRD).

---

## Acceptance

- [ ] Settings Locations page loads real owned locations (no demo rows in production path).
- [ ] KPIs match server aggregates for the restaurant scope.
- [ ] List supports search, lifecycle/setup/city filters, name sort, page, city facets.
- [ ] Setup tab returns `no-active-qr` and `privacy-review` when applicable, each with `locationIds`.
- [ ] New restaurants without `PrivacyConsentReadyAt` mark Active/Paused locations Needs attention for privacy.
- [ ] Activity feed shows ops events and privacy/consent events (including guest unsubscribe).
- [ ] Add and Import create Drafts; require city + postcode; respect location cap.
- [ ] Draft → Activate enforces name, address, city, postcode.
- [ ] Pause sets Paused, pauses Capture, and blocks Campaign send / Offer issue / new guest intake at that location.
- [ ] Resume returns Active and restores Capture QR set without unpausing unrelated Campaigns/Offers.
- [ ] Archive rejected unless Paused; Restore lands on Paused.
- [ ] Switcher shows Active + Paused only; Paused has badge; Active has none.
- [ ] Area `locations` View/Manage enforced on reads/writes.
