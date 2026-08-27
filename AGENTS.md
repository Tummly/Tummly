# AGENTS.md

## Agent skills

### Issue tracker

Local markdown — issues and PRDs live as files under `.scratch/<feature-slug>/`. See `docs/agents/issue-tracker.md`. The map is the index for **lock** tickets and later **build** tickets.

When creating **Wayfinder map tickets** OR **Issue Tickets** via **/to-tickets** that need UI or has any UI pieces, attach **full Figma URLs** (with `node-id`) on the ticket — see **Figma URLs on UI map tickets** in `docs/agents/issue-tracker.md`. Do not rely on node ids or frame names alone.

### Triage labels

Five canonical roles using default strings (`needs-triage`, `needs-info`, `ready-for-agent`, `ready-for-human`, `wontfix`). See `docs/agents/triage-labels.md`.

### Domain docs

Single-context — `CONTEXT.md` at repo root, ADRs in `docs/adr/`. See `docs/agents/domain.md`.

### Figma

Prefer the **Figma MCP** plugin (`get_design_context`) for design-to-code. Optional REST API fallback: set `FIGMA_ACCESS_TOKEN` in a gitignored root `.env` (see `.env.example`). Token: Figma → Settings → Security → Personal access tokens.

**Icons and controls:** When implementing Figma chrome, prefer existing **Lucide** icons and **shadcn/ui** primitives (`Checkbox`, `Button`, etc.) over downloading or committing one-off Figma SVG exports. Use Figma assets only when no project icon/component matches the glyph or control (e.g. unique brand artwork). Layout, spacing, and copy still follow the Figma reference.

### UI components

Do **not** invent one-off UI (raw `<button>`, ad-hoc badge chips, custom checkbox+label rows, hand-rolled select menus, etc.) when a shared primitive already exists.

Before adding or styling custom markup:

1. **Reuse first** — check `src/components/ui/` (and nearby feature wrappers) for an existing component (`Button`, `Badge`, `Checkbox` / `CheckboxLabel`, `Textarea`, `Select` / `FloatingLabelSelect`, `Drawer`, etc.). Prefer variants/`className` on that component over a parallel control.
2. **Registry next** — if nothing fits, search the **shadcn registry** (`npx shadcn@latest search` / docs) and **suggest** adding that component before building a custom solution. Only hand-roll UI when no installed or registry component can express the interaction.
3. **Defer deliberate gaps** — some Operator patterns (filter tabs, spinners, empty states) are still duplicated on purpose; see `docs/adr/0014-extract-shared-operator-ui-primitives.md`. Prefer extracting or adopting a registry primitive when next touching those surfaces, not inventing a third copy.
4. **Use Design Tokens for Operator Dashboard and it's children** - use design tokens when creating compoenents, compare hex codes from figma to the current design tokens and use the closest match. If the hex code is not in the design tokens, add it to the design tokens.
5. For the dropdowns or popup or menus, if they are inside a sheet, drawer, dialogue or modal make surethey come on top of the parent and not behind it.

### Review

Operator settings review rules live in `CODING_STANDARDS.md` (review time, not implementation). That file also covers page-module `getSnapshot` identity, chrome access omit defaults, and EF migration cascade / Designer rules.

### Stuck revision

New API route empty **404** while `/api/auth/me` is **401** → *stuck revision* first. See `docs/agents/stuck-revision.md` and `scripts/probe-qa-api-revision.sh`. Do not assume the branch lacks the controller until Ready equals Latest.

### Language

Only report to me in ASD-STE100 Simplified Technical English.

### Sub Agents:

Only use Cursor Auto Subagents no Opus / GPT e.t.c. models to call sub agents.
