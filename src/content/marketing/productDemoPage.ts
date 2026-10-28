import {
  MARKETING_PRICING_PATH,
  MARKETING_PRODUCT_PATH,
} from "@/constants/marketingNav"
import { buildSignupPath } from "@/lib/signupPlanIntent"

/** Public product walkthrough — E2E Guest Loop tour (§3–12). */
export const PRODUCT_DEMO = {
  venueLine: "Mavero Kitchen — Camden High Street",
  entry: {
    headline: "Experience Tummly from both sides.",
    body: "Start as a guest at Mavero Kitchen - Camden High Street. Leave sample feedback, then see how it appears inside Tummly and what the restaurant can do next.",
    qrLabel: "Scan to try on your phone",
    openGuestFormLabel: "Open Guest Form",
    restaurantSkipLabel: "See how guest feedback appears in Tummly",
    reassurance: "No account needed. Demo only. No messages are sent.",
    unavailableTitle: "Demo is temporarily unavailable",
    unavailableBody:
      "You can still start a 30-day Pilot or see pricing while we restore the demo.",
    restartLabel: "Restart demo",
    sessionExpiredTitle: "This demo session ended",
    sessionExpiredBody:
      "Restart the demo to begin again from the Mavero Kitchen sample account.",
  },
  guestThankYou: {
    headline: "Thanks - your feedback has been received.",
    offerLabel: "DEMO OFFER - NOT REDEEMABLE",
    offerTitle: "20% off your next order",
    primaryLabel: "See how guest feedback appears in Tummly",
    secondaryLabel: "Start 30-day Pilot",
  },
  welcome: {
    eyebrow: "RESTAURANT VIEW - DEMO",
    title: "Welcome to Tummly",
    body: "See how Tummly helps a restaurant turn guest feedback into the right next action - from receiving feedback and recovering a guest to building engagement and understanding performance.",
    bodyWithSubmission:
      "See how your feedback appears in Tummly and what the restaurant can do next - from recovery to engagement and performance.",
    primaryLabel: "Start tour",
    safetyLine: "Demo only - nothing will be sent or changed outside this demo.",
  },
  tour: {
    fabLabel: "Tour guide",
    fabBadgeTotal: 4,
    exploreTitle: "Explore the Guest Loop",
    exploreBody:
      "See how Tummly turns guest feedback into action and repeat engagement.",
    continueLabel: "Continue tour",
    completeBody:
      "You have seen how Tummly connects Feedback, recovery, guest context, engagement and insight.",
    chapters: [
      {
        id: "feedback",
        label: "Feedback & recovery",
        // Step 1 points at the Feedback nav item, so the chapter starts on Home.
        route: "home" as const,
        steps: [
          {
            id: "ch1-s1",
            title: "Feedback arrives here",
            body: "Guest feedback collected through Tummly appears here, where the restaurant can review it, manage follow-up and take the right next action.",
            ctaLabel: "Open Feedback",
            target: "nav-feedback",
            action: "navigateFeedback" as const,
          },
          {
            id: "ch1-s2",
            title: "Find the feedback that needs attention",
            body: "Tummly brings each guest submission into the Feedback inbox with its source, issue tags and workflow status.",
            ctaLabel: "View feedback",
            target: "feedback-needs-attention-row",
            action: "openFeedbackDetails" as const,
          },
          {
            id: "ch1-s3",
            title: "Understand what happened",
            body: "See the guest's original feedback together with the Location, source, issue and guest context needed to decide what to do next.",
            ctaLabel: "See recovery options",
            target: "feedback-detail-panel",
            action: "none" as const,
          },
          {
            id: "ch1-s4",
            title: "Start a recovery",
            body: "When feedback needs a response, the restaurant can begin a recovery directly from the Feedback record.",
            ctaLabel: "Start recovery",
            target: "feedback-start-recovery",
            action: "openRecovery" as const,
          },
          {
            id: "ch1-s5",
            title: "Prepare the right response",
            body: "Tummly can help prepare a personalised recovery response and, where appropriate, include a controlled recovery Offer.",
            ctaLabel: "Review recovery",
            target: "feedback-recovery-drawer",
            action: "none" as const,
          },
          {
            id: "ch1-s6",
            title: "Track the outcome",
            body: "Tummly keeps the recovery connected to the original feedback so the team can track what happened next.",
            ctaLabel: "Complete chapter",
            target: "feedback-recovery-summary",
            action: "completeChapter" as const,
          },
        ],
      },
      {
        id: "guests",
        label: "Guest context",
        route: "guests" as const,
        steps: [
          {
            id: "ch2-s1",
            title: "Guest context builds over time",
            body: "Tummly brings guest interactions together so the restaurant can understand the relationship beyond one response.",
            ctaLabel: "Open guest",
            target: "guest-high-engagement",
            action: "openGuestProfile" as const,
          },
          {
            id: "ch2-s2",
            title: "See the relationship, not just one response",
            body: "See how feedback, visits and engagement build a useful guest history over time.",
            ctaLabel: "See permissions",
            target: "guest-profile-panel",
            action: "none" as const,
          },
          {
            id: "ch2-s3",
            title: "Permission controls what happens next",
            body: "Tummly keeps permission state alongside the guest record so the restaurant knows which actions are allowed.",
            ctaLabel: "Complete chapter",
            target: "guest-permissions",
            action: "completeChapter" as const,
          },
        ],
      },
      {
        id: "offers",
        label: "Offers & Campaigns",
        route: "offers" as const,
        steps: [
          {
            id: "ch3-s1",
            title: "Create a governed Offer",
            body: "Offers can be configured with value, validity, claim frequency and redemption rules before they are used with guests.",
            ctaLabel: "View Offer",
            target: "offer-primary-row",
            action: "openOffer" as const,
          },
          {
            id: "ch3-s2",
            title: "Track claims and redemptions",
            body: "Tummly tracks what was claimed and what was actually redeemed so the restaurant can understand real usage.",
            ctaLabel: "Open Campaigns",
            target: "offer-metrics",
            action: "navigateCampaigns" as const,
          },
          {
            id: "ch3-s3",
            title: "Reach eligible guests",
            body: "Campaigns use eligible guest audiences and keep permission/suppression rules authoritative.",
            ctaLabel: "View campaign",
            target: "campaign-list",
            action: "openCampaign" as const,
          },
          {
            id: "ch3-s4",
            title: "See what actually worked",
            body: "See the full path from message delivery to engagement, Offer claim and redemption.",
            ctaLabel: "Complete chapter",
            target: "campaign-report",
            action: "completeChapter" as const,
          },
        ],
      },
      {
        id: "reports",
        label: "Reports & AI",
        route: "reports" as const,
        steps: [
          {
            id: "ch4-s1",
            title: "See the Guest Loop as a system",
            body: "Reports connect Capture, Feedback, Guests, Offers and Campaigns so the restaurant can understand what happened across the Guest Loop.",
            ctaLabel: "Open Weekly Brief",
            target: "reports-weekly-brief",
            action: "openWeeklyBrief" as const,
          },
          {
            id: "ch4-s2",
            title: "Know what changed this week",
            body: "The Weekly Brief summarises what changed, what needs attention and what the restaurant should do next.",
            ctaLabel: "Ask AI",
            target: "weekly-brief-panel",
            action: "openAi" as const,
          },
          {
            id: "ch4-s3",
            title: "Ask Tummly what to do next",
            body: 'Ask a real operational question such as: "Summarise feedback from the last 30 days and tell me what needs attention."',
            ctaLabel: "Show answer",
            target: "ai-assistant-panel",
            action: "showAiAnswer" as const,
          },
          {
            id: "ch4-s4",
            title: "Turn insight into a governed next step",
            body: "Tummly can explain performance, recommend a next step and prepare governed work for the operator to review.",
            ctaLabel: "Complete tour",
            target: "ai-assistant-panel",
            action: "completeTour" as const,
          },
        ],
      },
    ],
  },
  exit: {
    title: "Ready to try this with your own guests?",
    body: "Start Tummly with one verified Location. Set up your Guest Form and QR placements, then experience the same Guest Loop with real guests at your restaurant.",
    trustLine: "30-day Pilot · No payment card required · No automatic paid renewal",
    bullets: ["No payment card required", "No automatic paid renewal"],
    primaryLabel: "Start 30-day Pilot",
    primaryTo: buildSignupPath({ plan: "Pilot" }),
    secondaryLabel: "See pricing",
    secondaryTo: MARKETING_PRICING_PATH,
    productTo: MARKETING_PRODUCT_PATH,
  },
} as const

export type ProductDemoChapterId =
  (typeof PRODUCT_DEMO.tour.chapters)[number]["id"]

export type ProductDemoTourStepId =
  (typeof PRODUCT_DEMO.tour.chapters)[number]["steps"][number]["id"]

export type ProductDemoTourAction =
  (typeof PRODUCT_DEMO.tour.chapters)[number]["steps"][number]["action"]

export function productDemoTotalSteps(): number {
  return PRODUCT_DEMO.tour.chapters.reduce(
    (sum, chapter) => sum + chapter.steps.length,
    0
  )
}

export function productDemoChapterProgressPercent(
  completedChapterIds: readonly string[]
): number {
  const total = PRODUCT_DEMO.tour.chapters.length
  if (total === 0) return 0
  return Math.round((completedChapterIds.length / total) * 100)
}

/** Flat step index across all chapters (0-based). */
export function productDemoFlatStepIndex(
  chapterIndex: number,
  stepIndex: number
): number {
  let flat = 0
  for (let i = 0; i < chapterIndex; i++) {
    flat += PRODUCT_DEMO.tour.chapters[i]?.steps.length ?? 0
  }
  return flat + stepIndex
}
