import {
  MARKETING_DEMO_PATH,
  MARKETING_FAQS_PATH,
  MARKETING_PRICING_PATH,
} from "@/constants/marketingNav"
import { AI_ASSISTANT_PLANS } from "@/content/marketing/aiAssistantPage"

/** Shared section inset — matches How it works / Product (20px mobile, 60px desktop). */
export const PILOT_SECTION_INSET = "px-5 lg:px-[60px]"

/** Shared vertical pad — 50px mobile, 70px desktop. */
export const PILOT_SECTION_Y = "py-12.5 lg:py-[70px]"

export const PILOT_SECTION_BODY =
  "font-sans text-base font-normal leading-[22px] text-[#141414] lg:text-[18px] lg:leading-6"

export const PILOT_SECTION_HEADING =
  "font-jakarta text-[28px] font-medium leading-9 text-[#141414] lg:text-[46px] lg:leading-[52px]"

/** Figma 30-day Pilot hero `5661:19874` — split copy + lifestyle frame. */
export const PILOT_HERO = {
  eyebrow: "FREE 30-DAY PILOT",
  title: "Turn everyday visits into reasons to return.",
  body: "Try Tummly free for 30 days. Collect private guest Feedback through a simple QR code, build a permission-based guest list and use relevant Offers and Campaigns to encourage repeat orders and visits.",
  primaryLabel: "Start 30-day Pilot",
  emailPlaceholder: "Work email",
  planIntent: { plan: "Pilot" as const },
  trustItems: [
    "Free for 30 days · One Location",
    "No payment card required",
  ] as const,
  /** marketingMediaMeta / remoteSimpleUrl id */
  mediaId: "pilot/hero",
  imageAlt:
    "Cafe host at a laptop beside a green Guest Loop QR table tent on the counter.",
} as const

export type PilotWhyCard = {
  id: "hear" | "relationships" | "return"
  title: string
  body: string
  /** marketingMediaMeta / remoteSimpleImg id */
  mediaId: string
  imageAlt: string
  /** CSS display size of the illustration plate. */
  imageWidth: number
  imageHeight: number
}

/** Figma Pilot “Why Tummly” `6543:18855`. */
export const PILOT_WHY_TUMMLY = {
  eyebrow: "WHY TUMMLY",
  title: "Feedback is the beginning. The next visit is the opportunity.",
  body: "When a guest leaves your restaurant, the relationship doesn't have to end. Tummly helps you understand their experience, stay connected when they choose to hear from you and create more opportunities for repeat orders.",
  cards: [
    {
      id: "hear",
      title: "Hear what matters.",
      body: "Understand what guests enjoyed, identify concerns and find opportunities to improve their experience.",
      mediaId: "pilot/why-hear",
      imageAlt: "Illustration of a megaphone with speech bubbles.",
      imageWidth: 300,
      imageHeight: 300,
    },
    {
      id: "relationships",
      title: "Build stronger guest relationships.",
      body: "Create a guest list through everyday interactions and stay connected with guests who give permission to receive future updates.",
      mediaId: "pilot/why-relationships",
      imageAlt:
        "Illustration of a restaurant and a guest profile tablet linked by hearts.",
      imageWidth: 326,
      imageHeight: 326,
    },
    {
      id: "return",
      title: "Give guests a reason to return.",
      body: "Use relevant Offers and Campaigns to reconnect with eligible guests and encourage another order or visit.",
      mediaId: "pilot/why-return",
      imageAlt: "Illustration of an open gift box with a percent sprout inside.",
      imageWidth: 266,
      imageHeight: 266,
    },
  ] satisfies PilotWhyCard[],
} as const

export type PilotGettingStartedStep = {
  id: "setup" | "prepare" | "place" | "hear"
  number: string
  title: string
  body: string
}

/**
 * Figma Pilot “Getting Started” `6535:18445`.
 * Right column is one composite (stairs + step copy). Steps stay for a11y.
 */
export const PILOT_GETTING_STARTED = {
  eyebrow: "GETTING STARTED",
  title: "From setup to your first guest scan.",
  body: "Getting started is straightforward. Set up your restaurant, prepare your Guest Form and place your included QR Counter Display where guests can easily see it.",
  primaryLabel: "Start 30-day Pilot",
  secondaryLabel: "Compare paid plans",
  secondaryTo: `${MARKETING_PRICING_PATH}#compare-plans`,
  planIntent: { plan: "Pilot" as const },
  mediaId: "pilot/getting-started-steps",
  imageAlt:
    "Four steps from setup to first guest scan: set up your restaurant, prepare your Guest Loop, place your QR Counter Display, then start hearing from guests.",
  steps: [
    {
      id: "setup",
      number: "01",
      title: "Set up your restaurant",
      body: "Create your account, add your Location and complete verification.",
    },
    {
      id: "prepare",
      number: "02",
      title: "Prepare your Guest Loop",
      body: "Set up your Guest Form and QR placement. You can also configure an eligible Thank-You Offer.",
    },
    {
      id: "place",
      number: "03",
      title: "Place your QR Counter Display",
      body: "Position your included display at the till, service counter or collection point, ready for guests to scan.",
    },
    {
      id: "hear",
      number: "04",
      title: "Start hearing from guests",
      body: "Guests scan your QR code and share private Feedback directly with your restaurant.",
    },
  ] satisfies PilotGettingStartedStep[],
} as const

