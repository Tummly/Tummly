# Offers — Operator surface build

Status: ready-for-agent  
Label: `ready-for-agent`

Authority: Figma for layout and chrome (URLs below and on [map.md](./map.md)); this PRD + Wayfinder Answers for behaviour. Glossary: root `CONTEXT.md`. Decision source: [map.md](./map.md) and resolved tickets **01–15**, plus PRD gap locks recorded on [Author Offers build PRD](./issues/16-author-offers-build-prd.md).

Hand-off is this PRD + [implementation issues](./issues/) **17+** for `/to-tickets` / build — not implementation inside the Wayfinder map.

## Problem Statement

Catalog create + get-by-id and Campaign **create-and-select** exist; Operator **Offers** page, list/metrics, claim/redeem/void lifecycle UI, catalog update/pause/archive, Offer Details, templates picker, staff Redeem, and Campaign **Existing offer** browse do not. Research: [research/offers-catalog-surface.md](./research/offers-catalog-surface.md).

## Solution

Ship the Operator **Offers** surface: main page (Performance, Needs attention, table + filters + CTAs), Offer templates (Campaign picker chrome, Offers data), Offers-owned shared Create/Edit Offer drawer (copy then replace Campaign callers), Offer Details (five tabs + dialogues), staff Redeem, Void request flows, location-wide redemption log, and Campaign wizard **Existing offer** unlock. First build = full chrome, honest empties, KPI zeros; Edit Save and list/Details lifecycle writes are live (**31** / **32**); remaining writes stay gated or stubbed per the gating table.

**Recovery ↔ catalog cutover** is out of this PRD — product lock: [../recovery-catalog-offers/PRD.md](../recovery-catalog-offers/PRD.md); build handoff: [../recovery-catalog-offers/IMPLEMENT.md](../recovery-catalog-offers/IMPLEMENT.md). Feedback Recovery create-in-wizard stays until that effort.

Reuse Campaigns/Guests **architecture** (page module, Filter sheet kernel, KPI card chrome, template-picker shape); fork Offers module/schemas/KPIs/catalogue/Needs attention — research [research/campaigns-patterns-for-offers.md](./research/campaigns-patterns-for-offers.md).

## Glossary

Use `CONTEXT.md`: **Offers catalog**, **Offer issue** / **Offer claim** / **Offer redemption** / **Offer Claim code**, list tabs (**All** / **Drafts** / **In flight** / **Sent** / **Needs attention**), **Void request**, **Offers page**, **Offers Performance**, **Offer redemption log**, **Offer Details**, **Offer recommendation**, **Campaign offer attach**.

## User Stories

### Main page and list

1. As an operator, I want Create offer → templates → Create drawer, Open staff redeem, and View redemption log, so that I can manage offers from one page.
2. As an operator, I want Performance KPIs with Main date presets (Last 7 / 30 / This month / Custom), so that window metrics stay clear of Details’ 7 / 30 / 90.
3. As an operator, I want Needs attention overview (rule warnings when data; AI empty first), so that urgent offers surface without leaving the list.
4. As an operator, I want tabs All → Needs attention → Drafts → In flight → Sent with Campaigns-style empties, so that lifecycle views stay honest.
5. As an operator, I want search by title, Campaign name, and open Claim-code prefix, plus Filters (Status + attach source), so that I can find offers without Guests-pollution chips.

### Templates and Create/Edit

6. As an operator, I want seven Offer templates (incl. Custom + Completed recovery) with Use template soft-fill, so that create is fast without Preview MVP.
7. As an operator, I want one shared Create/Edit drawer owned by Offers, so that Campaign and Offers share one chrome.
8. As an operator, I want Edit to lock offer type and apply benefit/validity changes to new issues only, so that existing passes stay stable.

### Details, Redeem, Void, Campaign

9. As an operator, I want Offer Details with Overview + Claims + Redemptions + Campaigns + Void requests, so that one definition has full lifecycle visibility.
10. As an operator, I want location-wide staff Redeem (code/QR → Check → confirm → toast), so that till staff can redeem without Claim-first.
11. As an operator, I want Void request create/review/approve/reject on Redeemed passes, so that wrong redemptions can be corrected with audit.
12. As an operator, I want Campaign Existing offer browse + Select attach, so that wizards reuse catalog offers.

## Implementation Decisions

### Scope and authority

- Figma wins layout/chrome when URLs exist. This PRD + ticket Answers win behaviour (exceptions recorded below).
- Prefer Lucide + shadcn/ui / Operator primitives; map Figma hex to Operator design tokens.
- Prefer Filter sheet / Dialog / AlertDialog already used on Campaigns; do not invent parallel controls.
- Dropdowns inside sheets/drawers/dialogs must stack above the parent.

