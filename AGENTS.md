# AGENTS.md

## Agent skills

### Issue tracker

Local markdown — issues and PRDs live as files under `.scratch/<feature-slug>/`. See `docs/agents/issue-tracker.md`.

### Triage labels

Five canonical roles using default strings (`needs-triage`, `needs-info`, `ready-for-agent`, `ready-for-human`, `wontfix`). See `docs/agents/triage-labels.md`.

### Domain docs

Single-context — `CONTEXT.md` at repo root, ADRs in `docs/adr/`. See `docs/agents/domain.md`.

### Figma

Prefer the **Figma MCP** plugin (`get_design_context`) for design-to-code. Optional REST API fallback: set `FIGMA_ACCESS_TOKEN` in a gitignored root `.env` (see `.env.example`). Token: Figma → Settings → Security → Personal access tokens.

**Icons and controls:** When implementing Figma chrome, prefer existing **Lucide** icons and **shadcn/ui** primitives (`Checkbox`, `Button`, etc.) over downloading or committing one-off Figma SVG exports. Use Figma assets only when no project icon/component matches the glyph or control (e.g. unique brand artwork). Layout, spacing, and copy still follow the Figma reference.
