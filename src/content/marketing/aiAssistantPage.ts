import { MARKETING_DEMO_PATH, MARKETING_FAQS_PATH, MARKETING_PRICING_PATH } from "@/constants/marketingNav"
import type { AiAssistantVideoKey } from "@/lib/marketingMedia"
import type { MarketingSignUpCtaContent } from "@/components/marketing/MarketingSignUpCta"

/** Shared section inset — matches Pricing (20px mobile, 60px desktop). */
export const AI_ASSISTANT_SECTION_INSET = "px-5 lg:px-[60px]"

/** Shared vertical pad — 50px mobile, 70px desktop. */
export const AI_ASSISTANT_SECTION_Y = "py-12.5 lg:py-[70px]"

export const AI_ASSISTANT_SECTION_HEADING =
  "font-jakarta text-[34px] font-medium leading-none text-black lg:text-[46px] lg:leading-[58px]"

export const AI_ASSISTANT_SECTION_BODY =
  "font-sans text-base font-normal leading-[22px] text-[#141414] lg:text-[18px] lg:leading-6"

/** Figma hero `5661:14925`. */
export const AI_ASSISTANT_HERO = {
  title: "Restaurant intelligence, built into Tummly AI.",
  body: "Tummly AI helps restaurant teams understand what guests are saying, analyse performance, identify what needs attention and prepare Campaign, Offer and recovery drafts for review.",
  badge: "No payment card required",
  primaryLabel: "Start 30-day Pilot",
  secondaryLabel: "See how it works",
  secondaryHref: "#guest-loop-video",
} as const

export type AiAssistantGuestLoopChip = {
  id: string
  label: string
  icon: "feedback" | "guests" | "campaigns" | "reports" | "ai"
}

/** Figma Guest Loop `5924:17849`. */
export const AI_ASSISTANT_GUEST_LOOP = {
  title: "Built for restaurant teams across the Guest Loop.",
  chips: [
    { id: "feedback", label: "Private Feedback", icon: "feedback" },
    { id: "guests", label: "Guest relationships", icon: "guests" },
    { id: "campaigns", label: "Offers & Campaigns", icon: "campaigns" },
    { id: "reports", label: "Reports", icon: "reports" },
    { id: "ai", label: "AI-assisted actions", icon: "ai" },
  ] satisfies AiAssistantGuestLoopChip[],
} as const

export type AiAssistantHowItWorksStep = {
  id: string
  eyebrow: string
  title: string
  body: string
  videoKey: AiAssistantVideoKey
  /** Text on the left (true) or right (false) on desktop. */
  textFirst: boolean
  primaryLabel: string
  secondaryLabel: string
}

/** Figma How it works `5892:31812`. */
export const AI_ASSISTANT_HOW_IT_WORKS = {
  eyebrow: "How Tummly AI works",
  titleLine1: "Useful assistance.",
  titleLine2: "Controlled execution.",
  body: "Tummly AI works within your account, Location and permissions. It can answer questions and prepare work, while important actions remain under your review and control.",
  steps: [
    {
      id: "ask",
      eyebrow: "01 Ask",
      title: "Ask a question or request a piece of work.",
      body: "Ask Tummly AI from the page you’re working on or from the full Assistant workspace.",
      videoKey: "stepAsk",
      textFirst: true,
      primaryLabel: "Start 30-day Pilot",
      secondaryLabel: "Try a demo",
    },
    {
      id: "context",
      eyebrow: "02 Understand context",
      title: "Tummly understands the context of your request.",
      body: "Your Restaurant, Location, current page and selected period help Tummly understand what you’re asking about.",
      videoKey: "stepContext",
      textFirst: false,
      primaryLabel: "Start 30-day Pilot",
      secondaryLabel: "Try a demo",
    },
    {
      id: "permissions",
      eyebrow: "03 Check access & data",
      title: "Tummly checks what it can use.",
      body: "Your access, Location permissions, guest permissions and relevant product rules determine which information Tummly can use for your request.",
      videoKey: "stepPermissions",
      textFirst: true,
      primaryLabel: "Start 30-day Pilot",
      secondaryLabel: "Try a demo",
    },
    {
      id: "draft",
      eyebrow: "04 Prepare",
      title: "Get an answer or prepare the next step.",
      body: "Tummly AI can explain what is happening, recommend what to do next or prepare work for you to review.",
      videoKey: "stepDraft",
      textFirst: false,
      primaryLabel: "Start 30-day Pilot",
      secondaryLabel: "Try a demo",
    },
    {
      id: "review",
      eyebrow: "05 Review & act",
      title: "Review important actions before they happen.",
      body: "When Tummly prepares a Campaign, Offer, recovery response or another important action, you review the details before anything is sent or activated.",
      videoKey: "stepReview",
      textFirst: true,
      primaryLabel: "Start 30-day Pilot",
      secondaryLabel: "Try a demo",
    },
  ] satisfies AiAssistantHowItWorksStep[],
} as const

/** Figma mid CTA `5661:15749`. */
export const AI_ASSISTANT_MID_CTA = {
  title: "One AI assistant across the Guest Loop",
  body: "Understand Feedback, analyse performance and prepare Campaign, Offer and recovery drafts using the context of your Restaurant, Location and permissions.",
  primaryLabel: "Start 30-day Pilot",
  secondaryLabel: "Try a demo",
  secondaryTo: MARKETING_DEMO_PATH,
} as const