### Domain locks

**Claim code (01):** One unique **Offer Claim code** per **Offer issue**, created at issue time. Catalog definition has no shared redeemable code. Redeem burns that pass only. MVP POS = manual (no Tummly→POS push).

**Issue / Claim events (04):** Issue on Campaign Email/SMS provider **Accepted**, Recovery successful send, or Guest form thank-you submit with live attach. Reject/fail accept → no Issue; late bounce after Accept keeps Issue; guest preview/send test → no Issue. Claim nested after Issue, once sticky. MVP Claim proxy: Email/SMS ClaimedAt ≈ Accepted; thank-you = paint. Freeze proxy history when open-tracking ships. Redeem without Claim allowed. KPI windows use each event’s own timestamp.

**List tabs (03):** Order All → Needs attention → Drafts → In flight → Sent; default All; badges on all but All. Drafts ∪ In flight ∪ Sent = All; Needs attention may overlap (not summed into All). **Paused → Sent** (≠ Campaigns). **Expired** = catalog fixed end date only (venue TZ when known); relative “N days after issue” does not auto-Sent. Resume from Paused: ≥1 live attach → In flight; else Drafts.

**First-build Draft reality (16 gap):** Catalog create today writes **Active**. First build: Create saves **Active**; **Drafts** tab = Active-with-zero-live-attaches ∪ any future stored Draft. Hide **Delete draft** until a stored-Draft write path exists. Do not invent Publish/Activate in first build.

**In flight before Recovery cutover:** Membership rules include Recovery / thank-you attaches; until cutover, live attaches in product are **Campaign** only — In flight ≈ Campaign-attached Active offers.

### Main Offers page (14, 07, 03)

- Header: **Create offer** → templates (**08**) → drawer (**09**); **Open staff redeem** (**05**); **View redemption log** → full-page location-wide log (Details Redemptions columns + **Offer** column); first build chrome + honest empty + Retry.
- Performance: presets Last 7 / 30 / This month / Custom. **Active offers** KPI (MVP, ticket **16**) = count of all stored **Active** at location (includes Active-no-attach); ignores date; not the older “issuable now” wording from ticket **14**. Issued / Claims / Redemptions = window events. Rate = Redemptions ÷ Claims; **0 Claims → —**; helper: “Share of claims in this period that staff redeemed.” (Fix bad Figma rate subtitle.)
- Needs attention overview (**07**): hybrid Warning + AI chrome; MVP rules when data = expiring ≤7 venue days + open Void; AI empty first; max 5 rows; keep section when empty; session collapse; AI Not now session-only when live.
- Search: title + attached Campaign name(s) + **open** Claim-code prefix (issued; not redeemed/cancelled/voided-unusable).
- Filters: Campaigns Filter sheet kernel. Fields: Status (Draft/Active/Paused/Expired/Archived) · **Offer type** UI label = live **attach source** (Campaign / Recovery / Guest form thank-you / Manual if any). No Date; Location deferred. Drop Guests sample chips. Do **not** filter catalog `offerType` benefit enum under this control.
- Table: Offer + live-attach subline; Status; Validity; **lifetime** Claims/Redeemed/Rate; Controls (Unique code · Use rule · Validity); slim ⋮. Row click / View → Details.
- Slim ⋮ matrix (14): View/Edit/Pause/Resume/Duplicate/Archive by status. State changes → one-line AlertDialog.

### Offer templates (08)

- Reuse Campaign template picker chrome; fork Offers catalogue + seed. Seven client-seed templates (see ticket **08** table). Preview disabled/omitted MVP.
- Use template → soft-fill Create draft; no catalog row until Save. Mapping + `{{restaurant_name}}` rules per **08**.

### Create / Edit drawer (09, 15)

- Offers owns shared drawer; copy Campaign panel then replace callers; Offers Create Figma = chrome/copy SoT.
- Create success (Offers): close, stay on list. Campaign create-and-select behaviour kept; stance card retitled to Offers Figma.
- Edit: same drawer; skip type picker; type read-only. Edit Save is live via update API (**31**); Campaign Edit hydrates and updates, **never re-POSTs**.
- Field matrix (**15**): type locked; benefit / purchase / validity / title / description / staff editable; validity+benefit → **new issues only**; soft confirm when issues ≥1 and those fields dirty; no location editor MVP.
- Callers: Offers Create, Campaign wizard, templates Use template. Not Recovery create-in-wizard; not Home Live Offers CTA.

