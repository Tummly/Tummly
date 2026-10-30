import type { MarketingSignUpCtaContent } from "@/components/marketing/MarketingSignUpCta"
import {
  MARKETING_FAQS_PATH,
  MARKETING_FOR_GROUPS_PATH,
  MARKETING_HOW_IT_WORKS_PATH,
  MARKETING_PILOT_PATH,
} from "@/constants/marketingNav"
import { AI_ASSISTANT_PLANS } from "@/content/marketing/aiAssistantPage"
import { planLocationsLimit } from "@/content/marketing/homePage"

/** Shared section inset — matches Product / How it works (20px mobile, 60px desktop). */
export const QR_MATERIALS_SECTION_INSET = "px-5 lg:px-[60px]"

/** Shared vertical pad — 50px mobile, 70px desktop. Figma hero `5661:19336`. */
export const QR_MATERIALS_SECTION_Y = "py-12.5 lg:py-[70px]"

export const QR_MATERIALS_SECTION_BODY =
  "font-sans text-base font-normal leading-[22px] text-[#141414] lg:text-[18px] lg:leading-6"

/** Figma QR materials hero `5661:19336`. */
export const QR_MATERIALS_HERO = {
  /** Figma text; the hero applies `uppercase`. */
  eyebrow: "QR materials",
  title: "Put a simple invitation where customers will see it.",
  body: "From counters and tables to takeaway packaging, make it easy for customers to scan and share private feedback with your restaurant. Explore QR formats for different touchpoints and see what's available.",
  primaryLabel: "Get started",
  secondaryLabel: "See how it works",
  secondaryTo: MARKETING_HOW_IT_WORKS_PATH,
  note: "30-day Pilot: £0 at one Location. No card. No automatic paid renewal.",
  imageAlt:
    "Friends sharing a meal at a restaurant table with a green Tummly QR display.",
} as const

/** Figma section heading — 36 / 48 *headline* semibold (`5661:19349`). */
export const QR_MATERIALS_SECTION_HEADING =
  "font-jakarta text-[28px] font-semibold leading-9 text-black lg:text-[36px] lg:leading-[48px]"

/**
 * Figma QR materials catalogue `5661:19364`.
 * Same Capture materials panel as Home QR materials (`4902:19649`), no eyebrow.
 */
export const QR_MATERIALS_CATALOGUE = {
  title: "Find the right place to start the conversation.",
  body: "From counters and tables to takeaway and delivery orders, choose the QR materials that fit your restaurant and help you collect valuable guest feedback.",
} as const

export type QrMaterialsFeedbackStep = {
  id: "location" | "preview" | "place" | "review"
  stepLabel: string
  title: string
  body: string
}

/**
 * Figma QR materials steps `5661:19391`.
 * Four illustrated cards (`5661:19400` … `5661:19415`) joined by chevron chips.
 */
export const QR_MATERIALS_FEEDBACK_STEPS = {
  title: "From the right QR to useful customer feedback.",
  steps: [
    {
      id: "location",
      stepLabel: "Step 1",
      title: "Choose your Location",
      body: "Select the restaurant where you want to collect customer feedback.",
    },
    {
      id: "preview",
      stepLabel: "Step 2",
      title: "Preview your Guest Form",
      body: "Check your restaurant details and the feedback experience customers will see.",
    },
    {
      id: "place",
      stepLabel: "Step 3",
      title: "Place and test your QR",
      body: "Display your QR or share your guest link. Check that it opens the correct form.",
    },
    {
      id: "review",
      stepLabel: "Step 4",
      title: "Review customer feedback",
      body: "Read what customers share and identify any concerns that need a response.",
    },
  ] satisfies QrMaterialsFeedbackStep[],
} as const

/**
 * Figma QR materials pricing `6859:12685`.
 * Home Pricing plan strip (`4902:19866`) with QR page header copy.
 */
function planLocationsLine(planId: "Starter" | "Growth" | "Group") {
  const limit = planLocationsLimit(planId)
  return limit === "1" ? "1 location only" : `${limit} locations`
}

const QR_MATERIALS_PRICING_SUBLINES = {
  Pilot: "No payment card required.",
  Starter: planLocationsLine("Starter"),
  Growth: planLocationsLine("Growth"),
  Group: planLocationsLine("Group"),
} as const

