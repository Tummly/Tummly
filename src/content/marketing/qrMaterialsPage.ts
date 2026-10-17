import type { MarketingSignUpCtaContent } from "@/components/marketing/MarketingSignUpCta"
import {
  MARKETING_FOR_GROUPS_PATH,
  MARKETING_HOW_IT_WORKS_PATH,
  MARKETING_PRODUCT_PATH,
} from "@/constants/marketingNav"

/** Shared section inset — matches Product / How it works (20px mobile, 60px desktop). */
export const QR_MATERIALS_SECTION_INSET = "px-5 lg:px-[60px]"

/** Shared vertical pad — 50px mobile, 70px desktop. Figma hero `5661:19336`. */
export const QR_MATERIALS_SECTION_Y = "py-12.5 lg:py-[70px]"

export const QR_MATERIALS_SECTION_BODY =
  "font-sans text-base font-normal leading-[22px] text-[#141414] lg:text-[18px] lg:leading-6"

/** Figma QR materials hero `5661:19336`. */
export const QR_MATERIALS_HERO = {
  title: "Put Guest Loop where guests already interact.",
  body: "Use restaurant-ready QR materials across counters, tables, receipts, takeaway packaging and other natural guest moments. Each QR placement connects to the right Location and Guest Form while keeping its source context attached.",
  primaryLabel: "Start 30-day Pilot",
  secondaryLabel: "See how it works",
  secondaryTo: MARKETING_HOW_IT_WORKS_PATH,
  note: "No website or in-house printer required. Digital and self-print options are also available.",
  productImageAlt:
    "Hand holding a green Tummly QR guest card with a thank-you offer and scan QR code.",
} as const

/** Figma section heading — 36 / 48 *headline* semibold (`5661:19349`). */
export const QR_MATERIALS_SECTION_HEADING =
  "font-jakarta text-[28px] font-semibold leading-9 text-black lg:text-[36px] lg:leading-[48px]"

export type QrMaterialsTouchpointCard = {
  id: string
  title: string
  body: string
}

/** Figma QR materials touchpoint context `5661:19347`. */
export const QR_MATERIALS_TOUCHPOINTS = {
  title: "Every scan starts with a known restaurant touchpoint.",
  body: "Each Tummly QR placement belongs to a Location, a source and a Guest Form. That keeps the guest experience simple while preserving useful context about where Guest Loop activity came from.",
  cards: [
    {
      id: "location",
      title: "Location context stays attached",
      body: "Know which Location the placement belongs to.",
    },
    {
      id: "source",
      title: "Source context stays attached",
      body: "Distinguish activity from a counter display, table tent, QR guest card or other placement.",
    },
    {
      id: "reorder",
      title: "Reordering keeps continuity",
      body: "Normal material reorders preserve the existing QR placement and its history.",
    },
  ] satisfies QrMaterialsTouchpointCard[],
} as const

/**
 * Figma QR materials catalogue `5661:19364`.
 * Outer intro + reused Capture materials panel + physical/digital band.
 */
export const QR_MATERIALS_CATALOGUE = {
  titleLine1: "Choose the touchpoints",
  titleLine2: "that fit your service.",
  body: "Use different QR formats across the restaurant while keeping each placement connected to the same Guest Loop. Choose the formats that suit how guests order, collect, dine and leave.",
  /** Same panel intro as Home QR materials `4902:19649`. */
  panelEyebrow: "QR MATERIALS",
  panelTitle: "QR materials made for real restaurant moments.",
  panelBody:
    "Choose ready-to-use Tummly QR materials for tables, counters, receipts, takeaway, delivery and other guest touchpoints.",
  deployment: {
    title: "Physical when you need it. Digital when it fits.",
    body: "Guest Loop can be deployed through restaurant-ready physical materials or through available digital and self-print assets. Use the route that fits the touchpoint without making a website or in-house printer a requirement.",
    physical: {
      title: "Restaurant-ready physical materials",
      body: "Use professionally prepared QR materials for counters, tables, receipts, packaging and other suitable restaurant touchpoints.",
      ctaLabel: "Start 30-day Pilot",
    },
    digital: {
      title: "Digital & self-print",
      body: "Need a QR straight away or already have a suitable digital touchpoint? Use your QR image, Smart Guest Link or available self-print assets while physical materials are being prepared.",
      note: "Examples: digital receipt, Email, website/social where available, existing digital display, self-print.",
    },
  },
} as const

export type QrMaterialsConnectedStep = {
  id: string
  stepLabel: string
  title: string
  body: string
}

/**
 * Figma QR materials connected placements `5661:19391`.
 * Two columns: steps 1+3 left, steps 2+4 right.
 */
export const QR_MATERIALS_CONNECTED = {
  eyebrow: "MORE THAN A PRINTED QR",
  title: "More than a printed QR. Each placement stays connected to Guest Loop.",
  body: "Every QR placement is configured as part of the Tummly product, not treated as an anonymous printed code.",
  /** Left column then right — render order preserved for a11y list. */
  columns: [
    [
      {
        id: "choose-moment",
        stepLabel: "Step 1",
        title: "Choose the moment",
        body: "Decide where the prompt belongs — for example at the counter, on a table, with a receipt or inside an order.",
      },
      {
        id: "put-live",
        stepLabel: "Step 3",
        title: "Put it live",
        body: "Deploy the physical material or use an available digital/self-print asset.",
      },
    ],
    [
      {
        id: "connect-form",
        stepLabel: "Step 2",
        title: "Connect the Guest Form",
        body: "The placement is linked to the relevant Location and Published Guest Form.",
      },
      {
        id: "see-engagement",
        stepLabel: "Step 4",
        title: "See where engagement comes from",
        body: "Tummly keeps QR placement context attached so the operator can understand where Guest Loop activity is happening.",
      },
    ],
  ] satisfies QrMaterialsConnectedStep[][],
} as const