### Offer Details (10 + 16 gaps)

- Header: **Edit offer** · **Open staff redeem** · ⋮. Drop Figma **Redeem Offer**.
- Status set: Draft / Active / Paused / Expired / Archived (no Scheduled).
- **Cancel offer** (Figma/Details copy) = **Archive** (same stored status as list Archive). Prefer label **Archive offer** in build; if UI keeps “Cancel offer”, map to Archived only — no Cancelled status.
- Hide navigate-only ⋮ until routes exist: View activity / View active passes / View audit details / View historical record.
- **Resend offer**: show gated until channel/API ticket. **Export record**: omit first build.
- Hide **Delete draft** until stored Draft write path exists.
- Overview: meta; KPI strip date-scoped **7 / 30 / 90 / Custom** (default 7) — deliberate ≠ Main; middle block title **Claims and redemptions over time** (definition body; date scopes KPIs only); Recommended next step empty; Manager override read-only fact.
- Tabs: Overview · Claims · Redemptions · Campaigns (Linked + Issuance sources) · Void requests. Hide Redemptions Override column MVP.
- Claims ⋮: View guest · Resend (gated) · Cancel claim · Copy code. Cancel claim only if not redeemed/voided.
- Redemptions ⋮: View redemption/pass/guest/issued terms · Request void · (no Export first build).
- Claims empty SoT Figma `3527-54811` + Share in campaign → wizard with offer pre-attached.

### Staff Redeem (05)

- One location-wide dialogue from Main + Details. Code or QR → Check offer → confirm → Mark as redeemed → toast. QR auto-Check. Claim not required. No manager override MVP. Inline Check errors + Failed attempts.

### Void request (06)

- MVP create = **Redeemed** only. Reason (+ Explanation if Other); correction Keep unusable | Restore one use (none preselected). One Pending per pass. Approve: keep original redemption audit; add correction; Redeemed KPI −1; apply correction. Any location operator may approve. Notify approver then submitter.

### Campaign Existing unlock (11)

- Enable Existing; browse Active/attachable (not stored Draft, not Sent); include Active-no-attach + In flight; include replacement_item. Inline picker (not modal); Search; Select + View details (Details route live, **23**); no kebab. Select attaches like create-and-select; Continue requires OfferId for Existing + Create; empty → Create drawer; load fail Retry.

### First-build gating

Refresh (**43**): Edit Save and Pause / Resume / Archive / Duplicate are **not** gated — they shipped in **31** / **32**. **Active offers** KPI = all stored **Active** (MVP, **16**).

| Layer | Ship | Live when ready | Still gated / stubbed |
| --- | --- | --- | --- |
| Chrome | Full Main, Details, templates, drawer, Redeem, Void dialogues, redemption-log page | — | Navigate-only Details ⋮ until routes exist; Template Preview until Figma; Export omit first build |
| Read | get-by-id, list/metrics when APIs, empties, KPI zeros | Rule Needs attention when validity/void data exist | AI live empty until pipeline allow-list |
| Writes | Create catalog; Campaign create-and-select; **Edit Save** (**31**); **Pause / Resume / Archive / Duplicate** (**32**); Campaign Edit updates (never re-POST) | Staff Redeem + Void persist when **38** / **39** land (chrome + stubs until then) | Cancel claim; Resend; Delete draft until stored-Draft write path |
| Existing | Unlock UI + browse + Select; View details → Details route (**23**) | — | — |
| KPIs | **Active offers** = all stored **Active** at location (incl. Active-no-attach); ignores date | Issued / Claims / Redemptions / Rate window-scoped | — |
| Honest empty | Campaigns-style empties; Claims empty Figma; Needs attention section at zero | — | Do not fake rows/KPIs |

### Frontend module shape

- Fork `operatorOffers` page module + Provider (Campaigns pattern). Shared Create/Edit under Offers; Campaign imports shared export.
- Do not reuse Campaign recommendation / messaging APIs for Offers Needs attention.

## Acceptance

Agents satisfy ticket Answers **01–15** and this PRD’s gap locks. Surface checklists:

