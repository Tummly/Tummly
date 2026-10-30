import { AiAssistantPlans } from "@/components/marketing/ai-assistant/AiAssistantPlans"
import { HOME_PRICING } from "@/content/marketing/homePage"

/**
 * Home Pricing — Figma `4902:19866` (Pricing instance `7190:11030`).
 * Same plan strip as AI Assistant plans: split header (Compare all plans on the
 * right), location-limit sublines, no AI allowance row.
 */
export function HomePricing() {
  return (
    <AiAssistantPlans
      content={HOME_PRICING}
      showAiAllowance={false}
      splitHeader
      sublineSource="card"
    />
  )
}
