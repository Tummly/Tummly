import type {
  BillingCadence,
  ManagePlanId,
  PlanFeatureIcon,
} from "@/lib/operatorBillingCredits/managePlanPresentation"
import { MANAGE_PLAN_IDS } from "@/lib/operatorBillingCredits/managePlanPresentation"
import { HELP_CENTRE_CONTACT_URL } from "@/config/support"
import {
  MARKETING_FAQS_PATH,
  MARKETING_PRICING_PATH,
} from "@/constants/marketingNav"

import counterCardImage from "@/assets/images/marketing/pricing/counter-card.png"
import tableTentImage from "@/assets/images/marketing/pricing/table-tent.png"
import windowStickerImage from "@/assets/images/marketing/pricing/window-sticker.png"

export type PricingPlanId = ManagePlanId

export type PricingFeatureRow = {
  label: string
  value: string
  icon?: PlanFeatureIcon
}

export type PricingPlanCard = {
  id: PricingPlanId
  description: string
  monthlyPrice: string
  annualPrice: string
  monthlySubline: string
  annualSubline: string
  ctaLabel: string
  isMostPopular: boolean
  coreFeatures: PricingFeatureRow[]
  allowanceFeatures: PricingFeatureRow[]
}

export type PricingComparisonGroup = {
  id: string
  title: string
  rows: Array<{
    label: string
    values: Record<PricingPlanId, string>
  }>
}

export type PricingFaqItem = {
  id: string
  question: string
  answer: string
}

/** Figma Pricing hero (`4974:29050` desktop / `4974:29544` mobile). */
export const PRICING_PAGE_HERO = {
  title: "Pricing built around Locations and real usage.",
  body: "Start with one Location for 30 days, or choose a paid plan based on the number of Locations you operate. Every paid plan includes monthly AI, Email and SMS allowances, with prepaid top-ups available when you need more.",
  /** Mobile frame uses “free for 30 days” wording. */
  bodyMobile:
    "Start with one Location free for 30 days, or choose a paid plan based on the number of Locations you operate. Every paid plan includes monthly AI, Email and SMS allowances, with prepaid top-ups available when you need more.",
  trustItems: [
    "No automatic Pilot renewal",
    "No uncontrolled usage charges",
  ],
} as const

export const PRICING_CADENCE_COPY = {
  monthly: "Monthly",
  annual: "Annual",
  saveBadge: "SAVE 15%",
  completeFeaturesList: "Complete features list",
} as const

