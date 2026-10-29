import type { MarketingSignUpCtaContent } from "@/components/marketing/MarketingSignUpCta"
import {
  MARKETING_AI_ASSISTANT_PATH,
  MARKETING_FAQS_PATH,
  MARKETING_FOR_GROUPS_PATH,
  MARKETING_HOW_IT_WORKS_PATH,
  MARKETING_TRUST_PRIVACY_PATH,
} from "@/constants/marketingNav"
import { AI_ASSISTANT_PLANS } from "@/content/marketing/aiAssistantPage"

/** Shared section inset — matches AI Assistant / Pricing (20px mobile, 60px desktop). */
export const PRODUCT_SECTION_INSET = "px-5 lg:px-[60px]"

/** Shared vertical pad — 50px mobile, 70px desktop. Figma hero `5661:22625`. */
export const PRODUCT_SECTION_Y = "py-12.5 lg:py-[70px]"

export const PRODUCT_SECTION_HEADING =
  "font-jakarta text-[34px] font-medium leading-none text-black lg:text-[46px] lg:leading-[58px]"

export const PRODUCT_SECTION_BODY =
  "font-sans text-base font-normal leading-[22px] text-[#141414] lg:text-[18px] lg:leading-6"

/** Figma eyebrow — functional 14px SemiBold, uppercase, 1.12px tracking, `#4a4a4a`. */
export const PRODUCT_EYEBROW =
  "m-0 font-sans text-sm font-semibold uppercase leading-4 tracking-[1.12px] text-[#4a4a4a]"

/** Figma split-band title — Jakarta Medium 36/48 (28/38 below `lg`). */
export const PRODUCT_BAND_HEADING =
  "m-0 font-jakarta text-[28px] font-medium leading-[38px] text-black lg:text-[36px] lg:leading-[48px]"

/** Figma split-band body — 16/22, `#141414`. */
export const PRODUCT_BAND_BODY =
  "m-0 font-sans text-base font-normal leading-[22px] text-[#141414]"

/** Figma section title — Jakarta Medium 46/52 (34/40 below `lg`). */
export const PRODUCT_DISPLAY_HEADING =
  "m-0 font-jakarta text-[34px] font-medium leading-10 text-black lg:text-[46px] lg:leading-[52px]"

export type ProductFeaturePoint = {
  id: string
  title: string
  body: string
}


/** Figma Product hero `5661:22625`. */
export const PRODUCT_HERO = {
  eyebrow: "The Tummly Guest Loop",
  title: "Run the whole Guest Loop from one place.",
  body: "Collect private feedback from orders and visits, understand the customer’s context and prepare the response. Connect service replies, offers, campaigns and reporting so your team can act on what it learns.",
  primaryLabel: "Get started",
  secondaryLabel: "See how it works",
  secondaryTo: MARKETING_HOW_IT_WORKS_PATH,
  trustItems: ["£0 for 30 days", "No payment card required"] as const,
  imageAlt:
    "Restaurant operator at a laptop with a Campaign Draft ready for review, recent guests selected, and follow-up available.",
} as const

export type ProductCaptureMaterial = {
  id: string
  label: string
  caption: string
  /** marketingMediaMeta / remoteSimpleUrl id */
  mediaId: string
  imageAlt: string
}

/**
 * Figma Capture materials `6106:17567` … `6106:18055`.
 * Desktop: click or list-hover wheel; mobile uses tap.
 */