export const QR_MATERIALS_PRICING = {
  eyebrow: "PLANS & PRICING",
  title: "A plan that fits your restaurant.",
  body: "Whether you're running one restaurant or managing multiple locations, choose a Tummly plan that suits your business and supports your growth.",
  compareLabel: AI_ASSISTANT_PLANS.compareLabel,
  compareTo: AI_ASSISTANT_PLANS.compareTo,
  cards: AI_ASSISTANT_PLANS.cards.map((card) => ({
    ...card,
    description:
      card.id === "Pilot"
        ? "Try Tummly at one Location before choosing a paid plan."
        : card.description,
    subline: QR_MATERIALS_PRICING_SUBLINES[card.id],
  })),
} as const

/** Figma QR materials Counter Display panel `6855:12460`. */
export const QR_MATERIALS_COUNTER_DISPLAY = {
  title: "A Counter Display to help each new Location get started.",
  body: "Your first A5 QR Counter Display is included for each eligible new restaurant Location. We’ll arrange it automatically once setup and UK delivery details are confirmed, aiming to deliver within 3–5 working days of order confirmation. You can start using your digital QR link without waiting for delivery.",
  primaryLabel: "Get started",
  secondaryLabel: "Explore 30-day Pilot",
  secondaryTo: MARKETING_PILOT_PATH,
  trustItems: ["Free for 30 days · One Location", "No payment card required"],
  planIntent: { plan: "Pilot" },
} as const

/** Figma QR materials Built for your restaurant `6853:12456`. */
export const QR_MATERIALS_BUILT_FOR = {
  eyebrow: "BUILT FOR YOUR RESTAURANT",
  title: "Keep each restaurant connected to its customers.",
  body: "Give every Location its own feedback entry point. See where comments came from and keep the local context as you add more restaurants.",
  primaryLabel: "Get started",
  secondaryLabel: "See Tummly for groups",
  secondaryTo: MARKETING_FOR_GROUPS_PATH,
  trustItems: ["£0 for 30 days", "No payment card required"],
  planIntent: { plan: "Pilot" },
  imageAlt: "Friends sharing seafood at a window table with a green Tummly QR display.",
} as const

export type QrMaterialsFaqItem = {
  id: string
  question: string
  answer: string
}

/** Figma QR materials FAQs `6858:12612`. */
export const QR_MATERIALS_FAQS = {
  title: "Frequently asked questions",
  moreTitle: "Have another question?",
  moreBody:
    "Find more answers about Tummly, pricing, QR materials and Guest Loop.",
  moreCtaLabel: "Explore all FAQs",
  moreCtaTo: MARKETING_FAQS_PATH,
  items: [
    {
      id: "how-work",
      question: "How do Tummly QR materials work?",
      answer:
        "Customers scan your restaurant's QR code to open a private feedback form. Your team can review their experiences and respond to concerns.",
    },
    {
      id: "where-place",
      question: "Where can I place Tummly QR materials?",
      answer:
        "From counters and collection points to tables and takeaway packaging, choose formats suited to your restaurant as they become available.",
    },
    {
      id: "website-printer",
      question: "Do I need a website or printer?",
      answer:
        "No. Your Guest Form has its own link. We automatically arrange the included A5 QR Counter Display for each qualifying new Location after verification, activation and UK delivery-address confirmation. Digital QR access does not depend on its arrival.",
    },
    {
      id: "multiple-placements",
      question: "Can I use multiple QR placements?",
      answer:
        "Yes. Use multiple QR placements within your plan's allowance to collect feedback and understand where customers are engaging.",
    },
    {
      id: "multiple-restaurants",
      question: "Can I use QR materials across multiple restaurants?",
      answer:
        "Yes. Each Location can have its own QR placements and Guest Form, depending on your plan.",
    },
    {
      id: "complimentary",
      question: "Do I receive complimentary QR materials when I join?",
      answer:
        "One A5 QR Counter Display is included for each qualifying new restaurant Location, once only. We arrange it automatically after eligible activation and confirmation of a UK delivery address. Moving the same Location from Pilot to paid does not provide another.",
    },
    {
      id: "additional",
      question: "Can I order additional QR materials?",
      answer:
        "Yes. Additional A5 QR Counter Displays can be ordered through Tummly Shop at the current price and delivery charge. The other formats follow the availability shown in the Tummly Shop.",
    },
  ] satisfies QrMaterialsFaqItem[],
} as const

/** Bottom Sign-up CTA — Figma `5661:19499` (Sign up `6942:25257`, editorial). */
export const QR_MATERIALS_SIGN_UP_CTA: MarketingSignUpCtaContent = {
  title: "Give customers an easy way to tell you.",
  body: "Choose your first Location, check the form and put the invitation where it belongs.",
  primaryLabel: "Get started",
  planIntent: { plan: "Pilot" },
  legalNote: "read",
}
