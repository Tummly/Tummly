import type {
  BillingCadence,
  ManagePlanId,
  PlanFeatureIcon,
} from "@/lib/operatorBillingCredits/managePlanPresentation"
import { MANAGE_PLAN_IDS } from "@/lib/operatorBillingCredits/managePlanPresentation"
import type { PublicPlanPrices } from "@/lib/marketingCustomPlan/publicPlanPrices"
import {
  MARKETING_CUSTOM_PLAN_PATH,
  MARKETING_FAQS_PATH,
  MARKETING_QR_MATERIALS_PATH,
} from "@/constants/marketingNav"

export type PricingPlanId = ManagePlanId

export type PricingFeatureRow = {
  label: string
  value: string
  icon?: PlanFeatureIcon
}

export type PricingPlanCard = {
  id: PricingPlanId
  description: string
  /** Pilot only. Paid plan prices come from the published catalogue. */
  monthlyPrice?: string
  annualPrice?: string
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

/**
 * Figma customise CTA `6618:17381` — below plan cards.
 * Heading uses “Need more than 5 Locations?” (replaces the old Group banner).
 */
export const PRICING_CUSTOMISE_PLAN = {
  eyebrow: "Customise your plan",
  title: "Need more than 5 Locations?",
  body: "Choose your Locations, add extra team users, QR placements, Offers and monthly credits, and see your estimated subscription price.",
  ctaLabel: "Customise your plan",
  ctaTo: MARKETING_CUSTOM_PLAN_PATH,
} as const

/** Figma UK restaurant sectors strip `6731:14319`. */
export const PRICING_SECTORS = {
  title: "Built for the way UK restaurants work",
  items: [
    { id: "fish-and-chip", label: "Fish-and-chip shops" },
    { id: "kebab", label: "Kebab shops" },
    { id: "sushi", label: "Sushi bars" },
    { id: "noodle", label: "Noodle bars" },
    { id: "burrito", label: "Burrito bars" },
    { id: "dessert", label: "Dessert parlours" },
    { id: "bubble-tea", label: "Bubble-tea shops" },
    { id: "juice", label: "Juice bars" },
    { id: "casual-dining", label: "Casual-dining restaurants" },
  ],
} as const

export type PricingSectorId = (typeof PRICING_SECTORS.items)[number]["id"]

/** Shared Pricing section inset — 20px mobile, 60px desktop (Figma `5139:4326`). */
export const PRICING_SECTION_INSET = "px-5 lg:px-[60px]"

/** Shared Pricing section vertical pad — 50px mobile, 70px desktop. */
export const PRICING_SECTION_Y = "py-12.5 lg:py-[70px]"

/**
 * Pricing section H2 — *headline* Jakarta Medium 500 (Figma), not Bold.
 * Use `font-[500]` so variable-font weight cannot fall back to browser h2 bold.
 * Do not compose with `marketingSectionHeading` (that token is Bold).
 */
export const PRICING_SECTION_HEADING =
  "font-jakarta text-[34px] font-[500] leading-none text-black lg:text-[46px]"

/** Pricing hero H1 — Jakarta Medium 66/74 desktop (Figma `5437:13643`). */
export const PRICING_HERO_HEADING =
  "font-jakarta text-[34px] font-[500] leading-none text-[#141414] lg:text-[66px] lg:leading-[74px]"

/** Pricing body — *functional* Helvetica Neue Regular 18/24. */
export const PRICING_SECTION_BODY =
  "font-sans text-base font-normal leading-[22px] text-[#141414] lg:text-[18px] lg:leading-6"

/**
 * Marketing plan cards — Figma `5661:13671`.
 * Keep separate from Operator `MANAGE_PLAN_*` tokens.
 */
export const PRICING_PLAN_CARDS_GRID_CLASS =
  "grid grid-cols-1 gap-6 xl:grid-cols-[repeat(4,minmax(0,1fr))] xl:gap-4"

export const PRICING_PLAN_CARD_CLASS =
  "flex h-full w-full min-w-0 flex-col gap-10 rounded-[6px] px-3 py-6"

export const PRICING_PLAN_CARD_POPULAR_CLASS =
  "flex h-full w-full min-w-0 flex-col gap-10 rounded-[6px] border border-solid border-[#14a74a] bg-[rgba(20,167,74,0.05)] px-[26px] py-6"

/** Plan name — *headline* Jakarta SemiBold 30. */
export const PRICING_PLAN_CARD_TITLE_CLASS =
  "m-0 font-jakarta text-[30px] font-semibold leading-none text-[#141414]"

/** Plan blurb — *functional* Helvetica Neue Regular 14/19. */
export const PRICING_PLAN_CARD_DESCRIPTION_CLASS =
  "m-0 max-w-[346px] font-sans text-sm font-normal leading-[19px] text-[#141414]"

/** Price amount — *functional* Helvetica Neue Regular 36 (Figma, not Jakarta). */
export const PRICING_PLAN_PRICE_AMOUNT_CLASS =
  "font-sans text-[36px] font-normal leading-none text-[#141414]"

/** Price suffix — *functional* Helvetica Neue Regular 14. */
export const PRICING_PLAN_PRICE_SUFFIX_CLASS =
  "font-sans text-sm font-normal leading-none text-[#141414]"

/** Price subline — *functional* Helvetica Neue Medium 14 / `#7c7c7c`. */
export const PRICING_PLAN_PRICE_SUBLINE_CLASS =
  "m-0 min-h-10 font-sans text-sm font-medium leading-[normal] text-[#7c7c7c]"

export const PRICING_PLAN_FEATURE_ROW_CLASS =
  "flex items-center justify-between gap-3 font-sans text-sm whitespace-nowrap text-[#141414]"

export const PRICING_PLAN_FEATURE_LABEL_CLASS = "shrink-0 font-medium"

export const PRICING_PLAN_FEATURE_VALUE_CLASS = "shrink-0 text-right font-normal"

/** Figma Guest Loop cards `5661:14067`. */
export const PRICING_GUEST_LOOP = {
  title: "Every plan starts with the same Guest Loop.",
  body: "From a guest's first scan to future engagement, Tummly connects the key parts of your restaurant's guest experience. Plans differ in Locations, allowances and additional capabilities.",
  cards: [
    {
      id: "capture",
      title: "Capture",
      body: "Invite guests through QR touchpoints",
      imageAlt: "Isometric QR stand illustration",
    },
    {
      id: "listen",
      title: "Listen",
      body: "Receive private guest Feedback",
      imageAlt: "Isometric speech bubble with sound-wave illustration",
    },
    {
      id: "engage",
      title: "Engage",
      body: "Create Offers and eligible Campaigns",
      imageAlt: "Isometric envelope with offer card illustration",
    },
    {
      id: "understand",
      title: "Understand",
      body: "Review supported performance data",
      imageAlt: "Isometric laptop with chart and magnifying glass illustration",
    },
  ],
} as const

export type PricingGuestLoopCardId =
  (typeof PRICING_GUEST_LOOP.cards)[number]["id"]

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
    ],
  },
]