export const PRICING_PLAN_CARDS: PricingPlanCard[] = [
  {
    id: "Pilot",
    description:
      "Experience Guest Loop at one real Location before choosing a paid plan.",
    monthlyPrice: "£0",
    annualPrice: "£0",
    monthlySubline: "For 30 days, no payment card required.",
    annualSubline: "For 30 days, no payment card required.",
    ctaLabel: "Start 30-day Pilot",
    isMostPopular: false,
    coreFeatures: [
      { label: "Locations", value: "1" },
      { label: "Team users", value: "2" },
      { label: "Guest Form access", value: "1 Published + 1 Draft" },
      { label: "Active QR placements", value: "Up to 5" },
      { label: "Private Feedback & recovery", value: "Included" },
      { label: "Guest list & consent", value: "Included" },
      { label: "Campaigns", value: "Included" },
      { label: "Active Offers", value: "1" },
      { label: "Location-based team access", value: "—" },
      { label: "Multi-Location reporting", value: "—" },
      { label: "Starter Kit", value: "Qualifying Location" },
    ],
    allowanceFeatures: [
      { label: "Email credits", value: "500 once", icon: "email" },
      { label: "SMS credits", value: "20 once", icon: "sms" },
      { label: "AI actions", value: "20 once", icon: "ai" },
      { label: "Top-ups", value: "Not available" },
      { label: "Support", value: "Standard Email" },
    ],
  },
  {
    id: "Starter",
    description:
      "For one Location ready to hear from guests, build a permission-based guest list and start bringing them back.",
    monthlyPrice: "£39",
    annualPrice: "£398",
    monthlySubline: "Billed monthly",
    annualSubline: "Billed annually",
    ctaLabel: "Choose Starter",
    isMostPopular: false,
    coreFeatures: [
      { label: "Locations", value: "1" },
      { label: "Team users", value: "3" },
      { label: "Guest Form access", value: "Included" },
      { label: "Active QR placements", value: "Up to 10" },
      { label: "Private Feedback & recovery", value: "Included" },
      { label: "Guest list & consent", value: "Included" },
      { label: "Campaigns", value: "Included" },
      { label: "Active Offers", value: "Up to 3" },
      { label: "Location-based team access", value: "—" },
      { label: "Multi-Location reporting", value: "—" },
      { label: "Starter Kit", value: "Qualifying Location" },
    ],
    allowanceFeatures: [
      { label: "Email credits", value: "2,500/month", icon: "email" },
      { label: "SMS credits", value: "100/month", icon: "sms" },
      { label: "AI actions", value: "100/month", icon: "ai" },
      { label: "Weekly Brief", value: "Included" },
      { label: "Top-ups", value: "Available" },
      { label: "Support", value: "Standard Email" },
    ],
  },
  {
    id: "Growth",
    description:
      "For growing restaurants and small groups that need more reach and multi-Location visibility.",
    monthlyPrice: "£99",
    annualPrice: "£1,010",
    monthlySubline: "Billed monthly",
    annualSubline: "Billed annually",
    ctaLabel: "Choose Growth",
    isMostPopular: true,
    coreFeatures: [
      { label: "Locations", value: "Up to 3" },
      { label: "Team users", value: "10" },
      { label: "Guest Form access", value: "Included" },
      { label: "Active QR placements", value: "Up to 25 per Location" },
      { label: "Private Feedback & recovery", value: "Included" },
      { label: "Guest list & consent", value: "Included" },
      { label: "Campaigns", value: "Included" },
      { label: "Active Offers", value: "Up to 10 account-wide" },
      { label: "Multi-Location reporting", value: "Included" },
      { label: "Location-based team access", value: "Included" },
      { label: "Starter Kit", value: "Each qualifying Location" },
    ],
    allowanceFeatures: [
      { label: "Email credits", value: "10,000/month", icon: "email" },
      { label: "SMS credits", value: "350/month", icon: "sms" },
      { label: "AI actions", value: "500/month", icon: "ai" },
      { label: "Weekly Brief", value: "Location + consolidated" },
      { label: "Top-ups", value: "Available" },
      { label: "Support", value: "Priority Email" },
    ],
  },
  {
    id: "Group",
    description:
      "For restaurant groups that need consolidated reporting, group Campaigns and stronger Location-level control.",
    monthlyPrice: "£199",
    annualPrice: "£2,030",
    monthlySubline: "Billed monthly",
    annualSubline: "Billed annually",
    ctaLabel: "Choose Group",
    isMostPopular: false,
    coreFeatures: [
      { label: "Locations", value: "Up to 5" },
      { label: "Team users", value: "25" },
      { label: "Guest Form access", value: "Included" },
      { label: "Active QR placements", value: "Up to 50 per Location" },
      { label: "Private Feedback & recovery", value: "Included" },
      { label: "Guest list & consent", value: "Included" },
      { label: "Campaigns", value: "Included" },
      { label: "Active Offers", value: "Up to 25 account-wide" },
      { label: "Multi-Location reporting", value: "Included" },
      { label: "Location-based team access", value: "Included" },
      { label: "Starter Kit", value: "Each qualifying Location" },
    ],
    allowanceFeatures: [
      { label: "Email credits", value: "25,000/month", icon: "email" },
      { label: "SMS credits", value: "700/month", icon: "sms" },
      { label: "AI actions", value: "1,500/month", icon: "ai" },
      { label: "Weekly Brief", value: "Location + consolidated" },
      { label: "Top-ups", value: "Available" },
      { label: "Support", value: "Priority + assisted onboarding" },
    ],
  },
]

export const PRICING_GROUP_BANNER = {
  title: "Need more than 5 Locations?",
  body: "Group includes up to 5 Locations. Add more Locations up to 30 for £39/month each, or £398/year each.",
  /** Mobile frame (`4974:29922`) — Contact-only CTA copy. */
  bodyMobile:
    "Add Locations to Group for £39/month. Self-serve Group accounts support up to 30 Locations. Need something different? Talk to us.",
  primaryLabel: "Start with Group",
  secondaryLabel: "Contact us",
  secondaryTo: HELP_CENTRE_CONTACT_URL,
} as const

/** Shared Pricing section inset — 20px mobile, 60px desktop (Figma). */
export const PRICING_SECTION_INSET = "px-5 lg:px-[60px]"

