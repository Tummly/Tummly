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

/** Figma FAQ hero (`Frame 269` / prototype). */
export const FAQS_PAGE_HERO = {
  title: "Questions? Start here.",
  body: "Everything you need to know about setting up Tummly, collecting private Feedback, building guest relationships and running Guest Loop in your restaurant.",
  searchPlaceholder: "Search Tummly FAQs",
} as const

const PLACEHOLDER_ANSWER =
  "Placeholder answer — final copy pending."

const STUB_ANSWER =
  "Answers for this topic will be published soon."

/**
 * Section order and titles follow the Figma FAQ frame stack.
 * Getting started questions match the prototype list.
 * Full answer copy still pending (Figma export locked) except stub sections.
 */
export const FAQS_PAGE_SECTIONS: FaqsPageSection[] = [
  {
    id: "getting-started",
    title: "Getting started",
    items: [
      {
        id: "what-is-tummly",
        question: "What is Tummly?",
        answer: PLACEHOLDER_ANSWER,
      },
      {
        id: "who-is-tummly-for",
        question: "Who is Tummly for?",
        answer: PLACEHOLDER_ANSWER,
      },
      {
        id: "need-website",
        question: "Do I need a website to use Tummly?",
        answer: PLACEHOLDER_ANSWER,
      },
      {
        id: "need-printer",
        question: "Do I need my own printer?",
        answer: PLACEHOLDER_ANSWER,
      },
      {
        id: "need-pos",
        question: "Do I need a POS integration?",
        answer: PLACEHOLDER_ANSWER,
      },
      {
        id: "how-to-start",
        question: "How do I get started?",
        answer: PLACEHOLDER_ANSWER,
      },
      {
        id: "verify-restaurant",
        question: "Do you need to verify my restaurant?",
        answer: PLACEHOLDER_ANSWER,
      },
    ],
  },
  {
    id: "pilot-pricing",
    title: "Pilot & pricing",
    items: [
      {
        id: "is-pilot-free",
        question: "Is the Tummly Pilot free?",
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

export function filterFaqsPageSections(
  sections: FaqsPageSection[],
  query: string,
): FaqsPageSection[] {
  const normalized = query.trim().toLowerCase()
  if (normalized.length === 0) {
    return sections
  }

  return sections
    .map((section) => ({
      ...section,
      items: section.items.filter(
        (item) =>
          item.question.toLowerCase().includes(normalized)
          || item.answer.toLowerCase().includes(normalized)
          || section.title.toLowerCase().includes(normalized),
      ),
    }))
    .filter((section) => section.items.length > 0)
}
