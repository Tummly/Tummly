import type { MarketingSignUpCtaContent } from "@/components/marketing/MarketingSignUpCta"
import {
  MARKETING_FAQS_PATH,
  MARKETING_PRICING_PATH,
} from "@/constants/marketingNav"
import { AI_ASSISTANT_PLANS } from "@/content/marketing/aiAssistantPage"

/** Shared section inset — matches How it works / Product (20px mobile, 60px desktop). */
export const PILOT_SECTION_INSET = "px-5 lg:px-[60px]"

/** Shared vertical pad — 50px mobile, 70px desktop. Figma hero `5661:19874`. */
export const PILOT_SECTION_Y = "py-12.5 lg:py-[70px]"

export const PILOT_SECTION_BODY =
  "font-sans text-base font-normal leading-[22px] text-[#141414] lg:text-[18px] lg:leading-6"

export const PILOT_SECTION_HEADING =
  "font-jakarta text-[28px] font-medium leading-9 text-[#141414] lg:text-[46px] lg:leading-[52px]"

/** Figma 30-day Pilot hero `5661:19874`. */
export const PILOT_HERO = {
  eyebrow: "30-DAY PILOT",
  title: "Put Guest Loop to work in one real Location for 30 days.",
  body: "Set up your Location, get Guest Loop into real guest moments and decide from real activity whether Tummly is worth continuing with.",
  trustBadge: "£0 / No payment card required",
  primaryLabel: "Get started",
  emailPlaceholder: "Work email",
  planIntent: { plan: "Pilot" as const },
  footnote:
    "Your 30 days are for live Guest Loop use — not waiting for setup.",
} as const

export type PilotPurposeOption = {
  id: "real-deployment" | "private-listening" | "guest-context" | "action-learning"
  title: string
  body: string
}

/**
 * Figma Pilot “What the Pilot is for” `5661:19885`.
 * Option clicks swap the visual; all options share one image for now.
 */
export const PILOT_PURPOSE = {
  eyebrow: "WHAT THE PILOT IS FOR",
  title: "See Guest Loop working in a real restaurant.",
  body: "Use one verified Location, put Guest Loop into real guest moments and use your own activity to decide whether the workflow fits your restaurant.",
  footnote: "This is a real-location Pilot, not a pre-filled software demo.",
  imageAlt:
    "Guests at a restaurant table while one person scans a Guest Loop QR table tent on their phone.",
  options: [
    {
      id: "real-deployment",
      title: "Real deployment",
      body: "Put Guest Loop into suitable counter, table, receipt, takeaway or delivery moments.",
    },
    {
      id: "private-listening",
      title: "Private listening",
      body: "Collect private Feedback through your restaurant’s Guest Form and keep it separate from public reviews.",
    },
    {
      id: "guest-context",
      title: "Useful guest context",
      body: "See how guest details, interaction history and current contact/opt-in context begin to build around real activity.",
    },
    {
      id: "action-learning",
      title: "Action + learning",
      body: "Review Feedback, use available Campaign and Offer tools where appropriate, and see what the resulting activity tells you.",
    },
  ] satisfies PilotPurposeOption[],
} as const

export type PilotTestingCard = {
  id: string
  number: string
  title: string
  body: string
}

/** Figma Pilot “What you’re testing” `5661:19910`. */
export const PILOT_TESTING = {
  eyebrow: "WHAT YOU’RE TESTING",
  title:
    "By Day 30, you should know whether Guest Loop earns a place in your operation.",
  cards: [
    {
      id: "deployment",
      number: "01",
      title: "Deployment",
      body: "Can you place Guest Loop naturally into real guest moments?",
    },
    {
      id: "participation",
      number: "02",
      title: "Participation",
      body: "Do guests use the QR touchpoints when they encounter them?",
    },
    {
      id: "listening",
      number: "03",
      title: "Listening",
      body: "Does private Feedback give your team useful information?",
    },
    {
      id: "relationship",
      number: "04",
      title: "Relationship",
      body: "Does useful guest/contact context begin to build?",
    },
    {
      id: "action",
      number: "05",
      title: "Action",
      body: "Can you follow up, use an Offer or run an eligible Campaign without creating excessive work?",
    },
    {
      id: "learning",
      number: "06",
      title: "Learning",
      body: "Can you understand what happened and decide what to do next?",
    },
  ] satisfies PilotTestingCard[],
} as const

