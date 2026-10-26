import { HELP_CENTRE_CONTACT_URL } from "@/config/support"
import { PRICING_CADENCE_COPY } from "@/content/marketing/pricingPage"
import {
  CUSTOM_PLAN_PACKS,
  type CustomPlanPackCounts,
} from "@/lib/marketingCustomPlan/customPlanEstimate"
import type { BillingCadence } from "@/lib/operatorBillingCredits/managePlanPresentation"

/** Contact form topic for calculator enquiries. */
export const CUSTOM_PLAN_CONTACT_TOPIC = "plans-and-pricing"

export type CustomPlanContactInput = {
  locations: number
  cadence: BillingCadence
  packs: CustomPlanPackCounts
}

/** The contact form's Location range for a count; empty with no Locations. */
export function locationCountOptionFor(locations: number): string {
  if (locations < 1) return ""
  if (locations === 1) return "1"
  if (locations <= 3) return "2-3"
  if (locations <= 5) return "4-5"
  if (locations <= 10) return "6-10"
  if (locations <= 20) return "11-20"
  if (locations <= 30) return "21-30"
  return "30+"
}

/** Plain-text summary of the calculator, one line for each chosen pack. */
export function buildCustomPlanContactMessage({
  locations,
  cadence,
  packs,
}: CustomPlanContactInput): string {
  const count = locations.toLocaleString("en-GB")
  const chosen = CUSTOM_PLAN_PACKS.filter((pack) => packs[pack.packKey] > 0)
  const lines = [
    `I would like a Tummly plan for ${count} Location${locations === 1 ? "" : "s"}.`,
    "",
    `Billing: ${PRICING_CADENCE_COPY[cadence]}`,
  ]
  if (chosen.length === 0) {
    lines.push("Additional packs: none")
  } else {
    lines.push("Additional packs:")
    for (const pack of chosen) {
      lines.push(`- ${pack.title} × ${packs[pack.packKey]}`)
    }
  }
  return lines.join("\n")
}

/** Contact form link with the topic, Location range and plan summary filled in. */
export function buildCustomPlanContactHref(input: CustomPlanContactInput): string {
  const params = new URLSearchParams({ topic: CUSTOM_PLAN_CONTACT_TOPIC })
  const locationCount = locationCountOptionFor(input.locations)
  if (locationCount !== "") {
    params.set("locations", locationCount)
  }
  params.set("message", buildCustomPlanContactMessage(input))
  return `${HELP_CENTRE_CONTACT_URL}?${params.toString()}`
}
