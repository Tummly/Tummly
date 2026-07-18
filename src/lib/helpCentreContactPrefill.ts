import type { HelpCentreContactPrefill } from "@/types/helpCentre"

/** Prefill restaurantLocationId for the Help Centre contact form. */
export function resolveHelpCentreContactPrefillLocationId(
  locations: HelpCentreContactPrefill["locations"]
): string {
  if (locations.length === 0) {
    return ""
  }

  return String(locations[0].id)
}
