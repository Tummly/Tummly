# Marketing FAQs Page Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship a dedicated marketing FAQ page at `/faqs` with sectioned accordion content, link Resources → FAQ and Footer → FAQ to that route, and keep homepage `#faqs` unchanged.

**Architecture:** Plain content module drives seven topic sections. Thin presentational components (`FaqsPageHero`, `FaqsTopicSection`, `FaqsAccordionItem`) compose into `FaqsPage` with existing `CTALaunch` + `Footer`. `MainLayout` already supplies `MarketingHeader`. Nav constants switch FAQ hrefs from hash to route.

**Tech Stack:** React, React Router, shadcn/Radix Accordion, Vitest, Tailwind marketing layout tokens.

**Spec:** [docs/superpowers/specs/2026-09-21-marketing-faqs-page-design.md](../specs/2026-09-21-marketing-faqs-page-design.md)

## Global Constraints

- Path is `/faqs` (`MARKETING_FAQS_PATH`).
- Keep homepage `#faqs` / `MARKETING_FAQS_HASH` behaviour.
- Resources → FAQ and Footer → FAQ must use `{ kind: "route", to: MARKETING_FAQS_PATH }`.
- Seven sections; `ai-assistant` and `trust-privacy` are stubs; other sections use explicit placeholder answers.
- Reuse `CTALaunch` and `Footer`; do not rewrite `src/components/home/Faqs.tsx`.
- Match homepage FAQ accordion class strings (duplicate, do not extract shared package).
- Placeholder answers must include “Placeholder answer — final copy pending.” (or the stub wording below).
- Commit only when the human asks (skip Commit steps unless asked).
- Subagents: Cursor Auto / `inherit` only.
- Report to the human in ASD-STE100 Simplified Technical English.

---

## File map

| File | Role |
|---|---|
| `src/content/marketing/faqsPage.ts` | Hero copy + seven sections + items |
| `src/content/marketing/faqsPage.test.ts` | Shape / stub / placeholder guards |
| `src/components/marketing/faqs/FaqsAccordionItem.tsx` | One Q&A accordion row |
| `src/components/marketing/faqs/FaqsTopicSection.tsx` | Section title + accordion list |
| `src/components/marketing/faqs/FaqsPageHero.tsx` | Page hero |
| `src/pages/public/FaqsPage.tsx` | Page composition |
| `src/constants/marketingNav.ts` | `MARKETING_FAQS_PATH` + FAQ href updates |
| `src/pages/routes/AppRoutes.tsx` | Register `/faqs` |

---

### Task 1: FAQ content module + shape test

**Files:**
- Create: `src/content/marketing/faqsPage.ts`
- Test: `src/content/marketing/faqsPage.test.ts`

**Interfaces:**
- Consumes: none
- Produces:

```ts
export type FaqsPageItem = {
  id: string
  question: string
  answer: string
}

export type FaqsPageSection = {
  id: string
  title: string
  intro?: string
  items: FaqsPageItem[]
}

export const FAQS_PAGE_HERO: { title: string; body: string }
export const FAQS_PAGE_SECTIONS: FaqsPageSection[]
export const FAQS_PAGE_DEFAULT_OPEN_VALUE: string // `${sectionId}::${itemId}` of first item in first section
```

- [ ] **Step 1: Write the failing test**

