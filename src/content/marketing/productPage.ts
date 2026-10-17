import type { MarketingSignUpCtaContent } from "@/components/marketing/MarketingSignUpCta"
import {
  MARKETING_AI_ASSISTANT_PATH,
  MARKETING_FAQS_PATH,
  MARKETING_FOR_GROUPS_PATH,
  MARKETING_HOW_IT_WORKS_PATH,
  MARKETING_QR_MATERIALS_PATH,
  MARKETING_TRUST_PRIVACY_PATH,
} from "@/constants/marketingNav"

/** Shared section inset — matches AI Assistant / Pricing (20px mobile, 60px desktop). */
export const PRODUCT_SECTION_INSET = "px-5 lg:px-[60px]"

/** Shared vertical pad — 50px mobile, 70px desktop. Figma hero `5661:22625`. */
export const PRODUCT_SECTION_Y = "py-12.5 lg:py-[70px]"

export const PRODUCT_SECTION_HEADING =
  "font-jakarta text-[34px] font-medium leading-none text-black lg:text-[46px] lg:leading-[58px]"

export const PRODUCT_SECTION_BODY =
  "font-sans text-base font-normal leading-[22px] text-[#141414] lg:text-[18px] lg:leading-6"

/** Figma Product hero `5661:22625`. */
export const PRODUCT_HERO = {
  titleLine1: "Run the whole Guest Loop",
  titleLine2: "from one place.",
  body: "Manage QR touchpoints, private Feedback, guest relationships, Offers, Campaigns and reporting from one restaurant-first workspace — with Tummly AI available to help you understand what is happening and prepare the next step.",
  primaryLabel: "Start 30-day Pilot",
  secondaryLabel: "See how it works",
  secondaryTo: MARKETING_HOW_IT_WORKS_PATH,
  trustItems: ["£0 for 30 days", "No payment card required"] as const,
  dashboardImageAlt:
    "Restaurant operator at a laptop with a Campaign Draft ready for review, recent guests selected, and follow-up available.",
} as const

export type ProductGuestLoopStep = {
  id: string
  title: string
  body: string
}

/** Figma Guest Loop overview `5661:22650`. */
export const PRODUCT_GUEST_LOOP = {
  eyebrow: "The Guest Loop",
  title: "From first scan to the next useful action.",
  body: "Tummly connects what happens in the restaurant with what your team can do next — from a QR interaction and private Feedback to guest follow-up, Campaigns, Offers and clearer reporting.",
  steps: [
    {
      id: "capture",
      title: "Capture",
      body: "Tracked QR touchpoints connect guests to the right Guest Form.",
    },
    {
      id: "feedback",
      title: "Feedback",
      body: "Private Feedback arrives with Location and source context.",
    },
    {
      id: "guests",
      title: "Guests",
      body: "Guest details and contactability stay connected to activity.",
    },
    {
      id: "act",
      title: "Act",
      body: "Prepare recovery, Campaigns and Offers for operator review.",
    },
    {
      id: "learn",
      title: "Learn",
      body: "Reports, Weekly Briefs and AI show what changed and where to look next.",
    },
  ] satisfies ProductGuestLoopStep[],
} as const

/** Figma Product Capture / QR materials `5661:22668`. */
export const PRODUCT_CAPTURE = {
  eyebrow: "Capture",
  title: "Capture starts with the right touchpoint.",
  body: "Create tracked QR placements for the moments that matter — counter, table, receipt, takeaway, delivery or window — and connect each one to the right Location and Guest Form.",
  ctaLabel: "Explore QR materials",
  ctaTo: MARKETING_QR_MATERIALS_PATH,
} as const

export type ProductCaptureMaterial = {
  id: string
  label: string
  caption: string
  /** marketingMediaMeta / remoteSimpleUrl id */
  mediaId: string
  imageAlt: string
}

/**
 * Figma Capture materials `6106:17567` … `6106:18055`.
 * Desktop: click or list-hover wheel; mobile uses tap.
 */
