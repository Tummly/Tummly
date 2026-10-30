import { AiAssistantPlans } from "@/components/marketing/ai-assistant/AiAssistantPlans"
import { PRODUCT_PRICING } from "@/content/marketing/productPage"

/**
 * Product Pricing — Figma `6823:36468` (Pricing instance `7190:11029`).
 * Same plan strip as Home Pricing: location-limit sublines, no AI allowance
 * row, "Compare all plans" under the cards.
 */
export function ProductPricing() {
  return (
    <AiAssistantPlans
      content={PRODUCT_PRICING}
      showAiAllowance={false}
      sublineSource="card"
      surfaceClassName="bg-[#f0f0f0] rounded-bl-[12px] rounded-br-[12px]"
    />
  )
}