export type PilotSetupCard = {
  id: "location" | "guest-loop" | "go-live"
  number: string
  title: string
  body: string
  imageAlt: string | null
}

/**
 * Figma Pilot “Set up first” `5661:19947`.
 * Cards 01–02 include product visuals; card 03 is copy-only in Figma.
 */
export const PILOT_SETUP = {
  body: "Create your workspace, verify your Location and prepare your Guest Form and QR placements before your live Pilot begins.",
  title: "Set up first. Use your 30 days for live Guest Loop activity.",
  cards: [
    {
      id: "location",
      number: "01",
      title: "Set up your Location",
      body: "Create your Tummly account, add your first Location and complete verification.",
      imageAlt:
        "Location setup form with name, address, country, timezone and Set up your account.",
    },
    {
      id: "guest-loop",
      number: "02",
      title: "Prepare Guest Loop",
      body: "Review your Guest Form, create the QR placements you want to use and prepare the Location for production.",
      imageAlt:
        "Guest Feedback form on a phone for Mavero Kitchen, Camden High Street.",
    },
    {
      id: "go-live",
      number: "03",
      title: "Go live",
      body: "Place Guest Loop into real restaurant moments. Your live 30-day Pilot then starts under the Pilot activation rules.",
      imageAlt: null,
    },
  ] satisfies PilotSetupCard[],
} as const

/**
 * Figma Pilot “From setup to live” `5681:46039`.
 * Centred copy inside the shared gradient panel.
 */
export const PILOT_ACTIVATION_JOURNEY = {
  eyebrow: "FROM SETUP TO LIVE",
  title: "We help you get from setup to useful evidence.",
  steps: [
    "Set up — Account, Location and verification ready.",
    "Prepare — Guest Form and QR placements ready.",
    "Receive — Starter materials delivered where the qualifying entitlement applies.",
    "Place — QR touchpoints deployed in the restaurant.",
    "Go live — First valid production activity begins the real Pilot.",
    "Learn — Use Feedback, guest context and available actions to judge the fit.",
  ],
  footnote:
    "Your live 30-day Pilot starts at the earlier of your first valid production guest scan or 48 hours after confirmed delivery of your included Starter Kit. Preview/test activity does not start the Pilot.",
} as const

/**
 * Figma Pilot “Physical deployment” `5681:46057`.
 * Centred copy inside the shared gradient panel.
 */
export const PILOT_PHYSICAL_DEPLOYMENT = {
  eyebrow: "PHYSICAL DEPLOYMENT",
  title: "Start online. Put Guest Loop into the restaurant when you’re ready.",
  intro:
    "Setup does not depend on the parcel arriving. Prepare your Location, Guest Form and QR placements online, then deploy the physical materials when they reach you.",
  details: [
    "Online setup — Location verified · Guest Form published · QR placements ready.",
    "Physical deployment — Restaurant-ready QR guest cards / table tent or counter display / window sticker, based on the current Starter Kit configuration.",
  ],
  footnote:
    "Restaurant-ready Starter Kit included for the qualifying Pilot Location. We provide the initial standard materials needed to help put Guest Loop into real restaurant moments. Exact contents follow the current Starter Kit configuration.",
} as const

/**
 * Figma Pilot “What happens on Day 30” `5661:20456`.
 * Decision-point copy + compare-plans CTA.
 */
export const PILOT_DAY_30 = {
  eyebrow: "WHAT HAPPENS ON DAY 30",
  title: "Day 30 is a decision point — not an automatic charge.",
  body: "Your Pilot does not automatically become a paid subscription. When the live Pilot ends, choose a paid plan if you want to keep Guest Loop running. Your existing Location, QR placement history and supported setup remain part of the conversion path rather than requiring you to start again.",
  ctaLabel: "Compare paid plans",
  ctaTo: `${MARKETING_PRICING_PATH}#compare-plans`,
} as const

