# Extract shared UI primitives for Operator tabs, spinners, and empty states

Operator dashboard screens still hand-roll a few patterns that should live as reusable `components/ui` (or shared Operator) primitives. We already moved buttons, badges, textarea, and checkbox labels onto shared components; the remaining copies are not blocking product work, but each new surface risks another one-off. **Accepted direction:** extract when we next touch these areas (or in a dedicated cleanup), not as a must-do now.

## Candidates

| Primitive | Current copies | Intent |
|---|---|---|
| **Tabs** (underline / filter tablist) | `OperatorHomeLatestActivity`, `OperatorNotificationsDrawer` — `role="tablist"` + per-tab `Button`s | Shared `Tabs` / `TabsList` / `TabsTrigger` / `TabsContent` matching Figma filter chrome (selected underline or weight, muted unselected) |
| **Spinner** | `OperatorDashboard`, `OperatorHomeBody`, Feedback details drawer, Notifications drawer (list + settings) — repeated `animate-spin` ring markup | Shared `Spinner` with size variants (`sm` / `md`) and accessible `role="status"` / label |
| **Empty state** | Needs attention, Live offers, Latest activity, Recommended next step, Weekly brief — centered copy (+ optional helper / CTAs) in muted section shells | Shared empty composition (title/copy/helper/actions slots) so section shells stay local but empty chrome does not |

## Consequences

- Prefer adding shadcn/registry primitives (or thin wrappers) over inventing parallel APIs.
- When extracting, migrate all Operator copies in the same change so styles do not drift.
- Do not block feature PRs on this; leave a follow-up or do it opportunistically when editing the host file.
