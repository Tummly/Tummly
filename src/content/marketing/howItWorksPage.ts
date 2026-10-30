import type { MarketingSignUpCtaContent } from "@/components/marketing/MarketingSignUpCta"
import {
  MARKETING_FAQS_PATH,
  MARKETING_PILOT_PATH,
} from "@/constants/marketingNav"
import { AI_ASSISTANT_PLANS } from "@/content/marketing/aiAssistantPage"
import { QR_MATERIALS_PRICING } from "@/content/marketing/qrMaterialsPage"

/** Shared section inset — matches Product / AI Assistant (20px mobile, 60px desktop). */
export const HOW_IT_WORKS_SECTION_INSET = "px-5 lg:px-[60px]"

/** Shared vertical pad — 50px mobile, 70px desktop. Figma hero `5661:19524`. */
export const HOW_IT_WORKS_SECTION_Y = "py-12.5 lg:py-[70px]"

export const HOW_IT_WORKS_SECTION_HEADING =
  "font-jakarta text-[34px] font-medium leading-10 text-black lg:text-[46px] lg:leading-[52px]"

export const HOW_IT_WORKS_SECTION_BODY =
  "font-sans text-base font-normal leading-[22px] text-[#141414] lg:text-[18px] lg:leading-6"

/** Section eyebrow — *functional* 14/16, tracking 1.12px, #4a4a4a. */
export const HOW_IT_WORKS_SECTION_EYEBROW =
  "m-0 font-sans text-sm font-semibold uppercase leading-4 tracking-[1.12px] text-[#4a4a4a]"

/** Figma How it works hero `5661:19524`. */
export const HOW_IT_WORKS_HERO = {
  eyebrow: "How Tummly works",
  title: "From the first scan to a reason to return.",
  body: "Set up your first restaurant, share its QR or guest link and read the feedback that comes in. From there, reply to a concern or prepare an offer for customers who want to hear from you.",
  trustBadge: "No payment card required",
  primaryLabel: "Get started",
  emailPlaceholder: "Work email",
  planIntent: { plan: "Pilot" as const },
  imageAlt:
    "Tummly Feedback dashboard showing a guest comment with the Recover guest action.",
} as const

/** Figma How it works strip `6863:12786` (same chips as Home `6757:45771`). */
export const HOW_IT_WORKS_STRIP = {
  eyebrow: "Built for the way UK restaurants work",
} as const

export type HowItWorksStepId = "set-up" | "invite" | "review"

type HowItWorksStep = {
  id: HowItWorksStepId
  title: string
  body: string
  /** Illustration slot size and vertical offset inside the 470px card (Figma). */
  artSize: number
  artOffset: number
  /** Body text box width at lg+ (Figma `6865:35348`, `6865:35403`, `6865:35406`). */
  bodyWidth: number
  /** Extra title drop on the third card (Figma title sits at y=66, others y=50). */
  titleDrop: boolean
  imageAlt: string
}

/** Figma How it works steps `6865:35312`. */
export const HOW_IT_WORKS_STEPS = {
  eyebrow: "HOW IT WORKS",
  title: "Get the Guest Loop started in three steps.",
  body: "From collecting private guest feedback to understanding your customers and encouraging them to return, Tummly connects the entire process.",
  primaryLabel: "Get started",
  secondaryLabel: "Explore 30-day pilot",
  secondaryTo: MARKETING_PILOT_PATH,
  planIntent: { plan: "Pilot" as const },
  steps: [
    {
      id: "set-up",
      title: "Set up your Location",
      body: "Choose your Pilot or paid plan, add the first restaurant and check its Guest Form.",
      artSize: 300,
      artOffset: -23,
      bodyWidth: 331,
      titleDrop: false,
      imageAlt: "QR display on a green platform",
    },
    {
      id: "invite",
      title: "Invite feedback",
      body: "Share your guest link or place a QR where customers will notice it. You can begin online while physical materials are on their way.",
      artSize: 332,
      artOffset: 10,
      bodyWidth: 348,
      titleDrop: false,
      imageAlt: "Speech bubble with a microphone",
    },
    {
      id: "review",
      title: "Review and respond",
      body: "Read what customers say, reply to concerns and prepare relevant invitations for people who choose to hear from you.",
      artSize: 266,
      artOffset: 0,
      bodyWidth: 306,
      titleDrop: true,
      imageAlt: "Restaurant with a return arrow and an offer ticket",
    },
  ] satisfies HowItWorksStep[],
} as const

/** Figma How it works Guest Form `5887:20699` (+ step variants `5887:20694`–`5887:20697`). */
export const HOW_IT_WORKS_GUEST_FORM = {
  eyebrow: "THE GUEST EXPERIENCE",
  title: "A short form. Space for an honest answer.",
  body: "See how customers can share their experiences with your restaurant in just a few simple steps.",
} as const

export type HowItWorksGuestFormStepId =
  | "feedback"
  | "details"
  | "updates"
  | "thank-you"

export type HowItWorksGuestFormStep = {
  id: HowItWorksGuestFormStepId
  /** "01" … "04". */
  number: string
  title: string
  body: string
  imageAlt: string
}