export const PRICING_GUEST_LOOP = {
  title: "Every plan starts with the same Guest Loop.",
  body: "Capture real guest interactions, hear private Feedback, build permission-aware guest relationships and use Tummly to prepare the next appropriate action.",
  steps: [
    {
      id: "capture",
      label: "01 Capture",
      detail: "QR placements\nGuest Forms",
    },
    {
      id: "listen",
      label: "02 Listen",
      detail: "Private Feedback\nAppropriate recovery",
    },
    {
      id: "build",
      label: "03 Build",
      detail: "Permission-aware\nguest list",
    },
    {
      id: "engage",
      label: "04 Engage & learn",
      detail: "Offers, Campaigns, reporting\nand AI-supported next steps",
    },
  ],
} as const

export const PRICING_COMPARE_HEADING = "Compare plans"

export const PRICING_COMPARISON_GROUPS: PricingComparisonGroup[] = [
  {
    id: "plan-essentials",
    title: "Plan essentials",
    rows: [
      {
        label: "Locations",
        values: { Pilot: "1", Starter: "1", Growth: "Up to 3", Group: "Up to 5" },
      },
      {
        label: "Team users",
        values: { Pilot: "2", Starter: "3", Growth: "10", Group: "25" },
      },
      {
        label: "Support",
        values: {
          Pilot: "Pilot support",
          Starter: "Standard Email",
          Growth: "Priority Email",
          Group: "Priority + assisted onboarding",
        },
      },
    ],
  },
  {
    id: "guest-loop",
    title: "Guest Loop",
    rows: [
      {
        label: "Active QR placements",
        values: {
          Pilot: "Up to 5",
          Starter: "Up to 10",
          Growth: "Up to 25 per Location",
          Group: "Up to 50 per Location",
        },
      },
      {
        label: "Private Feedback & recovery",
        values: {
          Pilot: "Included",
          Starter: "Included",
          Growth: "Included",
          Group: "Included",
        },
      },
      {
        label: "Guest Form access",
        values: {
          Pilot: "1 Published + 1 Draft",
          Starter: "Included",
          Growth: "Included",
          Group: "Included",
        },
      },
      {
        label: "Guest list & consent",
        values: {
          Pilot: "Included",
          Starter: "Included",
          Growth: "Included",
          Group: "Included",
        },
      },
      {
        label: "Campaigns",
        values: {
          Pilot: "Included",
          Starter: "Included",
          Growth: "Included",
          Group: "Included",
        },
      },
      {
        label: "Active Offers",
        values: {
          Pilot: "1",
          Starter: "Up to 3",
          Growth: "Up to 10 account-wide",
          Group: "Up to 25 account-wide",
        },
      },
    ],
  },
  {
    id: "multi-location",
    title: "Multi-Location & insights",
    rows: [
      {
        label: "Multi-Location reporting",
        values: { Pilot: "—", Starter: "—", Growth: "Included", Group: "Included" },
      },
      {
        label: "Location-based team access",
        values: { Pilot: "—", Starter: "—", Growth: "Included", Group: "Included" },
      },
      {
        label: "Weekly Brief",
        values: {
          Pilot: "—",
          Starter: "Included",
          Growth: "Per Location + consolidated",
          Group: "Per Location + consolidated",
        },
      },
    ],
  },
  {
    id: "usage-extras",
    title: "Usage & extras",
    rows: [
      {
        label: "AI actions",
        values: {
          Pilot: "20 actions once",
          Starter: "100/month",
          Growth: "500/month",
          Group: "1,500/month",
        },
      },
      {
        label: "Email credits",
        values: {
          Pilot: "500 credits once",
          Starter: "2,500/month",
          Growth: "10,000/month",
          Group: "25,000/month",
        },
      },
      {
        label: "SMS credits",
        values: {
          Pilot: "20 credits once",
          Starter: "100/month",
          Growth: "350/month",
          Group: "700/month",
        },
      },
      {
        label: "Top-ups",
        values: {
          Pilot: "Not available",
          Starter: "Available",
          Growth: "Available",
          Group: "Available",
        },
      },
      {
        label: "Starter Kit",
        values: {
          Pilot: "Qualifying Location",
          Starter: "Qualifying Location",
          Growth: "Each qualifying Location",
          Group: "Each qualifying Location",
        },
      },
      {
        label: "Additional Locations",
        values: {
          Pilot: "—",
          Starter: "—",
          Growth: "—",
          Group: "£39/month each",
        },
      },
    ],
  },
]