export const PRODUCT_CAPTURE_MATERIALS = [
  {
    id: "guest-cards",
    label: "QR Guest Cards",
    caption: "Compact cards for takeaway, delivery and counter handout.",
    mediaId: "product/capture-guest-cards",
    imageAlt: "Hand holding a green Tummly QR guest card with an offer.",
  },
  {
    id: "counter-display",
    label: "QR Counter Display",
    caption:
      "Freestanding QR display for tills, counters and collection points.",
    mediaId: "product/capture-counter-display",
    imageAlt: "Freestanding QR counter display on a restaurant counter.",
  },
  {
    id: "tray-liner",
    label: "Tray Liner",
    caption: "Printed QR tray liner for dine-in Feedback and Offers.",
    mediaId: "product/capture-tray-liner",
    imageAlt: "Printed QR tray liner on a tray with a meal.",
  },
  {
    id: "table-tent",
    label: "Table Tent",
    caption:
      "Table display that makes private Feedback easy to share while dining.",
    mediaId: "product/capture-table-tent",
    imageAlt: "QR table tent standing on a dining table.",
  },
  {
    id: "table-talker",
    label: "Table Talker",
    caption:
      "Three-sided table display that keeps the QR visible from more angles.",
    mediaId: "product/capture-table-talker",
    imageAlt: "Three-sided QR table talker on a restaurant table.",
  },
  {
    id: "window-sticker",
    label: "Window Sticker",
    caption: "Branded QR sticker for entrances, exits and front windows.",
    mediaId: "product/capture-window-sticker",
    imageAlt: "Branded QR window sticker on a restaurant window.",
  },
  {
    id: "receipt-label",
    label: "Receipt Label",
    caption:
      "Small QR label for receipts, designed to collect private Feedback.",
    mediaId: "product/capture-receipt-label",
    imageAlt: "Receipt with a small Tummly QR label.",
  },
  {
    id: "delivery-bag-seal",
    label: "Delivery Bag Seal",
    caption: "QR-enabled seal for takeaway and delivery bags.",
    mediaId: "product/capture-delivery-bag-seal",
    imageAlt: "Delivery bag sealed with a QR-enabled sticker.",
  },
  {
    id: "package-label",
    label: "Standard Package Label",
    caption: "Flexible QR label for boxes, bags and takeaway packaging.",
    mediaId: "product/capture-package-label",
    imageAlt: "Takeaway packaging with a standard QR package label.",
  },
  {
    id: "drink-cup-seal",
    label: "Drink / Cup Seal",
    caption: "Compact QR seal designed for drinks and cups.",
    mediaId: "product/capture-drink-cup-seal",
    imageAlt: "Drink cup with a compact QR seal on the lid.",
  },
  {
    id: "pizza-box-label",
    label: "Pizza Box Label",
    caption: "QR label for pizza boxes, inviting Feedback after the order.",
    mediaId: "product/capture-pizza-box-label",
    imageAlt: "Pizza box with a Tummly QR label.",
  },
  {
    id: "poster-club",
    label: "Poster / Customer Club",
    caption: "Larger QR display designed to grow your guest list.",
    mediaId: "product/capture-poster-club",
    imageAlt: "Customer club poster with a large Tummly QR code.",
  },
] as const satisfies readonly ProductCaptureMaterial[]

