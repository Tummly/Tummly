import type { MarketingSignUpCtaContent } from "@/components/marketing/MarketingSignUpCta"
import {
  MARKETING_AI_ASSISTANT_PATH,
  MARKETING_FAQS_PATH,
  MARKETING_PRODUCT_PATH,
  MARKETING_QR_MATERIALS_PATH,
  MARKETING_TRUST_PRIVACY_PATH,
} from "@/constants/marketingNav"
import { AI_ASSISTANT_PLANS } from "@/content/marketing/aiAssistantPage"

/** Shared section inset — matches Product / AI Assistant (20px mobile, 60px desktop). */
export const HOW_IT_WORKS_SECTION_INSET = "px-5 lg:px-[60px]"

/** Shared vertical pad — 50px mobile, 70px desktop. Figma hero `5661:19524`. */
export const HOW_IT_WORKS_SECTION_Y = "py-12.5 lg:py-[70px]"

export const HOW_IT_WORKS_SECTION_HEADING =
  "font-jakarta text-[34px] font-medium leading-none text-black lg:text-[46px] lg:leading-[58px]"

export const HOW_IT_WORKS_SECTION_BODY =
  "font-sans text-base font-normal leading-[22px] text-[#141414] lg:text-[18px] lg:leading-6"

/** Figma How it works hero `5661:19524`. */
export const HOW_IT_WORKS_HERO = {
  title: "From a QR scan to a guest relationship you can build on.",
  body: "Tummly connects the moments guests already interact with your restaurant to private Feedback, optional contact details, separate permission choices and useful next actions.",
} as const

/** Figma How it works loop intro `5661:19534`. */
export const HOW_IT_WORKS_LOOP_INTRO = {
  title: "One simple loop. Each step makes the next one more useful.",
  body: "Guest Loop connects physical restaurant touchpoints with a simple digital guest experience, then helps your team turn what you learn into the next useful action.",
} as const

export type HowItWorksStepId =
  | "place"
  | "listen"
  | "connect"
  | "act"
  | "learn"
  | "improve"

export type HowItWorksStep = {
  id: HowItWorksStepId
  tabLabel: string
  title: string
  body: string
  ctaLabel: string
  ctaTo: string
  /** Place reuses home material stack; other steps use Figma grey frame until media ships. */
  media: "place" | "placeholder"
}

/**
 * How it works Guest Loop steps —
 * Figma `5661:19539`, `5667:32260`, `5667:32275`, `5667:32290`,
 * `5667:32305`, `5667:32320`.
 */
export const HOW_IT_WORKS_STEPS = [
  {
    id: "place",
    tabLabel: "Step 01 — Place",
    title: "Start where guests already interact.",
    body: "Use ready-to-go Tummly QR materials across tables, counters, receipts, takeaway and delivery moments. Each placement connects guests to the right digital Guest Form.",
    ctaLabel: "Explore QR materials",
    ctaTo: MARKETING_QR_MATERIALS_PATH,
    media: "place",
  },
  {
    id: "listen",
    tabLabel: "Step 02 — Listen",
    title: "Collect quick, private Feedback.",
    body: "Guests type Feedback or use voice-to-text, review the text, then submit it privately to the restaurant.",
    ctaLabel: "See the Guest Form",
    ctaTo: MARKETING_PRODUCT_PATH,
    media: "placeholder",
  },
  {
    id: "connect",
    tabLabel: "Step 03 — Connect",
    title: "Let guests choose how they want to hear from you.",
    body: "After Feedback, guests can optionally add a first name and one contact method. Permission choices stay separate from the contact detail.",
    ctaLabel: "See how permissions work",
    ctaTo: MARKETING_TRUST_PRIVACY_PATH,
    media: "placeholder",
  },
  {
    id: "act",
    tabLabel: "Step 04 — Act",
    title: "Respond when it matters.",
    body: "Review Feedback that needs attention, prepare follow-up where permitted, or create a Campaign and Offer for guests who are eligible for that channel.",
    ctaLabel: "Explore Campaigns & Offers",
    ctaTo: MARKETING_PRODUCT_PATH,
    media: "placeholder",
  },
  {
    id: "learn",
    tabLabel: "Step 05 — Learn",
    title: "See what is happening across Guest Loop.",
    body: "Reports connect valid QR activity, Feedback, guest capture, Campaigns, Offers and Redemptions using consistent definitions.",
    ctaLabel: "Explore Reports & Briefs",
    ctaTo: MARKETING_PRODUCT_PATH,
    media: "placeholder",
  },
  {
    id: "improve",
    tabLabel: "Step 06 — Improve",
    title: "Turn what you learn into the next action.",
    body: "Weekly Briefs and Tummly AI explain measured changes and can prepare governed Drafts for review. You stay in control of consequential actions.",
    ctaLabel: "Explore AI Assistant",
    ctaTo: MARKETING_AI_ASSISTANT_PATH,
    media: "placeholder",
  },
] as const satisfies readonly HowItWorksStep[]

