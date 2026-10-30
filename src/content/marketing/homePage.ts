import type { MarketingSignUpCtaContent } from "@/components/marketing/MarketingSignUpCta"
import {
  MARKETING_HOW_IT_WORKS_PATH,
  MARKETING_PRODUCT_PATH,
  MARKETING_TRUST_PRIVACY_PATH,
} from "@/constants/marketingNav"
import { AI_ASSISTANT_PLANS } from "@/content/marketing/aiAssistantPage"
import { PRICING_PLAN_CARDS } from "@/content/marketing/pricingPage"
import {
  PRODUCT_CAPTURE_MATERIALS,
  type ProductCaptureMaterial,
} from "@/content/marketing/productPage"

/** Figma Home hero `5433:12691` (Frame 269). */
export const HOME_HERO = {
  eyebrow: "Customer feedback and marketing for restaurants",
  title: "Give your customers more reasons to come back.",
  body: "Learn what customers value, respond to service problems and give them a reason to choose you again. Bring private feedback together with relevant offers for customers who choose to hear from you.",
  trustBadge: "No payment card required",
  primaryLabel: "Start 30-day Pilot",
  emailPlaceholder: "Work email",
  planIntent: { plan: "Pilot" as const },
  visualAlt: "",
} as const

/** Figma Home "Built for the way you serve" strip `6757:45771`. */
export const HOME_SERVE_STRIP = {
  eyebrow: "Built for the way you serve",
  /** Chip order follows Figma. Icons live in `HomeServeStrip`. */
  items: [
    { id: "fish-and-chip-shops", label: "Fish-and-chip shops" },
    { id: "kebab-shops", label: "Kebab shops" },
    { id: "sushi-bars", label: "Sushi bars" },
    { id: "noodle-bars", label: "Noodle bars" },
    { id: "burrito-bars", label: "Burrito bars" },
    { id: "dessert-parlours", label: "Dessert parlours" },
    { id: "bubble-tea-shops", label: "Bubble-tea shops" },
    { id: "juice-bars", label: "Juice bars" },
    { id: "casual-dining-restaurants", label: "Casual-dining restaurants" },
  ],
} as const

export type HomePlatformCard = {
  id: "hear-what-matters" | "customer-context" | "reason-to-return" | "learn"
  title: string
  body: string
  /** Max body text width (px). Figma text boxes are 271–323px (DM Sans); Helvetica Neue runs wider, so cards 2–4 use 330. */
  bodyWidth: number
  /** Phone image (card 1) or the For Groups product loop shown behind the card. */
  media: "phone" | "guest-relationships" | "reasons-to-return" | "whats-working"
  mediaAlt: string
}

/** Figma Home The platform `6761:46375` (cards `6761:46432` … `6761:46444`). */
export const HOME_PLATFORM = {
  eyebrow: "THE PLATFORM",
  title: "Make customer feedback useful to your business.",
  body: "Public reviews help people choose where to eat. Tummly helps with the work that follows: hear the customer’s experience, respond and prepare a relevant invitation to return.",
  ctaLabel: "Explore product features",
  ctaTo: MARKETING_PRODUCT_PATH,
  cards: [
    {
      id: "hear-what-matters",
      title: "Hear what matters",
      body: "Find out what customers enjoyed and what needs attention, in their own words.",
      bodyWidth: 271,
      media: "phone",
      mediaAlt: "Private feedback form on a phone",
    },
    {
      id: "customer-context",
      title: "Know the customer context",
      body: "See previous feedback alongside the latest comment, so your response feels considered.",
      bodyWidth: 330,
      media: "guest-relationships",
      mediaAlt: "",
    },
    {
      id: "reason-to-return",
      title: "Create a reason to return",
      body: "Prepare an offer or update for customers who have chosen to hear from your restaurant.",
      bodyWidth: 330,
      media: "reasons-to-return",
      mediaAlt: "",
    },
    {
      id: "learn",
      title: "Learn from the response",
      body: "Review recurring comments, campaign engagement and confirmed offer redemptions to guide your next move.",
      bodyWidth: 330,
      media: "whats-working",
      mediaAlt: "",
    },
  ],
} as const satisfies {
  eyebrow: string
  title: string
  body: string
  ctaLabel: string
  ctaTo: string
  cards: readonly HomePlatformCard[]
}

/** Figma Home How it works intro `5088:12372` (text frame `4902:19644`). */
export const HOME_HOW_IT_WORKS = {
  eyebrow: "HOW IT WORKS",
  title: "A simple scan. A more useful connection.",
} as const

