# Pilot “Activate Tummly” dashboard dialog

Date: 2026-09-19  
Status: approved  
Figma: [5043:10805](https://www.figma.com/design/UP1DqyGGGrxx80Dp7jReTT/Tummly---Marketing-Website?node-id=5043-10805)

## Goal

After sign-up / sign-in, when an Operator lands on the Dashboard and the restaurant is on the **Pilot** subscription plan, show the Figma “Activate Tummly to publish offers” dialog **after workspace loading finishes**. The hero image must be ready when the dialog opens (not pop in after). Chrome must use **Operator light and dark semantic tokens** (no hardcoded light-only or dark-only hex for surfaces, text, or close control).

## Decisions (locked)

| Topic | Choice |
|---|---|
| Host | Operator Dashboard shell path (Approach 1) — not Home-only |
| Gate | Workspace `status === "loaded"` and `subscriptionPlan === "Pilot"` |
| Frequency | Every sign-in while still on Pilot |
| Session dismiss | `sessionStorage` — X / overlay / CTA dismisses for this browser tab session only |
| Start CTA | Mark dismissed → navigate to Manage Plan (`operatorDashboardBillingCreditsManagePlanPath`) |
| View plans CTA | Mark dismissed → same Manage Plan path |
| Backend | No new Pilot-start API in this work |
| Carousel | Out of scope (single hero image) |
| Theme | Operator semantic tokens for light and dark |

## When it opens

1. Operator workspace finishes load (`status === "loaded"`).
2. `subscriptionPlan` from the workspace snapshot equals `"Pilot"` (case-sensitive match to the locations / chrome wire value).
3. Session dismiss key is not set for this sign-in.
4. Hero image preload has completed, or a short timeout has elapsed (so a failed image does not block the shell forever).

Do not open while `loading` / `idle` / `error`. Do not open when `subscriptionPlan` is not Pilot.

## UI

Match Figma node `5043:10805`:

- Title: **Activate Tummly to publish offers**
- Body: **Start your 30-day Pilot or choose a plan to create live offers and make them available to guests.**
- Primary: **Start 30-day Pilot** (`Button` `variant="op-primary"`)
- Secondary: **View plans** (`Button` `variant="op-tertiary"`)
- Close: collapse-style control (`variant="op-collapse"` + Lucide `XIcon`), same pattern as `OperatorDestructiveConfirmDialog`
- Layout: header row (copy + close), then CTA row, then full-bleed hero under the content
- Reuse shared `Dialog` / `DialogContent` / `DialogTitle` / `DialogDescription`; set `showCloseButton={false}` and own the Figma close chip

### Light / dark tokens (required)

Override default `DialogContent` marketing hex so Operator chrome follows theme:

| Role | Token / class (intent) |
|---|---|
| Dialog surface | `bg-op-surface-secondary` (or closest Main Bg / Cards semantic already used by Operator dialogs) — must resolve correctly in light and dark; no `#edefee` / `#1b1b1b` hardcodes on this surface |
| Title / primary text | `text-op-text-primary` |
| Body text | `text-op-text-secondary` or muted Operator gray token that is theme-aware |
| Close chip | `op-collapse` (uses `op-button-collapse-background` / primary text) |
| Primary CTA | `op-primary` (existing primary green + inverse text) |
| Secondary CTA | `op-tertiary` (border + text tokens) |
| Radius / padding | Match Figma (~4px radius, ~32px header padding); use Operator radius tokens where they already exist |

If a Figma hex has no token, add the closest match to design tokens per AGENTS.md — do not leave raw hex on this dialog chrome.

## Image

1. Export the Figma hero (lifestyle QR / phone composition).
2. Optimize to WebP (commit under `src/assets/…`).
3. `import` the asset in the dialog module so the bundler emits a hashed URL.
4. Preload / `decode()` before setting `open=true`.
5. Render with fixed aspect / `object-cover` full width so layout does not jump.

## Session dismiss

- Key e.g. `tummly.operator.activate-tummly-dialog.dismissed` in `sessionStorage`.
- Set on X, overlay dismiss, Start, or View plans.
- Cleared automatically on new browser session / new sign-in tab lifecycle (no `localStorage`).
- After dismiss, do not reopen until the next sign-in session while still Pilot.

## Wiring

- New presentational component: `ActivateTummlyPilotDialog` under Operator dashboard components.
- Small gate helpers (pure): should-open predicate + sessionStorage read/write — unit tested.
- Mount from Operator `Dashboard` (or thin host next to shell) once workspace snapshot is available: mode + `selectedLocationId` for Manage Plan hrefs.
- If `billingCreditsAccess === "none"` or location id is missing, still allow dismiss; hide or no-op navigation CTAs only if Manage Plan is unreachable — prefer navigate when access is `view` or `manage` (default owner path).

## Testing

- Gate: Pilot + loaded + not dismissed → open; non-Pilot → never open; dismissed → never open; loading → never open.
- Presentation classes use Operator tokens (no hardcoded dialog surface hex).
- Optional: preload helper resolves before open flag.

## Out of scope

- New backend endpoints or Pilot clock mutation from this dialog
- Carousel / multiple hero slides
- Once-per-account server-side “seen” flag
- Marketing-site (non-Operator) reuse

## Spec self-review

- No TBD placeholders.
- Start and View plans both go to Manage Plan (locked v1; no separate start API).
- Theme requirement is explicit and testable.
- Scope fits one implementation plan.