export const PRICING_USAGE = {
  title: "Usage stays predictable",
  body: "Included AI, Email and SMS allowances reset monthly. If one allowance is exhausted, only that channel stops until more allowance is available. Tummly does not create a negative balance or automatically bill postpaid overage. Other channels continue to work.",
  channels: [
    {
      id: "ai",
      title: "AI actions",
      definition: "1 action = 1 usable AI result",
      note: "Used when Tummly returns one usable operator-triggered AI result. Automatic Feedback classification and the standard Weekly Brief do not use AI actions.",
      tone: "ai" as const,
      icon: "ai" as const,
    },
    {
      id: "email",
      title: "Email credits",
      definition: "1 credit = 1 eligible Campaign recipient",
      note: "System Emails and private Feedback recovery Emails do not use Campaign credits.",
      tone: "neutral" as const,
      icon: "email" as const,
    },
    {
      id: "sms",
      title: "SMS credits",
      definition: "1 credit = 1 billable SMS segment",
      note: "Longer messages may use more than one segment. Tummly shows the estimated credits before sending.",
      tone: "sms" as const,
      icon: "sms" as const,
    },
  ],
  topUps: {
    title: "Need more? Add prepaid credits when you need them.",
    body: "Top-ups are available on active paid plans only and expire 12 months after purchase. Pilot does not support top-ups.",
    footnote: "*5,000 SMS requires Group or manual approval.",
    rows: [
      {
        id: "ai",
        label: "AI actions",
        icon: "ai" as const,
        packs: ["100 = £5", "500 = £15", "2,000 = £39"],
      },
      {
        id: "email",
        label: "Email credits",
        icon: "email" as const,
        packs: ["5,000 = £10", "20,000 = £30", "50,000 = £60"],
      },
      {
        id: "sms",
        label: "SMS credits",
        icon: "sms" as const,
        packs: ["100 = £12", "500 = £55", "1,000 = £100"],
      },
    ],
  },
} as const

export const PRICING_QR_MATERIALS = {
  title: "Physical QR deployment is included from the start.",
  paragraphs: [
    "Each qualifying newly activated production Location can receive one complimentary standard Starter Kit.",
    "Need more QR guest cards, table tents, counter displays or replacement materials? Additional physical materials are ordered separately through the Tummly Shop after you sign in.",
  ],
  ctaLabel: "See QR materials",
  ctaTo: MARKETING_FAQS_PATH,
  products: [
    {
      id: "counter-card",
      image: counterCardImage,
      imageAlt: "Tummly counter cards with QR codes and guest offers",
    },
    {
      id: "table-tent",
      image: tableTentImage,
      imageAlt: "Tummly table tent with QR code for private feedback",
    },
    {
      id: "window-sticker",
      image: windowStickerImage,
      imageAlt: "Tummly window sticker inviting guests to share feedback",
    },
  ],
} as const

/** Figma Pricing Sign up CTA (`4974:29531`). */
export const PRICING_SIGN_UP_CTA = {
  title: "Ready to see Guest Loop in a real restaurant?",
  body: "Start with one Location and see how Guest Loop works with real guests.",
  primaryLabel: "Start 30-day Pilot",
  secondaryLabel: "See pricing",
  secondaryTo: MARKETING_PRICING_PATH,
  badge: "1 verified Location · No payment card required",
  planIntent: { plan: "Pilot" as const },
} as const