```ts
import { describe, expect, it } from "vitest"

import {
  FAQS_PAGE_DEFAULT_OPEN_VALUE,
  FAQS_PAGE_HERO,
  FAQS_PAGE_SECTIONS,
} from "./faqsPage"

describe("faqsPage content", () => {
  it("exposes a hero title and body", () => {
    expect(FAQS_PAGE_HERO.title.length).toBeGreaterThan(0)
    expect(FAQS_PAGE_HERO.body.length).toBeGreaterThan(0)
  })

  it("has exactly seven sections with required stub ids", () => {
    expect(FAQS_PAGE_SECTIONS).toHaveLength(7)
    expect(FAQS_PAGE_SECTIONS.map((s) => s.id)).toEqual([
      "getting-started",
      "guest-loop",
      "guest-list",
      "offers-campaigns",
      "plans-billing",
      "ai-assistant",
      "trust-privacy",
    ])
  })

  it("uses stub answers for AI Assistant and Trust & Privacy", () => {
    const ai = FAQS_PAGE_SECTIONS.find((s) => s.id === "ai-assistant")
    const trust = FAQS_PAGE_SECTIONS.find((s) => s.id === "trust-privacy")
    expect(ai?.items).toHaveLength(1)
    expect(trust?.items).toHaveLength(1)
    expect(ai?.items[0]?.answer).toMatch(/published soon/i)
    expect(trust?.items[0]?.answer).toMatch(/published soon/i)
  })

  it("marks non-stub answers as placeholders", () => {
    for (const section of FAQS_PAGE_SECTIONS) {
      if (section.id === "ai-assistant" || section.id === "trust-privacy") {
        continue
      }
      expect(section.items.length).toBeGreaterThan(0)
      for (const item of section.items) {
        expect(item.answer).toContain("Placeholder answer — final copy pending.")
      }
    }
  })

  it("default open value matches the first item of the first section", () => {
    const first = FAQS_PAGE_SECTIONS[0]?.items[0]
    expect(first).toBeDefined()
    expect(FAQS_PAGE_DEFAULT_OPEN_VALUE).toBe(
      `${FAQS_PAGE_SECTIONS[0]!.id}::${first!.id}`
    )
  })
})
```

- [ ] **Step 2: Run test to verify it fails**

Run: `npm test -- src/content/marketing/faqsPage.test.ts`

Expected: FAIL (module not found / exports missing)

- [ ] **Step 3: Write minimal implementation**

Create `src/content/marketing/faqsPage.ts`:

```ts
export type FaqsPageItem = {
  id: string
  question: string
  answer: string
}

export type FaqsPageSection = {
  id: string
  title: string
  intro?: string
  items: FaqsPageItem[]
}

export const FAQS_PAGE_HERO = {
  title: "FAQs",
  body: "Answers to common questions about Tummly for restaurants. Final copy will replace these placeholders.",
} as const

const PLACEHOLDER_ANSWER =
  "Placeholder answer — final copy pending."

const STUB_ANSWER =
  "Answers for this topic will be published soon."

export const FAQS_PAGE_SECTIONS: FaqsPageSection[] = [
  {
    id: "getting-started",
    title: "Getting started",
    intro: "Placeholder section intro — final copy pending.",
    items: [
      {
        id: "what-is-tummly",
        question: "What is Tummly?",
        answer: PLACEHOLDER_ANSWER,
      },
      {
        id: "how-to-start",
        question: "How do I start with Tummly?",
        answer: PLACEHOLDER_ANSWER,
      },
    ],
  },
  {
    id: "guest-loop",
    title: "Guest Loop & feedback",
    items: [
      {
        id: "what-is-guest-loop",
        question: "What is a Guest Loop?",
        answer: PLACEHOLDER_ANSWER,
      },
      {
        id: "how-guests-leave-feedback",
        question: "How do guests leave feedback?",
        answer: PLACEHOLDER_ANSWER,
      },
    ],
  },
  {
    id: "guest-list",
    title: "Guest list & consent",
    items: [
      {
        id: "who-controls-guest-list",
        question: "Who controls the guest list?",
        answer: PLACEHOLDER_ANSWER,
      },
      {
        id: "how-consent-works",
        question: "How does guest consent work?",
        answer: PLACEHOLDER_ANSWER,
      },
    ],
  },
  {
    id: "offers-campaigns",
    title: "Offers & campaigns",
    items: [
      {
        id: "what-are-offers",
        question: "What are offers and campaigns?",
        answer: PLACEHOLDER_ANSWER,
      },
      {
        id: "how-to-send",
        question: "How do I send a return offer?",
        answer: PLACEHOLDER_ANSWER,
      },
    ],
  },
  {
    id: "plans-billing",
    title: "Plans & billing",
    items: [
      {
        id: "is-payment-taken-on-signup",
        question: "Is payment taken when I sign up?",
        answer: PLACEHOLDER_ANSWER,
      },
      {
        id: "what-plans-exist",
        question: "What plans are available?",
        answer: PLACEHOLDER_ANSWER,
      },
    ],
  },
  {
    id: "ai-assistant",
    title: "Tummly AI Assistant",
    items: [
      {
        id: "ai-assistant-stub",
        question: "What can the Tummly AI Assistant do?",
        answer: STUB_ANSWER,
      },
    ],
  },
  {
    id: "trust-privacy",
    title: "Trust & Privacy",
    items: [
      {
        id: "trust-privacy-stub",
        question: "How does Tummly handle trust and privacy?",
        answer: STUB_ANSWER,
      },
    ],
  },
]

export const FAQS_PAGE_DEFAULT_OPEN_VALUE = `${FAQS_PAGE_SECTIONS[0]!.id}::${FAQS_PAGE_SECTIONS[0]!.items[0]!.id}`
```

