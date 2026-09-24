# Typography

**Leading words:** *headline* · *functional* · *editorial*

One typography system for Tummly. Pick the **family by role**, then encode **weight / size / line-height / letter-spacing** as reusable tokens (from Figma metrics). Do not invent a third typeface.

## Family roles (authoritative)

| Role | Typeface | Intent | Tailwind | CSS token |
| --- | --- | --- | --- | --- |
| *headline* | **Plus Jakarta Sans** | Brand confidence | `font-jakarta` or `font-heading` | `--font-jakarta` / `--font-heading` |
| *functional* | **Helvetica Neue** | Clarity and usability | `font-sans` | `--font-sans` |
| *editorial* | **Lora** | Selective brand accent | `font-serif` | `--font-serif` |

**Pick family from this table**, even when Figma or legacy code still names Inter, Season Mix, Roboto Serif, or another stand-in. Figma still owns **weight, size, line-height, and letter-spacing** for each text style.

### *headline* — Plus Jakarta Sans

Use for:

- H1, H2, H3
- Section titles
- Key stats / KPI values when they read as display numbers
- Prominent card headings

### *functional* — Helvetica Neue

Use for:

- Body copy
- Navigation
- Buttons and controls
- Labels, forms, inputs, placeholders
- Tables
- Pricing details
- FAQs (question + answer body)
- General UI text

Default UI text is *functional* unless the element is clearly a *headline* or an intentional *editorial* accent.

### *editorial* — Lora

Use **only** for short, intentional brand moments:

- Pull quotes
- Occasional brand statements on marketing surfaces

Do **not** use Lora as a regular heading font or as body copy. Prefer *headline* or *functional* when unsure.

## Surface split

| Surface | Families |
| --- | --- |
| **Marketing website** (public marketing, legal, trust, FAQs landing, auth marketing chrome) | *headline* + *functional*; *editorial* only where the design intentionally wants an editorial feel |
| **WebApp** (Operator, Admin, Support, guest-loop product UI, in-app dialogs/sheets) | *headline* + *functional* only. Little or no *editorial* — skip Lora unless Figma marks a rare brand accent |

## Decision steps (every UI text change)

1. **Name the surface** — marketing website or WebApp.
2. **Name the role** — *headline*, *functional*, or (marketing only, rare) *editorial*.
3. **Apply the family class / token** from the table above.
4. **Read Figma** for weight, size, line-height, letter-spacing (and colour via design tokens).
5. **Encode metrics as tokens** when the style will be reused — do not leave one-off magic numbers for hierarchy styles that appear more than once.
6. **Keep the same hierarchy across desktop / tablet / mobile** — same style names and families at every breakpoint; scale size/line-height with responsive classes if Figma scales them, but do not swap families by viewport.

Completion check: every new or touched text style has an explicit family role, and WebApp copy does not use Lora unless Figma shows a deliberate editorial accent.

## Reusable text-style tokens

A text style token is the full stack, not family alone:

- `font-family` (role → token above)
- `font-weight`
- `font-size`
- `line-height`
- `letter-spacing`

**Where to define**

- Shared CSS / Tailwind theme: `src/index.css` (`@theme` font families; Operator scale under `--op-font-size-*` and related component tokens).
- Feature presentation modules: shared class constants (for example `src/lib/operatorHome/*Presentation.ts`) when a screen reuses the same style many times.

**How to name**

- Prefer semantic names tied to role + use (`op-card-title`, marketing section title, KPI value), not raw pixel names.
- Reuse existing `--op-*` typography tokens on Operator surfaces before adding parallel values.
- When Figma introduces a hex or size with no token, add the closest token (or a new named token) in `src/index.css` — same rule as colour tokens in `AGENTS.md`.

**Hierarchy consistency**

- H1 / H2 / H3 / body / label / button stay the same roles across breakpoints.
- Do not promote body to *headline* or demote H2 to *functional* solely for mobile density.

## Code mapping (current repo)

Defined in `src/index.css` `@theme` and `src/assets/fonts/helvetica-neue.css`:

| Role | Live stack | Agent action |
| --- | --- | --- |
| *headline* | Plus Jakarta Sans Variable (`--font-jakarta`, `--font-heading`) | `font-jakarta` or `font-heading` |
| *functional* | Licensed **Helvetica Neue** webfont (`--font-sans`) from `src/assets/fonts/helvetica-neue/` | `font-sans` for body and UI chrome |
| *editorial* | Lora Variable (`--font-serif`) via `@fontsource-variable/lora` | `font-serif` **only** for pull quotes / rare brand accents |

**Operator dashboard (`html.op`):** body and chrome use Helvetica Neue; `h1`–`h3` and dialog/drawer/sheet titles use Plus Jakarta Sans via `src/index.css`. Section titles, KPI values, and prominent card headings in presentation modules also set `font-jakarta`.

Keep Tailwind class names stable. Do not reintroduce Inter or Roboto Serif. Do not commit Helvetica Neue source files outside `src/assets/fonts/helvetica-neue/`.

## Figma

- Marketing file often exposes `Typography/Heading` → Plus Jakarta Sans and `Typography/Sub-heading` → Helvetica Neue — treat those as role hints that match this doc.
- Prefer **Figma MCP** `get_design_context` / `get_variable_defs` for metrics; family still follows the role table if a frame still shows Inter or a serif display font on a normal heading.
- Prefer Lucide + shadcn for chrome; typography still follows this doc.

## Anti-patterns

- Using Lora / `font-serif` for ordinary H1–H3 or body on the WebApp
- Using Plus Jakarta Sans for dense UI chrome (tables, form labels, nav) when Helvetica Neue / `font-sans` is the functional default
- Hardcoding `"Inter"`, `"Roboto Serif"`, or raw `font-family` strings in components when a theme token exists
- Using `font-serif` for ordinary headings (use `font-jakarta` / `font-heading`)
- Different type families for the same semantic style on mobile vs desktop
