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

/** Figma For Groups hero `5661:20525`. Hero visual is video on Blob. */
export const FOR_GROUPS_HERO = {
  eyebrow: "FOR RESTAURANT GROUPS",
  title: "One Guest Loop across every Location.",
  body: "Bring your restaurants together with Tummly. Understand guest experiences across Locations, build stronger guest relationships and create more opportunities for repeat visits, with central visibility and local control.",
  primaryLabel: "Get started",
  emailPlaceholder: "Work email",
  planIntent: { plan: "Pilot" as const },
} as const

export type ForGroupsHeroTrustBadgeId =
  | "iso-27001"
  | "cyber-essentials-plus"
  | "ico-dpe"
  | "ico-data"
  | "cyber-essentials"
  | "iasme-level-1"
  | "good-business-charter"
  | "iso-27701"

export type ForGroupsHeroTrustBadge = {
  id: ForGroupsHeroTrustBadgeId
  label: string
  /** Media id under `for-groups/trust-*` in marketingMediaMeta. */
  mediaId: `for-groups/trust-${ForGroupsHeroTrustBadgeId}`
}

/** Trust strip under the hero editorial (Figma `6424:16986`). */
export const FOR_GROUPS_HERO_TRUST = {
  title: "Built with trust at the heart of Tummly.",
  badges: [
    {
      id: "iso-27001",
      label: "ISO/IEC 27001",
      mediaId: "for-groups/trust-iso-27001",
    },
    {
      id: "cyber-essentials-plus",
      label: "Cyber Essentials Plus",
      mediaId: "for-groups/trust-cyber-essentials-plus",
    },
    {
      id: "ico-dpe",
      label: "ICO Data Protection Essentials",
      mediaId: "for-groups/trust-ico-dpe",
    },
    {
      id: "ico-data",
      label: "ICO Data Protection Registration",
      mediaId: "for-groups/trust-ico-data",
    },
    {
      id: "cyber-essentials",
      label: "Cyber Essentials",
      mediaId: "for-groups/trust-cyber-essentials",
    },
    {
      id: "iasme-level-1",
      label: "IASME Cyber Assurance Level One",
      mediaId: "for-groups/trust-iasme-level-1",
    },
    {
      id: "good-business-charter",
      label: "Good Business Charter",
      mediaId: "for-groups/trust-good-business-charter",
    },
    {
      id: "iso-27701",
      label: "ISO/IEC 27701:2025",
      mediaId: "for-groups/trust-iso-27701",
    },
  ] satisfies ForGroupsHeroTrustBadge[],
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
}

/**
 * Figma For Groups Product `6405:36122`.
 * Four equal cards with Blob product-loop videos + centred CTA.
 */
export const FOR_GROUPS_PRODUCT = {
  eyebrow: "ONE CONNECTED GUEST LOOP",
  title: "Everything your restaurants need, working together.",
  body: "From private Feedback to relevant guest engagement, Tummly connects the everyday interactions that help your restaurants build stronger relationships.",
  ctaLabel: "Explore product features",
  ctaTo: MARKETING_PRODUCT_PATH,
  /** Fallback fill under looping card videos. */
  cardBackground: "#e1dfdb",
  cards: [
    {
      id: "hear-from-guests",
      title: "Hear directly from guests",
      body: "Collect private Feedback at every Location and understand what matters to your guests.",
    },
    {
      id: "guest-relationships",
      title: "Build lasting guest relationships",
      body: "Grow guest lists through everyday interactions and stay connected with guests who choose to hear from you.",
    },
    {
      id: "reasons-to-return",
      title: "Give guests reasons to return",
      body: "Create relevant Offers and Campaigns for eligible guests across your authorised Locations.",
    },
    {
      id: "whats-working",
      title: "Understand what's working",
      body: "Review Feedback, guest engagement and Offer performance across your restaurants.",
    },
  ] satisfies ForGroupsProductCard[],
} as const

/** Figma For Groups Group-wide visibility `5661:20730` — media left. */
export const FOR_GROUPS_REPORTING = {
  eyebrow: "GROUP-WIDE VISIBILITY",
  title: "See the whole group. Then look closer.",
  body: "Understand what's happening across your restaurants. Compare private Feedback, QR activity and Campaign and Offer engagement, then explore individual Locations to see where attention may be needed.",
  points: [
    {
      id: "compare-activity",
      title: "Compare restaurant activity",
      body: "Review supported Guest Loop results across authorised Locations.",
    },
    {
      id: "recurring-concerns",
      title: "Identify recurring concerns",
      body: "Understand where similar guest issues are appearing across your group.",
    },
    {
      id: "guest-engagement",
      title: "Review guest engagement",
      body: "See how guests respond to Campaigns and Offers, including recorded Claims and Redemptions.",
    },
  ] satisfies ProductFeaturePoint[],
  imageAlt:
    "Group reporting dashboard showing guest capture, Feedback and Offer metrics across Locations",
} as const