/**
 * Figma How usage works `5661:14315` — after Compare plans.
 * Email body uses recipient wording (Figma paste still had SMS segment copy).
 */
export const PRICING_HOW_USAGE_WORKS = {
  eyebrow: "Usage and credits",
  title: "Understand your credits before you send.",
  body: "Every paid plan includes Email, SMS and AI allowances. See how they're used, estimate what your restaurant may need and review extra credit options.",
  ctaLabel: "Calculate my Usage and credits",
  ctaTo: MARKETING_CUSTOM_PLAN_PATH,
  channels: [
    {
      id: "ai" as const,
      title: "AI actions",
      badge: "1 action = 1 successful AI result",
      body: "1 action for each completed, usable operator-triggered AI output.",
    },
    {
      id: "email" as const,
      title: "Email credits",
      badge: "1 credit = 1 email recipient",
      body: "1 credit for each Campaign email recipient.",
    },
    {
      id: "sms" as const,
      title: "SMS credits",
      badge: "1 credit = 1 SMS segment",
      body: "1 credit per provider-billable SMS segment. Longer messages may use more.",
    },
  ],
} as const

export type PricingHowUsageChannelId =
  (typeof PRICING_HOW_USAGE_WORKS.channels)[number]["id"]

/** Figma Pricing QR materials `5661:14445` — below How usage works. */
export const PRICING_QR_MATERIALS = {
  eyebrow: "QR materials",
  title: "Put Tummly where guests already are.",
  paragraphs: [
    "Each eligible, newly activated and verified restaurant Location receives one included QR Counter Display, ready to invite guests to your Guest Loop.",
    "You can also use supported downloadable QR assets and explore additional materials for your restaurant.",
  ],
  ctaLabel: "Explore QR materials",
  ctaTo: MARKETING_QR_MATERIALS_PATH,
  imageAlt:
    "Cafe counter with a Tummly QR Counter Display while a staff member works on a laptop",
} as const

