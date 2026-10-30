import type { MarketingSignUpCtaContent } from "@/components/marketing/MarketingSignUpCta"
import { HELP_CENTRE_CONTACT_URL } from "@/config/support"
import {
  MARKETING_AI_ASSISTANT_PATH,
  MARKETING_FAQS_PATH,
  MARKETING_PRICING_PATH,
  MARKETING_PRODUCT_PATH,
  MARKETING_QR_MATERIALS_PATH,
} from "@/constants/marketingNav"
import type { ProductFeaturePoint } from "@/content/marketing/productPage"
import type { PricingFeatureRow } from "@/content/marketing/pricingPage"
/** Shared section inset — matches How it works / Product (20px mobile, 60px desktop). */
export const FOR_GROUPS_SECTION_INSET = "px-5 lg:px-[60px]"

/** Shared vertical pad — 50px mobile, 70px desktop. Figma hero `5661:20525`. */
export const FOR_GROUPS_SECTION_Y = "py-12.5 lg:py-[70px]"

export const FOR_GROUPS_SECTION_BODY =
  "font-sans text-base font-normal leading-[22px] text-[#141414] lg:text-[18px] lg:leading-6"

export const FOR_GROUPS_SECTION_HEADING =
  "font-jakarta text-[28px] font-medium leading-9 text-[#141414] lg:text-[46px] lg:leading-[52px]"

/** Figma For Groups hero `5661:20525`. Hero photo is a local webp (`for-groups-images.ts`). */
export const FOR_GROUPS_HERO = {
  eyebrow: "Tummly for restaurant groups",
  title: "See where your restaurant group needs attention.",
  body: "Spot recurring service issues without losing sight of the restaurant behind them. Give local managers a focused view of their customers, then review feedback and promotional response together across your group.",
  primaryLabel: "Get started",
  emailPlaceholder: "Work email",
  planIntent: { plan: "Pilot" as const },
  /** Trust strip under the hero visual (Figma `6424:16986`). Badges: `PRODUCT_TRUST.certifications`. */
  trustTitle: "Built with trust at the heart of Tummly.",
} as const

/** Figma For Groups Managing multiple restaurants `5661:20535`. */
export const FOR_GROUPS_MANAGING = {
  eyebrow: "MANAGING MULTIPLE RESTAURANTS",
  title: "A group view with the local detail that matters.",
  body: "Spot patterns in customer feedback across your restaurants, keep local managers focused and expand your group at your own pace.",
  cards: [
    {
      id: "spot-recurring-issues",
      title: "Spot recurring issues",
      body: "Identify common concerns across your restaurants and explore the customer feedback behind them.",
    },
    {
      id: "keep-managers-focused",
      title: "Keep managers focused",
      body: "Give each manager access to the restaurants they're responsible for.",
    },
    {
      id: "grow-at-your-own-pace",
      title: "Grow at your own pace",
      body: "Start with one restaurant, then add more Locations whenever you're ready.",
    },
  ],
} as const

export type ForGroupsProductCardId =
  | "hear-from-guests"
  | "guest-relationships"
  | "reasons-to-return"
  | "whats-working"

export type ForGroupsProductCard = {
  id: ForGroupsProductCardId
  title: string
  body: string
  /** Figma body text box width (px) — sets where the 14/20 copy wraps. */
  bodyWidth: number
}

/**
 * Figma For Groups Keep the Guest Loop working `6405:36122`.
 * Four equal cards with Blob product-loop videos (same scenes as the Figma
 * stills) + centred CTA.
 */
export const FOR_GROUPS_PRODUCT = {
  title: "Keep the Guest Loop working across your restaurants.",
  body: "Bring customer feedback, thoughtful follow-up and relevant offers together, with a clearer view of activity across your group.",
  ctaLabel: "Explore product features",
  ctaTo: MARKETING_PRODUCT_PATH,
  /** Fallback fill under looping card videos. */
  cardBackground: "#e1dfdb",
  cards: [
    {
      id: "hear-from-guests",
      title: "Local feedback",
      body: "Know which restaurant a customer is talking about.",
      bodyWidth: 273,
    },
    {
      id: "guest-relationships",
      title: "Considered follow-up",
      body: "Give the responsible manager the context to reply.",
      bodyWidth: 209,
    },
    {
      id: "reasons-to-return",
      title: "Relevant invitations",
      body: "Prepare offers and updates for the right restaurant audience.",
      bodyWidth: 261,
    },
    {
      id: "whats-working",
      title: "Shared visibility",
      body: "Bring feedback and engagement into a consistent group review.",
      bodyWidth: 273,
    },
  ] satisfies ForGroupsProductCard[],
} as const

