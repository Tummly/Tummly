import type { MarketingSignUpCtaContent } from "@/components/marketing/MarketingSignUpCta"
import { HELP_CENTRE_CONTACT_URL } from "@/config/support"
import {
  MARKETING_AI_ASSISTANT_PATH,
  MARKETING_FAQS_PATH,
  MARKETING_PRICING_PATH,
  MARKETING_PRODUCT_PATH,
} from "@/constants/marketingNav"
import type { ProductFeaturePoint } from "@/content/marketing/productPage"
import type { PricingFeatureRow } from "@/content/marketing/pricingPage"
import type { ForGroupsProductVideoKey } from "@/lib/marketingMedia"

/** Shared section inset — matches How it works / Product (20px mobile, 60px desktop). */
export const FOR_GROUPS_SECTION_INSET = "px-5 lg:px-[60px]"

/** Shared vertical pad — 50px mobile, 70px desktop. Figma hero `5661:20525`. */
export const FOR_GROUPS_SECTION_Y = "py-12.5 lg:py-[70px]"

export const FOR_GROUPS_SECTION_BODY =
  "font-sans text-base font-normal leading-[22px] text-[#141414] lg:text-[18px] lg:leading-6"

export const FOR_GROUPS_SECTION_HEADING =
  "font-jakarta text-[28px] font-medium leading-9 text-[#141414] lg:text-[46px] lg:leading-[52px]"

/** Figma For Groups hero `5661:20525`. */
export const FOR_GROUPS_HERO = {
  eyebrow: "FOR RESTAURANT GROUPS",
  title: "One Guest Loop across every Location.",
  body: "Give each restaurant its own Guest Loop while keeping your central team connected. Understand guest Feedback, build stronger guest relationships and oversee Offers, Campaigns and performance across your Locations, with access tailored to each team.",
  trustBadge: "No payment card required",
  primaryLabel: "Get started",
  emailPlaceholder: "Work email",
  planIntent: { plan: "Pilot" as const },
} as const

export type ForGroupsProductSlide = {
  id: ForGroupsProductVideoKey
  title: string
  body: string
}

/**
 * Figma For Groups Product carousel `6320:19475` / Frame 5564 cards.
 * Slide order matches the Figma track.
 */
export const FOR_GROUPS_PRODUCT = {
  eyebrow: "PRODUCT",
  title: "Everything you need to run the Guest Loop.",
  ctaLabel: "Explore product features",
  ctaTo: MARKETING_PRODUCT_PATH,
  /** Card chrome under video/image (Figma solid fill on Feedback). */
  cardBackground: "#008843",
  slides: [
    {
      id: "capture",
      title: "Capture",
      body: "Manage QR placements, Smart Guest Links, Guest Forms and source tracking.",
    },
    {
      id: "feedback",
      title: "Feedback",
      body: "See private Feedback, identify what needs attention and manage recovery.",
    },
    {
      id: "ai-assistant",
      title: "AI Assistant",
      body: "Ask what is happening and prepare governed Drafts for your review.",
    },
    {
      id: "guests",
      title: "Guests",
      body: "Understand guest history, contact details and permission status.",
    },
    {
      id: "offers",
      title: "Offers",
      body: "Create controlled Offers with clear validity, expiry and Redemption.",
    },
    {
      id: "campaigns",
      title: "Campaigns",
      body: "Send goal-led Email and SMS Campaigns to eligible guests.",
    },
    {
      id: "reports",
      title: "Reports",
      body: "Understand Capture, Feedback, Campaign and Offer performance with clear reporting.",
    },
    {
      id: "weekly-brief",
      title: "Weekly Brief",
      body: "See what changed, what needs attention and where to look next.",
    },
  ] satisfies ForGroupsProductSlide[],
} as const

export type ForGroupsBuiltForCard = {
  id: "location-context" | "group-view" | "access"
  title: string
  body: string
}

/** Figma For Groups “Built for restaurant groups” `5661:20535`. */
export const FOR_GROUPS_BUILT_FOR = {
  eyebrow: "BUILT FOR RESTAURANT GROUPS",
  title: "Every Location stays connected. Your group stays in control.",
  body: "Give each restaurant its own Guest Loop while your central team can understand performance, oversee guest relationships and manage access across your Locations.",
  cards: [
    {
      id: "location-context",
      title: "Each Location keeps its context",
      body: "Feedback, QR placements, Guest Forms, Offers and activity stay attributable to the correct Location.",
    },
    {
      id: "group-view",
      title: "Your group gets the wider view",
      body: "Compare Locations, identify what needs attention and drill into the underlying restaurant.",
    },
    {
      id: "access",
      title: "Access follows responsibility",
      body: "Give owners, area managers and Location teams only the scope they need.",
    },
  ] satisfies ForGroupsBuiltForCard[],
} as const