- [ ] Main page CTAs, Performance definitions, tabs/search/filters/table/slim ⋮ per **14**
- [ ] Needs attention shell + MVP rules wiring path per **07** (AI empty)
- [ ] Templates seed + Use template mapping per **08**
- [ ] Shared drawer ownership + Edit matrix per **09** / **15**
- [ ] Details five tabs + Archive mapping + hidden navigate-only / Export per **10** + **16**
- [ ] Staff Redeem flow per **05**
- [ ] Void dialogues + rules per **06**
- [ ] Campaign Existing unlock per **11**
- [ ] Claim code + Issue/Claim events respected in APIs/UI copy per **01** / **04**
- [ ] First-build gating table above (Edit Save + lifecycle live; Cancel claim / Resend / Delete draft / AI / Export / Template Preview / navigate-only still gated; Redeem/Void stubs until **38**/**39**); no fake data

## Figma index

File: `IQfpCZBNsQLbRAaPhnfmul` (Guest-Loop-MVP--01-Jul--2026-)

| Surface | URL |
| --- | --- |
| Main Offers page | https://www.figma.com/design/IQfpCZBNsQLbRAaPhnfmul/Guest-Loop-MVP--01-Jul--2026-?node-id=3498-1587&m=dev |
| Offer templates | https://www.figma.com/design/IQfpCZBNsQLbRAaPhnfmul/Guest-Loop-MVP--01-Jul--2026-?node-id=4783-30859&m=dev |
| Create type picker | https://www.figma.com/design/IQfpCZBNsQLbRAaPhnfmul/Guest-Loop-MVP--01-Jul--2026-?node-id=4770-99053&m=dev |
| Create percentage / fixed / free / replacement | https://www.figma.com/design/IQfpCZBNsQLbRAaPhnfmul/Guest-Loop-MVP--01-Jul--2026-?node-id=4770-100262&m=dev · https://www.figma.com/design/IQfpCZBNsQLbRAaPhnfmul/Guest-Loop-MVP--01-Jul--2026-?node-id=4770-101058&m=dev · https://www.figma.com/design/IQfpCZBNsQLbRAaPhnfmul/Guest-Loop-MVP--01-Jul--2026-?node-id=4770-101858&m=dev · https://www.figma.com/design/IQfpCZBNsQLbRAaPhnfmul/Guest-Loop-MVP--01-Jul--2026-?node-id=4770-102674&m=dev |
| Details Overview / Claims / Claims empty | https://www.figma.com/design/IQfpCZBNsQLbRAaPhnfmul/Guest-Loop-MVP--01-Jul--2026-?node-id=3523-53125&m=dev · https://www.figma.com/design/IQfpCZBNsQLbRAaPhnfmul/Guest-Loop-MVP--01-Jul--2026-?node-id=3523-53766&m=dev · https://www.figma.com/design/IQfpCZBNsQLbRAaPhnfmul/Guest-Loop-MVP--01-Jul--2026-?node-id=3527-54811&m=dev |
| Details Redemptions / Campaigns / Void | https://www.figma.com/design/IQfpCZBNsQLbRAaPhnfmul/Guest-Loop-MVP--01-Jul--2026-?node-id=3527-54982&m=dev · https://www.figma.com/design/IQfpCZBNsQLbRAaPhnfmul/Guest-Loop-MVP--01-Jul--2026-?node-id=3527-55555&m=dev · https://www.figma.com/design/IQfpCZBNsQLbRAaPhnfmul/Guest-Loop-MVP--01-Jul--2026-?node-id=5223-74432&m=dev |
| Void create / Review / Approve / Reject | https://www.figma.com/design/IQfpCZBNsQLbRAaPhnfmul/Guest-Loop-MVP--01-Jul--2026-?node-id=4783-24252&m=dev · https://www.figma.com/design/IQfpCZBNsQLbRAaPhnfmul/Guest-Loop-MVP--01-Jul--2026-?node-id=5223-75356&m=dev · https://www.figma.com/design/IQfpCZBNsQLbRAaPhnfmul/Guest-Loop-MVP--01-Jul--2026-?node-id=5223-76624&m=dev · https://www.figma.com/design/IQfpCZBNsQLbRAaPhnfmul/Guest-Loop-MVP--01-Jul--2026-?node-id=5229-80122&m=dev |
| Staff Redeem enter / confirm / toast | https://www.figma.com/design/IQfpCZBNsQLbRAaPhnfmul/Guest-Loop-MVP--01-Jul--2026-?node-id=3527-56860&m=dev · https://www.figma.com/design/IQfpCZBNsQLbRAaPhnfmul/Guest-Loop-MVP--01-Jul--2026-?node-id=3527-57426&m=dev · https://www.figma.com/design/IQfpCZBNsQLbRAaPhnfmul/Guest-Loop-MVP--01-Jul--2026-?node-id=3527-58361&m=dev |
| Campaign Existing stance / picker | https://www.figma.com/design/IQfpCZBNsQLbRAaPhnfmul/Guest-Loop-MVP--01-Jul--2026-?node-id=4730-53493&m=dev · https://www.figma.com/design/IQfpCZBNsQLbRAaPhnfmul/Guest-Loop-MVP--01-Jul--2026-?node-id=4744-63758&m=dev |

**Missing frames (chrome from PRD + sibling patterns):** View redemption log; Edit-distinct labels; Template Preview; Pause/Archive/Duplicate confirms (use AlertDialog); Filters sheet (annotation SoT); Redeem error frames.

### Figma drift to ignore

Redeem Offer CTA; Overview 4-tab strip; Scheduled/Exhausted in old ⋮ annotation; Redemptions Override samples; Guests filter chips; rate subtitle copy-paste; Restore “elevated permission” vs MVP any operator.

## Out of scope

- Recovery ↔ catalog **cutover implement** and Feedback create-in-wizard behaviour change ([../recovery-catalog-offers/PRD.md](../recovery-catalog-offers/PRD.md))
- Backfill of historical Feedback recovery one-offs into catalog
- Live Offers **AI** pipeline / allow-list (empty chrome only)
- Multi-location Offer ownership / Filters Location / Edit location multi-select
- Manager override on Redeem; Redemptions Override column; Claimed-but-not-redeemed Void create
- Template Preview until Figma exists
- Admin / Support Offers surfaces
- Home Live Offers Create offer CTA wiring
- Capture page **Offer claims** KPI wire-up
- Final backend DTO/route shapes beyond product seams named here
- POS integration / auto benefit push
- Navigate-only Details destinations until routes exist
- Export record first build
- Stored Draft create / Delete draft until write API supports it

## Deferred / open fog

- Offers AI pipeline allow-list types / live API (main queue + Details)
- Capture **Offer claims** KPI wire-up
- Multi-location ownership vs per-location Claim codes
- Backend list / metrics / claim / redeem / void API shapes (implement issues may propose; product behaviour is locked here)
- Recovery cutover implement plan — owned by recovery-catalog-offers PRD
- Stored Draft + Publish/Activate path
- Resend channel/API lock
- ~~Active offers vs “issuable now”~~ — locked MVP = all stored **Active** (**16**; gating refresh **43**)

## Implementation issues

| Issue | Title | Depends |
| --- | --- | --- |
| [17](./issues/17-offers-route-page-module-shell.md) | Offers route + page module shell | — |
| [18](./issues/18-shared-create-edit-offer-drawer.md) | Shared Create/Edit Offer drawer ownership | 17 |
| [19](./issues/19-offer-templates-picker-client-seed.md) | Offer templates picker + client seed | 18 |
| [20](./issues/20-main-offers-list-chrome.md) | Main Offers list chrome | 17 |
| [21](./issues/21-performance-strip-needs-attention-shell.md) | Performance strip + Needs attention shell | 17 |
| [22](./issues/22-catalog-list-lifecycle-write-apis.md) | Catalog list + lifecycle write APIs | — |
| [23](./issues/23-offer-details-route-overview.md) | Offer Details route + Overview chrome | 18 |
| [24](./issues/24-offer-details-lifecycle-tabs-chrome.md) | Details Claims / Redemptions / Campaigns / Void chrome | 23 |
| [25](./issues/25-staff-redeem-dialogue.md) | Staff Redeem dialogue | 17 |
| [26](./issues/26-void-request-dialogues.md) | Void request dialogues + tab wiring | 24 |
| [27](./issues/27-location-wide-redemption-log.md) | Location-wide redemption log page | 17 |
| [28](./issues/28-issue-claim-event-pipeline.md) | Issue / Claim event pipeline | — |
| [29](./issues/29-offers-metrics-apis.md) | Metrics APIs for Performance + Details KPIs | 28 |
| [30](./issues/30-campaign-existing-offer-unlock.md) | Campaign Existing offer unlock | 18, 22 |
| [31](./issues/31-edit-field-parity-update-api.md) | Edit field parity + update API | 18, 22 |
| [32](./issues/32-lifecycle-actions-live.md) | Main list / Details lifecycle actions live | 22, 23, 31 |
| [33](./issues/33-needs-attention-rule-engine.md) | Needs attention rule engine (non-AI) | 20, 21, 26 |

## Further Notes

- Wayfinder map: [map.md](./map.md)
- Recovery target: [../recovery-catalog-offers/PRD.md](../recovery-catalog-offers/PRD.md)
- Research: [research/offers-catalog-surface.md](./research/offers-catalog-surface.md), [research/campaigns-patterns-for-offers.md](./research/campaigns-patterns-for-offers.md)
