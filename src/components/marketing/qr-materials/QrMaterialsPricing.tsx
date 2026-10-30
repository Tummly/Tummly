import { AiAssistantPlans } from "@/components/marketing/ai-assistant/AiAssistantPlans"
import { QR_MATERIALS_PRICING } from "@/content/marketing/qrMaterialsPage"

/**
 * Figma QR materials pricing `6859:12685` (Pricing instance `7190:11029`).
 * Home Pricing plan strip with the compare button under the cards.
 */
export function QrMaterialsPricing() {
  return (
    <AiAssistantPlans
      content={QR_MATERIALS_PRICING}
      showAiAllowance={false}
      splitHeader
      compareBelow
      sublineSource="card"
    />
  )
}
