import {
  MARKETING_PRICING_PATH,
  MARKETING_PRODUCT_PATH,
} from "@/constants/marketingNav"
import { buildSignupPath } from "@/lib/signupPlanIntent"

/** Figma Demo version section `5744:20202` — public product walkthrough. */
export const PRODUCT_DEMO = {
  venueLine: "Mavero Kitchen — Camden High Street",
  welcome: {
    eyebrow: "Restaurant view · Demo",
    title: "Welcome to Tummly",
    body: "See how Tummly helps a restaurant turn guest feedback into the right next action, from receiving feedback and recovering a guest to building engagement and understanding performance.",
    primaryLabel: "Start tour",
    footNote: "No account needed",
  },
  tour: {
    fabLabel: "Tour guide",
    /** Figma badge shows total tour steps, not the current index. */
    fabBadgeTotal: 4,
    exploreTitle: "Explore the Guest Loop",
    exploreBody:
      "See how Tummly turns guest feedback into action and repeat engagement.",
    steps: [
      {
        id: "feedback",
        path: "feedback",
        title: "1. Feedback & recovery",
        body: "See where feedback arrives, understand the issue and recover the guest.",
        tipTitle: "Feedback arrives here",
        tipBody:
          "Guest feedback collected through Tummly appears in the Feedback section, where the restaurant can review, manage and follow up on it.",
      },
      {
        id: "guests",
        path: "guests",
        title: "2. Guest context",
        body: "See how Tummly keeps guest information, permissions and interaction history together.",
        tipTitle: "Guest context lives here",
        tipBody:
          "Each guest profile keeps permissions, notes and history together so follow-up stays accurate.",
      },
      {
        id: "offers",
        path: "offers",
        title: "3. Offers & Campaigns",
        body: "See how eligible guest relationships can continue through targeted Offers and Campaigns.",
        tipTitle: "Offers and Campaigns",
        tipBody:
          "Use Offers and Campaigns to continue the relationship with guests who are eligible to hear from you.",
      },
      {
        id: "reports",
        path: "reports",
        title: "4. Reports & AI",
        body: "See how Tummly turns activity into insight, recommendations and AI-assisted next actions.",
        tipTitle: "Reports and AI",
        tipBody:
          "Reports and the AI Assistant turn Guest Loop activity into clear next steps for the restaurant.",
      },
    ],
  },
  exit: {
    title: "Ready to try this with your own guests?",
    body: "Start Tummly with one verified Location. Set up your Guest Form and QR placements, then experience the same Guest Loop with real guests at your restaurant.",
    bullets: ["No payment card required", "No automatic paid renewal"],
    primaryLabel: "Start 30-day Pilot",
    primaryTo: buildSignupPath({ plan: "Pilot" }),
    secondaryLabel: "See pricing",
    secondaryTo: MARKETING_PRICING_PATH,
    continueLabel: "Continue exploring the demo",
    productTo: MARKETING_PRODUCT_PATH,
  },
} as const

export type ProductDemoTourStepId =
  (typeof PRODUCT_DEMO.tour.steps)[number]["id"]