export type HomeHowItWorksStep = {
  id: "connect" | "know" | "invite" | "understand"
  tabLabel: string
  title: string
  body: string
  primaryLabel: string
  secondaryLabel: string
  secondaryTo: string
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
 * Home How it works steps — Figma `5754:22093`; tab states `5339:30041`,
 * `4161:19285`, `4161:19284`, `4161:19283`.
 */
export const HOME_HOW_IT_WORKS_STEPS = [
  {
    id: "connect",
    tabLabel: "Connect with customers",
    title: "Hear how the order or visit went.",
    body: "A scan opens your restaurant’s private feedback form. Customers can type or dictate what they want the team to know.",
    primaryLabel: "Get started",
    secondaryLabel: "See how it works",
    secondaryTo: MARKETING_HOW_IT_WORKS_PATH,
    imageAlt:
      "Café counter with a green Tummly QR stand beside the card reader as a customer pays",
  },
  {
    id: "know",
    tabLabel: "Get to know your guests",
    title: "Give each response the context it deserves.",
    body: "Review earlier feedback securely linked to the guest and see which Location the latest comment concerns. Give your team a better starting point for a thoughtful reply.",
    primaryLabel: "Get started",
    secondaryLabel: "See how it works",
    secondaryTo: MARKETING_HOW_IT_WORKS_PATH,
    imageAlt:
      "Guests table with a guest drawer showing feedback history and the actions Send campaign and Recover guest",
  },
  {
    id: "invite",
    tabLabel: "Invite them back",
    title: "Make the next invitation relevant.",
    body: "A midweek lunch offer. A new menu update. Prepare an email or text for customers who choose to hear from you, then review it before sending.",
    primaryLabel: "Get started",
    secondaryLabel: "See how it works",
    secondaryTo: MARKETING_HOW_IT_WORKS_PATH,
    imageAlt:
      "Campaigns list with a campaign drawer showing audience, channel, send time and the actions Edit offer and Send campaign",
  },
  {
    id: "understand",
    tabLabel: "Understand the response",
    title: "See what customers say and respond to.",
    body: "Review feedback, campaign engagement and confirmed offer redemptions. Use what you learn to refine your next promotion and decide where service needs attention.",
    primaryLabel: "Get started",
    secondaryLabel: "See how it works",
    secondaryTo: MARKETING_HOW_IT_WORKS_PATH,
    imageAlt:
      "Feedback list with a feedback drawer showing the comment, previous feedback and the actions Mark as read and Recover guest",
  },
] as const satisfies readonly HomeHowItWorksStep[]

type ProductCaptureMaterialId = (typeof PRODUCT_CAPTURE_MATERIALS)[number]["id"]

/** Home list order, Figma `4902:19649` (`6967:33965`): Counter Display first. */
const HOME_QR_MATERIAL_ORDER = [
  "counter-display",
  "guest-cards",
  "tray-liner",
  "table-tent",
  "table-talker",
  "window-sticker",
  "receipt-label",
  "delivery-bag-seal",
  "package-label",
  "drink-cup-seal",
  "pizza-box-label",
  "poster-club",
] as const satisfies readonly ProductCaptureMaterialId[]

/** Home labels from Figma; captions, media ids and alt text stay as on Product. */
const HOME_QR_MATERIAL_LABELS: Record<ProductCaptureMaterialId, string> = {
  "counter-display": "QR Counter Display",
  "guest-cards": "QR Guest Cards",
  "tray-liner": "QR Tray Liner",
  "table-tent": "QR Table Tent",
  "table-talker": "QR Table Talker",
  "window-sticker": "QR Window Sticker",
  "receipt-label": "QR Receipt Sticker",
  "delivery-bag-seal": "QR Delivery Bag Seal",
  "package-label": "QR Packaging Sticker",
  "drink-cup-seal": "QR Cup Seal",
  "pizza-box-label": "QR Pizza Box Sticker",
  "poster-club": "QR Customer Club Poster",
}

function homeQrMaterial(id: ProductCaptureMaterialId): ProductCaptureMaterial {
  const material = PRODUCT_CAPTURE_MATERIALS.find((item) => item.id === id)
  if (material == null) {
    throw new Error(`Unknown capture material: ${id}`)
  }
  return { ...material, label: HOME_QR_MATERIAL_LABELS[id] }
}

/**
 * Home QR materials — Figma `4902:19649`.
 * Same formats as Product Capture, in Home order with Home labels. Only the
 * Counter Display photo is new in Figma (`qr-counter-display.webp`, see
 * `HomeQrMaterials`); the other formats reuse the published Product photos.
 */
export const HOME_QR_MATERIALS = {
  eyebrow: "QR MATERIALS",
  title: "Meet customers where the experience happens.",
  body: "A clear invitation at the counter. A reminder with a takeaway. Explore QR formats for the way you serve, starting with the available A5 Counter Display.",
  items: HOME_QR_MATERIAL_ORDER.map(homeQrMaterial),
} as const satisfies {
  eyebrow: string
  title: string
  body: string
  items: readonly ProductCaptureMaterial[]
}

export type HomeIndustry = {
  id: "takeaway" | "cafes" | "casual" | "groups"
  title: string
  body: string
  imageAlt: string
}

/**
 * Home Built for restaurants — Figma `6792:13613`.
 * Four photo cards (`6792:13649`, `6792:13643`, `6792:13654`, `6792:13659`).
 * Title block only: no list selection or per-card links.
 */
export const HOME_INDUSTRIES = {
  eyebrow: "BUILT FOR RESTAURANTS",
  title: "Built for real restaurant days.",
  body: "From takeaways and cafés to casual dining and restaurant groups, Tummly helps you understand your customers, grow your guest list and encourage more orders and visits.",
  items: [
    {
      id: "takeaway",
      title: "Takeaways & QSRs",
      body: "Hear how collection and delivery went, then make the next invitation count.",
      imageAlt: "Green Tummly QR display on the counter of a quick-service restaurant",
    },
    {
      id: "cafes",
      title: "Cafés & bakeries",
      body: "Learn what customers love and give them a reason to drop in again.",
      imageAlt: "Smiling café worker serving a guest beside a Tummly QR display",
    },
    {
      id: "casual",
      title: "Casual dining",
      body: "Follow up on a disappointing evening and recognise what made a good one.",
      imageAlt: "Friends laughing together at a restaurant table with a Tummly QR display",
    },
    {
      id: "groups",
      title: "Growing restaurant groups",
      body: "See what customers are saying across your restaurants and where managers should focus.",
      imageAlt: "Guests dining in a busy restaurant dining room",
    },
  ],
} as const satisfies {
  eyebrow: string
  title: string
  body: string
  items: readonly HomeIndustry[]
}

export type HomeTrustControlItem = {
  id: "private-feedback" | "permissions" | "restaurant-first" | "control"
  title: string
  body: string
}

/**
 * Home Trust & control — Figma `4942:19219`. The badge carousel reuses
 * `FOOTER_TRUST_BADGES` and stays behind the public trust-claims gate.
 */
export const HOME_TRUST_CONTROL = {
  eyebrow: "TRUST & CONTROL",
  title: "Good communication starts with trust.",
  body: "Customers share feedback privately and choose separately whether to receive marketing. You decide how to respond and which messages to send.",
  ctaLabel: "Explore Trust & Privacy",
  ctaTo: MARKETING_TRUST_PRIVACY_PATH,
  items: [
    {
      id: "private-feedback",
      title: "A private way to be heard",
      body: "The customer’s feedback goes to your restaurant team.",
    },
    {
      id: "permissions",
      title: "A clear choice about marketing",
      body: "Customers choose whether to receive offers and updates by email or text.",
    },
    {
      id: "restaurant-first",
      title: "Access that fits the job",
      body: "Give managers and staff the access they need for their restaurants.",
    },
    {
      id: "control",
      title: "Help with the work, oversight of the decision",
      body: "AI can explain feedback and prepare drafts for your team to review.",
    },
  ],
} as const satisfies {
  eyebrow: string
  title: string
  body: string
  ctaLabel: string
  ctaTo: string
  items: readonly HomeTrustControlItem[]
}

/**
 * Home Pricing strip — Figma `4902:19866` (Pricing instance `7190:11030`).
 * Plan names, CTAs and the Growth highlight come from the AI Assistant plan cards;
 * monthly amounts come from the published catalogue at render time. `subline` is the
 * grey Location-limit line (derived from the Pricing page plan cards; the Pilot note is Home copy).
 */
export function planLocationsLimit(planId: "Starter" | "Growth" | "Group") {
  const limit = PRICING_PLAN_CARDS.find(
    (card) => card.id === planId,
  )?.coreFeatures.find((feature) => feature.label === "Locations")?.value
  if (limit == null) {
    throw new Error(`Pricing plan ${planId} has no Locations limit`)
  }
  return limit
}

function locationLimitLine(planId: "Starter" | "Growth" | "Group") {
  const limit = planLocationsLimit(planId)
  return limit === "1" ? "1 Location only" : `${limit} Locations`
}

const HOME_PRICING_SUBLINES = {
  Pilot: "No payment card required.",
  Starter: locationLimitLine("Starter"),
  Growth: locationLimitLine("Growth"),
  Group: locationLimitLine("Group"),
} as const

export const HOME_PRICING = {
  eyebrow: "PRICING",
  title: "Start with a Pilot. Choose a plan that fits.",
  body: "Try the Guest Loop at one Location for 30 days. £0, no card and no automatic paid renewal.",
  compareLabel: AI_ASSISTANT_PLANS.compareLabel,
  compareTo: AI_ASSISTANT_PLANS.compareTo,
  cards: AI_ASSISTANT_PLANS.cards.map((card) => ({
    ...card,
    description:
      card.id === "Pilot"
        ? "Try Tummly at one Location before choosing a paid plan."
        : card.description,
    subline: HOME_PRICING_SUBLINES[card.id],
  })),
} as const

/** Home bottom Sign-up CTA — Figma `6788:42946` (component set `4769:17454`, Type=Default). */
export const HOME_SIGN_UP_CTA: MarketingSignUpCtaContent = {
  title: "Your next customer conversation starts here.",
  body: "Make what you learn from today’s customers useful for their next order or visit. Start with private feedback, a thoughtful response and a relevant invitation to return.",
  primaryLabel: "Get started",
  badge: "No payment card required",
  planIntent: { plan: "Pilot" },
  legalNote: "read",
}