export const PRODUCT_CAPTURE_MATERIALS = [
  {
    id: "guest-cards",
    label: "QR Guest Cards",
    caption: "Compact cards for takeaway, delivery and counter handout.",
    mediaId: "product/capture-guest-cards",
    imageAlt: "Hand holding a green Tummly QR guest card with an offer.",
  },
  {
    id: "counter-display",
    label: "QR Counter Display",
    caption:
      "Freestanding QR display for tills, counters and collection points.",
    mediaId: "product/capture-counter-display",
    imageAlt: "Freestanding QR counter display on a restaurant counter.",
  },
  {
    id: "tray-liner",
    label: "Tray Liner",
    caption: "Printed QR tray liner for dine-in Feedback and Offers.",
    mediaId: "product/capture-tray-liner",
    imageAlt: "Printed QR tray liner on a tray with a meal.",
  },
  {
    id: "table-tent",
    label: "Table Tent",
    caption:
      "Table display that makes private Feedback easy to share while dining.",
    mediaId: "product/capture-table-tent",
    imageAlt: "QR table tent standing on a dining table.",
  },
  {
    id: "table-talker",
    label: "Table Talker",
    caption:
      "Three-sided table display that keeps the QR visible from more angles.",
    mediaId: "product/capture-table-talker",
    imageAlt: "Three-sided QR table talker on a restaurant table.",
  },
  {
    id: "window-sticker",
    label: "Window Sticker",
    caption: "Branded QR sticker for entrances, exits and front windows.",
    mediaId: "product/capture-window-sticker",
    imageAlt: "Branded QR window sticker on a restaurant window.",
  },
  {
    id: "receipt-label",
    label: "Receipt Label",
    caption:
      "Small QR label for receipts, designed to collect private Feedback.",
    mediaId: "product/capture-receipt-label",
    imageAlt: "Receipt with a small Tummly QR label.",
  },
  {
    id: "delivery-bag-seal",
    label: "Delivery Bag Seal",
    caption: "QR-enabled seal for takeaway and delivery bags.",
    mediaId: "product/capture-delivery-bag-seal",
    imageAlt: "Delivery bag sealed with a QR-enabled sticker.",
  },
  {
    id: "package-label",
    label: "Standard Package Label",
    caption: "Flexible QR label for boxes, bags and takeaway packaging.",
    mediaId: "product/capture-package-label",
    imageAlt: "Takeaway packaging with a standard QR package label.",
  },
  {
    id: "drink-cup-seal",
    label: "Drink / Cup Seal",
    caption: "Compact QR seal designed for drinks and cups.",
    mediaId: "product/capture-drink-cup-seal",
    imageAlt: "Drink cup with a compact QR seal on the lid.",
  },
  {
    id: "pizza-box-label",
    label: "Pizza Box Label",
    caption: "QR label for pizza boxes, inviting Feedback after the order.",
    mediaId: "product/capture-pizza-box-label",
    imageAlt: "Pizza box with a Tummly QR label.",
  },
  {
    id: "poster-club",
    label: "Poster / Customer Club",
    caption: "Larger QR display designed to grow your guest list.",
    mediaId: "product/capture-poster-club",
    imageAlt: "Customer club poster with a large Tummly QR code.",
  },
] as const satisfies readonly ProductCaptureMaterial[]

export type ProductFeaturePoint = {
  id: string
  title: string
  body: string
}

/** Figma Product Feedback section `5661:22688`. */
export const PRODUCT_FEEDBACK = {
  title: "Make private Feedback easy to give.",
  body: "Guests can type what happened or use the microphone to dictate it. They can then add their details, review the offers-and-updates opt-in shown in the Guest Form and submit their Feedback privately to the restaurant.",
  points: [
    {
      id: "type-or-dictate",
      title: "Type or dictate",
      body: "Guests can enter Feedback in the way that suits the moment.",
    },
    {
      id: "restaurant-branded",
      title: "Restaurant-branded",
      body: "The restaurant and Location stay clear throughout the guest experience.",
    },
    {
      id: "private-by-design",
      title: "Private by design",
      body: "Guest Feedback goes into the restaurant workflow rather than becoming a public review.",
    },
    {
      id: "built-for-mobile",
      title: "Built for mobile",
      body: "The experience is designed for the phone already in the guest’s hand.",
    },
  ] satisfies ProductFeaturePoint[],
  phoneImageAlt:
    "Guest Feedback form on a phone for Mehmet’s Grill, showing type-or-dictate input and contact details.",
} as const