- [ ] **Step 4: Run test to verify it passes**

Run: `npm test -- src/content/marketing/faqsPage.test.ts`

Expected: PASS

- [ ] **Step 5: Commit** (skip unless the human asks)

```bash
git add src/content/marketing/faqsPage.ts src/content/marketing/faqsPage.test.ts
git commit -m "$(cat <<'EOF'
Add marketing FAQs page content shell with placeholders.

EOF
)"
```

---

### Task 2: FAQ section UI components

**Files:**
- Create: `src/components/marketing/faqs/FaqsAccordionItem.tsx`
- Create: `src/components/marketing/faqs/FaqsTopicSection.tsx`
- Create: `src/components/marketing/faqs/FaqsPageHero.tsx`

**Interfaces:**
- Consumes: `FaqsPageItem`, `FaqsPageSection`, `FAQS_PAGE_HERO` from `@/content/marketing/faqsPage`
- Produces:

```tsx
export function FaqsAccordionItem(props: {
  value: string
  question: string
  answer: string
}): JSX.Element

export function FaqsTopicSection(props: {
  section: FaqsPageSection
  defaultOpenValue?: string
}): JSX.Element

export function FaqsPageHero(): JSX.Element
```

Accordion value format: `${section.id}::${item.id}` (must match `FAQS_PAGE_DEFAULT_OPEN_VALUE`).

- [ ] **Step 1: Create `FaqsAccordionItem.tsx`**

Match class strings from `src/components/home/Faqs.tsx`:

```tsx
import {
  AccordionContent,
  AccordionItem,
  AccordionTrigger,
} from "@/components/ui/accordion"

const faqTriggerClassName =
  "items-center rounded-none border-0 py-0 hover:no-underline hover:cursor-pointer focus-visible:border-0 focus-visible:ring-0 **:data-[slot=accordion-trigger-icon]:box-content **:data-[slot=accordion-trigger-icon]:size-3 **:data-[slot=accordion-trigger-icon]:shrink-0 **:data-[slot=accordion-trigger-icon]:rounded-full **:data-[slot=accordion-trigger-icon]:bg-[#232323] **:data-[slot=accordion-trigger-icon]:p-1.75 **:data-[slot=accordion-trigger-icon]:text-white"

const faqQuestionClassName =
  "m-0 min-w-0 flex-1 text-left font-sans text-[22px] font-semibold leading-[normal] tracking-normal text-[#232323]"

const faqContentClassName = "pt-3 pb-0 pr-6"

const faqAnswerClassName =
  "m-0 font-sans text-base font-normal leading-5.5 tracking-normal text-[#232323]"

type FaqsAccordionItemProps = {
  value: string
  question: string
  answer: string
}

export function FaqsAccordionItem({
  value,
  question,
  answer,
}: FaqsAccordionItemProps) {
  return (
    <AccordionItem
      value={value}
      className="border-0 border-b border-[#e0e0e0] pb-3.5"
    >
      <AccordionTrigger className={faqTriggerClassName}>
        <span className={faqQuestionClassName}>{question}</span>
      </AccordionTrigger>
      <AccordionContent className={faqContentClassName}>
        <p className={faqAnswerClassName}>{answer}</p>
      </AccordionContent>
    </AccordionItem>
  )
}
```

- [ ] **Step 2: Create `FaqsTopicSection.tsx`**