export const PRICING_FAQS = {
  title: "Frequently asked questions",
  moreTitle: "Have another question?",
  moreBody:
    "Find more answers about Tummly, pricing, QR materials and Guest Loop.",
  moreCtaLabel: "Explore all FAQs",
  moreCtaTo: MARKETING_FAQS_PATH,
  items: [
    {
      id: "pilot-renew",
      question: "Does the 30-day Pilot renew automatically?",
      answer:
        "No. The Pilot is £0, requires no payment card and does not automatically convert to a paid plan.",
    },
    {
      id: "pilot-start",
      question: "When does my 30-day Pilot start?",
      answer:
        "Setup starts immediately. The live 30-day Pilot starts at the first real production guest scan, or 48 hours after the included Starter Kit is confirmed delivered - whichever happens first. Test scans do not start it.",
    },
    {
      id: "pilot-end",
      question: "What happens when my Pilot ends?",
      answer:
        "At day 30 the Pilot moves to a soft lock. You can still log in, and your history and QR setup are preserved while you decide whether to move to a paid plan.",
    },
    {
      id: "rollover",
      question: "Do unused monthly allowances roll over?",
      answer:
        "No. Included AI, Email and SMS allowances reset monthly. Annual plans also release their allowances monthly.",
    },
    {
      id: "allowance-exhausted",
      question: "What happens if I use all of an allowance?",
      answer:
        "Only the affected channel stops. Other channels continue to work. There is no negative balance or automatic postpaid overage.",
    },
    {
      id: "ai-action",
      question: "What counts as an AI action?",
      answer:
        "One AI action is charged when one usable operator-triggered AI output is returned. Failed, blocked or timed-out requests do not use an AI action. The standard scheduled Weekly Brief uses none.",
    },
    {
      id: "email-credit",
      question: "What counts as an Email credit?",
      answer:
        "One Email credit is one eligible Campaign recipient accepted by Tummly's Campaign Email provider. System Emails and controlled one-to-one Feedback recovery Emails do not use Campaign Email credits.",
    },
    {
      id: "sms-credit",
      question: "What counts as an SMS credit?",
      answer:
        "One SMS credit is one provider-billable SMS segment. Longer or Unicode messages can use more than one segment per recipient.",
    },
    {
      id: "buy-credits",
      question: "Can I buy more credits?",
      answer:
        "Yes, on an active paid plan. Top-ups expire 12 months after purchase. Pilot does not support top-ups.",
    },
    {
      id: "qr-before-start",
      question: "Do I need physical QR materials before I can start?",
      answer:
        "No. Setup can begin before physical delivery, and digital/self-print QR assets are available for immediate testing where suitable.",
    },
    {
      id: "starter-kit",
      question: "How does the included Starter Kit work?",
      answer:
        "Each newly activated, verified production Location receives one complimentary standard Starter Kit once under the qualifying operator/business relationship. Unused plan capacity does not itself create or dispatch a kit.",
    },
    {
      id: "pilot-to-paid",
      question: "What happens if I move from Pilot to a paid plan?",
      answer:
        "Your Location, Guest Form, QR placement/token and history continue. The same Location does not receive a second complimentary Starter Kit just because it moves from Pilot to paid.",
    },
    {
      id: "group-locations",
      question: "How many Locations can Group support?",
      answer:
        "Group includes up to 5 Locations. Additional Locations can be added up to a 30-Location self-serve limit for £39/month each or £398/year each.",
    },
    {
      id: "switch-plans",
      question: "Can I switch plans or add Locations later?",
      answer:
        "Yes. Tummly checks current plan capacity and the active pricebook before a paid change. If a charge is required, it is shown before you confirm.",
    },
    {
      id: "overages",
      question: "Will Tummly automatically charge me for usage overages?",
      answer:
        "No. When an allowance is exhausted, the affected channel stops until allowance becomes available, an eligible top-up is purchased or the plan is changed.",
    },
  ] satisfies PricingFaqItem[],
}

export function pricingPlanPrice(
  card: PricingPlanCard,
  cadence: BillingCadence,
): {
  amount: string
  suffix: string
  /** Desktop card subline (e.g. “Billed monthly”). */
  subline: string
  /** Mobile card subline — annual equivalent when previewing monthly. */
  sublineMobile: string
} {
  if (card.id === "Pilot") {
    return {
      amount: card.monthlyPrice,
      suffix: "/ for 30 days",
      subline: card.monthlySubline,
      sublineMobile: "No payment card required. No automatic paid renewal.",
    }
  }
  if (cadence === "annual") {
    return {
      amount: card.annualPrice,
      suffix: "/ year",
      subline: card.annualSubline,
      sublineMobile: card.annualSubline,
    }
  }
  return {
    amount: card.monthlyPrice,
    suffix: "/ month",
    subline: card.monthlySubline,
    sublineMobile: `${card.annualPrice}/year`,
  }
}

export function pricingCompareAnnualSubline(planId: PricingPlanId): string | null {
  if (planId === "Pilot") {
    return null
  }
  const card = PRICING_PLAN_CARDS.find((entry) => entry.id === planId)
  return card != null ? `${card.annualPrice}/year` : null
}

export { MANAGE_PLAN_IDS as PRICING_PLAN_IDS }
