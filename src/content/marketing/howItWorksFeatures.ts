import { MARKETING_AI_ASSISTANT_PATH } from "@/constants/marketingNav"

/**
 * How it Works feature rows — Figma marketing frames
 * Feedback `6870:35827`, Guests `6870:35871`, Campaigns & Offers `6871:35920`,
 * Reports & Briefs `6873:12598`, AI Assistant `6871:35965`.
 */
export type HowItWorksFeatureId =
  | "feedback"
  | "guests"
  | "campaigns"
  | "reports"
  | "ai-assistant"

export type HowItWorksFeaturePoint = {
  id: string
  title: string
  body: string
}

export type HowItWorksFeature = {
  id: HowItWorksFeatureId
  eyebrow: string
  /** `\n` renders as a line break. */
  title: string
  body: string
  points: readonly HowItWorksFeaturePoint[]
  /** Visual sits left of the copy on desktop. */
  mediaFirst: boolean
  /** Secondary CTA next to "Get started". */
  secondaryCta?: { label: string; to: string }
}

export const HOW_IT_WORKS_FEATURE_GET_STARTED_LABEL = "Get started"

export const HOW_IT_WORKS_FEATURES: readonly HowItWorksFeature[] = [
  {
    id: "feedback",
    eyebrow: "Feedback",
    title: "Read the experience.\nMake the reply personal.",
    body: "Open the comment alongside its restaurant and date. See what needs attention and give your team a useful starting point for a thoughtful reply.",
    points: [
      {
        id: "read-original",
        title: "Read the original",
        body: "Start with the customer’s own explanation.",
      },
      {
        id: "focus-concerns",
        title: "Focus on concerns",
        body: "Give a disappointing experience a considered response.",
      },
      {
        id: "learn-with-team",
        title: "Learn with the team",
        body: "Discuss recurring comments at the next service review.",
      },
    ],
    mediaFirst: false,
  },
  {
    id: "guests",
    eyebrow: "Guests",
    title: "Keep the customer context for next time.",
    body: "See previous feedback and offer activity alongside the latest comment. Build a more considered picture of the people you serve.",
    points: [
      {
        id: "previous-comments",
        title: "See previous comments",
        body: "Put the latest feedback in context.",
      },
      {
        id: "understand-experience",
        title: "Understand the experience",
        body: "Review recorded feedback and offer activity.",
      },
      {
        id: "customer-choices",
        title: "Respect customer choices",
        body: "Use email or text marketing only for people with the appropriate permission.",
      },
    ],
    mediaFirst: true,
  },
  {
    id: "campaigns",
    eyebrow: "Campaigns & Offers",
    title: "Give customers a relevant invitation.",
    body: "Choose an offer or update, select your email or text audience and check the wording, terms and timing. Review the credit estimate, then confirm when you are ready.",
    points: [
      {
        id: "choose-invitation",
        title: "Choose the invitation",
        body: "Set an offer or update that fits your restaurant.",
      },
      {
        id: "review-audience",
        title: "Review the audience",
        body: "Check the customers and channel before confirming.",
      },
      {
        id: "see-response",
        title: "See the response",
        body: "Review campaign engagement and confirmed offer use.",
      },
    ],
    mediaFirst: false,
  },
  {
    id: "reports",
    eyebrow: "Reports & Briefs",
    title: "Learn from the response.\nPlan what comes next.",
    body: "Review the comments customers keep making, engagement with your messages and confirmed offer redemptions. Use that information to refine a promotion or focus the next team conversation.",
    points: [
      {
        id: "review-feedback",
        title: "Review feedback",
        body: "See what customers keep mentioning.",
      },
      {
        id: "review-engagement",
        title: "Review engagement",
        body: "Look at the response to messages and offers.",
      },
      {
        id: "plan-discussion",
        title: "Plan the next discussion",
        body: "Use the evidence to decide what the team should examine.",
      },
    ],
    mediaFirst: true,
  },
  {
    id: "ai-assistant",
    eyebrow: "AI Assistant",
    title: "Let AI help with the first draft.",
    body: "Ask for a summary of this week’s comments or help writing a reply. Check the source comments and review the draft before you use it.",
    points: [
      {
        id: "summarise-comments",
        title: "Summarise the comments",
        body: "Ask for the recurring points in your selected period.",
      },
      {
        id: "explore-question",
        title: "Explore a question",
        body: "Check the source information behind an answer.",
      },
      {
        id: "prepare-draft",
        title: "Prepare a draft",
        body: "Review AI-assisted wording before using it.",
      },
    ],
    mediaFirst: false,
    secondaryCta: { label: "See Tummly AI", to: MARKETING_AI_ASSISTANT_PATH },
  },
]

/** Alt text for the product-UI visuals. */
export const HOW_IT_WORKS_FEATURE_VISUAL_ALT: Record<
  HowItWorksFeatureId,
  string
> = {
  feedback:
    "Tummly Feedback list headed What guests told you this week, with a negative comment and a Reply to Sophie button",
  guests:
    "Tummly Guests table with a guest profile card showing feedback history and a Send an offer button",
  campaigns:
    "Tummly campaign draft Come back this Sunday with a free dessert offer, audience and send time",
  reports:
    "Tummly weekly brief for 13 to 19 October with guest, feedback and offer figures and a feedback chart",
  "ai-assistant":
    "Tummly AI Assistant panel answering what to act on this week with a recommended next step",
}