```tsx
import { Accordion } from "@/components/ui/accordion"
import { FaqsAccordionItem } from "@/components/marketing/faqs/FaqsAccordionItem"
import type { FaqsPageSection } from "@/content/marketing/faqsPage"
import {
  marketingSectionBody,
  marketingSectionHeading,
  marketingSectionInset,
  marketingSectionPadding,
} from "@/lib/marketing-layout"
import { cn } from "@/lib/utils"

type FaqsTopicSectionProps = {
  section: FaqsPageSection
  defaultOpenValue?: string
}

export function FaqsTopicSection({
  section,
  defaultOpenValue,
}: FaqsTopicSectionProps) {
  const sectionDefault =
    defaultOpenValue?.startsWith(`${section.id}::`) === true
      ? defaultOpenValue
      : undefined

  return (
    <section
      id={section.id}
      className="w-full scroll-mt-44 bg-white"
    >
      <div
        className={cn(
          "mx-auto flex w-full flex-col gap-8 sm:gap-10 lg:flex-row lg:items-start lg:gap-10 xl:gap-16 2xl:gap-24 min-[1728px]:gap-47.5",
          marketingSectionInset,
          marketingSectionPadding,
        )}
      >
        <header className="flex w-full shrink-0 flex-col gap-3 sm:max-w-sm lg:max-w-72 xl:max-w-99.5">
          <h2 className={cn("m-0", marketingSectionHeading)}>{section.title}</h2>
          {section.intro ? (
            <p className={cn("m-0", marketingSectionBody)}>{section.intro}</p>
          ) : null}
        </header>

        <Accordion
          type="single"
          collapsible
          className="min-w-0 flex-1 gap-8.5"
          defaultValue={sectionDefault}
        >
          {section.items.map((item) => (
            <FaqsAccordionItem
              key={item.id}
              value={`${section.id}::${item.id}`}
              question={item.question}
              answer={item.answer}
            />
          ))}
        </Accordion>
      </div>
    </section>
  )
}
```

- [ ] **Step 3: Create `FaqsPageHero.tsx`**

```tsx
import { FAQS_PAGE_HERO } from "@/content/marketing/faqsPage"
import {
  marketingSectionBody,
  marketingSectionHeading,
  marketingSectionInset,
  marketingSectionPadding,
} from "@/lib/marketing-layout"
import { cn } from "@/lib/utils"

export function FaqsPageHero() {
  return (
    <section className="w-full bg-[#fafafa]">
      <div
        className={cn(
          "mx-auto flex w-full max-w-[1568px] flex-col gap-3",
          marketingSectionInset,
          marketingSectionPadding,
        )}
      >
        <h1 className={cn("m-0", marketingSectionHeading)}>
          {FAQS_PAGE_HERO.title}
        </h1>
        <p className={cn("m-0 max-w-3xl", marketingSectionBody)}>
          {FAQS_PAGE_HERO.body}
        </p>
      </div>
    </section>
  )
}
```

- [ ] **Step 4: Commit** (skip unless the human asks)

```bash
git add src/components/marketing/faqs/
git commit -m "$(cat <<'EOF'
Add marketing FAQs page section components.

EOF
)"
```

---

### Task 3: FaqsPage + App route

**Files:**
- Create: `src/pages/public/FaqsPage.tsx`
- Modify: `src/pages/routes/AppRoutes.tsx` (add import near other public pages; add route next to `privacy` / `unsubscribe`)

**Interfaces:**
- Consumes: `FaqsPageHero`, `FaqsTopicSection`, `FAQS_PAGE_SECTIONS`, `FAQS_PAGE_DEFAULT_OPEN_VALUE`, `CTALaunch`, `Footer`
- Produces: default-exported `FaqsPage` component; route `path="faqs"`

- [ ] **Step 1: Create `FaqsPage.tsx`**

```tsx
import CTALaunch from "@/components/home/CTALaunch"
import Footer from "@/components/home/Footer"
import { FaqsPageHero } from "@/components/marketing/faqs/FaqsPageHero"
import { FaqsTopicSection } from "@/components/marketing/faqs/FaqsTopicSection"
import {
  FAQS_PAGE_DEFAULT_OPEN_VALUE,
  FAQS_PAGE_SECTIONS,
} from "@/content/marketing/faqsPage"

export default function FaqsPage() {
  return (
    <>
      <FaqsPageHero />
      {FAQS_PAGE_SECTIONS.map((section) => (
        <FaqsTopicSection
          key={section.id}
          section={section}
          defaultOpenValue={FAQS_PAGE_DEFAULT_OPEN_VALUE}
        />
      ))}
      <CTALaunch />
      <Footer />
    </>
  )
}
```

- [ ] **Step 2: Register the route in `AppRoutes.tsx`**

Add import with other public pages:

```tsx
import FaqsPage from "../public/FaqsPage";
```

Inside the `MainLayout` route group, next to privacy/terms (around the `privacy` route), add:

```tsx
<Route path="faqs" element={<FaqsPage />} />
```

Place it **outside** `PublicOnlyRoute` and **outside** `ProtectedRoute` (same as `privacy` / `unsubscribe`), so signed-in and signed-out users can open it.

- [ ] **Step 3: Smoke-check TypeScript on touched files**

Run: `npx tsc --noEmit -p tsconfig.app.json 2>&1 | head -n 40`

Expected: no errors referencing `FaqsPage` / `faqsPage` / marketing faqs components (existing unrelated project errors may still appear; ignore those).

- [ ] **Step 4: Commit** (skip unless the human asks)

```bash
git add src/pages/public/FaqsPage.tsx src/pages/routes/AppRoutes.tsx
git commit -m "$(cat <<'EOF'
Add /faqs marketing page route.

EOF
)"
```

---

### Task 4: Point Resources and Footer FAQ links to `/faqs`

**Files:**
- Modify: `src/constants/marketingNav.ts`

**Interfaces:**
- Consumes: existing `MarketingNavItem` / `MarketingNavHref`
- Produces: `export const MARKETING_FAQS_PATH = "/faqs"`
- Keeps: `MARKETING_FAQS_HASH = "#faqs"` for homepage scroll

- [ ] **Step 1: Add path constant and update FAQ hrefs**

After `MARKETING_FAQS_HASH`, add:

```ts
export const MARKETING_FAQS_PATH = "/faqs"
```

In `MARKETING_RESOURCES_NAV`, change the FAQ item from:

```ts
{
  id: "faq",
  label: "FAQ",
  href: { kind: "hash", hash: MARKETING_FAQS_HASH },
},
```

to:

```ts
{
  id: "faq",
  label: "FAQ",
  href: { kind: "route", to: MARKETING_FAQS_PATH },
},
```

In `MARKETING_FOOTER_RESTAURANTS`, change the FAQ item the same way (route to `MARKETING_FAQS_PATH`).

Do **not** change `HomePage` hash scroll logic.

- [ ] **Step 2: Re-run content tests**

Run: `npm test -- src/content/marketing/faqsPage.test.ts`

Expected: PASS

- [ ] **Step 3: Commit** (skip unless the human asks)

```bash
git add src/constants/marketingNav.ts
git commit -m "$(cat <<'EOF'
Link marketing Resources and Footer FAQ to /faqs.

EOF
)"
```

---

### Task 5: Manual browser verification

**Files:** none (verification only)

- [ ] **Step 1: Start (or use) the Vite app and open `/faqs`**

Confirm:
1. Marketing Header is present.
2. Hero shows “FAQs”.
3. Seven sections render with titles from the content module.
4. First item of Getting started is open by default; accordion collapses/expands.
5. Stub sections show “published soon” answers.
6. Other sections show “Placeholder answer — final copy pending.”
7. Sign up CTA (`CTALaunch`) and Footer render.

- [ ] **Step 2: Click Resources → FAQ**

Expected: navigates to `/faqs` (not `/#faqs`).

- [ ] **Step 3: Click Footer → FAQ**

Expected: navigates to `/faqs`.

- [ ] **Step 4: Open `/#faqs` on the homepage**

Expected: homepage FAQ section still scrolls into view; homepage FAQ content unchanged.

---

## Spec coverage checklist

| Spec requirement | Task |
|---|---|
| `/faqs` under MainLayout Header | Task 3 |
| Resources + Footer FAQ → `/faqs` | Task 4 |
| Keep homepage `#faqs` | Task 4 (explicit non-change) + Task 5 |
| Hero + seven sections + CTALaunch + Footer | Tasks 2–3 |
| Stub AI Assistant + Trust & Privacy | Task 1 |
| Placeholder other sections | Task 1 |
| Accordion match homepage style | Task 2 |
| Content module + thin components | Tasks 1–2 |
| Manual verification | Task 5 |

## Self-review notes

- No TBD/TODO left in steps.
- Accordion value format is consistent: `${section.id}::${item.id}`.
- `MARKETING_FAQS_HASH` remains for `HomePage`; only FAQ nav entries switch to route.
- Working section titles for rows 1–5 are temporary per spec; content-only rename later.