/** Figma Product QR materials `5661:22668`. */
export const PRODUCT_QR_MATERIALS = {
  eyebrow: "QR MATERIALS",
  title: "Make it easy to tell you\nhow it went.",
  body: "Put your restaurant’s QR where customers notice it. Open a private feedback form from the counter, a collection point or a digital link, with a wider range of physical formats to explore.",
  /** Figma list order and labels; captions and photos reuse the Capture catalogue. */
  items: [
    {
      id: "counter-display",
      label: "QR Counter Display",
      caption:
        "Freestanding QR display for tills, counters and collection points.",
      mediaId: "product/capture-counter-display",
      imageAlt: "Freestanding QR counter display on a restaurant counter.",
    },
    {
      id: "guest-cards",
      label: "QR Guest Cards",
      caption: "Compact cards for takeaway, delivery and counter handout.",
      mediaId: "product/capture-guest-cards",
      imageAlt: "Hand holding a green Tummly QR guest card with an offer.",
    },
    {
      id: "tray-liner",
      label: "QR Tray Liner",
      caption: "Printed QR tray liner for dine-in Feedback and Offers.",
      mediaId: "product/capture-tray-liner",
      imageAlt: "Printed QR tray liner on a tray with a meal.",
    },
    {
      id: "table-tent",
      label: "QR Table Tent",
      caption:
        "Table display that makes private Feedback easy to share while dining.",
      mediaId: "product/capture-table-tent",
      imageAlt: "QR table tent standing on a dining table.",
    },
    {
      id: "table-talker",
      label: "QR Table Talker",
      caption:
        "Three-sided table display that keeps the QR visible from more angles.",
      mediaId: "product/capture-table-talker",
      imageAlt: "Three-sided QR table talker on a restaurant table.",
    },
    {
      id: "window-sticker",
      label: "QR Window Sticker",
      caption: "Branded QR sticker for entrances, exits and front windows.",
      mediaId: "product/capture-window-sticker",
      imageAlt: "Branded QR window sticker on a restaurant window.",
    },
    {
      id: "receipt-label",
      label: "QR Receipt Sticker",
      caption:
        "Small QR label for receipts, designed to collect private Feedback.",
      mediaId: "product/capture-receipt-label",
      imageAlt: "Receipt with a small Tummly QR label.",
    },
    {
      id: "delivery-bag-seal",
      label: "QR Delivery Bag Seal",
      caption: "QR-enabled seal for takeaway and delivery bags.",
      mediaId: "product/capture-delivery-bag-seal",
      imageAlt: "Delivery bag sealed with a QR-enabled sticker.",
    },
    {
      id: "package-label",
      label: "QR Packaging Sticker",
      caption: "Flexible QR label for boxes, bags and takeaway packaging.",
      mediaId: "product/capture-package-label",
      imageAlt: "Takeaway packaging with a standard QR package label.",
    },
    {
      id: "drink-cup-seal",
      label: "QR Cup Seal",
      caption: "Compact QR seal designed for drinks and cups.",
      mediaId: "product/capture-drink-cup-seal",
      imageAlt: "Drink cup with a compact QR seal on the lid.",
    },
    {
      id: "pizza-box-label",
      label: "QR Pizza Box Sticker",
      caption: "QR label for pizza boxes, inviting Feedback after the order.",
      mediaId: "product/capture-pizza-box-label",
      imageAlt: "Pizza box with a Tummly QR label.",
    },
    {
      id: "poster-club",
      label: "QR Customer Club Poster",
      caption: "Larger QR display designed to grow your guest list.",
      mediaId: "product/capture-poster-club",
      imageAlt: "Customer club poster with a large Tummly QR code.",
    },
  ],
} as const satisfies {
  eyebrow: string
  title: string
  body: string
  items: readonly ProductCaptureMaterial[]
}


/** Figma Product Guest Form `5661:22688`. */
export const PRODUCT_FEEDBACK = {
  eyebrow: "THE GUEST FORM",
  title: "Let customers explain in\ntheir own words.",
  body: "A short, restaurant-branded form invites customers to type or dictate their experience, then add a first name and either an email address or a UK mobile number. They can separately confirm their contact details for a reply about that feedback. Marketing is an optional choice and requires its own confirmation.",
  points: [
    {
      id: "private-feedback",
      title: "Private feedback, directly to your restaurant",
      body: "Give customers a simple way to share their experience privately with your team.",
    },
    {
      id: "in-their-own-words",
      title: "In their own words",
      body: "Customers can type or dictate their feedback, making it easier to explain what happened.",
    },
    {
      id: "marketing-optional",
      title: "Marketing is always optional",
      body: "Customers can submit feedback without agreeing to receive promotional emails or texts.",
    },
  ] satisfies ProductFeaturePoint[],
  imageAlt:
    "Guest Feedback form on a phone for Mehmet’s Grill, showing type-or-dictate input and contact details.",
} as const

/** Figma Product Feedback management `5661:22714`. */
export const PRODUCT_FEEDBACK_RECOVERY = {
  eyebrow: "FEEDBACK MANAGEMENT",
  title: "Give service concerns the\nattention they deserve.",
  body: "A late collection or a disappointing meal deserves a considered response. Read the original comment, see the restaurant and date, then prepare a personal reply about the experience.",
  points: [
    {
      id: "find-right-feedback",
      title: "Find the right feedback",
      body: "Review customer comments by restaurant, date and status to understand what happened.",
    },
    {
      id: "focus-what-matters",
      title: "Focus on what matters",
      body: "Identify service concerns that need a closer look or follow-up from your team.",
    },
    {
      id: "prepare-thoughtful-reply",
      title: "Prepare a thoughtful reply",
      body: "Draft a personal response that addresses the customer’s experience.",
    },
  ] satisfies ProductFeaturePoint[],
  imageAlt:
    "Feedback inbox showing a private guest response draft with AI recovery assistance.",
} as const