export type AiAssistantPlanStripCard = {
  id: "Pilot" | "Starter" | "Growth" | "Group"
  name: string
  description: string
  priceAmount: string
  priceSuffix: string
  subline: string
  ctaLabel: string
  aiAllowance: string
  isHighlighted: boolean
}

/** Figma plans strip `5661:17526` — monthly amounts + AI allowances. */
export const AI_ASSISTANT_PLANS = {
  eyebrow: "TUMMLY AI",
  title: "Tummly AI is included in every plan.",
  body: "Every Tummly plan includes AI usage, with the allowance depending on your plan.",
  compareLabel: "Compare all plans",
  compareTo: MARKETING_PRICING_PATH,
  cards: [
    {
      id: "Pilot",
      name: "Pilot",
      description:
        "Try Tummly at one real Location before choosing a paid plan.",
      priceAmount: "£0",
      priceSuffix: "/ for 30 days",
      subline: "No payment card required. No automatic paid renewal.",
      ctaLabel: "Start 30-day Pilot",
      aiAllowance: "20 AI actions once",
      isHighlighted: false,
    },
    {
      id: "Starter",
      name: "Starter",
      description:
        "For one Location ready to collect private Feedback, build a guest list and bring guests back.",
      priceAmount: "£39",
      priceSuffix: "/ month",
      subline: "£398/year",
      ctaLabel: "Choose Starter",
      aiAllowance: "100 / month",
      isHighlighted: false,
    },
    {
      id: "Growth",
      name: "Growth",
      description:
        "For growing restaurants and small groups that need more reach and multi-Location visibility.",
      priceAmount: "£99",
      priceSuffix: "/ month",
      subline: "£1,010/year",
      ctaLabel: "Choose Growth",
      aiAllowance: "500 / month",
      isHighlighted: true,
    },
    {
      id: "Group",
      name: "Group",
      description:
        "For growing restaurant groups that need more Locations, control and consolidated reporting.",
      priceAmount: "£199",
      priceSuffix: "/ month",
      subline: "£2,030/year",
      ctaLabel: "Choose Group",
      aiAllowance: "1,500 / month",
      isHighlighted: false,
    },
  ] satisfies AiAssistantPlanStripCard[],
} as const

export type AiAssistantFaqItem = {
  id: string
  question: string
  answer: string
}

/** Figma FAQs `5661:17590`. */
export const AI_ASSISTANT_FAQS = {
  title: "Frequently asked questions",
  moreTitle: "Have another question?",
  moreBody:
    "Find more answers about Tummly, pricing, QR materials and Guest Loop.",
  moreCtaLabel: "Explore all FAQs",
  moreCtaTo: MARKETING_FAQS_PATH,
  items: [
    {
      id: "what-help",
      question: "What can Tummly AI help me with?",
      answer:
        "Tummly AI can help you understand Feedback, analyse performance, identify what needs attention, and prepare Campaign, Offer and recovery drafts for review.",
    },
    {
      id: "access",
      question: "Does Tummly AI have access to everything in my account?",
      answer:
        "No. Tummly AI only uses the data and tools available within your Restaurant, Location, role permissions and selected context. It cannot bypass your access controls.",
    },
    {
      id: "feedback",
      question: "Can Tummly AI understand guest Feedback?",
      answer:
        "Yes. It can summarise Feedback, identify recurring themes and surface submissions that may need attention.",
    },
    {
      id: "permissions",
      question: "Can Tummly AI change guest permissions?",
      answer:
        "No. Guest permissions are managed separately. Tummly AI does not change follow-up, Email or SMS consent.",
    },
    {
      id: "recovery",
      question: "Can Tummly AI prepare guest recovery responses?",
      answer:
        "Yes. Where follow-up is available, Tummly AI can prepare recovery responses for you to review and edit before anything is sent.",
    },
    {
      id: "campaigns-auto",
      question: "Does Tummly AI send Campaigns automatically?",
      answer:
        "No. Tummly AI can prepare Campaign Drafts, but sending remains under your review and control.",
    },
    {
      id: "offers",
      question: "Can Tummly AI create Offers?",
      answer:
        "Tummly AI can prepare an Offer Draft for review, including proposed terms, validity and redemption settings. You decide whether to use or activate it.",
    },
    {
      id: "weekly-brief",
      question: "What is the Weekly Brief?",
      answer:
        "The Weekly Brief brings together recent Guest Loop activity and highlights what changed, what may need attention and the most relevant insights from the week.",
    },
    {
      id: "multi-location",
      question: "Does Tummly AI work across multiple Locations?",
      answer:
        "Where your plan and permissions allow it, Tummly AI can work with individual Locations or across multiple Locations available to you.",
    },
    {
      id: "included",
      question: "Is Tummly AI included in my plan?",
      answer:
        "Yes. Tummly AI is included across Tummly plans, with AI action allowances depending on the plan.",
    },
  ] satisfies AiAssistantFaqItem[],
} as const

export const AI_ASSISTANT_SIGN_UP_CTA: MarketingSignUpCtaContent = {
  title: "Ready to see Guest Loop in a real restaurant?",
  body: "Start with one Location and see how Guest Loop works with real guests.",
  primaryLabel: "Start 30-day Pilot",
  badge: "No payment card required",
  planIntent: { plan: "Pilot" },
}