/** Figma Product Feedback & Recovery `5661:22714`. */
export const PRODUCT_FEEDBACK_RECOVERY = {
  eyebrow: "Feedback & Recovery",
  title: "Hear what guests want you to know, privately.",
  body: "Bring private Feedback into one working inbox with the Location, QR source and available guest context attached. See what is new, what needs attention and what your team has already acted on.",
  points: [
    {
      id: "one-private-inbox",
      title: "One private inbox",
      body: "Keep guest Feedback separate from public-review activity.",
    },
    {
      id: "context-attached",
      title: "Context attached",
      body: "See the Location, QR placement/source and relevant guest history together.",
    },
    {
      id: "recovery-workflow",
      title: "Recovery workflow",
      body: "Move Feedback through New, Needs attention, In progress and Resolved states.",
    },
    {
      id: "ai-assisted-drafting",
      title: "AI-assisted drafting",
      body: "Where appropriate, Tummly can help prepare a recovery Draft for operator review.",
    },
  ] satisfies ProductFeaturePoint[],
  imageAlt:
    "Feedback inbox showing a private guest response draft with AI recovery assistance.",
} as const

/** Figma Product Guests `5661:23065`. */
export const PRODUCT_GUESTS = {
  eyebrow: "Guests",
  title: "Build a guest list with the right context attached.",
  body: "Bring guest details, interaction history, Location/source context and current contactability together, so your team understands the relationship before taking the next action.",
  points: [
    {
      id: "guest-details",
      title: "Guest details",
      body: "See the guest information collected through Guest Loop.",
    },
    {
      id: "contactability",
      title: "Contactability",
      body: "Understand the relevant contactability and permission context before taking action.",
    },
    {
      id: "location-source",
      title: "Location & source",
      body: "Keep each guest connected to the Location and QR source where the relationship began.",
    },
    {
      id: "interaction-history",
      title: "Interaction history",
      body: "See Feedback, Campaign, Offer and Redemption activity together.",
    },
  ] satisfies ProductFeaturePoint[],
  imageAlt:
    "Guest profile for Alex Thompson with contactability, tags, and interaction history.",
} as const

/** Figma Product Campaigns & Offers `5661:23511` — media left. */
export const PRODUCT_CAMPAIGNS = {
  eyebrow: "Campaigns & Offers",
  title: "Give eligible guests a useful reason to come back.",
  body: "Build a Campaign for an eligible guest audience, choose Email or SMS, add an Offer where useful and review everything before it is sent. Tummly can prepare the Campaign. You stay in control of the send.",
  points: [
    {
      id: "choose-a-goal",
      title: "Choose a goal",
      body: "Thank recent guests, fill a quieter time, re-engage or promote something new.",
    },
    {
      id: "choose-eligible-audience",
      title: "Choose an eligible audience",
      body: "Tummly applies the relevant contactability and Campaign eligibility rules.",
    },
    {
      id: "add-an-offer",
      title: "Add an Offer if useful",
      body: "Offers have clear validity, expiry and controlled Redemption.",
    },
    {
      id: "review-before-send",
      title: "Review before send",
      body: "Final audience, message, Offer, schedule and usage are confirmed before execution.",
    },
    {
      id: "track-redemption",
      title: "Track Redemption",
      body: "See Claims and Redemptions against the relevant Offer and Campaign execution.",
    },
  ] satisfies ProductFeaturePoint[],
  imageAlt:
    "Create a campaign screen with goal options such as thank recent guests and boost a quieter time.",
} as const