export type PilotFirstThirtyDaysCard = {
  id: "qr" | "feedback" | "relationships" | "performance"
  title: string
  body: string
}

/**
 * Figma Pilot “Your first 30 days” `6476:16898`.
 * Four insight cards + dual CTAs before the plans strip.
 */
export const PILOT_FIRST_THIRTY_DAYS = {
  eyebrow: "YOUR FIRST 30 DAYS",
  title: "See what guests are saying and how they respond.",
  body: "As guests start using Tummly, see which QR placements are getting attention, what guests think about their experience, how your guest list is growing and how people respond to your Campaigns and Offers.",
  primaryLabel: "Start 30-day Pilot",
  secondaryLabel: "Compare paid plans",
  secondaryTo: `${MARKETING_PRICING_PATH}#compare-plans`,
  planIntent: { plan: "Pilot" as const },
  cards: [
    {
      id: "qr",
      title: "QR activity",
      body: "See which QR placements guests are scanning and how they're engaging with your Guest Form.",
    },
    {
      id: "feedback",
      title: "Guest Feedback",
      body: "Understand what guests enjoyed and where their experience could improve.",
    },
    {
      id: "relationships",
      title: "Guest relationships",
      body: "See how your guest list is growing and how many guests have chosen to receive future marketing updates.",
    },
    {
      id: "performance",
      title: "Campaign & Offer performance",
      body: "Track Campaign engagement and see how guests respond to your Offers through recorded Claims and Redemptions.",
    },
  ] satisfies PilotFirstThirtyDaysCard[],
} as const

/**
 * Figma Pilot “Your next step” plans strip `5679:34414`.
 * Reuses shared plan cards; no AI allowance row.
 */
export const PILOT_PLANS = {
  eyebrow: "YOUR NEXT STEP",
  title: "Start free. Grow with Tummly.",
  body: "Begin with a free 30-day Pilot at one restaurant Location. When you're ready to continue, choose a paid plan that fits your business. Already know what you need? You can start with a paid plan instead.",
  footnote:
    "All paid plans are available with monthly or annual billing. Your Pilot will never automatically turn into a paid subscription.",
  compareLabel: "Compare all plans",
  compareTo: `${MARKETING_PRICING_PATH}#compare-plans`,
  cards: AI_ASSISTANT_PLANS.cards,
} as const

/**
 * Figma Pilot mid-page CTA panel `5681:46128`
 * (“YOUR 30-DAY PILOT” / Everything you need to get started).
 */
export const PILOT_WHY_ZERO = {
  eyebrow: "YOUR 30-DAY PILOT",
  title: "Everything you need to get started.",
  body: "Experience Tummly at one verified restaurant Location, with an included QR Counter Display and the tools to begin building stronger guest relationships.",
  primaryLabel: "Start 30-day pilot",
  secondaryLabel: "See full plan details",
  secondaryTo: `${MARKETING_PRICING_PATH}#compare-plans`,
  planIntent: { plan: "Pilot" as const },
  trustItems: [
    "Free for 30 days · One Location",
    "No payment card required",
  ] as const,
} as const

export type PilotFaqItem = {
  id: string
  question: string
  answer: string
}

/** Figma Pilot FAQs `5661:20468` — warm surface `#f2f0ec` (not white). */
export const PILOT_FAQS = {
  title: "Frequently asked questions",
  body: "Everything you need to know before starting your 30-day Pilot.",
  moreTitle: "Have another question?",
  moreBody:
    "Find more answers about Tummly, pricing, QR materials and Guest Loop.",
  moreCtaLabel: "Explore all FAQs",
  moreCtaTo: MARKETING_FAQS_PATH,
  items: [
    {
      id: "really-free",
      question: "Is Tummly really free for 30 days?",
      answer:
        "Yes. The 30-day Pilot is free for one eligible, verified restaurant Location. No payment card is required, and there is no automatic paid renewal.",
    },
    {
      id: "when-begins",
      question: "When do my 30 days begin?",
      answer:
        "Your 30-day Pilot begins when you complete your Tummly account registration. You can start setting up your restaurant immediately.",
    },
    {
      id: "what-during",
      question: "What can I do during my Pilot?",
      answer:
        "Explore Tummly's Guest Loop, collect private Feedback, build guest records, create Offers, run permission-based Campaigns and review available results, within your Pilot allowances.",
    },
    {
      id: "website-printer",
      question: "Do I need a website or printer?",
      answer:
        "No. Tummly provides your Guest Form and QR code, so you can start digitally. Eligible restaurants also receive an included QR Counter Display.",
    },
    {
      id: "qr-counter-display",
      question: "Is a QR Counter Display included?",
      answer:
        "Yes. One QR Counter Display is included for each eligible, newly activated and verified restaurant Location. It comes with your restaurant's QR code, ready for guests to scan.",
    },
    {
      id: "existing-guest-list",
      question: "Do I need an existing guest list?",
      answer:
        "No. Tummly helps you build guest records as customers use your Guest Loop. Guests can separately choose whether to receive Email or SMS marketing.",
    },
    {
      id: "repeat-visits",
      question: "Can Tummly help encourage repeat visits and orders?",
      answer:
        "Yes. Tummly helps you create Offers and Campaigns that give eligible guests reasons to return, while tracking supported engagement and recorded Offer Redemptions.",
    },
    {
      id: "auto-marketing",
      question: "Will guests automatically receive marketing messages?",
      answer:
        "No. Sharing contact details does not automatically give marketing permission. Guests must separately choose to receive Email or SMS marketing.",
    },
    {
      id: "pilot-ends",
      question: "What happens when my Pilot ends?",
      answer:
        "You won't be charged automatically. You can choose a paid plan to continue using Tummly. If you don't subscribe, new activity is restricted, while your existing account data is handled according to Tummly's access and retention rules.",
    },
    {
      id: "multi-restaurant",
      question: "Can I use Tummly at more than one restaurant?",
      answer:
        "Yes. The 30-day Pilot covers one restaurant Location. If you operate multiple restaurants, Tummly offers paid plans that let you manage your Locations together, with the flexibility to add more as your business grows.",
    },
  ] satisfies PilotFaqItem[],
} as const