/**
 * Figma Built for better guest relationships `6728:13979` —
 * after QR materials on Pricing.
 */
export const PRICING_GUEST_RELATIONSHIPS = {
  title: "Built for better guest relationships",
  body: "From QR touchpoints and private Feedback to permission-aware guest engagement and useful weekly insight.",
  cards: [
    {
      id: "qr-guest-links",
      title: "QR & Guest Links",
      body: "Make it easy for guests to share Feedback through QR touchpoints and Guest Links across your restaurant.",
    },
    {
      id: "private-feedback",
      title: "Private Feedback",
      body: "Hear directly from guests, understand their experiences and identify what needs attention.",
    },
    {
      id: "guest-relationships",
      title: "Guest Relationships",
      body: "Build stronger guest connections through permission-aware Campaigns, Offers and thoughtful follow-ups.",
    },
    {
      id: "reports-briefs",
      title: "Reports & Briefs",
      body: "Track your Guest Loop performance, understand what guests are saying and identify where improvements are needed.",
    },
  ],
} as const

export type PricingGuestRelationshipsCardId =
  (typeof PRICING_GUEST_RELATIONSHIPS.cards)[number]["id"]

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
        "When you successfully complete setup for the Pilot account you selected. Your setup confirmation shows the start and end dates. No QR scan or physical delivery is needed.",
    },
    {
      id: "pilot-end",
      question: "What happens when my Pilot ends?",
      answer:
        "There is no automatic charge or paid renewal. Live features pause when the Pilot ends. Choose a paid plan to continue, with account history retained under the applicable retention rules.",
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
      question: "How are extra Locations priced?",
      answer:
        "Group includes five Locations. Additional slots are priced progressively: 6–30, 31–50, 51–100, 101–250 and 251–500. Each band applies to those additional slots; earlier slots keep their rate. Your quote shows the calculation.",
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

export type PricingPlanPriceView = {
  /** Null until the catalogue price for a paid plan has loaded. */
  amount: string | null
  suffix: string
  /** Desktop card subline (e.g. “Billed monthly”). */
  subline: string
  /** Mobile card subline; annual equivalent when previewing monthly. Null while loading. */
  sublineMobile: string | null
}

export function pricingPlanPrice(
  card: PricingPlanCard,
  cadence: BillingCadence,
  prices: PublicPlanPrices,
): PricingPlanPriceView {
  if (card.id === "Pilot") {
    return {
      amount: card.monthlyPrice ?? null,
      suffix: "/ for 30 days",
      subline: card.monthlySubline,
      sublineMobile: "No payment card required. No automatic paid renewal.",
    }
  }
  const planPrice = prices[card.id]
  if (cadence === "annual") {
    return {
      amount: planPrice?.annual ?? null,
      suffix: "/ year",
      subline: card.annualSubline,
      sublineMobile: card.annualSubline,
    }
  }
  return {
    amount: planPrice?.monthly ?? null,
    suffix: "/ month",
    subline: card.monthlySubline,
    sublineMobile:
      planPrice != null ? `${planPrice.annual}/year` : null,
  }
}

export function pricingCompareAnnualSubline(
  planId: PricingPlanId,
  prices: PublicPlanPrices,
): string | null {
  if (planId === "Pilot") {
    return null
  }
  const annual = prices[planId]?.annual
  return annual != null ? `${annual}/year` : null
}

export { MANAGE_PLAN_IDS as PRICING_PLAN_IDS }