/** Figma Product Reports & Briefs `5661:23587`. */
export const PRODUCT_REPORTS = {
  eyebrow: "Reports & Briefs",
  title: "See what happened, what changed and what needs attention.",
  body: "Understand Capture, Feedback, guest growth, Campaign and Offer activity with the relevant period, Location and source context attached.",
  points: [
    {
      id: "capture-feedback",
      title: "Capture & Feedback",
      body: "See scans, Feedback and issue themes with period, Location and source context.",
    },
    {
      id: "guest-growth",
      title: "Guest growth",
      body: "Understand guest growth and contactability over the selected period.",
    },
    {
      id: "campaigns-offers",
      title: "Campaigns & Offers",
      body: "Track Campaign activity and Offer Redemption in the same reporting context.",
    },
    {
      id: "weekly-brief",
      title: "Weekly Brief",
      body: "See what changed, what needs attention and the evidence-backed next place to look.",
    },
  ] satisfies ProductFeaturePoint[],
  imageAlt:
    "Reports overview with guest growth chart, feedback themes, and weekly brief.",
} as const

/** Figma Product AI Assistant `5661:23627`. */
export const PRODUCT_AI_ASSISTANT = {
  eyebrow: "AI Assistant",
  /** Two lines — rendered with `whitespace-pre-line`. */
  title: "Ask what is happening.\nPrepare the next step.",
  body: "Ask Tummly about the Guest Loop activity you are allowed to see. Get a concise answer with the relevant scope and evidence, then prepare a governed Draft for your review where an action is useful.",
  points: [
    {
      id: "evidence-aware-answers",
      title: "Evidence-aware answers",
      body: "See the relevant Location, period and supporting context with each answer.",
    },
    {
      id: "prepare-governed-drafts",
      title: "Prepare governed Drafts",
      body: "Prepare Campaign or recovery Drafts for operator review.",
    },
    {
      id: "stay-in-control",
      title: "Stay in control",
      body: "Tummly AI does not autonomously send Campaigns or bypass eligibility and permission rules.",
    },
  ] satisfies ProductFeaturePoint[],
  ctaLabel: "Explore AI Assistant",
  ctaTo: MARKETING_AI_ASSISTANT_PATH,
  imageAlt:
    "AI Assistant workspace greeting Oliver and inviting a question about Guest Loop activity.",
} as const

/** Figma Product “Built to work with your restaurant” `5661:24151`. */
export const PRODUCT_SYSTEMS = {
  eyebrow: "Built to work with your restaurant",
  title: "Start without replacing the systems you already use.",
  body: "Guest Loop works alongside the way your restaurant already operates. You can start with Tummly QR touchpoints without replacing your POS, ordering channels or existing restaurant systems.",
  cards: [
    {
      id: "no-pos",
      title: "No POS integration required",
      body: "Guest Loop can deliver its core value without a POS connection.",
    },
    {
      id: "no-website",
      title: "No website required",
      body: "Guests reach the restaurant’s Guest Form directly from Tummly QR touchpoints or a Smart Guest Link.",
    },
    {
      id: "no-printer",
      title: "No in-house printer required",
      body: "Restaurant-ready QR materials support physical deployment, with digital and self-print options available where suitable.",
    },
  ] satisfies ProductFeaturePoint[],
} as const

/** Figma Product multi-location banner `5661:24169`. */
export const PRODUCT_GROUPS = {
  eyebrow: "For single and multi-location teams",
  title: "Keep the Location context as you grow.",
  body: "Run Guest Loop across multiple Locations while keeping Feedback, guests, QR sources and reporting connected to the right place. Eligible plans can also provide consolidated reporting and group-level operating views.",
  ctaLabel: "See Tummly for groups",
  ctaTo: MARKETING_FOR_GROUPS_PATH,
} as const