/**
 * Figma Pilot Paid Continuation plans strip `5679:34414`.
 * Reuses shared plan cards; no AI allowance row.
 */
export const PILOT_PLANS = {
  eyebrow: "PAID CONTINUATION",
  title: "If you want to continue, choose the plan that fits your operation.",
  body: "Nothing converts automatically. At the end of the Pilot, you decide whether to continue.",
  compareLabel: AI_ASSISTANT_PLANS.compareLabel,
  compareTo: AI_ASSISTANT_PLANS.compareTo,
  cards: AI_ASSISTANT_PLANS.cards,
} as const

/**
 * Figma Pilot “Why £0?” `5681:46128`.
 * Centred copy inside the shared gradient panel.
 */
export const PILOT_WHY_ZERO = {
  eyebrow: "WHY £0?",
  title: "Why a £0 Pilot?",
  intro:
    "Because the useful question is whether Guest Loop works in your restaurant. Tummly is designed to let you test that with one verified Location before deciding whether to pay for ongoing use.",
  points: [
    "No payment card required to start.",
    "No automatic paid renewal.",
    "No requirement to choose a paid plan in advance.",
    "One verified production Location.",
    "Qualifying Starter Kit and setup support under the current Pilot rules.",
  ],
} as const

export type PilotFaqItem = {
  id: string
  question: string
  answer: string
}

/** Figma Pilot FAQs `5661:20468`. */
export const PILOT_FAQS = {
  title: "Frequently asked questions",
  moreTitle: "Have another question?",
  moreBody:
    "Find more answers about Tummly, pricing, QR materials and Guest Loop.",
  moreCtaLabel: "Explore all FAQs",
  moreCtaTo: MARKETING_FAQS_PATH,
  items: [
    {
      id: "really-zero",
      question: "Is the Tummly 30-day Pilot really £0?",
      answer:
        "Yes. The Pilot is £0 for 30 live days at one qualifying verified Location. No payment card is required to start and there is no automatic paid renewal.",
    },
    {
      id: "payment-card",
      question: "Do I need a payment card to start?",
      answer:
        "No. You do not need to provide a payment card to start the 30-day Pilot.",
    },
    {
      id: "when-begins",
      question: "When does my 30-day Pilot begin?",
      answer:
        "Your account setup can begin immediately. Your live 30-day Pilot starts at the earlier of your first valid production guest scan or 48 hours after confirmed delivery of your included Starter Kit. Preview/test activity does not start the Pilot.",
    },
    {
      id: "what-during",
      question: "What can I do during the Pilot?",
      answer:
        "You can set up one verified Location, prepare your Guest Form and QR placements, collect private Feedback, begin building guest context, use the Campaign/Offer capabilities available under the current Pilot entitlements and use the included AI, Email and SMS balances.",
    },
    {
      id: "qr-materials",
      question: "Does the Pilot include QR materials?",
      answer:
        "Yes, one qualifying standard Starter Kit is included for the verified Pilot Location under the current entitlement rules. Exact contents follow the current Starter Kit configuration.",
    },
    {
      id: "setup-before-materials",
      question: "Can I start setting up before the materials arrive?",
      answer:
        "Yes. Software access and setup begin online. You can prepare your Location, Guest Form and QR placements before physical materials arrive.",
    },
    {
      id: "multi-location",
      question: "Can I run the Pilot across more than one Location?",
      answer:
        "No. The standard Pilot is for one verified Location. If you operate multiple Locations, you can evaluate the Pilot at one Location and review Growth or Group options for broader rollout.",
    },
    {
      id: "campaigns-offers",
      question: "Can I send Campaigns or use Offers during the Pilot?",
      answer:
        "Yes, subject to the Pilot’s current feature entitlements, remaining channel balances and guest eligibility/contactability rules. Tummly does not bypass those rules because the account is on Pilot.",
    },
    {
      id: "unlimited-usage",
      question: "Are Email, SMS and AI usage unlimited?",
      answer:
        "No. The Pilot includes defined one-time balances. The website and application should show the current authoritative balances rather than implying unlimited usage.",
    },
    {
      id: "top-ups",
      question: "Can I buy top-ups during the Pilot?",
      answer:
        "No. Top-ups are not available on the Pilot. If a Pilot balance is exhausted, the relevant channel is limited according to the current product rules; other available Pilot capabilities can continue within their own limits.",
    },
    {
      id: "few-scans",
      question: "What if I do not get many guest scans?",
      answer:
        "The Pilot is intended to help you judge how Guest Loop fits your restaurant using real activity. Results vary by Location, footfall and deployment. Tummly does not guarantee a particular number of scans, guests, repeat visits or revenue outcome.",
    },
    {
      id: "day-30",
      question: "What happens on Day 30?",
      answer:
        "The Pilot ends and moves into the current post-Pilot account state. Tummly does not charge you automatically. If you want to continue, choose a paid plan. Your existing Location, QR history and supported setup remain part of the conversion path.",
    },
    {
      id: "auto-charge",
      question: "Will I be charged automatically?",
      answer:
        "No. There is no automatic Pilot-to-paid conversion and no automatic paid renewal.",
    },
    {
      id: "paid-setup",
      question: "What happens to my setup if I choose a paid plan?",
      answer:
        "Your existing Location, Guest Form, QR placement/token and supported history are preserved through Pilot-to-paid conversion. You do not need to rebuild the Location from scratch.",
    },
    {
      id: "second-starter-kit",
      question:
        "Will I receive another Starter Kit if I become a paid customer?",
      answer:
        "No second complimentary Starter Kit is issued for the same Pilot Location simply because the account converts to paid. The Pilot kit already uses that Location’s one-time qualifying entitlement. Additional or replacement materials can be ordered separately through Tummly Shop.",
    },
  ] satisfies PilotFaqItem[],
} as const

