import { LEGAL_ROUTES } from "@/constants/legalRoutes"
import { MARKETING_PRICING_PATH } from "@/constants/marketingNav"

/** Figma Trust & privacy hero (`4974:26876`). */
export const TRUST_PRIVACY_HERO = {
  title: "Guest relationships deserve careful handling.",
  paragraphs: [
    "Tummly helps restaurants collect private Feedback, build permission-based guest relationships and take useful action without treating trust as a checkbox.",
    "Guest permissions stay clear, restaurant data stays separated, and consequential actions remain under operator control.",
  ],
  primaryLabel: "Read our Privacy Notice",
  primaryTo: LEGAL_ROUTES.privacy,
  secondaryLabel: "See how Guest Loop works",
  /** How-it-works page is not shipped yet — send visitors to the home product story. */
  secondaryTo: "/",
} as const

/** Figma permission section (`4974:26885`). */
export const TRUST_PRIVACY_PERMISSION = {
  title: "Permission should be clear, not bundled.",
  intro: "A guest can give private Feedback without agreeing to marketing.",
  recordsLead: "Where relevant, Tummly keeps separate records for:",
  records: [
    {
      id: "feedback-follow-up",
      title: "Feedback follow-up",
      body: "Permission connected to responding to a guest about their Feedback.",
    },
    {
      id: "email-marketing",
      title: "Email marketing",
      body: "Permission to receive eligible marketing Emails.",
    },
    {
      id: "sms-marketing",
      title: "SMS marketing",
      body: "Permission to receive eligible marketing text messages.",
    },
  ],
  closing:
    "Each permission can carry its own wording, source, Location, time and withdrawal history.",
  note: "Having an Email address or phone number does not automatically make a guest eligible for marketing.",
} as const

export type TrustPrivacyProseSection = {
  id: string
  title: string
  /** Paragraph blocks; use blank lines between paragraphs in render. */
  paragraphs: string[]
  /** Optional bullet list inserted after the first paragraph. */
  bullets?: string[]
  /** Paragraphs rendered after the bullet list. */
  afterBullets?: string[]
  /** `#f0f0f0` when true; white when false. */
  mutedBackground: boolean
}

/**
 * Content blocks after the permission section — Figma `4974:26905` … `4974:26930`.
 */
export const TRUST_PRIVACY_PROSE_SECTIONS: TrustPrivacyProseSection[] = [
  {
    id: "feedback-not-gating",
    title: "Feedback is for the restaurant, not a review-gating system.",
    mutedBackground: false,
    paragraphs: [
      "Guest Feedback collected through Tummly is part of the restaurant’s private Feedback workflow.",
      "Tummly does not use a guest’s rating to decide who is allowed to leave a public review, and Offers must not be used to reward only positive public reviewers.",
    ],
  },
  {
    id: "tenant-isolation",
    title: "One restaurant’s guest list is not another restaurant’s guest list.",
    mutedBackground: true,
    paragraphs: [
      "Tummly is designed as a multi-tenant service with restaurant and Location boundaries enforced by the product.",
      "Identifiable guests are not merged across unrelated restaurants.",
      "Tummly does not sell guest contact lists or expose one restaurant’s identifiable guest data to another.",
      "Access is determined by the user’s authorised workspace, Location and role.",
    ],
  },
  {
    id: "ai-control",
    title: "AI can prepare the next step. You stay in control.",
    mutedBackground: false,
    paragraphs: [
      "Tummly AI can work with the Guest Loop information you are permitted to access to:",
    ],
    bullets: [
      "explain performance;",
      "summarise Feedback;",
      "identify evidence-backed themes;",
      "draft recovery responses;",
      "prepare Campaign Drafts;",
      "recommend practical next actions.",
    ],
    afterBullets: [
      "It cannot change guest permission, bypass suppression, invent billing or eligibility information, cross restaurant boundaries or autonomously send consequential guest communications.",
      "When an action needs review or confirmation, Tummly brings you back into the decision.",
    ],
  },
  {
    id: "access-control",
    title: "Access is controlled by role and Location.",
    mutedBackground: true,
    paragraphs: [
      "Tummly is designed around server-side tenant isolation, Location scope and role-based permissions.",
      "That means a Location manager does not automatically receive the same access as an account administrator, and reporting or billing access does not automatically provide unrestricted access to guest contact information.",
      "Material actions and support access should be appropriately logged and auditable.",
      "Payment card details are handled through the approved payment-provider architecture rather than being stored as raw card data by Tummly.",
    ],
  },
  {
    id: "guest-withdrawal",
    title: "A guest can change their mind.",
    mutedBackground: false,
    paragraphs: [
      "Withdrawal and suppression are part of the Guest Loop permission model.",
      "When a permission is withdrawn, that change must feed into future messaging eligibility rather than being bypassed by Campaigns, manual actions or AI.",
      "Historical permission evidence may still need to be preserved as part of the record of what happened.",
    ],
  },
  {
    id: "privacy-lifecycle",
    title: "Privacy is more than collection.",
    mutedBackground: true,
    paragraphs: [
      "Tummly is being built to support governed processes for:",
    ],
    bullets: [
      "access and export;",
      "correction;",
      "deletion requests;",
      "withdrawal and suppression;",
      "retention;",
      "secure downloads;",
      "relevant audit history.",
    ],
    afterBullets: [
      "Removing identifiable guest information does not necessarily mean deleting unrelated accounting, security, transaction or audit records that need to be retained appropriately.",
    ],
  },
]

/** Figma Trust & privacy Sign up CTA (`4974:26948`). */
export const TRUST_PRIVACY_SIGN_UP_CTA = {
  title: "Trust should be visible in how the product works.",
  body: "See how private Feedback, guest permissions and controlled Campaigns fit together inside Guest Loop.",
  primaryLabel: "Start 30-day Pilot",
  secondaryLabel: "See pricing",
  secondaryTo: MARKETING_PRICING_PATH,
  badge: "1 verified Location · No payment card required",
} as const