/** Figma Product “Built around restaurant control” `5661:24177`. */
export const PRODUCT_CONTROL = {
  eyebrow: "Built around restaurant control",
  title: "Guest choice. Restaurant control. Clear history.",
  body: "Private Feedback, communication controls, human review and traceable activity keep important decisions under restaurant control.",
  columns: [
    [
      {
        id: "private-feedback",
        title: "Private Feedback",
        body: "Feedback stays within the restaurant workflow and remains separate from public reviews.",
      },
      {
        id: "controlled-communication",
        title: "Controlled communication",
        body: "Guest contact and Campaign eligibility are evaluated before communication is sent.",
      },
    ],
    [
      {
        id: "human-review",
        title: "Human review",
        body: "Tummly AI can prepare work; consequential actions remain under operator control.",
      },
      {
        id: "traceable-activity",
        title: "Traceable activity",
        body: "Important product actions retain the relevant Location, source, time and history.",
      },
    ],
  ] satisfies ProductFeaturePoint[][],
  ctaLabel: "Learn about trust & privacy",
  ctaTo: MARKETING_TRUST_PRIVACY_PATH,
} as const

export type ProductFaqItem = {
  id: string
  question: string
  answer: string
}

/** Figma Product FAQs `5661:24202`. */
export const PRODUCT_FAQS = {
  title: "Frequently asked questions",
  moreTitle: "Have another question?",
  moreBody:
    "Find more answers about Tummly, pricing, QR materials and Guest Loop.",
  moreCtaLabel: "Explore all FAQs",
  moreCtaTo: MARKETING_FAQS_PATH,
  items: [
    {
      id: "replace-pos",
      question: "Do I need to replace my POS or ordering system?",
      answer:
        "No. Tummly is designed to work alongside the way your restaurant already operates. Its core Guest Loop does not require you to replace your POS or ordering channels.",
    },
    {
      id: "guest-app",
      question: "Do guests need to download an app?",
      answer:
        "No. Guests open the restaurant’s Guest Form from a Tummly QR touchpoint or Smart Guest Link in their browser.",
    },
    {
      id: "feedback-public",
      question: "Is Feedback public?",
      answer:
        "No. Feedback submitted through the Guest Form stays within the restaurant workflow and remains separate from public-review activity.",
    },
    {
      id: "contact-details",
      question: "Do guests need to provide contact details?",
      answer:
        "No. Guests can submit private Feedback without providing contact details. They can optionally add a first name and one contact method if they want the restaurant to follow up.",
    },
    {
      id: "multi-location",
      question: "Can I use Tummly across multiple Locations?",
      answer:
        "Yes. Eligible plans support multiple Locations while retaining the relevant Location context across Capture, Feedback, guests and reporting.",
    },
    {
      id: "ai-auto-campaigns",
      question: "Can Tummly AI send Campaigns automatically?",
      answer:
        "No. Tummly AI can analyse permitted data and prepare governed Drafts, but consequential communications remain subject to operator review and controlled sending.",
    },
    {
      id: "website-printer",
      question: "Do I need my own website or printer?",
      answer:
        "No. Guest Loop can be entered through Tummly QR touchpoints or a Smart Guest Link, and restaurant-ready QR materials are available alongside digital and self-print options where suitable.",
    },
    {
      id: "without-campaigns",
      question: "Can I use Tummly without creating Offers or Campaigns?",
      answer:
        "Yes. Tummly can deliver value from private Feedback, guest context and reporting before you choose to run a Campaign or Offer.",
    },
  ] satisfies ProductFaqItem[],
} as const

/** Shared bottom Sign-up CTA — same Pilot copy as Pricing / AI Assistant. */
export const PRODUCT_SIGN_UP_CTA: MarketingSignUpCtaContent = {
  title: "Ready to see Guest Loop in a real restaurant?",
  body: "Start with one Location and see how Guest Loop works with real guests.",
  primaryLabel: "Start 30-day Pilot",
  badge: "No payment card required",
  planIntent: { plan: "Pilot" },
}
