import type { MarketingSignUpCtaContent } from "@/components/marketing/MarketingSignUpCta"
import {
  MARKETING_DEMO_PATH,
  MARKETING_FOR_GROUPS_PATH,
  MARKETING_HOW_IT_WORKS_PATH,
  MARKETING_PRODUCT_PATH,
  MARKETING_QR_MATERIALS_PATH,
  MARKETING_TRUST_PRIVACY_PATH,
} from "@/constants/marketingNav"
import { AI_ASSISTANT_PLANS } from "@/content/marketing/aiAssistantPage"

/** Figma Home hero `5433:12691` (Frame 269). */
export const HOME_HERO = {
  groupsPrompt: "Running more than one Location?",
  groupsCtaLabel: "See Tummly for groups",
  groupsCtaTo: MARKETING_FOR_GROUPS_PATH,
  title:
    "Turn everyday orders and visits into guest relationships you can build on.",
  body: "Tummly helps restaurants hear directly from guests, build their own guest list, strengthen relationships and bring more guests back with relevant Offers and Campaigns.",
  trustBadge: "No payment card required",
  primaryLabel: "Start 30-day Pilot",
  emailPlaceholder: "Work email",
  planIntent: { plan: "Pilot" as const },
  visualAlt: "",
} as const

/** Fixed demo QR chrome (Figma `5661:30036` / Scan variants). */
export const HOME_DEMO_QR = {
  ariaLabel: "Scan to try on your phone",
  href: MARKETING_DEMO_PATH,
  /** Expanded Variant3 `5557:7269` — wraps in a 137px column. */
  title: "Scan to try on your phone",
} as const

/** Figma Home value props `4902:19631`. */
export const HOME_VALUE_PROPS = {
  heading: "Built for restaurants that want to own the guest relationship.",
  items: [
    {
      id: "no-website",
      title: "Works without a restaurant website",
      body: "Guests can enter the Guest Loop through Tummly QR touchpoints across dine-in, collection, takeaway and delivery moments.",
    },
    {
      id: "qr-materials",
      title: "Professional QR materials are available",
      body: "Your qualifying Location can receive its included Starter Kit, with additional and replacement QR materials available through the Tummly Shop.",
    },
    {
      id: "setup-now",
      title: "Start setting up immediately",
      body: "Create your Location, publish your Guest Form and prepare QR placements before physical materials arrive.",
    },
    {
      id: "private-feedback",
      title: "Private Feedback stays separate",
      body: "Guests can share Feedback privately with the restaurant. Public-review activity is kept separate.",
    },
  ],
} as const

/** Figma Home How it works intro `4902:19644`. */
export const HOME_HOW_IT_WORKS = {
  eyebrow: "HOW IT WORKS",
  title: "From a quick scan to a stronger guest relationship.",
  body: "Place the Guest Loop where guests already interact. Tummly turns each scan into private Feedback, a growing guest list and more opportunities to bring guests back.",
} as const

export type HomeHowItWorksStep = {
  id: "place" | "hear" | "build" | "bring"
  tabLabel: string
  title: string
  body: string
  ctaLabel: string
  ctaTo: string
  imageAlt: string
}

export type HomeHowItWorksPlaceMedia = {
  id: "counter-card" | "guest-cards" | "window-sticker"
  label: string
  /** marketingMediaMeta / remoteSimpleUrl id — reuse Product Capture assets. */
  mediaId: string
  imageAlt: string
}

/**
 * Place the Guest Loop media stack — Figma `5339:30041`.
 * Reuses Product Capture Blob images; panels expand on label button click.
 */
export const HOME_HOW_IT_WORKS_PLACE_MEDIA = [
  {
    id: "counter-card",
    label: "Counter card",
    mediaId: "product/capture-counter-display",
    imageAlt: "Freestanding QR counter display on a restaurant counter",
  },
  {
    id: "guest-cards",
    label: "QR Guest Cards",
    mediaId: "product/capture-guest-cards",
    imageAlt: "Hand holding a green Tummly QR guest card with an offer",
  },
  {
    id: "window-sticker",
    label: "Window sticker",
    mediaId: "product/capture-window-sticker",
    imageAlt: "Branded QR window sticker on a restaurant window",
  },
] as const satisfies readonly HomeHowItWorksPlaceMedia[]