/** Figma For Groups Group-wide visibility `5661:20730` — media left. */
export const FOR_GROUPS_REPORTING = {
  eyebrow: "GROUP-WIDE VISIBILITY",
  title: "Know where to look closer.",
  body: "Compare customer feedback, campaign engagement and confirmed offer redemptions across your restaurants. Explore a Location to understand what changed and what the team should discuss.",
  points: [
    {
      id: "compare-restaurants",
      title: "Compare restaurants",
      body: "Review customer feedback and engagement for the same period.",
    },
    {
      id: "look-behind-change",
      title: "Look behind a change",
      body: "Open the local comments and activity.",
    },
    {
      id: "focus-review",
      title: "Focus the review",
      body: "Bring the evidence to the manager responsible.",
    },
  ] satisfies ProductFeaturePoint[],
  imageAlt:
    "Group reporting dashboard showing guest capture, Feedback and Offer metrics across Locations",
} as const

/** Figma For Groups Adding your restaurants `5661:21077` — copy left, media right. */
export const FOR_GROUPS_LOCATION_MANAGEMENT = {
  eyebrow: "ADDING YOUR RESTAURANTS",
  title: "Bring your restaurants\ntogether, one at a time.",
  body: "Set up and verify your first UK restaurant, then add other Locations individually or import a CSV within your paid plan. If the details are not ready, finish later in Settings. Each restaurant completes its own verification and activation.",
  points: [
    {
      id: "start-with-one",
      title: "Start with one restaurant",
      body: "Add your first restaurant's details and get started with Tummly.",
    },
    {
      id: "add-more",
      title: "Add more when you're ready",
      body: "Bring your other restaurants on board now or whenever it suits your team.",
    },
    {
      id: "keep-distinct",
      title: "Keep every restaurant distinct",
      body: "Give customers the right feedback experience for each restaurant.",
    },
  ] satisfies ProductFeaturePoint[],
  ctaLabel: "Explore QR Materials",
  ctaTo: MARKETING_QR_MATERIALS_PATH,
  imageAlt:
    "Private Feedback themes across Locations with a theme breakdown card for Service speed",
} as const

/** Figma For Groups Team access `5661:21424` — media left. */
export const FOR_GROUPS_TEAMS = {
  eyebrow: "TEAM ACCESS",
  title: "Give people access that\nfits their responsibility.",
  body: "Let managers work with their assigned restaurants, while staff check and confirm offer redemptions. Keep guest contact details masked in everyday views.",
  points: [
    {
      id: "assigned-restaurants",
      title: "Assigned restaurants",
      body: "Give each manager access for their responsibility.",
    },
    {
      id: "staff-task",
      title: "A focused staff task",
      body: "Keep offer checking and confirmation easy to reach.",
    },
    {
      id: "restricted-billing",
      title: "Restricted billing",
      body: "Keep subscription and payment controls with authorised people.",
    },
  ] satisfies ProductFeaturePoint[],
  imageAlt:
    "Team access workspace showing Location-scoped roles for a restaurant group.",
} as const

/** Figma For Groups Offers & campaigns `5661:21775` — copy left, media right. */
export const FOR_GROUPS_CAMPAIGNS = {
  eyebrow: "OFFERS & CAMPAIGNS",
  title: "Make the invitation fit the\nrestaurant.",
  body: "Support repeat orders and visits with offers that fit each restaurant. Select customers with the appropriate email or text permission, then review the message, offer terms and timing before sending.",
  points: [
    {
      id: "restaurant-scope",
      title: "Choose the restaurant scope",
      body: "Prepare work for the Locations you manage.",
    },
    {
      id: "match-audience",
      title: "Match the audience",
      body: "Use the appropriate permission for email or text.",
    },
    {
      id: "confirm-details",
      title: "Confirm the details",
      body: "Review wording, offer terms, credits and timing.",
    },
  ] satisfies ProductFeaturePoint[],
  imageAlt:
    "Group Campaign draft showing send-from-each-Location controls and local send times.",
} as const

/** Figma For Groups AI assistant `5661:22127` — media left. */
export const FOR_GROUPS_AI = {
  eyebrow: "AI ASSISTANT",
  title: "Ask a group question. Get\ncloser to the detail.",
  body: "Ask which customer concerns recur across your restaurants, compare a period or prepare a campaign draft. Check the source information and review the work before using it.",
  points: [
    {
      id: "recurring-concern",
      title: "Explore a recurring concern",
      body: "Ask what customers have mentioned across your restaurants.",
    },
    {
      id: "local-detail",
      title: "Check the local detail",
      body: "Review the sources and selected period.",
    },
    {
      id: "prepare-follow-up",
      title: "Prepare the follow-up",
      body: "Create a draft for the responsible team to review.",
    },
  ] satisfies ProductFeaturePoint[],
  ctaLabel: "Explore Tummly AI",
  ctaTo: MARKETING_AI_ASSISTANT_PATH,
  imageAlt:
    "Tummly AI Assistant answering which Feedback keeps coming up across a group's restaurants, with a Campaign draft prompt.",
} as const

/**
 * Figma For Groups pricing `5661:22463`.
 * Plan column + Locations calculator; feature rows match Pricing Group card.
 */