/** Figma How it works Guest Form `5887:20697` (+ step variants). */
export const HOW_IT_WORKS_GUEST_FORM = {
  title: "A Guest Form designed to keep Feedback simple.",
  body: "Guests share private Feedback first, then can optionally add a first name and one contact method and make separate permission choices where applicable.",
} as const

export type HowItWorksGuestFormStepId =
  | "feedback"
  | "details"
  | "updates"
  | "submit"
  | "offer"

export type HowItWorksGuestFormStep = {
  id: HowItWorksGuestFormStepId
  label: string
  title: string
  body: string
  mediaId: string
  imageAlt: string
}

export const HOW_IT_WORKS_GUEST_FORM_STEPS = [
  {
    id: "feedback",
    label: "01 Share private Feedback",
    title: "Tell us about your experience",
    body: "Guests type Feedback or use voice-to-text, review what they've written, then submit it privately to the restaurant.",
    mediaId: "how-it-works/guest-form-step-01",
    imageAlt:
      "Guest Form phone showing the Feedback field and Dictate control",
  },
  {
    id: "details",
    label: "02 Add your details",
    title: "Add your name and one contact method",
    body: "Guests can optionally add a first name and one contact method so the restaurant can follow up where appropriate.",
    mediaId: "how-it-works/guest-form-step-02",
    imageAlt:
      "Guest Form phone showing optional name and contact detail fields",
  },
  {
    id: "updates",
    label: "03 Choose whether to receive updates",
    title: "Stay in control of future messages",
    body: "Guests choose separately whether they want to receive applicable offers and updates from the restaurant.",
    mediaId: "how-it-works/guest-form-step-03",
    imageAlt:
      "Guest Form phone showing the permission choice for offers and updates",
  },
  {
    id: "submit",
    label: "04 Submit Feedback",
    title: "Share Feedback privately",
    body: "The Feedback is submitted privately to the restaurant and is not posted publicly.",
    mediaId: "how-it-works/guest-form-step-04",
    imageAlt: "Guest Form phone with the Submit feedback control",
  },
  {
    id: "offer",
    label: "05 See an Offer when one applies",
    title: "Receive a relevant Offer",
    body: "If the restaurant has a valid Offer available for that guest journey, it can appear after the Feedback is submitted.",
    mediaId: "how-it-works/guest-form-step-05",
    imageAlt:
      "Guest Form phone showing a thank-you screen with a relevant Offer",
  },
] as const satisfies readonly HowItWorksGuestFormStep[]

/**
 * How it works product areas —
 * Figma `5661:19613`, `5667:32335`, `5667:32505`, `5667:32617`.
 */
export type HowItWorksFeatureId =
  | "feedback"
  | "guests"
  | "campaigns"
  | "reports"

export type HowItWorksFeature = {
  id: HowItWorksFeatureId
  tabLabel: string
  title: string
  /** One or more body paragraphs. */
  body: readonly string[]
  /** Active tab + media frame fill from Figma. */
  accent: string
  /** Feedback uses grey media; others match their accent. */
  mediaBg: string
}

export const HOW_IT_WORKS_FEATURES = [
  {
    id: "feedback",
    tabLabel: "01 Feedback",
    title: "See what guests are telling you, with the context attached.",
    body: [
      "Review private Feedback alongside the Location, QR placement, guest comment and follow-up status, with Tummly AI helping classify Feedback as Positive, Neutral or Negative.",
    ],
    accent: "#dfebe3",
    mediaBg: "#e9e9e9",
  },
  {
    id: "guests",
    tabLabel: "02 Guests",
    title: "Build a guest list you can actually use responsibly.",
    body: [
      "Keep guest identity, contact methods, permission evidence and Guest Loop history connected in one place, without treating contact details as marketing permission.",
    ],
    accent: "#e9f0fd",
    mediaBg: "#e9f0fd",
  },
  {
    id: "campaigns",
    tabLabel: "03 Campaigns & Offers",
    title: "Bring eligible guests back with Campaigns and Offers.",
    body: [
      "Choose the goal, audience, channel, message and schedule. Add an Offer when it makes sense, then review final eligibility, exclusions and usage before sending.",
      "Offers include clear validity, expiry and Redemption controls so your team can manage them properly.",
    ],
    accent: "#eee0d3",
    mediaBg: "#eee0d3",
  },
  {
    id: "reports",
    tabLabel: "04 Reports & Briefs",
    title: "See the complete Guest Loop, not isolated activity.",
    body: [
      "Understand how valid QR activity moves through Feedback, guest capture, Campaigns, Offers and Redemption.",
      "Reports and Weekly Briefs use consistent definitions so performance is easier to understand across your Locations and over time.",
    ],
    accent: "#eaebdf",
    mediaBg: "#eaebdf",
  },
] as const satisfies readonly HowItWorksFeature[]

