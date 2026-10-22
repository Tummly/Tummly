/** Signup plan step + Compare plans dialog copy — Figma `6775:39425`, `6780:42627`. */

export type SignupPlanCardId = "Pilot" | "Starter" | "Growth" | "Group"

export const SIGNUP_PLAN_CARD_IDS: readonly SignupPlanCardId[] = [
  "Pilot",
  "Starter",
  "Growth",
  "Group",
] as const

export const SIGNUP_PLAN_STEP_COPY = {
  title: "Choose how you'd like to start",
  body: "Choose the option that best fits your restaurant. Compare plans or start with one Location on the 30-day Pilot.",
  seeFeatures: "See full features list",
  customiseTitle: "Make your plan work for your restaurants.",
  customiseBody:
    "Need more Locations, team users, active Offers, QR placements or monthly Email, SMS and AI allowances? Customise your selected plan and pay only for the additional capacity you need.",
  customiseCta: "Customise your plan",
  back: "Back",
  checkout: "Check out",
  startPilot: "Start 30-day Pilot",
  selected: "Selected plan",
} as const

export const SIGNUP_PLAN_DESCRIPTIONS: Record<SignupPlanCardId, string> = {
  Pilot: "Try Tummly at one real Location before choosing a paid plan.",
  Starter:
    "For one Location ready to collect private Feedback, build a guest list and bring guests back.",
  Growth:
    "For growing restaurants and small groups that need more reach and multi-Location visibility.",
  Group:
    "For growing restaurant groups that need more Locations, control and consolidated reporting.",
}

export const SIGNUP_PILOT_PRICE = {
  amount: "£0",
  suffix: "/ for 30 days",
  subline: "No payment card required. No automatic paid renewal.",
} as const

const NONE = "—"
const INCLUDED = "Included"

export type SignupCompareRow = {
  label: string
  values: Record<SignupPlanCardId, string>
}

/** Feature rows of the Compare plans dialog (Figma `6780:42627`). */
export const SIGNUP_COMPARE_ROWS: readonly SignupCompareRow[] = [
  { label: "Locations", values: { Pilot: "1", Starter: "1", Growth: "Up to 3", Group: "Up to 5" } },
  { label: "Users", values: { Pilot: "2", Starter: "3", Growth: "10", Group: "25" } },
  {
    label: "Active QR placements",
    values: {
      Pilot: "Up to 5",
      Starter: "Up to 10",
      Growth: "Up to 25 per Location",
      Group: "Up to 50 per Location",
    },
  },
  { label: "Private Feedback", values: { Pilot: INCLUDED, Starter: INCLUDED, Growth: INCLUDED, Group: INCLUDED } },
  { label: "Guest list & permissions", values: { Pilot: INCLUDED, Starter: INCLUDED, Growth: INCLUDED, Group: INCLUDED } },
  { label: "Feedback recovery", values: { Pilot: INCLUDED, Starter: INCLUDED, Growth: INCLUDED, Group: INCLUDED } },
  { label: "Campaigns", values: { Pilot: INCLUDED, Starter: INCLUDED, Growth: INCLUDED, Group: INCLUDED } },
  { label: "Saved audiences", values: { Pilot: NONE, Starter: NONE, Growth: INCLUDED, Group: INCLUDED } },
  {
    label: "Active Offers",
    values: {
      Pilot: "1",
      Starter: "Up to 3",
      Growth: "Up to 10 account-wide",
      Group: "Up to 25 account-wide",
    },
  },
  { label: "Multi-Location reporting", values: { Pilot: NONE, Starter: NONE, Growth: INCLUDED, Group: INCLUDED } },
  { label: "Location-scoped permissions", values: { Pilot: NONE, Starter: NONE, Growth: NONE, Group: INCLUDED } },
  {
    label: "Weekly Brief",
    values: {
      Pilot: NONE,
      Starter: INCLUDED,
      Growth: "Location + consolidated",
      Group: "Location + consolidated",
    },
  },
  { label: "AI", values: { Pilot: "20 actions once", Starter: "100/month", Growth: "500/month", Group: "1,500/month" } },
  {
    label: "Email",
    values: {
      Pilot: "500 credits once",
      Starter: "2,500/month",
      Growth: "10,000/month",
      Group: "25,000/month",
    },
  },
  { label: "SMS", values: { Pilot: "20 credits once", Starter: "100/month", Growth: "350/month", Group: "700/month" } },
  { label: "Top-ups", values: { Pilot: "Not available", Starter: "Available", Growth: "Available", Group: "Available" } },
  {
    label: "Support",
    values: {
      Pilot: "Pilot support",
      Starter: "Standard Email",
      Growth: "Priority Email",
      Group: "Priority + assisted onboarding",
    },
  },
]
