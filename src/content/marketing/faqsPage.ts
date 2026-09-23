import { MARKETING_PRICING_PATH } from "@/constants/marketingNav"

export type FaqsPageItem = {
  id: string
  question: string
  /** Paragraph blocks separated by blank lines (`\n\n`); use `\n` for tight list lines. */
  answer: string
  /** Optional trailing CTA row (Figma “Start 30-day Pilot →” / “Compare all plans →”). */
  cta?: {
    label: string
    to: string
  }
}

export type FaqsPageSection = {
  id: string
  title: string
  intro?: string
  items: FaqsPageItem[]
}

/** Figma FAQ hero (`Frame 269`). */
export const FAQS_PAGE_HERO = {
  title: "Questions? Start here.",
  body: "Everything you need to know about setting up Tummly, collecting private Feedback, building guest relationships and running Guest Loop in your restaurant.",
  searchPlaceholder: "Search Tummly FAQs",
} as const

/** Figma FAQ Sign up CTA (`4974:26424`). */
export const FAQS_PAGE_SIGN_UP_CTA = {
  title: "Ready to see Guest Loop in a real restaurant?",
  body: "Start with one Location and see how Guest Loop works with real guests.",
  primaryLabel: "Start 30-day Pilot",
  secondaryLabel: "See pricing",
  badge: "1 verified Location · No payment card required",
} as const

/**
 * AI Assistant and Trust & privacy rows are collapsed in Figma with question
 * overrides only — no answer overrides exist on those instances yet.
 */
const STUB_ANSWER =
  "Answers for this topic will be published soon."

/**
 * Section order, titles, questions and answers follow Figma FAQ frame `4974:26242`.
 */