/** Figma For Groups Location management `5661:21077` — copy left, media right. */
export const FOR_GROUPS_LOCATION_MANAGEMENT = {
  eyebrow: "LOCATION MANAGEMENT",
  title: "Add new Locations. Keep everything connected.",
  body: "Bring new restaurants into your existing Tummly group. Set up each Location with its own Guest Form and QR placements, assign the right team access and maintain central visibility as your group grows.",
  points: [
    {
      id: "connected-group",
      title: "One connected group",
      body: "Manage your restaurants within one Tummly account, with each Location clearly identified.",
    },
    {
      id: "set-up-restaurant",
      title: "Set up each restaurant",
      body: "Prepare Location-specific Guest Forms, QR placements and the relevant Guest Loop settings.",
    },
    {
      id: "expand-control",
      title: "Expand with control",
      body: "Add Locations within your plan's capacity or arrange additional Location access as your group expands.",
    },
  ] satisfies ProductFeaturePoint[],
  ctaLabel: "Explore QR Materials",
  ctaTo: MARKETING_QR_MATERIALS_PATH,
  imageAlt:
    "Private Feedback themes across Locations with a theme breakdown card for Service speed",
} as const

/** Figma For Groups Team Access `5661:21424` — media left. */
export const FOR_GROUPS_TEAMS = {
  eyebrow: "TEAM ACCESS",
  title: "Give each person the right level of access.",
  body: "Keep your teams focused on the restaurants they manage. Assign permissions by role and Location, so authorised group users have wider visibility while local managers work within their assigned Locations.",
  points: [
    {
      id: "group-owners",
      title: "Group owners and administrators",
      body: "Oversee the group and manage authorised administrative settings.",
    },
    {
      id: "area-managers",
      title: "Area and regional managers",
      body: "Review the Locations assigned to them and work within their approved responsibilities.",
    },
    {
      id: "location-managers",
      title: "Location managers",
      body: "Focus on their restaurant's Feedback, guest experience and permitted actions without unnecessary access to other Locations.",
    },
  ] satisfies ProductFeaturePoint[],
  imageAlt:
    "Team access workspace showing Location-scoped roles for a restaurant group.",
} as const

/** Figma For Groups Offers & Campaigns `5661:21775` — copy left, media right. */
export const FOR_GROUPS_CAMPAIGNS = {
  eyebrow: "OFFERS & CAMPAIGNS",
  title: "Give guests more reasons to return across your group.",
  body: "Stay connected with guests who have chosen to hear from your restaurants. Create relevant Offers and coordinate Campaigns across authorised Locations to encourage repeat orders and visits.",
  points: [
    {
      id: "thank-recent",
      title: "Thank recent guests",
      body: "Keep your restaurant in mind after a positive visit with a relevant follow-up.",
    },
    {
      id: "promote-new",
      title: "Promote something new",
      body: "Share new menu items, restaurant updates or special occasions with eligible guests.",
    },
    {
      id: "understand-response",
      title: "Understand the response",
      body: "Review Campaign engagement and recorded Offer activity by the appropriate Location and scope.",
    },
  ] satisfies ProductFeaturePoint[],
  imageAlt:
    "Group Campaign draft showing send-from-each-Location controls and local send times.",
} as const

/** Figma For Groups AI Assistant `5661:22127` — media left. */
export const FOR_GROUPS_AI = {
  eyebrow: "AI ASSISTANT",
  title: "Ask about one Location. Or your whole group.",
  body: "Use Tummly's AI Assistant to understand guest experiences across your authorised Locations. Summarise Feedback, identify recurring concerns and explore Campaign or Offer performance using available Guest Loop data.",
  points: [
    {
      id: "understand-feedback",
      title: "Understand guest Feedback",
      body: "Get useful summaries of what guests are saying across the selected period.",
    },
    {
      id: "compare-locations",
      title: "Compare your Locations",
      body: "Explore supported differences between restaurants within your permitted group scope.",
    },
    {
      id: "prepare-campaigns",
      title: "Prepare relevant Campaigns",
      body: "Use AI to help draft Campaigns and Offers, with your team reviewing and confirming actions before execution.",
    },
  ] satisfies ProductFeaturePoint[],
  ctaLabel: "Explore Tummly AI",
  ctaTo: MARKETING_AI_ASSISTANT_PATH,
  imageAlt:
    "Tummly AI Assistant greeting with group-scoped prompts for restaurant operators.",
} as const

/**
 * Figma For Groups pricing `5661:22463`.
 * Plan column + Locations calculator; feature rows match Pricing Group card.
 */
export const FOR_GROUPS_PRICING = {
  eyebrow: "GROUP PRICING",
  title: "Group pricing that grows with your Locations.",
  body: "Bring your restaurants together with group reporting, team access and coordinated guest engagement. Choose a plan that fits your current Locations and expand as your business grows.",
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
    { label: "Active QR placements", value: "Up to 50 per Location" },
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
    body: "Group includes five Locations. From six, add Locations at progressive rates up to 500 online. Adjust users, active offers, QR capacity and recurring credits separately, then review your total.",
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

/** Figma For Groups bottom Sign up `6405:36178` — editorial neon marble. */
export const FOR_GROUPS_SIGN_UP_CTA: MarketingSignUpCtaContent = {
  title: "Bring Tummly to your restaurant group.",
  body: "Give each restaurant the tools to hear from guests, build stronger relationships and encourage repeat orders, with central visibility across your Locations.",
  primaryLabel: "Get started",
  planIntent: { plan: "Group", cadence: "monthly" },
}