export const FOR_GROUPS_PRICING = {
  eyebrow: "GROUP PRICING",
  title: "Build a plan around your group.",
  body: "Group includes five Locations. From six, add Locations at progressive rates up to 500. Adjust users, active offers, QR capacity and recurring credits separately, then review your total.",
  planName: "Group",
  planDescription:
    "For restaurant groups that need wider oversight, coordinated Campaigns and Location-based team access.",
  includedLocations: 5,
  minLocations: 5,
  maxLocations: 500,
  priceSuffix: "/ month",
  ctaLabel: "Choose Group plan",
  compareLabel: "Compare all plans",
  compareTo: MARKETING_PRICING_PATH,
  coreFeatures: [
    { label: "Locations", value: "Up to 5" },
    { label: "Team users", value: "25" },
    { label: "Guest Form access", value: "Included" },
    { label: "Active QR placements", value: "Up to 50 across your account" },
    { label: "Private Feedback & recovery", value: "Included" },
    { label: "Guest list & consent", value: "Included" },
    { label: "Campaigns", value: "Included" },
    { label: "Active Offers", value: "Up to 25 account-wide" },
    { label: "Multi-Location reporting", value: "Included" },
    { label: "Location-based team access", value: "Included" },
    { label: "Starter Kit", value: "Each qualifying Location" },
  ] satisfies PricingFeatureRow[],
  allowanceFeatures: [
    { label: "Email credits", value: "25,000/month", icon: "email" },
    { label: "SMS credits", value: "700/month", icon: "sms" },
    { label: "AI actions", value: "1,500/month", icon: "ai" },
    { label: "Weekly Brief", value: "Location + consolidated" },
    { label: "Top-ups", value: "Available" },
    { label: "Support", value: "Priority + assisted onboarding" },
  ] satisfies PricingFeatureRow[],
  calculator: {
    eyebrow: "BUILD YOUR GROUP PLAN",
    title: "How many Locations do you have?",
    body: "Choose the number of restaurants you'd like to bring onto Tummly.",
    locationLabel: "Restaurant Locations",
    locationHint: "5 included in Group",
    estimateHeading: "YOUR ESTIMATED PLAN",
    estimatedTotalLabel: "Estimated total",
    continueLabel: "Continue with Group",
    contactLabel: "Contact us",
    contactTo: HELP_CENTRE_CONTACT_URL,
  },
} as const

export type ForGroupsFaqItem = {
  id: string
  question: string
  answer: string
}

/** Figma For Groups FAQs `5661:22571`. */
export const FOR_GROUPS_FAQS = {
  title: "Frequently asked questions",
  moreTitle: "Have another question?",
  moreBody:
    "Find more answers about Tummly, pricing, QR materials and Guest Loop.",
  moreCtaLabel: "Explore all FAQs",
  moreCtaTo: MARKETING_FAQS_PATH,
  items: [
    {
      id: "one-account",
      question: "Can I manage all my restaurants in one account?",
      answer:
        "Yes. Growth includes up to three Locations and Group includes five, with the flexibility to add more as your business grows.",
    },
    {
      id: "manager-access",
      question: "Can managers see only the restaurants they're responsible for?",
      answer:
        "Yes. Each manager's access is based on their role and the restaurants assigned to them.",
    },
    {
      id: "compare-feedback",
      question: "Can I compare customer feedback across my restaurants?",
      answer:
        "Yes. Compare customer feedback, campaign engagement and confirmed offer redemptions across your restaurants, then explore the details for each Location.",
    },
    {
      id: "run-campaigns",
      question: "Can I run campaigns across different restaurants?",
      answer:
        "Yes. Prepare campaigns for the restaurants you manage, reaching customers who have chosen to receive marketing through the relevant channel. Review and confirm each campaign before sending.",
    },
    {
      id: "own-feedback-form",
      question: "Does each restaurant have its own feedback form?",
      answer:
        "Yes. Each restaurant has its own branded feedback form, so you can understand customer experiences at each Location while maintaining a group-wide view.",
    },
    {
      id: "setup-help",
      question: "Can I get help setting up Tummly for my group?",
      answer:
        "Yes. Contact our team to discuss your restaurant group and find the right setup for your needs.",
    },
    {
      id: "customer-concern",
      question: "What happens when a customer raises a concern?",
      answer:
        "Your team can review the feedback, identify what needs attention and prepare a personal response for the customer.",
    },
    {
      id: "offers-per-restaurant",
      question: "Can I create different offers for each restaurant?",
      answer:
        "Yes, you can create offers tailored to each restaurant, set their terms and expiry dates, and see when staff have confirmed they've been used.",
    },
    {
      id: "ai-assistant",
      question: "How can Tummly's AI Assistant help my group?",
      answer:
        "You can ask questions about customer feedback across your restaurants, identify recurring concerns and prepare drafts for your team to review.",
    },
  ] satisfies ForGroupsFaqItem[],
} as const

/** Figma For Groups bottom Sign up `6405:36178` — editorial heat-map plate, "read" legal note. */
export const FOR_GROUPS_SIGN_UP_CTA: MarketingSignUpCtaContent = {
  title: "Make customer experience part of your group routine.",
  body: "Give local managers a clear view of today’s concerns and the next invitation. Build a group plan around the restaurants you run.",
  primaryLabel: "Get started",
  planIntent: { plan: "Group", cadence: "monthly" },
  legalNote: "read",
}