export const FAQS_PAGE_SECTIONS: FaqsPageSection[] = [
  {
    id: "getting-started",
    title: "Getting started",
    items: [
      {
        id: "what-is-tummly",
        question: "What is Tummly?",
        answer:
          "Tummly Guest Loop helps restaurants turn everyday guest interactions into relationships they can build on.\n\nGuests scan a QR or Smart Guest Link to leave private Feedback and, if they choose, provide a contact method and relevant permissions. Restaurants can then manage Feedback, build a guest list, use Offers and Campaigns, understand performance and use Tummly AI to prepare the next step.",
      },
      {
        id: "who-is-tummly-for",
        question: "Who is Tummly for?",
        answer:
          "Tummly is built for independent and growing QSR, takeaway, café and hospitality operators.\n\nIt works for single-Location restaurants as well as emerging multi-Location groups.",
      },
      {
        id: "need-website",
        question: "Do I need a website to use Tummly?",
        answer:
          "No.\n\nTummly can work through QR touchpoints placed around your restaurant, packaging, collection and delivery experience.\n\nA restaurant website is not required.",
      },
      {
        id: "need-printer",
        question: "Do I need my own printer?",
        answer:
          "No.\n\nTummly supports professionally prepared QR materials, while digital and self-print options remain available when useful.",
      },
      {
        id: "need-pos",
        question: "Do I need a POS integration?",
        answer:
          "No.\n\nGuest Loop is designed to work without replacing or depending on your POS.",
      },
      {
        id: "how-to-start",
        question: "How do I get started?",
        answer:
          "Choose the 30-day Pilot or a paid plan, create your account and verify your Email.\n\nWe'll then guide you through your restaurant and Location details, Guest Form and QR setup.",
        cta: {
          label: "Start 30-day Pilot →",
          to: "/signup",
        },
      },
      {
        id: "verify-restaurant",
        question: "Do you need to verify my restaurant?",
        answer:
          "Yes. Tummly verifies the business and Location during setup.\n\nThe normal process is automated first. If something cannot be verified automatically, additional review may be required.",
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
        answer:
          "Yes.\n\nThe Pilot is £0 for 30 days for one qualifying verified Location.\n\nIt includes a limited set of QR placements, Offers and AI, Email and SMS usage so you can test Guest Loop with real guests.",
      },
      {
        id: "need-card-for-pilot",
        question: "Do I need a payment card for the Pilot?",
        answer:
          "No.\n\nNo payment card is required to start the 30-day Pilot.",
      },
      {
        id: "auto-charge-after-pilot",
        question: "Will I automatically be charged after the Pilot?",
        answer:
          "No.\n\nThe Pilot does not automatically convert into a paid subscription.\n\nAt the end of the Pilot, you can choose whether to move onto a paid plan.",
      },
      {
        id: "when-pilot-starts",
        question: "When does my 30-day Pilot start?",
        answer:
          "Your setup access starts before the live Pilot clock begins.\n\nThe 30-day live Pilot starts at the earlier of:\n\nthe first valid production guest scan; or\n48 hours after your included starter kit is confirmed delivered.\n\nPreview scans, test scans, bots and development traffic do not start the Pilot.",
      },
      {
        id: "when-pilot-ends",
        question: "What happens when the 30-day Pilot ends?",
        answer:
          "The Pilot ends without an automatic charge.\n\nYour account moves out of live Pilot use, while the appropriate account history and conversion path are preserved so you can choose a paid plan.",
      },
      {
        id: "how-much-cost",
        question: "How much does Tummly cost?",
        answer:
          "Current UK monthly plans are:\n\nPilot — £0 for 30 days\nStarter — £39/month\nGrowth — £99/month\nGroup — £199/month\n\nAnnual options are also available for paid plans.",
        cta: {
          label: "Compare all plans →",
          to: MARKETING_PRICING_PATH,
        },
      },
      {
        id: "ai-email-sms-allowances",
        question: "What are AI, Email and SMS allowances?",
        answer:
          "Each paid plan includes separate monthly allowances for:\n\nAI actions\nCampaign Email credits\nSMS credits\n\nThey are separate balances rather than one combined usage allowance.",
      },
      {
        id: "credits-exhausted",
        question: "What happens if I use all my Email or SMS credits?",
        answer:
          "Only the affected channel stops when its available balance is exhausted.\n\nFor example, using all SMS credits does not stop Email or normal Feedback collection.\n\nEligible paid accounts can purchase additional top-up packs.",
      },
      {
        id: "credits-roll-over",
        question: "Do unused monthly credits roll over?",
        answer:
          "No.\n\nIncluded monthly AI, Email and SMS allowances do not roll over to the next billing period.\n\nPurchased top-up packs follow their separate validity rules.",
      },
    ],
  },
  {
    id: "qr-materials",
    title: "QR materials",
    items: [
      {
        id: "where-put-qr",
        question: "Where can I put a Tummly QR?",
        answer:
          "Guest Loop can be placed across suitable restaurant touchpoints, including:\n\ncounters\ntables\nreceipts\npackaging\ndelivery inserts\nwindows\n\nYou can choose placements that make sense for how your guests order, collect, dine or receive deliveries.",
      },
      {
        id: "what-qr-materials",
        question: "What QR materials are available?",
        answer:
          "Tummly's launch direction includes materials such as:\n\ncounter cards\ntable tents\nreceipt stickers\npackaging stickers\ndelivery inserts\nwindow stickers\ndigital and self-print QR assets\n\nThe live QR materials catalogue shows the products currently available to order.",
      },
      {
        id: "starter-kit-included",
        question: "Is a starter kit included?",
        answer:
          "Qualifying accounts can receive one included physical starter-kit entitlement per Billing Account lifetime.\n\nThe exact contents can vary according to the current Tummly catalogue and configuration.",
      },
      {
        id: "wait-for-materials",
        question: "Do I have to wait for QR materials before I can use Tummly?",
        answer:
          "No.\n\nYour software setup can begin before your physical materials arrive.\n\nThe physical parcel is not required to log in or access your account.",
      },
      {
        id: "print-own-qr",
        question: "Can I print my own QR code?",
        answer:
          "Yes.\n\nDigital and self-print assets are available where appropriate.\n\nTummly's physical materials are there for restaurants that want professionally prepared deployment or do not have a suitable printer.",
      },
      {
        id: "reorder-qr",
        question: "What happens if I reorder QR materials?",
        answer:
          "A normal reorder keeps the existing QR placement and its attribution.\n\nReordering does not normally create a new QR identity or cause already printed materials to stop working.",
      },
      {
        id: "track-qr-placements",
        question: "Can I see which QR placements guests use?",
        answer:
          "Yes.\n\nTummly maintains source attribution for individual QR placements so you can understand which touchpoints are generating Guest Loop activity.",
      },
    ],
  },
  {
    id: "feedback-guests",
    title: "Feedback & Guests",
    items: [
      {
        id: "is-feedback-public",
        question: "Is Feedback left through Tummly public?",
        answer:
          "No.\n\nTummly Guest Loop is designed around private Feedback between the guest and the restaurant.\n\nPrivate Feedback is separate from public-review platforms.",
      },
      {
        id: "must-leave-contact",
        question: "Does a guest have to leave their contact details?",
        answer:
          "No.\n\nGuests can submit Feedback without being required to provide marketing contact details.\n\nContact capture comes after the core Feedback interaction and is optional where presented.",
      },
      {
        id: "what-guest-info",
        question: "What information do you ask guests for?",
        answer:
          "The Guest Form is designed to stay short.\n\nA typical flow can include:\n\na rating\nquick Feedback selections\nan optional comment\nfirst name\none appropriate contact method\nseparate permission choices\n\nThe exact configured experience can vary.",
      },
      {
        id: "email-means-marketing",
        question: "Does having a guest's Email mean I can send them marketing?",
        answer:
          "No.\n\nHaving a contact method and having permission to use that contact method for marketing are different things.\n\nTummly keeps contact state and permission state separate.",
      },
      {
        id: "email-sms-permissions",
        question: "Are Email and SMS permissions the same?",
        answer:
          "No.\n\nTummly treats relevant permission purposes separately, including:\n\nFeedback follow-up\nEmail marketing\nSMS marketing\n\nPermission for one purpose or channel does not automatically become permission for another.",
      },
      {
        id: "withdraw-permission",
        question: "Can guests withdraw permission?",
        answer:
          "Yes.\n\nWithdrawal and suppression are reflected in future contact eligibility.\n\nTummly's Campaign eligibility checks are designed so withdrawn or suppressed guests cannot simply be added back into a send.",
      },
      {
        id: "guest-history",
        question: "Can I see an individual guest's history?",
        answer:
          "Where your role and Location permissions allow it, a guest profile can show relevant Guest Loop history such as Feedback, contact state, permission context, Campaign activity and Offer activity.",
      },
    ],
  },
  {
    id: "campaigns-offers",
    title: "Campaigns & Offers",
    items: [
      {
        id: "email-campaigns",
        question: "Can I send Email Campaigns with Tummly?",
        answer:
          "Yes.\n\nTummly supports Campaign Email to guests who are eligible for the selected Campaign and channel.\n\nEligibility is checked before sending.",
      },
      {
        id: "sms-campaigns",
        question: "Can I send SMS Campaigns?",
        answer:
          "Yes, where SMS is available on your plan and the guest is eligible for SMS marketing.\n\nSMS and Email use separate composition and credit rules.",
      },
      {
        id: "ai-choose-audience",
        question: "Can AI choose who receives a Campaign?",
        answer:
          "AI can help explain or propose audience criteria, but it does not override Tummly's eligibility rules.\n\nFinal recipient eligibility is determined by the product using current permission, suppression, contact and account rules.",
      },
      {
        id: "auto-send-campaigns",
        question: "Can Tummly send Campaigns automatically for me?",
        answer:
          "Not as an autonomous AI action.\n\nTummly AI can prepare a Campaign Draft, but consequential sends require the appropriate operator review and confirmation.",
      },
      {
        id: "what-is-offer",
        question: "What is an Offer?",
        answer:
          "An Offer gives you a controlled way to give eligible guests a reason to return.\n\nOffers can include defined validity, expiry and Redemption controls rather than behaving like unrestricted coupon codes.",
      },
      {
        id: "offers-for-reviews",
        question: "Can Offers be used to encourage positive public reviews?",
        answer:
          "No.\n\nTummly does not support rewarding guests for positive public reviews or hiding public-review opportunities from guests who leave negative Feedback.\n\nPrivate Feedback and public reviews remain separate.",
      },
      {
        id: "offer-redemption",
        question: "How does Offer Redemption work?",
        answer:
          "An Offer can use a controlled Claim or pass and track whether it is:\n\nvalid\nredeemed\nexpired\nalready used\nnot found\nineligible\n\nThis helps reduce accidental reuse and abuse.",
      },
    ],
  },
  {
    id: "ai-assistant",
    title: "Tummly AI Assistant",
    items: [
      {
        id: "what-can-ai-do",
        question: "What can Tummly AI do?",
        answer: STUB_ANSWER,
      },
      {
        id: "ai-send-without-me",
        question: "Can Tummly AI send messages without me?",
        answer: STUB_ANSWER,
      },
      {
        id: "ai-change-permissions",
        question: "Can AI change guest permissions?",
        answer: STUB_ANSWER,
      },
      {
        id: "weekly-brief-allowance",
        question: "Does the Weekly Brief use my AI allowance?",
        answer: STUB_ANSWER,
      },
      {
        id: "what-counts-ai-action",
        question: "What counts as an AI action?",
        answer: STUB_ANSWER,
      },
      {
        id: "ai-unavailable",
        question: "What happens if AI is unavailable?",
        answer: STUB_ANSWER,
      },
    ],
  },
  {
    id: "trust-privacy",
    title: "Trust & privacy",
    items: [
      {
        id: "handle-guest-permissions",
        question: "How does Tummly handle guest permissions?",
        answer: STUB_ANSWER,
      },
      {
        id: "combine-guests",
        question: "Does Tummly combine guests across different restaurants?",
        answer: STUB_ANSWER,
      },
      {
        id: "sell-guest-contacts",
        question: "Does Tummly sell restaurant guest contacts to other restaurants?",
        answer: STUB_ANSWER,
      },
      {
        id: "team-see-every-guest",
        question: "Can my team see every guest?",
        answer: STUB_ANSWER,
      },
      {
        id: "correct-or-delete-data",
        question: "Can guests request their data to be corrected or deleted?",
        answer: STUB_ANSWER,
      },
      {
        id: "manipulate-public-reviews",
        question: "Does Tummly manipulate public reviews?",
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
          || item.cta?.label.toLowerCase().includes(normalized) === true
          || section.title.toLowerCase().includes(normalized),
      ),
    }))
    .filter((section) => section.items.length > 0)
}