/**
 * Figma For Groups QR materials `5661:20711`.
 * Same Capture materials panel as Home / Product; intro copy is group-specific.
 */
export const FOR_GROUPS_QR_MATERIALS = {
  eyebrow: "QR MATERIALS",
  title: "Bring Guest Loop into every restaurant.",
  body: "Give each Location its own Guest Loop deployment while keeping every QR placement tied to the correct restaurant.",
} as const

/** Figma For Groups Group Reporting `5661:20730` — media left. */
export const FOR_GROUPS_REPORTING = {
  eyebrow: "GROUP REPORTING",
  title: "See the whole group.\nThen look closer.",
  body: "Start with a consolidated view across authorised Locations, then drill into the restaurant, period or source behind the change.",
  points: [
    {
      id: "across-locations",
      title: "Across Locations",
      body: "Compare authorised Locations using the same metric definitions and period.",
    },
    {
      id: "by-location",
      title: "By Location",
      body: "Drill into a restaurant without losing the context behind the change.",
    },
    {
      id: "over-time",
      title: "Over time",
      body: "Compare periods with clear scope, timezone and freshness.",
    },
  ] satisfies ProductFeaturePoint[],
  imageAlt: "",
} as const

/** Figma For Groups Feedback across Locations `5661:21077`. */
export const FOR_GROUPS_FEEDBACK = {
  eyebrow: "FEEDBACK ACROSS LOCATIONS",
  title: "Understand what guests are telling you across Locations.",
  body: "Compare private Feedback themes and workflow status across the group, then open the underlying Location or Feedback item when something needs attention.",
  points: [
    {
      id: "compare-locations",
      title: "Compare Locations",
      body: "See Feedback volume, recurring themes and Needs attention by Location.",
    },
    {
      id: "keep-local-context",
      title: "Keep local context",
      body: "Every Feedback item stays connected to the Location and source it came from.",
    },
    {
      id: "follow-the-workflow",
      title: "Follow the workflow",
      body: "Move from New and Needs attention through In progress to Resolved.",
    },
  ] satisfies ProductFeaturePoint[],
  imageAlt: "",
} as const

/** Figma For Groups Teams & Permissions `5661:21424` — media left. */
export const FOR_GROUPS_TEAMS = {
  eyebrow: "TEAMS & PERMISSIONS",
  title: "Give each person the right level of access.",
  body: "Central teams can oversee the group while Location teams work within the restaurants they are responsible for.",
  points: [
    {
      id: "owner-group-admin",
      title: "Owner / Group Admin",
      body: "Group-wide authorised view and account controls",
    },
    {
      id: "area-manager",
      title: "Area Manager",
      body: "Selected Location scope and relevant operational access",
    },
    {
      id: "location-manager",
      title: "Location Manager",
      body: "Own Location and permitted day-to-day actions",
    },
    {
      id: "reporting-user",
      title: "Reporting user",
      body: "Read-only or limited authorised reporting",
    },
  ] satisfies ProductFeaturePoint[],
  imageAlt:
    "Team and permissions workspace showing Location-scoped access controls.",
} as const

/** Figma For Groups Group Campaigns `5661:21775`. */
export const FOR_GROUPS_CAMPAIGNS = {
  eyebrow: "GROUP CAMPAIGNS",
  title: "Run Campaigns centrally without losing Location relevance.",
  body: "Choose the authorised Location scope, build the eligible audience and prepare the Campaign in one controlled workflow.",
  points: [
    {
      id: "location-scope",
      title: "Location scope",
      body: "Choose the restaurants included in the Campaign.",
    },
    {
      id: "eligible-audience",
      title: "Eligible audience",
      body: "Contactability and eligibility are revalidated before sending.",
    },
    {
      id: "optional-offer",
      title: "Optional Offer",
      body: "Use a governed Offer that is valid for the selected Locations.",
    },
    {
      id: "final-review",
      title: "Final review",
      body: "Confirm recipients, exclusions, usage and schedule before send.",
    },
  ] satisfies ProductFeaturePoint[],
  imageAlt:
    "Group Campaign workflow showing Location scope and audience controls.",
} as const

/** Figma For Groups Tummly AI `5661:22127` — media left, CTA only. */
export const FOR_GROUPS_AI = {
  eyebrow: "TUMMLY AI",
  title: "Ask about one Location.\nOr ask about the whole group.",
  body: "Tummly AI works within the group and Location scope you are authorised to access, explains what changed and prepares governed next steps for review.",
  ctaLabel: "Explore Tummly AI",
  ctaTo: MARKETING_AI_ASSISTANT_PATH,
  imageAlt:
    "Tummly AI assistant answering a group-scoped question for restaurant operators.",
} as const

