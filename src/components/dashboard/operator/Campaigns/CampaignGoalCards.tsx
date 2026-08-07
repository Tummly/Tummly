import type { LucideIcon } from "lucide-react"
import {
  GiftIcon,
  HistoryIcon,
  MegaphoneIcon,
  RefreshCwIcon,
  SquarePenIcon,
  UserRoundIcon,
} from "lucide-react"

import { Button } from "@/components/ui/button"
import type { CampaignGoalId } from "@/lib/operatorCampaigns/campaignWizardPresentation"
import type { CampaignWizardGoalCardViewModel } from "@/lib/operatorCampaigns/createCampaignWizardModule"
import { cn } from "@/lib/utils"

const GOAL_ICONS: Record<CampaignGoalId, LucideIcon> = {
  "thank-recent-guests": GiftIcon,
  "boost-quieter-time": HistoryIcon,
  "re-engage-inactive": UserRoundIcon,
  "promote-something-new": MegaphoneIcon,
  "follow-up-completed-recovery": RefreshCwIcon,
  "custom-campaign": SquarePenIcon,
}

type CampaignGoalCardsProps = {
  goals: readonly CampaignWizardGoalCardViewModel[]
  onSelectGoal: (goalId: CampaignGoalId) => void
}

/** Figma Goal cards — selectable 2×3 grid (4691:47781). */
export function CampaignGoalCards({
  goals,
  onSelectGoal,
}: CampaignGoalCardsProps) {
  return (
    <div
      className="flex w-full flex-col gap-5"
      role="radiogroup"
      aria-label="Campaign goal"
    >
      <div className="grid grid-cols-1 gap-5 md:grid-cols-3">
        {goals.map((goal) => {
          const Icon = GOAL_ICONS[goal.id]
          return (
            <Button
              key={goal.id}
              type="button"
              variant="ghost"
              role="radio"
              aria-checked={goal.selected}
              className={cn(
                "h-auto min-h-0 flex-col items-start gap-[22px] rounded-[6px] border px-[22px] py-5 text-left whitespace-normal hover:bg-transparent",
                goal.selected
                  ? "border-op-text-muted bg-op-background-primary"
                  : "border-op-card-border bg-op-background-primary hover:border-op-text-muted"
              )}
              onClick={() => {
                onSelectGoal(goal.id)
              }}
            >
              <span className="flex size-10 shrink-0 items-center justify-center rounded-[2px] bg-op-background-secondary p-2.5">
                <Icon className="size-5 text-op-text-primary" aria-hidden />
              </span>
              <span className="flex w-full flex-col gap-2">
                <span className="text-lg font-medium leading-normal text-op-text-primary">
                  {goal.title}
                </span>
                <span className="text-xs font-normal leading-[19px] text-[var(--op-color-gray-550)]">
                  {goal.description}
                </span>
              </span>
            </Button>
          )
        })}
      </div>
    </div>
  )
}
