import type { LocationGuestMarketingPreference } from "@/types/dashboard"

export function isLocationGuestMarketingIneligible(
  preference: LocationGuestMarketingPreference | undefined
): boolean {
  return preference !== "allowed"
}