export const HOW_IT_WORKS_GUEST_FORM_STEPS = [
  {
    id: "feedback",
    number: "01",
    title: "Share your experience",
    body: "Customers scan, type or dictate what they want the restaurant to know, then check their words before submitting.",
    imageAlt:
      "Guest Form on a phone showing the feedback field, the Dictate control and the details fields",
  },
  {
    id: "details",
    number: "02",
    title: "Add your details",
    body: "Guests add a first name and either an email address or a UK mobile number. They can separately confirm that contact for a reply about the feedback.",
    imageAlt:
      "Guest Form on a phone with written feedback, a first name and a mobile number",
  },
  {
    id: "updates",
    number: "03",
    title: "Choose whether to hear more",
    body: "Marketing is optional and starts unticked. A chosen email or text subscription becomes active only after the guest confirms the relevant contact and choice.",
    imageAlt:
      "Guest Form on a phone with the thank-you screen and an offer to join Tummly by SMS",
  },
  {
    id: "thank-you",
    number: "04",
    title: "See the thank-you",
    body: "Feedback is complete whether or not the guest joins marketing. Restaurant Offers appear only when configured and eligible. A standard thank-you Offer is independent of marketing; a separate Join Offer states its confirmation conditions.",
    imageAlt:
      "Guest Form on a phone showing the thank-you screen with an offer QR code",
  },
] as const satisfies readonly HowItWorksGuestFormStep[]

/**
 * How it works plans strip — Figma `5661:19770` (Pricing instance `7190:11240`).
 * Same plan cards and sublines as the QR materials plans.
 */
export const HOW_IT_WORKS_PLANS = {
  eyebrow: "GET STARTED WITH TUMMLY",
  title: "Choose how you would like to start.",
  body: "Try Tummly at one Location with the no-card Pilot, or choose a paid plan for your restaurants.",
  compareLabel: AI_ASSISTANT_PLANS.compareLabel,
  compareTo: AI_ASSISTANT_PLANS.compareTo,
  cards: QR_MATERIALS_PRICING.cards,
} as const

type HowItWorksFaqItem = {
  id: string
  question: string
  answer: string
}

/** Figma How it works FAQs `6874:12909`. Copy is verbatim from the frame. */
export const HOW_IT_WORKS_FAQS = {
  title: "Frequently asked questions",
  moreTitle: "Have another question?",
  moreBody:
    "Find more answers about Tummly, pricing, QR materials and Guest Loop.",
  moreCtaLabel: "Explore all FAQs",
  moreCtaTo: MARKETING_FAQS_PATH,
  items: [
    {
      id: "website",
      question: "Do I need a website?",
      answer: "No. The Guest Form has its own link.",
    },
    {
      id: "printer",
      question: "Do I need my own printer?",
      answer:
        "No. We automatically arrange one included A5 QR Counter Display for each qualifying new Location after verification, activation and UK delivery-address confirmation. You can use your digital guest link independently of delivery.",
    },
    {
      id: "feedback-public",
      question: "Is feedback public?",
      answer:
        "Feedback shared through this form goes privately to the restaurant. It does not replace or control public reviews.",
    },
    {
      id: "guest-details",
      question: "What details does a guest provide?",
      answer:
        "Their feedback, first name and either an email address or a UK mobile number. They can separately confirm their contact for a reply about that feedback. Joining marketing is optional and requires its own confirmation.",
    },
    {
      id: "marketing-everyone",
      question: "Can I send marketing to everyone who leaves feedback?",
      answer:
        "Only customers with the appropriate marketing permission for the selected email or text channel can receive promotional messages.",
    },
    {
      id: "offer-without-marketing",
      question: "Can guests get an offer without joining marketing?",
      answer:
        "Yes, where the restaurant has configured an eligible standard thank-you Offer. It is independent of marketing choice. A separate Join Offer states its marketing condition and requires the relevant contact and subscription confirmation.",
    },
    {
      id: "ai-send",
      question: "Can AI send messages for me?",
      answer:
        "AI can prepare the draft. Your authorised team member reviews and confirms sending or scheduling.",
    },
    {
      id: "several-locations",
      question: "Can I use Tummly across several Locations?",
      answer:
        "Yes. Add your first Location, then add others individually, import them or finish later within your paid plan.",
    },
    {
      id: "try-first",
      question: "Can I try Tummly before subscribing?",
      answer:
        "Yes. The 30-day Pilot is £0 at one eligible Location, with no card and no automatic paid renewal.",
    },
  ] satisfies HowItWorksFaqItem[],
} as const

/** Bottom Sign-up CTA — Figma `6874:12985` (Sign up `6955:37020`). */
export const HOW_IT_WORKS_SIGN_UP_CTA: MarketingSignUpCtaContent = {
  title: "Start with your restaurant. Build from there.",
  body: "Try the route from private feedback to a relevant invitation, with one restaurant and a clear next step for your team.",
  primaryLabel: "Get started",
  planIntent: { plan: "Pilot" },
  legalNote: "read",
}