/**
 * Figma For Groups pricing `5661:22463`.
 * Feature rows follow the Pricing Group card, with For Groups label wording.
 */
export const FOR_GROUPS_PRICING = {
  title: "Group pricing that grows with your Locations.",
  body: "Group includes up to 5 Locations. Add more Locations at the current per-Location rate, with self-service setup supported up to 30 Locations.",
  planName: "Group",
  planDescription:
    "For growing restaurant groups that need more Locations, control and consolidated reporting.",
  priceAmount: "£199",
  priceSuffix: "/ month",
  priceSubline: "£2,030/year",
  ctaLabel: "Choose Group",
  compareLabel: "Compare all plans",
  compareTo: MARKETING_PRICING_PATH,
  coreFeatures: [
    { label: "Locations", value: "Up to 5" },
    { label: "Team users", value: "25" },
    { label: "Guest Forms", value: "Included" },
    { label: "Active QR placements", value: "Up to 50 per Location" },
    { label: "Private Feedback & recovery", value: "Included" },
    { label: "Guest list & permissions", value: "Included" },
    { label: "Campaigns", value: "Included" },
    { label: "Active Offers", value: "Up to 25 account-wide" },
    { label: "Consolidated reporting", value: "Included" },
    { label: "Location-scoped permissions", value: "Included" },
  ] satisfies PricingFeatureRow[],
  allowanceFeatures: [
    { label: "Email sends", value: "25,000/month", icon: "email" },
    { label: "SMS credits", value: "700/month", icon: "sms" },
    { label: "AI actions", value: "1,500/month", icon: "ai" },
    { label: "Weekly Brief", value: "Location + consolidated" },
    { label: "Top-ups", value: "Available" },
    { label: "Support", value: "Priority + assisted onboarding" },
  ] satisfies PricingFeatureRow[],
  growthPanel: {
    title: "Adding more than 5 Locations?",
    body: "Add Locations as your group grows. Self-service setup is supported up to 30 Locations at the current per-Location rate.",
    primaryLabel: "Set up your group",
    secondaryLabel: "Planning 31+ Locations?",
    secondaryTo: HELP_CENTRE_CONTACT_URL,
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
      id: "locations-included",
      question: "How many Locations are included in Group?",
      answer:
        "Group includes up to 5 Locations. Additional Locations can be added at the current per-Location rate, with self-service Group setup supported up to 30 Locations.",
    },
    {
      id: "add-locations",
      question: "Can I add more Locations later?",
      answer:
        "Yes. You can add Locations as the group grows using the same governed setup flow rather than creating a new account.",
    },
    {
      id: "same-guest-form",
      question: "Do all Locations use the same Guest Form?",
      answer:
        "No. Each Location can keep the Guest Form and QR placement context it needs while group reporting consolidates activity without losing the underlying Location.",
    },
    {
      id: "head-office-access",
      question: "Can Head Office see every Location?",
      answer:
        "Authorised group users can access the Locations and consolidated views their role permits. Location teams do not automatically receive group-wide access.",
    },
    {
      id: "area-managers",
      question: "Can area managers be limited to selected Locations?",
      answer:
        "Yes, where the configured role model permits. Access can be scoped to the relevant authorised Locations rather than exposing the whole group.",
    },
    {
      id: "compare-locations",
      question: "Can I compare Locations?",
      answer:
        "Yes. Group reporting can compare authorised Locations using consistent metric definitions and period context, then drill into the underlying Location.",
    },
    {
      id: "multi-location-campaign",
      question: "Can we run a Campaign across several Locations?",
      answer:
        "Yes. Group Campaigns can be prepared for an authorised Location scope. Audience eligibility and contactability are revalidated before controlled sending.",
    },
    {
      id: "ai-across-group",
      question: "Can Tummly AI work across the whole group?",
      answer:
        "Tummly AI can work within the group and Location scope the signed-in user is authorised to access, explain relevant changes and prepare governed Drafts for review.",
    },
    {
      id: "over-30-locations",
      question: "Can I set up more than 30 Locations online?",
      answer:
        "The current self-service Group limit is 30 Locations. For more than 30, use the assisted rollout route so Tummly can assess the required operating model and current product fit.",
    },
  ] satisfies ForGroupsFaqItem[],
} as const

/** Figma For Groups Sign up `5661:22602` — media (video) variant. */
export const FOR_GROUPS_SIGN_UP_CTA: MarketingSignUpCtaContent = {
  title: "Ready to see Guest Loop in a real restaurant?",
  body: "Start with one Location and see how Guest Loop works with real guests.",
  primaryLabel: "Get started",
  badge: "No payment card required",
  planIntent: { plan: "Pilot" },
}