/**
 * How it works plans strip — Figma `5661:19770`.
 * Reuses AI Assistant / Home plan cards; no eyebrow, no AI allowance row.
 */
export const HOW_IT_WORKS_PLANS = {
  title: "Choose how you'd like to start.",
  body: "Try Tummly at one Location with the 30-day Pilot, or choose a paid plan based on the number of Locations you operate.",
  compareLabel: AI_ASSISTANT_PLANS.compareLabel,
  compareTo: AI_ASSISTANT_PLANS.compareTo,
  cards: AI_ASSISTANT_PLANS.cards,
} as const

export type HowItWorksFaqItem = {
  id: string
  question: string
  answer: string
}

/** Figma How it works FAQs `5661:19821`. */
export const HOW_IT_WORKS_FAQS = {
  title: "Frequently asked questions",
  moreTitle: "Have another question?",
  moreBody:
    "Find more answers about Tummly, pricing, QR materials and Guest Loop.",
  moreCtaLabel: "Explore all FAQs",
  moreCtaTo: MARKETING_FAQS_PATH,
  items: [
    {
      id: "qr-scan",
      question: "What happens when a guest scans a Tummly QR code?",
      answer:
        "The QR code opens your digital Guest Form. Guests can leave private Feedback and, if they choose, share contact details and make separate communication permission choices. Their submission then becomes available to your team in Tummly.",
    },
    {
      id: "rating",
      question: "Do guests need to leave a rating?",
      answer:
        "No. Guests leave Feedback through the digital Guest Form rather than choosing a star rating. Tummly AI helps classify submitted Feedback as Positive, Neutral or Negative so your team can understand sentiment and identify what may need attention.",
    },
    {
      id: "feedback-private",
      question: "Is Feedback submitted through Tummly private?",
      answer:
        "Yes. Feedback submitted through the Tummly Guest Form is private to the restaurant and remains separate from public reviews.",
    },
    {
      id: "contact-details",
      question: "Do guests have to provide their contact details?",
      answer:
        "No. Guests can leave Feedback without providing contact details. Sharing contact information is optional, and communication permissions are handled separately.",
    },
    {
      id: "email-sms",
      question:
        "How do guests choose whether they can receive Email or SMS messages?",
      answer:
        "Guests make separate permission choices for the communication channels available to them. Providing contact details does not automatically mean a guest is eligible to receive marketing messages.",
    },
    {
      id: "website-printer",
      question: "Do I need a website or printer to use Guest Loop?",
      answer:
        "No. Guest Forms are digital, and physical Tummly QR materials can be ordered ready to use through the Tummly Shop. You do not need your own website or in-house printer to get Guest Loop running.",
    },
    {
      id: "after-feedback",
      question: "What can I do after guests leave Feedback?",
      answer:
        "Your team can review private Feedback, understand what may need attention and manage guest follow-up. Where a guest is eligible to receive communications, you can also use Tummly Offers and Campaigns to support future engagement.",
    },
    {
      id: "ai-auto-campaigns",
      question: "Can Tummly AI automatically send Campaigns to guests?",
      answer:
        "No. Tummly AI can help analyse Feedback and support actions such as preparing Campaign or Offer content, but it does not independently send Campaigns. Your team remains in control of what is reviewed and sent.",
    },
  ] satisfies HowItWorksFaqItem[],
} as const

/** Shared bottom Sign-up CTA — same Pilot copy as Product / Home / AI Assistant. */
export const HOW_IT_WORKS_SIGN_UP_CTA: MarketingSignUpCtaContent = {
  title: "Ready to see Guest Loop in a real restaurant?",
  body: "Start with one Location and see how Guest Loop works with real guests.",
  primaryLabel: "Start 30-day Pilot",
  badge: "No payment card required",
  planIntent: { plan: "Pilot" },
}