/**
 * Home How it works steps —
 * Figma `5339:30041`, `4161:19285`, `4161:19284`, `4161:19283`.
 */
export const HOME_HOW_IT_WORKS_STEPS = [
  {
    id: "place",
    tabLabel: "Place the Guest Loop",
    title: "Put Guest Loop where guests already look.",
    body: "Place a Guest Loop QR touchpoint where guests already interact at the counter, table, receipt, takeaway or delivery moment.",
    ctaLabel: "Explore QR Materials",
    ctaTo: MARKETING_QR_MATERIALS_PATH,
    imageAlt:
      "Counter card, QR guest cards and window sticker Guest Loop materials",
  },
  {
    id: "hear",
    tabLabel: "Hear from guests",
    title: "Make private Feedback easy to share.",
    body: "Guests can quickly share private written Feedback about their experience through your Guest Form, in their own words.",
    ctaLabel: "See how it works",
    ctaTo: MARKETING_HOW_IT_WORKS_PATH,
    imageAlt: "Restaurant dining room with guests — Hear from guests video",
  },
  {
    id: "build",
    tabLabel: "Build your guest list",
    title: "Build your own guest list.",
    body: "Guests can choose to share their contact details and opt in to Email or SMS, helping you build stronger guest relationships and encourage repeat visits.",
    ctaLabel: "Explore Product",
    ctaTo: MARKETING_PRODUCT_PATH,
    imageAlt: "Guest profile created with consent confirmed contact preferences",
  },
  {
    id: "bring",
    tabLabel: "Bring guests back",
    title: "Bring guests back with relevant Offers and Campaigns.",
    body: "Reconnect with eligible guests using timely Offers and Campaigns designed to strengthen relationships and encourage repeat visits.",
    ctaLabel: "Explore Product",
    ctaTo: MARKETING_PRODUCT_PATH,
    imageAlt: "Campaign draft ready for review with offer and audience details",
  },
] as const satisfies readonly HomeHowItWorksStep[]

/**
 * Home QR materials — Figma `4902:19649`.
 * Same materials list as Product Capture; intro copy sits inside the panel.
 */
export const HOME_QR_MATERIALS = {
  eyebrow: "QR MATERIALS",
  title: "QR materials made for real restaurant moments.",
  body: "Choose ready-to-use Tummly QR materials for tables, counters, receipts, takeaway, delivery and other guest touchpoints.",
} as const

export type HomeIndustry = {
  id: "takeaway" | "cafes" | "casual" | "groups"
  title: string
  body: string
  imageAlt: string
}

/**
 * Home Built for restaurants —
 * Figma `5088:12319`, `5088:12318`, `5088:12317`, `5088:12316`.
 */
export const HOME_INDUSTRIES = {
  eyebrow: "BUILT FOR RESTAURANTS",
  title: "Built for the way restaurants actually serve guests.",
  body: "From counter service to collection, dine-in and delivery, Tummly fits around the guest touchpoints you already have.",
  items: [
    {
      id: "takeaway",
      title: "Takeaway & QSR",
      body: "Counter, collection, packaging and delivery touchpoints.",
      imageAlt: "Staff using a counter POS in a takeaway restaurant",
    },
    {
      id: "cafes",
      title: "Cafés & bakeries",
      body: "Counters, tables, receipts and takeaway packaging.",
      imageAlt: "Busy café counter with guests and pastry display",
    },
    {
      id: "casual",
      title: "Casual dining",
      body: "Tables, receipts and post-visit Feedback.",
      imageAlt: "Couple dining at a casual restaurant table",
    },
    {
      id: "groups",
      title: "Growing restaurant groups",
      body: "Location-level Guest Loop with group visibility and appropriate access controls.",
      imageAlt: "Server speaking with guests in a busy dining room",
    },
  ],
} as const satisfies {
  eyebrow: string
  title: string
  body: string
  items: readonly HomeIndustry[]
}

