import { HELP_CENTRE_CONTACT_URL } from "@/config/support"
import { MARKETING_FAQS_PATH } from "@/constants/marketingNav"
import { CUSTOM_PLAN_QUOTE_COPY } from "@/content/marketing/customPlanQuoteCopy"

/** Figma Custom Plan hero `6738:14851`. */
export const CUSTOM_PLAN_HERO = {
  eyebrow: "Build your own plan",
  title: "A Tummly plan built around your restaurants.",
  body: "Choose your Locations, adjust your team and QR capacity, and add the Email, SMS or AI allowances you need. See what's included and how your subscription price is calculated.",
} as const

export const CUSTOM_PLAN_SECTION_INSET = "px-5 lg:px-[60px]"

export const CUSTOM_PLAN_SECTION_Y = "py-12.5 lg:py-[70px]"

export const CUSTOM_PLAN_SECTION_HEADING =
  "font-jakarta text-[34px] font-[500] leading-none text-black lg:text-[46px]"

export const CUSTOM_PLAN_STEPS = {
  locations: {
    step: "01",
    title: "How many Locations do you have?",
    body: "Choose the number of restaurants you'd like to manage with Tummly.",
    locationLabel: "Restaurant Locations",
  },
  capacity: {
    step: "2",
    title: "Customise your capacity",
    includedTitle: "Included allowances",
    includedBody:
      "Add only what you need beyond the allowances in your starting package.",
    /** Figma lock pill `6661:39157` — shown while Locations = 0 (doc 08). */
    lockHint: CUSTOM_PLAN_QUOTE_COPY.locationEmpty,
    teamUsersTitle: "Additional team users",
    offersTitle: "Additional active Offers",
    qrTitle: "Additional QR placements",
    qrBody:
      "Need more QR placements than your plan includes? Add extra capacity now and assign it to your restaurants after signup.",
    qrPackTitle: "Extra QR placements",
    /** Cadence-neutral: the amount's suffix carries the period. */
    qrExtraCostLabel: "Additional cost",
    qrFootnote:
      "Your plan's included QR placements are already covered. You can allocate purchased extra capacity to your Locations after signup.",
  },
  credits: {
    step: "3",
    title: "Add monthly credits",
    body: "Select recurring allowances for Email, SMS and AI. One-off top-ups are purchased separately.",
    channels: [
      {
        channel: "email" as const,
        title: "Email credits",
        tip: "1 credit = 1 email recipient",
      },
      {
        channel: "sms" as const,
        title: "SMS credits",
        tip: "1 credit = 1 SMS segment",
      },
      {
        channel: "ai" as const,
        title: "AI actions",
        tip: "1 action = 1 successful AI result",
      },
    ],
    packPriceSuffix: "/month per pack",
  },
  estimate: {
    heading: "YOUR ESTIMATED PLAN",
    recommendedBadge: "Recommended",
    planName: "Custom",
    planDescription:
      "Build à-la-carte Locations, capacity and recurring credits around your restaurants.",
    locationsLabel: "Locations",
    teamUsersLabel: "Team users",
    activeQrLabel: "Active QR",
    activeQrSuffix: " total",
    activeOffersLabel: "Active Offers",
    activeOffersSuffix: " account-wide",
    emailLabel: "Email",
    smsLabel: "SMS",
    aiLabel: "AI",
    perMonthSuffix: "/mo",
    estimatedTotalLabel: "Estimated total",
    monthlySuffix: "/ per month",
    annualSuffix: "/ per year",
    continueLabel: "Continue with Custom",
    contactLabel: "Contact us",
    contactTo: HELP_CENTRE_CONTACT_URL,
    checkoutNote:
      "No payment is taken until your subscription details are confirmed at checkout.",
    emptyIllustrationAlt:
      "Illustration of green boots climbing white circular steps",
  },
} as const

export type CustomPlanFaqItem = {
  id: string
  question: string
  answer: string
}

/** Figma Custom Plan FAQs `6661:39170`. */
export const CUSTOM_PLAN_FAQS = {
  title: "Frequently asked questions",
  moreTitle: "Have another question?",
  moreBody:
    "Find more answers about Tummly, pricing, QR materials and Guest Loop.",
  moreCtaLabel: "Explore all FAQs",
  moreCtaTo: MARKETING_FAQS_PATH,
  items: [
    {
      id: "add-offers",
      question: "Can I add more Offers without upgrading my plan?",
      answer:
        "Yes. You can add extra active Offers individually without moving to a higher plan. You can also add team members and QR placements as needed.",
    },
    {
      id: "included-allowances",
      question: "Will I pay again for what's already included in my plan?",
      answer:
        "No. Your plan's included allowances are counted first. You only pay for the extras you choose to add.",
    },
    {
      id: "fewer-credits",
      question: "What happens if I don't use all my included credits?",
      answer:
        "You can use as many as you need within your allowance. Unused monthly credits don't carry over or reduce your subscription price.",
    },
    {
      id: "add-locations-later",
      question: "Can I add more restaurants later?",
      answer:
        "Yes. You can add more restaurants as your business grows, up to 500 Locations. You'll see the updated subscription price before confirming.",
    },
    {
      id: "recurring-vs-topups",
      question:
        "What's the difference between monthly credits and one-off top-ups?",
      answer:
        "Monthly credits refresh each month, even with annual billing. One-off top-ups are purchased separately, remain valid for 12 months and are used after your included credits. Top-ups are available on paid plans only.",
    },
    {
      id: "change-after-subscribe",
      question: "Can I change my custom plan after subscribing?",
      answer:
        "Yes. You can add more restaurants, team members, Offers or QR placements during your subscription, with any additional cost shown before confirmation. Reductions and billing-period changes normally take effect at your next renewal.",
    },
  ] satisfies CustomPlanFaqItem[],
} as const