/** Figma Product Guest context `5661:23065`. */
export const PRODUCT_GUESTS = {
  eyebrow: "Guest context",
  title: "See the person behind\nthe latest comment.",
  body: "Bring previous feedback and recorded offer activity into the conversation. Help your team understand the customer’s experience before deciding how to respond.",
  points: [
    {
      id: "understand-previous-feedback",
      title: "Understand previous feedback",
      body: "See what customers have shared before and identify recurring experiences or concerns.",
    },
    {
      id: "review-offer-activity",
      title: "Review offer activity",
      body: "View recorded offer claims and confirmed redemptions alongside customer feedback.",
    },
    {
      id: "channel-choices",
      title: "Channel choices",
      body: "See whether the customer chose email or text marketing.",
    },
  ] satisfies ProductFeaturePoint[],
  imageAlt:
    "Guest list with a profile for Charlotte Wilson showing feedback shared, offers redeemed and email marketing choice.",
} as const

/** Figma Product Offers & campaigns `5661:23511` — media left. */
export const PRODUCT_CAMPAIGNS = {
  eyebrow: "OFFERS & CAMPAIGNS",
  title: "Give customers a reason\nto order or visit again.",
  body: "Promote a new dish, fill customers in on a menu change or offer a midweek treat. Choose customers who have agreed to hear from you, prepare your email or text and review it before sending or scheduling.",
  points: [
    {
      id: "make-offer-clear",
      title: "Make the offer clear",
      body: "Set the benefit, terms, restaurant and expiry.",
    },
    {
      id: "prepare-invitation",
      title: "Prepare the invitation",
      body: "Create an email or text for the appropriate audience.",
    },
    {
      id: "review-response",
      title: "Review the response",
      body: "See campaign engagement and staff-confirmed offer use.",
    },
  ] satisfies ProductFeaturePoint[],
  imageAlt:
    "Campaign draft “Come back this Sunday” with a free dessert offer for 212 eligible guests.",
} as const

/** Figma Product Reports `5661:23587`. */
export const PRODUCT_REPORTS = {
  eyebrow: "REPORTS",
  title: "Know what deserves\nyour attention.",
  body: "See recurring feedback, campaign engagement and confirmed offer redemptions by restaurant and reporting period. Use the picture to plan your next promotion or decide what to discuss with the team.",
  points: [
    {
      id: "spot-recurring-feedback",
      title: "Spot recurring feedback",
      body: "Identify common themes and explore the customer comments behind them.",
    },
    {
      id: "measure-campaign-response",
      title: "Measure campaign response",
      body: "Review message engagement and confirmed offer redemptions.",
    },
    {
      id: "make-informed-decisions",
      title: "Make informed decisions",
      body: "Use reporting insights to guide your next team discussion and priorities.",
    },
  ] satisfies ProductFeaturePoint[],
  imageAlt:
    "Weekly brief for Mavero Kitchen showing guests captured, positive feedback and a service-speed concern.",
} as const

/** Figma Product AI Assistant `5661:23627`. */
export const PRODUCT_AI_ASSISTANT = {
  eyebrow: "AI ASSISTANT",
  title: "From a restaurant\nquestion to a useful draft.",
  body: "Ask what customers have mentioned this week, explore the response to an offer or prepare a reply. Tummly AI helps with the analysis and first draft, ready for your judgement.",
  points: [
    {
      id: "ask-about-feedback",
      title: "Ask about feedback",
      body: "Explore what customers have mentioned in a chosen period.",
    },
    {
      id: "understand-figures",
      title: "Understand the figures",
      body: "Ask for an explanation of the activity shown.",
    },
    {
      id: "prepare-wording",
      title: "Prepare the wording",
      body: "Create an offer, reply or campaign draft to review.",
    },
  ] satisfies ProductFeaturePoint[],
  ctaLabel: "Explore Tummly AI",
  ctaTo: MARKETING_AI_ASSISTANT_PATH,
  imageAlt:
    "AI Assistant workspace asking what guests love most about the restaurant and what to do with it this Sunday.",
} as const

