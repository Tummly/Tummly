/** Campaign wizard chrome — Goal (step 0) + numbered steps 1–6 (ticket 22 / Figma 4691:47781). */

export type CampaignGoalId =
  | "thank-recent-guests"
  | "boost-quieter-time"
  | "re-engage-inactive"
  | "promote-something-new"
  | "follow-up-completed-recovery"
  | "custom-campaign"

export type CampaignWizardStepId =
  | "goal"
  | "audience"
  | "channel"
  | "offer"
  | "message"
  | "schedule"
  | "review"
  | "success"

export type CampaignGoalOption = {
  id: CampaignGoalId
  title: string
  description: string
}

export const CAMPAIGN_WIZARD_COPY = {
  pageTitle: "Create a campaign",
  goalStepHeading: "What do you want this campaign to do?",
  goalStepDescription:
    "Choose a practical goal. Tummly will suggest a suitable audience, channel and campaign structure for you to review.",
  preferTemplatePrompt: "Prefer a starting template?",
  browseTemplates: "Browse templates",
  continue: "Continue",
} as const

/** Numbered strip for steps 1–6 (Goal has no numbered strip). */
export const CAMPAIGN_WIZARD_NUMBERED_STEPS = [
  { id: "audience", label: "Audience" },
  { id: "channel", label: "Channel" },
  { id: "offer", label: "Offer" },
  { id: "message", label: "Message" },
  { id: "schedule", label: "Schedule" },
  { id: "review", label: "Review" },
] as const satisfies ReadonlyArray<{ id: CampaignWizardStepId; label: string }>

export const CAMPAIGN_GOAL_OPTIONS: readonly CampaignGoalOption[] = [
  {
    id: "thank-recent-guests",
    title: "Thank recent guests",
    description:
      "Send a simple thank-you to guests who recently joined your guest list.",
  },
  {
    id: "boost-quieter-time",
    title: "Boost a quieter time",
    description: "Invite eligible guests back during a day or time you choose.",
  },
  {
    id: "re-engage-inactive",
    title: "Re-engage inactive guests",
    description:
      "Reach marketing-eligible guests with no recent Guest Loop activity.",
  },
  {
    id: "promote-something-new",
    title: "Promote something new",
    description:
      "Tell eligible guests about a new menu item, restaurant update or limited-time offer.",
  },
  {
    id: "follow-up-completed-recovery",
    title: "Follow up after completed recovery",
    description:
      "Send a considered follow-up after a private feedback case has been handled.",
  },
  {
    id: "custom-campaign",
    title: "Custom campaign",
    description:
      "Start without a predefined campaign structure and configure each step yourself.",
  },
] as const

/**
 * Select / popover menus inside the Campaign wizard must sit above
 * OperatorWizardShell (`z-[130]`). Pass as SelectContent / PopoverContent
 * `className` when later steps add menus (mirrors FEEDBACK_RECOVERY_SELECT_MENU_CLASS).
 */
export const CAMPAIGN_WIZARD_SELECT_MENU_CLASS = "z-[140]"

export function labelForCampaignGoalId(
  goalId: CampaignGoalId | null
): string | null {
  if (goalId == null) {
    return null
  }
  return CAMPAIGN_GOAL_OPTIONS.find((goal) => goal.id === goalId)?.title ?? null
}

export function formatCampaignWizardHeaderSubtitle(input: {
  goalId: CampaignGoalId | null
  locationName: string
  now: Date
}): string {
  const month = new Intl.DateTimeFormat("en-GB", { month: "long" }).format(
    input.now
  )
  const goalLabel = labelForCampaignGoalId(input.goalId)
  if (goalLabel == null) {
    return `${input.locationName} · ${month}`
  }
  return `${goalLabel} · ${input.locationName} · ${month}`
}