/**
 * Figma Pilot bottom split CTA `6464:35738`.
 * White surface + lifestyle frame (not dark video / About marble overlay).
 */
export const PILOT_SIGN_UP_CTA = {
  title: "Ready to give guests a reason to return?",
  body: "Start your free 30-day Pilot and put Tummly to work in your restaurant. Hear directly from guests, build stronger relationships and create more opportunities for repeat orders.",
  primaryLabel: "Start 30-day Pilot",
  emailPlaceholder: "Work email",
  planIntent: { plan: "Pilot" as const },
  mediaId: "pilot/sign-up-cta",
  imageAlt:
    "Friends and a child sharing plates and drinks at a sunlit restaurant table.",
} as const

export type PilotInActionTab = {
  id: "hear" | "connected" | "opportunity" | "working"
  tabLabel: string
  title: string
  body: string
  /** marketingMediaMeta / remoteSimpleImg id */
  mediaId: string
  imageAlt: string
}

/**
 * Figma Pilot “See Tummly in Action” tabbed showcase —
 * `6495:18145`, `6495:18183`, `6495:18223`, `6495:18254`.
 */
export const PILOT_IN_ACTION = {
  eyebrow: "SEE TUMMLY IN ACTION",
  title: "One guest interaction. More opportunities to reconnect",
  body: "See how Tummly connects private Feedback, guest relationships and relevant follow-up, helping you understand your guests and give them more reasons to return.",
  primaryLabel: "Start 30-day Pilot",
  secondaryLabel: "Try a demo",
  secondaryTo: MARKETING_DEMO_PATH,
  planIntent: { plan: "Pilot" as const },
  /** Active tab fill — Figma Tab selected `#dfebe3`. */
  activeTabColor: "#dfebe3",
  tabs: [
    {
      id: "hear",
      tabLabel: "Hear directly from guests",
      title: "Know what your guests really think.",
      body: "Give guests a simple way to share private Feedback after their visit. Understand what they enjoyed, spot concerns early and make improvements that give them more reasons to return.",
      mediaId: "pilot/in-action-hear",
      imageAlt:
        "Feedback inbox with a guest detail card for Sophie H. and Respond actions.",
    },
    {
      id: "connected",
      tabLabel: "Stay connected beyond the visit",
      title: "Turn one visit into an ongoing relationship.",
      body: "Build a guest list through everyday interactions. Stay connected with guests who choose to receive updates from your restaurant, so the conversation doesn't have to end when they leave.",
      mediaId: "pilot/in-action-connected",
      imageAlt:
        "Guests list with a Permissions panel for Charlotte W. showing email opt-in.",
    },
    {
      id: "opportunity",
      tabLabel: "Create the next opportunity",
      title: "Give guests a reason to order again.",
      body: "Bring eligible guests back with relevant Offers and Campaigns. From thanking recent visitors to promoting something new, create more opportunities for repeat orders and visits.",
      mediaId: "pilot/in-action-opportunity",
      imageAlt:
        "Email campaign setup beside a mobile preview of a free dessert offer.",
    },
    {
      id: "working",
      tabLabel: "See what is working",
      title: "Understand your guests. See what’s working.",
      body: "Track guest Feedback, Campaign engagement and Offer Redemptions in one place. Use AI to summarise Feedback, identify recurring issues and understand how guests are responding to your restaurant’s Offers and Campaigns.",
      mediaId: "pilot/in-action-working",
      imageAlt:
        "Weekly report table with an AI Assistant chat summarising Feedback and Offers.",
    },
  ] satisfies PilotInActionTab[],
} as const