/** Figma Product outcomes `5661:24151`. */
export const PRODUCT_OUTCOMES = {
  title: "More useful work from the feedback you collect.",
  cards: [
    {
      id: "considered-service-reply",
      title: "A considered service reply",
      body: "Give the team context to answer a concern with care.",
    },
    {
      id: "relevant-reason-to-return",
      title: "A relevant reason to return",
      body: "Prepare invitations that fit your restaurant and the people who chose to hear from you.",
    },
    {
      id: "clearer-management-conversation",
      title: "A clearer management conversation",
      body: "Discuss recurring comments and promotional response with evidence in front of you.",
    },
  ] satisfies ProductFeaturePoint[],
} as const

/** Figma Product For Groups `6821:36359`. */
export const PRODUCT_GROUPS = {
  eyebrow: "BUILT FOR GROWING RESTAURANTS",
  title: "Keep the local detail as your group grows.",
  body: "Review your restaurants together, then look closer at the Location behind a change. Give managers access to the restaurants they look after.",
  points: [
    {
      id: "view-across-restaurants",
      title: "A view across restaurants",
      body: "Review customer feedback and engagement across your group.",
    },
    {
      id: "local-responsibility",
      title: "Local responsibility",
      body: "Give managers access to the restaurants they manage.",
    },
    {
      id: "room-to-look-closer",
      title: "Room to look closer",
      body: "Open the Location detail behind a group-level change.",
    },
  ] satisfies ProductFeaturePoint[],
  primaryLabel: "Get started",
  ctaLabel: "Explore For Groups",
  ctaTo: MARKETING_FOR_GROUPS_PATH,
  trustItems: ["£0 for 30 days", "No payment card required"] as const,
  imageAlt:
    "Friends laughing around a restaurant table with a Tummly QR table tent.",
} as const

export type ProductTrustItem = {
  id: string
  title: string
  body: string
}

export type ProductCertification = {
  id: string
  label: string
}

/** Figma Product Trust & privacy `6937:13422`. */
export const PRODUCT_TRUST = {
  eyebrow: "TRUST & PRIVACY",
  title: "Guest trust.\nRestaurant control.",
  body: "Private feedback, optional marketing and review before important actions help keep the experience clear for customers and practical for your team.",
  ctaLabel: "Explore Trust & Privacy",
  ctaTo: MARKETING_TRUST_PRIVACY_PATH,
  items: [
    {
      id: "private-way-to-be-heard",
      title: "A private way to be heard",
      body: "The customer’s feedback goes to your restaurant team.",
    },
    {
      id: "clear-marketing-choice",
      title: "A clear choice about marketing",
      body: "Customers choose whether to receive offers and updates by email or text.",
    },
    {
      id: "access-fits-the-job",
      title: "Access that fits the job",
      body: "Give managers and staff the access they need for their restaurants.",
    },
    {
      id: "oversight-of-the-decision",
      title: "Help with the work, oversight of the decision",
      body: "AI can explain feedback and prepare drafts for your team to review.",
    },
  ] satisfies ProductTrustItem[],
  certifications: [
    { id: "iso-27001", label: "ISO/IEC 27001" },
    { id: "cyber-essentials-plus", label: "Cyber Essentials Plus" },
    { id: "ico-dpe", label: "ICO Data Protection Essentials" },
    { id: "ico-data", label: "ICO Data Protection Fee Registration" },
    { id: "cyber-essentials", label: "Cyber Essentials" },
    { id: "iasme-level-1", label: "IASME Cyber Assurance Level One" },
    { id: "good-business-charter", label: "Good Business Charter" },
    { id: "iso-27701", label: "ISO/IEC 27701:2025" },
    { id: "uk-gdpr", label: "UK GDPR & Data Protection" },
    { id: "privacy-choices", label: "Cookie & Privacy Choices" },
    { id: "opt-in-marketing", label: "Permission-Based Marketing" },
    { id: "data-rights", label: "Guest Data Rights" },
  ] satisfies ProductCertification[],
} as const