export type QrMaterialsLoopStage = {
  id: string
  title: string
  body: string
  /** First stage uses mint surface (`#eef8f1`); others use `#f7f7f4`. */
  highlighted?: boolean
}

/**
 * Figma QR materials post-scan Guest Loop `5661:19419`.
 */
export const QR_MATERIALS_STARTING_POINT = {
  eyebrow: "THE QR IS ONLY THE STARTING POINT",
  title: "The QR is only the starting point.",
  body: "A scan opens the restaurant’s Guest Form. From there, a guest can share private Feedback, optionally add their details and use the choices presented in the live Guest Form. Tummly keeps the relevant Location and placement context attached so the operator can understand and act appropriately.",
  ctaLabel: "Explore the Guest Loop",
  ctaTo: MARKETING_PRODUCT_PATH,
  stages: [
    {
      id: "scan",
      title: "Scan",
      body: "Guest opens the relevant Guest Form.",
      highlighted: true,
    },
    {
      id: "feedback",
      title: "Private Feedback",
      body: "Guest shares their experience in their own words.",
    },
    {
      id: "details",
      title: "Optional guest details",
      body: "Where provided, contact context can become part of the guest relationship.",
    },
    {
      id: "offer",
      title: "Thank-you / Offer",
      body: "A governed Offer can be shown where configured.",
    },
    {
      id: "follow-up",
      title: "Follow-up and insight",
      body: "The operator can review Feedback, understand context and take the next appropriate action.",
    },
  ] satisfies QrMaterialsLoopStage[],
} as const

/**
 * Figma QR materials no-website reassurance `5661:19452`.
 */
export const QR_MATERIALS_NO_WEBSITE = {
  title: "No website or in-house printer? You can still get Guest Loop live.",
  body: "Tummly can support restaurant-ready physical QR deployment without requiring a website, social audience or in-house printing setup. Digital and self-print options remain available where they are useful.",
  ctaLabel: "See how Tummly works",
  ctaTo: MARKETING_HOW_IT_WORKS_PATH,
} as const

/**
 * Figma QR materials starter kit CTA `5661:19459`.
 */
export const QR_MATERIALS_STARTER_KIT = {
  eyebrow: "GETTING YOUR FIRST LOCATION LIVE",
  title: "Start with restaurant-ready materials for your first Location.",
  body: [
    "Each qualifying newly activated, verified production Location can receive one complimentary standard Starter Kit once under the qualifying operator/business relationship. Additional and replacement materials are available separately through the Tummly Shop.",
    "Your included kit is prepared after the Location and required setup details are ready.",
  ],
  ctaLabel: "Start 30-day Pilot",
  note: "One qualifying Starter Kit for the activated Pilot Location.",
} as const

export type QrMaterialsPrincipleCard = {
  id: string
  title: string
  body: string
}

/**
 * Figma QR materials guest-experience principles `5661:19470`.
 */
export const QR_MATERIALS_GUEST_EXPERIENCE = {
  eyebrow: "BUILT AROUND THE GUEST EXPERIENCE",
  title: "Designed around the restaurant and the guest experience.",
  cards: [
    {
      id: "brand-leads",
      title: "Restaurant brand leads",
      body: "Guest-facing materials should feel connected to the restaurant, with Tummly supporting the experience rather than overpowering it.",
    },
    {
      id: "private-feedback",
      title: "Private Feedback stays private",
      body: "Guest Loop is designed for private restaurant Feedback and remains separate from public-review activity.",
    },
    {
      id: "simple-experience",
      title: "The guest experience stays simple",
      body: "Each placement should lead clearly into the restaurant’s current Guest Form without unnecessary steps.",
    },
  ] satisfies QrMaterialsPrincipleCard[],
} as const

/**
 * Figma QR materials for-groups CTA `5661:19490`.
 */
export const QR_MATERIALS_FOR_GROUPS = {
  eyebrow: "FOR GROWING RESTAURANT GROUPS",
  title: "Keep every placement tied to the right Location as you grow.",
  body: "Use Guest Loop across multiple Locations while preserving the Location, QR placement and Guest Form context behind each interaction. Group reporting can then compare activity without losing where it came from.",
  ctaLabel: "See Tummly for groups",
  ctaTo: MARKETING_FOR_GROUPS_PATH,
} as const

/** Shared bottom Sign-up CTA (Figma `5661:14510` / `4151:3811`). */
export const QR_MATERIALS_SIGN_UP_CTA: MarketingSignUpCtaContent = {
  title: "Ready to see Guest Loop in a real restaurant?",
  body: "Start with one Location and see how Guest Loop works with real guests.",
  primaryLabel: "Start 30-day Pilot",
  badge: "No payment card required",
  planIntent: { plan: "Pilot" },
}