export type HomeProductFeature = {
  id:
    | "capture"
    | "feedback"
    | "guests"
    | "offers"
    | "campaigns"
    | "reports"
    | "weekly-brief"
    | "ai-assistant"
  title: string
  body: string
}

/**
 * Home Product features grid — Figma `4902:19791`.
 * Icons: `src/assets/images/marketing/home/product-icons/`.
 */
export const HOME_PRODUCT = {
  eyebrow: "PRODUCT",
  title: "Everything you need to run the Guest Loop.",
  ctaLabel: "Explore product features",
  ctaTo: MARKETING_PRODUCT_PATH,
  features: [
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
    {
      id: "ai-assistant",
      title: "AI Assistant",
      body: "Ask what is happening and prepare governed Drafts for your review.",
    },
  ],
} as const satisfies {
  eyebrow: string
  title: string
  ctaLabel: string
  ctaTo: string
  features: readonly HomeProductFeature[]
}

export type HomeTrustControlItem = {
  id: "private-feedback" | "permissions" | "restaurant-first" | "control"
  title: string
  body: string
}

export type HomeTrustBadge = {
  id:
    | "ico-registered"
    | "data-protection"
    | "cyber-essentials"
    | "cyber-essentials-plus"
    | "iso-27001"
  label: string
}

/**
 * Home Trust & control — Figma `4942:19219` / expanded accordion `5667:26894`.
 */
export const HOME_TRUST_CONTROL = {
  eyebrow: "TRUST & CONTROL",
  title: "Built around guest trust and operator control.",
  ctaLabel: "See Trust & privacy",
  ctaTo: MARKETING_TRUST_PRIVACY_PATH,
  items: [
    {
      id: "private-feedback",
      title: "Private Feedback",
      body: "Guest Feedback stays within the restaurant workflow rather than being treated as a public review.",
    },
    {
      id: "permissions",
      title: "Separate permissions",
      body: "Feedback follow-up, Email marketing and SMS marketing are handled separately.",
    },
    {
      id: "restaurant-first",
      title: "Restaurant-first experience",
      body: "Your restaurant brand leads the Guest Form, with Tummly supporting the experience in the background.",
    },
    {
      id: "control",
      title: "You stay in control",
      body: "Tummly AI can explain activity and prepare Drafts, but consequential communications require operator review.",
    },
  ],
  badges: [
    { id: "ico-registered", label: "ICO Registered" },
    { id: "data-protection", label: "Data Protection Essentials" },
    { id: "cyber-essentials", label: "Cyber Essentials" },
    { id: "cyber-essentials-plus", label: "Cyber Essentials Plus" },
    { id: "iso-27001", label: "ISO/IEC 27001" },
  ],
} as const satisfies {
  eyebrow: string
  title: string
  ctaLabel: string
  ctaTo: string
  items: readonly HomeTrustControlItem[]
  badges: readonly HomeTrustBadge[]
}

/**
 * Home Pricing strip — Figma `4902:19866`.
 * Reuses AI Assistant plan cards; no AI allowance row on Home.
 */
export const HOME_PRICING = {
  eyebrow: "PRICING",
  title: "Start with a 30-day Pilot or choose a paid plan.",
  compareLabel: AI_ASSISTANT_PLANS.compareLabel,
  compareTo: AI_ASSISTANT_PLANS.compareTo,
  cards: AI_ASSISTANT_PLANS.cards,
} as const

/** Shared bottom Sign-up CTA — same Pilot copy as AI Assistant / Pricing. */
export const HOME_SIGN_UP_CTA: MarketingSignUpCtaContent = {
  title: "Ready to see Guest Loop in a real restaurant?",
  body: "Start with one Location and see how Guest Loop works with real guests.",
  primaryLabel: "Start 30-day Pilot",
  badge: "No payment card required",
  planIntent: { plan: "Pilot" },
}