/**
 * Figma Product Pricing `6823:36468`. Plan names, CTAs and the Growth highlight
 * come from the AI Assistant plan cards; monthly amounts come from the published
 * catalogue at render time. `subline` is the grey location-limit line.
 */
const PRODUCT_PRICING_SUBLINES = {
  Pilot: "No payment card required.",
  Starter: "1 location only",
  Growth: "Up to 3 locations",
  Group: "Up to 5 locations",
} as const

export const PRODUCT_PRICING = {
  eyebrow: "PRICING",
  title: "Choose a plan for the way\nyou run your restaurant.",
  body: "Try one Location with the 30-day Pilot, or compare paid plans for your restaurants, team and marketing capacity.",
  compareLabel: AI_ASSISTANT_PLANS.compareLabel,
  compareTo: AI_ASSISTANT_PLANS.compareTo,
  cards: AI_ASSISTANT_PLANS.cards.map((card) => ({
    ...card,
    description:
      card.id === "Pilot"
        ? "Try Tummly at one Location before choosing a paid plan."
        : card.description,
    subline: PRODUCT_PRICING_SUBLINES[card.id],
  })),
} as const

export type ProductFaqItem = {
  id: string
  question: string
  answer: string
}

/** Figma Product FAQs `5661:24202`. */
export const PRODUCT_FAQS = {
  title: "Frequently asked questions",
  moreTitle: "Have another question?",
  moreBody:
    "Find more answers about Tummly, pricing, QR materials and Guest Loop.",
  moreCtaLabel: "Explore all FAQs",
  moreCtaTo: MARKETING_FAQS_PATH,
  items: [
    {
      id: "what-tummly-does",
      question: "What does Tummly help my restaurant do?",
      answer:
        "Tummly connects private feedback with service replies, guest context, relevant offers and campaigns. It helps your team understand customer experiences, respond to concerns and create more reasons for customers to return.",
    },
    {
      id: "takeaway-delivery",
      question: "Is it suitable for takeaway and delivery?",
      answer:
        "Yes. Customers can reach your feedback form after collection, delivery or a visit, using your QR or guest link.",
    },
    {
      id: "multiple-locations",
      question: "Can I manage multiple Locations?",
      answer:
        "Yes. Growth includes up to three Locations and Group up to five. You can configure a paid plan for up to 500 Locations.",
    },
    {
      id: "website",
      question: "Do I need a website?",
      answer:
        "No. Your restaurant’s Tummly feedback form has its own link, which customers can open from a QR code.",
    },
    {
      id: "contact-customers",
      question: "Can I contact customers?",
      answer:
        "Your team can reply about the feedback where the contact has been confirmed and a service reply is available. Promotional email and text require the appropriate confirmed marketing permission for that channel. Providing contact details alone does not grant marketing permission.",
    },
    {
      id: "create-offers",
      question: "Can I create offers?",
      answer:
        "Yes. Set the offer, terms and expiry, then invite eligible customers to use it. A standard thank-you offer is separate from any offer explicitly conditional on joining marketing.",
    },
    {
      id: "how-ai-works",
      question: "How does AI work?",
      answer:
        "Ask practical questions about your restaurant’s feedback and activity. AI can explain the information and prepare drafts for you to review.",
    },
    {
      id: "try-before-paid",
      question: "Can I try before choosing a paid plan?",
      answer:
        "Yes. Start a £0 30-day Pilot at one eligible Location, with no card and no automatic paid renewal.",
    },
  ] satisfies ProductFaqItem[],
} as const

/**
 * Bottom Sign-up CTA — Figma Sign up `6824:12527` (editorial variant, no trust
 * badge, "Read our Terms of Service…" note).
 */
export const PRODUCT_SIGN_UP_CTA: MarketingSignUpCtaContent = {
  title: "Put the Guest Loop to work in your restaurant.",
  body: "Start with one Location and try the work behind repeat business: hear the experience, respond thoughtfully and invite customers who want to hear from you to order or visit again.",
  primaryLabel: "Get started",
  planIntent: { plan: "Pilot" },
  legalNote: "read",
}