/** Figma Pilot Sign up CTA — media (video) variant. */
export const PILOT_SIGN_UP_CTA: MarketingSignUpCtaContent = {
  title: "Ready to see Guest Loop in a real restaurant?",
  body: "Start with one Location and see how Guest Loop works with real guests.",
  primaryLabel: "Get started",
  badge: "No payment card required",
  planIntent: { plan: "Pilot" },
}

export type PilotEvidenceRow = {
  id: "feedback" | "guest-context" | "campaigns" | "results"
  title: string
  body: string
  /** Desktop: media on the left. Mobile always copy first. */
  mediaFirst: boolean
  media:
    | { kind: "image"; imageAlt: string }
    | { kind: "placeholder"; label: string }
}

/**
 * Figma Pilot “Real activity” evidence strip `5661:20056`.
 * Campaigns & Results stay gray placeholders for now.
 */
export const PILOT_EVIDENCE = {
  eyebrow: "REAL ACTIVITY. USEFUL EVIDENCE.",
  title: "Use the Pilot to learn from real activity, not sample data.",
  body: "As guests interact with Guest Loop, Tummly starts building the evidence you need to judge whether it fits your restaurant.",
  rows: [
    {
      id: "feedback",
      title: "Hear what real guests choose to tell you.",
      body: "See private Feedback submitted through your restaurant’s Guest Form and understand what needs attention.",
      mediaFirst: true,
      media: {
        kind: "image",
        imageAlt:
          "Feedback inbox showing private guest comments, sentiment tags and locations.",
      },
    },
    {
      id: "guest-context",
      title: "See useful guest context begin to build.",
      body: "As guests choose to share details, see useful contact and interaction context building around real Guest Loop activity.",
      mediaFirst: false,
      media: {
        kind: "image",
        imageAlt:
          "Guest context card for Daniel with contact details and recent activity.",
      },
    },
    {
      id: "campaigns",
      title: "Campaigns & Offers",
      body: "Try appropriate re-engagement where your Pilot entitlements and guest eligibility allow.",
      mediaFirst: false,
      media: {
        kind: "placeholder",
        label: "Governed Draft / Offer state",
      },
    },
    {
      id: "results",
      title: "Results",
      body: "See the activity Tummly records so you can decide whether to continue.",
      mediaFirst: false,
      media: {
        kind: "placeholder",
        label: "Compact report / Pilot summary",
      },
    },
  ] satisfies PilotEvidenceRow[],
} as const
