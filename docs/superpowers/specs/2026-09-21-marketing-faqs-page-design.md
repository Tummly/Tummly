# Marketing FAQs page

Date: 2026-09-21  
Status: approved for plan  
Figma: [FAQ page `4974:26242`](https://www.figma.com/design/UP1DqyGGGrxx80Dp7jReTT/Tummly---Marketing-Website?node-id=4974-26242&m=dev)

## Goal

Ship a dedicated marketing FAQ page at `/faqs`, linked from Marketing Header → Resources → FAQ (and Footer → FAQ). Reuse existing Header and Footer chrome. Split content by topic sections. Use placeholder Q&A for this pass; keep short stubs for **Tummly AI Assistant** and **Trust & Privacy**.

## Decisions (locked)

| Topic | Choice |
|---|---|
| Path | `/faqs` |
| Homepage `#faqs` | Keep the existing homepage FAQ block and hash scroll |
| Resources → FAQ | Route to `/faqs` (not `#faqs`) |
| Footer → FAQ | Route to `/faqs` (same as Resources) |
| Copy for this pass | Placeholder answers for all real sections; replace later when Figma export unlocks or copy is pasted |
| Stub sections | **Tummly AI Assistant** and **Trust & Privacy** — short stub description/answer only |
| Header / Footer | Existing `MarketingHeader` (via `MainLayout`) and `Footer` |
| Bottom CTA | Reuse existing homepage `CTALaunch` (“Sign up”) |
| Approach | Content module + thin page + shared accordion row (not one giant inline page; not extracting homepage FAQs into a shared package in this pass) |

## Out of scope

- Unlocking or scraping Figma export for final copy
- Changing homepage FAQ questions or answers
- New Product / How it works / QR materials marketing pages
- New design tokens beyond matching existing marketing FAQ accordion styles
- Backend or Help Centre article sync

## Page structure

Matches the Figma frame stack (copy blocked by file export lock; layout known from canvas):

```text
MarketingHeader (MainLayout)
  → Hero (FAQ page title + short intro)
  → Seven topic sections (accordion per section)
  → CTALaunch (Sign up)
  → Footer
```

Hero uses marketing section inset/padding tokens from `@/lib/marketing-layout`, same visual family as other marketing sections. Exact hero copy is placeholder until Figma text is available; working defaults:

- Title: `FAQs`
- Body: short placeholder describing common restaurant questions about Tummly

## Sections

Seven topic sections, each with: `id`, `title`, optional short `intro`, and `items[]` of `{ id, question, answer }`.

| Order | Section id | Title | Copy mode |
|---:|---|---|---|
| 1 | `getting-started` | Getting started | Placeholder Q&A (1–3 items) |
| 2 | `guest-loop` | Guest Loop & feedback | Placeholder Q&A (1–3 items) |
| 3 | `guest-list` | Guest list & consent | Placeholder Q&A (1–3 items) |
| 4 | `offers-campaigns` | Offers & campaigns | Placeholder Q&A (1–3 items) |
| 5 | `plans-billing` | Plans & billing | Placeholder Q&A (1–3 items) |
| 6 | `ai-assistant` | Tummly AI Assistant | **Stub** — one item, stub answer |
| 7 | `trust-privacy` | Trust & Privacy | **Stub** — one item, stub answer |

Working titles for rows 1–5 are temporary. Rename and replace Q&A when final Figma copy is available. Row order may be adjusted to match Figma once export is unlocked; keep **seven** sections and the two stub ids.

Stub answer wording (intent, not final legal copy):

- AI Assistant: short note that answers for this topic will be published soon.
- Trust & Privacy: short note that answers for this topic will be published soon. Do not add a Privacy Policy link in this pass.

Placeholder answers for other sections must read as temporary (e.g. “Placeholder answer — final copy pending.”) so they are not mistaken for product claims.

## Components and files

| Unit | Responsibility |
|---|---|
| `src/content/marketing/faqsPage.ts` | Section + item data only (plain strings; no JSX) |
| `src/components/marketing/faqs/FaqsPageHero.tsx` | Page hero |
| `src/components/marketing/faqs/FaqsTopicSection.tsx` | One topic: title, optional intro, accordion |
| `src/components/marketing/faqs/FaqsAccordionItem.tsx` | Single Q&A row; match `src/components/home/Faqs.tsx` trigger/answer styles |
| `src/pages/public/FaqsPage.tsx` | Compose hero + sections + `CTALaunch` + `Footer` |
| `src/constants/marketingNav.ts` | Add `MARKETING_FAQS_PATH = "/faqs"`; point FAQ nav items to `{ kind: "route", to: MARKETING_FAQS_PATH }` |
| `src/pages/routes/AppRoutes.tsx` | Register `/faqs` → `FaqsPage` under `MainLayout` |

Do **not** remove or rewrite `src/components/home/Faqs.tsx` in this pass. Duplicate accordion class strings to match homepage FAQs; do not extract a shared package unless a later pass needs it.

## Navigation

Update both FAQ entries that currently use `{ kind: "hash", hash: MARKETING_FAQS_HASH }`:

- `MARKETING_RESOURCES_NAV` → `faq`
- `MARKETING_FOOTER_RESTAURANTS` → `faq`

Both become `{ kind: "route", to: MARKETING_FAQS_PATH }` (or `"/faqs"`).

`MarketingNavLink` already supports `kind: "route"`. No HashLink change required for this page.

## Interaction

- Accordion: `type="single"` collapsible per section (same pattern as homepage FAQs).
- Default open: first item of the first section only (same idea as homepage FAQs).
- No search, filters, or sticky section nav in this pass.

## Testing / verification

- Manual: open `/faqs` — Header, hero, seven sections, Sign up CTA, Footer.
- Manual: Resources → FAQ and Footer → FAQ navigate to `/faqs`.
- Manual: homepage `/#faqs` still scrolls to homepage FAQ.
- Manual: accordion open/close on a stub section and a placeholder section.
- No new backend tests. Frontend unit test optional for content module shape only if cheap; not required to ship the shell.

## Risks

| Risk | Mitigation |
|---|---|
| Working section titles differ from Figma | Data lives in one content module; rename is a content edit |
| Placeholder answers look like real claims | Explicit “placeholder / pending” wording |
| Figma export still locked | Shell ships without blocking on API |

## Success criteria

1. `/faqs` renders under existing marketing Header and Footer.
2. Resources → FAQ and Footer → FAQ go to `/faqs`.
3. Homepage `#faqs` still works.
4. Page has seven sections; AI Assistant and Trust & Privacy are stubs; others are clear placeholders.
5. Accordion works; Sign up CTA and Footer match existing marketing chrome.
