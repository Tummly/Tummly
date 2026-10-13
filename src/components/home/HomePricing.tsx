import { AiAssistantPlans } from "@/components/marketing/ai-assistant/AiAssistantPlans"
import { HOME_PRICING } from "@/content/marketing/homePage"

/**
 * Home Pricing — Figma `4902:19866`.
 * Same plan strip as AI Assistant plans; Pricing header, no AI allowance row.
 */
export function HomePricing() {
  return (
    <AiAssistantPlans
      content={HOME_PRICING}
      showAiAllowance={false}
      surfaceClassName="bg-white"
      compactHeader
    />
  )
}
